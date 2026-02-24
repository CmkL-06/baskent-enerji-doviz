/**
 * İhtiyarın Mekanı - i18n Çeviri Düzeltme Script'i v2
 * Eksik Türkçe çevirileri DOM üzerinden düzeltir.
 * Güvenli: Sadece metin değişikliği yapar, yapıyı bozmaz.
 * Geri alınabilir: Script'i kaldırmak yeterli.
 */
(function(){
'use strict';

// ═══════════════════════════════════════════════════════
// CSS BAZLI ÇEVİRİ (Vue re-render'a dayanıklı)
// ═══════════════════════════════════════════════════════
const cssfix=document.createElement('style');
cssfix.id='i18n-css-fix';
cssfix.textContent=`
/* Vault status badge - CSS ile Türkçe çeviri */
.vault-status{font-size:0!important;display:inline-flex!important;align-items:center!important;gap:4px!important}
.vault-status .status-dot{font-size:0!important;width:8px!important;height:8px!important;display:inline-block!important}
.vault-status.active::after{content:'Aktif';font-size:13px!important;color:#16a34a;font-weight:500}
.vault-status:not(.active)::after{content:'Pasif';font-size:13px!important;color:#dc2626;font-weight:500}
/* Card status (yeni layout) */
.card-status.st-active{font-size:0!important}
.card-status.st-active::after{content:'AKTİF';font-size:11px!important;font-weight:600;color:#16a34a}
.card-status:not(.st-active){font-size:0!important}
.card-status:not(.st-active)::after{content:'PASİF';font-size:11px!important;font-weight:600;color:#dc2626}
`;
(document.head||document.documentElement).appendChild(cssfix);

// ═══════════════════════════════════════════════════════
// ÇEVİRİ HARİTASI
// ═══════════════════════════════════════════════════════
const TR={
  // ── Döviz İşlemleri - işlem türü seçimi ──
  'exchange.transactionType.selectTitle':'İşlem Türü Seçin',
  'exchange.transactionType.buy':'ALIŞ',
  'exchange.transactionType.buyDesc':'Döviz Alış İşlemi',
  'exchange.transactionType.sell':'SATIŞ',
  'exchange.transactionType.sellDesc':'Döviz Satış İşlemi',

  // ── Döviz İşlemleri - hızlı seçim ──
  'exchange.quickSelect.title':'Hızlı Döviz Seçimi',

  // ── Döviz İşlemleri - ana form ──
  'exchange.operations.title':'Döviz İşlemi',
  'exchange.operations.transactionNumber':'İşlem No',
  'exchange.operations.addButton':'Yeni İşlem Ekle',
  'exchange.operations.receivedCurrency':'Alınan Döviz',
  'exchange.operations.fromCustomer':'Müşteriden Alınan',
  'exchange.operations.givenCurrency':'Verilen Döviz',
  'exchange.operations.fromVault':'Kasadan Verilen',
  'exchange.operations.selectCurrency':'Para Birimi Seçin',
  'exchange.operations.amount':'Miktar',
  'exchange.operations.rate':'Kur',
  'exchange.operations.customRate':'Özel Kur',
  'exchange.operations.toVault':'Kasaya Gelen',
  'exchange.operations.toCustomer':'Müşteriye Verilen',
  'exchange.notes.label':'Notlar',
  'exchange.notes.placeholder':'İşlem notu ekleyin...',

  // ── Döviz İşlemleri - özet paneli ──
  'exchange.summary.details':'İşlem Özet Detayları',
  'exchange.summary.title':'İşlem Özeti',
  'exchange.summary.receivedFromCustomer':'Müşteriden Alınan',
  'exchange.summary.exchangeRate':'Döviz Kuru',
  'exchange.summary.givenFromVault':'Kasadan Verilen',
  'exchange.summary.givenFrom':'Kasadan Verilen',
  'exchange.summary.givenToCustomer':'Müşteriye Verilen',
  'exchange.summary.paymentAmount':'Ödeme Tutarı',
  'exchange.summary.submit':'İşlemi Onayla',
  'exchange.summary.vaultBalance':'Kasa Bakiyesi',
  'exchange.summary.insufficient':'Yetersiz',
  'exchange.summary.available':'Mevcut',
  'exchange.summary.required':'Gerekli',
  'exchange.summary.noTransaction':'İşlem yok',
  'exchange.summary.processing':'İşlem yapılıyor...',

  // ── Döviz İşlemleri - mesajlar ──
  'exchange.messages.loadingData':'Veriler yükleniyor...',
  'exchange.messages.transactionSuccess':'İşlem başarıyla tamamlandı',
  'exchange.messages.transactionFailed':'İşlem başarısız',
  'exchange.messages.unknownError':'Bilinmeyen hata',

  // ── Ofis/Kasa seçim uyarıları ──
  'exchange.officeVault.warningTitle':'Dikkat!',
  'exchange.officeVault.warningOffice':'Lütfen bir ofis seçin',
  'exchange.officeVault.warningVault':'Lütfen bir kasa seçin',

  // ── USDT ──
  'usdt.title':'USDT Ödemeleri',

  // ── İşlem Geçmişi (TransactionHistory - birçok sayfada kullanılır) ──
  'transactionHistory.title':'İşlem Geçmişi',
  'transactionHistory.type':'İşlem Tipi',
  'transactionHistory.date':'Tarih',
  'transactionHistory.vault':'Kasa',
  'transactionHistory.rate':'Kur',
  'transactionHistory.status':'Durum',
  'transactionHistory.actions':'İşlemler',
  'transactionHistory.pending':'Beklemede',
  'transactionHistory.completed':'Tamamlandı',
  'transactionHistory.cancelled':'İptal Edildi',
  'transactionHistory.buy':'ALIŞ',
  'transactionHistory.sell':'SATIŞ',
  'transactionHistory.viewDetails':'İşlem Detayları',
  'transactionHistory.loadingTransactions':'İşlemler yükleniyor...',
  'transactionHistory.notes':'Notlar',
  'transactionHistory.noData':'Kayıt bulunamadı',
  'transactionHistory.amount':'Tutar',
  'transactionHistory.total':'Toplam',
  'transactionHistory.customer':'Müşteri',
  'transactionHistory.description':'Açıklama',
  'transactionHistory.reference':'Referans',
  'transactionHistory.deleteConfirm':'Bu işlemi silmek istediğinize emin misiniz?',
  'transactionHistory.deleteSuccess':'İşlem başarıyla silindi',

  // ── Cariler (Party) ──
  'parties.pleaseSelectOffice':'Lütfen bir ofis seçin',
  'parties.title':'Cari Hesaplar',
  'parties.search':'Cari Ara...',
  'parties.addNew':'Yeni Cari Ekle',
  'parties.name':'Cari Adı',
  'parties.balance':'Bakiye',
  'parties.phone':'Telefon',
  'parties.email':'E-posta',
  'parties.actions':'İşlemler',
  'parties.credit':'Alacak',
  'parties.debit':'Borç',
  'parties.noData':'Kayıt bulunamadı',

  // ── Ortak (Common) ──
  'common.loading':'Yükleniyor...',
  'common.save':'Kaydet',
  'common.cancel':'İptal',
  'common.delete':'Sil',
  'common.edit':'Düzenle',
  'common.add':'Ekle',
  'common.search':'Ara',
  'common.filter':'Filtrele',
  'common.close':'Kapat',
  'common.confirm':'Onayla',
  'common.yes':'Evet',
  'common.no':'Hayır',
  'common.success':'Başarılı',
  'common.error':'Hata',
  'common.warning':'Uyarı',
  'common.info':'Bilgi',
  'common.noData':'Kayıt bulunamadı',
  'common.actions':'İşlemler',
  'common.status':'Durum',
  'common.date':'Tarih',
  'common.amount':'Tutar',
  'common.description':'Açıklama',
  'common.total':'Toplam',
  'common.back':'Geri',
  'common.next':'İleri',
  'common.previous':'Önceki',
  'common.details':'Detaylar',
  'common.refresh':'Yenile',
  'common.export':'Dışa Aktar',
  'common.import':'İçe Aktar',
  'common.print':'Yazdır',
  'common.download':'İndir',
  'common.upload':'Yükle',
  'common.selectAll':'Tümünü Seç',
  'common.deselectAll':'Seçimi Kaldır',
  'common.required':'Zorunlu alan',
  'common.optional':'İsteğe bağlı',
  'common.name':'Ad',
  'common.type':'Tip',
  'common.note':'Not',
  'common.notes':'Notlar',
  'common.active':'Aktif',
  'common.inactive':'Pasif',
  'common.all':'Tümü',
  'common.none':'Hiçbiri',
  'common.other':'Diğer',
  'common.unknown':'Bilinmiyor',
  'common.processing':'İşleniyor...',
  'common.pleaseWait':'Lütfen bekleyin...',
  'common.page':'Sayfa',
  'common.of':'/',
  'common.rows':'Satır',
  'common.showing':'Gösterilen',
  'common.to':'-',
  'common.entries':'kayıt',
  'common.perPage':'Sayfa başına',

  // ── Kasa/Şube (Vault) ──
  'vault.title':'Şubeler',
  'vault.status.active':'Aktif',
  'vault.status.inactive':'Pasif',
  'vault.balance':'Bakiye',
  'vault.name':'Şube Adı',
  'vault.transfer':'Transfer',
  'vault.transfer.title':'Kasalar Arası Transfer',
  'vault.transfer.from':'Kaynak Kasa',
  'vault.transfer.to':'Hedef Kasa',
  'vault.transfer.amount':'Tutar',
  'vault.transfer.notes':'Notlar',
  'vault.transfer.submit':'Transferi Onayla',
  'vault.transfer.success':'Transfer başarıyla tamamlandı',

  // ── Kullanıcılar (Users) ──
  'users.title':'Kullanıcı Yönetimi',
  'users.addNew':'Yeni Kullanıcı Ekle',
  'users.name':'Ad Soyad',
  'users.email':'E-posta',
  'users.role':'Rol',
  'users.status':'Durum',
  'users.actions':'İşlemler',
  'users.lastLogin':'Son Giriş',

  // ── Giderler (Expenses) ──
  'expenses.title':'Gider Yönetimi',
  'expenses.addNew':'Yeni Gider Ekle',
  'expenses.category':'Kategori',
  'expenses.amount':'Tutar',
  'expenses.date':'Tarih',
  'expenses.description':'Açıklama',
  'expenses.status':'Durum',

  // ── Z Raporu ──
  'zReport.title':'Z Raporu',
  'zReport.daily':'Günlük',
  'zReport.monthly':'Aylık',
  'zReport.yearly':'Yıllık',
  'zReport.summary':'Özet',
  'zReport.generate':'Rapor Oluştur',
  'zReport.print':'Yazdır',
  'zReport.export':'Dışa Aktar',
  'zReport.totalBuy':'Toplam Alış',
  'zReport.totalSell':'Toplam Satış',
  'zReport.profit':'Kar',
  'zReport.loss':'Zarar',
  'zReport.netProfit':'Net Kar',

  // ── Kur Yönetimi (AutoRate) ──
  'autoRate.strategy.bestBuy':'En Karlı',
  'autoRate.strategy.average':'Ortalama',
  'autoRate.strategy.competitive':'Rekabetçi',

  // ── Ayarlar ──
  'settings.title':'Ayarlar',
  'settings.general':'Genel',
  'settings.notifications':'Bildirimler',
  'settings.security':'Güvenlik',
  'settings.save':'Kaydet',

  // ── Login / Auth ──
  'auth.login':'Giriş Yap',
  'auth.logout':'Çıkış',
  'auth.email':'E-posta',
  'auth.password':'Şifre',
  'auth.rememberMe':'Beni Hatırla',
  'auth.forgotPassword':'Şifremi Unuttum',
  'auth.sessionExpired':'Oturumunuz sona erdi',

  // ── Sidebar butonları ──
  'sidebar.exchangeRates':'Döviz Kurları',
  'sidebar.usdtPayments':'USDT Ödemeleri',
  'sidebar.newPaymentMethod':'Yeni Ödeme Yöntemi',
  'sidebar.expenseCategories':'Gider Kategorileri',
  'sidebar.expenseReports':'Gider Raporları',
  'sidebar.expenses':'Giderler',
  'sidebar.dashboard':'Ana Sayfa',
  'sidebar.exchange':'Döviz İşlemleri',
  'sidebar.vaults':'Şubeler',
  'sidebar.parties':'Cariler',
  'sidebar.users':'Kullanıcılar',
  'sidebar.settings':'Ayarlar',
  'sidebar.zReport':'Z Raporu',

  // ── Dashboard ──
  'dashboard.title':'Kontrol Paneli',
  'dashboard.totalAssets':'Toplam Varlık',
  'dashboard.dailyPL':'Günlük K/Z',
  'dashboard.monthlyPL':'Aylık K/Z',
  'dashboard.transactionCount':'İşlem Sayısı',
  'dashboard.welcome':'Hoş Geldiniz',

  // ── Sayfa başlıkları ve metin değişimleri ──
  'Exchange Office':'Döviz Bürosu',
  'Toplam Kasa Bakiyeleri':'Toplam Şube Bakiyeleri',
  'Ofis kasaları ve bakiyeleri':'Şube kasaları ve bakiyeleri',

  // ── Kasa→Şube terminoloji ──
  'Kasa İşlemleri':'Şube İşlemleri',
  'Ofise Göre Filtrele':'Şubeye Göre Filtrele',
  'Tüm Ofisler':'Tüm Şubeler',
  'Kasa Bakiyeleri':'Şube Bakiyeleri',

  // ── Kasa/Şube durumları ──
  'Created Invalid Date':'',
  'Inactive':'Pasif',
  'Active':'Aktif',

  // ── İngilizce durum/tip metinleri (hardcoded) ──
  'Pending':'Beklemede',
  'Completed':'Tamamlandı',
  'Cancelled':'İptal Edildi',
  'Processing':'İşleniyor',
  'Processing...':'İşleniyor...',
  'Success':'Başarılı',
  'Failed':'Başarısız',
  'Loading...':'Yükleniyor...',
  'No data':'Kayıt bulunamadı',
  'No Data':'Kayıt Bulunamadı',
  'No results':'Sonuç bulunamadı',
  'No results found':'Sonuç bulunamadı',

  // ── Kullanıcı rolleri ──
  'User':'Kullanıcı',
  'Customer':'Müşteri',
  'Admin':'Yönetici',
  'Banned':'Yasaklı',
  'Author':'Yazar',
  'Moderator':'Moderatör',
  'Unknown':'Bilinmiyor',

  // ── Ödeme yöntemleri (GhostParty) ──
  'Cash':'Nakit',
  'Card':'Kart',
  'Credit':'Alacak',
  'Debit':'Borç',

  // ── Butonlar (hardcoded İngilizce) ──
  'Cancel':'İptal',
  'Save':'Kaydet',
  'Delete':'Sil',
  'Edit':'Düzenle',
  'Close':'Kapat',
  'Confirm':'Onayla',
  'Submit':'Gönder',
  'Search':'Ara',
  'Filter':'Filtrele',
  'Refresh':'Yenile',
  'Export':'Dışa Aktar',
  'Print':'Yazdır',
  'Download':'İndir',
  'Add':'Ekle',
  'Update':'Güncelle',
  'Transfer':'Transfer',
  'Back':'Geri',
  'Next':'İleri',
  'Previous':'Önceki',
  'Details':'Detaylar',
  'View':'Görüntüle',
  'View Details':'Detayları Gör',

  // ── Tablo başlıkları ──
  'Name':'Ad',
  'Date':'Tarih',
  'Amount':'Tutar',
  'Status':'Durum',
  'Actions':'İşlemler',
  'Type':'Tip',
  'Description':'Açıklama',
  'Total':'Toplam',
  'Balance':'Bakiye',
  'Rate':'Kur',
  'Notes':'Notlar',
  'Phone':'Telefon',
  'Email':'E-posta',
  'Address':'Adres',
  'Role':'Rol',
  'Office':'Şube',
  'Vault':'Kasa',
  'Currency':'Para Birimi',
  'Reference':'Referans',
  'Created':'Oluşturulma',
  'Updated':'Güncellenme',

  // ── Kur stratejileri (AutoRate ekranında görünen) ──
  'BEST_BUY':'En Karlı',
  'AVERAGE':'Ortalama',
  'COMPETITIVE':'Rekabetçi',

  // ── BinanceDeposits ──
  'Off-chain':'Zincir Dışı',

  // ── ISLEM GECMISI alt başlık ──
  'ISLEM GECMISI':'İŞLEM GEÇMİŞİ'
};

// Uzun key'leri önce işle (partial match sorununu önle)
const KEYS=Object.keys(TR).sort((a,b)=>b.length-a.length);

// ═══════════════════════════════════════════════════════
// ÇEVİRİ FONKSİYONLARI
// ═══════════════════════════════════════════════════════

// Tam eşleşme gerektiren kısa key'ler (<=10 karakter + semantik risk)
const SHORT_EXACT=[
  'Active','Inactive','User','Customer','Admin','Banned','Author','Moderator','Unknown',
  'Cash','Card','Credit','Debit',
  'Cancel','Save','Delete','Edit','Close','Confirm','Submit','Search','Filter',
  'Refresh','Export','Print','Download','Add','Update','Transfer','Back','Next',
  'Previous','Details','View',
  'Name','Date','Amount','Status','Actions','Type','Description','Total','Balance',
  'Rate','Notes','Phone','Email','Address','Role','Office','Vault','Currency',
  'Reference','Created','Updated',
  'Pending','Completed','Cancelled','Processing','Success','Failed',
  'No data','No Data'
];

function fixTextNodes(){
  if(!document.body)return;
  const walker=document.createTreeWalker(document.body,NodeFilter.SHOW_TEXT);
  while(walker.nextNode()){
    const node=walker.currentNode;
    if(node.parentElement&&node.parentElement.closest&&node.parentElement.closest('.akm-modal,.akm-section,#akm-overlay,.bke-panel,.bke-modal'))continue;
    let text=node.textContent;
    let changed=false;
    for(const k of KEYS){
      if(!text.includes(k))continue;
      if(SHORT_EXACT.includes(k)){
        const trimmed=text.trim();
        if(trimmed===k){
          text=text.replace(k,TR[k]);
          changed=true;
        }
      }else{
        text=text.replace(k,TR[k]);
        changed=true;
      }
    }
    if(changed)node.textContent=text;
  }
}

function fixAttributes(){
  if(!document.body)return;
  document.querySelectorAll('[placeholder]').forEach(function(el){
    var p=el.getAttribute('placeholder');
    for(var i=0;i<KEYS.length;i++){
      var k=KEYS[i];
      if(p&&p.includes(k)){
        p=p.replace(k,TR[k]);
        el.setAttribute('placeholder',p);
      }
    }
  });
  document.querySelectorAll('[title]').forEach(function(el){
    var t=el.getAttribute('title');
    for(var i=0;i<KEYS.length;i++){
      var k=KEYS[i];
      if(t&&t.includes(k)){
        t=t.replace(k,TR[k]);
        el.setAttribute('title',t);
      }
    }
  });
  document.querySelectorAll('[aria-label]').forEach(function(el){
    var a=el.getAttribute('aria-label');
    for(var i=0;i<KEYS.length;i++){
      var k=KEYS[i];
      if(a&&a.includes(k)){
        a=a.replace(k,TR[k]);
        el.setAttribute('aria-label',a);
      }
    }
  });
}

function fixDirectSelectors(){
  // Vault status badge'leri
  document.querySelectorAll('.vault-status').forEach(function(el){
    el.childNodes.forEach(function(node){
      if(node.nodeType===3){
        var t=node.textContent.trim();
        if(t==='Active')node.textContent=' Aktif';
        else if(t==='Inactive')node.textContent=' Pasif';
      }
    });
  });

  // Kullanıcı rol badge'leri
  document.querySelectorAll('.user-role,.role-badge,.badge').forEach(function(el){
    if(el.children.length===0){
      var t=el.textContent.trim();
      var roleMap={'User':'Kullanıcı','Customer':'Müşteri','Admin':'Yönetici','Banned':'Yasaklı','Author':'Yazar','Moderator':'Moderatör','Unknown':'Bilinmiyor'};
      if(roleMap[t])el.textContent=roleMap[t];
    }
  });

  // Durum badge'leri (genel)
  document.querySelectorAll('.status-badge,.status,.badge-status').forEach(function(el){
    if(el.children.length===0){
      var t=el.textContent.trim();
      var statusMap={'Active':'Aktif','Inactive':'Pasif','Pending':'Beklemede','Completed':'Tamamlandı','Cancelled':'İptal Edildi','Processing':'İşleniyor','Success':'Başarılı','Failed':'Başarısız'};
      if(statusMap[t])el.textContent=statusMap[t];
    }
  });

  // "Created Invalid Date" gizle
  document.querySelectorAll('span,p,div').forEach(function(el){
    if(el.children.length===0&&el.textContent.trim()==='Created Invalid Date'){
      el.style.display='none';
    }
  });

  // Select/option elementleri içindeki İngilizce roller
  document.querySelectorAll('select option').forEach(function(opt){
    var t=opt.textContent.trim();
    var optMap={'User':'Kullanıcı','Customer':'Müşteri','Admin':'Yönetici','Banned':'Yasaklı','Author':'Yazar','Moderator':'Moderatör','Unknown':'Bilinmiyor','Cash':'Nakit','Card':'Kart','Credit':'Alacak','Debit':'Borç','Active':'Aktif','Inactive':'Pasif'};
    if(optMap[t])opt.textContent=optMap[t];
  });

  // /vaults sayfası özel düzeltmeleri
  if(location.pathname==='/ihtiyar/vaults'||location.pathname==='/ihtiyar/vaults/'){
    document.querySelectorAll('h1,h2,.page-title').forEach(function(el){
      if(el.children.length===0&&el.textContent.trim()==='Kasa'){
        el.textContent='Şubeler';
      }
    });
    document.querySelectorAll('input[placeholder]').forEach(function(el){
      var p=el.getAttribute('placeholder');
      if(p==='Kasa ara...')el.setAttribute('placeholder','Şube ara...');
    });
  }

  // Buton içindeki İngilizce metinler
  document.querySelectorAll('button').forEach(function(btn){
    if(btn.children.length===0){
      var t=btn.textContent.trim();
      var btnMap={'Cancel':'İptal','Save':'Kaydet','Delete':'Sil','Edit':'Düzenle','Close':'Kapat','Confirm':'Onayla','Submit':'Gönder','Search':'Ara','Filter':'Filtrele','Refresh':'Yenile','Export':'Dışa Aktar','Print':'Yazdır','Download':'İndir','Add':'Ekle','Update':'Güncelle','Back':'Geri','Next':'İleri','Previous':'Önceki','View Details':'Detayları Gör'};
      if(btnMap[t])btn.textContent=btnMap[t];
    }
  });

  // Tablo header'ları (th elementleri)
  document.querySelectorAll('th').forEach(function(th){
    if(th.children.length===0){
      var t=th.textContent.trim();
      var thMap={'Name':'Ad','Date':'Tarih','Amount':'Tutar','Status':'Durum','Actions':'İşlemler','Type':'Tip','Description':'Açıklama','Total':'Toplam','Balance':'Bakiye','Rate':'Kur','Notes':'Notlar','Phone':'Telefon','Email':'E-posta','Address':'Adres','Role':'Rol','Office':'Şube','Vault':'Kasa','Currency':'Para Birimi','Reference':'Referans','Created':'Oluşturulma','Updated':'Güncellenme'};
      if(thMap[t])th.textContent=thMap[t];
    }
  });
}

function fixAll(){
  fixTextNodes();
  fixAttributes();
  fixDirectSelectors();
}

// ═══════════════════════════════════════════════════════
// DEBOUNCED OBSERVER
// ═══════════════════════════════════════════════════════
var timer=null;
function debouncedFix(){
  clearTimeout(timer);
  timer=setTimeout(fixAll,150);
}

function init(){
  if(!document.body)return;
  fixAll();
  var obs=new MutationObserver(debouncedFix);
  obs.observe(document.body,{childList:true,subtree:true,characterData:true});
  var lastPath=location.pathname;
  setInterval(function(){
    if(location.pathname!==lastPath){
      lastPath=location.pathname;
      setTimeout(fixAll,500);
      setTimeout(fixAll,1500);
    }
  },300);
}

// ═══════════════════════════════════════════════════════
// BAŞLAT
// ═══════════════════════════════════════════════════════
if(document.readyState==='complete'||document.readyState==='interactive'){
  setTimeout(init,300);
}else{
  document.addEventListener('DOMContentLoaded',function(){setTimeout(init,300);});
}

})();
