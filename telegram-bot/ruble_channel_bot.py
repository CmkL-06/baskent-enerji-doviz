import asyncio
import pyodbc
import requests
from telegram import Update, InlineKeyboardButton, InlineKeyboardMarkup, InputFile
from telegram.ext import Application, CommandHandler, MessageHandler, CallbackQueryHandler, filters, ContextTypes
from datetime import datetime
import logging
import os
import uuid
from dotenv import load_dotenv
import io
load_dotenv()
logging.basicConfig(format='%(asctime)s - %(name)s - %(levelname)s - %(message)s', level=logging.INFO)
logger = logging.getLogger(__name__)
BOT_TOKEN = os.getenv('USDT_BOT_TOKEN', '')  # @MoneyExchangeUSDTBot — ayrı token, 409 çakışması önlendi
CHANNEL_ID = os.getenv('RUBLE_CHANNEL_ID', '')
DB_CONFIG = {
    'server': os.getenv('DB_SERVER'),
    'database': os.getenv('DB_NAME'),
    'username': os.getenv('DB_USERNAME'),
    'password': os.getenv('DB_PASSWORD'),
    'driver': '{ODBC Driver 17 for SQL Server}'
}
CONNECTION_STRING = f"DRIVER={DB_CONFIG['driver']};SERVER={DB_CONFIG['server']};DATABASE={DB_CONFIG['database']};UID={DB_CONFIG['username']};PWD={DB_CONFIG['password']}"
BANK_PROVIDERS = [7462722250, 534876470]
active_ruble_transactions = {}
BASKENT_CONFIG = {
    'api_url': os.getenv('BASKENT_API_URL'),
    'username': os.getenv('BASKENT_USERNAME'),
    'password': os.getenv('BASKENT_PASSWORD'),
    'default_vault_id': os.getenv('BASKENT_DEFAULT_VAULT_ID'),
    'usdt_currency_id': os.getenv('BASKENT_USDT_CURRENCY_ID'),
    'ruble_currency_id': os.getenv('BASKENT_RUBLE_CURRENCY_ID')
}
BASKENT_API_TOKEN = os.getenv('BASKENT_API_TOKEN', '')
TRANSLATIONS = {
    'tr': {
        'bank_info_header': '💳 **BANKA HESABI BİLGİLERİ**',
        'bank_info_copy_hint': '💡 Her satıra tıklayarak kopyalayabilirsiniz.',
        'bank_warning': '⚠️ **ÇOK ÖNEMLİ UYARI!**\n🚨 Parayı kesinlikle belirtilen bankaya gönderin!\n❌ Başka bankaya gönderim durumunda ofis sorumluluk kabul etmez ve işlem yapılmayabilir!',
        'copy_instruction': '💡 Yukarıdaki bilgilere uzun basarak kopyalayabilirsiniz.',
        'send_pdf_after_bank': '📎 Lütfen havale/EFT dekontunuzu PDF olarak gönderin.',
        'pdf_required': '⚠️ Dekont PDF formatında olmalıdır.',
        'message_instead_of_pdf': '❗ Mesaj yerine PDF dosyası gönderin.',
        'receipt_sent_success': '✅ Dekont başarıyla gönderildi!\nBanka hesabı sağlayıcısı kontrol edip size dönecektir.',
        'transaction_completed': '✅ **İŞLEM TAMAMLANDI!**',
        'transaction_code': '🎯 **İŞLEM KODU: {}**',
        'transaction_details': '📋 İşlem Detayları:',
        'transaction_id_label': '• İşlem ID: #{}',
        'ruble_amount_label': '• Ruble Miktarı: {:,.2f} RUB',
        'try_amount_label': '• TL Karşılığı: {:,.2f} TL',
        'exchange_rate_label': '• Kur: {:.4f}',
        'status_label': '• Durum: Onaylandı ✅',
        'show_code_to_dealer': '⚠️ **Bu kodu bayiye gösterin!**',
        'thank_you': 'Teşekkür ederiz! 🙏',
        'bank_info_sent': '✅ Banka hesabı bilgileri müşteriye gönderildi!\n📎 Dekont bekleniyor...',
        'approve_button': '✅ Onayla',
        'reject_button': '❌ Reddet',
        'new_ruble_transaction': '🟢 YENİ RUBLE İŞLEMİ',
        'customer_label': 'Müşteri',
        'amount_label': 'Miktar',
        'waiting_bank_info': 'Banka bilgisi bekleniyor...',
        'pdf_receipt_received': '📎 PDF DEKONT GELDİ'
    },
    'en': {
        'bank_info_header': '💳 **BANK ACCOUNT INFORMATION**',
        'bank_info_copy_hint': '💡 Click on each line to copy.',
        'bank_warning': '⚠️ **VERY IMPORTANT WARNING!**\n🚨 Send money strictly to the specified bank!\n❌ In case of transfer to another bank, office takes no responsibility and exchange may not be performed!',
        'copy_instruction': '💡 Long press on the information above to copy.',
        'send_pdf_after_bank': '📎 Please send your bank transfer receipt as PDF.',
        'pdf_required': '⚠️ Receipt must be in PDF format.',
        'message_instead_of_pdf': '❗ Send PDF file instead of message.',
        'receipt_sent_success': '✅ Receipt sent successfully!\nBank account provider will check and get back to you.',
        'transaction_completed': '✅ **TRANSACTION COMPLETED!**',
        'transaction_code': '🎯 **TRANSACTION CODE: {}**',
        'transaction_details': '📋 Transaction Details:',
        'transaction_id_label': '• Transaction ID: #{}',
        'ruble_amount_label': '• Ruble Amount: {:,.2f} RUB',
        'try_amount_label': '• TL Equivalent: {:,.2f} TL',
        'exchange_rate_label': '• Exchange Rate: {:.4f}',
        'status_label': '• Status: Approved ✅',
        'show_code_to_dealer': '⚠️ **Show this code to the dealer!**',
        'thank_you': 'Thank you! 🙏',
        'bank_info_sent': '✅ Bank account information sent to customer!\n📎 Waiting for receipt...',
        'approve_button': '✅ Approve',
        'reject_button': '❌ Reject',
        'new_ruble_transaction': '🟢 NEW RUBLE TRANSACTION',
        'customer_label': 'Customer',
        'amount_label': 'Amount',
        'waiting_bank_info': 'Waiting for bank info...',
        'pdf_receipt_received': '📎 PDF RECEIPT RECEIVED'
    },
    'ru': {
        'bank_info_header': '💳 **ИНФОРМАЦИЯ О БАНКОВСКОМ СЧЕТЕ**',
        'bank_info_copy_hint': '💡 Нажмите на каждую строку, чтобы скопировать.',
        'bank_warning': '⚠️ **ОЧЕНЬ ВАЖНОЕ ПРЕДУПРЕЖДЕНИЕ!**\n🚨 Деньги отправлять строго в указанный банк!\n❌ В случае отправки денег на другой банк, офис не несет ответственность и не гарантирует произведение обмена!',
        'copy_instruction': '💡 Нажмите и удерживайте информацию выше для копирования.',
        'send_pdf_after_bank': '📎 Пожалуйста, отправьте квитанцию о банковском переводе в формате PDF.',
        'pdf_required': '⚠️ Квитанция должна быть в формате PDF.',
        'message_instead_of_pdf': '❗ Отправьте PDF файл вместо сообщения.',
        'receipt_sent_success': '✅ Квитанция успешно отправлена!\nПоставщик банковского счета проверит и свяжется с вами.',
        'transaction_completed': '✅ **ТРАНЗАКЦИЯ ЗАВЕРШЕНА!**',
        'transaction_code': '🎯 **КОД ТРАНЗАКЦИИ: {}**',
        'transaction_details': '📋 Детали транзакции:',
        'transaction_id_label': '• ID транзакции: #{}',
        'ruble_amount_label': '• Сумма в рублях: {:,.2f} RUB',
        'try_amount_label': '• Эквивалент в TL: {:,.2f} TL',
        'exchange_rate_label': '• Курс обмена: {:.4f}',
        'status_label': '• Статус: Одобрено ✅',
        'show_code_to_dealer': '⚠️ **Покажите этот код дилеру!**',
        'thank_you': 'Спасибо! 🙏',
        'bank_info_sent': '✅ Информация о банковском счете отправлена клиенту!\n📎 Ожидаем квитанцию...',
        'approve_button': '✅ Одобрить',
        'reject_button': '❌ Отклонить',
        'new_ruble_transaction': '🟢 НОВАЯ РУБЛЬ ТРАНЗАКЦИЯ',
        'customer_label': 'Клиент',
        'amount_label': 'Сумма',
        'waiting_bank_info': 'Ожидание банковской информации...',
        'pdf_receipt_received': '📎 PDF КВИТАНЦИЯ ПОЛУЧЕНА'
    },
    'de': {
        'bank_info_header': '💳 **BANKKONTOINFORMATIONEN**',
        'bank_info_copy_hint': '💡 Klicken Sie auf jede Zeile zum Kopieren.',
        'bank_warning': '⚠️ **SEHR WICHTIGE WARNUNG!**\n🚨 Geld streng an die angegebene Bank senden!\n❌ Bei Überweisung an eine andere Bank übernimmt das Büro keine Verantwortung und der Umtausch wird möglicherweise nicht durchgeführt!',
        'copy_instruction': '💡 Lang auf die obigen Informationen drücken zum Kopieren.',
        'send_pdf_after_bank': '📎 Bitte senden Sie Ihre Überweisungsquittung als PDF.',
        'pdf_required': '⚠️ Quittung muss im PDF-Format sein.',
        'message_instead_of_pdf': '❗ PDF-Datei statt Nachricht senden.',
        'receipt_sent_success': '✅ Quittung erfolgreich gesendet!\nBankkonto-Anbieter wird prüfen und sich bei Ihnen melden.',
        'transaction_completed': '✅ **TRANSAKTION ABGESCHLOSSEN!**',
        'transaction_code': '🎯 **TRANSAKTIONSCODE: {}**',
        'transaction_details': '📋 Transaktionsdetails:',
        'transaction_id_label': '• Transaktions-ID: #{}',
        'ruble_amount_label': '• Rubel-Betrag: {:,.2f} RUB',
        'try_amount_label': '• TL-Gegenwert: {:,.2f} TL',
        'exchange_rate_label': '• Wechselkurs: {:.4f}',
        'status_label': '• Status: Genehmigt ✅',
        'show_code_to_dealer': '⚠️ **Zeigen Sie diesen Code dem Händler!**',
        'thank_you': 'Danke! 🙏',
        'bank_info_sent': '✅ Bankkontoinformationen an Kunde gesendet!\n📎 Warte auf Quittung...',
        'approve_button': '✅ Genehmigen',
        'reject_button': '❌ Ablehnen',
        'new_ruble_transaction': '🟢 NEUE RUBEL TRANSAKTION',
        'customer_label': 'Kunde',
        'amount_label': 'Betrag',
        'waiting_bank_info': 'Warte auf Bankinformationen...',
        'pdf_receipt_received': '📎 PDF QUITTUNG ERHALTEN'
    }
}
def get_user_language(customer_id):
    """Kullanıcının dil tercihini veritabanından al"""
    conn = get_db_connection()
    if conn:
        cursor = conn.cursor()
        cursor.execute("""
            SELECT language_code FROM Customers WHERE customer_id = ?
        """, customer_id)
        result = cursor.fetchone()
        conn.close()

        if result and result[0]:
            lang_code = result[0][:2].lower()
            if lang_code in TRANSLATIONS:
                return lang_code
    return 'tr'  # Default to Turkish

def get_text(customer_id, key, *args):
    """Get translated text based on customer's language"""
    lang = get_user_language(customer_id)
    text = TRANSLATIONS.get(lang, TRANSLATIONS['tr']).get(key, TRANSLATIONS['tr'][key])
    if args:
        return text.format(*args)
    return text

# ============================================
# BASKENT ENERJI API INTEGRATION
# ============================================

def baskent_login():
    """Login to Baskent Enerji API and get token"""
    global BASKENT_API_TOKEN
    try:
        login_url = f"{BASKENT_CONFIG['api_url']}/user/login"
        login_data = {
            "mail": BASKENT_CONFIG['username'],
            "password": BASKENT_CONFIG['password']
        }

        response = requests.post(login_url, json=login_data, timeout=10)

        if response.status_code == 200:
            data = response.json()
            BASKENT_API_TOKEN = data.get('apiToken', '')
            logger.info("[BASKENT API] Login successful")
            return True

        logger.error(f"[BASKENT API] Login failed: {response.status_code}")
        return False

    except Exception as e:
        logger.error(f"[BASKENT API] Login error: {e}")
        return False

def send_exchange_to_baskent(transaction_id, dealer_vault_id, currency, amount, is_buy, rate, dealer_name=None):
    """Send exchange transaction to Baskent Enerji API"""
    global BASKENT_API_TOKEN

    try:
        # Determine currency ID
        if currency.upper() == 'USDT':
            currency_id = BASKENT_CONFIG['usdt_currency_id']
        elif currency.upper() == 'RUBLE':
            currency_id = BASKENT_CONFIG['ruble_currency_id']
        else:
            logger.error(f"[BASKENT API] Unknown currency: {currency}")
            return False

        # Use dealer vault ID or default
        vault_id = dealer_vault_id if dealer_vault_id else BASKENT_CONFIG['default_vault_id']

        # Prepare notes
        if dealer_name:
            notes = f"QR üzerinden yapılan işlem : {dealer_name}"
        else:
            notes = f"İşlem ID: {transaction_id}"

        # Prepare exchange data as array
        payload = [{
            "vaultId": vault_id,
            "sourceCurrencyId": currency_id,
            "targetCurrencyId": "cd817762-d3f6-4df2-83bc-8a8f9cb476a2",  # Always TRY
            "sourceAmount": float(amount),
            "isBuyingFromCustomer": bool(is_buy),
            "customRate": float(rate),
            "notes": notes
        }]

        # Send request
        exchange_url = f"{BASKENT_CONFIG['api_url']}/exchange/exchange"
        headers = {
            "Authorization": f"Bearer {BASKENT_API_TOKEN}",
            "Content-Type": "application/json"
        }

        response = requests.post(exchange_url, json=payload, headers=headers, timeout=10)

        # If 401, try to login and retry
        if response.status_code == 401:
            logger.info("[BASKENT API] Token expired, attempting login...")
            if baskent_login():
                # Retry with new token
                headers["Authorization"] = f"Bearer {BASKENT_API_TOKEN}"
                response = requests.post(exchange_url, json=payload, headers=headers, timeout=10)

        if response.status_code in [200, 201]:
            logger.info(f"[BASKENT API] Exchange sent successfully for transaction {transaction_id}")
            return True
        else:
            logger.error(f"[BASKENT API] Exchange failed for transaction {transaction_id}: {response.status_code} - {response.text}")
            return False

    except Exception as e:
        logger.error(f"[BASKENT API] Exchange send error: {e}")
        return False

# Veritabanı bağlantısı
def get_db_connection():
    try:
        conn = pyodbc.connect(CONNECTION_STRING)
        return conn
    except Exception as e:
        logger.error(f"Database connection error: {e}")
        return None

def notify_web_panel(transaction_id):
    """Web panele anlık bildirim gönder"""
    try:
        import requests
        requests.post('http://localhost:5000/api/notify_message',
            json={'transaction_id': transaction_id},
            timeout=0.5)
    except:
        pass

# Ruble işlemini kanala gönder
async def send_ruble_to_channel(context, transaction_id, customer_name, amount_rub, amount_try):
    """Yeni Ruble işlemini kanala gönder"""
    try:
        message = await context.bot.send_message(
            chat_id=CHANNEL_ID,
            text=(
                f"🆕 **YENİ RUBLE İŞLEMİ**\n"
                f"━━━━━━━━━━━━━━━\n"
                f"👤 Müşteri: {customer_name}\n"
                f"💰 Miktar: {amount_rub:,.2f} RUB\n"
                f"💵 TL Karşılığı: {amount_try:,.2f} TL\n"
                f"🔖 İşlem ID: #{transaction_id}\n"
                f"━━━━━━━━━━━━━━━\n"
                f"💡 **Bu mesaja reply atarak banka hesabı bilgilerini gönderin**"
            ),
            parse_mode="Markdown"
        )

        # İşlemi aktif listeye ekle
        active_ruble_transactions[transaction_id] = {
            'message_id': message.message_id,
            'customer_name': customer_name,
            'amount_rub': amount_rub,
            'amount_try': amount_try,
            'status': 'waiting_bank'
        }

        return True
    except Exception as e:
        logger.error(f"Error sending to channel: {e}")
        return False

# Callback handler
async def button_callback(update: Update, context: ContextTypes.DEFAULT_TYPE):
    query = update.callback_query
    await query.answer()

    data = query.data.split("_")

    if data[0] == "provide" and data[1] == "bank":
        transaction_id = int(data[2])
        provider_id = query.from_user.id

        # Sağlayıcı yetkisi kontrolü
        if provider_id not in BANK_PROVIDERS:
            await query.answer("⛔ Bu işlemi yapmaya yetkiniz yok!", show_alert=True)
            return

        # İşlem kontrolü - eğer active_ruble_transactions'da yoksa, veritabanından kontrol et
        if transaction_id not in active_ruble_transactions:
            # Veritabanından işlem bilgilerini al
            conn = get_db_connection()
            if conn:
                cursor = conn.cursor()
                cursor.execute("""
                    SELECT t.amount, t.exchange_rate, c.first_name, t.currency
                    FROM Transactions t
                    JOIN Customers c ON t.customer_id = c.customer_id
                    WHERE t.transaction_id = ? AND t.currency = 'RUBLE'
                """, transaction_id)
                result = cursor.fetchone()
                conn.close()

                if result:
                    # TL tutarını hesapla
                    amount_rub = result[0]
                    exchange_rate = result[1] if result[1] else 0.40  # varsayılan kur
                    amount_try = amount_rub * exchange_rate

                    # İşlemi active_ruble_transactions'a ekle
                    active_ruble_transactions[transaction_id] = {
                        'customer_name': result[2],
                        'amount_rub': amount_rub,
                        'amount_try': amount_try,
                        'status': 'waiting_bank'
                    }
                else:
                    await query.answer("❌ İşlem bulunamadı!", show_alert=True)
                    return
            else:
                await query.answer("❌ Veritabanı bağlantı hatası!", show_alert=True)
                return

        # Banka hesabı bilgisi isteme
        active_ruble_transactions[transaction_id]['provider_id'] = provider_id
        active_ruble_transactions[transaction_id]['status'] = 'waiting_bank_info'

        await query.message.reply_text(
            f"💳 **İşlem #{transaction_id} için banka hesabı bilgilerini gönderin:**\n\n"
            f"Banka bilgilerini istediğiniz formatta gönderebilirsiniz.\n"
            f"Müşteri tam olarak yazdığınız gibi görecek.",
            parse_mode="Markdown"
        )

    elif data[0] == "approve" and data[1] == "payment":
        transaction_id = int(data[2])
        provider_id = query.from_user.id

        # Sağlayıcı yetkisi kontrolü
        if provider_id not in BANK_PROVIDERS:
            await query.answer("⛔ Bu işlemi yapmaya yetkiniz yok!", show_alert=True)
            return

        # İşlemi onayla
        conn = get_db_connection()
        if conn:
            cursor = conn.cursor()
            cursor.execute("""
                UPDATE Transactions
                SET status = 'completed',
                    completed_at = GETDATE()
                WHERE transaction_id = ?
            """, transaction_id)
            conn.commit()

            # Bayi bakiyesinden düşüm yap
            cursor.execute("""
                UPDATE d
                SET d.balance = ISNULL(d.balance, 0) - t.try_amount
                FROM Dealers d
                INNER JOIN Transactions t ON d.dealer_code = t.referral_code
                WHERE t.transaction_id = ?
            """, transaction_id)
            conn.commit()

            # Mevcut completion_code'u al (yoksa yeni oluştur)
            cursor.execute("""
                SELECT completion_code FROM Transactions WHERE transaction_id = ?
            """, transaction_id)
            result = cursor.fetchone()

            if result and result[0]:
                # Mevcut kod varsa onu kullan
                completion_code = result[0]
            else:
                # Yoksa yeni oluştur ve kaydet
                import random
                completion_code = random.randint(10000, 99999)

                cursor.execute("""
                    UPDATE Transactions
                    SET completion_code = ?
                    WHERE transaction_id = ?
                """, str(completion_code), transaction_id)
                conn.commit()

            # Onay mesajını ve işlem kodunu Messages tablosuna da kaydet
            cursor.execute("""
                INSERT INTO Messages (transaction_id, sender_id, sender_type, message_text)
                VALUES (?, ?, 'bank_provider', ?)
            """, transaction_id, provider_id, f"ÖDEME ONAYLANDI - {query.from_user.first_name} - İşlem Kodu: {completion_code}")
            conn.commit()

            # Send to Baskent API
            try:
                # Get dealer vault ID, name, isBuy, and exchange_rate from transaction
                cursor.execute("""
                    SELECT d.vaultId, d.dealer_name, t.isBuy, t.exchange_rate, t.amount
                    FROM Transactions t
                    LEFT JOIN Dealers d ON d.dealer_code = t.referral_code
                    WHERE t.transaction_id = ?
                """, transaction_id)
                api_info = cursor.fetchone()

                if api_info:
                    vault_id, dealer_name, is_buy, exchange_rate, ruble_amount = api_info
                    send_exchange_to_baskent(
                        transaction_id=transaction_id,
                        dealer_vault_id=vault_id,
                        currency='RUBLE',
                        amount=ruble_amount,
                        is_buy=(is_buy == 1) if is_buy is not None else True,
                        rate=exchange_rate if exchange_rate else 0.40,
                        dealer_name=dealer_name
                    )
            except Exception as api_error:
                logger.error(f"[RUBLE BOT] Baskent API error: {api_error}")
                # Continue even if API fails

            conn.close()

            # Kanalda güncelle - mesaj türüne göre güncelle
            try:
                # Önce edit_message_caption dene (eğer document ise)
                await query.edit_message_caption(
                    caption=(
                        f"✅ **İŞLEM TAMAMLANDI**\n"
                        f"━━━━━━━━━━━━━━━\n"
                        f"🔖 İşlem ID: #{transaction_id}\n"
                        f"✔️ Ödeme onaylandı\n"
                        f"👤 Onaylayan: {query.from_user.first_name}\n"
                        f"⏰ {datetime.now().strftime('%H:%M')}"
                    ),
                    parse_mode="Markdown"
                )
            except:
                # Eğer caption değilse, text olarak dene
                await query.edit_message_text(
                    f"✅ **İŞLEM TAMAMLANDI**\n"
                    f"━━━━━━━━━━━━━━━\n"
                    f"🔖 İşlem ID: #{transaction_id}\n"
                    f"✔️ Ödeme onaylandı\n"
                    f"👤 Onaylayan: {query.from_user.first_name}\n"
                    f"⏰ {datetime.now().strftime('%H:%M')}",
                    parse_mode="Markdown"
                )

            # Müşteriye bildirim gönder - yeni bağlantı aç
            conn2 = get_db_connection()
            if conn2:
                cursor2 = conn2.cursor()
                cursor2.execute("""
                    SELECT c.customer_id, t.amount, t.try_amount, t.exchange_rate
                    FROM Transactions t
                    JOIN Customers c ON t.customer_id = c.customer_id
                    WHERE t.transaction_id = ?
                """, transaction_id)
                result = cursor2.fetchone()
                conn2.close()

                if result:
                    customer_id = result[0]
                    ruble_amount = result[1]
                    try_amount = result[2]
                    exchange_rate = result[3]

                    main_bot_token = os.getenv('MAIN_BOT_TOKEN')
                    if main_bot_token and customer_id:
                        from telegram import Bot
                        main_bot = Bot(token=main_bot_token)
                        try:
                            await main_bot.send_message(
                                chat_id=customer_id,
                                text=(
                                    f"{get_text(customer_id, 'transaction_completed')}\n\n"
                                    f"{get_text(customer_id, 'transaction_code', completion_code)}\n\n"
                                    f"{get_text(customer_id, 'transaction_details')}\n"
                                    f"{get_text(customer_id, 'transaction_id_label', transaction_id)}\n"
                                    f"{get_text(customer_id, 'ruble_amount_label', ruble_amount)}\n"
                                    f"{get_text(customer_id, 'try_amount_label', try_amount)}\n"
                                    f"{get_text(customer_id, 'exchange_rate_label', exchange_rate)}\n"
                                    f"{get_text(customer_id, 'status_label')}\n\n"
                                    f"{get_text(customer_id, 'show_code_to_dealer')}\n\n"
                                    f"{get_text(customer_id, 'thank_you')}"
                                ),
                                parse_mode="Markdown"
                            )
                        except Exception as e:
                            logger.error(f"Error sending confirmation to customer: {e}")

            # Web panele anlık bildirim gönder
            notify_web_panel(transaction_id)

            await query.answer("✅ İşlem başarıyla onaylandı!", show_alert=True)

    elif data[0] == "reject" and data[1] == "payment":
        transaction_id = int(data[2])
        provider_id = query.from_user.id

        # Sağlayıcı yetkisi kontrolü
        if provider_id not in BANK_PROVIDERS:
            await query.answer("⛔ Bu işlemi yapmaya yetkiniz yok!", show_alert=True)
            return

        # İşlemi reddet
        conn = get_db_connection()
        if conn:
            cursor = conn.cursor()
            cursor.execute("""
                UPDATE Transactions
                SET status = 'bank_rejected'
                WHERE transaction_id = ?
            """, transaction_id)
            conn.commit()
            conn.close()

            # Kanalda güncelle - mesaj türüne göre güncelle
            try:
                # Önce edit_message_caption dene (eğer document ise)
                await query.edit_message_caption(
                    caption=(
                        f"❌ **ÖDEME REDDEDİLDİ**\n"
                        f"━━━━━━━━━━━━━━━\n"
                        f"🔖 İşlem ID: #{transaction_id}\n"
                        f"❌ Ödeme reddedildi\n"
                        f"👤 Reddeden: {query.from_user.first_name}\n"
                        f"⏰ {datetime.now().strftime('%H:%M')}"
                    ),
                    parse_mode="Markdown"
                )
            except:
                # Eğer caption değilse, text olarak dene
                await query.edit_message_text(
                    f"❌ **ÖDEME REDDEDİLDİ**\n"
                    f"━━━━━━━━━━━━━━━\n"
                    f"🔖 İşlem ID: #{transaction_id}\n"
                    f"❌ Ödeme reddedildi\n"
                    f"👤 Reddeden: {query.from_user.first_name}\n"
                    f"⏰ {datetime.now().strftime('%H:%M')}",
                    parse_mode="Markdown"
                )

            # Müşteriye red bildirimi gönder
            conn2 = get_db_connection()
            if conn2:
                cursor2 = conn2.cursor()
                cursor2.execute("""
                    SELECT customer_id FROM Transactions WHERE transaction_id = ?
                """, transaction_id)
                result = cursor2.fetchone()
                conn2.close()

                if result:
                    customer_id = result[0]
                    main_bot_token = os.getenv('MAIN_BOT_TOKEN')
                    if main_bot_token and customer_id:
                        from telegram import Bot
                        main_bot = Bot(token=main_bot_token)
                        try:
                            await main_bot.send_message(
                                chat_id=customer_id,
                                text=(
                                    f"❌ **ÖDEME REDDEDİLDİ**\n"
                                    f"━━━━━━━━━━━━━━━\n"
                                    f"İşlem ID: #{transaction_id}\n"
                                    f"Ödemeniz reddedildi.\n\n"
                                    f"Lütfen dekontu kontrol edip tekrar gönderin veya destek ile iletişime geçin."
                                ),
                                parse_mode="Markdown"
                            )
                        except Exception as e:
                            logger.error(f"Error sending rejection to customer: {e}")

            # Web panele anlık bildirim gönder
            notify_web_panel(transaction_id)

            await query.answer("❌ Ödeme reddedildi!", show_alert=True)

# Mesaj handler - Banka hesabı bilgisi ve dekont
async def handle_message(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if not update.message:
        return

    user_id = update.message.from_user.id
    text = update.message.text if update.message.text else ""

    # Banka hesabı sağlayıcısı mı kontrol et
    if user_id in BANK_PROVIDERS:
        # Reply to message kontrolü - sadece işlem mesajına reply ise kabul et
        if update.message.reply_to_message:
            original_msg = update.message.reply_to_message.text

            # İşlem ID'yi mesajdan çıkar
            if "İşlem ID: #" in original_msg:
                import re
                match = re.search(r'İşlem ID: #(\d+)', original_msg)
                if match:
                    trans_id = int(match.group(1))

                    # Bu işlem için banka hesabı gönder
                    if trans_id not in active_ruble_transactions:
                        # Veritabanından işlem bilgilerini al
                        conn = get_db_connection()
                        if conn:
                            cursor = conn.cursor()
                            cursor.execute("""
                                SELECT t.amount, t.exchange_rate, c.first_name, c.customer_id
                                FROM Transactions t
                                JOIN Customers c ON t.customer_id = c.customer_id
                                WHERE t.transaction_id = ? AND t.currency = 'RUBLE'
                            """, trans_id)
                            result = cursor.fetchone()

                            if result:
                                amount_rub = result[0]
                                exchange_rate = result[1] if result[1] else 0.40
                                amount_try = amount_rub * exchange_rate
                                customer_name = result[2]
                                customer_id = result[3]

                                # İşlemi active_ruble_transactions'a ekle
                                active_ruble_transactions[trans_id] = {
                                    'customer_name': customer_name,
                                    'customer_id': customer_id,
                                    'amount_rub': amount_rub,
                                    'amount_try': amount_try,
                                    'status': 'bank_provided',
                                    'bank_info': text
                                }

                                # Veritabanında güncelle - sadece status'u güncelle
                                cursor.execute("""
                                    UPDATE Transactions
                                    SET status = 'waiting_payment'
                                    WHERE transaction_id = ?
                                """, trans_id)

                                # Banka bilgilerini Messages tablosuna kaydet
                                cursor.execute("""
                                    INSERT INTO Messages (transaction_id, sender_id, sender_type, message_text)
                                    VALUES (?, ?, 'bank_provider', ?)
                                """, trans_id, user_id, f"BANKA BİLGİLERİ: {text}")

                                conn.commit()

                                # Web panele anlık bildirim gönder
                                try:
                                    import requests
                                    requests.post('http://localhost:5000/api/notify_message',
                                        json={'transaction_id': trans_id},
                                        timeout=0.5)
                                except:
                                    pass

                                # Müşteriye banka hesabı bilgilerini gönder
                                from telegram import Bot
                                main_bot = Bot(token=os.getenv('MAIN_BOT_TOKEN'))

                                # Banka bilgilerini düz metin olarak gönder
                                # Markdown escape karakterlerinden kurtul
                                import html
                                safe_bank_info = html.escape(text)

                                # Her satırı pre-formatted text içinde gönder (tüm metin tek blokta)
                                await main_bot.send_message(
                                    chat_id=customer_id,
                                    text=(
                                        f"{get_text(customer_id, 'bank_warning')}\n"
                                        f"━━━━━━━━━━━━━━━\n\n"
                                        f"{get_text(customer_id, 'bank_info_header')}\n"
                                        f"━━━━━━━━━━━━━━━\n\n"
                                        f"```\n{text}\n```\n\n"
                                        f"━━━━━━━━━━━━━━━\n"
                                        f"{get_text(customer_id, 'copy_instruction')}\n\n"
                                        f"{get_text(customer_id, 'send_pdf_after_bank')}\n"
                                        f"{get_text(customer_id, 'pdf_required')}"
                                    ),
                                    parse_mode="Markdown"
                                )

                                # Kanala onay mesajı
                                await update.message.reply_text(
                                    get_text(customer_id, 'bank_info_sent')
                                )

                                conn.close()
                            else:
                                await update.message.reply_text("❌ İşlem bulunamadı!")
                                conn.close()
                else:
                    await update.message.reply_text("❌ İşlem ID'si bulunamadı!")
            else:
                await update.message.reply_text("❌ Lütfen bir işlem mesajına reply atın!")
        else:
            await update.message.reply_text(
                "💡 **Kullanım:**\n"
                "İşlem mesajına reply atarak banka hesabı bilgilerini gönderin."
            )
        return

# Dosya handler - PDF dekont
async def handle_document(update: Update, context: ContextTypes.DEFAULT_TYPE):
    if not update.message or not update.message.document:
        return

    # Bu fonksiyon müşteriden gelen dekontları işlemek için
    # bot2.py'de işlenecek ve buraya forward edilecek
    pass

# Komutlar
async def start(update: Update, context: ContextTypes.DEFAULT_TYPE):
    user_id = update.message.from_user.id

    if user_id in BANK_PROVIDERS:
        await update.message.reply_text(
            "🏦 **Ruble İşlem Botu**\n\n"
            "Hoş geldiniz! Siz yetkili banka hesabı sağlayıcısısınız.\n\n"
            "📌 Kanalda yeni işlemler görünecek\n"
            "💳 'Banka Hesabı Ver' butonuna tıklayarak hesap bilgisi verebilirsiniz\n"
            "✅ Dekont geldiğinde onaylama yapabilirsiniz\n\n"
            "📝 **Komutlar:**\n"
            "/getid - Kendi ID'nizi öğrenin\n"
            "/list_providers - Tüm sağlayıcıları listele\n"
            "/add_provider [id] - Yeni sağlayıcı ekle (admin)\n"
            "/remove_provider [id] - Sağlayıcı kaldır (admin)",
            parse_mode="Markdown"
        )
    else:
        await update.message.reply_text(
            "⛔ Bu bot sadece yetkili banka hesabı sağlayıcıları için kullanılabilir.\n\n"
            "/getid komutunu kullanarak ID'nizi öğrenebilirsiniz."
        )

async def get_id(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Kullanıcının ID'sini göster"""
    user = update.message.from_user
    user_id = user.id
    username = user.username or "Yok"
    first_name = user.first_name

    is_provider = "✅ Evet" if user_id in BANK_PROVIDERS else "❌ Hayır"

    message = (
        f"👤 **Bilgileriniz:**\n\n"
        f"🆔 ID: `{user_id}`\n"
        f"📝 Ad: {first_name}\n"
        f"📱 Username: @{username}\n"
        f"🏦 Banka Sağlayıcısı: {is_provider}\n\n"
    )

    if user_id not in BANK_PROVIDERS:
        message += f"💡 Banka sağlayıcısı olmak için bu ID'yi yöneticiye bildirin: `{user_id}`"

    await update.message.reply_text(message, parse_mode="Markdown")

    # Konsola da yazdır
    print(f"\n{'='*50}")
    print(f"ID REQUEST:")
    print(f"Name: {first_name}")
    print(f"ID: {user_id}")
    print(f"Username: @{username}")
    print(f"Bank Provider: {is_provider}")
    print(f"{'='*50}\n")

async def add_provider(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Yeni banka hesabı sağlayıcısı ekle (sadece admin)"""
    admin_id = update.message.from_user.id

    # Sadece ilk sağlayıcı (admin) ekleyebilir
    if admin_id != 7462722250:
        await update.message.reply_text("⛔ Bu komutu kullanma yetkiniz yok!")
        return

    if not context.args:
        await update.message.reply_text("Kullanım: /add_provider [user_id]")
        return

    try:
        new_provider_id = int(context.args[0])
        if new_provider_id not in BANK_PROVIDERS:
            BANK_PROVIDERS.append(new_provider_id)
            await update.message.reply_text(
                f"✅ Yeni sağlayıcı eklendi!\n\n"
                f"🆔 ID: {new_provider_id}\n"
                f"📊 Toplam sağlayıcı sayısı: {len(BANK_PROVIDERS)}"
            )
        else:
            await update.message.reply_text("⚠️ Bu kullanıcı zaten sağlayıcı listesinde!")
    except ValueError:
        await update.message.reply_text("❌ Geçersiz kullanıcı ID!")

async def remove_provider(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Banka hesabı sağlayıcısını kaldır (sadece admin)"""
    admin_id = update.message.from_user.id

    # Sadece ilk sağlayıcı (admin) kaldırabilir
    if admin_id != 7462722250:
        await update.message.reply_text("⛔ Bu komutu kullanma yetkiniz yok!")
        return

    if not context.args:
        await update.message.reply_text("Kullanım: /remove_provider [user_id]")
        return

    try:
        provider_id = int(context.args[0])

        # Admin kendisini silemez
        if provider_id == 7462722250:
            await update.message.reply_text("⚠️ Admin silinemez!")
            return

        if provider_id in BANK_PROVIDERS:
            BANK_PROVIDERS.remove(provider_id)
            await update.message.reply_text(
                f"✅ Sağlayıcı kaldırıldı!\n\n"
                f"🆔 ID: {provider_id}\n"
                f"📊 Kalan sağlayıcı sayısı: {len(BANK_PROVIDERS)}"
            )
        else:
            await update.message.reply_text("❌ Bu kullanıcı sağlayıcı listesinde değil!")
    except ValueError:
        await update.message.reply_text("❌ Geçersiz kullanıcı ID!")

async def list_providers(update: Update, context: ContextTypes.DEFAULT_TYPE):
    """Tüm banka hesabı sağlayıcılarını listele"""
    user_id = update.message.from_user.id

    # Sadece sağlayıcılar görebilir
    if user_id not in BANK_PROVIDERS:
        await update.message.reply_text("⛔ Bu komutu kullanma yetkiniz yok!")
        return

    message = "🏦 **Aktif Banka Hesabı Sağlayıcıları:**\n\n"

    for i, provider_id in enumerate(BANK_PROVIDERS, 1):
        is_admin = " 👑 (Admin)" if provider_id == 7462722250 else ""
        is_you = " 👤 (Siz)" if provider_id == user_id else ""
        message += f"{i}. `{provider_id}`{is_admin}{is_you}\n"

    message += f"\n📊 Toplam: {len(BANK_PROVIDERS)} sağlayıcı"

    await update.message.reply_text(message, parse_mode="Markdown")

# Main function
def main():
    # Terminal başlığını ayarla
    import sys
    if sys.platform == "win32":
        os.system("title RUBLE KANAL BOTU - Money Transfer Turkey")

    print("\n" + "="*60)
    print("RUBLE KANAL BOTU - MONEY TRANSFER TURKEY")
    print("="*60)
    print("Ruble Channel Bot starting...")

    if not BOT_TOKEN:
        print("RUBLE_BOT_TOKEN not defined in .env file!")
        print("Please add to .env file:")
        print("RUBLE_BOT_TOKEN=your_bot_token_here")
        return

    if not CHANNEL_ID:
        print("RUBLE_CHANNEL_ID not defined in .env file!")
        print("Please add to .env file:")
        print("RUBLE_CHANNEL_ID=-100xxxxxxxxxx")
        return

    # Bot oluştur
    app = Application.builder().token(BOT_TOKEN).build()

    # Handlers
    app.add_handler(CommandHandler("start", start))
    app.add_handler(CommandHandler("getid", get_id))
    app.add_handler(CommandHandler("add_provider", add_provider))
    app.add_handler(CommandHandler("remove_provider", remove_provider))
    app.add_handler(CommandHandler("list_providers", list_providers))
    app.add_handler(CallbackQueryHandler(button_callback))
    app.add_handler(MessageHandler(filters.TEXT & ~filters.COMMAND, handle_message))
    app.add_handler(MessageHandler(filters.Document.PDF, handle_document))

    print(f"Bot started successfully!")
    print(f"Channel ID: {CHANNEL_ID}")
    print(f"Bank Providers: {BANK_PROVIDERS}")

    # Bot'u başlat
    app.run_polling(allowed_updates=Update.ALL_TYPES)

if __name__ == "__main__":
    main()