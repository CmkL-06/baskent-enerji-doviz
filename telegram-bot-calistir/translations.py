"""
Çok Dilli Çeviri Modülü — 4 Dil (TR, EN, RU, DE)
Rusça çeviriler doğal dilbilgisi kurallarına uygun yazılmıştır.
"""


def t(key, lang='tr', **kwargs):
    """
    Çeviri getir, format parametreleri uygula.
    Kullanım: t('welcome', 'ru', name='Ahmet')
    """
    entry = TEXTS.get(key)
    if not entry:
        return key
    text = entry.get(lang) or entry.get('tr', key)
    if kwargs:
        try:
            text = text.format(**kwargs)
        except (KeyError, IndexError):
            pass
    return text


# ═══════════════════════════════════════════════════════════════
# ANA METİNLER — Her anahtar 4 dilde
# ═══════════════════════════════════════════════════════════════

TEXTS = {

    # ──────────────────────────────────────
    # KARŞILAMA & GİRİŞ
    # ──────────────────────────────────────

    'welcome': {
        'tr': "🎉 Money Transfer Turkey'ye hoş geldiniz!",
        'en': "🎉 Welcome to Money Transfer Turkey!",
        'ru': "🎉 Добро пожаловать в Money Transfer Turkey!",
        'de': "🎉 Willkommen bei Money Transfer Turkey!",
    },

    'main_menu': {
        'tr': "Ana menüye hoş geldiniz. Yeni işlem başlatmak için butona tıklayın:",
        'en': "Welcome to main menu. Click the button to start a new transaction:",
        'ru': "Добро пожаловать в главное меню. Нажмите кнопку, чтобы начать новую операцию:",
        'de': "Willkommen im Hauptmenü. Klicken Sie auf die Schaltfläche, um eine neue Transaktion zu starten:",
    },

    'group_check_failed': {
        'tr': "❌ Bu botu kullanmak için önce gruba katılmalısınız!\n👉 @MoneyTransferTurkeyOfficial",
        'en': "❌ You must join our group first to use this bot!\n👉 @MoneyTransferTurkeyOfficial",
        'ru': "❌ Чтобы использовать этого бота, сначала вступите в нашу группу!\n👉 @MoneyTransferTurkeyOfficial",
        'de': "❌ Sie müssen zuerst unserer Gruppe beitreten!\n👉 @MoneyTransferTurkeyOfficial",
    },

    'qr_required': {
        'tr': "🚫 İşlem Yapılamaz!\nPara transferi yapmak için ofisteki QR kodunu okutmanız gerekmektedir.",
        'en': "🚫 Transaction Not Available!\nYou need to scan the QR code at the office to make a money transfer.",
        'ru': "🚫 Операция недоступна!\nДля перевода денег необходимо отсканировать QR-код в офисе.",
        'de': "🚫 Transaktion nicht verfügbar!\nSie müssen den QR-Code im Büro scannen, um Geld zu überweisen.",
    },

    'invalid_qr': {
        'tr': "❌ **Geçersiz QR Kod!**\n\nBu QR kod sistemde kayıtlı değil.\nLütfen geçerli bir QR kod okutunuz.",
        'en': "❌ **Invalid QR Code!**\n\nThis QR code is not registered in the system.\nPlease scan a valid QR code.",
        'ru': "❌ **Недействительный QR-код!**\n\nЭтот QR-код не зарегистрирован в системе.\nПожалуйста, отсканируйте действующий QR-код.",
        'de': "❌ **Ungültiger QR-Code!**\n\nDieser QR-Code ist nicht im System registriert.\nBitte scannen Sie einen gültigen QR-Code.",
    },

    'dealer_inactive': {
        'tr': "⚠️ **Bu Bayi Aktif Değil!**\n\nBu bayi şu anda aktif değildir.\nLütfen aktif bir bayiden QR kod okutunuz.",
        'en': "⚠️ **This Dealer is Inactive!**\n\nThis dealer is currently inactive.\nPlease scan a QR code from an active dealer.",
        'ru': "⚠️ **Этот дилер неактивен!**\n\nВ данный момент дилер не работает.\nПожалуйста, отсканируйте QR-код у активного дилера.",
        'de': "⚠️ **Dieser Händler ist inaktiv!**\n\nDieser Händler ist derzeit inaktiv.\nBitte scannen Sie einen QR-Code von einem aktiven Händler.",
    },

    # ──────────────────────────────────────
    # PARA BİRİMİ SEÇİMİ
    # ──────────────────────────────────────

    'choose_currency': {
        'tr': "Para birimi seçin:",
        'en': "Choose currency:",
        'ru': "Выберите валюту:",
        'de': "Währung wählen:",
    },

    'btn_sell_usdt': {
        'tr': "💵 USDT Sat",
        'en': "💵 Sell USDT",
        'ru': "💵 Продать USDT",
        'de': "💵 USDT verkaufen",
    },

    'btn_sell_ruble': {
        'tr': "💶 Ruble Gönder",
        'en': "💶 Send Ruble",
        'ru': "💶 Отправить рубли",
        'de': "💶 Rubel senden",
    },

    # ──────────────────────────────────────
    # MİKTAR GİRİŞİ
    # ──────────────────────────────────────

    'enter_amount': {
        'tr': "💵 Göndermek istediğiniz {currency} miktarını yazın:",
        'en': "💵 Enter the {currency} amount you want to send:",
        'ru': "💵 Введите сумму в {currency}, которую хотите отправить:",
        'de': "💵 Geben Sie den {currency}-Betrag ein, den Sie senden möchten:",
    },

    'enter_amount_prompt': {
        'tr': "💵 Ne kadar {currency} göndermek istiyorsunuz?\n\n📊 Güncel Kur: 1 {currency} = {rate} TL\n{min_line}\n{example_line}",
        'en': "💵 How much {currency} would you like to send?\n\n📊 Current Rate: 1 {currency} = {rate} TL\n{min_line}\n{example_line}",
        'ru': "💵 Сколько {currency} вы хотите отправить?\n\n📊 Текущий курс: 1 {currency} = {rate} TL\n{min_line}\n{example_line}",
        'de': "💵 Wie viel {currency} möchten Sie senden?\n\n📊 Aktueller Kurs: 1 {currency} = {rate} TL\n{min_line}\n{example_line}",
    },

    'minimum_amount': {
        'tr': "Minimum: {amount} {currency}",
        'en': "Minimum: {amount} {currency}",
        'ru': "Минимум: {amount} {currency}",
        'de': "Minimum: {amount} {currency}",
    },

    'example_amount': {
        'tr': "Örnek: {amount} {currency} = {try_amount} TL",
        'en': "Example: {amount} {currency} = {try_amount} TL",
        'ru': "Пример: {amount} {currency} = {try_amount} TL",
        'de': "Beispiel: {amount} {currency} = {try_amount} TL",
    },

    'invalid_amount': {
        'tr': "❌ Geçersiz miktar! Lütfen sadece sayı girin.",
        'en': "❌ Invalid amount! Please enter numbers only.",
        'ru': "❌ Неверная сумма! Пожалуйста, введите только число.",
        'de': "❌ Ungültiger Betrag! Bitte nur Zahlen eingeben.",
    },

    'minimum_usdt_error': {
        'tr': "❌ **Minimum tutar 10 USDT'dir!**\n\nLütfen en az 10 USDT veya daha fazla bir miktar girin.",
        'en': "❌ **Minimum amount is 10 USDT!**\n\nPlease enter at least 10 USDT or more.",
        'ru': "❌ **Минимальная сумма — 10 USDT!**\n\nПожалуйста, введите не менее 10 USDT.",
        'de': "❌ **Mindestbetrag ist 10 USDT!**\n\nBitte geben Sie mindestens 10 USDT oder mehr ein.",
    },

    'minimum_ruble_error': {
        'tr': "❌ **Minimum tutar 5000 RUB'dir!**\n\nLütfen en az 5000 RUB veya daha fazla bir miktar girin.",
        'en': "❌ **Minimum amount is 5000 RUB!**\n\nPlease enter at least 5000 RUB or more.",
        'ru': "❌ **Минимальная сумма — 5000 ₽!**\n\nПожалуйста, введите не менее 5000 рублей.",
        'de': "❌ **Mindestbetrag ist 5000 RUB!**\n\nBitte geben Sie mindestens 5000 RUB oder mehr ein.",
    },

    # ──────────────────────────────────────
    # İŞLEM ONAY & ÖZET
    # ──────────────────────────────────────

    'confirm_transaction': {
        'tr': "📋 **İşlem Özeti**\n\n{details}\n\nİşlemi onaylıyor musunuz?",
        'en': "📋 **Transaction Summary**\n\n{details}\n\nDo you confirm the transaction?",
        'ru': "📋 **Сводка операции**\n\n{details}\n\nПодтверждаете операцию?",
        'de': "📋 **Transaktionsübersicht**\n\n{details}\n\nBestätigen Sie die Transaktion?",
    },

    'current_rate': {
        'tr': "📊 Güncel Kur: 1 {currency} = {rate} TL",
        'en': "📊 Current Rate: 1 {currency} = {rate} TL",
        'ru': "📊 Текущий курс: 1 {currency} = {rate} TL",
        'de': "📊 Aktueller Kurs: 1 {currency} = {rate} TL",
    },

    'amount_to_send': {
        'tr': "💸 Gönderilecek: {amount} {currency}",
        'en': "💸 Amount to send: {amount} {currency}",
        'ru': "💸 К отправке: {amount} {currency}",
        'de': "💸 Zu senden: {amount} {currency}",
    },

    'amount_in_try': {
        'tr': "💰 TL Karşılığı: {amount} TL",
        'en': "💰 TL Equivalent: {amount} TL",
        'ru': "💰 Эквивалент в TL: {amount} TL",
        'de': "💰 TL Gegenwert: {amount} TL",
    },

    'exchange_rate': {
        'tr': "📈 Kur: {rate}",
        'en': "📈 Exchange rate: {rate}",
        'ru': "📈 Курс: {rate}",
        'de': "📈 Wechselkurs: {rate}",
    },

    'summary': {
        'tr': "📋 Özet:",
        'en': "📋 Summary:",
        'ru': "📋 Сводка:",
        'de': "📋 Zusammenfassung:",
    },

    'currency_label': {
        'tr': "• Para Birimi: {currency}",
        'en': "• Currency: {currency}",
        'ru': "• Валюта: {currency}",
        'de': "• Währung: {currency}",
    },

    'amount_label': {
        'tr': "• Miktar: {amount} {currency}",
        'en': "• Amount: {amount} {currency}",
        'ru': "• Сумма: {amount} {currency}",
        'de': "• Betrag: {amount} {currency}",
    },

    'try_equivalent': {
        'tr': "• TL Karşılığı: {amount} TL",
        'en': "• TL Equivalent: {amount} TL",
        'ru': "• Эквивалент в TL: {amount} TL",
        'de': "• TL Gegenwert: {amount} TL",
    },

    'processing_transaction': {
        'tr': "⏳ İşleminiz işleniyor...",
        'en': "⏳ Processing your transaction...",
        'ru': "⏳ Обработка вашей операции...",
        'de': "⏳ Ihre Transaktion wird bearbeitet...",
    },

    'transaction_created': {
        'tr': "✅ İşleminiz oluşturuldu!",
        'en': "✅ Transaction created!",
        'ru': "✅ Операция создана!",
        'de': "✅ Transaktion erstellt!",
    },

    # ──────────────────────────────────────
    # USDT AKIŞI
    # ──────────────────────────────────────

    'usdt_send_info': {
        'tr': "💳 **USDT GÖNDERİM BİLGİLERİ:**",
        'en': "💳 **USDT SENDING INFORMATION:**",
        'ru': "💳 **РЕКВИЗИТЫ ДЛЯ ОТПРАВКИ USDT:**",
        'de': "💳 **USDT-SENDEINFORMATIONEN:**",
    },

    'network_label': {
        'tr': "Ağ (Network): {network}",
        'en': "Network: {network}",
        'ru': "Сеть: {network}",
        'de': "Netzwerk: {network}",
    },

    'address_label': {
        'tr': "Adres: `{address}`",
        'en': "Address: `{address}`",
        'ru': "Адрес: `{address}`",
        'de': "Adresse: `{address}`",
    },

    'amount_usdt': {
        'tr': "Miktar: {amount} USDT",
        'en': "Amount: {amount} USDT",
        'ru': "Сумма: {amount} USDT",
        'de': "Betrag: {amount} USDT",
    },

    'warning_network': {
        'tr': "⚠️ **DİKKAT:** Doğru network'ü seçtiğinizden emin olun!",
        'en': "⚠️ **WARNING:** Make sure you select the correct network!",
        'ru': "⚠️ **ВНИМАНИЕ:** Убедитесь, что выбрали правильную сеть!",
        'de': "⚠️ **WARNUNG:** Stellen Sie sicher, dass Sie das richtige Netzwerk auswählen!",
    },

    'send_usdt': {
        'tr': "📤 **USDT'yi yukarıdaki adrese gönderin.**",
        'en': "📤 **Send USDT to the address above.**",
        'ru': "📤 **Отправьте USDT на указанный выше адрес.**",
        'de': "📤 **Senden Sie USDT an die obige Adresse.**",
    },

    'send_txid': {
        'tr': "✅ **Transfer yaptıktan sonra TXID (İşlem Hash'i) buraya gönderin.**",
        'en': "✅ **After making the transfer, send the TXID (Transaction Hash) here.**",
        'ru': "✅ **После перевода отправьте сюда TXID (хеш транзакции).**",
        'de': "✅ **Nach der Überweisung senden Sie die TXID (Transaktions-Hash) hier.**",
    },

    'example_txid': {
        'tr': "Örnek TXID: `0xa1b2c3d4...` veya `297834540333`",
        'en': "Example TXID: `0xa1b2c3d4...` or `297834540333`",
        'ru': "Пример TXID: `0xa1b2c3d4...` или `297834540333`",
        'de': "Beispiel TXID: `0xa1b2c3d4...` oder `297834540333`",
    },

    'invalid_txid': {
        'tr': "❌ Geçersiz TXID! Lütfen geçerli bir işlem hash'i girin.\nÖrnek: `0xa1b2c3d4...` veya işlem numarası",
        'en': "❌ Invalid TXID! Please enter a valid transaction hash.\nExample: `0xa1b2c3d4...` or transaction number",
        'ru': "❌ Неверный TXID! Пожалуйста, введите корректный хеш транзакции.\nПример: `0xa1b2c3d4...` или номер транзакции",
        'de': "❌ Ungültige TXID! Bitte geben Sie einen gültigen Transaktions-Hash ein.\nBeispiel: `0xa1b2c3d4...` oder Transaktionsnummer",
    },

    'waiting_txid': {
        'tr': "⏳ TXID doğrulanıyor, lütfen bekleyin...",
        'en': "⏳ Verifying TXID, please wait...",
        'ru': "⏳ Проверка TXID, пожалуйста, подождите...",
        'de': "⏳ TXID wird verifiziert, bitte warten...",
    },

    'txid_verification_failed': {
        'tr': "❌ TXID doğrulanamadı!",
        'en': "❌ TXID could not be verified!",
        'ru': "❌ Не удалось подтвердить TXID!",
        'de': "❌ TXID konnte nicht verifiziert werden!",
    },

    'txid_amount_mismatch': {
        'tr': "❌ **TXID Doğrulanamadı!**\n\n⚠️ **Tutar Uyuşmazlığı:**\n• Beklenen: {expected} USDT\n• Gönderilen: {sent} USDT\n\n📌 Lütfen doğru miktarda USDT gönderdiğinizden emin olun.",
        'en': "❌ **TXID Verification Failed!**\n\n⚠️ **Amount Mismatch:**\n• Expected: {expected} USDT\n• Sent: {sent} USDT\n\n📌 Please make sure you send the correct amount of USDT.",
        'ru': "❌ **Ошибка проверки TXID!**\n\n⚠️ **Несовпадение суммы:**\n• Ожидалось: {expected} USDT\n• Отправлено: {sent} USDT\n\n📌 Пожалуйста, убедитесь, что вы отправили правильную сумму USDT.",
        'de': "❌ **TXID-Überprüfung fehlgeschlagen!**\n\n⚠️ **Betragsabweichung:**\n• Erwartet: {expected} USDT\n• Gesendet: {sent} USDT\n\n📌 Bitte stellen Sie sicher, dass Sie den richtigen USDT-Betrag senden.",
    },

    'offchain_detected': {
        'tr': "⚠️ **Off-chain transfer tespit edildi!**\n\n📌 Binance'de işlem ID'si hemen görünmeyebilir.\n\nLütfen şunu yapın:\n1️⃣ 2-3 dakika bekleyin\n2️⃣ Binance'de sayfayı yenileyin\n3️⃣ TXID numarası göründüğünde tekrar gönderin",
        'en': "⚠️ **Off-chain transfer detected!**\n\n📌 Transaction ID may not appear immediately on Binance.\n\nPlease do the following:\n1️⃣ Wait 2-3 minutes\n2️⃣ Refresh the page on Binance\n3️⃣ Send again when TXID appears",
        'ru': "⚠️ **Обнаружен внутренний перевод (off-chain)!**\n\n📌 Идентификатор транзакции может не отобразиться сразу на Binance.\n\nВыполните следующее:\n1️⃣ Подождите 2–3 минуты\n2️⃣ Обновите страницу на Binance\n3️⃣ Отправьте TXID повторно, когда он появится",
        'de': "⚠️ **Off-Chain-Transfer erkannt!**\n\n📌 Die Transaktions-ID wird möglicherweise nicht sofort auf Binance angezeigt.\n\nBitte machen Sie Folgendes:\n1️⃣ Warten Sie 2-3 Minuten\n2️⃣ Aktualisieren Sie die Seite auf Binance\n3️⃣ Fügen Sie erneut ein, wenn TXID angezeigt wird",
    },

    'usdt_transfer_waiting': {
        'tr': "⚠️ **USDT Transfer Bekleniyor!**\n\nLütfen Binance'den aldığınız TXID numarasını gönderin.\n\n💡 Örnek: `0xa1b2c3d4...` veya `297834540333`",
        'en': "⚠️ **USDT Transfer Pending!**\n\nPlease paste the TXID number you received from Binance.\n\n💡 Example: `0xa1b2c3d4...` or `297834540333`",
        'ru': "⚠️ **Ожидается перевод USDT!**\n\nПожалуйста, введите номер TXID, полученный на Binance.\n\n💡 Пример: `0xa1b2c3d4...` или `297834540333`",
        'de': "⚠️ **USDT-Übertragung ausstehend!**\n\nBitte fügen Sie die TXID-Nummer ein, die Sie von Binance erhalten haben.\n\n💡 Beispiel: `0xa1b2c3d4...` oder `297834540333`",
    },

    'pending_usdt_transaction': {
        'tr': "⚠️ **Bekleyen USDT İşleminiz Var!**\n\n📌 Miktar: {amount} USDT\n🔑 İşlem Kodu: {code}\n💳 Ağ: {network}\n📬 Adres: `{address}`\n\nLütfen bu adrese {amount} USDT gönderip TXID numarasını buraya yazın.\n\n💡 Örnek: `0xa1b2c3d4...` veya `297834540333`",
        'en': "⚠️ **You have a pending USDT transaction!**\n\n📌 Amount: {amount} USDT\n🔑 Transaction Code: {code}\n💳 Network: {network}\n📬 Address: `{address}`\n\nPlease send {amount} USDT to this address and paste the TXID.\n\n💡 Example: `0xa1b2c3d4...` or `297834540333`",
        'ru': "⚠️ **У вас есть незавершённая операция USDT!**\n\n📌 Сумма: {amount} USDT\n🔑 Код операции: {code}\n💳 Сеть: {network}\n📬 Адрес: `{address}`\n\nОтправьте {amount} USDT на этот адрес и введите номер TXID.\n\n💡 Пример: `0xa1b2c3d4...` или `297834540333`",
        'de': "⚠️ **Sie haben eine ausstehende USDT-Transaktion!**\n\n📌 Betrag: {amount} USDT\n🔑 Transaktionscode: {code}\n💳 Netzwerk: {network}\n📬 Adresse: `{address}`\n\nBitte senden Sie {amount} USDT an diese Adresse und fügen Sie die TXID ein.\n\n💡 Beispiel: `0xa1b2c3d4...` oder `297834540333`",
    },

    'transaction_being_verified': {
        'tr': "⏳ **İŞLEM ONAYLANIYOR**\n\n📊 Durum: {confirmations}/10 onay\n{progress_bar}\n\n• TXID: `{txid}`\n\n⚠️ İşleminiz hâlâ onaylanıyor...\n📱 Birkaç dakika sonra tekrar /status yazarak kontrol edin.",
        'en': "⏳ **TRANSACTION BEING VERIFIED**\n\n📊 Status: {confirmations}/10 confirmations\n{progress_bar}\n\n• TXID: `{txid}`\n\n⚠️ Your transaction is still being confirmed...\n📱 Check again in a few minutes using /status.",
        'ru': "⏳ **ОПЕРАЦИЯ ПОДТВЕРЖДАЕТСЯ**\n\n📊 Статус: {confirmations}/10 подтверждений\n{progress_bar}\n\n• TXID: `{txid}`\n\n⚠️ Ваша операция ещё подтверждается...\n📱 Проверьте снова через несколько минут командой /status.",
        'de': "⏳ **TRANSAKTION WIRD VERIFIZIERT**\n\n📊 Status: {confirmations}/10 Bestätigungen\n{progress_bar}\n\n• TXID: `{txid}`\n\n⚠️ Ihre Transaktion wird noch bestätigt...\n📱 Prüfen Sie in einigen Minuten mit /status erneut.",
    },

    # ──────────────────────────────────────
    # RUBLE AKIŞI
    # ──────────────────────────────────────

    'bank_info_preparing': {
        'tr': "⏳ **Banka hesap bilgileri hazırlanıyor...**\nBirkaç dakika içinde size banka hesap bilgileri gönderilecek.",
        'en': "⏳ **Bank account information is being prepared...**\nBank account information will be sent to you in a few minutes.",
        'ru': "⏳ **Банковские реквизиты подготавливаются...**\nРеквизиты для оплаты будут отправлены вам в течение нескольких минут.",
        'de': "⏳ **Bankkontoinformationen werden vorbereitet...**\nBankkontoinformationen werden Ihnen in wenigen Minuten zugesendet.",
    },

    'bank_info_header': {
        'tr': "💳 **BANKA HESABI BİLGİLERİ**",
        'en': "💳 **BANK ACCOUNT INFORMATION**",
        'ru': "💳 **БАНКОВСКИЕ РЕКВИЗИТЫ**",
        'de': "💳 **BANKKONTOINFORMATIONEN**",
    },

    'bank_info_copy_hint': {
        'tr': "💡 Her satıra tıklayarak kopyalayabilirsiniz.",
        'en': "💡 Click on each line to copy.",
        'ru': "💡 Нажмите на строку, чтобы скопировать.",
        'de': "💡 Klicken Sie auf jede Zeile zum Kopieren.",
    },

    'bank_send_warning': {
        'tr': "⚠️ **Parayı sadece belirtilen hesaba gönderin!**",
        'en': "⚠️ **Send money only to the specified account!**",
        'ru': "⚠️ **Отправляйте деньги строго на указанный счёт!**",
        'de': "⚠️ **Senden Sie Geld nur an das angegebene Konto!**",
    },

    'send_pdf_receipt': {
        'tr': "📎 **Lütfen havale/EFT dekontunuzu PDF olarak gönderin.**\n\n⚠️ Dekont PDF formatında olmalıdır.\n❗ Mesaj yerine PDF dosyası gönderin.",
        'en': "📎 **Please send your bank transfer receipt as PDF.**\n\n⚠️ Receipt must be in PDF format.\n❗ Send PDF file instead of message.",
        'ru': "📎 **Пожалуйста, отправьте квитанцию о банковском переводе в формате PDF.**\n\n⚠️ Квитанция должна быть в формате PDF.\n❗ Отправьте PDF-файл, а не текстовое сообщение.",
        'de': "📎 **Bitte senden Sie Ihre Überweisungsquittung als PDF.**\n\n⚠️ Quittung muss im PDF-Format sein.\n❗ Senden Sie die PDF-Datei anstelle einer Nachricht.",
    },

    'send_pdf_after_bank': {
        'tr': "📎 Lütfen havale/EFT dekontunuzu PDF olarak gönderin.",
        'en': "📎 Please send your bank transfer receipt as PDF.",
        'ru': "📎 Пожалуйста, отправьте квитанцию о переводе в формате PDF.",
        'de': "📎 Bitte senden Sie Ihre Überweisungsquittung als PDF.",
    },

    'receipt_sent_success': {
        'tr': "✅ Dekont başarıyla gönderildi!\nBanka hesabı yöneticisi kontrol edip size dönecektir.",
        'en': "✅ Receipt sent successfully!\nThe bank account administrator will check and get back to you.",
        'ru': "✅ Квитанция успешно отправлена!\nАдминистратор проверит и свяжется с вами.",
        'de': "✅ Quittung erfolgreich gesendet!\nDer Kontoverwalter wird prüfen und sich bei Ihnen melden.",
    },

    'receipt_upload_error': {
        'tr': "⚠️ Dekontunuz alındı ancak kanala gönderilemedi.\nOperatör tarafından kontrol edilecektir.",
        'en': "⚠️ Receipt received but could not be sent to channel.\nIt will be checked by the operator.",
        'ru': "⚠️ Квитанция получена, но не удалось отправить в канал.\nОператор проверит её вручную.",
        'de': "⚠️ Quittung erhalten, konnte aber nicht an den Kanal gesendet werden.\nEs wird vom Operator überprüft.",
    },

    'pdf_only_for_ruble': {
        'tr': "❌ Lütfen PDF dekont gönderin.\nSadece PDF formatında dekont kabul edilmektedir.",
        'en': "❌ Please send PDF receipt.\nOnly PDF format receipts are accepted.",
        'ru': "❌ Пожалуйста, отправьте квитанцию в формате PDF.\nПринимаются только квитанции в формате PDF.",
        'de': "❌ Bitte senden Sie die Quittung als PDF.\nEs werden nur Quittungen im PDF-Format akzeptiert.",
    },

    'pdf_instead_of_message': {
        'tr': "❌ Lütfen PDF dekont gönderin.\nMesaj değil, PDF dosyası göndermeniz gerekiyor.",
        'en': "❌ Please send PDF receipt.\nYou need to send a PDF file, not a message.",
        'ru': "❌ Пожалуйста, отправьте PDF-файл квитанции.\nНеобходимо отправить файл, а не текстовое сообщение.",
        'de': "❌ Bitte senden Sie die PDF-Quittung.\nSie müssen eine PDF-Datei senden, keine Nachricht.",
    },

    # ──────────────────────────────────────
    # OPERATÖR BAĞLANTISI
    # ──────────────────────────────────────

    'connecting_operator': {
        'tr': "🔄 Operatöre bağlanıyorsunuz...",
        'en': "🔄 Connecting to operator...",
        'ru': "🔄 Подключение к оператору...",
        'de': "🔄 Verbindung zum Operator wird hergestellt...",
    },

    'connected_operator': {
        'tr': "✅ Operatörle bağlandınız! Artık direkt operatörle konuşuyorsunuz.",
        'en': "✅ Connected to operator! You are now talking directly with the operator.",
        'ru': "✅ Вы подключены к оператору! Теперь вы общаетесь напрямую с оператором.",
        'de': "✅ Mit Operator verbunden! Sie sprechen jetzt direkt mit dem Operator.",
    },

    'finding_operator': {
        'tr': "🔍 Size uygun operatör aranıyor...",
        'en': "🔍 Finding a suitable operator for you...",
        'ru': "🔍 Подбираем для вас подходящего оператора...",
        'de': "🔍 Suche nach einem geeigneten Operator...",
    },

    'operator_assigned': {
        'tr': "✅ Operatör atandı! Lütfen bekleyin...",
        'en': "✅ Operator assigned! Please wait...",
        'ru': "✅ Оператор назначен! Пожалуйста, подождите...",
        'de': "✅ Operator zugewiesen! Bitte warten...",
    },

    'message_sent_to_operator': {
        'tr': "✅ Mesajınız operatöre iletildi.",
        'en': "✅ Your message has been sent to the operator.",
        'ru': "✅ Ваше сообщение передано оператору.",
        'de': "✅ Ihre Nachricht wurde an den Operator gesendet.",
    },

    'no_operator_yet': {
        'tr': "⚠️ Henüz bir operatör atanmamış. Lütfen bekleyin.",
        'en': "⚠️ No operator assigned yet. Please wait.",
        'ru': "⚠️ Оператор ещё не назначен. Пожалуйста, подождите.",
        'de': "⚠️ Noch kein Operator zugewiesen. Bitte warten.",
    },

    'request_received': {
        'tr': "✅ Talebiniz alındı!",
        'en': "✅ Your request has been received!",
        'ru': "✅ Ваш запрос принят!",
        'de': "✅ Ihre Anfrage wurde erhalten!",
    },

    'operator_will_contact': {
        'tr': "⏳ Bir operatör size en kısa sürede dönüş yapacak.\n📝 Operatör bağlandığında bu sohbetten yazışabilirsiniz.",
        'en': "⏳ An operator will get back to you as soon as possible.\n📝 You can chat here when the operator connects.",
        'ru': "⏳ Оператор свяжется с вами в ближайшее время.\n📝 Когда оператор подключится, вы сможете общаться здесь.",
        'de': "⏳ Ein Operator wird sich so schnell wie möglich bei Ihnen melden.\n📝 Sie können hier chatten, wenn der Operator eine Verbindung herstellt.",
    },

    # ──────────────────────────────────────
    # İŞLEM TAMAMLAMA
    # ──────────────────────────────────────

    'transaction_completed': {
        'tr': "✅ İşleminiz tamamlandı!\n💰 İşlem kodu: {code}",
        'en': "✅ Transaction completed!\n💰 Transaction code: {code}",
        'ru': "✅ Операция завершена!\n💰 Код операции: {code}",
        'de': "✅ Transaktion abgeschlossen!\n💰 Transaktionscode: {code}",
    },

    'transaction_completed_full': {
        'tr': "✅ **İŞLEM TAMAMLANDI!**\n\n🎯 **İŞLEM KODU: {code}**\n\n📋 İşlem Detayları:\n• TXID: `{txid}`\n• Onay Sayısı: {confirmations}/10 ✅\n\n⚠️ **Bu kodu bayiye gösterin!**",
        'en': "✅ **TRANSACTION COMPLETED!**\n\n🎯 **TRANSACTION CODE: {code}**\n\n📋 Transaction Details:\n• TXID: `{txid}`\n• Confirmations: {confirmations}/10 ✅\n\n⚠️ **Show this code to the dealer!**",
        'ru': "✅ **ОПЕРАЦИЯ ЗАВЕРШЕНА!**\n\n🎯 **КОД ОПЕРАЦИИ: {code}**\n\n📋 Детали операции:\n• TXID: `{txid}`\n• Подтверждения: {confirmations}/10 ✅\n\n⚠️ **Покажите этот код дилеру!**",
        'de': "✅ **TRANSAKTION ABGESCHLOSSEN!**\n\n🎯 **TRANSAKTIONSCODE: {code}**\n\n📋 Transaktionsdetails:\n• TXID: `{txid}`\n• Bestätigungen: {confirmations}/10 ✅\n\n⚠️ **Zeigen Sie diesen Code dem Händler!**",
    },

    'transaction_cancelled': {
        'tr': "❌ İşlem iptal edildi.",
        'en': "❌ Transaction cancelled.",
        'ru': "❌ Операция отменена.",
        'de': "❌ Transaktion abgebrochen.",
    },

    # ──────────────────────────────────────
    # DURUM SORGULAMA
    # ──────────────────────────────────────

    'no_pending_transaction': {
        'tr': "⚠️ Bekleyen işleminiz bulunamadı.",
        'en': "⚠️ No pending transaction found.",
        'ru': "⚠️ Незавершённых операций не найдено.",
        'de': "⚠️ Keine ausstehende Transaktion gefunden.",
    },

    'no_active_transaction': {
        'tr': "ℹ️ Aktif veya bekleyen bir işleminiz yok.",
        'en': "ℹ️ You have no active or pending transactions.",
        'ru': "ℹ️ У вас нет активных или незавершённых операций.",
        'de': "ℹ️ Sie haben keine aktiven oder ausstehenden Transaktionen.",
    },

    'status_check_error': {
        'tr': "❌ Durum kontrolü sırasında hata oluştu.",
        'en': "❌ Error occurred during status check.",
        'ru': "❌ Ошибка при проверке статуса.",
        'de': "❌ Fehler bei der Statusprüfung.",
    },

    # ──────────────────────────────────────
    # DOSYA / MEDYA
    # ──────────────────────────────────────

    'photo_received': {
        'tr': "📸 Fotoğraf alındı ve operatöre iletildi.",
        'en': "📸 Photo received and forwarded to operator.",
        'ru': "📸 Фотография получена и передана оператору.",
        'de': "📸 Foto erhalten und an Operator weitergeleitet.",
    },

    'file_received': {
        'tr': "📎 Dosya alındı: {filename}",
        'en': "📎 File received: {filename}",
        'ru': "📎 Файл получен: {filename}",
        'de': "📎 Datei erhalten: {filename}",
    },

    'unsupported_file': {
        'tr': "❌ Desteklenmeyen dosya türü.",
        'en': "❌ Unsupported file type.",
        'ru': "❌ Неподдерживаемый тип файла.",
        'de': "❌ Nicht unterstützter Dateityp.",
    },

    # ──────────────────────────────────────
    # HATALAR
    # ──────────────────────────────────────

    'system_error': {
        'tr': "⚠️ **Sistem Hatası**\n\nŞu anda işlem yapılamıyor. Lütfen daha sonra tekrar deneyin.",
        'en': "⚠️ **System Error**\n\nCannot process transaction now. Please try again later.",
        'ru': "⚠️ **Системная ошибка**\n\nВ данный момент обработка операции невозможна. Пожалуйста, попробуйте позже.",
        'de': "⚠️ **Systemfehler**\n\nTransaktion kann jetzt nicht verarbeitet werden. Bitte versuchen Sie es später erneut.",
    },

    'error_occurred': {
        'tr': "❌ Bir hata oluştu. Lütfen tekrar deneyin.",
        'en': "❌ An error occurred. Please try again.",
        'ru': "❌ Произошла ошибка. Пожалуйста, попробуйте ещё раз.",
        'de': "❌ Ein Fehler ist aufgetreten. Bitte versuchen Sie es erneut.",
    },

    # ──────────────────────────────────────
    # BUTONLAR
    # ──────────────────────────────────────

    'confirm': {
        'tr': "✅ Onayla",
        'en': "✅ Confirm",
        'ru': "✅ Подтвердить",
        'de': "✅ Bestätigen",
    },

    'cancel': {
        'tr': "❌ İptal",
        'en': "❌ Cancel",
        'ru': "❌ Отмена",
        'de': "❌ Abbrechen",
    },

    'cancel_transaction': {
        'tr': "❌ İşlemi İptal Et",
        'en': "❌ Cancel Transaction",
        'ru': "❌ Отменить операцию",
        'de': "❌ Transaktion abbrechen",
    },

    'new_transaction': {
        'tr': "🔄 Yeni İşlem Başlat",
        'en': "🔄 Start New Transaction",
        'ru': "🔄 Начать новую операцию",
        'de': "🔄 Neue Transaktion starten",
    },

    'join_group': {
        'tr': "👥 Gruba Katıl",
        'en': "👥 Join Group",
        'ru': "👥 Вступить в группу",
        'de': "👥 Gruppe beitreten",
    },

    # ──────────────────────────────────────
    # OPERATÖR BOTU METİNLERİ
    # ──────────────────────────────────────

    'op_welcome': {
        'tr': "👋 Operatör Paneline Hoş Geldiniz!\n\nKaydınız admin onayına gönderildi. Onaylandığında bildirim alacaksınız.",
        'en': "👋 Welcome to Operator Panel!\n\nYour registration has been sent for admin approval. You will be notified when approved.",
        'ru': "👋 Добро пожаловать в панель оператора!\n\nВаша регистрация отправлена на одобрение администратору. Вы получите уведомление после одобрения.",
        'de': "👋 Willkommen im Operator-Panel!\n\nIhre Registrierung wurde zur Admin-Genehmigung gesendet. Sie werden benachrichtigt, wenn sie genehmigt wird.",
    },

    'op_already_registered': {
        'tr': "✅ Zaten kayıtlısınız. Durum: {status}",
        'en': "✅ You are already registered. Status: {status}",
        'ru': "✅ Вы уже зарегистрированы. Статус: {status}",
        'de': "✅ Sie sind bereits registriert. Status: {status}",
    },

    'op_pending_list_header': {
        'tr': "📋 **Bekleyen Müşteriler:**\n",
        'en': "📋 **Pending Customers:**\n",
        'ru': "📋 **Ожидающие клиенты:**\n",
        'de': "📋 **Wartende Kunden:**\n",
    },

    'op_no_pending': {
        'tr': "✅ Bekleyen müşteri yok.",
        'en': "✅ No pending customers.",
        'ru': "✅ Нет ожидающих клиентов.",
        'de': "✅ Keine wartenden Kunden.",
    },

    'op_stats': {
        'tr': "📊 **İstatistikleriniz:**\n\n• Bugün: {today} işlem\n• Toplam: {total} işlem",
        'en': "📊 **Your Statistics:**\n\n• Today: {today} transactions\n• Total: {total} transactions",
        'ru': "📊 **Ваша статистика:**\n\n• Сегодня: {today} операций\n• Всего: {total} операций",
        'de': "📊 **Ihre Statistiken:**\n\n• Heute: {today} Transaktionen\n• Gesamt: {total} Transaktionen",
    },

    'op_not_active': {
        'tr': "⚠️ Hesabınız aktif değil. Admin onayı bekleyin.",
        'en': "⚠️ Your account is not active. Wait for admin approval.",
        'ru': "⚠️ Ваш аккаунт неактивен. Ожидайте одобрения администратора.",
        'de': "⚠️ Ihr Konto ist nicht aktiv. Warten Sie auf die Admin-Genehmigung.",
    },

    'op_customer_connected': {
        'tr': "🔗 Müşteri #{customer_id} ({name}) ile bağlandınız.\nMesajlarınız müşteriye iletilecek.",
        'en': "🔗 Connected to customer #{customer_id} ({name}).\nYour messages will be forwarded to the customer.",
        'ru': "🔗 Вы подключены к клиенту #{customer_id} ({name}).\nВаши сообщения будут переданы клиенту.",
        'de': "🔗 Verbunden mit Kunde #{customer_id} ({name}).\nIhre Nachrichten werden an den Kunden weitergeleitet.",
    },

    'op_transaction_completed': {
        'tr': "✅ İşlem #{tid} tamamlandı!\nTamamlama kodu: **{code}**",
        'en': "✅ Transaction #{tid} completed!\nCompletion code: **{code}**",
        'ru': "✅ Операция №{tid} завершена!\nКод завершения: **{code}**",
        'de': "✅ Transaktion #{tid} abgeschlossen!\nAbschlusscode: **{code}**",
    },

    # ──────────────────────────────────────
    # RUBLE KANAL BOTU METİNLERİ
    # ──────────────────────────────────────

    'ruble_new_request': {
        'tr': "🔔 **YENİ RUBLE TALEBİ**\n\n👤 Müşteri: {name} (#{cid})\n💰 Tutar: {amount} ₽\n💱 TL Karşılığı: {try_amount} TL\n📈 Kur: {rate}\n🎫 İşlem №{tid}\n\n📎 Banka bilgilerini bu mesaja yanıt vererek gönderin.",
        'en': "🔔 **NEW RUBLE REQUEST**\n\n👤 Customer: {name} (#{cid})\n💰 Amount: {amount} ₽\n💱 TL Equivalent: {try_amount} TL\n📈 Rate: {rate}\n🎫 Transaction #{tid}\n\n📎 Reply to this message with bank details.",
        'ru': "🔔 **НОВЫЙ ЗАПРОС НА РУБЛИ**\n\n👤 Клиент: {name} (#{cid})\n💰 Сумма: {amount} ₽\n💱 Эквивалент в TL: {try_amount} TL\n📈 Курс: {rate}\n🎫 Операция №{tid}\n\n📎 Ответьте на это сообщение, указав банковские реквизиты.",
        'de': "🔔 **NEUE RUBEL-ANFRAGE**\n\n👤 Kunde: {name} (#{cid})\n💰 Betrag: {amount} ₽\n💱 TL Gegenwert: {try_amount} TL\n📈 Kurs: {rate}\n🎫 Transaktion #{tid}\n\n📎 Antworten Sie auf diese Nachricht mit den Bankdaten.",
    },

    'ruble_bank_info_sent': {
        'tr': "✅ Banka bilgileri müşteriye gönderildi.",
        'en': "✅ Bank details sent to customer.",
        'ru': "✅ Банковские реквизиты отправлены клиенту.",
        'de': "✅ Bankdaten an Kunden gesendet.",
    },

    'ruble_receipt_received': {
        'tr': "📎 **DEKONT ALINDI**\n\n🎫 İşlem №{tid}\n👤 Müşteri: {name}\n💰 Tutar: {amount} ₽\n\nOnaylıyor musunuz?",
        'en': "📎 **RECEIPT RECEIVED**\n\n🎫 Transaction #{tid}\n👤 Customer: {name}\n💰 Amount: {amount} ₽\n\nDo you approve?",
        'ru': "📎 **КВИТАНЦИЯ ПОЛУЧЕНА**\n\n🎫 Операция №{tid}\n👤 Клиент: {name}\n💰 Сумма: {amount} ₽\n\nОдобрить?",
        'de': "📎 **QUITTUNG ERHALTEN**\n\n🎫 Transaktion #{tid}\n👤 Kunde: {name}\n💰 Betrag: {amount} ₽\n\nGenehmigen?",
    },

    'ruble_approved': {
        'tr': "✅ **Ödeme Onaylandı!**\n\nİşlem №{tid} başarıyla tamamlandı.\nMuhasebe kaydı oluşturuldu.",
        'en': "✅ **Payment Approved!**\n\nTransaction #{tid} completed successfully.\nAccounting record created.",
        'ru': "✅ **Оплата одобрена!**\n\nОперация №{tid} успешно завершена.\nЗапись в бухгалтерию создана.",
        'de': "✅ **Zahlung genehmigt!**\n\nTransaktion #{tid} erfolgreich abgeschlossen.\nBuchhaltungseintrag erstellt.",
    },

    'ruble_rejected': {
        'tr': "❌ **Ödeme Reddedildi!**\n\nİşlem №{tid} reddedildi.\nMüşteriye bildirim gönderildi.",
        'en': "❌ **Payment Rejected!**\n\nTransaction #{tid} was rejected.\nCustomer has been notified.",
        'ru': "❌ **Оплата отклонена!**\n\nОперация №{tid} отклонена.\nКлиент уведомлён.",
        'de': "❌ **Zahlung abgelehnt!**\n\nTransaktion #{tid} wurde abgelehnt.\nKunde wurde benachrichtigt.",
    },

    'ruble_transaction_completed': {
        'tr': "✅ **RUBLE İŞLEMİ TAMAMLANDI!**",
        'en': "✅ **RUBLE TRANSACTION COMPLETED!**",
        'ru': "✅ **РУБЛЁВАЯ ОПЕРАЦИЯ ЗАВЕРШЕНА!**",
        'de': "✅ **RUBEL-TRANSAKTION ABGESCHLOSSEN!**",
    },

    'completion_code_label': {
        'tr': "İŞLEM KODU: {code}",
        'en': "TRANSACTION CODE: {code}",
        'ru': "КОД ОПЕРАЦИИ: {code}",
        'de': "TRANSAKTIONSCODE: {code}",
    },

    'transaction_details_header': {
        'tr': "📋 İşlem Detayları:",
        'en': "📋 Transaction Details:",
        'ru': "📋 Детали операции:",
        'de': "📋 Transaktionsdetails:",
    },

    'transaction_id_label': {
        'tr': "• İşlem ID: #{tid}",
        'en': "• Transaction ID: #{tid}",
        'ru': "• Операция №{tid}",
        'de': "• Transaktions-ID: #{tid}",
    },

    'ruble_amount_label': {
        'tr': "• Ruble Miktarı: {amount} RUB",
        'en': "• Ruble Amount: {amount} RUB",
        'ru': "• Сумма в рублях: {amount} ₽",
        'de': "• Rubel-Betrag: {amount} RUB",
    },

    'try_amount_label': {
        'tr': "• TL Karşılığı: {amount} TL",
        'en': "• TL Equivalent: {amount} TL",
        'ru': "• Эквивалент в TL: {amount} TL",
        'de': "• TL-Gegenwert: {amount} TL",
    },

    'exchange_rate_label': {
        'tr': "• Kur: {rate}",
        'en': "• Exchange Rate: {rate}",
        'ru': "• Курс: {rate}",
        'de': "• Wechselkurs: {rate}",
    },

    'status_approved': {
        'tr': "• Durum: Onaylandı ✅",
        'en': "• Status: Approved ✅",
        'ru': "• Статус: Одобрено ✅",
        'de': "• Status: Genehmigt ✅",
    },

    'show_code_to_dealer': {
        'tr': "Bu kodu bayiye gösterin!",
        'en': "Show this code to the dealer!",
        'ru': "Покажите этот код дилеру!",
        'de': "Zeigen Sie diesen Code dem Händler!",
    },

    'thank_you': {
        'tr': "Teşekkür ederiz! 🙏",
        'en': "Thank you! 🙏",
        'ru': "Спасибо! 🙏",
        'de': "Danke! 🙏",
    },

    'payment_rejected': {
        'tr': "ÖDEME REDDEDİLDİ",
        'en': "PAYMENT REJECTED",
        'ru': "ПЛАТЁЖ ОТКЛОНЁН",
        'de': "ZAHLUNG ABGELEHNT",
    },

    'payment_rejected_detail': {
        'tr': "Ödemeniz reddedildi.\nLütfen dekontu kontrol edip tekrar gönderin veya destek ile iletişime geçin.",
        'en': "Your payment was rejected.\nPlease check the receipt and try again, or contact support.",
        'ru': "Ваш платёж отклонён.\nПожалуйста, проверьте квитанцию и отправьте повторно, или свяжитесь со службой поддержки.",
        'de': "Ihre Zahlung wurde abgelehnt.\nBitte überprüfen Sie die Quittung und versuchen Sie es erneut, oder kontaktieren Sie den Support.",
    },

    'bank_warning': {
        'tr': "⚠️ **ÇOK ÖNEMLİ UYARI!**\n🚨 Parayı kesinlikle belirtilen bankaya gönderin!\n❌ Başka bankaya gönderim durumunda ofis sorumluluk kabul etmez!",
        'en': "⚠️ **VERY IMPORTANT WARNING!**\n🚨 Send money strictly to the specified bank!\n❌ The office is not responsible for transfers to another bank!",
        'ru': "⚠️ **ОЧЕНЬ ВАЖНОЕ ПРЕДУПРЕЖДЕНИЕ!**\n🚨 Отправляйте деньги строго на указанный счёт!\n❌ При отправке на другой счёт компания не несёт ответственности!",
        'de': "⚠️ **SEHR WICHTIGE WARNUNG!**\n🚨 Geld streng an die angegebene Bank senden!\n❌ Bei Überweisung an eine andere Bank übernimmt das Büro keine Verantwortung!",
    },

    'copy_instruction': {
        'tr': "💡 Yukarıdaki bilgilere uzun basarak kopyalayabilirsiniz.",
        'en': "💡 Long press on the information above to copy.",
        'ru': "💡 Нажмите и удерживайте текст выше, чтобы скопировать.",
        'de': "💡 Lang auf die obigen Informationen drücken zum Kopieren.",
    },

    'amount_usdt_label': {
        'tr': "Miktar: {amount} USDT",
        'en': "Amount: {amount} USDT",
        'ru': "Сумма: {amount} USDT",
        'de': "Betrag: {amount} USDT",
    },

    'txid_checking': {
        'tr': "⏳ TXID doğrulanıyor, lütfen bekleyin...",
        'en': "⏳ Verifying TXID, please wait...",
        'ru': "⏳ Проверка TXID, пожалуйста, подождите...",
        'de': "⏳ TXID wird verifiziert, bitte warten...",
    },

    'txid_checking_progress': {
        'tr': "İŞLEM ONAYLANIYOR\n\n📊 Durum: {confirmations}/{total} onay\n{bar}\n\n• TXID: `{txid}`",
        'en': "TRANSACTION BEING VERIFIED\n\n📊 Status: {confirmations}/{total} confirmations\n{bar}\n\n• TXID: `{txid}`",
        'ru': "ОПЕРАЦИЯ ПОДТВЕРЖДАЕТСЯ\n\n📊 Статус: {confirmations}/{total} подтверждений\n{bar}\n\n• TXID: `{txid}`",
        'de': "TRANSAKTION WIRD VERIFIZIERT\n\n📊 Status: {confirmations}/{total} Bestätigungen\n{bar}\n\n• TXID: `{txid}`",
    },

    'ruble_payment_rejected_customer': {
        'tr': "❌ **Ödemeniz reddedildi.**\n\nLütfen doğru bilgilerle tekrar deneyin veya operatörle iletişime geçin.",
        'en': "❌ **Your payment was rejected.**\n\nPlease try again with correct information or contact an operator.",
        'ru': "❌ **Ваш платёж отклонён.**\n\nПожалуйста, попробуйте снова с правильными данными или свяжитесь с оператором.",
        'de': "❌ **Ihre Zahlung wurde abgelehnt.**\n\nBitte versuchen Sie es erneut mit korrekten Daten oder kontaktieren Sie einen Operator.",
    },

    'btn_approve': {
        'tr': "✅ Onayla",
        'en': "✅ Approve",
        'ru': "✅ Одобрить",
        'de': "✅ Genehmigen",
    },

    'btn_reject': {
        'tr': "❌ Reddet",
        'en': "❌ Reject",
        'ru': "❌ Отклонить",
        'de': "❌ Ablehnen",
    },

    # ──────────────────────────────────────
    # YARDIMCI / DİĞER
    # ──────────────────────────────────────

    'example_format': {
        'tr': "Örnek:",
        'en': "Example:",
        'ru': "Пример:",
        'de': "Beispiel:",
    },

    'or_just_numbers': {
        'tr': "veya sadece",
        'en': "or just",
        'ru': "или просто",
        'de': "oder nur",
    },

    'both_formats_accepted': {
        'tr': "Her iki formatı da kabul ediyorum",
        'en': "I accept both formats",
        'ru': "Принимаются оба формата",
        'de': "Ich akzeptiere beide Formate",
    },

    # ──────────────────────────────────────
    # ADMİN BİLDİRİMLERİ
    # ──────────────────────────────────────

    'admin_new_operator': {
        'tr': "🆕 **Yeni operatör kaydı:**\n\n👤 {name}\n🆔 ID: {oid}\n📛 @{username}\n\nOnaylamak için /onayla_{oid}\nReddetmek için /reddet_{oid}",
        'en': "🆕 **New operator registration:**\n\n👤 {name}\n🆔 ID: {oid}\n📛 @{username}\n\nApprove: /approve_{oid}\nReject: /reject_{oid}",
        'ru': "🆕 **Новая регистрация оператора:**\n\n👤 {name}\n🆔 ID: {oid}\n📛 @{username}\n\nОдобрить: /approve_{oid}\nОтклонить: /reject_{oid}",
        'de': "🆕 **Neue Operator-Registrierung:**\n\n👤 {name}\n🆔 ID: {oid}\n📛 @{username}\n\nGenehmigen: /approve_{oid}\nAblehnen: /reject_{oid}",
    },

    'admin_bot_crashed': {
        'tr': "🚨 **BOT ÇÖKTÜ!**\n\n🤖 Bot: {bot_name}\n⏰ Zaman: {time}\n❌ Hata: {error}\n\n🔄 Otomatik yeniden başlatma deneniyor...",
        'en': "🚨 **BOT CRASHED!**\n\n🤖 Bot: {bot_name}\n⏰ Time: {time}\n❌ Error: {error}\n\n🔄 Attempting automatic restart...",
        'ru': "🚨 **БОТ УПАЛ!**\n\n🤖 Бот: {bot_name}\n⏰ Время: {time}\n❌ Ошибка: {error}\n\n🔄 Попытка автоматического перезапуска...",
        'de': "🚨 **BOT ABGESTÜRZT!**\n\n🤖 Bot: {bot_name}\n⏰ Zeit: {time}\n❌ Fehler: {error}\n\n🔄 Automatischer Neustart wird versucht...",
    },

    'admin_bot_restarted': {
        'tr': "✅ **Bot yeniden başlatıldı:** {bot_name}",
        'en': "✅ **Bot restarted:** {bot_name}",
        'ru': "✅ **Бот перезапущен:** {bot_name}",
        'de': "✅ **Bot neu gestartet:** {bot_name}",
    },
}
