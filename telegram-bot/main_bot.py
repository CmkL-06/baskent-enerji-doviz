"""
Ana Müşteri Botu — Money Transfer Turkey
QR → Para Birimi → Miktar → USDT/Ruble Akışı → Tamamlama Kodu
python-telegram-bot 20.6 (async)
"""

import asyncio
import os
import re
import uuid
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
from exchange_rates import get_rates
from crypto_exchanges import BinanceAPI
from baskent_api import send_exchange_for_transaction, process_queue as baskent_queue_processor

logger = logging.getLogger(__name__)

# ═══════════════════════════════════════════════
# STATE YÖNETİMİ — Memory cache + DB persistence
# ═══════════════════════════════════════════════

_state_cache: dict = {}  # {customer_id: {'state': '...', 'data': {...}, 'transaction_id': N}}


def get_state(customer_id: int) -> dict | None:
    if customer_id in _state_cache:
        return _state_cache[customer_id]
    db_state = db.get_user_state(customer_id)
    if db_state:
        _state_cache[customer_id] = db_state
    return db_state


def set_state(customer_id: int, state: str, transaction_id: int = None, **data):
    obj = {'state': state, 'data': data}
    if transaction_id:
        obj['transaction_id'] = transaction_id
    elif customer_id in _state_cache and 'transaction_id' in _state_cache[customer_id]:
        obj['transaction_id'] = _state_cache[customer_id]['transaction_id']
    _state_cache[customer_id] = obj
    db.set_user_state(customer_id, state, data)


def clear_state(customer_id: int):
    _state_cache.pop(customer_id, None)


def get_user_state_name(customer_id: int) -> str:
    s = get_state(customer_id)
    return s['state'] if s else 'idle'


def recover_active_sessions():
    active = db.get_all_active_states()
    for cid, sdata in active.items():
        _state_cache[cid] = sdata
    logger.info(f"{len(active)} aktif oturum kurtarıldı")


# ═══════════════════════════════════════════════
# YARDIMCI FONKSİYONLAR
# ═══════════════════════════════════════════════

def get_lang(user) -> str:
    lang = getattr(user, 'language_code', 'tr') or 'tr'
    lang = lang[:2].lower()
    return lang if lang in ('tr', 'en', 'ru', 'de') else 'tr'


def get_lang_by_id(customer_id: int) -> str:
    return db.get_customer_language(customer_id)


async def check_group_membership(context: ContextTypes.DEFAULT_TYPE, user_id: int) -> bool:
    if Config.SKIP_GROUP_CHECK:
        return True
    try:
        member = await context.bot.get_chat_member(
            chat_id=Config.REQUIRED_GROUP_ID, user_id=user_id
        )
        return member.status in ('member', 'administrator', 'creator')
    except Exception as e:
        err = str(e).lower()
        if 'chat not found' in err:
            logger.warning(f"Bot gruba admin olarak eklenmeli! Grup ID: {Config.REQUIRED_GROUP_ID}")
        return False


def generate_completion_code() -> str:
    return str(random.randint(10000, 99999))


def progress_bar(current: int, total: int = 10) -> str:
    filled = min(int(current), total)
    return '🟩' * filled + '⬜' * (total - filled)


# ═══════════════════════════════════════════════
# /start KOMUTU
# ═══════════════════════════════════════════════

async def cmd_start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user = update.message.from_user
    user_id = user.id
    lang = get_lang(user)

    # QR kodu (referral) kontrolü
    if not context.args:
        await update.message.reply_text(t('qr_required', lang), parse_mode="Markdown")
        return

    param = context.args[0]

    # Bayi doğrulama
    dealer = db.get_dealer(param)
    if not dealer:
        await update.message.reply_text(t('invalid_qr', lang), parse_mode="Markdown")
        return
    if not dealer.get('is_active'):
        await update.message.reply_text(t('dealer_inactive', lang), parse_mode="Markdown")
        return

    # Müşteriyi kaydet
    db.upsert_customer(user)

    # Bekleyen USDT işlemi var mı?
    pending = db.get_pending_usdt(user_id)
    if pending:
        _restore_pending_usdt(context, pending)
        set_state(user_id, 'waiting_txid', pending['transaction_id'])
        await update.message.reply_text(
            t('pending_usdt_transaction', lang,
              amount=pending['amount'],
              code=pending.get('completion_code', '---'),
              network=pending.get('crypto_network', 'TRC20'),
              address=pending.get('crypto_address', ''),
              amount2=pending['amount']),
            parse_mode="Markdown"
        )
        return

    # Grup üyeliği kontrolü
    is_member = await check_group_membership(context, user_id)
    if not is_member:
        context.user_data['referral'] = param
        context.user_data['dealer_name'] = dealer.get('dealer_name')
        kb = [
            [InlineKeyboardButton(t('join_group', lang),
                                   url="https://t.me/MoneyTransferTurkeyOfficial")],
            [InlineKeyboardButton("✅ Katıldım, Devam Et", callback_data="check_joined")]
        ]
        await update.message.reply_text(
            t('group_check_failed', lang),
            reply_markup=InlineKeyboardMarkup(kb),
            parse_mode="Markdown"
        )
        return

    # Referral'ı sakla
    context.user_data['referral'] = param
    context.user_data['dealer_name'] = dealer.get('dealer_name')

    # Para birimi seçimi
    await _show_currency_selection(update.message, lang)
    set_state(user_id, 'selecting_currency')


def _restore_pending_usdt(context, pending: dict):
    context.user_data['transaction_id'] = pending['transaction_id']
    context.user_data['pending_transaction_id'] = pending['transaction_id']
    context.user_data['pending_completion_code'] = pending.get('completion_code')
    context.user_data['amount'] = pending['amount']
    context.user_data['dealer_id'] = pending.get('dealer_id')
    context.user_data['network'] = pending.get('crypto_network')
    context.user_data['deposit_address'] = pending.get('crypto_address')
    context.user_data['api_key'] = pending.get('api_key')
    context.user_data['api_secret'] = pending.get('api_secret')


async def _show_currency_selection(message, lang: str):
    rates = get_rates()
    usdt = rates.get('USDT', Config.DEFAULT_USDT_RATE)
    rub = rates.get('RUB', Config.DEFAULT_RUB_RATE)
    kb = [
        [InlineKeyboardButton(f"💵 USDT (1 USDT = {usdt:.2f} TL)", callback_data="currency_usdt")],
        [InlineKeyboardButton(f"₽ Ruble (1 RUB = {rub:.4f} TL)", callback_data="currency_ruble")],
    ]
    await message.reply_text(
        f"🏦 *Money Transfer Turkey Bot*\n\n"
        f"{t('welcome', lang)}\n{t('choose_currency', lang)}",
        reply_markup=InlineKeyboardMarkup(kb),
        parse_mode="Markdown"
    )


# ═══════════════════════════════════════════════
# /status KOMUTU
# ═══════════════════════════════════════════════

async def cmd_status(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user = update.message.from_user
    user_id = user.id
    lang = get_lang_by_id(user_id) or get_lang(user)

    state_name = get_user_state_name(user_id)
    if state_name != 'waiting_confirmations':
        await update.message.reply_text(t('no_active_transaction', lang))
        return

    txid = context.user_data.get('pending_txid')
    api_key = context.user_data.get('api_key')
    api_secret = context.user_data.get('api_secret')
    network = context.user_data.get('network')
    deposit_address = context.user_data.get('deposit_address')
    amount = context.user_data.get('amount')
    completion_code = context.user_data.get('pending_completion_code')
    transaction_id = context.user_data.get('pending_transaction_id')

    if not (api_key and api_secret and txid):
        await update.message.reply_text(t('no_pending_transaction', lang))
        return

    try:
        binance = BinanceAPI(api_key, api_secret)
        is_valid, info = binance.verify_deposit(
            txid=txid, address=deposit_address,
            amount=float(amount), network=network,
            time_limit_minutes=Config.TXID_VERIFY_TIME_LIMIT
        )

        if is_valid:
            confirmations = int(info.get('confirmations', 0))
            if confirmations >= Config.USDT_CONFIRMATIONS_REQUIRED:
                await _complete_usdt(context, user_id, transaction_id,
                                     txid, confirmations, completion_code, lang)
                await update.message.reply_text(
                    t('transaction_completed_full', lang,
                      code=completion_code, txid=txid, confirmations=confirmations),
                    parse_mode="Markdown"
                )
            else:
                db.update_crypto_deposit(txid=txid, confirmations=confirmations)
                await update.message.reply_text(
                    t('txid_checking_progress', lang,
                      confirmations=confirmations, total=10,
                      bar=progress_bar(confirmations), txid=txid),
                    parse_mode="Markdown"
                )
        else:
            await update.message.reply_text(t('txid_verification_failed', lang))
    except Exception as e:
        logger.error(f"Status check error: {e}")
        await update.message.reply_text(t('error_occurred', lang))


# ═══════════════════════════════════════════════
# /cancel KOMUTU
# ═══════════════════════════════════════════════

async def cmd_cancel(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.message.from_user.id
    lang = get_lang_by_id(user_id) or get_lang(update.message.from_user)

    state = get_state(user_id)
    if state and state.get('transaction_id'):
        db.cancel_transaction(state['transaction_id'], user_id)
    clear_state(user_id)
    context.user_data.clear()
    await update.message.reply_text(t('transaction_cancelled', lang))


# ═══════════════════════════════════════════════
# CALLBACK (BUTON) İŞLEYİCİ
# ═══════════════════════════════════════════════

async def handle_callback(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()
    user = query.from_user
    user_id = user.id
    lang = get_lang_by_id(user_id) or get_lang(user)
    data = query.data

    # ── Kanala Katıldım Kontrolü ──
    if data == "check_joined":
        is_member = await check_group_membership(context, user_id)
        if not is_member:
            await query.answer(
                "❌ Henüz kanala katılmadınız! Önce kanala katılın, sonra tekrar deneyin.",
                show_alert=True
            )
            return
        referral = context.user_data.get('referral')
        if not referral:
            await query.edit_message_text(
                "⚠️ Oturum zaman aşımına uğradı.\nLütfen QR kodu tekrar okutun.",
                parse_mode="Markdown"
            )
            return
        await query.edit_message_text(
            f"✅ **Kanala katılım doğrulandı!**\n\n"
            f"Hoş geldiniz! Lütfen işlem türünü seçin:",
            parse_mode="Markdown"
        )
        await _show_currency_selection(query.message, lang)
        set_state(user_id, 'selecting_currency')
        return

    # ── Para Birimi Seçimi ──
    if data.startswith("currency_"):
        currency = data.split("_")[1].upper()
        context.user_data['currency'] = currency

        rates = get_rates()
        if currency == "USDT":
            rate = rates.get('USDT', Config.DEFAULT_USDT_RATE)
            min_amt, example_amt = Config.MIN_USDT, 100
        else:
            rate = rates.get('RUB', Config.DEFAULT_RUB_RATE)
            min_amt, example_amt = Config.MIN_RUBLE, 10000

        context.user_data['exchange_rate'] = rate
        set_state(user_id, 'waiting_amount')

        await query.edit_message_text(
            t('enter_amount_prompt', lang,
              currency=currency,
              rate=f"{rate:.4f}" if currency == "RUBLE" else f"{rate:.2f}",
              min_line=t('minimum_amount', lang, amount=int(min_amt), currency=currency),
              example_line=t('example_amount', lang, amount=example_amt, currency=currency,
                             try_amount=f"{example_amt * rate:,.2f}"))
        )
        return

    # ── İşlem İptali ──
    if data.startswith("cancel_transaction_"):
        tid = int(data.split("_")[2])
        if db.cancel_transaction(tid, user_id):
            clear_state(user_id)
            context.user_data.clear()
            await query.edit_message_text(
                f"✅ **{t('transaction_cancelled', lang)}**\n\n"
                f"/start ile yeni işlem başlatabilirsiniz.",
                parse_mode="Markdown"
            )
        else:
            await query.answer(t('error_occurred', lang), show_alert=True)
        return

    # ── Yeni İşlem (QR gerekli) ──
    if data == "new_transaction":
        await query.answer(t('qr_required', lang), show_alert=True)
        await query.edit_message_text(t('qr_required', lang), parse_mode="Markdown")
        return

    # ── Durum Yenile ──
    if data.startswith("refresh_status_"):
        tid = int(data.split("_")[2])
        await _handle_refresh_status(query, context, tid, user_id, lang)
        return


# ═══════════════════════════════════════════════
# DURUM YENİLEME
# ═══════════════════════════════════════════════

async def _handle_refresh_status(query, context, transaction_id: int, user_id: int, lang: str):
    deposit = db.get_pending_crypto_deposit(transaction_id)
    if not deposit:
        await query.answer(t('no_pending_transaction', lang), show_alert=True)
        return

    api_key = deposit.get('api_key')
    api_secret = deposit.get('api_secret')
    if not (api_key and api_secret):
        await query.answer(t('error_occurred', lang), show_alert=True)
        return

    try:
        binance = BinanceAPI(api_key, api_secret)
        is_valid, info = binance.verify_deposit(
            txid=deposit['txid'], address=deposit['to_address'],
            amount=float(deposit['amount']), network=deposit['network'],
            time_limit_minutes=Config.TXID_VERIFY_TIME_LIMIT
        )

        if is_valid:
            confirmations = int(info.get('confirmations', 0))
            if confirmations >= Config.USDT_CONFIRMATIONS_REQUIRED:
                await _complete_usdt(context, user_id, transaction_id,
                                     deposit['txid'], confirmations,
                                     deposit.get('completion_code'), lang)
                await query.edit_message_text(
                    t('transaction_completed_full', lang,
                      code=deposit.get('completion_code', '---'),
                      txid=deposit['txid'], confirmations=confirmations),
                    parse_mode="Markdown"
                )
            else:
                db.update_crypto_deposit(txid=deposit['txid'], confirmations=confirmations)
                kb = [[InlineKeyboardButton("🔄", callback_data=f"refresh_status_{transaction_id}")]]
                await query.edit_message_text(
                    t('txid_checking_progress', lang,
                      confirmations=confirmations, total=10,
                      bar=progress_bar(confirmations), txid=deposit['txid']),
                    parse_mode="Markdown",
                    reply_markup=InlineKeyboardMarkup(kb)
                )
        else:
            await query.answer(t('txid_verification_failed', lang), show_alert=True)
    except Exception as e:
        logger.error(f"Refresh status error: {e}")
        await query.answer(t('error_occurred', lang), show_alert=True)


# ═══════════════════════════════════════════════
# MESAJ İŞLEYİCİ (METİN)
# ═══════════════════════════════════════════════

async def handle_message(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user = update.message.from_user
    user_id = user.id
    text = update.message.text
    lang = get_lang_by_id(user_id) or get_lang(user)

    state_name = get_user_state_name(user_id)

    # ── Miktar Girişi ──
    if state_name == 'waiting_amount':
        await _handle_amount(update, context, user_id, text, lang)
        return

    # ── TXID Girişi ──
    if state_name == 'waiting_txid':
        await _handle_txid(update, context, user_id, text, lang)
        return

    # ── Operatör Chat ──
    if state_name == 'in_chat':
        await _handle_chat_message(update, context, user_id, text, lang)
        return

    # ── Ruble bekleyen dekont ──
    if state_name in ('waiting_bank_account', 'waiting_receipt'):
        pending_ruble = db.get_pending_ruble(user_id)
        if pending_ruble:
            await update.message.reply_text(t('send_pdf_receipt', lang), parse_mode="Markdown")
            return

    # ── State yok — bekleyen işlem var mı? ──
    pending_usdt = db.get_pending_usdt(user_id)
    if pending_usdt:
        _restore_pending_usdt(context, pending_usdt)
        set_state(user_id, 'waiting_txid', pending_usdt['transaction_id'])
        await update.message.reply_text(
            t('pending_usdt_transaction', lang,
              amount=pending_usdt['amount'],
              code=pending_usdt.get('completion_code', '---'),
              network=pending_usdt.get('crypto_network', 'TRC20'),
              address=pending_usdt.get('crypto_address', ''),
              amount2=pending_usdt['amount']),
            parse_mode="Markdown"
        )
        return

    pending_ruble = db.get_pending_ruble(user_id)
    if pending_ruble:
        await update.message.reply_text(t('send_pdf_receipt', lang), parse_mode="Markdown")
        return

    # Hiçbir state yok → QR gerekli
    await update.message.reply_text(t('qr_required', lang), parse_mode="Markdown")


# ═══════════════════════════════════════════════
# MİKTAR İŞLEME
# ═══════════════════════════════════════════════

async def _handle_amount(update: Update, context: ContextTypes.DEFAULT_TYPE,
                         user_id: int, text: str, lang: str):
    try:
        amount = float(text.replace(",", "."))
    except ValueError:
        await update.message.reply_text(t('invalid_amount', lang))
        return

    currency = context.user_data.get('currency', 'USDT')
    referral = context.user_data.get('referral', '')

    # Minimum kontrol
    if currency == 'USDT' and amount < Config.MIN_USDT:
        await update.message.reply_text(t('minimum_usdt_error', lang), parse_mode="Markdown")
        return
    if currency == 'RUBLE' and amount < Config.MIN_RUBLE:
        await update.message.reply_text(t('minimum_ruble_error', lang), parse_mode="Markdown")
        return

    context.user_data['amount'] = amount
    exchange_rate = context.user_data.get('exchange_rate', Config.DEFAULT_USDT_RATE)
    try_amount = amount * exchange_rate

    # İşlem oluştur
    tid = db.create_transaction(user_id, currency, amount, referral, exchange_rate, try_amount)
    if not tid:
        await update.message.reply_text(t('error_occurred', lang))
        return

    context.user_data['transaction_id'] = tid
    context.user_data['pending_transaction_id'] = tid

    # Tamamlama kodu üret
    completion_code = generate_completion_code()
    db.update_transaction(tid, completion_code=completion_code)
    context.user_data['completion_code'] = completion_code
    context.user_data['pending_completion_code'] = completion_code

    # Operatörlere bildirim
    await _notify_operators(context, update.message.from_user, currency, amount, try_amount, referral, tid)

    # RUBLE akışı
    if currency == 'RUBLE':
        await _start_ruble_flow(update, context, user_id, tid, amount, try_amount, lang)
        return

    # USDT akışı
    await _start_usdt_flow(update, context, user_id, tid, amount, try_amount,
                           referral, completion_code, lang)


async def _notify_operators(context, user, currency, amount, try_amount, referral, tid):
    import html as _html
    customer_name = _html.escape(user.first_name or "Müşteri")

    # Operatör kanalına bildirim gönder (MoneyTransferTurkey_Operator)
    operator_channel = Config.OPERATOR_CHANNEL_ID or Config.RUBLE_CHANNEL_ID
    if operator_channel:
        try:
            async with Bot(token=Config.OPERATOR_BOT_TOKEN) as op_bot:
                await op_bot.send_message(
                    chat_id=operator_channel,
                    text=(
                        f"🔔 <b>YENİ MÜŞTERİ</b>\n"
                        f"━━━━━━━━━━━━━━━\n"
                        f"👤 Müşteri: {customer_name}\n"
                        f"💱 İşlem: {amount} {currency}\n"
                        f"💰 TL Karşılığı: {try_amount:,.2f} TL\n"
                        f"🔗 Referans: {referral}\n"
                        f"🔖 İşlem ID: #{tid}\n"
                        f"⏰ Saat: {datetime.now().strftime('%H:%M')}\n"
                        f"━━━━━━━━━━━━━━━\n"
                        f"💰 İşlem Türü: Alış"
                    ),
                    parse_mode="HTML"
                )
        except Exception as e:
            logger.error(f"Operatör kanalına bildirim hatası: {e}")

    # Operatörlere DM bildirim
    operators = db.get_active_operators()
    if not operators:
        return

    try:
        async with Bot(token=Config.OPERATOR_BOT_TOKEN) as op_bot:
            for op in operators:
                try:
                    await op_bot.send_message(
                        chat_id=op['id'],
                        text=(
                            f"🔔 <b>YENİ MÜŞTERİ!</b>\n"
                            f"━━━━━━━━━━━━━━━\n"
                            f"👤 Müşteri: {customer_name}\n"
                            f"💱 İşlem: {amount} {currency}\n"
                            f"💰 TL Karşılığı: {try_amount:,.2f} TL\n"
                            f"🔗 Referans: {referral}\n"
                            f"🔖 İşlem ID: #{tid}\n"
                            f"━━━━━━━━━━━━━━━\n"
                            f"💰 İşlem Türü: Alış"
                        ),
                        parse_mode="HTML"
                    )
                except Exception as e:
                    logger.error(f"Operatör {op['id']} bildirim hatası: {e}")
    except Exception as e:
        logger.error(f"Operatör bot bildirim hatası: {e}")


# ═══════════════════════════════════════════════
# USDT AKIŞI
# ═══════════════════════════════════════════════

async def _start_usdt_flow(update, context, user_id, tid, amount, try_amount,
                           referral, completion_code, lang):
    dealer = db.get_dealer(referral)
    if not dealer or not dealer.get('crypto_address'):
        # Bayi kripto adresi yok → operatöre yönlendir
        set_state(user_id, 'in_chat', tid)
        await update.message.reply_text(
            f"{t('request_received', lang)}\n\n"
            f"{t('summary', lang)}\n"
            f"• {t('currency_label', lang, currency='USDT')}\n"
            f"• {t('amount_label', lang, amount=amount, currency='USDT')}\n"
            f"• {t('try_equivalent', lang, total=f'{try_amount:,.2f}')}\n\n"
            f"{t('operator_will_contact', lang)}",
            parse_mode="Markdown"
        )
        return

    # Bayi bilgilerini context'e kaydet
    context.user_data['dealer_id'] = dealer['dealer_id']
    context.user_data['network'] = dealer['crypto_network']
    context.user_data['deposit_address'] = dealer['crypto_address']
    context.user_data['exchange_name'] = dealer.get('exchange_name', 'binance')
    context.user_data['api_key'] = dealer.get('api_key')
    context.user_data['api_secret'] = dealer.get('api_secret')

    set_state(user_id, 'waiting_txid', tid)

    # İptal butonu
    kb = [[InlineKeyboardButton(t('cancel_transaction', lang),
                                 callback_data=f"cancel_transaction_{tid}")]]

    # Bot mesajını DB'ye kaydet
    db.save_message(tid, 0, 'bot',
                    f"USDT gönderim bilgileri: Network={dealer['crypto_network']}, "
                    f"Adres={dealer['crypto_address']}, Miktar={amount}")

    await update.message.reply_text(
        f"{t('transaction_created', lang)}\n\n"
        f"{t('summary', lang)}\n"
        f"• {t('currency_label', lang, currency='USDT')}\n"
        f"• {t('amount_label', lang, amount=amount, currency='USDT')}\n"
        f"• {t('try_equivalent', lang, total=f'{try_amount:,.2f}')}\n\n"
        f"{t('usdt_send_info', lang)}\n"
        f"{t('network_label', lang, network=dealer['crypto_network'])}\n"
        f"{t('address_label', lang, address=dealer['crypto_address'])}\n"
        f"{t('amount_usdt_label', lang, amount=amount)}\n"
        f"{t('warning_network', lang)}\n\n"
        f"{t('send_usdt', lang)}\n"
        f"{t('send_txid', lang)}\n\n"
        f"{t('example_txid', lang)}",
        parse_mode="Markdown",
        reply_markup=InlineKeyboardMarkup(kb)
    )


# ═══════════════════════════════════════════════
# RUBLE AKIŞI
# ═══════════════════════════════════════════════

async def _start_ruble_flow(update, context, user_id, tid, amount, try_amount, lang):
    # Ruble kanalına bildirim gönder
    try:
        import html as _html
        customer_name = _html.escape(update.message.from_user.first_name or "Müşteri")
        async with Bot(token=Config.RUBLE_BOT_TOKEN) as ruble_bot:
            await ruble_bot.send_message(
                chat_id=Config.RUBLE_CHANNEL_ID,
                text=(
                    f"🟢🟢🟢 <b>YENİ MÜŞTERİ</b> 🟢🟢🟢\n"
                    f"━━━━━━━━━━━━━━━\n"
                    f"🆕 <b>YENİ RUBLE İŞLEMİ</b>\n"
                    f"━━━━━━━━━━━━━━━\n"
                    f"👤 Müşteri: {customer_name}\n"
                    f"💰 Miktar: {amount:,.2f} RUB\n"
                    f"🔖 İşlem ID: #{tid}\n"
                    f"⏰ Saat: {datetime.now().strftime('%H:%M')}\n"
                    f"━━━━━━━━━━━━━━━\n"
                    f"⚡ <b>HEMEN CEVAP VERİN!</b>\n"
                    f"💡 <b>Bu mesaja reply atarak banka hesabı bilgilerini gönderin</b>"
                ),
                parse_mode="HTML"
            )
    except Exception as e:
        logger.error(f"Ruble kanalına bildirim hatası: {e}")

    set_state(user_id, 'waiting_bank_account', tid)
    db.update_transaction(tid, status='waiting_payment')

    await update.message.reply_text(
        f"{t('transaction_created', lang)}\n\n"
        f"{t('summary', lang)}\n"
        f"• {t('currency_label', lang, currency='RUBLE')}\n"
        f"• {t('amount_label', lang, amount=f'{amount:,.2f}', currency='RUB')}\n"
        f"• {t('try_equivalent', lang, total=f'{try_amount:,.2f}')}\n\n"
        f"{t('bank_info_preparing', lang)}",
        parse_mode="Markdown"
    )


# ═══════════════════════════════════════════════
# TXID İŞLEME
# ═══════════════════════════════════════════════

async def _handle_txid(update: Update, context: ContextTypes.DEFAULT_TYPE,
                       user_id: int, text: str, lang: str):
    original_txid = text.strip()
    txid = original_txid

    transaction_id = context.user_data.get('pending_transaction_id') or \
                     context.user_data.get('transaction_id')

    # Müşteri mesajını kaydet
    if transaction_id:
        db.save_message(transaction_id, user_id, 'customer', f"TXID: {txid}")

    # Off-chain temizliği
    if 'off-chain' in txid.lower():
        match = re.search(r'[\da-fA-F]{20,}|\d{10,}', txid)
        if match:
            txid = match.group()
        else:
            await update.message.reply_text(t('offchain_detected', lang), parse_mode="Markdown")
            return

    # TXID uzunluk kontrolü
    if len(txid) < Config.TXID_MIN_LENGTH:
        await update.message.reply_text(t('invalid_txid', lang), parse_mode="Markdown")
        return

    # Daha önce kullanılmış mı?
    if db.check_txid_used(txid):
        await update.message.reply_text(
            "❌ Bu TXID daha önce kullanılmış! Dolandırıcılık girişimi kaydedildi.",
            parse_mode="Markdown"
        )
        return

    # Dealer bilgilerini al (context'te yoksa DB'den)
    if not context.user_data.get('api_key'):
        dealer_info = _get_dealer_from_transaction(transaction_id)
        if dealer_info:
            context.user_data.update(dealer_info)
        else:
            await update.message.reply_text(t('error_occurred', lang))
            return

    amount = context.user_data.get('amount')
    completion_code = context.user_data.get('pending_completion_code') or \
                      context.user_data.get('completion_code')
    dealer_id = context.user_data.get('dealer_id')
    network = context.user_data.get('network')
    deposit_address = context.user_data.get('deposit_address')
    api_key = context.user_data.get('api_key')
    api_secret = context.user_data.get('api_secret')

    await update.message.reply_text(t('txid_checking', lang))

    # Binance doğrulama
    if not (api_key and api_secret):
        await update.message.reply_text(t('txid_verification_failed', lang))
        return

    try:
        binance = BinanceAPI(api_key, api_secret)
        is_valid, deposit_info = binance.verify_deposit(
            txid=txid, address=deposit_address,
            amount=float(amount), network=network,
            time_limit_minutes=Config.TXID_VERIFY_TIME_LIMIT
        )
    except Exception as e:
        logger.error(f"Binance API error: {e}")
        is_valid = False
        deposit_info = {"error": str(e)}

    if not is_valid:
        error_msg = deposit_info.get('error', 'Bilinmeyen hata')
        await update.message.reply_text(
            f"❌ **TXID DOĞRULANAMADI!**\n\nHata: {error_msg}\n\n"
            f"Lütfen kontrol edip tekrar deneyin.",
            parse_mode="Markdown"
        )
        return

    confirmations = int(deposit_info.get('confirmations', 0))

    if confirmations >= Config.USDT_CONFIRMATIONS_REQUIRED:
        # Tamamen onaylandı
        db.save_crypto_deposit(transaction_id, dealer_id, original_txid, amount,
                               network, deposit_address, confirmations, 'confirmed')
        await _complete_usdt(context, user_id, transaction_id, txid,
                             confirmations, completion_code, lang)
        await update.message.reply_text(
            t('transaction_completed_full', lang,
              code=completion_code, txid=txid, confirmations=confirmations),
            parse_mode="Markdown"
        )
    else:
        # Bekleyen onaylar
        db.save_crypto_deposit(transaction_id, dealer_id, txid, amount,
                               network, deposit_address, confirmations, 'pending')
        db.update_transaction(transaction_id, txid=txid, crypto_verified=0)

        context.user_data['pending_txid'] = txid
        set_state(user_id, 'waiting_confirmations', transaction_id)

        progress_msg = await update.message.reply_text(
            f"⏳ **{t('txid_checking_progress', lang, confirmations=confirmations, total=10, bar=progress_bar(confirmations), txid=txid)}**\n\n"
            f"• Miktar: {deposit_info['amount']} USDT\n"
            f"• Network: {deposit_info['network']}\n\n"
            f"🔄 Otomatik güncelleniyor...",
            parse_mode="Markdown"
        )

        # Otomatik güncelleme task'ı
        asyncio.create_task(
            _auto_update_progress(context, progress_msg.chat_id, progress_msg.message_id,
                                  transaction_id, user_id, lang)
        )


def _get_dealer_from_transaction(transaction_id: int) -> dict | None:
    try:
        from database import get_conn
        with get_conn() as conn:
            c = conn.cursor()
            c.execute("""
                SELECT d.dealer_id, d.crypto_network, d.crypto_address,
                       d.api_key, d.api_secret, t.amount, t.completion_code
                FROM Transactions t
                JOIN Dealers d ON d.dealer_code = t.referral_code
                WHERE t.transaction_id = ?
            """, transaction_id)
            row = c.fetchone()
            if row:
                return {
                    'dealer_id': row[0], 'network': row[1],
                    'deposit_address': row[2], 'api_key': row[3],
                    'api_secret': row[4], 'amount': row[5],
                    'pending_completion_code': row[6],
                }
    except:
        pass
    return None


# ═══════════════════════════════════════════════
# USDT TAMAMLAMA
# ═══════════════════════════════════════════════

async def _complete_usdt(context, user_id, transaction_id, txid, confirmations,
                         completion_code, lang):
    # CryptoDeposit güncelle
    db.update_crypto_deposit(txid=txid, confirmations=confirmations, status='confirmed')

    # Transaction güncelle
    db.update_transaction(transaction_id,
                          txid=txid, crypto_verified=1,
                          status='completed', completed_at=datetime.now())

    # Dealer bakiye düş
    trans = db.get_transaction(transaction_id)
    if trans:
        rates = get_rates()
        rate = rates.get('USDT', Config.DEFAULT_USDT_RATE)
        amount_try = float(trans['amount']) * rate
        db.reduce_dealer_balance(trans['referral_code'], amount_try)

    # BaşkentEnerji API
    try:
        send_exchange_for_transaction(transaction_id)
    except Exception as e:
        logger.error(f"BaşkentEnerji API hatası: {e}")

    set_state(user_id, 'completed', transaction_id)


# ═══════════════════════════════════════════════
# OTOMATİK ONAY TAKİBİ
# ═══════════════════════════════════════════════

async def _auto_update_progress(context, chat_id, message_id, transaction_id,
                                user_id, lang, max_attempts=60):
    """Her 10 saniyede Binance'dan onay sayısını kontrol et"""
    for _ in range(max_attempts):
        await asyncio.sleep(10)

        # State değiştiyse çık
        if get_user_state_name(user_id) != 'waiting_confirmations':
            return

        deposit = db.get_pending_crypto_deposit(transaction_id)
        if not deposit:
            return

        api_key = deposit.get('api_key')
        api_secret = deposit.get('api_secret')
        if not (api_key and api_secret):
            continue

        try:
            binance = BinanceAPI(api_key, api_secret)
            is_valid, info = binance.verify_deposit(
                txid=deposit['txid'], address=deposit['to_address'],
                amount=float(deposit['amount']), network=deposit['network'],
                time_limit_minutes=Config.TXID_VERIFY_TIME_LIMIT
            )

            if not is_valid:
                continue

            confirmations = int(info.get('confirmations', 0))

            if confirmations >= Config.USDT_CONFIRMATIONS_REQUIRED:
                await _complete_usdt(context, user_id, transaction_id,
                                     deposit['txid'], confirmations,
                                     deposit.get('completion_code'), lang)
                try:
                    await context.bot.edit_message_text(
                        chat_id=chat_id, message_id=message_id,
                        text=t('transaction_completed_full', lang,
                               code=deposit.get('completion_code', '---'),
                               txid=deposit['txid'], confirmations=confirmations),
                        parse_mode="Markdown"
                    )
                except:
                    pass
                return
            else:
                db.update_crypto_deposit(txid=deposit['txid'], confirmations=confirmations)
                try:
                    await context.bot.edit_message_text(
                        chat_id=chat_id, message_id=message_id,
                        text=(
                            f"⏳ **İŞLEM ONAYLANIYOR**\n\n"
                            f"📊 Durum: {confirmations}/10 onay\n"
                            f"{progress_bar(confirmations)}\n\n"
                            f"• TXID: `{deposit['txid']}`\n\n"
                            f"⚠️ İşleminiz onaylanıyor...\n"
                            f"🔄 Otomatik güncelleniyor..."
                        ),
                        parse_mode="Markdown"
                    )
                except:
                    pass  # Mesaj zaten güncel veya silinmiş

        except Exception as e:
            logger.error(f"Auto update error: {e}")


# ═══════════════════════════════════════════════
# DOSYA / FOTOĞRAF İŞLEYİCİ
# ═══════════════════════════════════════════════

async def handle_file(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user = update.message.from_user
    user_id = user.id
    lang = get_lang_by_id(user_id) or get_lang(user)

    # İşlem ID'sini bul
    transaction_id = context.user_data.get('transaction_id')

    # Ruble bekleyen işlem var mı?
    if not transaction_id:
        pending_ruble = db.get_pending_ruble(user_id)
        if pending_ruble:
            transaction_id = pending_ruble['transaction_id']
            context.user_data['transaction_id'] = transaction_id

    if not transaction_id:
        await update.message.reply_text(t('qr_required', lang), parse_mode="Markdown")
        return

    # Uploads klasörü
    upload_dir = Config.UPLOAD_DIR
    os.makedirs(upload_dir, exist_ok=True)

    file_url = None
    file_type_str = None
    original_name = "file"

    if update.message.photo:
        photo = update.message.photo[-1]
        tg_file = await context.bot.get_file(photo.file_id)
        fname = f"{transaction_id}_{user_id}_{uuid.uuid4().hex[:8]}.jpg"
        fpath = os.path.join(upload_dir, fname)
        await tg_file.download_to_drive(fpath)
        file_url = f"/uploads/{fname}"
        file_type_str = "photo"
        original_name = fname
        await update.message.reply_text(t('photo_received', lang))

    elif update.message.document:
        doc = update.message.document
        original_name = doc.file_name or "file"
        ext = os.path.splitext(original_name)[1].lower()

        if ext not in Config.ALLOWED_EXTENSIONS:
            await update.message.reply_text(t('unsupported_file', lang))
            return

        tg_file = await context.bot.get_file(doc.file_id)
        fname = f"{transaction_id}_{user_id}_{uuid.uuid4().hex[:8]}{ext}"
        fpath = os.path.join(upload_dir, fname)
        await tg_file.download_to_drive(fpath)
        file_url = f"/uploads/{fname}"
        file_type_str = "document"
        await update.message.reply_text(t('file_received', lang, filename=original_name))
    else:
        await update.message.reply_text(t('unsupported_file', lang))
        return

    # DB'ye kaydet
    db.save_message(transaction_id, user_id, 'customer',
                    f"[{file_type_str.upper()}]", file_url, file_type_str)

    # Ruble işlemi ise → kanala gönder
    trans = db.get_transaction(transaction_id)
    if trans and trans.get('currency') == 'RUBLE':
        await _send_receipt_to_channel(update, context, transaction_id,
                                       fpath, file_type_str, original_name, lang)
        db.update_transaction(transaction_id, status='payment_sent')
        db.notify_web_panel(transaction_id)
    elif trans and trans.get('assigned_operator_id'):
        # Operatöre bildirim
        try:
            await Bot(token=Config.OPERATOR_BOT_TOKEN).send_message(
                chat_id=trans['assigned_operator_id'],
                text=f"📎 Müşteri dosya gönderdi: {original_name}"
            )
        except:
            pass


async def _send_receipt_to_channel(update, context, transaction_id,
                                   file_path, file_type, original_name, lang):
    try:
        ruble_bot = Bot(token=Config.RUBLE_BOT_TOKEN)
        kb = [[
            InlineKeyboardButton("✅ Ödemeyi Onayla",
                                 callback_data=f"approve_payment_{transaction_id}"),
            InlineKeyboardButton("❌ Ödemeyi Reddet",
                                 callback_data=f"reject_payment_{transaction_id}")
        ]]
        markup = InlineKeyboardMarkup(kb)
        user_name = update.message.from_user.first_name or "Müşteri"

        caption = (
            f"📎 **DEKONT GÖNDERİLDİ**\n"
            f"━━━━━━━━━━━━━━━\n"
            f"🔖 İşlem ID: #{transaction_id}\n"
            f"👤 Müşteri: {user_name}\n"
            f"━━━━━━━━━━━━━━━\n"
            f"Lütfen dekontu kontrol edip onaylayın veya reddedin."
        )

        with open(file_path, 'rb') as f:
            if file_type == 'photo':
                await ruble_bot.send_photo(
                    chat_id=Config.RUBLE_CHANNEL_ID, photo=f,
                    caption=caption, parse_mode="Markdown", reply_markup=markup
                )
            else:
                await ruble_bot.send_document(
                    chat_id=Config.RUBLE_CHANNEL_ID, document=f,
                    caption=caption, parse_mode="Markdown", reply_markup=markup
                )

        await update.message.reply_text(t('receipt_sent_success', lang))
    except Exception as e:
        logger.error(f"Kanala dekont gönderim hatası: {e}")
        await update.message.reply_text(t('receipt_upload_error', lang))


# ═══════════════════════════════════════════════
# OPERATÖR CHAT
# ═══════════════════════════════════════════════

async def _handle_chat_message(update, context, user_id, text, lang):
    transaction_id = context.user_data.get('transaction_id')

    if not transaction_id:
        active = db.get_active_transaction(user_id)
        if active:
            transaction_id = active['transaction_id']
            context.user_data['transaction_id'] = transaction_id
        else:
            await update.message.reply_text(t('no_active_transaction', lang))
            return

    # Mesajı kaydet
    db.save_message(transaction_id, user_id, 'customer', text)
    db.notify_web_panel(transaction_id)

    # Operatöre ilet
    trans = db.get_transaction(transaction_id)
    if trans and trans.get('assigned_operator_id'):
        try:
            op_bot = Bot(token=Config.OPERATOR_BOT_TOKEN)
            await op_bot.send_message(
                chat_id=trans['assigned_operator_id'],
                text=f"💬 **Müşteri yazdı:**\n\n{text}",
                parse_mode="Markdown"
            )
            await update.message.reply_text(t('message_sent_to_operator', lang))
        except Exception as e:
            logger.error(f"Operatöre mesaj iletme hatası: {e}")
    else:
        await update.message.reply_text(t('no_operator_yet', lang))


# ═══════════════════════════════════════════════
# GRUP MESAJLARI
# ═══════════════════════════════════════════════

async def handle_group_message(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if not update.message:
        return

    # Yeni üye katıldığında
    if update.message.new_chat_members:
        bot_username = (await context.bot.get_me()).username
        for member in update.message.new_chat_members:
            if member.is_bot:
                continue
            kb = [[InlineKeyboardButton("🚀 BOTA GİT VE İŞLEME DEVAM ET",
                                         url=f"https://t.me/{bot_username}")]]
            temp_msg = await update.message.reply_text(
                f"✅ Hoş geldin {member.first_name}!\n\n"
                f"💵 Para transferi için aşağıdaki butona tıkla:",
                reply_markup=InlineKeyboardMarkup(kb),
                reply_to_message_id=update.message.message_id
            )
            # 15 saniye sonra sil
            await asyncio.sleep(15)
            try:
                await temp_msg.delete()
                await update.message.delete()
            except:
                pass
        return

    # /start komutu grupta
    if update.message.text and "/start" in update.message.text:
        bot_username = (await context.bot.get_me()).username
        kb = [[InlineKeyboardButton("🤖 Bota Git ve İşlem Başlat",
                                     url=f"https://t.me/{bot_username}")]]
        await update.message.reply_text(
            "👋 Merhaba! Para transferi için bota özel mesaj gönderin:",
            reply_markup=InlineKeyboardMarkup(kb)
        )


# ═══════════════════════════════════════════════
# BANKA BİLGİSİ İLETME (Ruble bot → Main bot)
# ═══════════════════════════════════════════════

async def forward_bank_info(customer_id: int, bank_text: str, transaction_id: int):
    """Ruble bot tarafından çağrılır — müşteriye banka bilgisi iletir"""
    lang = get_lang_by_id(customer_id)
    try:
        app_bot = Bot(token=Config.MAIN_BOT_TOKEN)
        await app_bot.send_message(
            chat_id=customer_id,
            text=(
                f"{t('bank_info_header', lang)}\n"
                f"━━━━━━━━━━━━━━━\n\n"
                f"{bank_text}\n\n"
                f"━━━━━━━━━━━━━━━\n"
                f"{t('send_pdf_receipt', lang)}"
            ),
            parse_mode="Markdown"
        )
        set_state(customer_id, 'waiting_receipt', transaction_id)
        db.update_transaction(transaction_id, status='bank_provided')
    except Exception as e:
        logger.error(f"Banka bilgisi iletme hatası: {e}")


async def notify_customer(customer_id: int, text: str):
    """Diğer botlardan müşteriye mesaj gönder"""
    try:
        bot = Bot(token=Config.MAIN_BOT_TOKEN)
        await bot.send_message(chat_id=customer_id, text=text, parse_mode="Markdown")
    except Exception as e:
        logger.error(f"Müşteriye bildirim hatası: {e}")


# ═══════════════════════════════════════════════
# BOT BAŞLATMA
# ═══════════════════════════════════════════════

_app: Application = None  # Global reference for cross-bot communication


def create_app() -> Application:
    global _app
    app = Application.builder().token(Config.MAIN_BOT_TOKEN).build()

    # Komutlar
    app.add_handler(CommandHandler("start", cmd_start))
    app.add_handler(CommandHandler("status", cmd_status))
    app.add_handler(CommandHandler("cancel", cmd_cancel))

    # Grup mesajları (önce)
    app.add_handler(MessageHandler(
        filters.ChatType.GROUP | filters.ChatType.SUPERGROUP,
        handle_group_message
    ))

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


async def _heartbeat_loop():
    """60 saniyede bir heartbeat gönder"""
    while True:
        try:
            db.update_heartbeat('main_bot', len(_state_cache))
        except Exception as e:
            logger.error(f"Heartbeat hatası: {e}")
        await asyncio.sleep(60)


async def start():
    """Bot'u başlat (run_all.py'den çağrılır)"""
    recover_active_sessions()
    app = create_app()
    await app.initialize()
    await app.start()
    await app.updater.start_polling(drop_pending_updates=True)
    logger.info("Ana müşteri botu başlatıldı")

    # Background tasks
    asyncio.create_task(_heartbeat_loop())
    asyncio.create_task(baskent_queue_processor())
    logger.info("BaskentEnerji kuyruk işleyici başlatıldı")

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
