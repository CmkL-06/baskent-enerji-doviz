"""
Veritabanı Modülü — Bağlantı Havuzu + CRUD İşlemleri
Tüm SQL işlemleri burada merkezileştirilir.
"""
import pyodbc
import logging
from datetime import datetime
from contextlib import contextmanager
from config import Config

logger = logging.getLogger(__name__)

# ═══════════════════════════════════════════════
# BAĞLANTI HAVUZU
# ═══════════════════════════════════════════════

_pool = []
_POOL_SIZE = 5


def _create_connection():
    """Yeni bir DB bağlantısı oluştur"""
    try:
        conn = pyodbc.connect(Config.connection_string(), timeout=10)
        conn.autocommit = False
        return conn
    except Exception as e:
        logger.error(f"DB bağlantı hatası: {e}")
        return None


@contextmanager
def get_conn():
    """
    Bağlantı havuzundan bağlantı al, işlem bitince geri koy.
    Kullanım:
        with get_conn() as conn:
            cursor = conn.cursor()
            cursor.execute(...)
            conn.commit()
    """
    conn = None
    # Havuzdan al
    while _pool:
        conn = _pool.pop()
        try:
            # Bağlantı hâlâ geçerli mi?
            conn.cursor().execute("SELECT 1")
            break
        except:
            try:
                conn.close()
            except:
                pass
            conn = None

    if conn is None:
        conn = _create_connection()

    if conn is None:
        raise ConnectionError("Veritabanına bağlanılamadı")

    try:
        yield conn
    except Exception:
        try:
            conn.rollback()
        except:
            pass
        raise
    finally:
        # Havuza geri koy
        if len(_pool) < _POOL_SIZE:
            try:
                conn.rollback()  # temiz state
                _pool.append(conn)
            except:
                try:
                    conn.close()
                except:
                    pass
        else:
            try:
                conn.close()
            except:
                pass


# ═══════════════════════════════════════════════
# TABLO OLUŞTURMA
# ═══════════════════════════════════════════════

def init_database():
    """Gerekli tabloları oluştur (IF NOT EXISTS)"""
    try:
        with get_conn() as conn:
            c = conn.cursor()

            c.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Customers' AND xtype='U')
                CREATE TABLE Customers (
                    customer_id BIGINT PRIMARY KEY,
                    first_name NVARCHAR(100),
                    last_name NVARCHAR(100),
                    username NVARCHAR(100),
                    language_code NVARCHAR(10) DEFAULT 'tr',
                    referral_code NVARCHAR(50),
                    created_at DATETIME DEFAULT GETDATE(),
                    last_activity DATETIME DEFAULT GETDATE()
                )
            """)

            c.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Operators' AND xtype='U')
                CREATE TABLE Operators (
                    operator_id BIGINT PRIMARY KEY,
                    first_name NVARCHAR(100),
                    username NVARCHAR(100),
                    is_active BIT DEFAULT 1,
                    is_admin BIT DEFAULT 0,
                    created_at DATETIME DEFAULT GETDATE()
                )
            """)

            c.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Transactions' AND xtype='U')
                CREATE TABLE Transactions (
                    transaction_id INT IDENTITY(1,1) PRIMARY KEY,
                    customer_id BIGINT,
                    currency NVARCHAR(10),
                    amount DECIMAL(18,2),
                    exchange_rate DECIMAL(18,6),
                    try_amount DECIMAL(18,2),
                    status NVARCHAR(30) DEFAULT 'pending',
                    referral_code NVARCHAR(50),
                    assigned_operator_id BIGINT,
                    completion_code NVARCHAR(20),
                    txid NVARCHAR(200),
                    crypto_verified BIT DEFAULT 0,
                    crypto_verified_at DATETIME,
                    isBuy BIT DEFAULT 1,
                    created_at DATETIME DEFAULT GETDATE(),
                    completed_at DATETIME,
                    FOREIGN KEY (customer_id) REFERENCES Customers(customer_id)
                )
            """)

            c.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Messages' AND xtype='U')
                CREATE TABLE Messages (
                    message_id INT IDENTITY(1,1) PRIMARY KEY,
                    transaction_id INT,
                    sender_id BIGINT,
                    sender_type NVARCHAR(20),
                    message_text NVARCHAR(MAX),
                    file_url NVARCHAR(500),
                    file_type NVARCHAR(20),
                    created_at DATETIME DEFAULT GETDATE(),
                    FOREIGN KEY (transaction_id) REFERENCES Transactions(transaction_id)
                )
            """)

            c.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ChatSessions' AND xtype='U')
                CREATE TABLE ChatSessions (
                    session_id INT IDENTITY(1,1) PRIMARY KEY,
                    customer_id BIGINT,
                    operator_id BIGINT,
                    transaction_id INT,
                    is_active BIT DEFAULT 1,
                    created_at DATETIME DEFAULT GETDATE(),
                    closed_at DATETIME,
                    FOREIGN KEY (transaction_id) REFERENCES Transactions(transaction_id)
                )
            """)

            c.execute("""
                IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='CryptoDeposits' AND xtype='U')
                CREATE TABLE CryptoDeposits (
                    deposit_id INT IDENTITY(1,1) PRIMARY KEY,
                    transaction_id INT,
                    dealer_id INT,
                    txid NVARCHAR(200),
                    amount DECIMAL(18,8),
                    network NVARCHAR(20),
                    to_address NVARCHAR(200),
                    deposit_time DATETIME DEFAULT GETDATE(),
                    confirmations INT DEFAULT 0,
                    status NVARCHAR(20) DEFAULT 'pending',
                    FOREIGN KEY (transaction_id) REFERENCES Transactions(transaction_id)
                )
            """)

            # Eksik kolonları ekle (mevcut DB uyumluluğu + yeni iyileştirmeler)
            _add_columns = [
                ('Messages', 'file_url', 'NVARCHAR(500)'),
                ('Messages', 'file_type', 'NVARCHAR(20)'),
                # İyileştirme A: DB-backed state
                ('Transactions', 'state_data', 'NVARCHAR(MAX)'),
                # İyileştirme B: Idempotency key
                ('Transactions', 'idempotency_key', 'NVARCHAR(50)'),
            ]
            for table, col, typ in _add_columns:
                c.execute(f"""
                    IF NOT EXISTS (
                        SELECT * FROM sys.columns
                        WHERE object_id = OBJECT_ID('{table}') AND name = '{col}'
                    )
                    ALTER TABLE {table} ADD {col} {typ}
                """)

            conn.commit()
            logger.info("Veritabanı tabloları hazır")
            return True

    except Exception as e:
        logger.error(f"Tablo oluşturma hatası: {e}")
        return False


# ═══════════════════════════════════════════════
# CUSTOMER İŞLEMLERİ
# ═══════════════════════════════════════════════

def upsert_customer(user):
    """Müşteriyi kaydet veya güncelle"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            lang = getattr(user, 'language_code', 'tr') or 'tr'
            c.execute("""
                IF EXISTS (SELECT 1 FROM Customers WHERE customer_id = ?)
                    UPDATE Customers SET
                        first_name = ?, last_name = ?, username = ?,
                        language_code = ?, last_activity = GETDATE()
                    WHERE customer_id = ?
                ELSE
                    INSERT INTO Customers
                        (customer_id, first_name, last_name, username, language_code)
                    VALUES (?, ?, ?, ?, ?)
            """,
                user.id, user.first_name, user.last_name, user.username, lang, user.id,
                user.id, user.first_name, user.last_name, user.username, lang
            )
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"Müşteri kaydetme hatası: {e}")
        return False


def get_customer_language(customer_id):
    """Müşterinin dil tercihini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("SELECT language_code FROM Customers WHERE customer_id = ?", customer_id)
            row = c.fetchone()
            if row and row[0]:
                lang = row[0][:2].lower()
                if lang in ('tr', 'en', 'ru', 'de'):
                    return lang
    except:
        pass
    return 'tr'


# ═══════════════════════════════════════════════
# TRANSACTION İŞLEMLERİ
# ═══════════════════════════════════════════════

def create_transaction(customer_id, currency, amount, referral_code,
                       exchange_rate=None, try_amount=None):
    """Yeni işlem oluştur, transaction_id döndür"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                INSERT INTO Transactions
                    (customer_id, currency, amount, referral_code, exchange_rate, try_amount)
                OUTPUT INSERTED.transaction_id
                VALUES (?, ?, ?, ?, ?, ?)
            """, customer_id, currency, amount, referral_code, exchange_rate, try_amount)
            tid = c.fetchone()[0]
            conn.commit()
            return tid
    except Exception as e:
        logger.error(f"İşlem oluşturma hatası: {e}")
        return None


def update_transaction(transaction_id, **kwargs):
    """İşlemi güncelle — kwargs ile istenen alanları güncelle"""
    if not kwargs:
        return False
    try:
        sets = []
        vals = []
        for k, v in kwargs.items():
            sets.append(f"{k} = ?")
            vals.append(v)
        vals.append(transaction_id)

        with get_conn() as conn:
            c = conn.cursor()
            c.execute(
                f"UPDATE Transactions SET {', '.join(sets)} WHERE transaction_id = ?",
                *vals
            )
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        logger.error(f"İşlem güncelleme hatası: {e}")
        return False


def get_transaction(transaction_id):
    """Tek işlem detayını getir (dict)"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT t.*, c.first_name AS customer_name, c.username AS customer_username
                FROM Transactions t
                JOIN Customers c ON t.customer_id = c.customer_id
                WHERE t.transaction_id = ?
            """, transaction_id)
            row = c.fetchone()
            if row:
                cols = [d[0] for d in c.description]
                return dict(zip(cols, row))
    except Exception as e:
        logger.error(f"İşlem getirme hatası: {e}")
    return None


def get_pending_usdt(customer_id):
    """Müşterinin bekleyen USDT işlemini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT TOP 1
                    t.transaction_id, t.currency, t.crypto_verified,
                    t.completion_code, t.amount,
                    d.crypto_address, d.crypto_network, d.dealer_id,
                    d.api_key, d.api_secret
                FROM Transactions t
                LEFT JOIN Dealers d ON d.dealer_code = t.referral_code
                WHERE t.customer_id = ?
                  AND t.status IN ('pending', 'pending_crypto')
                  AND t.currency = 'USDT'
                  AND t.crypto_verified = 0
                ORDER BY t.created_at DESC
            """, customer_id)
            row = c.fetchone()
            if row:
                cols = [d[0] for d in c.description]
                return dict(zip(cols, row))
    except Exception as e:
        logger.error(f"Bekleyen USDT sorgu hatası: {e}")
    return None


def get_pending_ruble(customer_id):
    """Müşterinin bekleyen Ruble işlemini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT TOP 1 transaction_id, status
                FROM Transactions
                WHERE customer_id = ?
                  AND currency = 'RUBLE'
                  AND status IN ('waiting_payment', 'bank_provided')
                ORDER BY created_at DESC
            """, customer_id)
            row = c.fetchone()
            if row:
                return {'transaction_id': row[0], 'status': row[1]}
    except Exception as e:
        logger.error(f"Bekleyen Ruble sorgu hatası: {e}")
    return None


def get_active_transaction(customer_id):
    """Müşterinin son aktif işlemini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT TOP 1 transaction_id, currency, crypto_verified,
                       status, assigned_operator_id
                FROM Transactions
                WHERE customer_id = ?
                  AND status IN ('pending', 'in_progress', 'pending_crypto',
                                 'waiting_payment', 'payment_sent', 'operator_chat')
                ORDER BY created_at DESC
            """, customer_id)
            row = c.fetchone()
            if row:
                cols = [d[0] for d in c.description]
                return dict(zip(cols, row))
    except:
        pass
    return None


def get_pending_transactions():
    """Bekleyen tüm işlemleri getir (operatör listesi için)"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT t.transaction_id, c.first_name, c.customer_id,
                       t.currency, t.amount, t.created_at
                FROM Transactions t
                JOIN Customers c ON t.customer_id = c.customer_id
                WHERE t.status = 'pending'
                ORDER BY t.created_at DESC
            """)
            rows = c.fetchall()
            cols = [d[0] for d in c.description]
            return [dict(zip(cols, r)) for r in rows]
    except Exception as e:
        logger.error(f"Bekleyen işlemler sorgu hatası: {e}")
    return []


def complete_transaction(transaction_id, completion_code=None):
    """İşlemi tamamla"""
    updates = {
        'status': 'completed',
        'completed_at': datetime.now()
    }
    if completion_code:
        updates['completion_code'] = str(completion_code)
    return update_transaction(transaction_id, **updates)


def cancel_transaction(transaction_id, customer_id):
    """İşlemi iptal et"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                UPDATE Transactions
                SET status = 'cancelled', completed_at = GETDATE()
                WHERE transaction_id = ?
                  AND customer_id = ?
                  AND status IN ('pending', 'pending_crypto', 'in_progress')
            """, transaction_id, customer_id)
            conn.commit()
            return c.rowcount > 0
    except:
        return False


# ═══════════════════════════════════════════════
# MESAJ İŞLEMLERİ
# ═══════════════════════════════════════════════

def save_message(transaction_id, sender_id, sender_type, message_text,
                 file_url=None, file_type=None):
    """Mesajı DB'ye kaydet"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                INSERT INTO Messages
                    (transaction_id, sender_id, sender_type, message_text, file_url, file_type)
                VALUES (?, ?, ?, ?, ?, ?)
            """, transaction_id, sender_id, sender_type, message_text, file_url, file_type)
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"Mesaj kaydetme hatası: {e}")
        return False


# ═══════════════════════════════════════════════
# OPERATÖR İŞLEMLERİ
# ═══════════════════════════════════════════════

def get_operator(operator_id):
    """Operatör bilgisini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT operator_id, first_name, is_active, is_admin
                FROM Operators WHERE operator_id = ?
            """, operator_id)
            row = c.fetchone()
            if row:
                return {
                    'operator_id': row[0], 'first_name': row[1],
                    'is_active': row[2], 'is_admin': row[3]
                }
    except:
        pass
    return None


def register_operator(user):
    """Yeni operatör kaydı (pasif olarak)"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                IF NOT EXISTS (SELECT 1 FROM Operators WHERE operator_id = ?)
                    INSERT INTO Operators (operator_id, first_name, username, is_active, is_admin)
                    VALUES (?, ?, ?, 0, 0)
            """, user.id, user.id, user.first_name, user.username)
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"Operatör kayıt hatası: {e}")
        return False


def get_active_operators():
    """Aktif operatörlerin listesini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT operator_id, first_name, username
                FROM Operators WHERE is_active = 1
            """)
            return [{'id': r[0], 'name': r[1], 'username': r[2]} for r in c.fetchall()]
    except:
        return []


def get_operator_stats(operator_id):
    """Operatör istatistiklerini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT COUNT(*)
                FROM Transactions
                WHERE assigned_operator_id = ?
                  AND CAST(completed_at AS DATE) = CAST(GETDATE() AS DATE)
            """, operator_id)
            today = c.fetchone()[0] or 0

            c.execute("""
                SELECT COUNT(*)
                FROM Transactions
                WHERE assigned_operator_id = ?
                  AND status = 'completed'
            """, operator_id)
            total = c.fetchone()[0] or 0

            return {'today': today, 'total': total}
    except:
        return {'today': 0, 'total': 0}


def set_operator_access(operator_id, is_active=None, is_admin=None):
    """Operatör yetkisini/aktifliğini güncelle"""
    if is_active is None and is_admin is None:
        return False
    if int(operator_id) == int(Config.ADMIN_ID):
        # Owner hesabı sistem tarafından yönetilir.
        return False
    try:
        sets = []
        vals = []
        if is_active is not None:
            sets.append("is_active = ?")
            vals.append(1 if is_active else 0)
        if is_admin is not None:
            sets.append("is_admin = ?")
            vals.append(1 if is_admin else 0)
        vals.append(operator_id)

        with get_conn() as conn:
            c = conn.cursor()
            c.execute(
                f"UPDATE Operators SET {', '.join(sets)} WHERE operator_id = ?",
                *vals
            )
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        logger.error(f"Operatör erişim güncelleme hatası: {e}")
        return False


def get_all_operators(include_inactive=True, exclude_ids=None):
    """Tüm operatörleri getir"""
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
                    SELECT operator_id, first_name, username, is_active, is_admin, created_at
                    FROM Operators
                    ORDER BY created_at DESC
                """)
            else:
                c.execute("""
                    SELECT operator_id, first_name, username, is_active, is_admin, created_at
                    FROM Operators
                    WHERE is_active = 1
                    ORDER BY created_at DESC
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
        logger.error(f"Operatör listeleme hatası: {e}")
        return []


def get_owner_conversations(limit=20, only_active=False):
    """
    Owner/Admin paneli için operatöre atanmış görüşmeleri getir.
    Not: limit güvenli integer'a normalize edilir.
    """
    try:
        top_n = max(1, min(int(limit), 100))
        where = (
            "WHERE t.assigned_operator_id IS NOT NULL "
            "AND t.assigned_operator_id <> ?"
        )
        if only_active:
            where += " AND t.status IN ('pending', 'in_progress', 'operator_chat', 'waiting_payment', 'payment_sent')"

        sql = f"""
            SELECT TOP {top_n}
                t.transaction_id,
                t.status,
                t.currency,
                t.amount,
                t.created_at,
                t.completed_at,
                c.customer_id,
                c.first_name AS customer_name,
                o.operator_id,
                o.first_name AS operator_name,
                (
                    SELECT MAX(m.created_at)
                    FROM Messages m
                    WHERE m.transaction_id = t.transaction_id
                ) AS last_message_at,
                (
                    SELECT COUNT(*)
                    FROM Messages m
                    WHERE m.transaction_id = t.transaction_id
                ) AS message_count
            FROM Transactions t
            LEFT JOIN Customers c ON c.customer_id = t.customer_id
            LEFT JOIN Operators o ON o.operator_id = t.assigned_operator_id
            {where}
            ORDER BY
                COALESCE((
                    SELECT MAX(m.created_at)
                    FROM Messages m
                    WHERE m.transaction_id = t.transaction_id
                ), t.created_at) DESC
        """
        with get_conn() as conn:
            c = conn.cursor()
            c.execute(sql, int(Config.ADMIN_ID))
            rows = c.fetchall()
            cols = [d[0] for d in c.description]
            return [dict(zip(cols, r)) for r in rows]
    except Exception as e:
        logger.error(f"Owner görüşme listesi hatası: {e}")
        return []


def get_transaction_messages(transaction_id, limit=120):
    """Bir işlemin mesaj geçmişini getir (eski -> yeni)."""
    try:
        top_n = max(1, min(int(limit), 500))
        sql = f"""
            SELECT TOP {top_n}
                m.message_id,
                m.transaction_id,
                m.sender_id,
                m.sender_type,
                m.message_text,
                m.file_url,
                m.file_type,
                m.created_at,
                c.first_name AS customer_name,
                o.first_name AS operator_name
            FROM Messages m
            LEFT JOIN Customers c
                ON c.customer_id = m.sender_id AND m.sender_type = 'customer'
            LEFT JOIN Operators o
                ON o.operator_id = m.sender_id AND m.sender_type IN ('operator', 'bank_provider')
            WHERE m.transaction_id = ?
            ORDER BY m.created_at ASC
        """
        with get_conn() as conn:
            c = conn.cursor()
            c.execute(sql, transaction_id)
            rows = c.fetchall()
            cols = [d[0] for d in c.description]
            return [dict(zip(cols, r)) for r in rows]
    except Exception as e:
        logger.error(f"İşlem mesajları getirme hatası: {e}")
        return []


# ═══════════════════════════════════════════════
# DEALER İŞLEMLERİ
# ═══════════════════════════════════════════════

def get_dealer(dealer_code):
    """Bayi bilgisini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT dealer_id, dealer_code, dealer_name, is_active,
                       balance, vaultId, crypto_address, crypto_network,
                       exchange_name, api_key, api_secret
                FROM Dealers WHERE dealer_code = ?
            """, dealer_code)
            row = c.fetchone()
            if row:
                cols = [d[0] for d in c.description]
                return dict(zip(cols, row))
    except:
        pass
    return None


def reduce_dealer_balance(dealer_code, amount_try):
    """Bayi bakiyesinden düş"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                UPDATE Dealers SET balance = balance - ? WHERE dealer_code = ?
            """, amount_try, dealer_code)
            conn.commit()
            return True
    except:
        return False


# ═══════════════════════════════════════════════
# KUR İŞLEMLERİ
# ═══════════════════════════════════════════════

def get_exchange_rates_from_db():
    """Veritabanından kurları getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT currency, buy_rate FROM ExchangeRates
                WHERE currency IN ('USDT', 'RUB')
            """)
            rates = {}
            for currency, rate in c.fetchall():
                rates[currency] = float(rate)
            return rates if rates else None
    except:
        return None


# ═══════════════════════════════════════════════
# CRYPTO DEPOSIT İŞLEMLERİ
# ═══════════════════════════════════════════════

def check_txid_used(txid):
    """TXID daha önce kullanılmış mı?"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("SELECT deposit_id FROM CryptoDeposits WHERE txid = ?", txid)
            return c.fetchone() is not None
    except:
        return False


def save_crypto_deposit(transaction_id, dealer_id, txid, amount, network,
                        to_address, confirmations, status='pending'):
    """Kripto deposit kaydı oluştur"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                INSERT INTO CryptoDeposits
                    (transaction_id, dealer_id, txid, amount, network,
                     to_address, deposit_time, confirmations, status)
                VALUES (?, ?, ?, ?, ?, ?, GETDATE(), ?, ?)
            """, transaction_id, dealer_id, txid, amount, network,
                to_address, confirmations, status)
            conn.commit()
            return True
    except Exception as e:
        logger.error(f"Crypto deposit kayıt hatası: {e}")
        return False


def update_crypto_deposit(txid=None, transaction_id=None, **kwargs):
    """Crypto deposit güncelle"""
    if not kwargs:
        return False
    try:
        sets = [f"{k} = ?" for k in kwargs]
        vals = list(kwargs.values())

        if txid:
            where = "txid = ?"
            vals.append(txid)
        else:
            where = "transaction_id = ?"
            vals.append(transaction_id)

        with get_conn() as conn:
            c = conn.cursor()
            c.execute(
                f"UPDATE CryptoDeposits SET {', '.join(sets)} WHERE {where}",
                *vals
            )
            conn.commit()
            return True
    except:
        return False


def get_pending_crypto_deposit(transaction_id):
    """Bekleyen crypto deposit bilgisini getir"""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT cd.txid, cd.amount, cd.network, cd.to_address,
                       cd.confirmations, t.completion_code,
                       d.api_key, d.api_secret
                FROM CryptoDeposits cd
                JOIN Transactions t ON cd.transaction_id = t.transaction_id
                JOIN Dealers d ON cd.dealer_id = d.dealer_id
                WHERE cd.transaction_id = ? AND cd.status = 'pending'
            """, transaction_id)
            row = c.fetchone()
            if row:
                cols = [d[0] for d in c.description]
                return dict(zip(cols, row))
    except:
        pass
    return None


# ═══════════════════════════════════════════════
# WEB PANEL BİLDİRİM
# ═══════════════════════════════════════════════

def notify_web_panel(transaction_id):
    """Web panele anlık bildirim gönder"""
    try:
        import requests as req
        req.post(
            f'{Config.WEB_PANEL_URL}/api/notify_message',
            json={'transaction_id': transaction_id},
            timeout=0.5
        )
    except:
        pass


# ═══════════════════════════════════════════════
# STATE YÖNETİMİ (İyileştirme A — DB-Backed)
# ═══════════════════════════════════════════════

def get_user_state(customer_id):
    """Müşterinin aktif işlem durumunu DB'den getir"""
    try:
        import json
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT TOP 1 transaction_id, status, state_data
                FROM Transactions
                WHERE customer_id = ?
                  AND status NOT IN ('completed', 'cancelled')
                ORDER BY created_at DESC
            """, customer_id)
            row = c.fetchone()
            if row:
                state_data = {}
                if row[2]:
                    try:
                        state_data = json.loads(row[2])
                    except:
                        pass
                return {
                    'transaction_id': row[0],
                    'state': state_data.get('state', row[1]),
                    'data': state_data.get('data', {})
                }
    except Exception as e:
        logger.error(f"State getirme hatası: {e}")
    return None


def set_user_state(customer_id, state, data=None):
    """Müşterinin aktif işlem durumunu DB'ye kaydet"""
    try:
        import json
        state_json = json.dumps({'state': state, 'data': data or {}}, ensure_ascii=False)
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                UPDATE Transactions
                SET state_data = ?
                WHERE customer_id = ?
                  AND status NOT IN ('completed', 'cancelled')
                  AND transaction_id = (
                      SELECT TOP 1 transaction_id FROM Transactions
                      WHERE customer_id = ?
                        AND status NOT IN ('completed', 'cancelled')
                      ORDER BY created_at DESC
                  )
            """, state_json, customer_id, customer_id)
            conn.commit()
            return c.rowcount > 0
    except Exception as e:
        logger.error(f"State kaydetme hatası: {e}")
        return False


def get_all_active_states():
    """
    Tüm aktif oturumları getir — Bot başlangıcında recovery için.
    Dönen dict: {customer_id: {'state': '...', 'data': {...}, 'transaction_id': N}}
    """
    result = {}
    try:
        import json
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT customer_id, transaction_id, status, state_data
                FROM Transactions
                WHERE status NOT IN ('completed', 'cancelled')
                  AND state_data IS NOT NULL
            """)
            for row in c.fetchall():
                cid, tid, status, raw = row
                state_data = {}
                if raw:
                    try:
                        state_data = json.loads(raw)
                    except:
                        pass
                result[cid] = {
                    'transaction_id': tid,
                    'state': state_data.get('state', status),
                    'data': state_data.get('data', {})
                }
    except Exception as e:
        logger.error(f"Aktif state'ler getirme hatası: {e}")
    return result


# ═══════════════════════════════════════════════
# IDEMPOTENCY (İyileştirme B)
# ═══════════════════════════════════════════════

def set_idempotency_key(transaction_id, key):
    """İşleme idempotency key ata"""
    return update_transaction(transaction_id, idempotency_key=key)


def check_idempotency_key(key):
    """Bu key daha önce kullanılmış mı? Kullanıldıysa transaction_id döner."""
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT transaction_id FROM Transactions
                WHERE idempotency_key = ?
            """, key)
            row = c.fetchone()
            if row:
                return row[0]
    except:
        pass
    return None
