"""
Veritabani Modulu -- Baglanti Havuzu + CRUD Islemleri
Tum SQL islemleri burada merkezlestirilir.

mtturkey_exchange DB'sindeki TgXxx tablolari kullanilir (PascalCase sutunlar).
"""
import pyodbc
import logging
import time
from datetime import datetime
from contextlib import contextmanager
from config import Config

logger = logging.getLogger(__name__)

# ===================================================
# BAGLANTI HAVUZU
# ===================================================
# Havuzdaki her girdi (conn, created_at) tuple'i -- pyodbc.Connection
# nesnelerine keyfi ozellik eklemek guvenilir olmadigi icin yaratilma zamani
# ayri tutuluyor. MAX_CONN_LIFETIME'i asan baglantilar, canlilik kontrolunden
# gecse bile zorla yenilenir (uzun sureli SQL Server tarafli timeout/DNS
# degisikligi gibi durumlara karsi).

_pool = []
_POOL_SIZE = 5
_MAX_CONN_LIFETIME = 1800  # saniye (30 dakika)


def _create_connection():
    """Yeni bir DB baglantisi olustur"""
    try:
        conn = pyodbc.connect(Config.connection_string(), timeout=10)
        conn.autocommit = False
        return conn
    except Exception as e:
        logger.error(f"DB baglanti hatasi: {e}")
        return None


@contextmanager
def get_conn():
    """
    Baglanti havuzundan baglanti al, islem bitince geri koy.
    Kullanim:
        with get_conn() as conn:
            cursor = conn.cursor()
            cursor.execute(...)
            conn.commit()
    """
    conn = None
    while _pool:
        pooled_conn, created_at = _pool.pop()
        if time.monotonic() - created_at > _MAX_CONN_LIFETIME:
            try:
                pooled_conn.close()
            except Exception:
                pass
            continue
        try:
            pooled_conn.cursor().execute("SELECT 1")
            conn, conn_created_at = pooled_conn, created_at
            break
        except Exception:
            try:
                pooled_conn.close()
            except Exception:
                pass

    if conn is None:
        conn = _create_connection()
        conn_created_at = time.monotonic()

    if conn is None:
        raise ConnectionError("Veritabanina baglanilmadi")

    try:
        yield conn
    except Exception:
        try:
            conn.rollback()
        except Exception:
            pass
        raise
    finally:
        if len(_pool) < _POOL_SIZE:
            try:
                conn.rollback()
                _pool.append((conn, conn_created_at))
            except Exception:
                try:
                    conn.close()
                except Exception:
                    pass
        else:
            try:
                conn.close()
            except Exception:
                pass


# kwargs -> SQL sutun adi donusumu (update_transaction icin)
_TX_COL_MAP = {
    'status': 'Status',
    'completed_at': 'CompletedAt',
    'assigned_operator_id': 'AssignedOperatorId',
    'completion_code': 'CompletionCode',
    'txid': 'Txid',
    'crypto_verified': 'CryptoVerified',
    'crypto_verified_at': 'CryptoVerifiedAt',
    'state_data': 'StateData',
    'idempotency_key': 'IdempotencyKey',
    'exchange_rate': 'ExchangeRate',
    'try_amount': 'TryAmount',
    'currency': 'Currency',
    'amount': 'Amount',
    'referral_code': 'ReferralCode',
    'isBuy': 'IsBuy',
    'delivery_method': 'DeliveryMethod',
    'customer_address': 'CustomerAddress',
    'customer_phone': 'CustomerPhone',
}

_CD_COL_MAP = {
    'status': 'Status',
    'confirmations': 'Confirmations',
    'amount': 'Amount',
    'network': 'Network',
}


# ===================================================
# TABLO DOGRULAMA
# ===================================================

def init_database():
    """TgXxx tablolarinin var oldugunu dogrula (EF Core tarafindan olusturulur)"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT COUNT(*) FROM sysobjects
                WHERE name IN (
                    'TgCustomers','TgTransactions','TgMessages','TgOperators',
                    'TgChatSessions','TgCryptoDeposits','TgBankProviders',
                    'TgBotHeartbeats','TgApiQueue','TgDealers','TgExchangeRates'
                ) AND xtype='U'
            """)
            count = c.fetchone()[0]
            if count >= 9:
                logger.info(f"Veritabani tablolari hazir ({count}/11 Tg tablo)")
                return True
            else:
                logger.error(f"Eksik tablolar! Sadece {count}/11 Tg tablo mevcut")
                return False
    except Exception as e:
        logger.error(f"Tablo dogrulama hatasi: {e}")
        return False


# ===================================================
# CUSTOMER ISLEMLERI
# ===================================================

def upsert_customer(user):
    """Musteriyi kaydet veya guncelle"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            lang = getattr(user, 'language_code', 'tr') or 'tr'
            c.execute("""
                IF EXISTS (SELECT 1 FROM TgCustomers WHERE CustomerId = ?)
                    UPDATE TgCustomers SET
                        FirstName = ?, LastName = ?, Username = ?,
                        LanguageCode = ?, LastActivity = GETDATE()
                    WHERE CustomerId = ?
                ELSE
                    INSERT INTO TgCustomers
                        (CustomerId, FirstName, LastName, Username, LanguageCode, CreatedAt, LastActivity)
                    VALUES (?, ?, ?, ?, ?, GETDATE(), GETDATE())
            """,
                user.id, user.first_name, user.last_name, user.username, lang, user.id,
                user.id, user.first_name, user.last_name, user.username, lang
            )
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"Musteri kaydetme hatasi: {e}")
        return False


def set_customer_referral(customer_id, referral_code):
    """Musterinin henuz islem baslatmadan once secilen bayi/referans kodunu kalici olarak sakla.

    set_user_state/get_user_state TgTransactions tablosuna yazar ve musterinin ZATEN bir
    islem kaydi olmasini gerektirir -- ama referans kodu tam olarak islem henuz yokken
    (QR okutuldu, grup katilim bekleniyor) belirleniyor. O yuzden burada TgCustomers'in
    kendi ReferralCode kolonu kullanilir; bu satir upsert_customer ile zaten olusturulmus
    olur, boylece bot yeniden baslasa bile (context.user_data sifirlansa bile) kalici kalir.
    """
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("UPDATE TgCustomers SET ReferralCode = ? WHERE CustomerId = ?",
                      referral_code, customer_id)
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"Musteri referans kaydetme hatasi: {e}")
        return False


def get_customer_referral(customer_id):
    """set_customer_referral ile kaydedilen bekleyen referans kodunu getirir."""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("SELECT ReferralCode FROM TgCustomers WHERE CustomerId = ?", customer_id)
            row = c.fetchone()
            return row[0] if row and row[0] else None
    except Exception as e:
        logger.error(f"Musteri referans getirme hatasi: {e}")
        return None


def get_customer_language(customer_id):
    """Musterinin dil tercihini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("SELECT LanguageCode FROM TgCustomers WHERE CustomerId = ?", customer_id)
            row = c.fetchone()
            if row and row[0]:
                lang = row[0][:2].lower()
                if lang in ('tr', 'en', 'ru', 'de'):
                    return lang
    except Exception as e:
        logger.error(f"DB error in get_customer_language: {e}")
    return 'tr'


# ===================================================
# TRANSACTION ISLEMLERI
# ===================================================

def create_transaction(customer_id, currency, amount, referral_code,
                       exchange_rate=None, try_amount=None):
    """Yeni islem olustur, transaction_id dondur"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                INSERT INTO TgTransactions
                    (CustomerId, Currency, Amount, ReferralCode, ExchangeRate, TryAmount)
                OUTPUT INSERTED.TransactionId
                VALUES (?, ?, ?, ?, ?, ?)
            """, customer_id, currency, amount, referral_code, exchange_rate, try_amount)
            tid = c.fetchone()[0]
            conn.commit()
            return tid
    except Exception as e:
        logger.error(f"Islem olusturma hatasi: {e}")
        return None


def update_transaction(transaction_id, **kwargs):
    """Islemi guncelle -- kwargs ile istenen alanlari guncelle"""
    if not kwargs:
        return False
    try:
        sets = []
        vals = []
        for k, v in kwargs.items():
            col = _TX_COL_MAP.get(k, k)
            sets.append(f"{col} = ?")
            vals.append(v)
        vals.append(transaction_id)

        with get_conn() as conn:
            c = conn.cursor()
            c.execute(
                f"UPDATE TgTransactions SET {', '.join(sets)} WHERE TransactionId = ?",
                *vals
            )
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        logger.error(f"Islem guncelleme hatasi: {e}")
        return False


def assign_transaction_to_operator(transaction_id, operator_id):
    """İşlemi bir operatöre atomik olarak atar.

    İki operatörün aynı anda "Bu Müşteriyi Al" butonuna basması durumunda sessiz
    çifte atamayı önlemek için, sadece işlem HENÜZ atanmamışsa (veya zaten aynı
    operatöre atanmışsa) güncelleme yapılır — WHERE koşulu bunu DB seviyesinde
    atomik olarak garanti eder.
    """
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                UPDATE TgTransactions
                SET AssignedOperatorId = ?, Status = 'in_progress'
                WHERE TransactionId = ?
                  AND (AssignedOperatorId IS NULL OR AssignedOperatorId = ?)
            """, operator_id, transaction_id, operator_id)
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        logger.error(f"Islem atama hatasi: {e}")
        return False


def get_transaction(transaction_id):
    """Tek islem detayini getir (dict)"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT t.TransactionId AS transaction_id,
                       t.CustomerId AS customer_id,
                       t.Currency AS currency,
                       t.Amount AS amount,
                       t.ExchangeRate AS exchange_rate,
                       t.TryAmount AS try_amount,
                       t.Status AS status,
                       t.ReferralCode AS referral_code,
                       t.AssignedOperatorId AS assigned_operator_id,
                       t.CompletionCode AS completion_code,
                       t.Txid AS txid,
                       t.CryptoVerified AS crypto_verified,
                       t.CryptoVerifiedAt AS crypto_verified_at,
                       t.IsBuy AS isBuy,
                       t.CreatedAt AS created_at,
                       t.CompletedAt AS completed_at,
                       t.StateData AS state_data,
                       t.IdempotencyKey AS idempotency_key,
                       c.FirstName AS customer_name,
                       c.Username AS customer_username
                FROM TgTransactions t
                JOIN TgCustomers c ON t.CustomerId = c.CustomerId
                WHERE t.TransactionId = ?
            """, transaction_id)
            row = c.fetchone()
            if row:
                cols = [d[0] for d in c.description]
                return dict(zip(cols, row))
    except Exception as e:
        logger.error(f"Islem getirme hatasi: {e}")
    return None


def get_pending_usdt(customer_id):
    """Musterinin bekleyen USDT islemini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT TOP 1
                    t.TransactionId AS transaction_id,
                    t.Currency AS currency,
                    t.CryptoVerified AS crypto_verified,
                    t.CompletionCode AS completion_code,
                    t.Amount AS amount,
                    d.CryptoAddress AS crypto_address,
                    d.CryptoNetwork AS crypto_network,
                    d.DealerId AS dealer_id,
                    d.ApiKey AS api_key,
                    d.ApiSecret AS api_secret
                FROM TgTransactions t
                LEFT JOIN TgDealers d ON d.DealerCode = t.ReferralCode
                WHERE t.CustomerId = ?
                  AND t.Status IN ('pending', 'pending_crypto')
                  AND t.Currency = 'USDT'
                  AND t.CryptoVerified = 0
                ORDER BY t.CreatedAt DESC
            """, customer_id)
            row = c.fetchone()
            if row:
                cols = [d[0] for d in c.description]
                return dict(zip(cols, row))
    except Exception as e:
        logger.error(f"Bekleyen USDT sorgu hatasi: {e}")
    return None


def get_pending_ruble(customer_id):
    """Musterinin bekleyen Ruble islemini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT TOP 1
                    TransactionId AS transaction_id,
                    Status AS status
                FROM TgTransactions
                WHERE CustomerId = ?
                  AND Currency = 'RUBLE'
                  AND Status IN ('waiting_payment', 'bank_provided')
                ORDER BY CreatedAt DESC
            """, customer_id)
            row = c.fetchone()
            if row:
                return {'transaction_id': row[0], 'status': row[1]}
    except Exception as e:
        logger.error(f"Bekleyen Ruble sorgu hatasi: {e}")
    return None


def get_active_transaction(customer_id):
    """Musterinin son aktif islemini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT TOP 1
                    TransactionId AS transaction_id,
                    Currency AS currency,
                    CryptoVerified AS crypto_verified,
                    Status AS status,
                    AssignedOperatorId AS assigned_operator_id
                FROM TgTransactions
                WHERE CustomerId = ?
                  AND Status IN ('pending', 'in_progress', 'pending_crypto',
                                 'waiting_payment', 'payment_sent', 'operator_chat')
                ORDER BY CreatedAt DESC
            """, customer_id)
            row = c.fetchone()
            if row:
                cols = [d[0] for d in c.description]
                return dict(zip(cols, row))
    except Exception as e:
        logger.error(f"DB error in get_active_transaction: {e}")
    return None


def get_pending_transactions():
    """Bekleyen tum islemleri getir (operator listesi icin)"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT t.TransactionId AS transaction_id,
                       c.FirstName AS first_name,
                       c.CustomerId AS customer_id,
                       t.Currency AS currency,
                       t.Amount AS amount,
                       t.CreatedAt AS created_at
                FROM TgTransactions t
                JOIN TgCustomers c ON t.CustomerId = c.CustomerId
                WHERE t.Status = 'pending'
                ORDER BY t.CreatedAt DESC
            """)
            rows = c.fetchall()
            cols = [d[0] for d in c.description]
            return [dict(zip(cols, r)) for r in rows]
    except Exception as e:
        logger.error(f"Bekleyen islemler sorgu hatasi: {e}")
    return []


def complete_transaction(transaction_id, completion_code=None):
    """Islemi tamamla"""
    updates = {
        'status': 'completed',
        'completed_at': datetime.now()
    }
    if completion_code:
        updates['completion_code'] = str(completion_code)
    return update_transaction(transaction_id, **updates)


def complete_transaction_atomic(transaction_id, completion_code=None):
    """
    Islemi SADECE henuz 'completed' olmayan bir durumdaysa tamamlar.
    Cok-processli bot mimarisinde (main_bot/operator_bot/ruble_bot ayri
    python.exe surecleri) ayni transaction_id icin ayni anda iki tamamlama
    denemesi gelebilir; asyncio.Lock sadece tek surec icini korur, bu yuzden
    gercek koruma SQL Server'in satir seviyesi atomik UPDATE'ine dayanir.

    Donus: True  -> bu cagri islemi tamamladi (bakiye dusme/API cagrilarina devam et)
           False -> zaten completed idi (dur, kullaniciya bildir, tekrar isleme)
    """
    try:
        with get_conn() as conn:
            c = conn.cursor()
            if completion_code:
                c.execute("""
                    UPDATE TgTransactions
                    SET Status = 'completed', CompletedAt = GETDATE(), CompletionCode = ?
                    WHERE TransactionId = ? AND Status <> 'completed'
                """, str(completion_code), transaction_id)
            else:
                c.execute("""
                    UPDATE TgTransactions
                    SET Status = 'completed', CompletedAt = GETDATE()
                    WHERE TransactionId = ? AND Status <> 'completed'
                """, transaction_id)
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        logger.error(f"DB error in complete_transaction_atomic: {e}")
        return False


def cancel_transaction(transaction_id, customer_id):
    """Islemi iptal et"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                UPDATE TgTransactions
                SET Status = 'cancelled', CompletedAt = GETDATE()
                WHERE TransactionId = ?
                  AND CustomerId = ?
                  AND Status IN ('pending', 'pending_crypto', 'in_progress')
            """, transaction_id, customer_id)
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        logger.error(f"DB error in cancel_transaction: {e}")
        return False


# ===================================================
# MESAJ ISLEMLERI
# ===================================================

def save_message(transaction_id, sender_id, sender_type, message_text,
                 file_url=None, file_type=None):
    """Mesaji DB'ye kaydet"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                INSERT INTO TgMessages
                    (TransactionId, SenderId, SenderType, MessageText, FileUrl, FileType)
                VALUES (?, ?, ?, ?, ?, ?)
            """, transaction_id, sender_id, sender_type, message_text, file_url, file_type)
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"Mesaj kaydetme hatasi: {e}")
        return False


# ===================================================
# OPERATOR ISLEMLERI
# ===================================================

def get_operator(operator_id):
    """Operator bilgisini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT OperatorId, FirstName, IsActive, IsAdmin
                FROM TgOperators WHERE OperatorId = ?
            """, operator_id)
            row = c.fetchone()
            if row:
                return {
                    'operator_id': row[0], 'first_name': row[1],
                    'is_active': row[2], 'is_admin': row[3]
                }
    except Exception as e:
        logger.error(f"DB error in get_operator: {e}")
    return None


def register_operator(user):
    """Yeni operator kaydi (pasif olarak)"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                IF NOT EXISTS (SELECT 1 FROM TgOperators WHERE OperatorId = ?)
                    INSERT INTO TgOperators (OperatorId, FirstName, Username, IsActive, IsAdmin)
                    VALUES (?, ?, ?, 0, 0)
            """, user.id, user.id, user.first_name, user.username)
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"Operator kayit hatasi: {e}")
        return False


def validate_and_consume_invite_token(token, telegram_id):
    """Operatör davet tokenini dogrula ve atomik olarak tuket.

    WHERE kosulu (UsedAt IS NULL AND ExpiresAt > GETUTCDATE()) sayesinde ayni
    tokenin iki eszamanli istekle iki kez kullanilmasi engellenir; sadece
    gecerli ve daha once kullanilmamis bir token icin rowcount > 0 doner.
    """
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                UPDATE TgOperatorInviteTokens
                SET UsedAt = GETUTCDATE(), UsedByTelegramId = ?
                WHERE Token = ? AND UsedAt IS NULL AND ExpiresAt > GETUTCDATE()
            """, telegram_id, token)
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        logger.error(f"Invite token dogrulama hatasi: {e}")
        return False


def get_active_operators():
    """Aktif operatorlerin listesini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT OperatorId, FirstName, Username
                FROM TgOperators WHERE IsActive = 1
            """)
            return [{'id': r[0], 'name': r[1], 'username': r[2]} for r in c.fetchall()]
    except Exception as e:
        logger.error(f"DB error in get_active_operators: {e}")
        return []


def get_operator_stats(operator_id):
    """Operator istatistiklerini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT COUNT(*)
                FROM TgTransactions
                WHERE AssignedOperatorId = ?
                  AND CAST(CompletedAt AS DATE) = CAST(GETDATE() AS DATE)
            """, operator_id)
            today = c.fetchone()[0] or 0

            c.execute("""
                SELECT COUNT(*)
                FROM TgTransactions
                WHERE AssignedOperatorId = ?
                  AND Status = 'completed'
            """, operator_id)
            total = c.fetchone()[0] or 0

            return {'today': today, 'total': total}
    except Exception as e:
        logger.error(f"DB error in get_operator_stats: {e}")
        return {'today': 0, 'total': 0}


def set_operator_access(operator_id, is_active=None, is_admin=None):
    """Operator yetkisini/aktifligini guncelle"""
    if is_active is None and is_admin is None:
        return False
    if int(operator_id) == int(Config.ADMIN_ID):
        return False
    try:
        sets = []
        vals = []
        if is_active is not None:
            sets.append("IsActive = ?")
            vals.append(1 if is_active else 0)
        if is_admin is not None:
            sets.append("IsAdmin = ?")
            vals.append(1 if is_admin else 0)
        vals.append(operator_id)

        with get_conn() as conn:
            c = conn.cursor()
            c.execute(
                f"UPDATE TgOperators SET {', '.join(sets)} WHERE OperatorId = ?",
                *vals
            )
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        logger.error(f"Operator erisim guncelleme hatasi: {e}")
        return False


def get_all_operators(include_inactive=True, exclude_ids=None):
    """Tum operatorleri getir"""
    try:
        excluded = []
        if exclude_ids:
            for raw_id in exclude_ids:
                try:
                    excluded.append(int(raw_id))
                except (TypeError, ValueError):
                    continue

        with get_conn() as conn:
            c = conn.cursor()
            if include_inactive:
                c.execute("""
                    SELECT OperatorId AS operator_id, FirstName AS first_name,
                           Username AS username, IsActive AS is_active,
                           IsAdmin AS is_admin, CreatedAt AS created_at
                    FROM TgOperators
                    ORDER BY CreatedAt DESC
                """)
            else:
                c.execute("""
                    SELECT OperatorId AS operator_id, FirstName AS first_name,
                           Username AS username, IsActive AS is_active,
                           IsAdmin AS is_admin, CreatedAt AS created_at
                    FROM TgOperators
                    WHERE IsActive = 1
                    ORDER BY CreatedAt DESC
                """)
            rows = c.fetchall()
            cols = [d[0] for d in c.description]
            data = [dict(zip(cols, r)) for r in rows]
            if not excluded:
                return data
            return [
                row for row in data
                if int(row.get('operator_id') or 0) not in excluded
            ]
    except Exception as e:
        logger.error(f"Operator listeleme hatasi: {e}")
        return []


def get_owner_conversations(limit=20, only_active=False):
    """
    Owner/Admin paneli icin operatore atanmis gorusmeleri getir.
    """
    try:
        top_n = max(1, min(int(limit), 100))
        where = (
            "WHERE t.AssignedOperatorId IS NOT NULL "
            "AND t.AssignedOperatorId <> ?"
        )
        if only_active:
            where += " AND t.Status IN ('pending', 'in_progress', 'operator_chat', 'waiting_payment', 'payment_sent')"

        sql = f"""
            SELECT TOP {top_n}
                t.TransactionId AS transaction_id,
                t.Status AS status,
                t.Currency AS currency,
                t.Amount AS amount,
                t.CreatedAt AS created_at,
                t.CompletedAt AS completed_at,
                c.CustomerId AS customer_id,
                c.FirstName AS customer_name,
                o.OperatorId AS operator_id,
                o.FirstName AS operator_name,
                (
                    SELECT MAX(m.CreatedAt)
                    FROM TgMessages m
                    WHERE m.TransactionId = t.TransactionId
                ) AS last_message_at,
                (
                    SELECT COUNT(*)
                    FROM TgMessages m
                    WHERE m.TransactionId = t.TransactionId
                ) AS message_count
            FROM TgTransactions t
            LEFT JOIN TgCustomers c ON c.CustomerId = t.CustomerId
            LEFT JOIN TgOperators o ON o.OperatorId = t.AssignedOperatorId
            {where}
            ORDER BY
                COALESCE((
                    SELECT MAX(m.CreatedAt)
                    FROM TgMessages m
                    WHERE m.TransactionId = t.TransactionId
                ), t.CreatedAt) DESC
        """
        with get_conn() as conn:
            c = conn.cursor()
            c.execute(sql, int(Config.ADMIN_ID))
            rows = c.fetchall()
            cols = [d[0] for d in c.description]
            return [dict(zip(cols, r)) for r in rows]
    except Exception as e:
        logger.error(f"Owner gorusme listesi hatasi: {e}")
        return []


def get_transaction_messages(transaction_id, limit=120):
    """Bir islemin mesaj gecmisini getir (eski -> yeni)."""
    try:
        top_n = max(1, min(int(limit), 500))
        sql = f"""
            SELECT TOP {top_n}
                m.MessageId AS message_id,
                m.TransactionId AS transaction_id,
                m.SenderId AS sender_id,
                m.SenderType AS sender_type,
                m.MessageText AS message_text,
                m.FileUrl AS file_url,
                m.FileType AS file_type,
                m.CreatedAt AS created_at,
                c.FirstName AS customer_name,
                o.FirstName AS operator_name
            FROM TgMessages m
            LEFT JOIN TgCustomers c
                ON c.CustomerId = m.SenderId AND m.SenderType = 'customer'
            LEFT JOIN TgOperators o
                ON o.OperatorId = m.SenderId AND m.SenderType IN ('operator', 'bank_provider')
            WHERE m.TransactionId = ?
            ORDER BY m.CreatedAt ASC
        """
        with get_conn() as conn:
            c = conn.cursor()
            c.execute(sql, transaction_id)
            rows = c.fetchall()
            cols = [d[0] for d in c.description]
            return [dict(zip(cols, r)) for r in rows]
    except Exception as e:
        logger.error(f"Islem mesajlari getirme hatasi: {e}")
        return []


# ===================================================
# DEALER ISLEMLERI
# ===================================================

def get_dealer(dealer_code):
    """Bayi bilgisini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT DealerId AS dealer_id, DealerCode AS dealer_code,
                       DealerName AS dealer_name, IsActive AS is_active,
                       Balance AS balance, VaultId AS vaultId,
                       CryptoAddress AS crypto_address, CryptoNetwork AS crypto_network,
                       ExchangeName AS exchange_name, ApiKey AS api_key,
                       ApiSecret AS api_secret
                FROM TgDealers WHERE DealerCode = ?
            """, dealer_code)
            row = c.fetchone()
            if row:
                cols = [d[0] for d in c.description]
                return dict(zip(cols, row))
    except Exception as e:
        logger.error(f"DB error in get_dealer: {e}")
    return None


def find_dealer_by_city(city_text):
    """Sehir adina gore aktif bayi bul (basit metin eslesmesi, MERKEZ haric)"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT TOP 1 DealerCode AS dealer_code, DealerName AS dealer_name,
                       City AS city, Address AS address
                FROM TgDealers
                WHERE IsActive = 1 AND DealerCode <> 'MERKEZ'
                      AND City IS NOT NULL AND City LIKE '%' + ? + '%'
            """, city_text.strip())
            row = c.fetchone()
            if row:
                cols = [d[0] for d in c.description]
                return dict(zip(cols, row))
    except Exception as e:
        logger.error(f"DB error in find_dealer_by_city: {e}")
    return None


def reduce_dealer_balance(dealer_code, amount_try, transaction_id=None):
    """
    Bayi bakiyesinden dus. Negatife dusme engellenmez (islem zaten tamamlanmis
    sayildigi icin dusumu engellemek daha buyuk bir tutarsizlik yaratir -- negatif
    bakiye 'bayi acik' durumunu mesru sekilde temsil edebilir), ama izlenebilirlik
    icin loglanir.
    """
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                UPDATE TgDealers
                SET Balance = Balance - ?
                OUTPUT INSERTED.Balance
                WHERE DealerCode = ?
            """, amount_try, dealer_code)
            row = c.fetchone()
            conn.commit()
            if row and row[0] < 0:
                logger.warning(
                    f"Dealer {dealer_code} bakiyesi negatife dustu: {row[0]} "
                    f"(TransactionId={transaction_id}, dusulen={amount_try})"
                )
            return True
    except Exception as e:
        logger.error(f"DB error in reduce_dealer_balance: {e}")
        return False


# ===================================================
# KUR ISLEMLERI
# ===================================================

def get_exchange_rates_from_db():
    """Veritabanindan kurlari getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT Currency, BuyRate FROM TgExchangeRates
                WHERE Currency IN ('USDT', 'RUB')
            """)
            rates = {}
            for currency, rate in c.fetchall():
                rates[currency] = float(rate)
            return rates if rates else None
    except Exception as e:
        logger.error(f"DB error in get_exchange_rates_from_db: {e}")
        return None


# ===================================================
# CRYPTO DEPOSIT ISLEMLERI
# ===================================================

def check_txid_used(txid):
    """TXID daha once kullanilmis mi?"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("SELECT DepositId FROM TgCryptoDeposits WHERE Txid = ?", txid)
            return c.fetchone() is not None
    except Exception as e:
        logger.error(f"DB error in check_txid_used: {e}")
        return False


def save_crypto_deposit(transaction_id, dealer_id, txid, amount, network,
                        to_address, confirmations, status='pending'):
    """Kripto deposit kaydi olustur"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                INSERT INTO TgCryptoDeposits
                    (TransactionId, DealerId, Txid, Amount, Network,
                     ToAddress, DepositTime, Confirmations, Status)
                VALUES (?, ?, ?, ?, ?, ?, GETDATE(), ?, ?)
            """, transaction_id, dealer_id, txid, amount, network,
                to_address, confirmations, status)
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"Crypto deposit kayit hatasi: {e}")
        return False


def update_crypto_deposit(txid=None, transaction_id=None, **kwargs):
    """Crypto deposit guncelle"""
    if not kwargs:
        return False
    try:
        sets = []
        vals = []
        for k, v in kwargs.items():
            col = _CD_COL_MAP.get(k, k)
            sets.append(f"{col} = ?")
            vals.append(v)

        if txid:
            where = "Txid = ?"
            vals.append(txid)
        else:
            where = "TransactionId = ?"
            vals.append(transaction_id)

        with get_conn() as conn:
            c = conn.cursor()
            c.execute(
                f"UPDATE TgCryptoDeposits SET {', '.join(sets)} WHERE {where}",
                *vals
            )
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"DB error in update_crypto_deposit: {e}")
        return False


def get_pending_crypto_deposit(transaction_id, customer_id=None):
    """Bekleyen crypto deposit bilgisini getir.

    customer_id verilirse, işlemin gerçekten o müşteriye ait olduğu da doğrulanır —
    aksi halde callback_data'daki transaction_id tahmin/deneme ile başka bir
    müşterinin işlem durumunu (ve tamamlama kodunu) görüntülemek mümkün olurdu.
    """
    try:
        with get_conn() as conn:
            c = conn.cursor()
            if customer_id is not None:
                c.execute("""
                    SELECT cd.Txid AS txid, cd.Amount AS amount,
                           cd.Network AS network, cd.ToAddress AS to_address,
                           cd.Confirmations AS confirmations,
                           t.CompletionCode AS completion_code,
                           d.ApiKey AS api_key, d.ApiSecret AS api_secret
                    FROM TgCryptoDeposits cd
                    JOIN TgTransactions t ON cd.TransactionId = t.TransactionId
                    JOIN TgDealers d ON cd.DealerId = d.DealerId
                    WHERE cd.TransactionId = ? AND cd.Status = 'pending' AND t.CustomerId = ?
                """, transaction_id, customer_id)
            else:
                c.execute("""
                    SELECT cd.Txid AS txid, cd.Amount AS amount,
                           cd.Network AS network, cd.ToAddress AS to_address,
                           cd.Confirmations AS confirmations,
                           t.CompletionCode AS completion_code,
                           d.ApiKey AS api_key, d.ApiSecret AS api_secret
                    FROM TgCryptoDeposits cd
                    JOIN TgTransactions t ON cd.TransactionId = t.TransactionId
                    JOIN TgDealers d ON cd.DealerId = d.DealerId
                    WHERE cd.TransactionId = ? AND cd.Status = 'pending'
                """, transaction_id)
            row = c.fetchone()
            if row:
                cols = [d[0] for d in c.description]
                return dict(zip(cols, row))
    except Exception as e:
        logger.error(f"DB error in get_pending_crypto_deposit: {e}")
    return None


# ===================================================
# WEB PANEL BILDIRIM
# ===================================================

def notify_web_panel(transaction_id):
    """Web panele anlik bildirim gonder (.NET API SSE)"""
    if not Config.NOTIFY_SECRET:
        logger.warning("NOTIFY_SECRET tanımlı değil, web panel bildirimi atlanıyor")
        return
    try:
        import requests as req
        req.post(
            f'{Config.WEB_PANEL_URL}/api/v1/tg/notify',
            json={'transaction_id': transaction_id},
            headers={'X-Notify-Secret': Config.NOTIFY_SECRET},
            timeout=0.5
        )
    except Exception:
        pass  # Non-critical notification, fail silently


# ===================================================
# STATE YONETIMI (DB-Backed)
# ===================================================

def get_user_state(customer_id):
    """Musterinin aktif islem durumunu DB'den getir"""
    try:
        import json
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT TOP 1 TransactionId, Status, StateData
                FROM TgTransactions
                WHERE CustomerId = ?
                  AND Status NOT IN ('completed', 'cancelled')
                ORDER BY CreatedAt DESC
            """, customer_id)
            row = c.fetchone()
            if row:
                state_data = {}
                if row[2]:
                    try:
                        state_data = json.loads(row[2])
                    except Exception:
                        pass  # Invalid JSON in StateData, use empty dict
                return {
                    'transaction_id': row[0],
                    'state': state_data.get('state', row[1]),
                    'data': state_data.get('data', {})
                }
    except Exception as e:
        logger.error(f"State getirme hatasi: {e}")
    return None


def set_user_state(customer_id, state, data=None):
    """Musterinin aktif islem durumunu DB'ye kaydet"""
    try:
        import json
        state_json = json.dumps({'state': state, 'data': data or {}}, ensure_ascii=False)
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                UPDATE TgTransactions
                SET StateData = ?
                WHERE CustomerId = ?
                  AND Status NOT IN ('completed', 'cancelled')
                  AND TransactionId = (
                      SELECT TOP 1 TransactionId FROM TgTransactions
                      WHERE CustomerId = ?
                        AND Status NOT IN ('completed', 'cancelled')
                      ORDER BY CreatedAt DESC
                  )
            """, state_json, customer_id, customer_id)
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        logger.error(f"State kaydetme hatasi: {e}")
        return False


def get_all_active_states():
    """
    Tum aktif oturumlari getir -- Bot baslatildiginda recovery icin.
    """
    result = {}
    try:
        import json
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT CustomerId, TransactionId, Status, StateData
                FROM TgTransactions
                WHERE Status NOT IN ('completed', 'cancelled')
                  AND StateData IS NOT NULL
            """)
            for row in c.fetchall():
                cid, tid, status, raw = row
                state_data = {}
                if raw:
                    try:
                        state_data = json.loads(raw)
                    except Exception:
                        pass  # Invalid JSON in StateData, use empty dict
                result[cid] = {
                    'transaction_id': tid,
                    'state': state_data.get('state', status),
                    'data': state_data.get('data', {})
                }
    except Exception as e:
        logger.error(f"Aktif state'ler getirme hatasi: {e}")
    return result


# ===================================================
# IDEMPOTENCY
# ===================================================

def set_idempotency_key(transaction_id, key):
    """Isleme idempotency key ata"""
    return update_transaction(transaction_id, idempotency_key=key)


def check_idempotency_key(key):
    """Bu key daha once kullanilmis mi?"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT TransactionId FROM TgTransactions
                WHERE IdempotencyKey = ?
            """, key)
            row = c.fetchone()
            if row:
                return row[0]
    except Exception as e:
        logger.error(f"DB error in check_idempotency_key: {e}")
    return None


# ===================================================
# BANK PROVIDER ISLEMLERI
# ===================================================

def get_bank_providers():
    """Aktif banka saglayicilarinin ID listesini dondur"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("SELECT ProviderId FROM TgBankProviders WHERE IsActive = 1")
            return [row[0] for row in c.fetchall()]
    except Exception as e:
        logger.error(f"Bank provider listeleme hatasi: {e}")
        return []


def is_bank_provider(user_id):
    """Kullanici aktif banka saglayicisi mi?"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("SELECT 1 FROM TgBankProviders WHERE ProviderId = ? AND IsActive = 1", user_id)
            return c.fetchone() is not None
    except Exception as e:
        logger.error(f"Bank provider kontrol hatasi: {e}")
        return False


def add_bank_provider(provider_id, added_by):
    """Yeni banka saglayicisi ekle"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                IF EXISTS (SELECT 1 FROM TgBankProviders WHERE ProviderId = ?)
                    UPDATE TgBankProviders SET IsActive = 1 WHERE ProviderId = ?
                ELSE
                    INSERT INTO TgBankProviders (ProviderId, AddedBy) VALUES (?, ?)
            """, provider_id, provider_id, provider_id, added_by)
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"Bank provider ekleme hatasi: {e}")
        return False


def remove_bank_provider(provider_id):
    """Banka saglayicisini pasif yap"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("UPDATE TgBankProviders SET IsActive = 0 WHERE ProviderId = ?", provider_id)
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        logger.error(f"Bank provider kaldirma hatasi: {e}")
        return False


def seed_bank_providers(provider_ids):
    """Ilk calistirmada .env'deki provider'lari tabloya ekle (yoksa)"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            for pid in provider_ids:
                c.execute("""
                    IF NOT EXISTS (SELECT 1 FROM TgBankProviders WHERE ProviderId = ?)
                        INSERT INTO TgBankProviders (ProviderId, AddedBy) VALUES (?, ?)
                """, pid, pid, pid)
            conn.commit()
    except Exception as e:
        logger.error(f"Bank provider seed hatasi: {e}")


# ===================================================
# BOT HEARTBEAT ISLEMLERI
# ===================================================

def update_heartbeat(bot_name, active_sessions=0):
    """Bot heartbeat guncelle"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                IF EXISTS (SELECT 1 FROM TgBotHeartbeats WHERE BotName = ?)
                    UPDATE TgBotHeartbeats SET LastHeartbeat = GETDATE(), ActiveSessions = ?
                    WHERE BotName = ?
                ELSE
                    INSERT INTO TgBotHeartbeats (BotName, LastHeartbeat, ActiveSessions)
                    VALUES (?, GETDATE(), ?)
            """, bot_name, active_sessions, bot_name, bot_name, active_sessions)
            conn.commit()
    except Exception as e:
        logger.error(f"Heartbeat guncelleme hatasi: {e}")


def get_bot_heartbeats():
    """Tum bot heartbeat bilgilerini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT BotName AS bot_name, LastHeartbeat AS last_heartbeat,
                       LastTransactionAt AS last_transaction_at,
                       ActiveSessions AS active_sessions
                FROM TgBotHeartbeats
            """)
            rows = c.fetchall()
            cols = [d[0] for d in c.description]
            return [dict(zip(cols, r)) for r in rows]
    except Exception as e:
        logger.error(f"Heartbeat getirme hatasi: {e}")
        return []


# ===================================================
# BASKENT API KUYRUK ISLEMLERI
# ===================================================

def enqueue_baskent_api(transaction_id, operation_type='exchange', max_attempts=None):
    """
    Basarisiz BaskentEnerji API cagrisini kuyruga ekle.
    max_attempts verilirse (orn. kur eksikse otomatik tekrar denemenin anlami
    olmadigi 'exchange_missing_rate' durumu icin 1), varsayilan DB degeri (5)
    yerine o kullanilir -- boylece bu kayitlar process_queue() tarafindan
    bosuna tekrar tekrar denenmez, sadece admin panelde manuel mudahale
    bekleyen bir kayit olarak gorunur.
    """
    try:
        with get_conn() as conn:
            c = conn.cursor()
            if max_attempts is not None:
                c.execute("""
                    IF NOT EXISTS (
                        SELECT 1 FROM TgApiQueue
                        WHERE TransactionId = ? AND Status = 'pending' AND OperationType = ?
                    )
                    INSERT INTO TgApiQueue (TransactionId, OperationType, MaxAttempts)
                    VALUES (?, ?, ?)
                """, transaction_id, operation_type, transaction_id, operation_type, max_attempts)
            else:
                c.execute("""
                    IF NOT EXISTS (
                        SELECT 1 FROM TgApiQueue
                        WHERE TransactionId = ? AND Status = 'pending' AND OperationType = ?
                    )
                    INSERT INTO TgApiQueue (TransactionId, OperationType) VALUES (?, ?)
                """, transaction_id, operation_type, transaction_id, operation_type)
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"BaskentApi kuyruk ekleme hatasi: {e}")
        return False


def get_pending_baskent_queue():
    """Bekleyen BaskentEnerji API cagrilarini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT QueueId AS queue_id, TransactionId AS transaction_id,
                       Attempts AS attempts, MaxAttempts AS max_attempts,
                       LastError AS last_error, OperationType AS operation_type
                FROM TgApiQueue
                WHERE Status = 'pending' AND Attempts < MaxAttempts
                ORDER BY CreatedAt ASC
            """)
            rows = c.fetchall()
            cols = [d[0] for d in c.description]
            return [dict(zip(cols, r)) for r in rows]
    except Exception as e:
        logger.error(f"BaskentApi kuyruk getirme hatasi: {e}")
        return []


def update_baskent_queue(queue_id, status, error=None):
    """Kuyruk kaydini guncelle"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                UPDATE TgApiQueue
                SET Status = ?, Attempts = Attempts + 1, LastAttempt = GETDATE(), LastError = ?
                WHERE QueueId = ?
            """, status, error, queue_id)
            conn.commit()
    except Exception as e:
        logger.error(f"BaskentApi kuyruk guncelleme hatasi: {e}")


def get_active_operator_assignments():
    try:
        with get_conn() as conn:
            cursor = conn.cursor()
            cursor.execute(
                "SELECT AssignedOperatorId, CustomerId, TransactionId "
                "FROM TgTransactions WHERE Status IN ('processing','in_progress') "
                "AND AssignedOperatorId IS NOT NULL"
            )
            return [{'operator_id': r[0], 'customer_id': r[1], 'transaction_id': r[2]} for r in cursor.fetchall()]
    except Exception as e:
        logger.error(f"Aktif atama sorgulama hatasi: {e}")
        return []


def get_operator_active_chat(operator_id):
    try:
        with get_conn() as conn:
            cursor = conn.cursor()
            cursor.execute(
                "SELECT CustomerId, TransactionId FROM TgTransactions "
                "WHERE AssignedOperatorId=? AND Status IN ('processing','in_progress') "
                "ORDER BY CreatedAt DESC",
                operator_id
            )
            row = cursor.fetchone()
            if row:
                return {'customer_id': row[0], 'transaction_id': row[1]}
            return None
    except Exception as e:
        logger.error(f"Operator aktif chat sorgulama hatasi: {e}")
        return None
