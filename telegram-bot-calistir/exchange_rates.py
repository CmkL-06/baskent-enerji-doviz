"""
Döviz Kuru Modülü
Kurları DB → Binance → Varsayılan sırasıyla getirir.
"""
import requests
import logging
from config import Config
from database import get_exchange_rates_from_db

logger = logging.getLogger(__name__)

_cache = {}
_cache_ts = 0


def get_rates(use_cache=True):
    """
    Güncel döviz kurlarını getir.
    Sıra: DB → Binance API → Varsayılan
    Döndürür: {'USDT': float, 'RUB': float}
    """
    import time
    global _cache, _cache_ts

    # 30 saniye cache
    if use_cache and _cache and (time.time() - _cache_ts) < 30:
        return _cache

    rates = {}

    # 1. Veritabanından
    db_rates = get_exchange_rates_from_db()
    if db_rates and 'USDT' in db_rates and 'RUB' in db_rates:
        _cache = db_rates
        _cache_ts = time.time()
        return db_rates

    # 2. Binance API — USDT/TRY
    try:
        resp = requests.get(
            'https://api.binance.com/api/v3/ticker/price?symbol=USDTTRY',
            timeout=3
        )
        if resp.status_code == 200:
            rates['USDT'] = float(resp.json()['price'])
    except:
        pass

    if 'USDT' not in rates:
        rates['USDT'] = Config.DEFAULT_USDT_RATE

    # 3. exchangerate-api — RUB/TRY
    try:
        resp = requests.get(
            'https://api.exchangerate-api.com/v4/latest/USD',
            timeout=3
        )
        if resp.status_code == 200:
            data = resp.json()['rates']
            usd_try = data.get('TRY', 34.0)
            usd_rub = data.get('RUB', 90.0)
            rates['RUB'] = usd_try / usd_rub
    except:
        pass

    if 'RUB' not in rates:
        rates['RUB'] = Config.DEFAULT_RUB_RATE

    _cache = rates
    _cache_ts = time.time()
    return rates
