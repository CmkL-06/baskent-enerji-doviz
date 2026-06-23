/**
 * İhtiyarın Mekanı - i18n Çeviri Düzeltme Script'i
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
  // Döviz İşlemleri - işlem türü seçimi (üst kısım)
  'exchange.transactionType.selectTitle':'İşlem Türü Seçin',
  'exchange.transactionType.buy':'ALIŞ',
  'exchange.transactionType.buyDesc':'Döviz Alış İşlemi',
  'exchange.transactionType.sell':'SATIŞ',
  'exchange.transactionType.sellDesc':'Döviz Satış İşlemi',

  // Döviz İşlemleri - hızlı seçim
  'exchange.quickSelect.title':'Hızlı Döviz Seçimi',

  // Döviz İşlemleri sayfası - ana form
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
  'exchange.notes.label':'Notlar',

  // Döviz İşlemleri sayfası - özet paneli (uzun key'ler önce)
  'exchange.summary.details':'İşlem Özet Detayları',
  'exchange.summary.title':'İşlem Özeti',
  'exchange.summary.receivedFromCustomer':'Müşteriden Alınan',
  'exchange.summary.exchangeRate':'Döviz Kuru',
  'exchange.summary.givenFromVault':'Kasadan Verilen',
  'exchange.summary.givenFrom':'Kasadan Verilen',
  'exchange.summary.paymentAmount':'Ödeme Tutarı',
  'exchange.summary.submit':'İşlemi Onayla',

  // Döviz İşlemleri - işlem geçmişi tablosu
  'transactionHistory.actions':'İşlemler',

  // Döviz İşlemleri - alt bölüm
  'ISLEM GECMISI':'İŞLEM GEÇMİŞİ',

  // Kasa/Şube durumları
  'Created Invalid Date':'',
  'vault.status.active':'Aktif',
  'vault.status.inactive':'Pasif',
  'Inactive':'Pasif',
  'Active':'Aktif',

  // Kullanıcı rolleri
  'User':'Kullanıcı',
  'Customer':'Müşteri',
  'Admin':'Yönetici',

  // Sidebar butonları (birden fazla sayfada)
  'sidebar.exchangeRates':'Döviz Kurları',
  'sidebar.usdtPayments':'USDT Ödemeleri',
  'sidebar.newPaymentMethod':'Yeni Ödeme Yöntemi',
  'sidebar.expenseCategories':'Gider Kategorileri',
  'sidebar.expenseReports':'Gider Raporları',
  'sidebar.expenses':'Giderler',

  // Sayfa başlıkları
  'Exchange Office':'Döviz Bürosu',
  'Toplam Kasa Bakiyeleri':'Toplam Şube Bakiyeleri',
  'Ofis kasaları ve bakiyeleri':'Şube kasaları ve bakiyeleri',

  // Eski Kasa layout'u çevirileri
  'Kasa İşlemleri':'Şube İşlemleri',
  'Ofise Göre Filtrele':'Şubeye Göre Filtrele',
  'Tüm Ofisler':'Tüm Şubeler',
  'Kasa Bakiyeleri':'Şube Bakiyeleri'
};

// Uzun key'leri önce işle (partial match sorununu önle)
const KEYS=Object.keys(TR).sort((a,b)=>b.length-a.length);

// ═══════════════════════════════════════════════════════
// ÇEVİRİ FONKSİYONLARI
// ═══════════════════════════════════════════════════════
// Kısa key'ler (<=8 karakter) yanlışlıkla başka kelimelerde eşleşmesin
const SHORT_EXACT=['Active','Inactive','User','Customer','Admin'];

function fixTextNodes(){
  if(!document.body)return;
  const walker=document.createTreeWalker(document.body,NodeFilter.SHOW_TEXT);
  while(walker.nextNode()){
    const node=walker.currentNode;
    // Enhancement scriptinin kendi elementlerini atla
    if(node.parentElement&&node.parentElement.closest&&node.parentElement.closest('.akm-modal,.akm-section,#akm-overlay'))continue;
    let text=node.textContent;
    let changed=false;
    for(const k of KEYS){
      if(!text.includes(k))continue;
      // Kısa key'ler sadece tam eşleşmede (trimmed) değişsin
      if(SHORT_EXACT.includes(k)){
        if(text.trim()===k||text.trim()===' '+k||text.trim()===k+' '){
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
  // Placeholder'lar
  document.querySelectorAll('[placeholder]').forEach(el=>{
    const p=el.getAttribute('placeholder');
    for(const k of KEYS){
      if(p&&p.includes(k)){
        el.setAttribute('placeholder',p.replace(k,TR[k]));
      }
    }
  });
  // Title attribute'lar
  document.querySelectorAll('[title]').forEach(el=>{
    const t=el.getAttribute('title');
    for(const k of KEYS){
      if(t&&t.includes(k)){
        el.setAttribute('title',t.replace(k,TR[k]));
      }
    }
  });
}

function fixDirectSelectors(){
  // Vault status badge'lerini text node bazlı düzelt (span.status-dot korunur)
  document.querySelectorAll('.vault-status').forEach(el=>{
    el.childNodes.forEach(node=>{
      if(node.nodeType===3){
        const t=node.textContent.trim();
        if(t==='Active')node.textContent=' Aktif';
        else if(t==='Inactive')node.textContent=' Pasif';
      }
    });
  });
  // Kullanıcı rol badge'lerini düzelt
  document.querySelectorAll('.user-role, .role-badge, .badge').forEach(el=>{
    if(el.children.length===0){
      const t=el.textContent.trim();
      if(t==='User')el.textContent='Kullanıcı';
      else if(t==='Customer')el.textContent='Müşteri';
      else if(t==='Admin')el.textContent='Yönetici';
    }
  });
  // "Created Invalid Date" düzelt
  document.querySelectorAll('span,p,div').forEach(el=>{
    if(el.children.length===0&&el.textContent.trim()==='Created Invalid Date'){
      el.style.display='none';
    }
  });
  // Eski "Kasa" layout - sayfa başlığını düzelt (sadece /vaults sayfasında)
  if(location.pathname==='/ihtiyar/vaults'||location.pathname==='/ihtiyar/vaults/'){
    document.querySelectorAll('h1,h2,.page-title').forEach(el=>{
      if(el.children.length===0&&el.textContent.trim()==='Kasa'){
        el.textContent='Şubeler';
      }
    });
    // Placeholder çevirisi
    document.querySelectorAll('input[placeholder]').forEach(el=>{
      const p=el.getAttribute('placeholder');
      if(p==='Kasa ara...')el.setAttribute('placeholder','Şube ara...');
    });
  }
}

function fixAll(){
  fixTextNodes();
  fixAttributes();
  fixDirectSelectors();
}

// ═══════════════════════════════════════════════════════
// DEBOUNCED OBSERVER
// ═══════════════════════════════════════════════════════
let timer=null;
function debouncedFix(){
  clearTimeout(timer);
  timer=setTimeout(fixAll,150);
}

function init(){
  if(!document.body)return;
  fixAll();
  const obs=new MutationObserver(debouncedFix);
  obs.observe(document.body,{childList:true,subtree:true,characterData:true});
  // SPA navigasyon desteği - sayfa değişimlerini yakala
  let lastPath=location.pathname;
  setInterval(()=>{
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
  document.addEventListener('DOMContentLoaded',()=>setTimeout(init,300));
}

})();
