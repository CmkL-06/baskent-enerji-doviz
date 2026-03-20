"""
Telegram kanal/gruptaki botlari listeler.
Bizim 3 botumuz disindakileri "eski / ise yaramaz" adayi olarak isaretler.
Kullanim: telegram-bot klasorunde  python kanal_bot_kontrol.py
"""
import os
import sys
from pathlib import Path

# .env yukle
sys.path.insert(0, str(Path(__file__).resolve().parent))
from dotenv import load_dotenv
load_dotenv()

import requests

BASE = "https://api.telegram.org/bot"

def get_me(token):
    """Bot bilgisini al (id, username)."""
    r = requests.get(f"{BASE}{token}/getMe", timeout=10)
    if not r.ok or not r.json().get("ok"):
        return None
    u = r.json()["result"]
    return {"id": u["id"], "username": u.get("username") or "", "first_name": u.get("first_name") or ""}

def get_chat_admins(token, chat_id):
    """Grup/kanal yoneticilerini al (botlar dahil)."""
    # chat_id string olarak gonder (negatif id'ler icin)
    r = requests.get(f"{BASE}{token}/getChatAdministrators", params={"chat_id": str(chat_id)}, timeout=10)
    j = r.json() if r.ok else {}
    if not r.ok or not j.get("ok"):
        return None, j
    return j.get("result", []), None

def get_chat_info(token, chat_id):
    """Grup/kanal adi ve tipi."""
    r = requests.get(f"{BASE}{token}/getChat", params={"chat_id": chat_id}, timeout=10)
    if not r.ok or not r.json().get("ok"):
        return None
    c = r.json()["result"]
    return c.get("title") or c.get("username") or str(chat_id), c.get("type", "")

def main():
    main_t = os.getenv("MAIN_BOT_TOKEN", "").strip()
    op_t = os.getenv("OPERATOR_BOT_TOKEN", "").strip()
    ruble_t = os.getenv("RUBLE_BOT_TOKEN", "").strip()
    group_id = os.getenv("REQUIRED_GROUP_ID", "").strip()
    channel_id = os.getenv("RUBLE_CHANNEL_ID", "").strip()

    if not main_t:
        print("MAIN_BOT_TOKEN yok. .env dosyasini kontrol edin.")
        return

    # Bizim botlarimizin id ve username
    our_bots = {}
    for name, token in [("Ana bot", main_t), ("Operator bot", op_t), ("Ruble bot", ruble_t)]:
        if not token:
            continue
        me = get_me(token)
        if me:
            our_bots[me["id"]] = {"name": name, "username": me["username"], "id": me["id"]}

    our_ids = set(our_bots.keys())
    print("=" * 60)
    print("  BIZIM BOTLAR (env'deki token'lar)")
    print("=" * 60)
    for bid, info in our_bots.items():
        print(f"  {info['name']}: @{info['username']} (id: {info['id']})")
    print()

    # Grup (REQUIRED_GROUP_ID)
    if group_id:
        try:
            chat_id = int(group_id)
        except ValueError:
            chat_id = None
        if chat_id:
            info = get_chat_info(main_t, chat_id)
            title = info[0] if info else str(chat_id)
            admins, err = get_chat_admins(main_t, chat_id)
            if err is not None and not admins:
                print(f"Grup/Kanal: {title} (id: {chat_id})")
                desc = err.get("description") or err.get("error_description") or "bilinmeyen hata"
                print("  Yonetici listesi alinamadi:", desc)
                print("  (Bot grupta uye/yonetici degilse bu hata olur. Ana botu gruba ekleyip yonetici yapin.)")
            elif admins is not None:
                bot_admins = [a for a in admins if a.get("user", {}).get("is_bot")]
                print("=" * 60)
                print(f"  GRUP/KANAL: {title} (id: {chat_id})")
                print("  Yonetici botlar:")
                print("=" * 60)
                for a in bot_admins:
                    u = a["user"]
                    uid = u["id"]
                    uname = u.get("username") or "-"
                    fn = u.get("first_name") or ""
                    if uid in our_ids:
                        print(f"    [BIZIM] @{uname} (id: {uid}) - {our_bots[uid]['name']}")
                    else:
                        print(f"    [DIGER / ESKI ADAY] @{uname} (id: {uid}) - {fn}")
                if not bot_admins:
                    print("    (Yonetici bot yok.)")
                print()

    # Ruble kanali (varsa)
    if channel_id:
        try:
            cid = int(channel_id)
        except ValueError:
            cid = None
        if cid:
            info = get_chat_info(main_t, cid)
            title = info[0] if info else str(cid)
            admins, err = get_chat_admins(main_t, cid)
            if err is not None and not admins:
                print(f"Ruble Kanal: {title} (id: {cid}) - liste alinamadi")
            elif admins is not None:
                bot_admins = [a for a in admins if a.get("user", {}).get("is_bot")]
                print("=" * 60)
                print(f"  RUBLE KANAL: {title} (id: {cid})")
                print("  Yonetici botlar:")
                print("=" * 60)
                for a in bot_admins:
                    u = a["user"]
                    uid = u["id"]
                    uname = u.get("username") or "-"
                    fn = u.get("first_name") or ""
                    if uid in our_ids:
                        print(f"    [BIZIM] @{uname} (id: {uid}) - {our_bots[uid]['name']}")
                    else:
                        print(f"    [DIGER / ESKI ADAY] @{uname} (id: {uid}) - {fn}")
                if not bot_admins:
                    print("    (Yonetici bot yok.)")

    print()
    print("Ozet: [DIGER / ESKI ADAY] olarak isaretlenen botlar sizin 3 botunuz degil;")
    print("isterseniz gruptan/kanaldan kaldirabilirsiniz (Telegram > Gruba git > Uyeler > ilgili bot > Cikar).")

if __name__ == "__main__":
    main()
