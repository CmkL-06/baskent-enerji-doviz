"""
BaşkentEnerji API Client
Döviz işlemlerini muhasebe sistemine kaydeder.
"""
import requests
import logging
import time
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


def record_dealer_entry(dealer_code, currency, amount, amount_try,
                        exchange_rate, is_buy, transaction_id=None, max_attempts=3):
    """
    Cari hesaba kayıt düşür (Exchange API'den AYRI, vault'a dokunmaz).

    send_exchange_for_transaction (vault güncellemesi) başarısız olduğunda TgApiQueue'ya
    düşüp arka planda tekrar denenirken, bu çağrı için böyle bir kuyruk yoktu — tek bir ağ
    hatası, vault düşülmüş ama bayinin cari hesabına hiç yansımamış bir işlem bırakabiliyordu.
    Kalıcı bir kuyruk mekanizması eklemek yerine (şema değişikliği gerektirir), en sık görülen
    geçici ağ hatalarını burada kısa aralıklarla birkaç kez deneyerek gideriyoruz.
    """
    global _token
    url = f"{Config.BASKENT_API_URL}/tg/dealer/record-entry"
    payload = {
        "dealerCode": dealer_code,
        "transactionId": transaction_id,
        "currency": currency,
        "amount": float(amount),
        "amountTry": float(amount_try),
        "exchangeRate": float(exchange_rate),
        "isBuy": bool(is_buy)
    }

    for attempt in range(1, max_attempts + 1):
        headers = {
            "Authorization": f"Bearer {_token}",
            "Content-Type": "application/json"
        }
        try:
            resp = requests.post(url, json=payload, headers=headers, timeout=10)
            if resp.status_code == 401:
                if login():
                    headers["Authorization"] = f"Bearer {_token}"
                    resp = requests.post(url, json=payload, headers=headers, timeout=10)
            if resp.status_code in (200, 201):
                logger.info(f"[CariHesap] {dealer_code} kayıt başarılı — {currency} {'Alış' if is_buy else 'Satış'}"
                            + (f" ({attempt}. deneme)" if attempt > 1 else ""))
                return True
            logger.error(f"[CariHesap] Deneme {attempt}/{max_attempts} başarısız: {resp.status_code} — {resp.text[:200]}")
        except Exception as e:
            logger.error(f"[CariHesap] Deneme {attempt}/{max_attempts} hatası: {e}")

        if attempt < max_attempts:
            time.sleep(2 * attempt)

    logger.error(f"[CariHesap] İşlem #{transaction_id}: {max_attempts} denemenin tamamı başarısız — "
                 f"vault güncellendi ama cari hesap kaydı düşmedi, manuel kontrol gerekir.")
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
                SELECT d.VaultId, d.DealerName, t.IsBuy,
                       t.ExchangeRate, t.Amount, t.Currency
                FROM TgTransactions t
                LEFT JOIN TgDealers d ON d.DealerCode = t.ReferralCode
                WHERE t.TransactionId = ?
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
