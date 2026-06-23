"""
Operatör Yönetim Botu — Money Transfer Turkey
Operatör kaydı, müşteri yönetimi, chat, istatistik
python-telegram-bot 20.6 (async)
"""

import asyncio
import html
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
from translations import t

logger = logging.getLogger(__name__)

# ═══════════════════════════════════════════════
# AKTİF CHAT HARİTASI
# ═══════════════════════════════════════════════
# {operator_id: {'customer_id': int, 'transaction_id': int}}
active_chats: dict = {}


def _lang_from_user(user) -> str:
    lang = (getattr(user, 'language_code', None) or 'tr')[:2].lower()
    return lang if lang in ('tr', 'en', 'ru', 'de') else 'tr'


def _is_owner(user_id: int) -> bool:
    return int(user_id) == int(Config.ADMIN_ID)


def _is_admin(operator: dict | None) -> bool:
    return bool(operator and operator.get('is_admin'))


def _can_manage_panel(user_id: int, operator: dict | None) -> bool:
    if _is_owner(user_id):
        return True
    return _is_admin(operator)


# ═══════════════════════════════════════════════
# YARDIMCI
# ═══════════════════════════════════════════════

def _main_menu_kb(
    show_owner_panel: bool = False,
    show_admin_panel: bool = False
) -> InlineKeyboardMarkup:
    kb = [
        [InlineKeyboardButton("📋 Bekleyen Müşteriler", callback_data="show_pending")],
        [InlineKeyboardButton("📊 İstatistiklerim", callback_data="my_stats")],
        [InlineKeyboardButton("🔄 Yenile", callback_data="refresh")],
    ]
    if show_owner_panel:
        kb.append([InlineKeyboardButton("👑 Owner Panel", callback_data="owner_panel")])
    if show_admin_panel:
        kb.append([InlineKeyboardButton("🛠 Admin Panel", callback_data="admin_panel")])
    return InlineKeyboardMarkup(kb)


async def _send_menu(
    bot_or_context,
    chat_id: int,
    text: str = "🏠 **ANA MENÜ**\nBir işlem seçin:",
    show_owner_panel: bool = False,
    show_admin_panel: bool = False
):
    """Ana menüyü gönder — context veya bot nesnesi kabul eder"""
    if hasattr(bot_or_context, 'bot'):
        await bot_or_context.bot.send_message(
            chat_id=chat_id, text=text,
            reply_markup=_main_menu_kb(
                show_owner_panel=show_owner_panel,
                show_admin_panel=show_admin_panel
            ),
            parse_mode="Markdown"
        )
    else:
        await bot_or_context.send_message(
            chat_id=chat_id, text=text,
            reply_markup=_main_menu_kb(
                show_owner_panel=show_owner_panel,
                show_admin_panel=show_admin_panel
            ),
            parse_mode="Markdown"
        )


async def _notify_owner_live(text: str):
    """Owner'a canlı görüşme aynalama mesajı gönder."""
    if not Config.ADMIN_ID:
        return
    try:
        op_bot = Bot(token=Config.OPERATOR_BOT_TOKEN)
        await op_bot.send_message(
            chat_id=Config.ADMIN_ID,
            text=text,
            parse_mode="Markdown"
        )
    except Exception as e:
        logger.error(f"Owner canlı bildirim hatası: {e}")


# ═══════════════════════════════════════════════
# /start — OPERATÖR KAYDI & GİRİŞ
# ═══════════════════════════════════════════════

async def cmd_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user = update.message.from_user
    user_id = user.id
    lang = _lang_from_user(user)

    operator = db.get_operator(user_id)
    is_owner = _is_owner(user_id)
    is_admin = _is_admin(operator)

    # ── Owner kullanıcısını operatör tablosuna düşürmeden panele al ──
    if is_owner and not operator:
        await update.message.reply_text(
            (
                f"👑 <b>{html.escape(user.first_name)}</b>\n\n"
                "Owner paneli aktif.\n"
                "👇 Menüden işlem seçin."
            ),
            reply_markup=_main_menu_kb(show_owner_panel=True, show_admin_panel=True),
            parse_mode="HTML"
        )
        return

    # ── Kayıtlı değil → otomatik kayıt (pasif) ──
    if not operator:
        db.register_operator(user)
        await update.message.reply_text(
            (
                f"{t('op_welcome', lang)}\n\n"
                f"ℹ️ Telegram ID: `{user_id}`"
            ),
            parse_mode="Markdown"
        )

        # Admin'e bildir
        try:
            await context.bot.send_message(
                chat_id=Config.ADMIN_ID,
                text=t(
                    'admin_new_operator',
                    'tr',
                    name=user.first_name,
                    oid=user_id,
                    username=(user.username or "yok")
                ),
                parse_mode="Markdown"
            )
        except Exception as e:
            logger.error(f"Admin bildirim hatası: {e}")
        return

    # ── Kayıtlı ama aktif değil ──
    if not operator.get('is_active') and not is_owner:
        await update.message.reply_text(
            (
                f"{t('op_not_active', lang)}\n\n"
                f"ℹ️ Telegram ID: `{user_id}`"
            ),
            parse_mode="Markdown"
        )
        return

    # ── Aktif operatör — ana menü ──
    await update.message.reply_text(
        (
            f"✅ <b>{html.escape(user.first_name)}</b>\n\n"
            f"📋 Operatör paneli aktif.\n"
            f"🔔 Yeni müşteri geldiğinde bildirim alırsınız.\n"
            f"👇 Menüden işlem seçin."
        ),
        reply_markup=_main_menu_kb(
            show_owner_panel=is_owner,
            show_admin_panel=(is_owner or is_admin)
        ),
        parse_mode="HTML"
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
    operator = db.get_operator(operator_id)
    await _send_menu(
        context,
        operator_id,
        show_owner_panel=_is_owner(operator_id),
        show_admin_panel=_can_manage_panel(operator_id, operator)
    )


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


async def cmd_owner_panel(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.message.from_user.id
    if not _is_owner(user_id):
        await update.message.reply_text("⛔ Bu panel sadece Owner içindir.")
        return
    await _show_owner_panel(update.message, user_id)


async def cmd_admin_panel(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.message.from_user.id
    operator = db.get_operator(user_id)
    if not _can_manage_panel(user_id, operator):
        await update.message.reply_text("⛔ Bu panel sadece Owner/Admin içindir.")
        return
    await _show_admin_panel(update.message, user_id)


async def cmd_approve(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.message.from_user.id
    operator = db.get_operator(user_id)
    if not _can_manage_panel(user_id, operator):
        await update.message.reply_text("⛔ Yetkiniz yok.")
        return
    if not context.args:
        await update.message.reply_text("Kullanım: /approve <operator_id>")
        return
    try:
        target_id = int(context.args[0])
    except ValueError:
        await update.message.reply_text("❌ Geçersiz operator_id.")
        return
    if _is_owner(target_id):
        await update.message.reply_text("⛔ Owner hesabı yönetilemez.")
        return
    ok = db.set_operator_access(target_id, is_active=True)
    if ok:
        await update.message.reply_text(f"✅ Operatör aktif edildi: `{target_id}`", parse_mode="Markdown")
        try:
            op_bot = Bot(token=Config.OPERATOR_BOT_TOKEN)
            await op_bot.send_message(
                chat_id=target_id,
                text="✅ Hesabınız Owner/Admin tarafından aktif edildi. /start ile devam edin.",
                parse_mode="Markdown"
            )
        except Exception as e:
            logger.error(f"Aktivasyon bildirimi hatası: {e}")
    else:
        await update.message.reply_text("❌ Operatör güncellenemedi.")


async def cmd_reject(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.message.from_user.id
    operator = db.get_operator(user_id)
    if not _can_manage_panel(user_id, operator):
        await update.message.reply_text("⛔ Yetkiniz yok.")
        return
    if not context.args:
        await update.message.reply_text("Kullanım: /reject <operator_id>")
        return
    try:
        target_id = int(context.args[0])
    except ValueError:
        await update.message.reply_text("❌ Geçersiz operator_id.")
        return
    if _is_owner(target_id):
        await update.message.reply_text("⛔ Owner hesabı yönetilemez.")
        return
    ok = db.set_operator_access(target_id, is_active=False)
    if ok:
        await update.message.reply_text(f"✅ Operatör pasife alındı: `{target_id}`", parse_mode="Markdown")
    else:
        await update.message.reply_text("❌ Operatör güncellenemedi.")


async def cmd_operators(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.message.from_user.id
    operator = db.get_operator(user_id)
    if not _can_manage_panel(user_id, operator):
        await update.message.reply_text("⛔ Yetkiniz yok.")
        return
    ops = db.get_all_operators(include_inactive=True, exclude_ids=[Config.ADMIN_ID])
    if not ops:
        await update.message.reply_text("ℹ️ Kayıtlı operatör bulunamadı.")
        return
    lines = ["🧾 **Operator Listesi**"]
    for op in ops[:50]:
        state = "🟢 aktif" if op.get('is_active') else "🟡 bekliyor"
        admin_badge = " 👑admin" if op.get('is_admin') else ""
        uname = op.get('username') or "-"
        lines.append(f"- `{op.get('operator_id')}` {state}{admin_badge} @{uname}")
    await update.message.reply_text("\n".join(lines), parse_mode="Markdown")


async def cmd_viewchat(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.message.from_user.id
    operator = db.get_operator(user_id)
    if not _can_manage_panel(user_id, operator):
        await update.message.reply_text("⛔ Yetkiniz yok.")
        return
    if not context.args:
        await update.message.reply_text("Kullanım: /viewchat <transaction_id>")
        return
    try:
        tid = int(context.args[0])
    except ValueError:
        await update.message.reply_text("❌ Geçersiz transaction_id.")
        return
    await _send_chat_transcript(update.message, tid)


# ═══════════════════════════════════════════════
# CALLBACK (BUTON) İŞLEYİCİ
# ═══════════════════════════════════════════════

async def handle_callback(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    operator_id = query.from_user.id
    data = query.data

    # Panel yetkisi kontrol (Owner/Admin bypass)
    operator = db.get_operator(operator_id)
    is_owner = _is_owner(operator_id)
    is_admin = _is_admin(operator)
    can_manage = _can_manage_panel(operator_id, operator)
    is_active_operator = bool(operator and operator.get('is_active'))

    if not (is_owner or is_active_operator):
        await query.answer("⚠️ Operatör yetkiniz yok!", show_alert=True)
        return

    # ── Ana Menü ──
    if data == "main_menu" or data == "refresh":
        await _send_menu(
            context,
            operator_id,
            show_owner_panel=is_owner,
            show_admin_panel=(is_owner or is_admin)
        )
        return

    # ── Owner Panel ──
    if data == "owner_panel":
        if not is_owner:
            await query.answer("⛔ Yetkiniz yok.", show_alert=True)
            return
        await _show_owner_panel(query.message, operator_id)
        return

    if data.startswith("owner_view_"):
        if not is_owner:
            await query.answer("⛔ Yetkiniz yok.", show_alert=True)
            return
        tid = int(data.split("_")[2])
        await _send_chat_transcript(query.message, tid)
        return

    # ── Admin Panel ──
    if data == "admin_panel":
        if not can_manage:
            await query.answer("⛔ Yetkiniz yok.", show_alert=True)
            return
        await _show_admin_panel(query.message, operator_id)
        return

    if data.startswith("adm_activate_") or data.startswith("adm_deactivate_"):
        if not can_manage:
            await query.answer("⛔ Yetkiniz yok.", show_alert=True)
            return
        target_id = int(data.split("_")[2])
        if _is_owner(target_id):
            await query.answer("⛔ Owner hesabı yönetilemez.", show_alert=True)
            return
        activate = data.startswith("adm_activate_")
        ok = db.set_operator_access(target_id, is_active=activate)
        if ok:
            await query.message.reply_text(
                f"{'✅ Aktif edildi' if activate else '❌ Pasife alındı'}: `{target_id}`",
                parse_mode="Markdown"
            )
        else:
            await query.message.reply_text("⚠️ Operatör güncellenemedi.")
        return

    # ── Bekleyen Müşteriler ──
    if data == "show_pending":
        if not is_active_operator:
            await query.answer("Aktif operatör değilsiniz.", show_alert=True)
            return
        await _show_pending(query, context)
        return

    # ── İstatistiklerim ──
    if data == "my_stats":
        if not is_active_operator:
            await query.answer("Aktif operatör değilsiniz.", show_alert=True)
            return
        await _show_stats(query, operator_id)
        return

    # ── Müşteri Al ──
    if data.startswith("take_"):
        if not is_active_operator:
            await query.answer("Aktif operatör değilsiniz.", show_alert=True)
            return
        parts = data.split("_")
        if len(parts) >= 3:
            tid = int(parts[1])
            cid = int(parts[2])
            await _take_customer(query, context, operator_id, tid, cid)
        return

    # ── İşlemi Tamamla ──
    if data.startswith("complete_"):
        if not is_active_operator:
            await query.answer("Aktif operatör değilsiniz.", show_alert=True)
            return
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
        op = db.get_operator(query.from_user.id)
        await _send_menu(
            context,
            query.from_user.id,
            show_owner_panel=_is_owner(query.from_user.id),
            show_admin_panel=_can_manage_panel(query.from_user.id, op)
        )
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


async def _show_owner_panel(message, user_id: int):
    convs = db.get_owner_conversations(limit=15, only_active=False)
    all_ops = db.get_all_operators(include_inactive=True, exclude_ids=[Config.ADMIN_ID])
    active_ops = [o for o in all_ops if o.get('is_active')]
    pending_ops = [o for o in all_ops if not o.get('is_active')]
    if not convs:
        await message.reply_text(
            f"👑 <b>OWNER PANEL</b>\n\n"
            f"🟢 Aktif operatör: {len(active_ops)}\n"
            f"🟡 Onay bekleyen: {len(pending_ops)}\n\n"
            f"ℹ️ Henüz operatöre atanmış müşteri görüşmesi yok.\n"
            f"Müşteriler işlem başlattığında burada görünecek.",
            parse_mode="HTML"
        )
        await _send_menu(
            Bot(token=Config.OPERATOR_BOT_TOKEN),
            user_id,
            text="👑 Owner menü",
            show_owner_panel=True,
            show_admin_panel=True
        )
        return
    await message.reply_text("👑 <b>OWNER PANEL</b>\nOperatöre atanmış son görüşmeler:", parse_mode="HTML")
    for item in convs:
        tid = item.get('transaction_id')
        status = item.get('status') or "-"
        cname = item.get('customer_name') or "Müşteri"
        oname = item.get('operator_name') or "Atanmamış"
        count = item.get('message_count') or 0
        kb = [[InlineKeyboardButton("👁 Görüşmeyi Aç", callback_data=f"owner_view_{tid}")]]
        await message.reply_text(
            (
                f"🔖 **#{tid}** | `{status}`\n"
                f"👤 Müşteri: {cname}\n"
                f"🧑‍💼 Operatör: {oname}\n"
                f"💬 Mesaj: {count}"
            ),
            reply_markup=InlineKeyboardMarkup(kb),
            parse_mode="Markdown"
        )
    await _send_menu(
        Bot(token=Config.OPERATOR_BOT_TOKEN),
        user_id,
        text="👑 Owner menü",
        show_owner_panel=True,
        show_admin_panel=True
    )


async def _show_admin_panel(message, user_id: int):
    operators = db.get_all_operators(include_inactive=True, exclude_ids=[Config.ADMIN_ID])
    pending = [x for x in operators if not x.get('is_active')]
    active = [x for x in operators if x.get('is_active')]
    await message.reply_text(
        (
            "🛠 **ADMIN PANEL**\n\n"
            f"🟢 Aktif operatör: {len(active)}\n"
            f"🟡 Onay bekleyen: {len(pending)}\n\n"
            "Komutlar:\n"
            "`/operators`\n"
            "`/approve <operator_id>`\n"
            "`/reject <operator_id>`"
        ),
        parse_mode="Markdown"
    )

    for op in pending[:15]:
        oid = op.get('operator_id')
        uname = op.get('username') or "-"
        kb = [[
            InlineKeyboardButton("✅ Onayla", callback_data=f"adm_activate_{oid}"),
            InlineKeyboardButton("❌ Reddet", callback_data=f"adm_deactivate_{oid}")
        ]]
        await message.reply_text(
            f"⏳ Bekleyen: `{oid}` @{uname}",
            reply_markup=InlineKeyboardMarkup(kb),
            parse_mode="Markdown"
        )


def _format_sender(msg: dict) -> str:
    stype = (msg.get('sender_type') or "").lower()
    if stype == 'customer':
        return f"👤 {msg.get('customer_name') or 'Müşteri'}"
    if stype == 'operator':
        return f"🧑‍💼 {msg.get('operator_name') or 'Operatör'}"
    if stype == 'bank_provider':
        return "🏦 Bank Provider"
    if stype == 'bot':
        return "🤖 Bot"
    return f"ℹ️ {stype or 'system'}"


async def _send_chat_transcript(message, transaction_id: int):
    msgs = db.get_transaction_messages(transaction_id, limit=200)
    if not msgs:
        await message.reply_text(f"ℹ️ #{transaction_id} için mesaj bulunamadı.")
        return

    header = f"📚 **GÖRÜŞME DÖKÜMÜ** #{transaction_id}\n"
    lines = [header]
    for m in msgs:
        who = _format_sender(m)
        text = (m.get('message_text') or '').strip()
        if not text and m.get('file_type'):
            text = f"[{m.get('file_type')}]"
        if not text:
            text = "[boş]"
        lines.append(f"{who}: {text}")

    payload = "\n".join(lines)
    # Telegram mesaj limiti için parçalayıp gönder.
    chunk_size = 3500
    for i in range(0, len(payload), chunk_size):
        await message.reply_text(payload[i:i + chunk_size], parse_mode="Markdown")


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
    operator = db.get_operator(operator_id)
    await _send_menu(
        context,
        operator_id,
        show_owner_panel=_is_owner(operator_id),
        show_admin_panel=_can_manage_panel(operator_id, operator)
    )


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
    if operator_id != Config.ADMIN_ID:
        await _notify_owner_live(
            (
                f"👑 **CANLI GÖRÜŞME / Operatör**\n"
                f"🔖 İşlem: #{tid}\n"
                f"🧑‍💼 Operator ID: `{operator_id}`\n"
                f"👤 Customer ID: `{cid}`\n\n"
                f"{text}"
            )
        )

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
            if operator_id != Config.ADMIN_ID:
                await _notify_owner_live(
                    (
                        f"👑 **CANLI GÖRÜŞME / Operatör Dosya**\n"
                        f"🔖 İşlem: #{tid}\n"
                        f"🧑‍💼 Operator ID: `{operator_id}`\n"
                        f"👤 Customer ID: `{cid}`\n"
                        f"📎 Tür: photo"
                    )
                )
        elif update.message.document:
            doc = update.message.document
            await main_bot.send_document(
                chat_id=cid, document=doc.file_id,
                caption="🧑‍💼 Operatörden dosya:"
            )
            db.save_message(tid, operator_id, 'operator', f'[DOCUMENT] {doc.file_name}')
            if operator_id != Config.ADMIN_ID:
                await _notify_owner_live(
                    (
                        f"👑 **CANLI GÖRÜŞME / Operatör Dosya**\n"
                        f"🔖 İşlem: #{tid}\n"
                        f"🧑‍💼 Operator ID: `{operator_id}`\n"
                        f"👤 Customer ID: `{cid}`\n"
                        f"📎 Tür: document ({doc.file_name})"
                    )
                )

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
        await _notify_owner_live(
            (
                f"👑 **CANLI GÖRÜŞME / Müşteri**\n"
                f"🔖 İşlem: #{transaction_id or '-'}\n"
                f"👤 Customer ID: `{customer_id}`\n"
                f"🧑‍💼 Operator ID: `{target_op}`\n\n"
                f"{text}"
            )
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
        await _notify_owner_live(
            (
                f"👑 **CANLI GÖRÜŞME / Müşteri Dosya**\n"
                f"🔖 İşlem: #-\n"
                f"👤 Customer ID: `{customer_id}`\n"
                f"🧑‍💼 Operator ID: `{target_op}`\n"
                f"📎 Tür: {file_type}"
            )
        )
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
    app.add_handler(CommandHandler("ownerpanel", cmd_owner_panel))
    app.add_handler(CommandHandler("adminpanel", cmd_admin_panel))
    app.add_handler(CommandHandler("approve", cmd_approve))
    app.add_handler(CommandHandler("reject", cmd_reject))
    app.add_handler(CommandHandler("operators", cmd_operators))
    app.add_handler(CommandHandler("viewchat", cmd_viewchat))

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
