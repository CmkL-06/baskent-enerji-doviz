"""
Ruble Kanal Botu — Money Transfer Turkey
Ruble işlemlerini Telegram kanalı üzerinden yönetir.
Banka sağlayıcıları reply ile banka bilgisi gönderir, dekont onaylar/reddeder.
python-telegram-bot 20.6 (async)
"""

import asyncio
import html
import re
import random
import logging
from datetime import datetime

from telegram import (
    Update, Bot,
    InlineKeyboardButton, InlineKeyboardMarkup,
)
from telegram.ext import (
    Application, CommandHandler, MessageHandler,
    CallbackQueryHandler, filters, ContextTypes,
)

from config import Config
from translations import t
import database as db
from baskent_api import send_exchange_for_transaction, record_dealer_entry

logger = logging.getLogger(__name__)

# _approve_payment icin islem-basi kilit -- main_bot.py'deki _completion_locks
# ile ayni desen. Gercek koruma db.complete_transaction_atomic'in donus degeridir,
# bu kilit tek process icini korumak icin ek/ucuz bir guvence.
_completion_locks: dict[int, asyncio.Lock] = {}


def _get_completion_lock(transaction_id: int) -> asyncio.Lock:
    lock = _completion_locks.get(transaction_id)
    if lock is None:
        lock = asyncio.Lock()
        _completion_locks[transaction_id] = lock
    return lock

# ═══════════════════════════════════════════════
# AKTİF İŞLEMLER (memory cache — DB ile yedekli)
# ═══════════════════════════════════════════════
# {transaction_id: {'customer_id': int, 'customer_name': str, 'amount_rub': float, ...}}
active_ruble_transactions: dict = {}


# ═══════════════════════════════════════════════
# YARDIMCI
# ═══════════════════════════════════════════════

def _is_bank_provider(user_id: int) -> bool:
    return db.is_bank_provider(user_id)


def _get_customer_lang(customer_id: int) -> str:
    return db.get_customer_language(customer_id)


def _extract_transaction_id(text: str) -> int | None:
    """Mesaj metninden İşlem ID'sini çıkar"""
    if not text:
        return None
    match = re.search(r'İşlem ID:\s*#?(\d+)', text)
    if match:
        return int(match.group(1))
    # Alternatif format
    match = re.search(r'#(\d+)', text)
    if match:
        return int(match.group(1))
    return None


def _load_transaction_to_cache(trans_id: int) -> bool:
    """İşlemi DB'den cache'e yükle"""
    if trans_id in active_ruble_transactions:
        return True

    trans = db.get_transaction(trans_id)
    if not trans or trans.get('currency') != 'RUBLE':
        return False

    amount_rub = float(trans.get('amount', 0))
    exchange_rate = float(trans.get('exchange_rate', 0)) or Config.DEFAULT_RUB_RATE
    amount_try = float(trans.get('try_amount', 0)) or (amount_rub * exchange_rate)

    active_ruble_transactions[trans_id] = {
        'customer_id': trans.get('customer_id'),
        'customer_name': trans.get('first_name', 'Müşteri'),
        'amount_rub': amount_rub,
        'amount_try': amount_try,
        'exchange_rate': exchange_rate,
        'status': trans.get('status', 'pending'),
    }
    return True


# ═══════════════════════════════════════════════
# /start KOMUTU
# ═══════════════════════════════════════════════

async def cmd_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.message.from_user.id

    if _is_bank_provider(user_id):
        await update.message.reply_text(
            "🏦 <b>Ruble İşlem Botu</b>\n\n"
            "Hoş geldiniz! Siz yetkili banka hesabı sağlayıcısısınız.\n\n"
            "📌 Kanalda yeni işlemler görünecek\n"
            "💳 İşlem mesajına reply atarak banka hesabı bilgisi gönderin\n"
            "✅ Dekont geldiğinde onaylama yapabilirsiniz\n\n"
            "📝 <b>Komutlar:</b>\n"
            "/getid — Kendi ID'nizi öğrenin\n"
            "/list_providers — Tüm sağlayıcıları listele",
            parse_mode="HTML"
        )
    else:
        await update.message.reply_text(
            "⛔ Bu bot sadece yetkili banka hesabı sağlayıcıları için kullanılabilir.\n\n"
            "/getid komutunu kullanarak ID'nizi öğrenebilirsiniz."
        )


# ═══════════════════════════════════════════════
# /getid KOMUTU
# ═══════════════════════════════════════════════

async def cmd_getid(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user = update.message.from_user
    is_provider = "✅ Evet" if _is_bank_provider(user.id) else "❌ Hayır"

    msg = (
        f"👤 <b>Bilgileriniz:</b>\n\n"
        f"🆔 ID: <code>{user.id}</code>\n"
        f"📝 Ad: {html.escape(user.first_name)}\n"
        f"📱 Username: @{html.escape(user.username or 'Yok')}\n"
        f"🏦 Banka Sağlayıcısı: {is_provider}"
    )

    if not _is_bank_provider(user.id):
        msg += f"\n\n💡 Banka sağlayıcısı olmak için bu ID'yi yöneticiye bildirin: <code>{user.id}</code>"

    await update.message.reply_text(msg, parse_mode="HTML")


# ═══════════════════════════════════════════════
# PROVIDER YÖNETİM KOMUTLARI
# ═══════════════════════════════════════════════

async def cmd_add_provider(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if update.message.from_user.id != Config.ADMIN_ID:
        await update.message.reply_text("⛔ Bu komutu kullanma yetkiniz yok!")
        return

    if not context.args:
        await update.message.reply_text("Kullanım: /add_provider [user_id]")
        return

    try:
        new_id = int(context.args[0])
        if db.is_bank_provider(new_id):
            await update.message.reply_text("⚠️ Bu kullanıcı zaten sağlayıcı listesinde!")
        else:
            db.add_bank_provider(new_id, update.message.from_user.id)
            providers = db.get_bank_providers()
            await update.message.reply_text(
                f"✅ Yeni sağlayıcı eklendi!\n\n"
                f"🆔 ID: {new_id}\n"
                f"📊 Toplam sağlayıcı sayısı: {len(providers)}"
            )
    except ValueError:
        await update.message.reply_text("❌ Geçersiz kullanıcı ID!")


async def cmd_remove_provider(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if update.message.from_user.id != Config.ADMIN_ID:
        await update.message.reply_text("⛔ Bu komutu kullanma yetkiniz yok!")
        return

    if not context.args:
        await update.message.reply_text("Kullanım: /remove_provider [user_id]")
        return

    try:
        pid = int(context.args[0])
        if pid == Config.ADMIN_ID:
            await update.message.reply_text("⚠️ Admin silinemez!")
            return

        if db.is_bank_provider(pid):
            db.remove_bank_provider(pid)
            providers = db.get_bank_providers()
            await update.message.reply_text(
                f"✅ Sağlayıcı kaldırıldı!\n🆔 ID: {pid}\n"
                f"📊 Kalan sağlayıcı sayısı: {len(providers)}"
            )
        else:
            await update.message.reply_text("❌ Bu kullanıcı sağlayıcı listesinde değil!")
    except ValueError:
        await update.message.reply_text("❌ Geçersiz kullanıcı ID!")


async def cmd_list_providers(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.message.from_user.id
    if not _is_bank_provider(user_id):
        await update.message.reply_text("⛔ Bu komutu kullanma yetkiniz yok!")
        return

    providers = db.get_bank_providers()
    msg = "🏦 <b>Aktif Banka Hesabı Sağlayıcıları:</b>\n\n"
    for i, pid in enumerate(providers, 1):
        admin_tag = " 👑 (Admin)" if pid == Config.ADMIN_ID else ""
        you_tag = " 👤 (Siz)" if pid == user_id else ""
        msg += f"{i}. <code>{pid}</code>{admin_tag}{you_tag}\n"
    msg += f"\n📊 Toplam: {len(providers)} sağlayıcı"

    await update.message.reply_text(msg, parse_mode="HTML")


# ═══════════════════════════════════════════════
# CALLBACK İŞLEYİCİ — ONAY / RED
# ═══════════════════════════════════════════════

async def handle_callback(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    user_id = query.from_user.id
    data = query.data

    # Yetki kontrolü
    if not _is_bank_provider(user_id):
        await query.answer("⛔ Bu işlemi yapmaya yetkiniz yok!", show_alert=True)
        return

    # ── Ödemeyi Onayla ──
    if data.startswith("approve_payment_"):
        tid = int(data.split("_")[2])
        await _approve_payment(query, tid, user_id)
        return

    # ── Ödemeyi Reddet ──
    if data.startswith("reject_payment_"):
        tid = int(data.split("_")[2])
        await _reject_payment(query, tid, user_id)
        return


# ═══════════════════════════════════════════════
# ÖDEME ONAYLAMA
# ═══════════════════════════════════════════════

async def _approve_payment(query, tid: int, provider_id: int):
    async with _get_completion_lock(tid):
        # İşlemi tamamla
        trans = db.get_transaction(tid)
        if not trans:
            await query.answer("❌ İşlem bulunamadı!", show_alert=True)
            return

        # Zaten tamamlanmış mı kontrol et (hızlı yol, atomik değil)
        if trans.get('status') == 'completed':
            logger.warning(f"İşlem #{tid} zaten tamamlanmış, tekrar onaylanmayacak")
            await query.answer("⚠️ Bu ödeme zaten onaylanmış!", show_alert=True)
            return

        # Mevcut completion_code varsa onu kullan, yoksa yeni üret
        completion_code = trans.get('completion_code')
        if not completion_code:
            completion_code = str(random.randint(10000, 99999))
            db.update_transaction(tid, completion_code=completion_code)

        # Atomik tamamlama -- çift onaya karşı gerçek koruma (DB seviyesi,
        # botlar ayrı process olduğu için yukarıdaki kontrol tek başına yetmez)
        if not db.complete_transaction_atomic(tid, completion_code):
            logger.warning(f"İşlem #{tid} zaten tamamlanmış (atomic guard), tekrar işlenmeyecek")
            await query.answer("⚠️ Bu ödeme zaten onaylanmış!", show_alert=True)
            return

        # Dealer bakiyesinden düş
        try_amount = trans.get('try_amount') or trans.get('amount_try')
        referral_code = trans.get('referral_code')
        if try_amount and referral_code:
            db.reduce_dealer_balance(referral_code, float(try_amount), transaction_id=tid)
            logger.info(f"Dealer {referral_code} bakiye düşürüldü: {try_amount} TRY (İşlem #{tid})")

        # Mesajı DB'ye kaydet
        db.save_message(tid, provider_id, 'bank_provider',
                        f"ÖDEME ONAYLANDI - {query.from_user.first_name} - İşlem Kodu: {completion_code}")

        # BaşkentEnerji API'ye gönder
        try:
            send_exchange_for_transaction(tid)
        except Exception as e:
            logger.error(f"BaşkentEnerji API hatası (İşlem #{tid}): {e}")

        # Cari hesap kaydı
        if try_amount and referral_code:
            try:
                rate = float(trans.get('exchange_rate') or 0)
                amount = float(trans.get('amount') or 0)
                if rate <= 0 or amount <= 0:
                    logger.warning(f"İşlem #{tid} rate={rate} amount={amount} — cari kayıt atlanıyor")
                else:
                    record_dealer_entry(
                        dealer_code=referral_code,
                        currency=trans.get('currency', 'RUBLE'),
                        amount=amount,
                        amount_try=float(try_amount),
                        exchange_rate=rate,
                        is_buy=True,
                        transaction_id=tid
                    )
            except Exception as e:
                logger.error(f"Cari hesap kayıt hatası (İşlem #{tid}): {e}")

        # Kanal mesajını güncelle
        try:
            await query.edit_message_caption(
                caption=(
                    f"✅ <b>İŞLEM TAMAMLANDI</b>\n"
                    f"━━━━━━━━━━━━━━━\n"
                    f"🔖 İşlem ID: #{tid}\n"
                    f"✔️ Ödeme onaylandı\n"
                    f"👤 Onaylayan: {html.escape(query.from_user.first_name)}\n"
                    f"⏰ {datetime.now().strftime('%H:%M')}"
                ),
                parse_mode="HTML"
            )
        except Exception:
            try:
                await query.edit_message_text(
                    f"✅ <b>İŞLEM TAMAMLANDI</b>\n"
                    f"━━━━━━━━━━━━━━━\n"
                    f"🔖 İşlem ID: #{tid}\n"
                    f"✔️ Ödeme onaylandı\n"
                    f"👤 Onaylayan: {html.escape(query.from_user.first_name)}\n"
                    f"⏰ {datetime.now().strftime('%H:%M')}",
                    parse_mode="HTML"
                )
            except Exception as e:
                logger.error(f"Kanal mesajı güncelleme hatası: {e}")

        # Müşteriye tamamlanma bildirimi
        customer_id = trans.get('customer_id')
        if customer_id:
            lang = _get_customer_lang(customer_id)
            ruble_amount = trans.get('amount', 0)
            exchange_rate = trans.get('exchange_rate', 0)
            try_amount_val = float(try_amount) if try_amount else 0

            try:
                async with Bot(token=Config.MAIN_BOT_TOKEN) as main_bot:
                    await main_bot.send_message(
                        chat_id=customer_id,
                        text=(
                            f"{t('ruble_transaction_completed', lang)}\n\n"
                            f"🎯 <b>{html.escape(t('completion_code_label', lang, code=completion_code))}</b>\n\n"
                            f"{t('transaction_details_header', lang)}\n"
                            f"• {t('transaction_id_label', lang, tid=tid)}\n"
                            f"• {t('ruble_amount_label', lang, amount=f'{ruble_amount:,.2f}')}\n"
                            f"• {t('try_amount_label', lang, amount=f'{try_amount_val:,.2f}')}\n"
                            f"• {t('exchange_rate_label', lang, rate=f'{exchange_rate:.4f}')}\n"
                            f"• {t('status_approved', lang)}\n\n"
                            f"⚠️ <b>{html.escape(t('show_code_to_dealer', lang))}</b>\n\n"
                            f"{t('thank_you', lang)}"
                        ),
                        parse_mode="HTML"
                    )
            except Exception as e:
                logger.error(f"Müşteriye tamamlama bildirimi hatası: {e}")

        # Web panele bildirim
        db.notify_web_panel(tid)

        # Cache temizle
        active_ruble_transactions.pop(tid, None)

        await query.answer("✅ İşlem başarıyla onaylandı!", show_alert=True)


# ═══════════════════════════════════════════════
# ÖDEME REDDİ
# ═══════════════════════════════════════════════

async def _reject_payment(query, tid: int, provider_id: int):
    trans = db.get_transaction(tid)
    if not trans:
        await query.answer("❌ İşlem bulunamadı!", show_alert=True)
        return

    # İşlemi reddedildi olarak işaretle
    db.update_transaction(tid, status='bank_rejected')

    # Mesajı DB'ye kaydet
    db.save_message(tid, provider_id, 'bank_provider',
                    f"ÖDEME REDDEDİLDİ - {query.from_user.first_name}")

    # Kanal mesajını güncelle
    try:
        await query.edit_message_caption(
            caption=(
                f"❌ <b>ÖDEME REDDEDİLDİ</b>\n"
                f"━━━━━━━━━━━━━━━\n"
                f"🔖 İşlem ID: #{tid}\n"
                f"❌ Ödeme reddedildi\n"
                f"👤 Reddeden: {html.escape(query.from_user.first_name)}\n"
                f"⏰ {datetime.now().strftime('%H:%M')}"
            ),
            parse_mode="HTML"
        )
    except Exception:
        try:
            await query.edit_message_text(
                f"❌ <b>ÖDEME REDDEDİLDİ</b>\n"
                f"━━━━━━━━━━━━━━━\n"
                f"🔖 İşlem ID: #{tid}\n"
                f"❌ Ödeme reddedildi\n"
                f"👤 Reddeden: {html.escape(query.from_user.first_name)}\n"
                f"⏰ {datetime.now().strftime('%H:%M')}",
                parse_mode="HTML"
            )
        except Exception as e:
            logger.error(f"Kanal mesajı güncelleme hatası: {e}")

    # Müşteriye red bildirimi
    customer_id = trans.get('customer_id')
    if customer_id:
        lang = _get_customer_lang(customer_id)
        try:
            async with Bot(token=Config.MAIN_BOT_TOKEN) as main_bot:
                await main_bot.send_message(
                    chat_id=customer_id,
                    text=(
                        f"❌ <b>{html.escape(t('payment_rejected', lang))}</b>\n"
                        f"━━━━━━━━━━━━━━━\n"
                        f"{t('transaction_id_label', lang, tid=tid)}\n\n"
                        f"{t('payment_rejected_detail', lang)}"
                    ),
                    parse_mode="HTML"
                )
        except Exception as e:
            logger.error(f"Müşteriye red bildirimi hatası: {e}")

    # Web panele bildirim
    db.notify_web_panel(tid)

    # Cache temizle
    active_ruble_transactions.pop(tid, None)

    await query.answer("❌ Ödeme reddedildi!", show_alert=True)


# ═══════════════════════════════════════════════
# MESAJ İŞLEYİCİ — BANKA BİLGİSİ GÖNDERME
# ═══════════════════════════════════════════════

async def handle_message(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if not update.message:
        return

    user_id = update.message.from_user.id
    text = update.message.text or ""

    # Sadece banka sağlayıcıları işleyebilir
    if not _is_bank_provider(user_id):
        return

    # Reply to message kontrolü
    if not update.message.reply_to_message:
        await update.message.reply_text(
            "💡 <b>Kullanım:</b>\n"
            "İşlem mesajına reply atarak banka hesabı bilgilerini gönderin.",
            parse_mode="HTML"
        )
        return

    # Orijinal mesajdan işlem ID'sini çıkar
    original_text = update.message.reply_to_message.text or \
                    update.message.reply_to_message.caption or ""
    trans_id = _extract_transaction_id(original_text)

    if not trans_id:
        await update.message.reply_text("❌ İşlem ID'si bulunamadı! Bir işlem mesajına reply atın.")
        return

    # İşlemi yükle
    if not _load_transaction_to_cache(trans_id):
        await update.message.reply_text("❌ İşlem bulunamadı veya Ruble işlemi değil!")
        return

    trans = db.get_transaction(trans_id)
    if not trans:
        await update.message.reply_text("❌ İşlem bulunamadı!")
        return

    customer_id = trans.get('customer_id')
    if not customer_id:
        await update.message.reply_text("❌ Müşteri bilgisi bulunamadı!")
        return

    # İşlem durumunu güncelle
    db.update_transaction(trans_id, status='waiting_payment')

    # Banka bilgilerini Messages tablosuna kaydet
    db.save_message(trans_id, user_id, 'bank_provider', f"BANKA BİLGİLERİ: {text}")

    # Web panele bildirim
    db.notify_web_panel(trans_id)

    # Müşteriye banka hesabı bilgilerini gönder (main bot üzerinden)
    lang = _get_customer_lang(customer_id)
    try:
        async with Bot(token=Config.MAIN_BOT_TOKEN) as main_bot:
            await main_bot.send_message(
                chat_id=customer_id,
                text=(
                    f"{t('bank_warning', lang)}\n"
                    f"━━━━━━━━━━━━━━━\n\n"
                    f"{t('bank_info_header', lang)}\n"
                    f"━━━━━━━━━━━━━━━\n\n"
                    f"<pre>{html.escape(text)}</pre>\n\n"
                    f"━━━━━━━━━━━━━━━\n"
                    f"{t('copy_instruction', lang)}\n\n"
                    f"{t('send_pdf_receipt', lang)}"
                ),
                parse_mode="HTML"
            )

        # Cache güncelle
        if trans_id in active_ruble_transactions:
            active_ruble_transactions[trans_id]['status'] = 'bank_provided'
            active_ruble_transactions[trans_id]['bank_info'] = text

        # Kanala onay mesajı
        await update.message.reply_text(
            f"✅ Banka hesabı bilgileri müşteriye gönderildi!\n"
            f"📎 Dekont bekleniyor..."
        )

    except Exception as e:
        logger.error(f"Banka bilgisi müşteriye gönderim hatası: {e}")
        await update.message.reply_text(f"❌ Banka bilgileri gönderilemedi!\nHata: {e}")


    # main_bot.py'deki state'i güncelle
    try:
        from main_bot import set_state
        set_state(customer_id, 'waiting_receipt', trans_id)
    except ImportError:
        logger.warning("main_bot modülü import edilemedi — state güncellenmedi")
    except Exception as e:
        logger.error(f"State güncelleme hatası: {e}")


# ═══════════════════════════════════════════════
# BOT BAŞLATMA
# ═══════════════════════════════════════════════

_app: Application = None


def create_app() -> Application:
    global _app
    app = Application.builder().token(Config.RUBLE_BOT_TOKEN).build()

    # Komutlar
    app.add_handler(CommandHandler("start", cmd_start))
    app.add_handler(CommandHandler("getid", cmd_getid))
    app.add_handler(CommandHandler("add_provider", cmd_add_provider))
    app.add_handler(CommandHandler("remove_provider", cmd_remove_provider))
    app.add_handler(CommandHandler("list_providers", cmd_list_providers))

    # Callback (buton) handler
    app.add_handler(CallbackQueryHandler(handle_callback))

    # Metin mesajları (banka sağlayıcılarından)
    app.add_handler(MessageHandler(
        filters.TEXT & ~filters.COMMAND,
        handle_message
    ))

    _app = app
    return app


async def _heartbeat_loop():
    """60 saniyede bir heartbeat gönder"""
    while True:
        try:
            db.update_heartbeat('ruble_bot', len(active_ruble_transactions))
        except Exception as e:
            logger.error(f"Heartbeat hatası: {e}")
        await asyncio.sleep(60)


async def start():
    """Bot'u başlat (run_all.py'den çağrılır)"""
    # .env'deki provider'ları DB'ye seed et
    db.seed_bank_providers(Config.BANK_PROVIDERS)

    app = create_app()
    await app.initialize()
    await app.start()
    await app.updater.start_polling(drop_pending_updates=True)
    logger.info("Ruble kanal botu başlatıldı")

    # Heartbeat background task
    asyncio.create_task(_heartbeat_loop())

    # Sonsuz bekle
    stop_event = asyncio.Event()
    await stop_event.wait()


async def stop():
    """Bot'u durdur"""
    if _app:
        await _app.updater.stop()
        await _app.stop()
        await _app.shutdown()


if __name__ == "__main__":
    logging.basicConfig(
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s',
        level=logging.INFO
    )
    logging.getLogger("httpx").setLevel(logging.WARNING)
    db.init_database()
    db.seed_bank_providers(Config.BANK_PROVIDERS)
    asyncio.run(start())
