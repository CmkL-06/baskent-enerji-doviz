"""
Kanal ID'sini getUpdates ile alir, .env'ye yazar.
1) Ruble botu (@MTTOperatorBot) kanala admin ekleyin
2) Kanala bir mesaj atin
3) Bu scripti calistirin: python kanal_id_al_ve_env_yaz.py
"""
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from dotenv import load_dotenv
load_dotenv()

import requests

ENV_FILE = Path(__file__).resolve().parent / ".env"

def get_updates():
    token = os.getenv("RUBLE_BOT_TOKEN", "").strip()
    if not token:
        print("RUBLE_BOT_TOKEN .env icinde yok.")
        return []
    r = requests.get(
        f"https://api.telegram.org/bot{token}/getUpdates",
        params={"limit": 50},
        timeout=15
    )
    if not r.ok or not r.json().get("ok"):
        return []
    return r.json().get("result", [])

def find_channel_ids(results):
    channels = []
    for u in results:
        msg = u.get("message") or u.get("channel_post")
        if not msg:
            continue
        chat = msg.get("chat", {})
        cid = chat.get("id")
        ctype = chat.get("type", "")
        title = chat.get("title") or chat.get("username") or str(cid)
        if cid and ctype in ("channel", "supergroup", "group"):
            channels.append((cid, ctype, title))
    return channels

def read_env():
    if not ENV_FILE.exists():
        return []
    with open(ENV_FILE, "r", encoding="utf-8") as f:
        return f.readlines()

def write_env(lines):
    with open(ENV_FILE, "w", encoding="utf-8") as f:
        f.writelines(lines)

def set_ruble_channel_id(lines, cid):
    out = []
    found = False
    for line in lines:
        if line.strip().startswith("RUBLE_CHANNEL_ID="):
            out.append(f"RUBLE_CHANNEL_ID={cid}\n")
            found = True
        else:
            out.append(line)
    if not found:
        out.append(f"RUBLE_CHANNEL_ID={cid}\n")
    return out

def main():
    print("=" * 60)
    print("  KANAL ID AL VE .ENV YE YAZ")
    print("=" * 60)
    print("\nRuble botu (@MTTOperatorBot) kanala admin ekleyip")
    print("kanalda bir mesaj attiniz mi? (Enter ile devam)")
    input()

    results = get_updates()
    channels = find_channel_ids(results)

    if not channels:
        print("\nHenuz kanal/grup mesaji bulunamadi.")
        print("Ruble botu kanala admin ekleyip kanalda mesaj atin,")
        print("sonra bu scripti tekrar calistirin.")
        return

    print("\nBulunan grup/kanallar:")
    for i, (cid, ctype, title) in enumerate(channels, 1):
        print(f"  {i}) ID: {cid}  Tip: {ctype}  Baslik: {title}")

    if len(channels) == 1:
        cid = channels[0][0]
        print(f"\nTek kanal bulundu. .env guncelleniyor: RUBLE_CHANNEL_ID={cid}")
    else:
        try:
            idx = int(input("\nHangisini .env ye yazalim? (numara): ").strip())
            if 1 <= idx <= len(channels):
                cid = channels[idx - 1][0]
            else:
                print("Gecersiz numara.")
                return
        except (ValueError, EOFError):
            print("Iptal.")
            return

    lines = read_env()
    lines = set_ruble_channel_id(lines, cid)
    write_env(lines)
    print(f"\n.env guncellendi: RUBLE_CHANNEL_ID={cid}")
    print("Botlari yeniden baslatin.")

if __name__ == "__main__":
    main()
