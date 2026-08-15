"""
Merkezi Konfigürasyon Modülü
Tüm ayarlar .env dosyasından okunur.
"""
import os
from dotenv import load_dotenv

load_dotenv()


class Config:
    """Uygulama konfigürasyonu"""

    # ── Telegram Bot Token'ları ──
    # @MoneyExchangeTurkeyBot  → main_bot.py
    # @MTTOperatorBot          → operator_bot.py
    # @MoneyExchangeRubleBot   → ruble_bot.py
    MAIN_BOT_TOKEN = os.getenv('MAIN_BOT_TOKEN', '')
    OPERATOR_BOT_TOKEN = os.getenv('OPERATOR_BOT_TOKEN', '')
    RUBLE_BOT_TOKEN = os.getenv('RUBLE_BOT_TOKEN', '')

    # ── Telegram Grup/Kanal ──
    REQUIRED_GROUP_ID = int(os.getenv('REQUIRED_GROUP_ID', '-1003021754065'))
    RUBLE_CHANNEL_ID = os.getenv('RUBLE_CHANNEL_ID', '')
    OPERATOR_CHANNEL_ID = os.getenv('OPERATOR_CHANNEL_ID', '')
    SKIP_GROUP_CHECK = os.getenv('SKIP_GROUP_CHECK', 'False').lower() == 'true'

    # ── Veritabanı ──
    DB_SERVER = os.getenv('DB_SERVER', 'localhost')
    DB_NAME = os.getenv('DB_NAME', 'mtturkey_telegram')
    DB_USERNAME = os.getenv('DB_USERNAME', '')
    DB_PASSWORD = os.getenv('DB_PASSWORD', '')
    DB_DRIVER = os.getenv('DB_DRIVER', '{ODBC Driver 17 for SQL Server}')

    @classmethod
    def connection_string(cls):
        return (
            f"DRIVER={cls.DB_DRIVER};"
            f"SERVER={cls.DB_SERVER};"
            f"DATABASE={cls.DB_NAME};"
            f"UID={cls.DB_USERNAME};"
            f"PWD={cls.DB_PASSWORD}"
        )

    # ── BaşkentEnerji API ──
    BASKENT_API_URL = os.getenv('BASKENT_API_URL', 'https://api.baskentenerji.com/api/v1')
    BASKENT_USERNAME = os.getenv('BASKENT_USERNAME', '')
    BASKENT_PASSWORD = os.getenv('BASKENT_PASSWORD', '')
    BASKENT_API_TOKEN = os.getenv('BASKENT_API_TOKEN', '')
    BASKENT_DEFAULT_VAULT_ID = os.getenv('BASKENT_DEFAULT_VAULT_ID', '')
    BASKENT_USDT_CURRENCY_ID = os.getenv('BASKENT_USDT_CURRENCY_ID', '')
    BASKENT_RUBLE_CURRENCY_ID = os.getenv('BASKENT_RUBLE_CURRENCY_ID', '')
    BASKENT_TRY_CURRENCY_ID = 'cd817762-d3f6-4df2-83bc-8a8f9cb476a2'

    # ── Yetkililer ──
    ADMIN_ID = int(os.getenv('ADMIN_ID', '7462722250'))
    BANK_PROVIDERS = list(map(int, os.getenv('BANK_PROVIDERS', '7462722250,534876470').split(',')))

    # ── Limitler ──
    MIN_USDT = float(os.getenv('MIN_USDT', '10'))
    MIN_RUBLE = float(os.getenv('MIN_RUBLE', '5000'))
    MAX_USDT = float(os.getenv('MAX_USDT', '100000'))
    MAX_RUBLE = float(os.getenv('MAX_RUBLE', '50000000'))
    TXID_MIN_LENGTH = int(os.getenv('TXID_MIN_LENGTH', '10'))
    TXID_MAX_LENGTH = int(os.getenv('TXID_MAX_LENGTH', '128'))
    USDT_CONFIRMATIONS_REQUIRED = int(os.getenv('USDT_CONFIRMATIONS', '10'))
    TXID_VERIFY_TIME_LIMIT = int(os.getenv('TXID_VERIFY_TIME_LIMIT', '120'))  # dakika

    # ── Varsayılan Kurlar (fallback) ──
    DEFAULT_USDT_RATE = float(os.getenv('DEFAULT_USDT_RATE', '39.00'))
    DEFAULT_RUB_RATE = float(os.getenv('DEFAULT_RUB_RATE', '0.40'))

    # ── .NET API'ye SSE bildirimi (notify_web_panel) ──
    # NOTIFY_SECRET için kaynak kodda bilinen/tahmin edilebilir bir varsayılan
    # KULLANILMAZ — .env'de tanımlı değilse boş kalır ve notify_web_panel bu durumda
    # isteği hiç göndermez (bkz. database.py notify_web_panel).
    WEB_PANEL_URL = os.getenv('WEB_PANEL_URL', 'http://localhost:5000')
    NOTIFY_SECRET = os.getenv('NOTIFY_SECRET', '')

    # ── Dosya Yükleme ──
    UPLOAD_DIR = os.getenv('UPLOAD_DIR', 'uploads')
    ALLOWED_EXTENSIONS = {'.pdf', '.png'}
    MAX_FILE_SIZE = 10 * 1024 * 1024  # 10 MB

    @classmethod
    def validate(cls):
        """Zorunlu ayarların mevcut olduğunu kontrol et"""
        errors = []
        if not cls.MAIN_BOT_TOKEN:
            errors.append("MAIN_BOT_TOKEN eksik")
        if not cls.OPERATOR_BOT_TOKEN:
            errors.append("OPERATOR_BOT_TOKEN eksik")
        if not cls.RUBLE_BOT_TOKEN:
            errors.append("RUBLE_BOT_TOKEN eksik")
        if not cls.DB_USERNAME:
            errors.append("DB_USERNAME eksik")
        if not cls.DB_PASSWORD:
            errors.append("DB_PASSWORD eksik")
        return errors
