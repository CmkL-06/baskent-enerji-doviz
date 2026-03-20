"""
Ruble botunun uye oldugu kanallarin chat id'sini getUpdates ile bulur.
Ruble botu kanala admin ekleyip bir mesaj atin, sonra bu scripti calistirin.
"""
import os
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from dotenv import load_dotenv
load_dotenv()

import requests

def main():
    token = os.getenv("RUBLE_BOT_TOKEN", "").strip()
    if not token:
        print("RUBLE_BOT_TOKEN .env icinde yok.")
        return
    url = f"https://api.telegram.org/bot{token}/getUpdates"
    r = requests.get(url, params={"limit": 100}, timeout=15)
    if not r.ok:
        print("API hatasi:", r.status_code)
        return
    j = r.json()
    if not j.get("ok"):
        print("Telegram API:", j.get("description", "?"))
        return
    results = j.get("result", [])
    channels = {}
    for u in results:
        msg = u.get("message") or u.get("channel_post")
        if not msg:
            continue
        chat = msg.get("chat", {})
        cid = chat.get("id")
        ctype = chat.get("type", "")
        title = chat.get("title") or chat.get("username") or str(cid)
        if cid and ctype in ("channel", "supergroup", "group"):
            key = (cid, ctype)
            if key not in channels:
                channels[key] = title
    print("=" * 60)
    print("  RUBLE BOT - GORULEN GRUP/KANAL ID'LERI (getUpdates)")
    print("=" * 60)
    if not channels:
        print("\nHenuz kanal/grup mesaji yok.")
        print("Ruble botu kanala admin ekleyip kanalda bir mesaj atin,")
        print("sonra bu scripti tekrar calistirin.")
        print("\nVeya kanala @userinfobot ekleyip bir mesaji iletin; chat id gorunur.")
    else:
        for (cid, ctype), title in sorted(channels.items(), key=lambda x: -abs(x[0][0])):
            print(f"\n  Chat ID: {cid}")
            print(f"  Tip: {ctype} | Baslik: {title}")
            if ctype == "channel" or (ctype == "supergroup" and str(cid).startswith("-100")):
                print(f"  >>> .env icin: RUBLE_CHANNEL_ID={cid}")
    print("\n" + "=" * 60)

if __name__ == "__main__":
    main()
