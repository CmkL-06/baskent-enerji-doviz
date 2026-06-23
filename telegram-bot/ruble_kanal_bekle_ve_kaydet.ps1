# Ruble botunu kanala ekledikten sonra bu scripti calistirin.
# Bot kanala eklendiginde veya kanalda mesaj atildiginda ID'yi yakalar ve .env'i gunceller.

$token = "8319544964:AAFzzd7z-o3LukChftcoljEs6qZ7WCPJ0I8"
$envFile = "$PSScriptRoot\.env"

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "  RUBLE KANAL ID BEKLENIYOR" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "YAPMANIZ GEREKEN:" -ForegroundColor Yellow
Write-Host "  1. Telegram'da Ruble kanalinizi acin" -ForegroundColor Yellow
Write-Host "  2. Kanal ayarlari > Yoneticiler > Yonetici Ekle" -ForegroundColor Yellow
Write-Host "  3. '@MoneyExchangeRubleBot' arayip ekleyin (mesaj gonderme izni verin)" -ForegroundColor Yellow
Write-Host "  4. Kanala herhangi bir mesaj atin" -ForegroundColor Yellow
Write-Host ""
Write-Host "Bu pencereyi acik tutun - ID yakalandiginda otomatik kaydedilecek." -ForegroundColor Green
Write-Host ""

$offset = 0
$found = $false
$attempts = 0

while (-not $found) {
    $attempts++
    try {
        $body = @{
            offset = $offset
            limit = 100
            timeout = 20
            allowed_updates = @("channel_post","my_chat_member","message")
        } | ConvertTo-Json

        $r = Invoke-RestMethod -Uri "https://api.telegram.org/bot$token/getUpdates" `
             -Method POST -ContentType "application/json" -Body $body -TimeoutSec 30

        if ($r.ok -and $r.result.Count -gt 0) {
            foreach ($u in $r.result) {
                $offset = $u.update_id + 1
                foreach ($key in @('channel_post','my_chat_member','message')) {
                    $msg = $u.$key
                    if (-not $msg) { continue }
                    $chat = $msg.chat
                    if ($chat.type -in @('channel','supergroup')) {
                        $channelId = $chat.id
                        $channelTitle = $chat.title
                        Write-Host ""
                        Write-Host "KANAL BULUNDU!" -ForegroundColor Green
                        Write-Host "  Baslik: $channelTitle" -ForegroundColor Green
                        Write-Host "  ID    : $channelId" -ForegroundColor Green
                        Write-Host ""

                        # .env dosyasini guncelle
                        $content = Get-Content $envFile -Encoding UTF8 -Raw
                        $newContent = $content -replace 'RUBLE_CHANNEL_ID=.*', "RUBLE_CHANNEL_ID=$channelId"
                        [System.IO.File]::WriteAllText($envFile, $newContent, [System.Text.Encoding]::UTF8)

                        Write-Host ".env guncellendi: RUBLE_CHANNEL_ID=$channelId" -ForegroundColor Green
                        Write-Host ""
                        Write-Host "Simdi BaskentBot-Ruble servisini yeniden baslatiliyor..." -ForegroundColor Cyan
                        Restart-Service -Name "BaskentBot-Ruble" -ErrorAction SilentlyContinue
                        $status = (Get-Service -Name "BaskentBot-Ruble" -ErrorAction SilentlyContinue).Status
                        Write-Host "Servis durumu: $status" -ForegroundColor Green
                        Write-Host ""
                        Write-Host "TAMAMLANDI! Ruble botu artik kanali dinliyor." -ForegroundColor Green
                        $found = $true
                        break
                    }
                }
                if ($found) { break }
            }
        } else {
            # Sessiz bekleme - 5 saniyede bir nokta yaz
            Write-Host "." -NoNewline -ForegroundColor DarkGray
            if ($attempts % 10 -eq 0) { Write-Host " ($attempts saniye)" -ForegroundColor DarkGray }
        }
    } catch {
        Write-Host "!" -NoNewline -ForegroundColor Red
        Start-Sleep -Seconds 5
    }
}
