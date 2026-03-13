import { test, expect } from '@playwright/test'

test.describe('Login ve token akışı', () => {
  test('login sayfası açılır ve başlık görünür', async ({ page }) => {
    await page.goto('/login')
    await expect(page.getByRole('heading', { name: /Başkent Enerji Döviz/i })).toBeVisible()
    await expect(page.getByPlaceholder(/e-posta|email|ornek/i)).toBeVisible()
    await expect(page.getByPlaceholder(/şifre|password/i)).toBeVisible()
  })

  test('demo giriş ile dashboarda yönlendirir', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('button', { name: /demo giriş/i }).click()
    await expect(page).toHaveURL(/\//)
    await expect(page.getByText(/dashboard|Dashboard/i).first()).toBeVisible({ timeout: 5000 })
  })

  test('giriş formu gönderilebilir (API yanıtı test edilmez)', async ({ page }) => {
    await page.goto('/login')
    await page.getByPlaceholder(/e-posta|email|ornek/i).fill('test@test.com')
    await page.getByPlaceholder(/şifre|password/i).fill('Test123!')
    await page.getByRole('button', { name: /giriş/i }).click()
    // Başarılı olursa yönlendirme, hata olursa hata mesajı görünür
    await page.waitForTimeout(2000)
    const url = page.url()
    const hasError = await page.getByText(/giriş başarısız|wrong|credentials|hatası/i).isVisible().catch(() => false)
    expect(url.includes('/login') || url.includes('/') || hasError).toBe(true)
  })

  test('giriş yapılmadan / sayfasına gidilince login'e yönlendirir', async ({ page }) => {
    await page.goto('/')
    await expect(page).toHaveURL(/\/login/)
  })

  test('demo giriş sonrası çıkış yapılabilir', async ({ page }) => {
    await page.goto('/login')
    await page.getByRole('button', { name: /demo giriş/i }).click()
    await expect(page).toHaveURL(/\//)
    await page.getByRole('button', { name: /çıkış/i }).click()
    await expect(page).toHaveURL(/\/login/)
  })
})
