<template>
  <div style="font-family: 'Times New Roman', serif; padding: 20px; background-color: #f0f0f0;">
    <h1 style="text-align: center; border-bottom: 2px solid black; padding-bottom: 10px;">Para Birimi Yönetimi</h1>

    <!-- Add/Edit Form -->
    <div style="border: 1px solid black; padding: 15px; margin: 20px 0; background-color: white;">
      <h2>Para Birimi {{ editingCurrency ? 'Güncelle' : 'Ekle' }}</h2>
      <form @submit.prevent="saveCurrency">
        <table>
          <tr>
            <td><label for="currencyCode">Para Birimi Kodu:</label></td>
            <td><input type="text" id="currencyCode" v-model="form.currencyCode" required style="width: 200px; padding: 5px;" /></td>
          </tr>
          <tr>
            <td><label for="currencyName">Para Birimi Adı:</label></td>
            <td><input type="text" id="currencyName" v-model="form.currencyName" required style="width: 200px; padding: 5px;" /></td>
          </tr>
        </table>
        <br>
        <button type="submit" style="padding: 8px 20px; background-color: #4CAF50; color: white; border: none; cursor: pointer;">
          {{ editingCurrency ? 'Güncelle' : 'Ekle' }}
        </button>
        <button v-if="editingCurrency" type="button" @click="cancelEdit" style="margin-left: 10px; padding: 8px 20px; background-color: #808080; color: white; border: none; cursor: pointer;">
          İptal
        </button>
      </form>
    </div>

    <!-- Currency List -->
    <div style="border: 1px solid black; padding: 15px; background-color: white;">
      <h2>Para Birimleri Listesi</h2>

      <div v-if="loading" style="text-align: center; padding: 20px;">Yükleniyor...</div>

      <table v-else border="1" cellpadding="5" cellspacing="0" style="width: 100%; border-collapse: collapse;">
        <thead>
          <tr style="background-color: #e0e0e0;">
            <th>ID</th>
            <th>Para Birimi Kodu</th>
            <th>Para Birimi Adı</th>
            <th>Sembol</th>
            <th>Oluşturulma Tarihi</th>
            <th>İşlemler</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="currency in currencies" :key="currency.id">
            <td>{{ currency.id.substring(0, 8) }}...</td>
            <td><strong>{{ currency.currencyCode }}</strong></td>
            <td>{{ currency.currencyName }}</td>
            <td>{{ currency.currencySymbol || '-' }}</td>
            <td>{{ formatDate(currency.createdDate) }}</td>
            <td>
              <button @click="editCurrency(currency)" style="padding: 5px 10px; margin-right: 5px; background-color: #2196F3; color: white; border: none; cursor: pointer;">
                Düzenle
              </button>
              <button @click="confirmDelete(currency)" style="padding: 5px 10px; background-color: #f44336; color: white; border: none; cursor: pointer;">
                Sil
              </button>
            </td>
          </tr>
          <tr v-if="currencies.length === 0">
            <td colspan="6" style="text-align: center; padding: 20px;">Henüz para birimi eklenmemiş</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import apiService from '@/services/apiservice'
import { useExchangeStore } from '@/stores/exchange'

interface Currency {
  id: string
  createdDate: string
  currencyCode: string
  currencyName: string
  currencySymbol?: string
}

const exchangeStore = useExchangeStore()
const currencies = ref<Currency[]>([])
const loading = ref(false)
const editingCurrency = ref<Currency | null>(null)

const form = ref({
  currencyCode: '',
  currencyName: ''
})

const loadCurrencies = async () => {
  loading.value = true
  try {
    const response = await apiService.getCurrencies()
    currencies.value = response
  } catch (error) {
    console.error('Failed to load currencies:', error)
    alert('Para birimleri yüklenemedi!')
  } finally {
    loading.value = false
  }
}

const saveCurrency = async () => {
  try {
    await apiService.saveCurrency({
      currencyCode: form.value.currencyCode.toUpperCase(),
      currencyName: form.value.currencyName
    })

    if (editingCurrency.value) {
      alert('Para birimi güncellendi!')
    } else {
      alert('Para birimi eklendi!')
    }

    // Reset form
    form.value = {
      currencyCode: '',
      currencyName: ''
    }
    editingCurrency.value = null

    // Reload list
    await loadCurrencies()
  } catch (error) {
    console.error('Failed to save currency:', error)
    alert('Para birimi kaydedilemedi!')
  }
}

const editCurrency = (currency: Currency) => {
  editingCurrency.value = currency
  form.value = {
    currencyCode: currency.currencyCode,
    currencyName: currency.currencyName
  }
}

const cancelEdit = () => {
  editingCurrency.value = null
  form.value = {
    currencyCode: '',
    currencyName: ''
  }
}

const confirmDelete = async (currency: Currency) => {
  const message = `⚠️ DİKKAT! ⚠️\n\n"${currency.currencyCode} - ${currency.currencyName}" para birimini silmek üzeresiniz!\n\nBU İŞLEM GERİ ALINAMAZ!\nBu para birimi ile ilgili TÜM İŞLEMLER DE SİLİNECEKTİR!\n\nDevam etmek istediğinize emin misiniz?`

  if (confirm(message)) {
    if (confirm('Gerçekten bu para birimini ve TÜM İLGİLİ İŞLEMLERİ silmek istiyor musun? bunu onayladıktan sonra işlemlerim nereye gitti diye ağlama ihtimaliniz var')) {
      await deleteCurrency(currency)
    }
  }
}

const deleteCurrency = async (currency: Currency) => {
  try {
    await apiService.deleteCurrency(currency.id)
    alert('Para birimi silindi!')
    await loadCurrencies()
  } catch (error) {
    console.error('Failed to delete currency:', error)
    alert('Para birimi silinemedi!')
  }
}

const formatDate = (dateString: string) => {
  const date = new Date(dateString)
  return date.toLocaleString('tr-TR')
}

onMounted(() => {
  loadCurrencies()
})
</script>