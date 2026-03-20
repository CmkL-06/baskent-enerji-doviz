"""
Eksik .env alanlarini interaktif doldurur. Mevcut degerler korunur; bos olanlar sorulur.
Calistirma: python env_eksikleri_doldur.py
"""
import os
import sys
from pathlib import Path

ENV_FILE = Path(__file__).resolve().parent / ".env"

def read_env():
    if not ENV_FILE.exists():
        return []
    with open(ENV_FILE, "r", encoding="utf-8") as f:
        return f.readlines()

def write_env(lines):
    with open(ENV_FILE, "w", encoding="utf-8") as f:
        f.writelines(lines)

def set_env_value(key, value, lines):
    if not value:
        return lines
    out = []
    found = False
    for line in lines:
        if line.strip().startswith(f"{key}="):
            cur = line.split("=", 1)[1].strip()
            if not cur and value:
                out.append(f"{key}={value}\n")
            else:
                out.append(line)
            found = True
        else:
            out.append(line)
    if not found:
        out.append(f"{key}={value}\n")
    return out

def main():
    print("=" * 60)
    print("  EKSIK .ENV ALANLARINI DOLDUR")
    print("  (Bos birakmak icin Enter)")
    print("=" * 60)
    lines = read_env()
    updates = [
        ("RUBLE_CHANNEL_ID", "Ruble kanal chat id (ornegin -1001234567890)"),
        ("BASKENT_USERNAME", "Bashent API mail/kullanici adi"),
        ("BASKENT_PASSWORD", "Bashent API sifre"),
        ("BASKENT_API_TOKEN", "Bashent API token (bos birakilirsa login ile alinir)"),
        ("BASKENT_DEFAULT_VAULT_ID", "Bashent varsayilan vault UUID"),
        ("BASKENT_USDT_CURRENCY_ID", "Bashent USDT para birimi UUID"),
        ("BASKENT_RUBLE_CURRENCY_ID", "Bashent Ruble para birimi UUID"),
    ]
    for key, label in updates:
        cur = ""
        for line in lines:
            if line.strip().startswith(f"{key}="):
                cur = line.split("=", 1)[1].strip()
                break
        if cur:
            print(f"\n[{key}] (mevcut dolu) atlanacak.")
            continue
        val = input(f"\n{label}\n  {key}= ").strip()
        if val:
            lines = set_env_value(key, val, lines)
            print("  -> Kaydedildi.")
    write_env(lines)
    print("\n.env guncellendi. Kontrol: python panel_kontrol.py")

if __name__ == "__main__":
    main()
