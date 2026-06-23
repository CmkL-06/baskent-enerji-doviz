"""
Tüm Botları Başlatıcı — Money Transfer Turkey
3 botu asyncio ile paralel çalıştırır + watchdog (çökme algılama & yeniden başlatma)
  @MoneyExchangeTurkeyBot  → main_bot.py      (ana müşteri botu)
  @MTTOperatorBot          → operator_bot.py  (operatör paneli)
  @MoneyExchangeRubleBot   → ruble_bot.py     (ruble kanal botu)
"""

import asyncio
import logging
import sys
import io
from datetime import datetime

# Windows cp1252 sorununu çöz — UTF-8 zorla
if hasattr(sys.stdout, 'buffer'):
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
if hasattr(sys.stderr, 'buffer'):
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

from config import Config
import database as db

logging.basicConfig(
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
    level=logging.INFO,
    handlers=[
        logging.StreamHandler(sys.stdout),
        logging.FileHandler('bot.log', encoding='utf-8'),
    ]
)
logger = logging.getLogger(__name__)


# ═══════════════════════════════════════════════
# BOT BAŞLATMA FONKSİYONLARI
# ═══════════════════════════════════════════════

async def run_main_bot():
    import main_bot
    await main_bot.start()


async def run_operator_bot():
    import operator_bot
    await operator_bot.start()


async def run_ruble_bot():
    import ruble_bot
    await ruble_bot.start()


# ═══════════════════════════════════════════════
# WATCHDOG — ÇÖKME ALGILA & YENİDEN BAŞLAT
# ═══════════════════════════════════════════════

BOT_RUNNERS = {
    'Main Bot': run_main_bot,
    'Operator Bot': run_operator_bot,
    'Ruble Bot': run_ruble_bot,
}

bot_tasks: dict[str, asyncio.Task] = {}
restart_counts: dict[str, int] = {}
MAX_RESTARTS = 10


async def notify_admin(message: str):
    try:
        from telegram import Bot
        bot = Bot(token=Config.MAIN_BOT_TOKEN)
        await bot.send_message(chat_id=Config.ADMIN_ID, text=message, parse_mode="Markdown")
    except Exception as e:
        logger.error(f"Admin bildirim hatası: {e}")


async def start_bot(name: str):
    runner = BOT_RUNNERS[name]
    try:
        logger.info(f"[{name}] başlatılıyor...")
        await runner()
    except Exception as e:
        logger.critical(f"[{name}] ÇÖKTÜ: {e}", exc_info=True)
        raise


async def watchdog():
    while True:
        await asyncio.sleep(30)

        for name, task in list(bot_tasks.items()):
            if task.done():
                count = restart_counts.get(name, 0)

                error = "Bilinmeyen"
                try:
                    exc = task.exception()
                    if exc:
                        error = str(exc)
                except (asyncio.CancelledError, asyncio.InvalidStateError):
                    error = "İptal edildi"

                logger.critical(f"[WATCHDOG] {name} çöktü! (Restart #{count + 1}): {error}")

                if count >= MAX_RESTARTS:
                    logger.critical(f"[WATCHDOG] {name} max restart sayısına ulaştı ({MAX_RESTARTS}). Durduruldu.")
                    await notify_admin(
                        f"🚨 **{name} DURDU!**\n\n"
                        f"Max yeniden başlatma sayısına ulaşıldı ({MAX_RESTARTS}).\n"
                        f"Son hata: {error}\n\n"
                        f"Manuel müdahale gerekiyor!"
                    )
                    continue

                await notify_admin(
                    f"🚨 **{name} ÇÖKTÜ!**\n\n"
                    f"Zaman: {datetime.now().strftime('%H:%M:%S')}\n"
                    f"Hata: {error}\n"
                    f"Yeniden başlatma #{count + 1}/{MAX_RESTARTS}..."
                )

                await asyncio.sleep(5)
                restart_counts[name] = count + 1
                bot_tasks[name] = asyncio.create_task(start_bot(name))
                logger.info(f"[WATCHDOG] {name} yeniden başlatıldı (#{count + 1})")
                await notify_admin(f"✅ **{name} yeniden başlatıldı** (#{count + 1})")


# ═══════════════════════════════════════════════
# ANA GİRİŞ NOKTASI
# ═══════════════════════════════════════════════

async def main():
    errors = Config.validate()
    if errors:
        for err in errors:
            logger.error(f"Konfigurasyon hatasi: {err}")
        logger.error("Bot baslatılamadı — .env dosyasını kontrol edin!")
        return

    logger.info("Veritabani baslatiliyor...")
    db.init_database()
    logger.info("Veritabani hazir")

    print("\n" + "=" * 60)
    print("    MONEY TRANSFER TURKEY - TELEGRAM BOT SISTEMI")
    print("=" * 60)
    print(f"    Zaman: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"    Botlar: Main, Operator, Ruble")
    print(f"    Watchdog: Aktif (30s aralik, max {MAX_RESTARTS} restart)")
    print("=" * 60 + "\n")

    for name in BOT_RUNNERS:
        restart_counts[name] = 0
        bot_tasks[name] = asyncio.create_task(start_bot(name))

    watchdog_task = asyncio.create_task(watchdog())

    try:
        await asyncio.gather(watchdog_task, *bot_tasks.values(), return_exceptions=True)
    except KeyboardInterrupt:
        logger.info("Kapatma sinyali alindi...")

    logger.info("Botlar durduruluyor...")
    try:
        import main_bot
        import operator_bot
        import ruble_bot
        await main_bot.stop()
        await operator_bot.stop()
        await ruble_bot.stop()
    except Exception as e:
        logger.error(f"Kapatma hatasi: {e}")

    logger.info("Tum botlar durduruldu.")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nCikis yapiliyor...")
