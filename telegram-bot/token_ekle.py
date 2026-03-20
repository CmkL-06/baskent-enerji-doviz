"""
BotFather'dan alınan token'ları .env dosyasına güvenli şekilde yazar.
Çalıştırma: telegram-bot klasöründe  python token_ekle.py
"""
import os
from pathlib import Path

ENV_FILE = Path(__file__).resolve().parent / ".env"
BOTFATHER_URL = "https://t.me/botfather"


def read_env():
    if not ENV_FILE.exists():
        return []
    with open(ENV_FILE, "r", encoding="utf-8") as f:
        return f.readlines()


def write_env(lines):
    with open(ENV_FILE, "w", encoding="utf-8") as f:
        f.writelines(lines)


def set_env_value(key: str, value: str, lines: list) -> list:
    out = []
    found = False
    for line in lines:
        if line.strip().startswith(f"{key}="):
            out.append(f"{key}={value}\n")
            found = True
        else:
            out.append(line)
    if not found:
        out.append(f"{key}={value}\n")
    return out


def main():
    print("=" * 60)
    print("  TELEGRAM BOT TOKENLARI .env DOSYASINA YAZ")
    print("=" * 60)
    print(f"\nBotFather: {BOTFATHER_URL}")
    print("  /newbot = yeni bot |  /mybots = mevcut bot tokeni\n")

    lines = read_env()
    tokens = [
        ("MAIN_BOT_TOKEN", "Ana bot tokeni"),
        ("OPERATOR_BOT_TOKEN", "Operator bot tokeni"),
        ("RUBLE_BOT_TOKEN", "Ruble bot tokeni"),
    ]

    for key, label in tokens:
        value = input(f"{label} [{key}]: ").strip()
        if value:
            lines = set_env_value(key, value, lines)
            print("  -> Kaydedildi.")
        else:
            print("  -> Bos birakildi.")

    write_env(lines)
    print("\n.env guncellendi. Botlari baslatmak icin: python run_all.py")


if __name__ == "__main__":
    main()
