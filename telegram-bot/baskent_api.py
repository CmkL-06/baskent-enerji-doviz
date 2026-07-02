"""
BaşkentEnerji API Client
Döviz işlemlerini muhasebe sistemine kaydeder.
"""
import requests
import logging
from config import Config

logger = logging.getLogger(__name__)

_token = Config.BASKENT_API_TOKEN


def login():
    """API'ye giriş yap ve token al"""
    global _token
    try:
        resp = requests.post(
            f"{Config.BASKENT_API_URL}/User/login",
            json={
                "mail": Config.BASKENT_USERNAME,
                "password": Config.BASKENT_PASSWORD
            },
            timeout=10
        )
        if resp.status_code == 200:
            _token = resp.json().get('apiToken', '')
            logger.info("[BaşkentAPI] Giriş başarılı")
            return True
        logger.error(f"[BaşkentAPI] Giriş başarısız: {resp.status_code}")
        return False
    except Exception as e:
        logger.error(f"[BaşkentAPI] Giriş hatası: {e}")
        return False


def send_exchange(transaction_id, dealer_vault_id, currency, amount,
                  is_buy, rate, dealer_name=None):
    """
    Döviz alış/satış işlemini BaşkentEnerji API'ye gönder.
    Başarılıysa True döner.
    """
    global _token

    # Para birimi ID'si
    currency_map = {
        'USDT': Config.BASKENT_USDT_CURRENCY_ID,
        'RUBLE': Config.BASKENT_RUBLE_CURRENCY_ID,
    }
    currency_id = currency_map.get(currency.upper())
    if not currency_id:
        logger.error(f"[BaşkentAPI] Bilinmeyen para birimi: {currency}")
        return False

    vault_id = dealer_vault_id or Config.BASKENT_DEFAULT_VAULT_ID
    notes = f"QR üzerinden yapılan işlem : {dealer_name}" if dealer_name else f"İşlem ID: {transaction_id}"

    payload = [{
        "vaultId": vault_id,
        "sourceCurrencyId": currency_id,
        "targetCurrencyId": Config.BASKENT_TRY_CURRENCY_ID,
        "sourceAmount": float(amount),
        "isBuyingFromCustomer": bool(is_buy),
        "customRate": float(rate),
        "notes": notes
    }]

    url = f"{Config.BASKENT_API_URL}/exchange/exchange"
    headers = {
        "Authorization": f"Bearer {_token}",
        "Content-Type": "application/json"
    }

    try:
        resp = requests.post(url, json=payload, headers=headers, timeout=10)

        # Token süresi dolmuşsa yenile ve tekrar dene
        if resp.status_code == 401:
            logger.info("[BaşkentAPI] Token süresi doldu, yenileniyor...")
            if login():
                headers["Authorization"] = f"Bearer {_token}"
                resp = requests.post(url, json=payload, headers=headers, timeout=10)

        if resp.status_code in (200, 201):
            logger.info(f"[BaşkentAPI] İşlem #{transaction_id} başarıyla gönderildi")
            return True

        logger.error(f"[BaşkentAPI] İşlem #{transaction_id} başarısız: "
                     f"{resp.status_code} — {resp.text[:200]}")
        return False

    except Exception as e:
        logger.error(f"[BaşkentAPI] İşlem #{transaction_id} gönderim hatası: {e}")
        return False


def send_exchange_for_transaction(transaction_id, enqueue_on_fail=True):
    """
    DB'den işlem bilgilerini alıp BaşkentEnerji'ye gönder.
    Başarısız olursa retry kuyruğuna ekle.
    """
    from database import get_conn, enqueue_baskent_api
    try:
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT d.vaultId, d.dealer_name, t.isBuy,
                       t.exchange_rate, t.amount, t.currency
                FROM Transactions t
                LEFT JOIN Dealers d ON d.dealer_code = t.referral_code
                WHERE t.transaction_id = ?
            """, transaction_id)
            row = c.fetchone()

            if row:
                vault_id, dealer_name, is_buy, rate, amount, currency = row
                success = send_exchange(
                    transaction_id=transaction_id,
                    dealer_vault_id=vault_id,
                    currency=currency,
                    amount=amount,
                    is_buy=(is_buy == 1) if is_buy is not None else True,
                    rate=rate or (39.0 if currency == 'USDT' else 0.40),
                    dealer_name=dealer_name
                )
                if not success and enqueue_on_fail:
                    enqueue_baskent_api(transaction_id)
                    logger.info(f"[BaşkentAPI] İşlem #{transaction_id} kuyruğa eklendi")
                return success
    except Exception as e:
        logger.error(f"[BaşkentAPI] İşlem #{transaction_id} bilgi alma hatası: {e}")
        if enqueue_on_fail:
            try:
                from database import enqueue_baskent_api
                enqueue_baskent_api(transaction_id)
            except Exception:
                pass
    return False


async def process_queue():
    """Background task: pending kuyruk kayıtlarını işle (60s aralıkla)"""
    import asyncio
    from database import get_pending_baskent_queue, update_baskent_queue

    while True:
        await asyncio.sleep(60)
        try:
            pending = get_pending_baskent_queue()
            if not pending:
                continue

            logger.info(f"[BaşkentAPI Queue] {len(pending)} bekleyen kayıt işleniyor")
            for item in pending:
                qid = item['queue_id']
                tid = item['transaction_id']
                try:
                    success = send_exchange_for_transaction(tid, enqueue_on_fail=False)
                    if success:
                        update_baskent_queue(qid, 'success')
                        logger.info(f"[BaşkentAPI Queue] #{tid} başarılı")
                    else:
                        attempts = item.get('attempts', 0) + 1
                        max_att = item.get('max_attempts', 5)
                        status = 'failed' if attempts >= max_att else 'pending'
                        update_baskent_queue(qid, status, f"Deneme {attempts}/{max_att} başarısız")
                        if status == 'failed':
                            logger.warning(f"[BaşkentAPI Queue] #{tid} kalıcı hata ({max_att} deneme)")
                except Exception as e:
                    update_baskent_queue(qid, 'pending', str(e)[:500])
                    logger.error(f"[BaşkentAPI Queue] #{tid} hata: {e}")
        except Exception as e:
            logger.error(f"[BaşkentAPI Queue] Kuyruk işleme hatası: {e}")
