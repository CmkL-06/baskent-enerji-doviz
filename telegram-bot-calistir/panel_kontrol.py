"""
Operator / Admin / USDT / Ruble panel ve config kontrolu.
Calistirma: telegram-bot klasorunde  python panel_kontrol.py
"""
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from dotenv import load_dotenv
load_dotenv()

def main():
    print("=" * 60)
    print("  PANEL KONTROL - Operator / Admin / USDT / Ruble")
    print("=" * 60)

    checks = []

    # Token'lar
    main_t = os.getenv("MAIN_BOT_TOKEN", "").strip()
    op_t = os.getenv("OPERATOR_BOT_TOKEN", "").strip()
    ruble_t = os.getenv("RUBLE_BOT_TOKEN", "").strip()
    if main_t:
        checks.append(("MAIN_BOT_TOKEN", True, "Tanimli"))
    else:
        checks.append(("MAIN_BOT_TOKEN", False, "Eksik"))
    if op_t:
        checks.append(("OPERATOR_BOT_TOKEN", True, "Tanimli"))
    else:
        checks.append(("OPERATOR_BOT_TOKEN", False, "Eksik"))
    if ruble_t:
        checks.append(("RUBLE_BOT_TOKEN", True, "Tanimli"))
    else:
        checks.append(("RUBLE_BOT_TOKEN", False, "Eksik"))

    # Paneller icin gerekli
    admin_id = os.getenv("ADMIN_ID", "").strip()
    if admin_id:
        checks.append(("ADMIN_ID (Owner)", True, f"Dolu: {admin_id[:4]}..."))
    else:
        checks.append(("ADMIN_ID (Owner)", False, "Eksik"))

    # Ruble kanal
    ruble_ch = os.getenv("RUBLE_CHANNEL_ID", "").strip()
    if ruble_ch:
        checks.append(("RUBLE_CHANNEL_ID", True, "Tanimli - Ruble kanal bildirimi acik"))
    else:
        checks.append(("RUBLE_CHANNEL_ID", False, "Bos - Ruble kanal bildirimi kapali"))

    # Web panel
    web_url = os.getenv("WEB_PANEL_URL", "").strip() or "http://localhost:5000"
    checks.append(("WEB_PANEL_URL", True, web_url))

    # DB (operator/admin panel icin)
    db_user = os.getenv("DB_USERNAME", "").strip()
    db_pass = os.getenv("DB_PASSWORD", "").strip()
    if db_user and db_pass:
        checks.append(("DB (operator/admin)", True, "Kullanici/sifre var"))
    else:
        checks.append(("DB (operator/admin)", False, "DB_USERNAME veya DB_PASSWORD bos"))

    # USDT/Ruble limitler
    min_usdt = os.getenv("MIN_USDT", "10").strip()
    min_ruble = os.getenv("MIN_RUBLE", "5000").strip()
    checks.append(("MIN_USDT / MIN_RUBLE", True, f"{min_usdt} USDT / {min_ruble} RUB"))

    print()
    for name, ok, msg in checks:
        status = "OK" if ok else "EKSIK/UYARI"
        print(f"  [{status}] {name}: {msg}")
    print()
    print("Detayli rapor: PANEL_KONTROL_RAPORU.md")
    print("=" * 60)

if __name__ == "__main__":
    main()
