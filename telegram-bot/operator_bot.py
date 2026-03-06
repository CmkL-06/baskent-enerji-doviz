"""
Operatör Yönetim Botu — Money Transfer Turkey
Operatör kaydı, müşteri yönetimi, chat, istatistik
python-telegram-bot 20.6 (async)
"""

import asyncio
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
import database as db

logger = logging.getLogger(__name__)

# ═══════════════════════════════════════════════
# AKTİF CHAT HARİTASI
# ═══════════════════════════════════════════════
# {operator_id: {'customer_id': int, 'transaction_id': int}}
active_chats: dict = {}


# ═══════════════════════════════════════════════
# YARDIMCI
# ═══════════════════════════════════════════════

def _main_menu_kb() -> InlineKeyboardMarkup:
    kb = [
        [InlineKeyboardButton("📋 Bekleyen Müşteriler", callback_data="show_pending")],
        [InlineKeyboardButton("📊 İstatistiklerim", callback_data="my_stats")],
        [InlineKeyboardButton("🔄 Yenile", callback_data="refresh")],
    ]
    return InlineKeyboardMarkup(kb)


async def _send_menu(bot_or_context, chat_id: int, text: str = "🏠 **ANA MENÜ**\nBir işlem seçin:"):
    """Ana menüyü gönder — context veya bot nesnesi kabul eder"""
    if hasattr(bot_or_context, 'bot'):
        await bot_or_context.bot.send_message(
            chat_id=chat_id, text=text,
            reply_markup=_main_menu_kb(), parse_mode="Markdown"
        )
    else:
        await bot_or_context.send_message(
            chat_id=chat_id, text=text,
            reply_markup=_main_menu_kb(), parse_mode="Markdown"
        )


# ═══════════════════════════════════════════════
# /start — OPERATÖR KAYDI & GİRİŞ
# ═══════════════════════════════════════════════

async def cmd_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user = update.message.from_user
    user_id = user.id

    operator = db.get_operator(user_id)

    # ── Kayıtlı değil → otomatik kayıt (pasif) ──
    if not operator:
        db.register_operator(user)
        await update.message.reply_text(
            f"👋 Merhaba {user.first_name}!\n\n"
            f"📝 Sisteme kaydınız alındı.\n"
            f"⏳ Admin onayı bekleniyor...\n\n"
            f"Admin size operatör yetkisi verdikten sonra bu botu kullanabileceksiniz.\n\n"
            f"ℹ️ Telegram ID'niz: `{user_id}`\n"
            f"(Admin'e bu ID'yi verin)",
            parse_mode="Markdown"
        )

        # Admin'e bildir
        try:
            await context.bot.send_message(
                chat_id=Config.ADMIN_ID,
                text=(
                    f"🆕 **YENİ OPERATÖR TALEBİ**\n\n"
                    f"👤 İsim: {user.first_name}\n"
                    f"🆔 ID: `{user_id}`\n"
                    f"👤 Username: @{user.username if user.username else 'yok'}\n\n"
                    f"Admin panelden bu kişiyi operatör olarak ekleyebilirsiniz."
                ),
                parse_mode="Markdown"
            )
        except Exception as e:
            logger.error(f"Admin bildirim hatası: {e}")
        return

    # ── Kayıtlı ama aktif değil ──
    if not operator.get('is_active'):
        await update.message.reply_text(
            f"⏳ Merhaba {user.first_name}!\n\n"
            f"Kaydınız sistemde var ancak henüz aktif edilmemiş.\n"
            f"Admin onayını bekleyin.\n\n"
            f"ℹ️ Telegram ID'niz: `{user_id}`",
            parse_mode="Markdown"
        )
        return

    # ── Aktif operatör — ana menü ──
    await update.message.reply_text(
        f"✅ **HOŞ GELDİN OPERATÖR!**\n\n"
        f"👤 {user.first_name}\n"
        f"📋 Artık müşteri bildirimleri alacaksınız.\n\n"
        f"🔔 Yeni müşteri geldiğinde size bildirim gelecek.\n"
        f"👇 Aşağıdaki butonları kullanın:",
        reply_markup=_main_menu_kb(),
        parse_mode="Markdown"
    )


# ═══════════════════════════════════════════════
# /end — SOHBET SONLANDIRMA
# ═══════════════════════════════════════════════

async def cmd_end(update: Update, context: ContextTypes.DEFAULT_TYPE):
    operator_id = update.message.from_user.id

    if operator_id not in active_chats:
        await update.message.reply_text("⚠️ Aktif sohbetiniz yok.")
        return

    chat_data = active_chats.pop(operator_id)
    tid = chat_data['transaction_id']
    cid = chat_data['customer_id']

    # Müşteriye bildir
    try:
        main_bot = Bot(token=Config.MAIN_BOT_TOKEN)
        await main_bot.send_message(
            chat_id=cid,
            text="📢 Operatör sohbeti sonlandırdı.\n"
                 "İşleminiz devam ediyor.",
            parse_mode="Markdown"
        )
    except Exception as e:
        logger.error(f"Müşteriye sohbet sonu bildirimi hatası: {e}")

    await update.message.reply_text(
        "✅ Sohbet sonlandırıldı.\n\n"
        "📋 Bekleyen Müşteriler butonundan yeni müşteri alabilirsiniz."
    )
    await _send_menu(context, operator_id)


# ═══════════════════════════════════════════════
# /myid — TELEGRAM ID ÖĞRENME
# ═══════════════════════════════════════════════

async def cmd_myid(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.message.from_user.id
    await update.message.reply_text(
        f"📱 **Sizin Telegram ID'niz:**\n"
        f"`{user_id}`\n\n"
        f"Bu ID'yi yöneticinize verin.",
        parse_mode="Markdown"
    )


# ═══════════════════════════════════════════════
# CALLBACK (BUTON) İŞLEYİCİ
# ═══════════════════════════════════════════════

async def handle_callback(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    operator_id = query.from_user.id
    data = query.data

    # Operatör yetkisi kontrol
    operator = db.get_operator(operator_id)
    if not operator or not operator.get('is_active'):
        await query.answer("⚠️ Operatör yetkiniz yok!", show_alert=True)
        return

    # ── Ana Menü ──
    if data == "main_menu" or data == "refresh":
        await _send_menu(context, operator_id)
        return

    # ── Bekleyen Müşteriler ──
    if data == "show_pending":
        await _show_pending(query, context)
        return

    # ── İstatistiklerim ──
    if data == "my_stats":
        await _show_stats(query, operator_id)
        return

    # ── Müşteri Al ──
    if data.startswith("take_"):
        parts = data.split("_")
        if len(parts) >= 3:
            tid = int(parts[1])
            cid = int(parts[2])
            await _take_customer(query, context, operator_id, tid, cid)
        return

    # ── İşlemi Tamamla ──
    if data.startswith("complete_"):
        tid = int(data.split("_")[1])
        await _complete_transaction(query, context, operator_id, tid)
        return


# ═══════════════════════════════════════════════
# BEKLEYEN MÜŞTERİLER
# ═══════════════════════════════════════════════

async def _show_pending(query, context):
    pending = db.get_pending_transactions()

    if not pending:
        await query.message.reply_text(
            "📭 **Bekleyen müşteri yok.**\n\n"
            "Yeni müşteri geldiğinde size bildirim gelecek.",
            parse_mode="Markdown"
        )
        await _send_menu(context, query.from_user.id)
        return

    await query.message.reply_text(
        f"📋 **BEKLEYEN MÜŞTERİLER ({len(pending)} kişi)**",
        parse_mode="Markdown"
    )

    for trans in pending[:10]:
        tid = trans['transaction_id']
        cid = trans['customer_id']
        cust_name = trans['first_name']
        currency = trans['currency']
        amount = trans['amount']
        created = trans['created_at']
        created_str = created.strftime('%H:%M') if isinstance(created, datetime) else str(created)

        kb = [[InlineKeyboardButton(
            "📌 Bu Müşteriyi Al",
            callback_data=f"take_{tid}_{cid}"
        )]]

        await query.message.reply_text(
            f"👤 **{cust_name}**\n"
            f"💰 {amount} {currency}\n"
            f"🕐 {created_str}\n"
            f"🆔 İşlem ID: #{tid}",
            reply_markup=InlineKeyboardMarkup(kb),
            parse_mode="Markdown"
        )

    # Ana menüye dön
    kb_back = [[InlineKeyboardButton("🏠 Ana Menüye Dön", callback_data="main_menu")]]
    await query.message.reply_text(
        "📱 Web panelden de müşterileri alabilirsiniz\n"
        f"🌐 {Config.WEB_PANEL_URL}",
        reply_markup=InlineKeyboardMarkup(kb_back)
    )


# ═══════════════════════════════════════════════
# İSTATİSTİKLER
# ═══════════════════════════════════════════════

async def _show_stats(query, operator_id: int):
    stats = db.get_operator_stats(operator_id)

    kb = [[InlineKeyboardButton("🏠 Ana Menüye Dön", callback_data="main_menu")]]

    await query.message.reply_text(
        f"📊 **İSTATİSTİKLERİNİZ**\n\n"
        f"📅 Bugün: {stats['today']} işlem\n"
        f"📈 Toplam: {stats['total']} işlem",
        reply_markup=InlineKeyboardMarkup(kb),
        parse_mode="Markdown"
    )


# ═══════════════════════════════════════════════
# MÜŞTERİ ALMA
# ═══════════════════════════════════════════════

async def _take_customer(query, context, operator_id: int, tid: int, cid: int):
    # Zaten aktif chat var mı kontrol
    if operator_id in active_chats:
        existing = active_chats[operator_id]
        await query.answer(
            f"⚠️ Zaten aktif müşteriniz var (İşlem #{existing['transaction_id']}). "
            f"Önce /end ile mevcut sohbeti kapatın.",
            show_alert=True
        )
        return

    # İşlemi operatöre ata
    db.update_transaction(tid, assigned_operator_id=operator_id, status='in_progress')

    # Active chat'e ekle
    active_chats[operator_id] = {
        'customer_id': cid,
        'transaction_id': tid,
    }

    # Müşteri bilgisini al
    trans = db.get_transaction(tid)
    cust_name = trans.get('first_name', 'Müşteri') if trans else 'Müşteri'
    currency = trans.get('currency', '?') if trans else '?'
    amount = trans.get('amount', '?') if trans else '?'

    # İşlem tamamlama butonu
    kb = [[InlineKeyboardButton("✅ İşlemi Tamamla", callback_data=f"complete_{tid}")]]

    await query.edit_message_text(
        f"✅ **Müşteri Size Atandı!**\n\n"
        f"👤 Müşteri: {cust_name}\n"
        f"🆔 Müşteri ID: `{cid}`\n"
        f"💰 İşlem: {amount} {currency}\n"
        f"🔖 İşlem ID: #{tid}\n\n"
        f"📝 **NASIL ÇALIŞIR:**\n"
        f"1️⃣ Bu pencerede yazdığınız mesajlar müşteriye iletilir\n"
        f"2️⃣ Müşterinin yazdıkları size iletilir\n"
        f"3️⃣ İşlem bitince '✅ İşlemi Tamamla' butonuna basın\n"
        f"4️⃣ Veya /end yazarak sohbeti kapatın\n\n"
        f"⚡ Şimdi bir mesaj yazarak müşteriye ulaşabilirsiniz.",
        reply_markup=InlineKeyboardMarkup(kb),
        parse_mode="Markdown"
    )

    # Müşteriye bildir
    try:
        main_bot = Bot(token=Config.MAIN_BOT_TOKEN)
        await main_bot.send_message(
            chat_id=cid,
            text="🧑‍💼 **Bir operatör size bağlandı!**\n\n"
                 "Artık mesajlarınız doğrudan operatöre iletilecek.\n"
                 "Lütfen bekleyin veya sorunuzu yazın.",
            parse_mode="Markdown"
        )
    except Exception as e:
        logger.error(f"Müşteriye operatör bağlantı bildirimi hatası: {e}")


# ═══════════════════════════════════════════════
# İŞLEM TAMAMLAMA
# ═══════════════════════════════════════════════

async def _complete_transaction(query, context, operator_id: int, tid: int):
    # İşlemi tamamla
    db.complete_transaction(tid)

    # Dealer bakiyesini düş
    trans = db.get_transaction(tid)
    if trans:
        amount_try = trans.get('try_amount') or trans.get('amount_try')
        referral_code = trans.get('referral_code')
        if amount_try and referral_code:
            db.reduce_dealer_balance(referral_code, float(amount_try))
            logger.info(f"Dealer {referral_code} bakiye düşürüldü: {amount_try} TRY (İşlem #{tid})")

        cid = trans.get('customer_id')
        cust_name = trans.get('first_name', 'Müşteri')
        amount = trans.get('amount', '?')
        currency = trans.get('currency', '?')
        completion_code = trans.get('completion_code', '---')

        # Operatöre onay mesajı
        await query.edit_message_text(
            f"✅ **İşlem Tamamlandı!**\n\n"
            f"👤 Müşteri: {cust_name}\n"
            f"💰 Tutar: {amount} {currency}\n"
            f"🔑 Tamamlama Kodu: `{completion_code}`\n"
            f"🕐 Tamamlanma: {datetime.now().strftime('%H:%M')}",
            parse_mode="Markdown"
        )

        # Müşteriye tamamlanma mesajı
        if cid:
            try:
                main_bot = Bot(token=Config.MAIN_BOT_TOKEN)
                await main_bot.send_message(
                    chat_id=cid,
                    text=(
                        f"🎉 **İŞLEMİNİZ TAMAMLANDI!**\n\n"
                        f"✅ Transfer işleminiz başarıyla tamamlandı.\n"
                        f"💰 Tutar: {amount} {currency}\n"
                        f"🔑 Tamamlama Kodu: `{completion_code}`\n\n"
                        f"Bizi tercih ettiğiniz için teşekkürler! 🙏"
                    ),
                    parse_mode="Markdown"
                )
            except Exception as e:
                logger.error(f"Müşteriye tamamlama bildirimi hatası: {e}")

    # Aktif chat'i temizle
    active_chats.pop(operator_id, None)

    # Ana menüyü göster
    await _send_menu(context, operator_id)


# ═══════════════════════════════════════════════
# MESAJ İLETME — OPERATÖR → MÜŞTERİ
# ═══════════════════════════════════════════════

async def handle_message(update: Update, context: ContextTypes.DEFAULT_TYPE):
    operator_id = update.message.from_user.id
    text = update.message.text

    # Operatör yetkisi kontrol
    operator = db.get_operator(operator_id)
    if not operator or not operator.get('is_active'):
        await update.message.reply_text(
            "⚠️ Operatör yetkiniz yok.\n/start ile kayıt olun."
        )
        return

    # Aktif chat var mı?
    if operator_id not in active_chats:
        await update.message.reply_text(
            "⚠️ **Aktif müşteriniz yok!**\n\n"
            "Önce bir müşteri almanız gerekiyor.",
            reply_markup=_main_menu_kb(),
            parse_mode="Markdown"
        )
        return

    chat_data = active_chats[operator_id]
    cid = chat_data['customer_id']
    tid = chat_data['transaction_id']

    # Mesajı DB'ye kaydet
    db.save_message(tid, operator_id, 'operator', text)

    # Ana bot üzerinden müşteriye gönder
    try:
        main_bot = Bot(token=Config.MAIN_BOT_TOKEN)
        await main_bot.send_message(
            chat_id=cid,
            text=f"🧑‍💼 **Operatör:**\n\n{text}",
            parse_mode="Markdown"
        )
        await update.message.reply_text("✅ Mesaj müşteriye iletildi.")
    except Exception as e:
        logger.error(f"Müşteriye mesaj iletme hatası: {e}")
        await update.message.reply_text(f"❌ Mesaj gönderilemedi!\nHata: {e}")


# ═══════════════════════════════════════════════
# DOSYA İLETME — OPERATÖR → MÜŞTERİ
# ═══════════════════════════════════════════════

async def handle_file(update: Update, context: ContextTypes.DEFAULT_TYPE):
    operator_id = update.message.from_user.id

    if operator_id not in active_chats:
        await update.message.reply_text(
            "⚠️ Aktif müşteriniz yok.",
            reply_markup=_main_menu_kb()
        )
        return

    chat_data = active_chats[operator_id]
    cid = chat_data['customer_id']
    tid = chat_data['transaction_id']

    try:
        main_bot = Bot(token=Config.MAIN_BOT_TOKEN)

        if update.message.photo:
            photo = update.message.photo[-1]
            await main_bot.send_photo(
                chat_id=cid, photo=photo.file_id,
                caption="🧑‍💼 Operatörden dosya:"
            )
            db.save_message(tid, operator_id, 'operator', '[PHOTO]')
        elif update.message.document:
            doc = update.message.document
            await main_bot.send_document(
                chat_id=cid, document=doc.file_id,
                caption="🧑‍💼 Operatörden dosya:"
            )
            db.save_message(tid, operator_id, 'operator', f'[DOCUMENT] {doc.file_name}')

        await update.message.reply_text("✅ Dosya müşteriye iletildi.")
    except Exception as e:
        logger.error(f"Dosya iletme hatası: {e}")
        await update.message.reply_text(f"❌ Dosya gönderilemedi!\nHata: {e}")


# ═══════════════════════════════════════════════
# MÜŞTERİDEN GELEN MESAJI OPERATÖRE İLET
# (Main bot tarafından çağrılır)
# ═══════════════════════════════════════════════

async def forward_to_operator(customer_id: int, text: str, transaction_id: int = None):
    """Main bot çağırır — müşteriden gelen mesajı ilgili operatöre iletir"""
    # active_chats'te bu müşteriyi bulan operatörü bul
    target_op = None
    for op_id, chat_data in active_chats.items():
        if chat_data['customer_id'] == customer_id:
            target_op = op_id
            break

    if not target_op:
        return False

    try:
        op_bot = Bot(token=Config.OPERATOR_BOT_TOKEN)
        await op_bot.send_message(
            chat_id=target_op,
            text=f"💬 **Müşteri yazdı:**\n\n{text}",
            parse_mode="Markdown"
        )
        return True
    except Exception as e:
        logger.error(f"Operatöre mesaj iletme hatası: {e}")
        return False


async def forward_file_to_operator(customer_id: int, file_id: str, file_type: str,
                                    caption: str = None):
    """Main bot çağırır — müşteriden gelen dosyayı operatöre iletir"""
    target_op = None
    for op_id, chat_data in active_chats.items():
        if chat_data['customer_id'] == customer_id:
            target_op = op_id
            break

    if not target_op:
        return False

    try:
        op_bot = Bot(token=Config.OPERATOR_BOT_TOKEN)
        cap = caption or "💬 Müşteriden dosya:"

        if file_type == 'photo':
            await op_bot.send_photo(chat_id=target_op, photo=file_id, caption=cap)
        else:
            await op_bot.send_document(chat_id=target_op, document=file_id, caption=cap)
        return True
    except Exception as e:
        logger.error(f"Operatöre dosya iletme hatası: {e}")
        return False


def get_operator_for_customer(customer_id: int) -> int | None:
    """Bir müşteriye atanmış operatörü bul (aktif chat'lerden)"""
    for op_id, chat_data in active_chats.items():
        if chat_data['customer_id'] == customer_id:
            return op_id
    return None


# ═══════════════════════════════════════════════
# BOT BAŞLATMA
# ═══════════════════════════════════════════════

_app: Application = None


def create_app() -> Application:
    global _app
    app = Application.builder().token(Config.OPERATOR_BOT_TOKEN).build()

    # Komutlar
    app.add_handler(CommandHandler("start", cmd_start))
    app.add_handler(CommandHandler("end", cmd_end))
    app.add_handler(CommandHandler("myid", cmd_myid))

    # Dosya/fotoğraf (özel mesaj)
    app.add_handler(MessageHandler(
        (filters.PHOTO | filters.Document.ALL) & filters.ChatType.PRIVATE,
        handle_file
    ))

    # Metin mesajları (özel mesaj)
    app.add_handler(MessageHandler(
        filters.TEXT & filters.ChatType.PRIVATE & ~filters.COMMAND,
        handle_message
    ))

    # Buton callback'leri
    app.add_handler(CallbackQueryHandler(handle_callback))

    _app = app
    return app


async def start():
    """Bot'u başlat (run_all.py'den çağrılır)"""
    app = create_app()
    await app.initialize()
    await app.start()
    await app.updater.start_polling(drop_pending_updates=True)
    logger.info("Operatör botu başlatıldı")

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
    db.init_database()
    asyncio.run(start())
