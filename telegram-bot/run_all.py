"""
Tüm Botları Başlatıcı — Money Transfer Turkey
3 botu asyncio ile paralel çalıştırır + watchdog (çökme algılama & yeniden başlatma)
"""

import asyncio
import logging
import sys
from datetime import datetime

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
    """Ana müşteri botunu başlat"""
    import main_bot
    await main_bot.start()


async def run_operator_bot():
    """Operatör botunu başlat"""
    import operator_bot
    await operator_bot.start()


async def run_ruble_bot():
    """Ruble kanal botunu başlat"""
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
    """Admin'e Telegram mesajı gönder"""
    try:
        from telegram import Bot
        bot = Bot(token=Config.MAIN_BOT_TOKEN)
        await bot.send_message(chat_id=Config.ADMIN_ID, text=message, parse_mode="Markdown")
    except Exception as e:
        logger.error(f"Admin bildirim hatası: {e}")


async def start_bot(name: str):
    """Tek bir botu başlat, hata yakalama ile"""
    runner = BOT_RUNNERS[name]
    try:
        logger.info(f"[{name}] başlatılıyor...")
        await runner()
    except Exception as e:
        logger.critical(f"[{name}] ÇÖKTÜ: {e}", exc_info=True)
        raise


async def watchdog():
    """Botları izle, çökenleri yeniden başlat"""
    while True:
        await asyncio.sleep(30)

        for name, task in list(bot_tasks.items()):
            if task.done():
                count = restart_counts.get(name, 0)

                # Hata bilgisini al
                error = "Bilinmeyen"
                try:
                    exc = task.exception()
                    if exc:
                        error = str(exc)
                except (asyncio.CancelledError, asyncio.InvalidStateError):
                    error = "İptal edildi"

                logger.critical(f"[WATCHDOG] {name} çöktü! (Restart #{count + 1}): {error}")

                # Max restart kontrolü
                if count >= MAX_RESTARTS:
                    logger.critical(f"[WATCHDOG] {name} max restart sayısına ulaştı ({MAX_RESTARTS}). Durduruldu.")
                    await notify_admin(
                        f"🚨 **{name} DURDU!**\n\n"
                        f"Max yeniden başlatma sayısına ulaşıldı ({MAX_RESTARTS}).\n"
                        f"Son hata: {error}\n\n"
                        f"Manuel müdahale gerekiyor!"
                    )
                    continue

                # Admin'e bildir
                await notify_admin(
                    f"🚨 **{name} ÇÖKTÜ!**\n\n"
                    f"⏰ Zaman: {datetime.now().strftime('%H:%M:%S')}\n"
                    f"❌ Hata: {error}\n"
                    f"🔄 Yeniden başlatma #{count + 1}/{MAX_RESTARTS}..."
                )

                # Bekle ve yeniden başlat
                await asyncio.sleep(5)

                restart_counts[name] = count + 1
                bot_tasks[name] = asyncio.create_task(start_bot(name))
                logger.info(f"[WATCHDOG] {name} yeniden başlatıldı (#{count + 1})")

                await notify_admin(f"✅ **{name} yeniden başlatıldı** (#{count + 1})")


# ═══════════════════════════════════════════════
# ANA GİRİŞ NOKTASI
# ═══════════════════════════════════════════════

async def main():
    # Konfigürasyon doğrulama
    errors = Config.validate()
    if errors:
        for err in errors:
            logger.error(f"Konfigürasyon hatası: {err}")
        logger.error("Bot başlatılamadı — .env dosyasını kontrol edin!")
        return

    # Veritabanı başlatma
    logger.info("Veritabanı başlatılıyor...")
    db.init_database()
    logger.info("Veritabanı hazır")

    # Banner
    print("\n" + "=" * 60)
    print("    MONEY TRANSFER TURKEY — TELEGRAM BOT SİSTEMİ")
    print("=" * 60)
    print(f"    Zaman: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"    Botlar: Main, Operator, Ruble")
    print(f"    Watchdog: Aktif (30s aralık, max {MAX_RESTARTS} restart)")
    print("=" * 60 + "\n")

    # Tüm botları başlat
    for name in BOT_RUNNERS:
        restart_counts[name] = 0
        bot_tasks[name] = asyncio.create_task(start_bot(name))

    # Watchdog başlat
    watchdog_task = asyncio.create_task(watchdog())

    # Tüm task'ların bitmesini bekle (normalde sonsuz çalışır)
    try:
        await asyncio.gather(watchdog_task, *bot_tasks.values(), return_exceptions=True)
    except KeyboardInterrupt:
        logger.info("Kapatma sinyali alındı...")

    # Temiz kapatma
    logger.info("Botlar durduruluyor...")
    try:
        import main_bot
        import operator_bot
        import ruble_bot
        await main_bot.stop()
        await operator_bot.stop()
        await ruble_bot.stop()
    except Exception as e:
        logger.error(f"Kapatma hatası: {e}")

    logger.info("Tüm botlar durduruldu.")


if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("\nÇıkış yapılıyor...")
