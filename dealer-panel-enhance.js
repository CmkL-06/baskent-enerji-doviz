/**
 * Başkent Enerji - Bayi Panel & Admin Yönetim Enhancement v2
 * Dashboard düzeltmeleri, Döviz İşlemleri sekmeleri, Bayi QR sistemi
 *
 * Sekme mantığı:
 *   Admin → Operatör Paneli | Admin Panel | Bayi QR Oluştur
 *   Şube  → Sadece Operatör Paneli (tab bar gizli)
 */
(function(){
'use strict';

const API='https://api.baskentenerji.com/api/v1';
const QR_LIB='https://cdn.jsdelivr.net/npm/qrcode@1.5.3/build/qrcode.min.js';
const SCOPED_ATTR='data-v-6cff5275';

// ═══════════════════════════════════════
// YARDIMCI FONKSİYONLAR
// ═══════════════════════════════════════
function getToken(){
  try{
    const a=document.getElementById('app').__vue_app__;
    return a.config.globalProperties.$pinia._s.get('auth')?.token;
  }catch(e){return localStorage.getItem('token')}
}

function getUser(){
  try{
    const a=document.getElementById('app').__vue_app__;
    return a.config.globalProperties.$pinia._s.get('auth')?.user;
  }catch(e){
    try{return JSON.parse(localStorage.getItem('user'))}catch(e2){return null}
  }
}

function isAdmin(){
  const u=getUser();
  return u&&(u.rank===99||u.rank==='Admin'||u.isAdmin===true);
}

async function apiFetch(ep,opts={}){
  const t=getToken();
  if(!t)return null;
  const o={headers:{'Authorization':'Bearer '+t,'Content-Type':'application/json'},...opts};
  const r=await fetch(API+ep,o);
  if(!r.ok){
    if(r.status===403)throw new Error('Yetkiniz yok');
    throw new Error('API Hata: '+r.status);
  }
  return r.json();
}

async function apiPost(ep,body){
  return apiFetch(ep,{method:'POST',body:JSON.stringify(body)});
}

async function apiPut(ep,body){
  return apiFetch(ep,{method:'PUT',body:JSON.stringify(body)});
}

function fmtDate(d){
  if(!d)return'-';
  const dt=new Date(d);
  return dt.toLocaleDateString('tr-TR')+' '+dt.toLocaleTimeString('tr-TR',{hour:'2-digit',minute:'2-digit'});
}

// ═══════════════════════════════════════
// 1. DASHBOARD DÜZELTMELERI
// ═══════════════════════════════════════
const CARD_FIXES={
  'Buna bascan mahooo':'Döviz alış-satış işlemleri',
  'Buna bascan':'Döviz alış-satış işlemleri'
};

function fixDashboardTexts(){
  if(!location.pathname.includes('/dashboard'))return;
  document.querySelectorAll('.card-subtitle,.menu-subtitle,p').forEach(function(el){
    var txt=el.textContent.trim();
    if(CARD_FIXES[txt])el.textContent=CARD_FIXES[txt];
  });
}

// ═══════════════════════════════════════
// 2. DÖVİZ İŞLEMLERİ SEKME SİSTEMİ
// ═══════════════════════════════════════
let tabsInjected=false;
let currentTab='operator';

function findExchangeContainer(){
  // Strateji 1: scoped attribute (ModernExchangeV2 bileşeni)
  var el=document.querySelector('['+SCOPED_ATTR+']');
  if(el)return el;

  // Strateji 2: Tailwind class (w-full max-w-7xl mx-auto) + exchange ipuçları
  var candidates=document.querySelectorAll('.w-full.max-w-7xl.mx-auto');
  for(var i=0;i<candidates.length;i++){
    var c=candidates[i];
    if(c.querySelector('.rounded-xl.shadow-lg')||
       c.querySelector('[class*="from-green"]')||
       c.querySelector('[class*="from-red"]')||
       c.querySelector('.grid.grid-cols-2')){
      return c;
    }
  }

  // Strateji 3: h-64 loading spinner (sayfa yükleniyor durumu)
  var loading=document.querySelector('.flex.items-center.justify-center.h-64');
  if(loading){
    var p=loading.closest('.w-full');
    if(p)return p;
  }

  return null;
}

function injectExchangeTabs(){
  if(tabsInjected)return;
  if(!location.pathname.includes('/exchange'))return;

  var attempts=0;
  var maxAttempts=30;

  var waitForContent=setInterval(function(){
    attempts++;
    if(attempts>maxAttempts){clearInterval(waitForContent);return}

    var container=findExchangeContainer();
    if(!container)return;
    clearInterval(waitForContent);

    if(document.getElementById('bke-exchange-tabs'))return;

    tabsInjected=true;
    var admin=isAdmin();

    // Sadece admin için tab bar göster
    if(!admin)return;

    // Tab bar'ı oluştur
    var tabBar=document.createElement('div');
    tabBar.id='bke-exchange-tabs';
    tabBar.innerHTML=
      '<div class="bke-tabs-bar">'+
        '<button class="bke-tab active" data-tab="operator">'+
          '<span class="material-symbols-outlined">currency_exchange</span>'+
          ' Operatör Paneli'+
        '</button>'+
        '<button class="bke-tab" data-tab="admin">'+
          '<span class="material-symbols-outlined">admin_panel_settings</span>'+
          ' Admin Panel'+
        '</button>'+
        '<button class="bke-tab" data-tab="dealer-qr">'+
          '<span class="material-symbols-outlined">qr_code_2</span>'+
          ' Bayi QR Oluştur'+
        '</button>'+
      '</div>';

    // Admin panel container
    var adminPanel=document.createElement('div');
    adminPanel.id='bke-admin-panel';
    adminPanel.className='bke-tab-content';
    adminPanel.style.display='none';

    // QR panel container
    var qrPanel=document.createElement('div');
    qrPanel.id='bke-dealer-qr-panel';
    qrPanel.className='bke-tab-content';
    qrPanel.style.display='none';

    // Vue fragment render: birden fazla root element olabilir
    // Hepsinin ortak parent'ını bul
    var parent=container.parentElement;
    if(!parent){console.warn('[BKE] No parent found');return}

    // Tüm Vue scoped elementleri bul (fragment children)
    var scopedEls=parent.querySelectorAll(':scope > ['+SCOPED_ATTR+']');
    var firstVueEl=scopedEls.length>0?scopedEls[0]:container;

    // Tab bar'ı Vue content'in ÖNÜNE ekle
    parent.insertBefore(tabBar,firstVueEl);

    // Admin ve QR panellerini Vue content'in SONRASINA ekle
    // Son Vue elemanından sonraya koy
    var lastVueEl=scopedEls.length>0?scopedEls[scopedEls.length-1]:container;
    var afterEl=lastVueEl.nextSibling;
    parent.insertBefore(adminPanel,afterEl);
    parent.insertBefore(qrPanel,afterEl);

    // Vue elementlerini gizle/göster (taşımadan!)
    function showOperator(){
      for(var i=0;i<scopedEls.length;i++)scopedEls[i].style.display='';
      adminPanel.style.display='none';
      qrPanel.style.display='none';
    }
    function showAdmin(){
      for(var i=0;i<scopedEls.length;i++)scopedEls[i].style.display='none';
      adminPanel.style.display='block';
      qrPanel.style.display='none';
      if(!adminPanel.dataset.loaded)loadAdminPanel(adminPanel);
    }
    function showQr(){
      for(var i=0;i<scopedEls.length;i++)scopedEls[i].style.display='none';
      adminPanel.style.display='none';
      qrPanel.style.display='block';
      if(!qrPanel.dataset.loaded)loadDealerQrPanel(qrPanel);
    }

    tabBar.querySelectorAll('.bke-tab').forEach(function(btn){
      btn.addEventListener('click',function(){
        tabBar.querySelectorAll('.bke-tab').forEach(function(b){b.classList.remove('active')});
        btn.classList.add('active');
        var tab=btn.dataset.tab;
        currentTab=tab;

        if(tab==='operator')showOperator();
        else if(tab==='admin')showAdmin();
        else if(tab==='dealer-qr')showQr();
      });
    });

    console.log('[BKE] Exchange tabs injected successfully. Vue elements:',scopedEls.length);
  },600);
}

// ═══════════════════════════════════════
// 3. ADMIN PANEL
// ═══════════════════════════════════════
async function loadAdminPanel(container){
  container.dataset.loaded='true';
  container.innerHTML=
    '<div class="bke-panel">'+
      '<div class="bke-panel-header">'+
        '<h2><span class="material-symbols-outlined">admin_panel_settings</span> Admin Panel</h2>'+
        '<p>Ofis yönetimi, kullanıcı istatistikleri ve sistem ayarları</p>'+
      '</div>'+
      '<div class="bke-stats-grid" id="bke-admin-stats">'+
        '<div class="bke-stat-card loading"><div class="bke-stat-label">Yükleniyor...</div></div>'+
      '</div>'+
      '<div class="bke-section">'+
        '<h3><span class="material-symbols-outlined">store</span> Ofis / Şube Durumu</h3>'+
        '<div id="bke-admin-offices" class="bke-table-wrap">'+
          '<div class="bke-loading">Yükleniyor...</div>'+
        '</div>'+
      '</div>'+
      '<div class="bke-section">'+
        '<h3><span class="material-symbols-outlined">group</span> Bayi Listesi</h3>'+
        '<div class="bke-toolbar">'+
          '<button class="bke-btn bke-btn-primary" onclick="window._bkeAddDealer()">'+
            '<span class="material-symbols-outlined">add</span> Yeni Bayi Ekle'+
          '</button>'+
          '<button class="bke-btn bke-btn-secondary" onclick="window._bkeRefreshDealers()">'+
            '<span class="material-symbols-outlined">refresh</span> Yenile'+
          '</button>'+
        '</div>'+
        '<div id="bke-admin-dealers" class="bke-table-wrap">'+
          '<div class="bke-loading">Yükleniyor...</div>'+
        '</div>'+
      '</div>'+
    '</div>';

  try{
    var results=await Promise.all([
      apiFetch('/exchange/dashboard').catch(function(){return null}),
      apiFetch('/exchange/dealer').catch(function(){return []})
    ]);
    var dashData=results[0];
    var dealers=results[1];

    var statsEl=document.getElementById('bke-admin-stats');
    var officesEl=document.getElementById('bke-admin-offices');
    var dealersEl=document.getElementById('bke-admin-dealers');

    if(dashData){
      var offices=dashData.officeDetails||dashData.offices||[];
      var totalOffices=offices.length;
      var activeOffices=offices.filter(function(o){return o.isActive!==false}).length;
      var txCount=dashData.summary?.todayTransactionCount||0;

      statsEl.innerHTML=
        '<div class="bke-stat-card primary">'+
          '<div class="bke-stat-icon"><span class="material-symbols-outlined">store</span></div>'+
          '<div class="bke-stat-value">'+totalOffices+'</div>'+
          '<div class="bke-stat-label">Toplam Şube</div>'+
        '</div>'+
        '<div class="bke-stat-card success">'+
          '<div class="bke-stat-icon"><span class="material-symbols-outlined">check_circle</span></div>'+
          '<div class="bke-stat-value">'+activeOffices+'</div>'+
          '<div class="bke-stat-label">Aktif Şube</div>'+
        '</div>'+
        '<div class="bke-stat-card info">'+
          '<div class="bke-stat-icon"><span class="material-symbols-outlined">receipt_long</span></div>'+
          '<div class="bke-stat-value">'+txCount+'</div>'+
          '<div class="bke-stat-label">Bugünkü İşlem</div>'+
        '</div>'+
        '<div class="bke-stat-card warning">'+
          '<div class="bke-stat-icon"><span class="material-symbols-outlined">groups</span></div>'+
          '<div class="bke-stat-value">'+(Array.isArray(dealers)?dealers.length:0)+'</div>'+
          '<div class="bke-stat-label">Toplam Bayi</div>'+
        '</div>';

      if(offices.length>0){
        officesEl.innerHTML='<table class="bke-table">'+
          '<thead><tr><th>Şube</th><th>Kasa Sayısı</th><th>Durum</th></tr></thead>'+
          '<tbody>'+offices.map(function(o){
            return '<tr>'+
              '<td><strong>'+(o.officeName||o.name||'-')+'</strong></td>'+
              '<td>'+(o.vaultCount||(o.vaults||[]).length||0)+'</td>'+
              '<td><span class="bke-badge '+(o.isActive!==false?'success':'danger')+'">'+(o.isActive!==false?'Aktif':'Pasif')+'</span></td>'+
            '</tr>';
          }).join('')+'</tbody></table>';
      }else{
        officesEl.innerHTML='<div class="bke-empty">Şube bulunamadı</div>';
      }
    }

    renderDealerTable(dealersEl,dealers,true);
  }catch(e){
    console.error('[BKE] Admin panel load error:',e);
    var s=document.getElementById('bke-admin-stats');
    if(s)s.innerHTML='<div class="bke-error">Veri yüklenirken hata: '+e.message+'</div>';
  }
}

// ═══════════════════════════════════════
// 4. BAYİ QR KOD PANELİ
// ═══════════════════════════════════════
async function loadDealerQrPanel(container){
  container.dataset.loaded='true';
  var admin=isAdmin();

  container.innerHTML=
    '<div class="bke-panel">'+
      '<div class="bke-panel-header">'+
        '<h2><span class="material-symbols-outlined">qr_code_2</span> Bayi QR Kod Oluşturucu</h2>'+
        '<p>'+(admin?'Tüm bayiler için QR kod oluşturabilirsiniz':'Kendi bayileriniz için QR kod oluşturabilirsiniz')+'</p>'+
      '</div>'+
      '<div class="bke-qr-layout">'+
        '<div class="bke-qr-left">'+
          '<div class="bke-section">'+
            '<h3><span class="material-symbols-outlined">list</span> Bayiler</h3>'+
            '<div class="bke-toolbar">'+
              '<select id="bke-qr-office-filter" class="bke-select" onchange="window._bkeFilterQrDealers()">'+
                '<option value="">Tüm Şubeler</option>'+
              '</select>'+
              '<button class="bke-btn bke-btn-primary" onclick="window._bkeAddDealer()">'+
                '<span class="material-symbols-outlined">add</span> Yeni Bayi'+
              '</button>'+
            '</div>'+
            '<div id="bke-qr-dealer-list" class="bke-dealer-grid">'+
              '<div class="bke-loading">Yükleniyor...</div>'+
            '</div>'+
          '</div>'+
        '</div>'+
        '<div class="bke-qr-right">'+
          '<div class="bke-qr-preview" id="bke-qr-preview">'+
            '<div class="bke-qr-empty">'+
              '<span class="material-symbols-outlined" style="font-size:64px;opacity:0.3">qr_code_2</span>'+
              '<p>QR kod oluşturmak için sol taraftan bir bayi seçin</p>'+
            '</div>'+
          '</div>'+
        '</div>'+
      '</div>'+
    '</div>';

  try{
    var results=await Promise.all([
      apiFetch('/exchange/dealer').catch(function(){return []}),
      apiFetch('/exchange/offices/summary').catch(function(){return []})
    ]);
    var dealers=results[0];
    var offices=results[1];

    var officeSelect=document.getElementById('bke-qr-office-filter');
    if(offices&&Array.isArray(offices)){
      offices.forEach(function(o){
        var opt=document.createElement('option');
        opt.value=o.officeId||o.id;
        opt.textContent=o.officeName||o.name;
        officeSelect.appendChild(opt);
      });
    }

    window._bkeDealerData=dealers||[];
    window._bkeOfficeData=offices||[];
    renderQrDealerList(dealers);
  }catch(e){
    console.error('[BKE] QR panel load error:',e);
    var el=document.getElementById('bke-qr-dealer-list');
    if(el)el.innerHTML='<div class="bke-error">Hata: '+e.message+'</div>';
  }
}

function renderQrDealerList(dealers){
  var el=document.getElementById('bke-qr-dealer-list');
  if(!el)return;
  if(!dealers||dealers.length===0){
    el.innerHTML=
      '<div class="bke-empty">'+
        '<span class="material-symbols-outlined" style="font-size:48px;opacity:0.3">store</span>'+
        '<p>Henüz bayi eklenmemiş</p>'+
        '<button class="bke-btn bke-btn-primary" onclick="window._bkeAddDealer()">'+
          '<span class="material-symbols-outlined">add</span> İlk Bayiyi Ekle'+
        '</button>'+
      '</div>';
    return;
  }

  el.innerHTML=dealers.map(function(d){
    return '<div class="bke-dealer-card '+(d.qrData?'has-qr':'')+'" data-id="'+d.id+'" onclick="window._bkeSelectDealer(\''+d.id+'\')">'+
      '<div class="bke-dc-header">'+
        '<div class="bke-dc-avatar">'+(d.dealerName||'?')[0].toUpperCase()+'</div>'+
        '<div class="bke-dc-info">'+
          '<div class="bke-dc-name">'+d.dealerName+'</div>'+
          '<div class="bke-dc-code">'+d.dealerCode+'</div>'+
        '</div>'+
        '<span class="bke-badge '+(d.isActive?'success':'danger')+'">'+(d.isActive?'Aktif':'Pasif')+'</span>'+
      '</div>'+
      '<div class="bke-dc-meta">'+
        '<span><span class="material-symbols-outlined" style="font-size:14px">store</span> '+(d.officeName||'-')+'</span>'+
        (d.phone?'<span><span class="material-symbols-outlined" style="font-size:14px">phone</span> '+d.phone+'</span>':'')+
      '</div>'+
      '<div class="bke-dc-footer">'+
        (d.qrData?
          '<span class="bke-badge info">QR Mevcut</span><span style="font-size:11px;color:#94a3b8">'+fmtDate(d.qrGeneratedDate)+'</span>':
          '<span class="bke-badge warning">QR Yok</span>')+
      '</div>'+
    '</div>';
  }).join('');
}

function renderDealerTable(container,dealers,showActions){
  if(!container)return;
  if(!dealers||!Array.isArray(dealers)||dealers.length===0){
    container.innerHTML='<div class="bke-empty">Bayi bulunamadı</div>';
    return;
  }
  container.innerHTML='<table class="bke-table">'+
    '<thead><tr>'+
      '<th>Bayi Adı</th><th>Kod</th><th>Şube</th><th>Telefon</th><th>Durum</th><th>QR</th>'+
      (showActions?'<th>İşlem</th>':'')+
    '</tr></thead>'+
    '<tbody>'+dealers.map(function(d){
      return '<tr>'+
        '<td><strong>'+d.dealerName+'</strong></td>'+
        '<td><code>'+d.dealerCode+'</code></td>'+
        '<td>'+(d.officeName||'-')+'</td>'+
        '<td>'+(d.phone||'-')+'</td>'+
        '<td><span class="bke-badge '+(d.isActive?'success':'danger')+'">'+(d.isActive?'Aktif':'Pasif')+'</span></td>'+
        '<td>'+(d.qrData?'<span class="bke-badge info">Var</span>':'<span class="bke-badge warning">Yok</span>')+'</td>'+
        (showActions?'<td>'+
          '<button class="bke-btn-sm" onclick="window._bkeGenerateQr(\''+d.id+'\')">QR Oluştur</button> '+
          '<button class="bke-btn-sm secondary" onclick="window._bkeEditDealer(\''+d.id+'\')">Düzenle</button>'+
        '</td>':'')+
      '</tr>';
    }).join('')+'</tbody></table>';
}

// ═══════════════════════════════════════
// 5. QR KOD OLUŞTURMA
// ═══════════════════════════════════════
var qrLibLoaded=false;

function loadQrLib(){
  return new Promise(function(resolve){
    if(qrLibLoaded||window.QRCode){qrLibLoaded=true;resolve();return}
    var s=document.createElement('script');
    s.src=QR_LIB;
    s.onload=function(){qrLibLoaded=true;resolve()};
    s.onerror=function(){resolve()};
    document.head.appendChild(s);
  });
}

window._bkeSelectDealer=async function(dealerId){
  var preview=document.getElementById('bke-qr-preview');
  if(!preview)return;

  var dealers=window._bkeDealerData||[];
  var dealer=dealers.find(function(d){return d.id===dealerId});
  if(!dealer){preview.innerHTML='<div class="bke-error">Bayi bulunamadı</div>';return}

  document.querySelectorAll('.bke-dealer-card').forEach(function(c){c.classList.remove('selected')});
  var card=document.querySelector('.bke-dealer-card[data-id="'+dealerId+'"]');
  if(card)card.classList.add('selected');

  preview.innerHTML=
    '<div class="bke-qr-detail">'+
      '<div class="bke-qr-dealer-info">'+
        '<div class="bke-dc-avatar large">'+(dealer.dealerName||'?')[0].toUpperCase()+'</div>'+
        '<h3>'+dealer.dealerName+'</h3>'+
        '<p class="bke-dc-code">'+dealer.dealerCode+'</p>'+
        '<p style="color:#64748b;font-size:13px">'+(dealer.officeName||'')+'</p>'+
      '</div>'+
      '<div class="bke-qr-canvas-wrap">'+
        (dealer.qrData?'<div class="bke-loading">QR kod oluşturuluyor...</div>':'<p style="color:#94a3b8">Henüz QR oluşturulmamış</p>')+
      '</div>'+
      '<div class="bke-qr-actions">'+
        '<button class="bke-btn bke-btn-primary" onclick="window._bkeGenerateQr(\''+dealer.id+'\')">'+
          '<span class="material-symbols-outlined">qr_code_2</span> '+
          (dealer.qrData?'QR Yeniden Oluştur':'QR Oluştur')+
        '</button>'+
        (dealer.qrData?'<button class="bke-btn bke-btn-secondary" onclick="window._bkeDownloadQr(\''+dealer.id+'\')">'+
          '<span class="material-symbols-outlined">download</span> İndir'+
        '</button>':'')+
      '</div>'+
    '</div>';

  if(dealer.qrData){
    await loadQrLib();
    var canvasWrap=preview.querySelector('.bke-qr-canvas-wrap');
    if(window.QRCode&&canvasWrap){
      var canvas=document.createElement('canvas');
      canvas.id='bke-qr-canvas-'+dealer.id;
      canvasWrap.innerHTML='';
      canvasWrap.appendChild(canvas);
      window.QRCode.toCanvas(canvas,dealer.qrData,{
        width:280,margin:2,
        color:{dark:'#1e293b',light:'#ffffff'}
      });
    }
  }
};

window._bkeGenerateQr=async function(dealerId){
  try{
    var result=await apiPost('/exchange/dealer/'+dealerId+'/generate-qr',{});
    if(result){
      var dealers=window._bkeDealerData||[];
      var idx=dealers.findIndex(function(d){return d.id===dealerId});
      if(idx>=0){
        dealers[idx].qrData=result.qrData;
        dealers[idx].qrGeneratedDate=result.qrGeneratedDate;
      }
      renderQrDealerList(dealers);
      window._bkeSelectDealer(dealerId);
      showToast('QR kod başarıyla oluşturuldu','success');
    }
  }catch(e){
    showToast('QR oluşturma hatası: '+e.message,'error');
  }
};

window._bkeDownloadQr=function(dealerId){
  var canvas=document.getElementById('bke-qr-canvas-'+dealerId);
  if(!canvas)return;
  var dealers=window._bkeDealerData||[];
  var dealer=dealers.find(function(d){return d.id===dealerId});
  var link=document.createElement('a');
  link.download='QR-'+(dealer?dealer.dealerCode:'bayi')+'.png';
  link.href=canvas.toDataURL('image/png');
  link.click();
};

window._bkeFilterQrDealers=function(){
  var sel=document.getElementById('bke-qr-office-filter');
  var officeId=sel?sel.value:'';
  var dealers=window._bkeDealerData||[];
  var filtered=officeId?dealers.filter(function(d){return d.officeId===officeId}):dealers;
  renderQrDealerList(filtered);
};

// ═══════════════════════════════════════
// 6. BAYİ EKLEME/DÜZENLEME MODAL
// ═══════════════════════════════════════
window._bkeAddDealer=function(){showDealerModal()};
window._bkeEditDealer=async function(id){
  try{
    var d=await apiFetch('/exchange/dealer/'+id);
    if(d)showDealerModal(d);
  }catch(e){showToast('Bayi bilgisi alınamadı','error')}
};

window._bkeRefreshDealers=async function(){
  try{
    var dealers=await apiFetch('/exchange/dealer');
    window._bkeDealerData=dealers||[];
    var adminEl=document.getElementById('bke-admin-dealers');
    if(adminEl)renderDealerTable(adminEl,dealers,true);
    var qrEl=document.getElementById('bke-qr-dealer-list');
    if(qrEl)renderQrDealerList(dealers);
    showToast('Bayi listesi güncellendi','success');
  }catch(e){showToast('Hata: '+e.message,'error')}
};

async function showDealerModal(existing){
  var offices=window._bkeOfficeData||[];
  if(!offices.length){
    try{offices=await apiFetch('/exchange/offices/summary')||[]}catch(e){}
  }

  var overlay=document.createElement('div');
  overlay.className='bke-modal-overlay';
  overlay.innerHTML=
    '<div class="bke-modal">'+
      '<div class="bke-modal-header">'+
        '<h3>'+(existing?'Bayi Düzenle':'Yeni Bayi Ekle')+'</h3>'+
        '<button class="bke-modal-close" onclick="this.closest(\'.bke-modal-overlay\').remove()">&times;</button>'+
      '</div>'+
      '<div class="bke-modal-body">'+
        '<div class="bke-form-row">'+
          '<label>Bayi Adı *</label>'+
          '<input type="text" id="bke-dealer-name" class="bke-input" value="'+(existing?existing.dealerName:'')+'" placeholder="Bayi adını girin">'+
        '</div>'+
        '<div class="bke-form-row">'+
          '<label>Şube *</label>'+
          '<select id="bke-dealer-office" class="bke-input">'+
            '<option value="">Şube seçin</option>'+
            offices.map(function(o){
              var oid=o.officeId||o.id;
              return '<option value="'+oid+'" '+(existing&&existing.officeId===oid?'selected':'')+'>'+(o.officeName||o.name)+'</option>';
            }).join('')+
          '</select>'+
        '</div>'+
        '<div class="bke-form-grid">'+
          '<div class="bke-form-row">'+
            '<label>Yetkili Kişi</label>'+
            '<input type="text" id="bke-dealer-contact" class="bke-input" value="'+(existing?existing.contactPerson||'':'')+'" placeholder="İsim">'+
          '</div>'+
          '<div class="bke-form-row">'+
            '<label>Telefon</label>'+
            '<input type="text" id="bke-dealer-phone" class="bke-input" value="'+(existing?existing.phone||'':'')+'" placeholder="0 5XX XXX XX XX">'+
          '</div>'+
        '</div>'+
        '<div class="bke-form-row">'+
          '<label>E-posta</label>'+
          '<input type="email" id="bke-dealer-email" class="bke-input" value="'+(existing?existing.email||'':'')+'" placeholder="mail@example.com">'+
        '</div>'+
        '<div class="bke-form-row">'+
          '<label>Adres</label>'+
          '<textarea id="bke-dealer-address" class="bke-input" rows="2" placeholder="Açık adres">'+(existing?existing.address||'':'')+'</textarea>'+
        '</div>'+
        '<div class="bke-form-row">'+
          '<label>Açıklama</label>'+
          '<textarea id="bke-dealer-desc" class="bke-input" rows="2" placeholder="Ek notlar">'+(existing?existing.description||'':'')+'</textarea>'+
        '</div>'+
      '</div>'+
      '<div class="bke-modal-footer">'+
        '<button class="bke-btn bke-btn-secondary" onclick="this.closest(\'.bke-modal-overlay\').remove()">İptal</button>'+
        '<button class="bke-btn bke-btn-primary" id="bke-dealer-save">'+
          '<span class="material-symbols-outlined">save</span> '+(existing?'Güncelle':'Kaydet')+
        '</button>'+
      '</div>'+
    '</div>';

  document.body.appendChild(overlay);
  overlay.querySelector('#bke-dealer-save').addEventListener('click',async function(){
    var name=document.getElementById('bke-dealer-name').value.trim();
    var officeId=document.getElementById('bke-dealer-office').value;
    if(!name){showToast('Bayi adı gerekli','error');return}
    if(!officeId){showToast('Şube seçin','error');return}

    var body={
      dealerName:name,
      officeId:officeId,
      contactPerson:document.getElementById('bke-dealer-contact').value.trim()||null,
      phone:document.getElementById('bke-dealer-phone').value.trim()||null,
      email:document.getElementById('bke-dealer-email').value.trim()||null,
      address:document.getElementById('bke-dealer-address').value.trim()||null,
      description:document.getElementById('bke-dealer-desc').value.trim()||null
    };

    try{
      if(existing){
        await apiPut('/exchange/dealer/'+existing.id,body);
        showToast('Bayi güncellendi','success');
      }else{
        await apiPost('/exchange/dealer',body);
        showToast('Bayi eklendi','success');
      }
      overlay.remove();
      window._bkeRefreshDealers();
    }catch(e){
      showToast('Hata: '+e.message,'error');
    }
  });
}

// ═══════════════════════════════════════
// 7. TOAST BİLDİRİM
// ═══════════════════════════════════════
function showToast(msg,type){
  var t=document.createElement('div');
  t.className='bke-toast bke-toast-'+type;
  t.innerHTML='<span class="material-symbols-outlined">'+(type==='success'?'check_circle':type==='error'?'error':'info')+'</span> '+msg;
  document.body.appendChild(t);
  setTimeout(function(){t.classList.add('show')},10);
  setTimeout(function(){t.classList.remove('show');setTimeout(function(){t.remove()},300)},3000);
}

// ═══════════════════════════════════════
// 8. CSS STİLLERİ
// ═══════════════════════════════════════
var style=document.createElement('style');
style.id='bke-dealer-styles';
style.textContent=
'/* Tab Bar */'+
'.bke-tabs-bar{display:flex;gap:4px;padding:8px 12px;background:#ffffff;border-radius:12px;margin-bottom:16px;box-shadow:0 1px 3px rgba(0,0,0,0.08);overflow-x:auto}'+
'.bke-tab{display:flex;align-items:center;gap:6px;padding:10px 18px;border:none;background:transparent;color:#64748b;font-size:13px;font-weight:500;border-radius:8px;cursor:pointer;transition:all .2s;white-space:nowrap}'+
'.bke-tab:hover{background:#f1f5f9;color:#334155}'+
'.bke-tab.active{background:#6366f1;color:#fff;box-shadow:0 2px 8px rgba(99,102,241,0.3)}'+
'.bke-tab .material-symbols-outlined{font-size:18px}'+
'/* Panel */'+
'.bke-panel{animation:bkeFadeIn .3s ease}'+
'.bke-panel-header{margin-bottom:24px}'+
'.bke-panel-header h2{display:flex;align-items:center;gap:8px;font-size:20px;font-weight:700;color:#1e293b;margin-bottom:4px}'+
'.bke-panel-header p{color:#64748b;font-size:13px}'+
'/* Stats Grid */'+
'.bke-stats-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:16px;margin-bottom:24px}'+
'.bke-stat-card{padding:20px;border-radius:12px;background:#fff;box-shadow:0 1px 3px rgba(0,0,0,0.06)}'+
'.bke-stat-card.primary{background:linear-gradient(135deg,#6366f1,#8b5cf6);color:#fff}'+
'.bke-stat-card.success{background:linear-gradient(135deg,#10b981,#059669);color:#fff}'+
'.bke-stat-card.info{background:linear-gradient(135deg,#3b82f6,#2563eb);color:#fff}'+
'.bke-stat-card.warning{background:linear-gradient(135deg,#f59e0b,#d97706);color:#fff}'+
'.bke-stat-icon{width:40px;height:40px;background:rgba(255,255,255,0.2);border-radius:10px;display:flex;align-items:center;justify-content:center;margin-bottom:12px}'+
'.bke-stat-value{font-size:28px;font-weight:700;margin-bottom:4px}'+
'.bke-stat-label{font-size:13px;opacity:0.9}'+
'/* Section */'+
'.bke-section{background:#fff;border-radius:12px;padding:20px;margin-bottom:16px;box-shadow:0 1px 3px rgba(0,0,0,0.06)}'+
'.bke-section h3{display:flex;align-items:center;gap:6px;font-size:16px;font-weight:600;color:#1e293b;margin-bottom:16px}'+
'.bke-section h3 .material-symbols-outlined{font-size:20px;color:#6366f1}'+
'/* Table */'+
'.bke-table-wrap{overflow-x:auto}'+
'.bke-table{width:100%;border-collapse:collapse;font-size:13px}'+
'.bke-table th{text-align:left;padding:10px 12px;background:#f8fafc;font-weight:600;color:#475569;font-size:12px;text-transform:uppercase;letter-spacing:.5px}'+
'.bke-table td{padding:10px 12px;border-top:1px solid #f1f5f9}'+
'.bke-table tr:hover{background:#f8fafc}'+
'/* Badge */'+
'.bke-badge{display:inline-block;padding:2px 8px;border-radius:6px;font-size:11px;font-weight:600}'+
'.bke-badge.success{background:#d1fae5;color:#065f46}'+
'.bke-badge.danger{background:#fee2e2;color:#991b1b}'+
'.bke-badge.warning{background:#fef3c7;color:#92400e}'+
'.bke-badge.info{background:#dbeafe;color:#1e40af}'+
'/* Buttons */'+
'.bke-btn{display:inline-flex;align-items:center;gap:6px;padding:8px 16px;border:none;border-radius:8px;font-size:13px;font-weight:500;cursor:pointer;transition:all .2s}'+
'.bke-btn-primary{background:#6366f1;color:#fff}.bke-btn-primary:hover{background:#4f46e5}'+
'.bke-btn-secondary{background:#f1f5f9;color:#475569;border:1px solid #e2e8f0}.bke-btn-secondary:hover{background:#e2e8f0}'+
'.bke-btn-sm{padding:4px 10px;font-size:12px;border:1px solid #e2e8f0;background:#fff;border-radius:6px;cursor:pointer}'+
'.bke-btn-sm:hover{background:#f1f5f9}'+
'.bke-btn-sm.secondary{color:#6366f1}'+
'/* Toolbar */'+
'.bke-toolbar{display:flex;gap:8px;margin-bottom:16px;flex-wrap:wrap;align-items:center}'+
'/* Select */'+
'.bke-select{padding:8px 12px;border:1px solid #e2e8f0;border-radius:8px;font-size:13px;background:#fff;min-width:160px}'+
'/* QR Layout */'+
'.bke-qr-layout{display:grid;grid-template-columns:1fr 360px;gap:20px}'+
'@media(max-width:900px){.bke-qr-layout{grid-template-columns:1fr}}'+
'.bke-qr-left{min-width:0}'+
'.bke-qr-right{position:sticky;top:20px;align-self:start}'+
'/* Dealer Grid */'+
'.bke-dealer-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(280px,1fr));gap:12px;max-height:60vh;overflow-y:auto;padding:2px}'+
'.bke-dealer-card{background:#fff;border:2px solid #f1f5f9;border-radius:12px;padding:14px;cursor:pointer;transition:all .2s}'+
'.bke-dealer-card:hover{border-color:#c7d2fe;box-shadow:0 4px 12px rgba(99,102,241,0.1)}'+
'.bke-dealer-card.selected{border-color:#6366f1;background:#f5f3ff;box-shadow:0 4px 12px rgba(99,102,241,0.15)}'+
'.bke-dealer-card.has-qr{border-left:4px solid #10b981}'+
'.bke-dc-header{display:flex;align-items:center;gap:10px;margin-bottom:8px}'+
'.bke-dc-avatar{width:36px;height:36px;background:linear-gradient(135deg,#6366f1,#8b5cf6);color:#fff;border-radius:10px;display:flex;align-items:center;justify-content:center;font-weight:700;font-size:16px;flex-shrink:0}'+
'.bke-dc-avatar.large{width:56px;height:56px;font-size:24px;border-radius:14px}'+
'.bke-dc-info{flex:1;min-width:0}'+
'.bke-dc-name{font-weight:600;font-size:14px;color:#1e293b;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}'+
'.bke-dc-code{font-size:12px;color:#94a3b8;font-family:monospace}'+
'.bke-dc-meta{display:flex;flex-wrap:wrap;gap:8px;font-size:12px;color:#64748b;margin-bottom:8px}'+
'.bke-dc-meta span{display:flex;align-items:center;gap:3px}'+
'.bke-dc-footer{display:flex;align-items:center;justify-content:space-between;padding-top:8px;border-top:1px solid #f1f5f9}'+
'/* QR Preview */'+
'.bke-qr-preview{background:#fff;border-radius:16px;padding:24px;box-shadow:0 1px 3px rgba(0,0,0,0.08);min-height:400px;display:flex;align-items:center;justify-content:center}'+
'.bke-qr-empty{text-align:center;color:#94a3b8}'+
'.bke-qr-detail{text-align:center;width:100%}'+
'.bke-qr-dealer-info{margin-bottom:20px}'+
'.bke-qr-dealer-info h3{font-size:18px;color:#1e293b;margin:8px 0 4px}'+
'.bke-qr-canvas-wrap{margin:20px 0;display:flex;justify-content:center}'+
'.bke-qr-canvas-wrap canvas{border-radius:12px;box-shadow:0 4px 12px rgba(0,0,0,0.1)}'+
'.bke-qr-actions{display:flex;gap:8px;justify-content:center;flex-wrap:wrap}'+
'/* Modal */'+
'.bke-modal-overlay{position:fixed;inset:0;background:rgba(0,0,0,0.5);display:flex;align-items:center;justify-content:center;z-index:10000;animation:bkeFadeIn .2s}'+
'.bke-modal{background:#fff;border-radius:16px;width:90%;max-width:520px;max-height:90vh;overflow-y:auto;box-shadow:0 20px 60px rgba(0,0,0,0.2)}'+
'.bke-modal-header{display:flex;justify-content:space-between;align-items:center;padding:20px 24px;border-bottom:1px solid #f1f5f9}'+
'.bke-modal-header h3{font-size:18px;font-weight:600}'+
'.bke-modal-close{width:32px;height:32px;border:none;background:#f1f5f9;border-radius:8px;font-size:18px;cursor:pointer;display:flex;align-items:center;justify-content:center}'+
'.bke-modal-body{padding:24px}'+
'.bke-modal-footer{padding:16px 24px;border-top:1px solid #f1f5f9;display:flex;justify-content:flex-end;gap:8px}'+
'/* Form */'+
'.bke-form-row{margin-bottom:16px}'+
'.bke-form-row label{display:block;font-size:13px;font-weight:500;color:#374151;margin-bottom:6px}'+
'.bke-input{width:100%;padding:10px 12px;border:1px solid #e2e8f0;border-radius:8px;font-size:14px;transition:all .2s;box-sizing:border-box}'+
'.bke-input:focus{outline:none;border-color:#6366f1;box-shadow:0 0 0 3px rgba(99,102,241,0.1)}'+
'.bke-form-grid{display:grid;grid-template-columns:1fr 1fr;gap:16px}'+
'/* Toast */'+
'.bke-toast{position:fixed;bottom:24px;right:24px;padding:12px 20px;border-radius:10px;font-size:13px;font-weight:500;display:flex;align-items:center;gap:8px;z-index:10001;transform:translateY(100px);opacity:0;transition:all .3s;box-shadow:0 4px 12px rgba(0,0,0,0.15)}'+
'.bke-toast.show{transform:translateY(0);opacity:1}'+
'.bke-toast-success{background:#059669;color:#fff}'+
'.bke-toast-error{background:#dc2626;color:#fff}'+
'.bke-toast-info{background:#2563eb;color:#fff}'+
'.bke-toast .material-symbols-outlined{font-size:18px}'+
'/* Utils */'+
'.bke-loading{text-align:center;padding:40px;color:#94a3b8}'+
'.bke-empty{text-align:center;padding:40px;color:#94a3b8}'+
'.bke-error{padding:16px;background:#fef2f2;color:#991b1b;border-radius:8px;font-size:13px}'+
'@keyframes bkeFadeIn{from{opacity:0;transform:translateY(8px)}to{opacity:1;transform:translateY(0)}}';
document.head.appendChild(style);

// ═══════════════════════════════════════
// 9. BAŞLAT
// ═══════════════════════════════════════
var lastPath='';
function checkAndInit(){
  var path=location.pathname;
  if(path===lastPath)return;
  lastPath=path;
  tabsInjected=false;

  // Önceki tab elementlerini temizle (SPA navigasyon)
  var oldTabs=document.getElementById('bke-exchange-tabs');
  if(oldTabs)oldTabs.remove();
  var oldAdmin=document.getElementById('bke-admin-panel');
  if(oldAdmin)oldAdmin.remove();
  var oldQr=document.getElementById('bke-dealer-qr-panel');
  if(oldQr)oldQr.remove();

  setTimeout(fixDashboardTexts,500);
  setTimeout(fixDashboardTexts,2000);

  if(path.includes('/exchange')){
    setTimeout(injectExchangeTabs,1000);
    setTimeout(function(){
      if(!tabsInjected)injectExchangeTabs();
    },3000);
  }
}

if(document.readyState==='complete'||document.readyState==='interactive'){
  setTimeout(checkAndInit,500);
}else{
  document.addEventListener('DOMContentLoaded',function(){setTimeout(checkAndInit,500)});
}

setInterval(checkAndInit,1000);
setInterval(fixDashboardTexts,3000);

})();
