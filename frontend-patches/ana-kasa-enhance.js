(function(){
'use strict';
const API='https://api.baskentenerji.com/api/v1';
function getToken(){try{const a=document.getElementById('app').__vue_app__;return a.config.globalProperties.$pinia._s.get('auth')?.token}catch(e){return null}}
async function apiFetch(ep){const t=getToken();if(!t)return null;const r=await fetch(API+ep,{headers:{'Authorization':'Bearer '+t,'Content-Type':'application/json'}});if(!r.ok)throw new Error(r.status);return r.json()}
function fmt(n,c){if(n==null||isNaN(n))return'₺0,00';const s=c||'TRY';const p=s==='TRY'?'₺':s==='USD'?'$':s==='EUR'?'€':s==='GBP'?'£':s+' ';return p+Number(n).toLocaleString('tr-TR',{minimumFractionDigits:2,maximumFractionDigits:2})}
function fmtN(n){if(n==null||isNaN(n))return'0';return Number(n).toLocaleString('tr-TR',{minimumFractionDigits:2,maximumFractionDigits:4})}
function fmtDate(d){if(!d)return'-';const dt=new Date(d);return dt.toLocaleDateString('tr-TR')+' '+dt.toLocaleTimeString('tr-TR',{hour:'2-digit',minute:'2-digit'})}
function fmtPct(n){if(n==null||isNaN(n))return'%0,00';return'%'+Number(n).toLocaleString('tr-TR',{minimumFractionDigits:2,maximumFractionDigits:2})}
function plColor(n){return(n||0)>=0?'#22c55e':'#ef4444'}
function plClass(n){return(n||0)>=0?'green':'red'}
function plSign(n){return(n||0)>=0?'+':''}
function badge(text,color){return`<span style="display:inline-block;padding:2px 8px;border-radius:4px;font-size:10px;font-weight:700;color:#fff;background:${color}">${text}</span>`}
function section(title,content){return`<div class="akm-section"><div class="akm-section-title">${title}</div>${content}</div>`}

// ═══════════════════════════════════════════════════════
// VAULT ID FIX - Fetch Interceptor (Retry on 404)
// Vault entity id alanı API çağrıları arasında değişiyor.
// Strateji: Orijinal çağrı 404 dönerse vault listesinden
// doğru vaultId'leri alıp sırayla dene.
// ═══════════════════════════════════════════════════════
const _origFetch=window.fetch;
let _vaultIds=null;
let _vaultIdsLoading=null;

function _getVaultIds(authHeader){
  if(_vaultIds)return Promise.resolve(_vaultIds);
  if(_vaultIdsLoading)return _vaultIdsLoading;
  _vaultIdsLoading=(async()=>{
    try{
      const r=await _origFetch.call(window,API+'/exchange/vaults',{
        headers:{'Authorization':authHeader,'Content-Type':'application/json'}
      });
      if(!r.ok){_vaultIds=[];return _vaultIds}
      const list=await r.json();
      _vaultIds=Array.isArray(list)?list.map(v=>v.vaultId).filter(Boolean):[];
      if(_vaultIds.length>0)console.log('[AKM] Vault ID listesi:',_vaultIds.length,'kasa');
    }catch(e){_vaultIds=[]}
    return _vaultIds;
  })();
  return _vaultIdsLoading;
}

window.fetch=function(input,init){
  const url=typeof input==='string'?input:(input instanceof Request?input.url:String(input));
  // Vault detay API çağrısını yakala
  const vm=url.match(/\/api\/v1\/exchange\/vaults\/([0-9a-f-]{30,})(\/.*)?$/i);
  if(vm){
    return _origFetch.call(window,input,init).then(response=>{
      if(response.status===404){
        // Auth header'ı çıkar
        let auth=null;
        if(init&&init.headers){
          if(init.headers instanceof Headers)auth=init.headers.get('Authorization');
          else if(typeof init.headers==='object')auth=init.headers['Authorization']||init.headers['authorization'];
        }
        if(!auth){try{auth='Bearer '+getToken()}catch(e){}}
        if(auth){
          const wrongId=vm[1];
          const suffix=vm[2]||'';
          return _getVaultIds(auth).then(ids=>{
            const tryNext=(idx)=>{
              if(idx>=ids.length)return response;
              if(ids[idx]===wrongId)return tryNext(idx+1);
              const tryUrl=url.replace(wrongId+suffix,ids[idx]+suffix);
              return _origFetch.call(window,tryUrl,init).then(r2=>{
                if(r2.ok){
                  console.log('[AKM] Vault ID düzeltildi:',wrongId.substring(0,8)+'…','→',ids[idx].substring(0,8)+'…');
                  return r2;
                }
                return tryNext(idx+1);
              });
            };
            return tryNext(0);
          });
        }
      }
      return response;
    });
  }
  return _origFetch.call(window,input,init);
};

// ═══════════════════════════════════════════════════════
// VAULT ID FIX - XMLHttpRequest Interceptor
// Vue/Axios XMLHttpRequest kullanır, fetch interceptor
// onu yakalayamaz. XHR.open() override ile URL'deki
// yanlış vault ID'yi cached vaultId ile düzelt.
// ═══════════════════════════════════════════════════════
const _origXHROpen=XMLHttpRequest.prototype.open;
XMLHttpRequest.prototype.open=function(method,url){
  const sUrl=String(url);
  const vm=sUrl.match(/\/api\/v1\/exchange\/vaults\/([0-9a-f-]{30,})(\/.*)?$/i);
  if(vm&&_vaultIds&&_vaultIds.length>0&&!_vaultIds.includes(vm[1])){
    const fixedUrl=sUrl.replace(vm[1],_vaultIds[0]);
    console.log('[AKM-XHR] Vault ID pre-fix:',vm[1].substring(0,8)+'…','→',_vaultIds[0].substring(0,8)+'…');
    arguments[1]=fixedUrl;
  }
  return _origXHROpen.apply(this,arguments);
};

// Vault detay sayfasında vaultId'leri önceden yükle
// (Vue modülleri yüklenmeden önce hazır olsun)
if(/\/ihtiyar\/vaults\/[0-9a-f-]{30,}/i.test(location.pathname)){
  try{
    const _preloadVaultIds=()=>{
      let t=getToken();
      // Fallback: localStorage apiToken (Vue henüz init olmamışsa)
      if(!t)t=localStorage.getItem('apiToken');
      if(t){_getVaultIds('Bearer '+t);return}
      setTimeout(_preloadVaultIds,150);
    };
    // Hemen başla (localStorage token hızlı erişilebilir)
    _preloadVaultIds();
  }catch(e){}
}

// ═══════════════════════════════════════════════════════
// DATE HELPERS
// ═══════════════════════════════════════════════════════
function getDateRange(period){
  const now=new Date();
  const today=new Date(now.getFullYear(),now.getMonth(),now.getDate());
  let start,end=today;
  switch(period){
    case 'today':start=today;break;
    case 'week':start=new Date(today);start.setDate(start.getDate()-start.getDay()+1);break;
    case 'month':start=new Date(today.getFullYear(),today.getMonth(),1);break;
    case 'year':start=new Date(today.getFullYear(),0,1);break;
    default:start=new Date(today.getFullYear(),today.getMonth(),1);
  }
  return{start:start.toISOString().split('T')[0],end:end.toISOString().split('T')[0]};
}

// Cache for offices list
let _officesCache=null;
async function getOfficesList(){
  if(_officesCache)return _officesCache;
  try{
    const data=await apiFetch('/exchange/dashboard');
    _officesCache=(data?.offices||[]).filter(o=>o.officeName!=='ANA KASA').map(o=>({id:o.officeId,name:o.officeName}));
  }catch(e){_officesCache=[];}
  return _officesCache;
}

// ═══════════════════════════════════════════════════════
// FILTER BAR BUILDER
// ═══════════════════════════════════════════════════════
function buildFilterBar(opts){
  const {showPeriod=true,showOffice=true,showCustomDate=true,onFilter}=opts||{};
  let html='<div class="akm-filter-bar">';

  if(showPeriod){
    html+=`<div class="akm-filter-group">
      <label>Dönem:</label>
      <select id="akm-period" class="akm-select">
        <option value="today">Bugün</option>
        <option value="week">Bu Hafta</option>
        <option value="month" selected>Bu Ay</option>
        <option value="year">Bu Yıl</option>
        <option value="custom">Özel</option>
      </select>
    </div>`;
  }

  if(showCustomDate){
    const dr=getDateRange('month');
    html+=`<div class="akm-filter-group akm-custom-dates" id="akm-custom-dates" style="display:none">
      <label>Başlangıç:</label>
      <input type="date" id="akm-date-start" class="akm-input" value="${dr.start}">
      <label>Bitiş:</label>
      <input type="date" id="akm-date-end" class="akm-input" value="${dr.end}">
    </div>`;
  }

  if(showOffice){
    html+=`<div class="akm-filter-group">
      <label>Şube:</label>
      <select id="akm-office" class="akm-select">
        <option value="">Tüm Şubeler</option>
      </select>
    </div>`;
  }

  html+=`<button id="akm-filter-btn" class="akm-filter-apply">Uygula</button>`;
  html+='</div>';
  return html;
}

async function initFilterBar(onFilter){
  // Populate offices dropdown
  const offices=await getOfficesList();
  const officeSelect=document.getElementById('akm-office');
  if(officeSelect){
    offices.forEach(o=>{
      const opt=document.createElement('option');
      opt.value=o.id;
      opt.textContent=o.name;
      officeSelect.appendChild(opt);
    });
  }

  // Period change handler
  const periodSelect=document.getElementById('akm-period');
  const customDates=document.getElementById('akm-custom-dates');
  if(periodSelect){
    periodSelect.addEventListener('change',()=>{
      if(customDates)customDates.style.display=periodSelect.value==='custom'?'flex':'none';
    });
  }

  // Apply button
  const applyBtn=document.getElementById('akm-filter-btn');
  if(applyBtn){
    applyBtn.addEventListener('click',()=>{
      const filters=getFilterValues();
      onFilter(filters);
    });
  }
}

function getFilterValues(){
  const period=document.getElementById('akm-period')?.value||'month';
  let dateRange;
  if(period==='custom'){
    dateRange={
      start:document.getElementById('akm-date-start')?.value,
      end:document.getElementById('akm-date-end')?.value
    };
  }else{
    dateRange=getDateRange(period);
  }
  const officeId=document.getElementById('akm-office')?.value||'';
  const officeName=document.getElementById('akm-office')?.selectedOptions[0]?.text||'Tüm Şubeler';
  return{period,dateRange,officeId,officeName};
}

// ═══════════════════════════════════════════════════════
// BUTTONS CONFIG
// ═══════════════════════════════════════════════════════
const BTNS=[
{id:'live-rates',icon:'currency_exchange',title:'Anlık Kurlar',desc:'Güncel döviz kurları',bg:'linear-gradient(135deg,#3b82f6,#1d4ed8)',fetch:fetchLiveRates,hasFilter:false},
{id:'party-sum',icon:'groups',title:'Cari Hesaplar',desc:'Alacak / borç özeti',bg:'linear-gradient(135deg,#eab308,#b45309)',fetch:fetchParty,hasFilter:false},
{id:'vault-st',icon:'account_balance_wallet',title:'Kasa Durumu',desc:'Kasa bakiyeleri',bg:'linear-gradient(135deg,#06b6d4,#0e7490)',fetch:fetchVaults,hasFilter:false},
{id:'pnl-rpt',icon:'analytics',title:'K/Z Raporu',desc:'Detaylı kar zarar analizi',bg:'linear-gradient(135deg,#22c55e,#15803d)',fetch:fetchPnL,hasFilter:true},
{id:'z-rpt',icon:'summarize',title:'Z Raporu',desc:'Günlük/aylık kasa raporu',bg:'linear-gradient(135deg,#ef4444,#b91c1c)',fetch:fetchZReport,hasFilter:true},
{id:'transfer',icon:'swap_horiz',title:'Transfer',desc:'Şubeler arası para transferi',bg:'linear-gradient(135deg,#ec4899,#be185d)',fetch:fetchTransfer,hasFilter:false},
{id:'capital',icon:'account_balance',title:'Sermaye',desc:'Sermaye takip ve yönetim',bg:'linear-gradient(135deg,#a855f7,#7c3aed)',fetch:fetchCapital,hasFilter:false},
{id:'recent-tx',icon:'receipt_long',title:'Son İşlemler',desc:'Tüm şube işlemleri',bg:'linear-gradient(135deg,#f97316,#ea580c)',fetch:fetchRecentTx,hasFilter:true}
];

// ═══════════════════════════════════════════════════════
// 1. ANLIK KURLAR
// ═══════════════════════════════════════════════════════
async function fetchLiveRates(){
  const data=await apiFetch('/exchange/dashboard');
  if(!data?.offices)return '<div class="akm-empty">Kur verisi bulunamadı</div>';

  const ratesByOffice={};
  const allRates=new Map();
  data.offices.forEach(o=>{
    if(o.officeName==='ANA KASA')return;
    ratesByOffice[o.officeName]=[];
    if(o.exchangeRates)o.exchangeRates.forEach(r=>{
      if(r.targetCurrencyCode==='TRY'&&r.sourceCurrencyCode!=='TRY'){
        const k=r.sourceCurrencyCode;
        ratesByOffice[o.officeName].push({code:k,buy:r.buyRate,sell:r.sellRate});
        if(!allRates.has(k))allRates.set(k,{code:k,offices:{}});
        allRates.get(k).offices[o.officeName]={buy:r.buyRate,sell:r.sellRate};
      }
    });
  });

  if(allRates.size===0)return '<div class="akm-empty">Kur verisi bulunamadı</div>';

  const order=['USD','EUR','GBP','USDT','RUB','KRUB','AED','SAR','CHF'];
  const sorted=[...allRates.values()].sort((a,b)=>{
    const ai=order.indexOf(a.code),bi=order.indexOf(b.code);
    return (ai===-1?99:ai)-(bi===-1?99:bi);
  });

  let h='<table class="akm-table"><thead><tr><th>Döviz</th><th>Alış Kuru</th><th>Satış Kuru</th><th>Spread</th><th>Spread %</th><th>Durum</th></tr></thead><tbody>';
  sorted.forEach(r=>{
    const offices=Object.values(r.offices);
    const avgBuy=offices.reduce((s,o)=>s+o.buy,0)/offices.length;
    const avgSell=offices.reduce((s,o)=>s+o.sell,0)/offices.length;
    const spread=avgSell-avgBuy;
    const spreadPct=(spread/avgBuy*100);
    const status=spreadPct>3?badge('İYİ','#22c55e'):spreadPct>1.5?badge('NORMAL','#eab308'):badge('DAR','#ef4444');
    h+=`<tr><td><strong>${r.code}</strong>/TRY</td><td style="color:#22c55e;font-weight:600">${fmtN(avgBuy)}</td><td style="color:#ef4444;font-weight:600">${fmtN(avgSell)}</td><td>${fmtN(spread)} ₺</td><td>${fmtPct(spreadPct)}</td><td>${status}</td></tr>`;
  });
  h+='</tbody></table>';

  // Office comparison
  const officeNames=Object.keys(ratesByOffice).filter(n=>ratesByOffice[n].length>0);
  if(officeNames.length>1){
    h+=section('Şube Kur Karşılaştırması','');
    h+='<table class="akm-table"><thead><tr><th>Döviz</th>';
    officeNames.forEach(n=>{h+=`<th colspan="2">${n}</th>`});
    h+='</tr><tr><th></th>';
    officeNames.forEach(()=>{h+='<th style="font-size:10px">Alış</th><th style="font-size:10px">Satış</th>'});
    h+='</tr></thead><tbody>';
    sorted.forEach(r=>{
      h+='<tr><td><strong>'+r.code+'</strong></td>';
      officeNames.forEach(n=>{
        const o=r.offices[n];
        h+=o?`<td>${fmtN(o.buy)}</td><td>${fmtN(o.sell)}</td>`:'<td>-</td><td>-</td>';
      });
      h+='</tr>';
    });
    h+='</tbody></table>';
  }

  h+=`<div class="akm-footer"><strong>Spread Notu:</strong> %3 üzeri iyi marj, %1.5 altı dar spread. | ${fmtDate(new Date())}</div>`;
  return h;
}

// ═══════════════════════════════════════════════════════
// 2. SON İŞLEMLER (filtre destekli)
// ═══════════════════════════════════════════════════════
async function fetchRecentTx(filters){
  const data=await apiFetch('/exchange/dashboard');
  if(!data?.offices)return '<div class="akm-empty">İşlem bulunamadı</div>';

  const selectedOffice=filters?.officeId||'';
  let txs=[];
  data.offices.forEach(o=>{
    if(o.officeName==='ANA KASA')return;
    if(selectedOffice&&o.officeId!==selectedOffice)return;
    if(o.recentTransactions)o.recentTransactions.forEach(t=>{
      txs.push({...t,officeName:o.officeName});
    });
  });

  // Filter by date range if applicable
  if(filters?.dateRange){
    const startD=new Date(filters.dateRange.start);
    const endD=new Date(filters.dateRange.end);endD.setHours(23,59,59);
    txs=txs.filter(t=>{
      const td=new Date(t.date||t.createdAt||t.createdDate);
      return td>=startD&&td<=endD;
    });
  }

  txs.sort((a,b)=>new Date(b.date||b.createdAt||b.createdDate)-new Date(a.date||a.createdAt||a.createdDate));
  txs=txs.slice(0,30);

  if(txs.length===0)return '<div class="akm-empty">Seçilen dönem/şubede işlem bulunamadı</div>';

  let totalBuy=0,totalSell=0,buyCount=0,sellCount=0;
  txs.forEach(t=>{
    const amt=Number(t.targetAmount||t.amount||0);
    if(t.type==='BUY'||t.isBuy){totalBuy+=amt;buyCount++}else{totalSell+=amt;sellCount++}
  });

  let h='<div class="akm-stats">';
  h+=`<div class="akm-stat blue"><div class="akm-stat-label">Alış</div><div class="akm-stat-value">${buyCount}</div></div>`;
  h+=`<div class="akm-stat orange"><div class="akm-stat-label">Satış</div><div class="akm-stat-value">${sellCount}</div></div>`;
  h+=`<div class="akm-stat green"><div class="akm-stat-label">Toplam Alış TRY</div><div class="akm-stat-value">${fmt(totalBuy)}</div></div>`;
  h+=`<div class="akm-stat red"><div class="akm-stat-label">Toplam Satış TRY</div><div class="akm-stat-value">${fmt(totalSell)}</div></div>`;
  h+='</div>';

  h+='<table class="akm-table" style="margin-top:12px"><thead><tr><th>Tarih</th><th>Şube</th><th>Tür</th><th>Döviz</th><th>Miktar</th><th>Kur</th><th>TRY</th><th>K/Z</th></tr></thead><tbody>';
  txs.forEach(t=>{
    const isBuy=(t.type==='BUY'||t.isBuy);
    const type=isBuy?'<span style="color:#22c55e;font-weight:700">ALIŞ</span>':'<span style="color:#ef4444;font-weight:700">SATIŞ</span>';
    const curr=t.sourceCurrencyCode||t.currencyCode||'';
    const amount=Number(t.sourceAmount||t.amount||0);
    const rate=Number(t.exchangeRate||t.rate||0);
    const tryAmt=Number(t.targetAmount||t.totalAmount||(amount*rate)||0);
    const profit=Number(t.profit||t.profitLoss||0);
    const profitHtml=profit!==0?`<span style="color:${plColor(profit)};font-weight:600">${plSign(profit)}${fmt(profit)}</span>`:'<span style="color:#94a3b8">-</span>';
    h+=`<tr><td style="font-size:11px;white-space:nowrap">${fmtDate(t.date||t.createdAt||t.createdDate)}</td><td>${t.officeName||'-'}</td><td>${type}</td><td><strong>${curr}</strong></td><td>${fmtN(amount)}</td><td>${fmtN(rate)}</td><td>${fmt(tryAmt)}</td><td>${profitHtml}</td></tr>`;
  });
  h+='</tbody></table>';
  h+=`<div class="akm-footer">Gösterilen: ${txs.length} işlem | Şube: ${filters?.officeName||'Tümü'}</div>`;
  return h;
}

// ═══════════════════════════════════════════════════════
// 3. CARİ HESAPLAR
// ═══════════════════════════════════════════════════════
async function fetchParty(){
  try{
    const [summary,parties]=await Promise.all([
      apiFetch('/exchange/party/reports/balance-summary').catch(()=>null),
      apiFetch('/exchange/parties?page=1&pageSize=50').catch(()=>null)
    ]);
    let h='<div class="akm-stats">';
    if(summary){
      h+=`<div class="akm-stat blue"><div class="akm-stat-label">Toplam Alacak</div><div class="akm-stat-value">${fmt(summary.totalReceivables)}</div></div>`;
      h+=`<div class="akm-stat red"><div class="akm-stat-label">Toplam Borç</div><div class="akm-stat-value">${fmt(summary.totalDebts||summary.totalPayables)}</div></div>`;
      h+=`<div class="akm-stat ${plClass(summary.netBalance)}"><div class="akm-stat-label">Net Bakiye</div><div class="akm-stat-value">${fmt(summary.netBalance)}</div></div>`;
      h+=`<div class="akm-stat purple"><div class="akm-stat-label">Aktif Cari</div><div class="akm-stat-value">${summary.activeAccounts||summary.totalAccounts||0}</div></div>`;
    }
    h+='</div>';
    const items=parties?.items||parties;
    if(items&&items.length>0){
      h+='<table class="akm-table" style="margin-top:12px"><thead><tr><th>Cari Adı</th><th>Telefon</th><th>Bakiye</th><th>Durum</th></tr></thead><tbody>';
      items.slice(0,15).forEach(p=>{
        const bal=Number(p.balance||0);
        const status=bal>0?badge('ALACAK','#3b82f6'):bal<0?badge('BORÇ','#ef4444'):badge('DENGEDE','#94a3b8');
        h+=`<tr><td><strong>${p.name||p.partyName||'-'}</strong></td><td>${p.phone||p.phoneNumber||'-'}</td><td style="color:${plColor(bal)};font-weight:600">${fmt(bal)}</td><td>${status}</td></tr>`;
      });
      h+='</tbody></table>';
    }
    if(!summary&&(!items||items.length===0))return '<div class="akm-empty">Cari hesap verisi bulunamadı</div>';
    return h;
  }catch(e){return `<div class="akm-empty">Cari hesap verisi yüklenemedi</div>`;}
}

// ═══════════════════════════════════════════════════════
// 4. KASA DURUMU
// ═══════════════════════════════════════════════════════
async function fetchVaults(){
  const data=await apiFetch('/exchange/dashboard');
  if(!data?.offices)return '<div class="akm-empty">Kasa verisi bulunamadı</div>';

  const currTotals={};
  let grandTotal=0;
  let h='';

  data.offices.forEach(o=>{
    if(o.officeName==='ANA KASA')return;
    if(!o.vaults||o.vaults.length===0)return;
    let officeTotal=0;
    h+=section(o.officeName,'');
    h+='<table class="akm-table"><thead><tr><th>Kasa</th><th>Döviz</th><th>Bakiye</th><th>TRY Karşılığı</th><th>Dağılım</th></tr></thead><tbody>';
    o.vaults.forEach(v=>{
      const balances=(v.balances||v.vaultBalances||[]).filter(b=>Number(b.amount)!==0);
      const vaultTotal=balances.reduce((s,b)=>s+Number(b.valueInBaseCurrency||0),0);
      balances.forEach((b,i)=>{
        const amt=Number(b.amount);const tryVal=Number(b.valueInBaseCurrency||0);
        officeTotal+=tryVal;
        if(!currTotals[b.currencyCode])currTotals[b.currencyCode]={amount:0,tryVal:0};
        currTotals[b.currencyCode].amount+=amt;currTotals[b.currencyCode].tryVal+=tryVal;
        const pct=vaultTotal>0?(tryVal/vaultTotal*100).toFixed(1):'0';
        h+=`<tr><td>${i===0?(v.name||v.vaultName||'-'):''}</td><td><strong>${b.currencyCode}</strong></td><td>${fmtN(amt)}</td><td>${fmt(tryVal)}</td><td><div style="display:flex;align-items:center;gap:6px"><div style="width:60px;height:6px;background:#e2e8f0;border-radius:3px;overflow:hidden"><div style="width:${pct}%;height:100%;background:#6366f1;border-radius:3px"></div></div><span style="font-size:10px;color:#64748b">${pct}%</span></div></td></tr>`;
      });
    });
    grandTotal+=officeTotal;
    h+=`<tr class="akm-total-row"><td colspan="3" style="font-weight:700">Şube Toplamı</td><td style="font-weight:700">${fmt(officeTotal)}</td><td></td></tr>`;
    h+='</tbody></table>';
  });

  if(Object.keys(currTotals).length>0){
    h+=section('Birleşik Döviz Dağılımı','');
    h+='<table class="akm-table"><thead><tr><th>Döviz</th><th>Toplam Miktar</th><th>TRY Karşılığı</th><th>Portföy Payı</th></tr></thead><tbody>';
    Object.entries(currTotals).sort((a,b)=>b[1].tryVal-a[1].tryVal).forEach(([code,d])=>{
      const pct=grandTotal>0?(d.tryVal/grandTotal*100).toFixed(1):'0';
      h+=`<tr><td><strong>${code}</strong></td><td>${fmtN(d.amount)}</td><td style="font-weight:600">${fmt(d.tryVal)}</td><td><div style="display:flex;align-items:center;gap:6px"><div style="width:80px;height:8px;background:#e2e8f0;border-radius:4px;overflow:hidden"><div style="width:${pct}%;height:100%;background:${code==='TRY'?'#22c55e':code==='USD'?'#3b82f6':code==='EUR'?'#8b5cf6':'#f97316'};border-radius:4px"></div></div><span style="font-size:11px;font-weight:600">${pct}%</span></div></td></tr>`;
    });
    h+=`<tr class="akm-total-row"><td style="font-weight:700">GENEL TOPLAM</td><td></td><td style="font-weight:700;font-size:15px">${fmt(grandTotal)}</td><td></td></tr>`;
    h+='</tbody></table>';
  }
  if(!h)return '<div class="akm-empty">Kasa bakiye verisi bulunamadı</div>';
  return h;
}

// ═══════════════════════════════════════════════════════
// 5. K/Z RAPORU (filtre destekli + şube karşılaştırması)
// ═══════════════════════════════════════════════════════
async function fetchPnL(filters){
  const period=filters?.period||'month';
  const officeId=filters?.officeId||'';

  // Determine which Z report endpoint to use based on period
  let zEndpoint='/exchange/reports/z-report/monthly';
  if(period==='today')zEndpoint='/exchange/reports/z-report/daily';
  else if(period==='week')zEndpoint='/exchange/reports/z-report/weekly';
  else if(period==='year')zEndpoint='/exchange/reports/z-report/yearly';

  // If specific office selected, add officeId parameter
  const epSuffix=officeId?`?officeId=${officeId}`:'';
  const zReport=await apiFetch(zEndpoint+epSuffix);
  if(!zReport?.summary)return '<div class="akm-empty">K/Z raporu bulunamadı</div>';

  const s=zReport.summary;
  const currencies=zReport.currencyDetails||[];
  const periodLabel={'today':'Bugün','week':'Bu Hafta','month':'Bu Ay','year':'Bu Yıl','custom':'Özel Dönem'}[period]||'Bu Ay';

  // === BÖLÜM 1: GENEL ÖZET ===
  let h=`<div class="akm-info" style="margin-bottom:12px"><strong>${periodLabel}</strong> raporu ${filters?.officeName||'Tüm Şubeler'} için gösteriliyor.</div>`;
  h+='<div class="akm-stats">';
  h+=`<div class="akm-stat ${plClass(s.totalProfit)}"><div class="akm-stat-label">Toplam K/Z</div><div class="akm-stat-value" style="color:${plColor(s.totalProfit)}">${plSign(s.totalProfit)}${fmt(s.totalProfit)}</div></div>`;
  h+=`<div class="akm-stat blue"><div class="akm-stat-label">İşlem Sayısı</div><div class="akm-stat-value">${s.totalTransactions||0}</div></div>`;
  h+=`<div class="akm-stat purple"><div class="akm-stat-label">İşlem Hacmi</div><div class="akm-stat-value">${fmt(s.totalForeignCurrencyProcessed)}</div></div>`;
  h+=`<div class="akm-stat orange"><div class="akm-stat-label">Kâr Marjı</div><div class="akm-stat-value">${fmtPct(s.profitMargin)}</div></div>`;
  h+='</div>';

  // === BÖLÜM 2: DÖVİZ BAZLI DETAYLI ANALİZ ===
  if(currencies.length>0){
    h+=section('Döviz Bazlı Kâr/Zarar Analizi','');
    currencies.forEach(c=>{
      if(c.buyTransactionCount===0&&c.sellTransactionCount===0)return;
      const isProfit=(c.profit||0)>=0;
      const profitBg=isProfit?'rgba(34,197,94,0.05)':'rgba(239,68,68,0.05)';
      const profitBorder=isProfit?'#22c55e':'#ef4444';

      h+=`<div style="border:1px solid ${profitBorder};border-radius:10px;padding:14px;margin-bottom:12px;background:${profitBg}">`;
      h+=`<div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:10px">`;
      h+=`<div style="font-size:15px;font-weight:700">${c.currencyCode} - ${c.currencyName}</div>`;
      h+=`<div style="font-size:16px;font-weight:800;color:${plColor(c.profit)}">${plSign(c.profit)}${fmt(c.profit)} ${badge(fmtPct(c.profitMargin),isProfit?'#22c55e':'#ef4444')}</div>`;
      h+=`</div>`;

      h+=`<table class="akm-table" style="margin-bottom:8px"><thead><tr><th></th><th>İşlem Adedi</th><th>Toplam Miktar</th><th>Toplam TRY</th><th>Ort. Kur</th></tr></thead><tbody>`;
      h+=`<tr><td style="color:#22c55e;font-weight:700">ALIŞ</td><td>${c.buyTransactionCount}</td><td>${fmtN(c.totalBoughtAmount)} ${c.currencyCode}</td><td>${fmt(c.totalBuyCost)}</td><td style="font-weight:600">${fmtN(c.averageBuyRate)}</td></tr>`;
      h+=`<tr><td style="color:#ef4444;font-weight:700">SATIŞ</td><td>${c.sellTransactionCount}</td><td>${fmtN(c.totalSoldAmount)} ${c.currencyCode}</td><td>${fmt(c.totalSellRevenue)}</td><td style="font-weight:600">${c.averageSellRate?fmtN(c.averageSellRate):'-'}</td></tr>`;
      h+=`</tbody></table>`;

      h+=`<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:8px;font-size:12px">`;
      const netPos=c.netPosition||0;
      h+=`<div style="background:rgba(99,102,241,0.08);padding:8px 10px;border-radius:6px"><div style="color:#64748b;font-size:10px;font-weight:600">NET POZİSYON</div><div style="font-weight:700;font-size:14px">${fmtN(netPos)} ${c.currencyCode}</div><div style="color:#64748b;font-size:10px">${netPos>0?'Açık pozisyon (eldeki stok)':'Tüm stok satılmış'}</div></div>`;

      if(c.averageBuyRate>0&&c.averageSellRate>0){
        const actualSpread=c.averageSellRate-c.averageBuyRate;
        const spreadPct=(actualSpread/c.averageBuyRate*100);
        h+=`<div style="background:rgba(99,102,241,0.08);padding:8px 10px;border-radius:6px"><div style="color:#64748b;font-size:10px;font-weight:600">ALIŞ-SATIŞ FARKI</div><div style="font-weight:700;font-size:14px;color:${plColor(actualSpread)}">${plSign(actualSpread)}${fmtN(actualSpread)} ₺</div><div style="color:#64748b;font-size:10px">Kur farkı: ${fmtPct(spreadPct)}</div></div>`;
      }

      if(c.currentBuyRate>0){
        h+=`<div style="background:rgba(99,102,241,0.08);padding:8px 10px;border-radius:6px"><div style="color:#64748b;font-size:10px;font-weight:600">GÜNCEL PİYASA</div><div style="font-size:11px">Alış: <strong>${fmtN(c.currentBuyRate)}</strong> | Satış: <strong>${fmtN(c.currentSellRate)}</strong></div><div style="color:#64748b;font-size:10px">Spread: ${fmtN(c.currentSellRate-c.currentBuyRate)} ₺</div></div>`;
      }

      if(netPos>0&&c.currentSellRate>0&&c.averageBuyRate>0){
        const unrealizedPL=netPos*(c.currentSellRate-c.averageBuyRate);
        h+=`<div style="background:rgba(99,102,241,0.08);padding:8px 10px;border-radius:6px"><div style="color:#64748b;font-size:10px;font-weight:600">GERÇEKLEŞMEMİŞ K/Z</div><div style="font-weight:700;font-size:14px;color:${plColor(unrealizedPL)}">${plSign(unrealizedPL)}${fmt(unrealizedPL)}</div><div style="color:#64748b;font-size:10px">${fmtN(netPos)} ${c.currencyCode} × (${fmtN(c.currentSellRate)} - ${fmtN(c.averageBuyRate)})</div></div>`;
      }
      h+=`</div>`;

      // Loss analysis
      if(c.profit<0){
        const reason=analyzeReason(c);
        h+=`<div style="margin-top:8px;padding:8px 12px;background:rgba(239,68,68,0.08);border-left:3px solid #ef4444;border-radius:0 6px 6px 0;font-size:12px"><strong style="color:#ef4444">Zarar Analizi:</strong> ${reason}</div>`;
      }
      h+=`</div>`;
    });
  }

  // === BÖLÜM 3: ŞUBE KARŞILAŞTIRMASI (sadece Tüm Şubeler seçiliyse) ===
  if(!officeId){
    const offices=(zReport.officeBreakdown||[]).filter(o=>o.officeName!=='ANA KASA');
    if(offices.length>1){
      h+=section('Şube Performans Karşılaştırması','');
      h+='<table class="akm-table"><thead><tr><th>Şube</th><th>İşlem</th><th>Toplam K/Z</th><th>Hacim</th><th>Marj</th><th>Performans</th></tr></thead><tbody>';

      let totalTx=0,totalPL=0,totalVol=0;
      offices.forEach(o=>{
        const os=o.summary||{};
        const perf=Number(os.profitMargin||0);
        totalTx+=(os.totalTransactions||0);
        totalPL+=(os.totalProfit||0);
        totalVol+=(os.totalForeignCurrencyProcessed||0);
        const perfBar=`<div style="display:flex;align-items:center;gap:4px"><div style="width:50px;height:6px;background:#e2e8f0;border-radius:3px;overflow:hidden"><div style="width:${Math.min(perf*10,100)}%;height:100%;background:${perf>3?'#22c55e':perf>1?'#eab308':'#ef4444'};border-radius:3px"></div></div><span style="font-size:10px">${fmtPct(perf)}</span></div>`;
        h+=`<tr><td><strong>${o.officeName}</strong></td><td>${os.totalTransactions||0}</td><td style="color:${plColor(os.totalProfit)};font-weight:700">${plSign(os.totalProfit)}${fmt(os.totalProfit)}</td><td>${fmt(os.totalForeignCurrencyProcessed)}</td><td>${fmtPct(os.profitMargin)}</td><td>${perfBar}</td></tr>`;
      });
      h+=`<tr class="akm-total-row"><td><strong>TOPLAM</strong></td><td><strong>${totalTx}</strong></td><td style="color:${plColor(totalPL)};font-weight:700"><strong>${plSign(totalPL)}${fmt(totalPL)}</strong></td><td><strong>${fmt(totalVol)}</strong></td><td></td><td></td></tr>`;
      h+='</tbody></table>';

      // Per-office currency breakdown comparison
      h+=section('Şube-Döviz Karşılaştırması','');
      const allCurrCodes=new Set();
      offices.forEach(o=>(o.currencyDetails||[]).forEach(c=>{if(c.buyTransactionCount>0||c.sellTransactionCount>0)allCurrCodes.add(c.currencyCode)}));

      if(allCurrCodes.size>0){
        h+='<table class="akm-table"><thead><tr><th>Döviz</th>';
        offices.forEach(o=>{h+=`<th colspan="2">${o.officeName}</th>`});
        h+='</tr><tr><th></th>';
        offices.forEach(()=>{h+='<th style="font-size:9px">K/Z</th><th style="font-size:9px">Marj</th>'});
        h+='</tr></thead><tbody>';
        allCurrCodes.forEach(code=>{
          h+=`<tr><td><strong>${code}</strong></td>`;
          offices.forEach(o=>{
            const cd=(o.currencyDetails||[]).find(c=>c.currencyCode===code);
            if(cd){
              h+=`<td style="color:${plColor(cd.profit)};font-weight:600">${plSign(cd.profit)}${fmt(cd.profit)}</td><td>${fmtPct(cd.profitMargin)}</td>`;
            }else{
              h+='<td style="color:#94a3b8">-</td><td>-</td>';
            }
          });
          h+='</tr>';
        });
        h+='</tbody></table>';
      }
    }
  }

  // === BÖLÜM 4: İŞLEM HACMİ ===
  if(s.totalVolumesByCurrency&&Object.keys(s.totalVolumesByCurrency).length>0){
    h+=section('İşlem Hacmi Dağılımı','');
    const totalVol=Object.values(s.totalVolumesByCurrency).reduce((a,b)=>a+b,0);
    h+='<div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(120px,1fr));gap:8px">';
    Object.entries(s.totalVolumesByCurrency).sort((a,b)=>b[1]-a[1]).forEach(([code,vol])=>{
      const pct=(vol/totalVol*100).toFixed(1);
      const colors={USD:'#3b82f6',EUR:'#8b5cf6',TRY:'#22c55e',GBP:'#f97316',USDT:'#06b6d4',KRUB:'#ef4444',RUB:'#ef4444'};
      h+=`<div style="background:#f8fafc;border-radius:8px;padding:10px;text-align:center;border:1px solid #e2e8f0"><div style="font-size:10px;color:#64748b;font-weight:600">${code}</div><div style="font-size:15px;font-weight:800;color:${colors[code]||'#475569'}">${fmtN(vol)}</div><div style="font-size:10px;color:#94a3b8">${pct}%</div></div>`;
    });
    h+='</div>';
  }

  h+=`<div class="akm-footer">${periodLabel} | ${filters?.officeName||'Tüm Şubeler'} | İşlem: ${s.totalTransactions} | Marj: ${fmtPct(s.profitMargin)}</div>`;
  return h;
}

function analyzeReason(c){
  const reasons=[];
  if(c.totalSoldAmount===0&&c.totalBoughtAmount>0){
    reasons.push(`Henüz satış yapılmamış - ${fmtN(c.totalBoughtAmount)} ${c.currencyCode} stokta. Zarar, piyasa kurundaki değer düşüklüğünden.`);
  }
  if(c.averageSellRate>0&&c.averageSellRate<c.averageBuyRate){
    reasons.push(`Satış kuru (${fmtN(c.averageSellRate)}) alış kurundan (${fmtN(c.averageBuyRate)}) düşük. Zamanlama hatası veya piyasa düşüşü.`);
  }
  if(c.currentBuyRate>0&&c.averageBuyRate>0){
    const rateChange=((c.currentBuyRate-c.averageBuyRate)/c.averageBuyRate*100);
    if(rateChange<-2)reasons.push(`Piyasa kuru ${fmtPct(Math.abs(rateChange))} düşmüş. Kur riski gerçekleşmiş.`);
  }
  if(reasons.length===0)reasons.push('Spread yetersizliği veya olumsuz kur hareketi.');
  return reasons.join(' ');
}

// ═══════════════════════════════════════════════════════
// 6. Z RAPORU (filtre destekli)
// ═══════════════════════════════════════════════════════
async function fetchZReport(filters){
  const period=filters?.period||'month';
  const officeId=filters?.officeId||'';

  // Fetch both daily and selected period
  const epMap={'today':'daily','week':'weekly','month':'monthly','year':'yearly','custom':'monthly'};
  const mainEp=`/exchange/reports/z-report/${epMap[period]||'monthly'}`;
  const epSuffix=officeId?`?officeId=${officeId}`:'';

  const [daily,main]=await Promise.all([
    apiFetch('/exchange/reports/z-report/daily'+epSuffix).catch(()=>null),
    period!=='today'?apiFetch(mainEp+epSuffix).catch(()=>null):null
  ]);

  const periodLabel={'today':'Bugün','week':'Bu Hafta','month':'Bu Ay','year':'Bu Yıl'}[period]||'Bu Ay';
  if(!daily&&!main)return '<div class="akm-empty">Z Raporu bulunamadı</div>';

  let h=`<div class="akm-info" style="margin-bottom:12px"><strong>${periodLabel}</strong> Z raporu ${filters?.officeName||'Tüm Şubeler'} için gösteriliyor.</div>`;

  // Daily section
  if(daily?.summary){
    const ds=daily.summary;
    h+=section('Günlük Z Raporu','');
    h+='<div class="akm-stats">';
    h+=`<div class="akm-stat ${plClass(ds.totalProfit)}"><div class="akm-stat-label">Günlük K/Z</div><div class="akm-stat-value" style="color:${plColor(ds.totalProfit)}">${plSign(ds.totalProfit)}${fmt(ds.totalProfit)}</div></div>`;
    h+=`<div class="akm-stat blue"><div class="akm-stat-label">İşlem</div><div class="akm-stat-value">${ds.totalTransactions||0}</div></div>`;
    h+=`<div class="akm-stat purple"><div class="akm-stat-label">Hacim</div><div class="akm-stat-value">${fmt(ds.totalForeignCurrencyProcessed)}</div></div>`;
    h+=`<div class="akm-stat orange"><div class="akm-stat-label">Durum</div><div class="akm-stat-value">${daily.isVaultOpen?'AÇIK':'KAPALI'}</div></div>`;
    h+='</div>';

    if(daily.currencyDetails?.length>0){
      h+='<table class="akm-table" style="margin-top:8px"><thead><tr><th>Döviz</th><th>Alış</th><th>Satış</th><th>Ort. Alış Kur</th><th>Ort. Satış Kur</th><th>K/Z</th></tr></thead><tbody>';
      daily.currencyDetails.forEach(c=>{
        if(c.buyTransactionCount===0&&c.sellTransactionCount===0)return;
        h+=`<tr><td><strong>${c.currencyCode}</strong></td><td>${c.buyTransactionCount} (${fmtN(c.totalBoughtAmount)})</td><td>${c.sellTransactionCount} (${fmtN(c.totalSoldAmount)})</td><td>${fmtN(c.averageBuyRate)}</td><td>${c.averageSellRate?fmtN(c.averageSellRate):'-'}</td><td style="color:${plColor(c.profit)};font-weight:700">${plSign(c.profit)}${fmt(c.profit)}</td></tr>`;
      });
      h+='</tbody></table>';
    }
  }

  // Period report
  if(main?.summary&&period!=='today'){
    const ms=main.summary;
    h+=section(`${periodLabel} Özet`,'');
    h+='<div class="akm-stats">';
    h+=`<div class="akm-stat ${plClass(ms.totalProfit)}"><div class="akm-stat-label">${periodLabel} K/Z</div><div class="akm-stat-value" style="color:${plColor(ms.totalProfit)}">${plSign(ms.totalProfit)}${fmt(ms.totalProfit)}</div></div>`;
    h+=`<div class="akm-stat blue"><div class="akm-stat-label">Toplam İşlem</div><div class="akm-stat-value">${ms.totalTransactions||0}</div></div>`;
    h+=`<div class="akm-stat purple"><div class="akm-stat-label">Ort. İşlem</div><div class="akm-stat-value">${fmt(ms.averageTransactionSize)}</div></div>`;
    h+=`<div class="akm-stat orange"><div class="akm-stat-label">Genel Marj</div><div class="akm-stat-value">${fmtPct(ms.profitMargin)}</div></div>`;
    h+='</div>';

    if(ms.vaultDeposits>0||ms.vaultWithdrawals>0){
      h+=`<div style="margin-top:10px;display:grid;grid-template-columns:1fr 1fr 1fr;gap:8px">
        <div style="background:#f0fdf4;padding:10px;border-radius:8px;text-align:center;border:1px solid #bbf7d0"><div style="font-size:10px;color:#15803d;font-weight:600">KASA GİRİŞ</div><div style="font-size:15px;font-weight:700;color:#22c55e">${fmt(ms.vaultDeposits)}</div></div>
        <div style="background:#fef2f2;padding:10px;border-radius:8px;text-align:center;border:1px solid #fecaca"><div style="font-size:10px;color:#b91c1c;font-weight:600">KASA ÇIKIŞ</div><div style="font-size:15px;font-weight:700;color:#ef4444">${fmt(ms.vaultWithdrawals)}</div></div>
        <div style="background:#eff6ff;padding:10px;border-radius:8px;text-align:center;border:1px solid #bfdbfe"><div style="font-size:10px;color:#1d4ed8;font-weight:600">NET HAREKET</div><div style="font-size:15px;font-weight:700;color:${plColor(ms.netVaultChange)}">${plSign(ms.netVaultChange)}${fmt(ms.netVaultChange)}</div></div>
      </div>`;
    }
  }

  // Office breakdown (when all offices selected)
  const report=main||daily;
  if(!officeId){
    const offices=(report?.officeBreakdown||[]).filter(o=>o.officeName!=='ANA KASA');
    if(offices.length>1){
      h+=section('Şube Karşılaştırması','');
      h+='<table class="akm-table"><thead><tr><th>Şube</th><th>İşlem</th><th>K/Z</th><th>Hacim</th><th>Marj</th></tr></thead><tbody>';
      offices.forEach(o=>{
        const os=o.summary||{};
        h+=`<tr><td><strong>${o.officeName}</strong></td><td>${os.totalTransactions||0}</td><td style="color:${plColor(os.totalProfit)};font-weight:700">${plSign(os.totalProfit)}${fmt(os.totalProfit)}</td><td>${fmt(os.totalForeignCurrencyProcessed)}</td><td>${fmtPct(os.profitMargin)}</td></tr>`;
      });
      h+='</tbody></table>';
    }
  }

  h+=`<div class="akm-footer">${periodLabel} | ${filters?.officeName||'Tüm Şubeler'} | Durum: ${daily?.isVaultOpen?'KASA AÇIK':'KASA KAPALI'}</div>`;
  return h;
}

// ═══════════════════════════════════════════════════════
// 7. TRANSFER (ANA KASA ↔ Şubeler)
// ═══════════════════════════════════════════════════════
async function fetchTransfer(){
  const data=await apiFetch('/exchange/dashboard');
  if(!data?.offices)return '<div class="akm-empty">Veri yüklenemedi</div>';
  const anaKasa=data.offices.find(o=>o.officeName==='ANA KASA');
  const branches=data.offices.filter(o=>o.officeName!=='ANA KASA');
  if(!anaKasa||!anaKasa.vaults||anaKasa.vaults.length===0)return '<div class="akm-empty">ANA KASA kasası bulunamadı</div>';

  const akVault=anaKasa.vaults[0];
  const akVaultId=akVault.vaultId;
  const akCurrencies=akVault.currencies||[];
  const akBalances=akCurrencies.filter(c=>c.balance>0);

  window.__akmTrData={
    akVaultId,
    akCurrencies,
    branches:branches.map(b=>({
      officeId:b.officeId,
      officeName:b.officeName,
      vaultId:b.vaults[0]?.vaultId||'',
      currencies:b.vaults[0]?.currencies||[]
    }))
  };

  let h='';
  let grandTotal=anaKasa.totalValueInBaseCurrency||0;
  branches.forEach(b=>{grandTotal+=(b.totalValueInBaseCurrency||0)});

  // Özet kartları
  h+='<div class="akm-stats">';
  h+=`<div class="akm-stat blue"><div class="akm-stat-label">ANA KASA</div><div class="akm-stat-value">${fmt(anaKasa.totalValueInBaseCurrency)}</div></div>`;
  branches.forEach(b=>{
    h+=`<div class="akm-stat purple"><div class="akm-stat-label">${b.officeName}</div><div class="akm-stat-value">${fmt(b.totalValueInBaseCurrency)}</div></div>`;
  });
  h+=`<div class="akm-stat green"><div class="akm-stat-label">Genel Toplam</div><div class="akm-stat-value">${fmt(grandTotal)}</div></div>`;
  h+='</div>';

  // Dağılım çubuğu
  if(grandTotal>0){
    const akPct=((anaKasa.totalValueInBaseCurrency||0)/grandTotal*100).toFixed(1);
    h+=`<div style="margin:10px 0;background:#e2e8f0;border-radius:6px;height:24px;overflow:hidden;display:flex;font-size:10px;font-weight:700">`;
    h+=`<div style="width:${akPct}%;background:linear-gradient(90deg,#3b82f6,#1d4ed8);color:#fff;display:flex;align-items:center;justify-content:center;min-width:40px" title="ANA KASA">ANA %${akPct}</div>`;
    branches.forEach((b,i)=>{
      const bPct=((b.totalValueInBaseCurrency||0)/grandTotal*100).toFixed(1);
      const colors=['#8b5cf6','#f97316','#06b6d4','#22c55e'];
      h+=`<div style="width:${bPct}%;background:${colors[i%colors.length]};color:#fff;display:flex;align-items:center;justify-content:center;min-width:40px" title="${b.officeName}">${b.officeName.substring(0,4)} %${bPct}</div>`;
    });
    h+=`</div>`;
  }

  // Transfer formu
  h+=section('Yeni Transfer','');
  h+=`<div style="display:grid;grid-template-columns:1fr 1fr;gap:8px;margin-bottom:8px">
    <div><label style="font-size:11px;color:#64748b;font-weight:600;display:block;margin-bottom:2px">Yön</label>
    <select id="akm-tr-dir" class="akm-select" style="width:100%;box-sizing:border-box" onchange="window.__akmTrDirChange()">
      <option value="send">ANA KASA \u2192 Şube (Gönder)</option>
      <option value="receive">Şube \u2192 ANA KASA (Al)</option>
    </select></div>
    <div><label style="font-size:11px;color:#64748b;font-weight:600;display:block;margin-bottom:2px">Şube</label>
    <select id="akm-tr-branch" class="akm-select" style="width:100%;box-sizing:border-box" onchange="window.__akmTrBranchChange()">
      <option value="">Şube Seçin</option>
      ${branches.map(b=>`<option value="${b.officeId}">${b.officeName}</option>`).join('')}
    </select></div>
  </div>
  <div style="display:grid;grid-template-columns:1fr 1fr 2fr;gap:8px;margin-bottom:12px">
    <div><label style="font-size:11px;color:#64748b;font-weight:600;display:block;margin-bottom:2px">Döviz</label>
    <select id="akm-tr-curr" class="akm-select" style="width:100%;box-sizing:border-box">
      <option value="">Döviz Seçin</option>
    </select></div>
    <div><label style="font-size:11px;color:#64748b;font-weight:600;display:block;margin-bottom:2px">Tutar</label>
    <input type="number" id="akm-tr-amt" class="akm-input" style="width:100%;box-sizing:border-box" placeholder="0.00" step="0.01" min="0"></div>
    <div><label style="font-size:11px;color:#64748b;font-weight:600;display:block;margin-bottom:2px">Not</label>
    <div style="display:flex;gap:6px">
      <input type="text" id="akm-tr-notes" class="akm-input" style="flex:1" placeholder="Transfer notu">
      <button onclick="window.__akmTrExec()" class="akm-filter-apply" style="padding:8px 20px;white-space:nowrap">Transfer Yap</button>
    </div></div>
  </div>`;

  // ANA KASA bakiye detayı
  if(akBalances.length>0){
    h+=section('ANA KASA Bakiyeleri','');
    h+='<table class="akm-table"><thead><tr><th>Döviz</th><th>Bakiye</th><th>TRY Karşılığı</th></tr></thead><tbody>';
    akBalances.forEach(c=>{
      h+=`<tr><td><strong>${c.currencyCode}</strong></td><td>${fmtN(c.balance)}</td><td>${fmt(c.valueInBaseCurrency)}</td></tr>`;
    });
    h+='</tbody></table>';
  }

  // Şube bakiyeleri
  h+=section('Şube Bakiyeleri','');
  h+='<table class="akm-table"><thead><tr><th>Şube</th><th>Döviz</th><th>Bakiye</th><th>TRY Karşılığı</th></tr></thead><tbody>';
  let hasBranch=false;
  branches.forEach(b=>{
    b.vaults.forEach(v=>{
      const nonZero=(v.currencies||[]).filter(c=>c.balance>0);
      nonZero.forEach((c,i)=>{
        hasBranch=true;
        h+=`<tr><td>${i===0?'<strong>'+b.officeName+'</strong>':''}</td><td><strong>${c.currencyCode}</strong></td><td>${fmtN(c.balance)}</td><td>${fmt(c.valueInBaseCurrency)}</td></tr>`;
      });
    });
  });
  if(!hasBranch)h+='<tr><td colspan="4" style="text-align:center;color:#94a3b8">Şubelerde bakiye yok</td></tr>';
  h+='</tbody></table>';

  h+=`<div class="akm-footer">${fmtDate(new Date())}</div>`;
  return h;
}

// Transfer yardımcı fonksiyonları
window.__akmTrDirChange=function(){
  const branchId=document.getElementById('akm-tr-branch')?.value;
  if(branchId)window.__akmTrBranchChange();
};

window.__akmTrBranchChange=function(){
  const dir=document.getElementById('akm-tr-dir')?.value;
  const branchId=document.getElementById('akm-tr-branch')?.value;
  const currSelect=document.getElementById('akm-tr-curr');
  if(!branchId||!currSelect)return;
  const d=window.__akmTrData;
  const branch=d.branches.find(b=>b.officeId===branchId);
  let currencies;
  if(dir==='send'){
    currencies=d.akCurrencies.filter(c=>c.balance>0);
  }else{
    currencies=(branch?.currencies||[]).filter(c=>c.balance>0);
  }
  currSelect.innerHTML='<option value="">Döviz Seçin</option>';
  currencies.forEach(c=>{
    const opt=document.createElement('option');
    opt.value=c.currencyId;
    opt.textContent=`${c.currencyCode} (Bakiye: ${Number(c.balance).toLocaleString('tr-TR',{minimumFractionDigits:2})})`;
    currSelect.appendChild(opt);
  });
};

window.__akmTrExec=async function(){
  const dir=document.getElementById('akm-tr-dir')?.value;
  const branchId=document.getElementById('akm-tr-branch')?.value;
  const currencyId=document.getElementById('akm-tr-curr')?.value;
  const amount=parseFloat(document.getElementById('akm-tr-amt')?.value);
  const notes=document.getElementById('akm-tr-notes')?.value?.trim()||'ANA KASA Transfer';
  if(!branchId||!currencyId||!amount||amount<=0){alert('Lütfen tüm alanları doldurun (şube, döviz, tutar).');return}
  const d=window.__akmTrData;
  const branch=d.branches.find(b=>b.officeId===branchId);
  if(!branch?.vaultId){alert('Hedef şubenin kasası bulunamadı.');return}
  const sourceVaultId=dir==='send'?d.akVaultId:branch.vaultId;
  const targetVaultId=dir==='send'?branch.vaultId:d.akVaultId;
  const dirLabel=dir==='send'?('ANA KASA \u2192 '+branch.officeName):(branch.officeName+' \u2192 ANA KASA');
  const currCode=document.getElementById('akm-tr-curr')?.selectedOptions[0]?.textContent?.split(' ')[0]||'';
  if(!confirm(dirLabel+'\nTutar: '+Number(amount).toLocaleString('tr-TR')+' '+currCode+'\nOnaylıyor musunuz?'))return;
  const token=getToken();
  if(!token){alert('Oturum bulunamadı.');return}
  try{
    const res=await fetch(API+'/exchange/transfer',{
      method:'POST',
      headers:{'Content-Type':'application/json','Authorization':'Bearer '+token},
      body:JSON.stringify({sourceVaultId,targetVaultId,currencyId,amount,notes})
    });
    if(!res.ok){const err=await res.text();throw new Error(err)}
    const result=JSON.parse(await res.text()||'{}');
    alert('Transfer başarılı!\nİşlem No: '+(result.transactionNumber||'-'));
    _officesCache=null;
    const body=document.getElementById('akm-modal-body');
    if(body){body.innerHTML='<div class="akm-loading"><div class="akm-spinner"></div>Güncelleniyor...</div>';body.innerHTML=await fetchTransfer()}
  }catch(e){
    alert('Transfer hatası: '+e.message);
  }
};

// ═══════════════════════════════════════════════════════
// 8. SERMAYE YÖNETİMİ (Capital Management)
// ═══════════════════════════════════════════════════════
const CAPITAL_KEY='akm_capital_entries';
function getCapitalEntries(){try{return JSON.parse(localStorage.getItem(CAPITAL_KEY)||'[]')}catch(e){return[]}}
function saveCapitalEntries(entries){localStorage.setItem(CAPITAL_KEY,JSON.stringify(entries))}
function addCapitalEntry(entry){const entries=getCapitalEntries();entry.id=Date.now();entries.push(entry);saveCapitalEntries(entries);return entries}
function deleteCapitalEntry(id){const entries=getCapitalEntries().filter(e=>e.id!==id);saveCapitalEntries(entries);return entries}

async function fetchCapital(){
  // Mevcut toplam varlıkları dashboard'dan al
  let totalAssets=0;
  try{
    const data=await apiFetch('/exchange/dashboard');
    if(data?.summary){totalAssets=data.summary.totalAssets||data.summary.totalAssetsInTRY||0}
    else if(data?.offices){totalAssets=data.offices.filter(o=>o.officeName!=='ANA KASA').reduce((s,o)=>s+(o.totalValueInBaseCurrency||0),0)}
  }catch(e){}

  const entries=getCapitalEntries();
  const totalInvested=entries.filter(e=>e.type==='deposit').reduce((s,e)=>s+Number(e.amount),0);
  const totalWithdrawn=entries.filter(e=>e.type==='withdrawal').reduce((s,e)=>s+Number(e.amount),0);
  const netCapital=totalInvested-totalWithdrawn;
  const roiAmount=totalAssets-netCapital;
  const roi=netCapital>0?((totalAssets-netCapital)/netCapital*100):0;
  const preservationRatio=netCapital>0?(totalAssets/netCapital*100):0;

  let h='';

  // === ÖZET KARTLARI ===
  h+='<div class="akm-stats">';
  h+=`<div class="akm-stat green"><div class="akm-stat-label">Toplam Yatırım</div><div class="akm-stat-value">${fmt(totalInvested)}</div></div>`;
  h+=`<div class="akm-stat red"><div class="akm-stat-label">Toplam Çekim</div><div class="akm-stat-value">${fmt(totalWithdrawn)}</div></div>`;
  h+=`<div class="akm-stat blue"><div class="akm-stat-label">Net Sermaye</div><div class="akm-stat-value">${fmt(netCapital)}</div></div>`;
  h+=`<div class="akm-stat purple"><div class="akm-stat-label">Mevcut Varlık</div><div class="akm-stat-value">${fmt(totalAssets)}</div></div>`;
  h+=`<div class="akm-stat ${plClass(roiAmount)}"><div class="akm-stat-label">Getiri/Kayıp</div><div class="akm-stat-value" style="color:${plColor(roiAmount)}">${plSign(roiAmount)}${fmt(roiAmount)}</div></div>`;
  h+='</div>';

  // === SERMAYE KORUMA ORANI & ROI DETAY ===
  if(netCapital>0){
    h+=`<div style="display:grid;grid-template-columns:1fr 1fr 1fr;gap:8px;margin-top:10px">`;
    h+=`<div style="background:${preservationRatio>=100?'#f0fdf4':'#fef2f2'};padding:10px;border-radius:8px;text-align:center;border:1px solid ${preservationRatio>=100?'#bbf7d0':'#fecaca'}"><div style="font-size:10px;color:#64748b;font-weight:600">SERMAYE KORUMA ORANI</div><div style="font-size:18px;font-weight:800;color:${preservationRatio>=100?'#22c55e':'#ef4444'}">${fmtPct(preservationRatio)}</div><div style="font-size:10px;color:#64748b">${preservationRatio>=100?'Sermaye korunuyor!':'Sermaye eriyor!'}</div></div>`;
    h+=`<div style="background:#eff6ff;padding:10px;border-radius:8px;text-align:center;border:1px solid #bfdbfe"><div style="font-size:10px;color:#64748b;font-weight:600">YATIRIM GETİRİSİ (ROI)</div><div style="font-size:18px;font-weight:800;color:${plColor(roi)}">${plSign(roi)}${fmtPct(Math.abs(roi))}</div><div style="font-size:10px;color:#64748b">${roi>=0?'Kârlı yatırım':'Zararda'}</div></div>`;
    const firstEntry=entries.filter(e=>e.type==='deposit').sort((a,b)=>new Date(a.date)-new Date(b.date))[0];
    if(firstEntry){
      const daysSince=Math.floor((new Date()-new Date(firstEntry.date))/(1000*60*60*24));
      const dailyReturn=daysSince>0?roiAmount/daysSince:0;
      h+=`<div style="background:#faf5ff;padding:10px;border-radius:8px;text-align:center;border:1px solid #e9d5ff"><div style="font-size:10px;color:#64748b;font-weight:600">GÜNLÜK ORT. GETİRİ</div><div style="font-size:18px;font-weight:800;color:${plColor(dailyReturn)}">${plSign(dailyReturn)}${fmt(Math.abs(dailyReturn))}</div><div style="font-size:10px;color:#64748b">${daysSince} gündür aktif</div></div>`;
    }else{
      h+=`<div style="background:#faf5ff;padding:10px;border-radius:8px;text-align:center;border:1px solid #e9d5ff"><div style="font-size:10px;color:#64748b;font-weight:600">GÜNLÜK ORT. GETİRİ</div><div style="font-size:18px;font-weight:800;color:#94a3b8">-</div></div>`;
    }
    h+=`</div>`;
  }

  // === KAYIT FORMU ===
  h+=section('Yeni Sermaye Kaydı','');
  h+=`<div style="display:grid;grid-template-columns:1fr 1fr 1fr 1fr;gap:8px;margin-bottom:8px">
    <div><label style="font-size:11px;color:#64748b;font-weight:600;display:block;margin-bottom:2px">Yatırımcı</label><input type="text" id="akm-cap-investor" class="akm-input" style="width:100%;box-sizing:border-box" placeholder="Ad Soyad"></div>
    <div><label style="font-size:11px;color:#64748b;font-weight:600;display:block;margin-bottom:2px">Tutar (TRY)</label><input type="number" id="akm-cap-amount" class="akm-input" style="width:100%;box-sizing:border-box" placeholder="0.00" step="0.01" min="0"></div>
    <div><label style="font-size:11px;color:#64748b;font-weight:600;display:block;margin-bottom:2px">Tür</label><select id="akm-cap-type" class="akm-select" style="width:100%;box-sizing:border-box"><option value="deposit">Sermaye Girişi</option><option value="withdrawal">Sermaye Çıkışı</option></select></div>
    <div><label style="font-size:11px;color:#64748b;font-weight:600;display:block;margin-bottom:2px">Tarih</label><input type="date" id="akm-cap-date" class="akm-input" style="width:100%;box-sizing:border-box" value="${new Date().toISOString().split('T')[0]}"></div>
  </div>
  <div style="display:flex;gap:8px;margin-bottom:16px">
    <input type="text" id="akm-cap-note" class="akm-input" style="flex:1" placeholder="Not (opsiyonel)">
    <button onclick="window.__akmCapitalAdd()" class="akm-filter-apply" style="padding:8px 20px">+ Ekle</button>
    <button onclick="window.__akmCapitalExport()" style="padding:8px 12px;background:#475569;color:#fff;border:none;border-radius:6px;font-size:11px;cursor:pointer" title="CSV Indir">Dışa Aktar</button>
  </div>`;

  // === YATIRIMCI BAZLI OZET ===
  const byInvestor={};
  entries.forEach(e=>{
    if(!byInvestor[e.investor])byInvestor[e.investor]={deposits:0,withdrawals:0};
    if(e.type==='deposit')byInvestor[e.investor].deposits+=Number(e.amount);
    else byInvestor[e.investor].withdrawals+=Number(e.amount);
  });

  if(Object.keys(byInvestor).length>0){
    h+=section('Yatırımcı Bazlı Özet','');
    h+='<table class="akm-table"><thead><tr><th>Yatırımcı</th><th>Yatırım</th><th>Çekim</th><th>Net Sermaye</th><th>Pay</th><th>Tahmini Değer</th><th>Getiri</th></tr></thead><tbody>';
    Object.entries(byInvestor).sort((a,b)=>(b[1].deposits-b[1].withdrawals)-(a[1].deposits-a[1].withdrawals)).forEach(([name,d])=>{
      const net=d.deposits-d.withdrawals;
      const share=netCapital>0?(net/netCapital*100):0;
      const estimatedValue=netCapital>0?(net/netCapital*totalAssets):0;
      const investorReturn=estimatedValue-net;
      h+=`<tr><td><strong>${name}</strong></td><td style="color:#22c55e">${fmt(d.deposits)}</td><td style="color:#ef4444">${fmt(d.withdrawals)}</td><td style="font-weight:700">${fmt(net)}</td><td style="font-weight:600">${fmtPct(share)}</td><td style="font-weight:700;color:#6366f1">${fmt(estimatedValue)}</td><td style="color:${plColor(investorReturn)};font-weight:600">${plSign(investorReturn)}${fmt(investorReturn)}</td></tr>`;
    });
    h+='</tbody></table>';
    h+=`<div style="font-size:10px;color:#94a3b8;margin-top:4px;font-style:italic">* Tahmini değer, net sermaye payına göre mevcut varlıklar üzerinden hesaplanır.</div>`;
  }

  // === SERMAYE HAREKET GECMISI ===
  if(entries.length>0){
    h+=section('Sermaye Hareket Geçmişi','');
    h+='<table class="akm-table"><thead><tr><th>Tarih</th><th>Yatırımcı</th><th>Tür</th><th>Tutar</th><th>Not</th><th>Kümülatif</th><th></th></tr></thead><tbody>';
    let cumulative=0;
    [...entries].sort((a,b)=>new Date(a.date)-new Date(b.date)).forEach(e=>{
      if(e.type==='deposit')cumulative+=Number(e.amount);else cumulative-=Number(e.amount);
      const typeLabel=e.type==='deposit'?'<span style="color:#22c55e;font-weight:700">GİRİŞ</span>':'<span style="color:#ef4444;font-weight:700">ÇIKIŞ</span>';
      h+=`<tr><td style="white-space:nowrap">${new Date(e.date).toLocaleDateString('tr-TR')}</td><td><strong>${e.investor}</strong></td><td>${typeLabel}</td><td style="font-weight:700">${fmt(e.amount)}</td><td style="color:#64748b;font-size:11px">${e.note||'-'}</td><td style="font-weight:600;color:#6366f1">${fmt(cumulative)}</td><td><button onclick="window.__akmCapitalDel(${e.id})" style="background:none;border:none;color:#ef4444;cursor:pointer;font-size:18px;padding:0 4px" title="Sil">x</button></td></tr>`;
    });
    h+='</tbody></table>';
  }else{
    h+=`<div class="akm-empty" style="margin-top:16px">Henüz sermaye kaydı yok. Yukarıdaki formdan yeni kayıt ekleyebilirsiniz.</div>`;
  }

  h+=`<div class="akm-footer" style="display:flex;justify-content:space-between;align-items:center"><span style="color:#f97316;font-weight:600">(!) Sermaye verileri bu tarayıcıda saklanır. Farklı cihazda görünmez.</span><span>${fmtDate(new Date())}</span></div>`;
  return h;
}

// Global fonksiyonlar (modal icinden erisilebilir)
window.__akmCapitalAdd=async function(){
  const investor=document.getElementById('akm-cap-investor')?.value?.trim();
  const amount=parseFloat(document.getElementById('akm-cap-amount')?.value);
  const type=document.getElementById('akm-cap-type')?.value;
  const date=document.getElementById('akm-cap-date')?.value;
  const note=document.getElementById('akm-cap-note')?.value?.trim()||'';
  if(!investor||!amount||amount<=0||!date){alert('Yatırımcı adı, tutar ve tarih zorunludur.');return}
  addCapitalEntry({investor,amount,type,date,note});
  const body=document.getElementById('akm-modal-body');
  if(body){body.innerHTML='<div class="akm-loading"><div class="akm-spinner"></div>Güncelleniyor...</div>';body.innerHTML=await fetchCapital()}
};

window.__akmCapitalDel=async function(id){
  if(!confirm('Bu sermaye kaydını silmek istediğinize emin misiniz?'))return;
  deleteCapitalEntry(id);
  const body=document.getElementById('akm-modal-body');
  if(body){body.innerHTML='<div class="akm-loading"><div class="akm-spinner"></div>Güncelleniyor...</div>';body.innerHTML=await fetchCapital()}
};

window.__akmCapitalExport=function(){
  const entries=getCapitalEntries();
  if(entries.length===0){alert('Dışa aktarılacak veri yok.');return}
  let csv='Tarih,Yatırımcı,Tür,Tutar,Not\n';
  entries.forEach(e=>{
    csv+=`${e.date},"${e.investor}",${e.type==='deposit'?'GİRİŞ':'ÇIKIŞ'},${e.amount},"${e.note||''}"\n`;
  });
  const blob=new Blob(['\ufeff'+csv],{type:'text/csv;charset=utf-8'});
  const url=URL.createObjectURL(blob);
  const a=document.createElement('a');a.href=url;a.download='sermaye-kayitlari.csv';document.body.appendChild(a);a.click();document.body.removeChild(a);URL.revokeObjectURL(url);
};

// ═══════════════════════════════════════════════════════
// MODAL (filtre barı destekli)
// ═══════════════════════════════════════════════════════
async function showModal(title,icon,fetchFn,color,hasFilter){
  let overlay=document.getElementById('akm-overlay');
  if(overlay)overlay.remove();
  overlay=document.createElement('div');
  overlay.id='akm-overlay';

  const filterHtml=hasFilter?buildFilterBar():'';

  overlay.innerHTML=`
    <div class="akm-modal">
      <div class="akm-modal-header" style="background:${color}">
        <div class="akm-modal-title"><span class="material-symbols-outlined">${icon}</span>${title}</div>
        <button class="akm-close" onclick="document.getElementById('akm-overlay').remove()">&times;</button>
      </div>
      ${filterHtml}
      <div class="akm-modal-body" id="akm-modal-body">
        <div class="akm-loading"><div class="akm-spinner"></div>Yükleniyor...</div>
      </div>
    </div>`;
  document.body.appendChild(overlay);
  overlay.addEventListener('click',e=>{if(e.target===overlay)overlay.remove()});

  // Initialize filter bar if present
  if(hasFilter){
    await initFilterBar(async(filters)=>{
      const body=document.getElementById('akm-modal-body');
      if(body)body.innerHTML='<div class="akm-loading"><div class="akm-spinner"></div>Yükleniyor...</div>';
      try{
        const html=await fetchFn(filters);
        if(body)body.innerHTML=html;
      }catch(e){
        if(body)body.innerHTML=`<div class="akm-empty">Hata: ${e.message}</div>`;
      }
    });
  }

  // Initial fetch with default filters
  try{
    const defaultFilters=hasFilter?getFilterValues():null;
    const html=await fetchFn(defaultFilters);
    const body=document.getElementById('akm-modal-body');
    if(body)body.innerHTML=html;
  }catch(e){
    const body=document.getElementById('akm-modal-body');
    if(body)body.innerHTML=`<div class="akm-empty">Veri yüklenemedi: ${e.message}</div>`;
  }
}

// ═══════════════════════════════════════════════════════
// HIDE COMPILED DASHBOARD "SON İŞLEMLER" SECTION
// ═══════════════════════════════════════════════════════
function hideDashboardSonIslemler(){
  // Find all h3/h4 headings with "Son İşlemler" text (from compiled Vue dashboard)
  document.querySelectorAll('h3,h4,.ak-panel-header h3').forEach(el=>{
    if(el.textContent.trim()==='Son İşlemler'||el.textContent.trim().includes('Son İşlemler')){
      // Don't hide our modal's content
      if(el.closest('.akm-modal')||el.closest('#akm-overlay'))return;
      // Don't hide our own "Hızlı İşlemler" header (already renamed)
      if(el.textContent.trim()==='Hızlı İşlemler')return;
      // Hide the parent panel/section
      const panel=el.closest('.ak-panel')||el.closest('[class*="panel"]')||el.closest('[class*="section"]')||el.parentElement?.parentElement;
      if(panel&&!panel.querySelector('.akm-btn-grid')){
        panel.style.display='none';
      }
    }
  });
}

// ═══════════════════════════════════════════════════════
// INJECT BUTTONS
// ═══════════════════════════════════════════════════════
function injectButtons(){
  const scope='data-v-2d041a49';
  const emptyEl=document.querySelector('.ak-empty');
  const panel=emptyEl?.closest('.ak-panel');
  if(!panel)return false;

  const header=panel.querySelector('.ak-panel-header');
  if(header){
    header.querySelector('h3').textContent='Hızlı İşlemler';
    const ico=header.querySelector('.material-symbols-outlined');
    if(ico)ico.textContent='dashboard';
  }
  if(emptyEl)emptyEl.remove();

  const grid=document.createElement('div');
  grid.className='akm-btn-grid';
  grid.setAttribute(scope,'');

  BTNS.forEach(b=>{
    const btn=document.createElement('button');
    btn.className='akm-btn';
    btn.style.background=b.bg;
    btn.setAttribute(scope,'');
    btn.innerHTML=`<span class="material-symbols-outlined">${b.icon}</span><div class="akm-btn-text"><strong>${b.title}</strong><small>${b.desc}</small></div>`;
    btn.addEventListener('click',async(e)=>{
      e.stopPropagation();e.preventDefault();
      showModal(b.title,b.icon,b.fetch,b.bg,b.hasFilter);
    });
    grid.appendChild(btn);
  });

  panel.appendChild(grid);
  // Hide the compiled dashboard's "Son İşlemler" section
  hideDashboardSonIslemler();
  setTimeout(hideDashboardSonIslemler,2000);
  return true;
}

// ═══════════════════════════════════════════════════════
// STYLES
// ═══════════════════════════════════════════════════════
function injectStyles(){
  if(document.getElementById('akm-styles'))return;
  const style=document.createElement('style');
  style.id='akm-styles';
  style.textContent=`
.akm-btn-grid{display:grid;grid-template-columns:repeat(4,1fr);gap:8px;padding:4px 0}
.akm-btn{display:flex;align-items:center;gap:10px;padding:10px 14px;border:none;border-radius:10px;color:#fff;cursor:pointer;transition:all .2s;text-align:left;box-shadow:0 2px 8px rgba(0,0,0,.2)}
.akm-btn:hover{transform:translateY(-2px);box-shadow:0 6px 20px rgba(0,0,0,.3);filter:brightness(1.1)}
.akm-btn:active{transform:translateY(0)}
.akm-btn .material-symbols-outlined{font-size:26px;opacity:.9}
.akm-btn-text{display:flex;flex-direction:column;gap:1px}
.akm-btn-text strong{font-size:12px;font-weight:700;letter-spacing:.3px}
.akm-btn-text small{font-size:10px;opacity:.75}
#akm-overlay{position:fixed;top:0;left:0;right:0;bottom:0;background:rgba(0,0,0,.6);backdrop-filter:blur(4px);z-index:9999;display:flex;align-items:center;justify-content:center;animation:akmFadeIn .2s}
@keyframes akmFadeIn{from{opacity:0}to{opacity:1}}
.akm-modal{background:#fff;border-radius:16px;width:92%;max-width:900px;max-height:85vh;overflow:hidden;box-shadow:0 20px 60px rgba(0,0,0,.3);animation:akmSlideUp .3s;display:flex;flex-direction:column}
@keyframes akmSlideUp{from{transform:translateY(30px);opacity:0}to{transform:translateY(0);opacity:1}}
.akm-modal-header{display:flex;justify-content:space-between;align-items:center;padding:16px 20px;color:#fff;flex-shrink:0}
.akm-modal-title{display:flex;align-items:center;gap:10px;font-size:18px;font-weight:700}
.akm-close{background:rgba(255,255,255,.2);border:none;color:#fff;font-size:24px;width:36px;height:36px;border-radius:50%;cursor:pointer;display:flex;align-items:center;justify-content:center;transition:background .2s}
.akm-close:hover{background:rgba(255,255,255,.3)}
.akm-filter-bar{display:flex;align-items:center;gap:10px;padding:10px 20px;background:#f8fafc;border-bottom:1px solid #e2e8f0;flex-wrap:wrap;flex-shrink:0}
.akm-filter-group{display:flex;align-items:center;gap:6px}
.akm-filter-group label{font-size:12px;font-weight:600;color:#475569;white-space:nowrap}
.akm-select{padding:6px 10px;border:1px solid #cbd5e1;border-radius:6px;font-size:12px;background:#fff;color:#1e293b;cursor:pointer;min-width:100px}
.akm-select:focus{outline:none;border-color:#6366f1;box-shadow:0 0 0 2px rgba(99,102,241,.2)}
.akm-input{padding:6px 8px;border:1px solid #cbd5e1;border-radius:6px;font-size:12px;background:#fff;color:#1e293b}
.akm-input:focus{outline:none;border-color:#6366f1;box-shadow:0 0 0 2px rgba(99,102,241,.2)}
.akm-custom-dates{display:flex;align-items:center;gap:6px}
.akm-filter-apply{padding:6px 16px;background:#6366f1;color:#fff;border:none;border-radius:6px;font-size:12px;font-weight:600;cursor:pointer;transition:all .2s}
.akm-filter-apply:hover{background:#4f46e5}
.akm-modal-body{padding:20px;overflow-y:auto;flex:1}
.akm-table{width:100%;border-collapse:collapse;font-size:12px}
.akm-table th{background:#f1f5f9;padding:7px 10px;text-align:left;font-weight:700;color:#475569;font-size:10px;text-transform:uppercase;letter-spacing:.3px;border-bottom:2px solid #e2e8f0;white-space:nowrap}
.akm-table td{padding:7px 10px;border-bottom:1px solid #f1f5f9;color:#1e293b}
.akm-table tbody tr:hover{background:#f8fafc}
.akm-total-row td{background:#f1f5f9!important;font-weight:700;border-top:2px solid #e2e8f0}
.akm-stats{display:grid;grid-template-columns:repeat(auto-fit,minmax(140px,1fr));gap:10px}
.akm-stat{background:#f8fafc;border-radius:10px;padding:12px;text-align:center;border:1px solid #e2e8f0}
.akm-stat.blue{border-left:3px solid #3b82f6}.akm-stat.green{border-left:3px solid #22c55e}.akm-stat.red{border-left:3px solid #ef4444}.akm-stat.orange{border-left:3px solid #f97316}.akm-stat.purple{border-left:3px solid #8b5cf6}
.akm-stat-label{font-size:10px;color:#64748b;font-weight:600;text-transform:uppercase;letter-spacing:.3px;margin-bottom:3px}
.akm-stat-value{font-size:17px;font-weight:800;color:#1e293b}
.akm-empty{text-align:center;padding:30px;color:#94a3b8;font-size:14px}
.akm-info{padding:8px 12px;background:#f0f9ff;border-radius:8px;color:#0369a1;font-size:12px}
.akm-footer{margin-top:12px;padding-top:10px;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;text-align:right}
.akm-section{margin-top:16px}
.akm-section-title{font-size:13px;font-weight:700;color:#334155;margin-bottom:8px;padding-bottom:4px;border-bottom:2px solid #e2e8f0}
.akm-loading{display:flex;flex-direction:column;align-items:center;gap:12px;padding:40px;color:#64748b}
.akm-spinner{width:36px;height:36px;border:3px solid #e2e8f0;border-top-color:#6366f1;border-radius:50%;animation:akmSpin .8s linear infinite}
@keyframes akmSpin{to{transform:rotate(360deg)}}
/* Kasa listesi: Toplam Bakiyeler grid - 8 kart tek satır */
.balances-card-content>.balances-grid{grid-template-columns:repeat(8,1fr)!important;gap:10px!important}
.balances-card-content>.balances-grid .balance-item{min-width:0!important;padding:10px 8px!important}
.balances-card-content>.balances-grid .balance-value{font-size:13px}
@media(max-width:768px){.akm-btn-grid{grid-template-columns:repeat(2,1fr)}.akm-stats{grid-template-columns:1fr 1fr}.akm-modal{width:96%;max-height:92vh}.akm-filter-bar{flex-direction:column;align-items:stretch}.balances-card-content>.balances-grid{grid-template-columns:repeat(4,1fr)!important}}
`;
  document.head.appendChild(style);
}

// ═══════════════════════════════════════════════════════
// ŞUBE DETAY SAYFASI GÜÇLENDİRME
// ═══════════════════════════════════════════════════════
function isVaultDetailPage(){
  const p=window.location.pathname;
  return/^\/ihtiyar\/vaults\/[0-9a-f-]{30,}$/i.test(p);
}
function isVaultsListPage(){
  return window.location.pathname==='/ihtiyar/vaults'||window.location.pathname==='/ihtiyar/vaults/';
}
function isExchangePage(){
  const p=window.location.pathname;
  return p==='/ihtiyar/exchange-v2'||p==='/ihtiyar/exchange';
}

// ═══════════════════════════════════════════════════════
// DÖVİZ SAYFASI SIDEBAR GÜÇLENDİRME
// ═══════════════════════════════════════════════════════
function enhanceExchangeSidebar(){
  if(!isExchangePage())return;
  // Zaten eklenmişse tekrar ekleme
  if(document.getElementById('akm-ruble-btn'))return;
  const actionGroup=document.querySelector('.action-group');
  if(!actionGroup)return;

  // RUBLE İŞLEMLERİ butonu
  const rubleBtn=document.createElement('button');
  rubleBtn.id='akm-ruble-btn';
  rubleBtn.className='sidebar-btn';
  rubleBtn.style.cssText='background:linear-gradient(135deg,#1e3a5f,#2563eb);color:#fff;border:none;border-radius:10px;padding:10px 16px;width:100%;cursor:pointer;font-size:13px;font-weight:600;display:flex;align-items:center;gap:8px;margin-top:8px;transition:all .2s ease;box-shadow:0 2px 8px rgba(37,99,235,.3)';
  rubleBtn.innerHTML='<span style="font-size:18px">₽</span><span>Ruble İşlemleri</span>';
  rubleBtn.onmouseenter=()=>{rubleBtn.style.transform='translateY(-1px)';rubleBtn.style.boxShadow='0 4px 12px rgba(37,99,235,.4)'};
  rubleBtn.onmouseleave=()=>{rubleBtn.style.transform='';rubleBtn.style.boxShadow='0 2px 8px rgba(37,99,235,.3)'};
  rubleBtn.onclick=()=>{
    // Ruble kanal botu paneline yönlendir
    window.open('https://tg.moneytransferturkey.com/admin','_blank');
  };
  actionGroup.appendChild(rubleBtn);

  // OPERATOR PANEL kısayolu
  const opBtn=document.createElement('button');
  opBtn.id='akm-operator-btn';
  opBtn.className='sidebar-btn';
  opBtn.style.cssText='background:linear-gradient(135deg,#7c3aed,#a855f7);color:#fff;border:none;border-radius:10px;padding:10px 16px;width:100%;cursor:pointer;font-size:13px;font-weight:600;display:flex;align-items:center;gap:8px;margin-top:8px;transition:all .2s ease;box-shadow:0 2px 8px rgba(124,58,237,.3)';
  opBtn.innerHTML='<span style="font-size:16px">🤖</span><span>Operatör Paneli</span>';
  opBtn.onmouseenter=()=>{opBtn.style.transform='translateY(-1px)';opBtn.style.boxShadow='0 4px 12px rgba(124,58,237,.4)'};
  opBtn.onmouseleave=()=>{opBtn.style.transform='';opBtn.style.boxShadow='0 2px 8px rgba(124,58,237,.3)'};
  opBtn.onclick=()=>{
    window.open('https://tg.moneytransferturkey.com','_blank');
  };
  actionGroup.appendChild(opBtn);
}
function getVaultIdFromUrl(){
  const m=window.location.pathname.match(/\/vaults\/([0-9a-f-]+)$/i);
  return m?m[1]:null;
}

// Şube detay sayfası: Bilgi paneli enjeksiyonu
async function enhanceVaultDetail(){
  if(!isVaultDetailPage())return;
  // Zaten enjekte edilmişse atla
  if(document.getElementById('akm-vault-info-panel'))return;

  const vaultId=getVaultIdFromUrl();
  if(!vaultId)return;

  try{
    // Paralel API çağrıları
    const[vaultData,dashData]=await Promise.all([
      apiFetch('/exchange/vaults/'+vaultId),
      apiFetch('/exchange/dashboard')
    ]);
    if(!vaultData||!dashData)return;

    // Dashboard'dan ofis bilgisini bul
    const officeId=vaultData.officeId;
    const office=(dashData.offices||[]).find(o=>o.officeId===officeId);

    const vaultName=vaultData.vaultName||vaultData.name||'Kasa';
    const officeName=vaultData.officeName||office?.officeName||'';
    const isActive=vaultData.isActive!==false;
    const createdDate=vaultData.createdDate?fmtDate(vaultData.createdDate):'-';
    const totalValue=vaultData.totalValueInBaseCurrency||office?.totalValueInBaseCurrency||0;
    const dailyPL=office?.dailyProfitLoss||0;
    const monthlyPL=office?.monthlyProfitLoss||0;
    const vaultCount=office?.vaultCount||1;
    const currencyCount=vaultData.balances?.length||0;
    const activeCurrencies=(vaultData.balances||[]).filter(b=>(b.balance||b.amount||0)>0).length;

    // Mevcut header'ı bul ve bilgi paneli ekle
    const header=document.querySelector('.vault-card-header')||document.querySelector('h1.page-title')?.parentElement;
    const mainEl=document.querySelector('main.flex-1');
    if(!mainEl)return;

    // Bilgi paneli HTML
    const panel=document.createElement('div');
    panel.id='akm-vault-info-panel';
    panel.innerHTML=`
    <style>
      #akm-vault-info-panel{margin:0 16px 20px;animation:akmFadeIn .4s ease}
      @keyframes akmFadeIn{from{opacity:0;transform:translateY(-8px)}to{opacity:1;transform:translateY(0)}}
      .akm-vip{background:linear-gradient(135deg,#1e293b 0%,#334155 100%);border-radius:16px;padding:24px;color:#fff;position:relative;overflow:hidden}
      .akm-vip::before{content:'';position:absolute;top:-50%;right:-20%;width:300px;height:300px;background:radial-gradient(circle,rgba(99,102,241,.15) 0%,transparent 70%);pointer-events:none}
      .akm-vip-top{display:flex;align-items:center;justify-content:space-between;margin-bottom:20px}
      .akm-vip-title{display:flex;align-items:center;gap:12px}
      .akm-vip-icon{width:48px;height:48px;border-radius:12px;background:linear-gradient(135deg,#6366f1,#8b5cf6);display:flex;align-items:center;justify-content:center;font-size:24px}
      .akm-vip h2{font-size:22px;font-weight:800;margin:0;color:#fff}
      .akm-vip-sub{font-size:13px;color:#94a3b8;margin-top:2px}
      .akm-vip-badge{padding:6px 14px;border-radius:20px;font-size:12px;font-weight:700;letter-spacing:.5px}
      .akm-vip-badge.active{background:rgba(34,197,94,.2);color:#4ade80;border:1px solid rgba(34,197,94,.3)}
      .akm-vip-badge.inactive{background:rgba(239,68,68,.2);color:#f87171;border:1px solid rgba(239,68,68,.3)}
      .akm-vip-grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:12px}
      .akm-vip-card{background:rgba(255,255,255,.07);backdrop-filter:blur(4px);border-radius:12px;padding:14px;border:1px solid rgba(255,255,255,.08)}
      .akm-vip-card-label{font-size:10px;color:#94a3b8;text-transform:uppercase;letter-spacing:.5px;font-weight:600;margin-bottom:4px}
      .akm-vip-card-value{font-size:18px;font-weight:800;color:#fff}
      .akm-vip-card-value.green{color:#4ade80}
      .akm-vip-card-value.red{color:#f87171}
      .akm-vip-card-value.blue{color:#60a5fa}
      .akm-vip-card-value.orange{color:#fb923c}
      .akm-vip-meta{display:flex;gap:16px;margin-top:16px;padding-top:12px;border-top:1px solid rgba(255,255,255,.08)}
      .akm-vip-meta-item{font-size:11px;color:#94a3b8;display:flex;align-items:center;gap:4px}
      .akm-vip-meta-item span.material-symbols-outlined{font-size:14px}
      @media(max-width:768px){.akm-vip-grid{grid-template-columns:1fr 1fr}.akm-vip-top{flex-direction:column;align-items:flex-start;gap:8px}}
    </style>
    <div class="akm-vip">
      <div class="akm-vip-top">
        <div class="akm-vip-title">
          <div class="akm-vip-icon"><span class="material-symbols-outlined">account_balance</span></div>
          <div>
            <h2>${vaultName}</h2>
            <div class="akm-vip-sub">📍 ${officeName} Şubesi</div>
          </div>
        </div>
        <div class="akm-vip-badge ${isActive?'active':'inactive'}">${isActive?'● Aktif':'● Pasif'}</div>
      </div>
      <div class="akm-vip-grid">
        <div class="akm-vip-card">
          <div class="akm-vip-card-label">Toplam Değer</div>
          <div class="akm-vip-card-value blue">${fmt(totalValue)}</div>
        </div>
        <div class="akm-vip-card">
          <div class="akm-vip-card-label">Günlük K/Z</div>
          <div class="akm-vip-card-value ${plClass(dailyPL)}">${plSign(dailyPL)}${fmt(dailyPL)}</div>
        </div>
        <div class="akm-vip-card">
          <div class="akm-vip-card-label">Aylık K/Z</div>
          <div class="akm-vip-card-value ${plClass(monthlyPL)}">${plSign(monthlyPL)}${fmt(monthlyPL)}</div>
        </div>
        <div class="akm-vip-card">
          <div class="akm-vip-card-label">Aktif / Toplam Döviz</div>
          <div class="akm-vip-card-value orange">${activeCurrencies} / ${currencyCount}</div>
        </div>
      </div>
      <div class="akm-vip-meta">
        <div class="akm-vip-meta-item"><span class="material-symbols-outlined">calendar_month</span> Oluşturulma: ${createdDate}</div>
        <div class="akm-vip-meta-item"><span class="material-symbols-outlined">store</span> Kasa Sayısı: ${vaultCount}</div>
        <div class="akm-vip-meta-item"><span class="material-symbols-outlined">shield</span> Son Sayım: ${vaultData.lastCountDate?fmtDate(vaultData.lastCountDate):'Yapılmadı'}</div>
      </div>
    </div>`;

    // Panel enjeksiyon hedefleri (öncelik sırasıyla)
    const hizliIslemler=mainEl.querySelector('h2');
    if(hizliIslemler&&hizliIslemler.textContent.includes('Hızlı İşlemler')){
      hizliIslemler.parentElement.parentElement.insertBefore(panel,hizliIslemler.parentElement);
    }else{
      // vault-header'dan sonra (yeni layout)
      const vaultHeader=mainEl.querySelector('.vault-header');
      const vaultCardHeader=mainEl.querySelector('.vault-card-header');
      const target=vaultHeader||vaultCardHeader;
      if(target){
        target.parentElement.insertBefore(panel,target.nextSibling);
      }else{
        // Son fallback: main'in ilk child'ının başına
        mainEl.firstElementChild?mainEl.firstElementChild.prepend(panel):mainEl.prepend(panel);
      }
    }

    // Mevcut başlık alanını gizle (bilgi paneli zaten gösteriyor)
    hideDuplicateHeader();

    // 0 bakiyeli para birimlerini gizle
    hideZeroBalanceCurrencies();

  }catch(e){console.warn('[AKM] Vault detail enhance error:',e)}
}

// Mevcut header'daki "Created Invalid Date" ve tekrarlayan bilgileri gizle
function hideDuplicateHeader(){
  // "Created Invalid Date" metnini düzelt
  document.querySelectorAll('p,span,div').forEach(el=>{
    if(el.textContent.trim().includes('Created Invalid Date')){
      el.style.display='none';
    }
  });
}

// 0 bakiyeli para birimlerini gizle + toggle buton
function hideZeroBalanceCurrencies(){
  if(document.getElementById('akm-zero-toggle'))return; // Zaten eklendi
  // Kasa Bakiyeleri bölümünü bul
  const h2s=document.querySelectorAll('h2');
  let bakiyeleriH2=null;
  h2s.forEach(h=>{if(h.textContent.trim()==='Kasa Bakiyeleri')bakiyeleriH2=h;});
  if(!bakiyeleriH2)return;

  // .balances-card veya en yakın parent container'ı bul
  const container=bakiyeleriH2.closest('.balances-card')||bakiyeleriH2.closest('div[class]')?.parentElement;
  if(!container)return;

  // .currency-card class'ına sahip kartları bul (currencies-grid içinde)
  const grid=container.querySelector('.currencies-grid');
  let cards=grid?grid.querySelectorAll('.currency-card'):[];
  // Fallback: class bazlı bulamazsak genel arama
  if(cards.length===0){
    cards=container.querySelectorAll('[class*="currency-card"],[class*="balance-card"]');
  }
  if(cards.length===0)return;

  let hidden=0;
  const hiddenCards=[];
  cards.forEach(card=>{
    const texts=card.textContent;
    // "Bakiye" ve rakam arasında boşluk olmayabilir (Bakiye0,00 veya Bakiye 500.000,00)
    const bakiyeMatch=texts.match(/Bakiye\s*([\d.,]+)/);
    if(bakiyeMatch){
      const val=parseFloat(bakiyeMatch[1].replace(/\./g,'').replace(',','.'));
      if(val===0){
        card.style.display='none';
        hiddenCards.push(card);
        hidden++;
      }
    }
  });

  if(hidden>0){
    // Toggle buton ekle
    const toggleBtn=document.createElement('button');
    toggleBtn.id='akm-zero-toggle';
    toggleBtn.className='akm-toggle';
    toggleBtn.style.cssText='margin:8px 0;padding:8px 16px;background:#f1f5f9;border:1px solid #e2e8f0;border-radius:8px;font-size:12px;color:#64748b;cursor:pointer;transition:all .2s';
    toggleBtn.innerHTML=`<span class="material-symbols-outlined" style="font-size:14px;vertical-align:middle">visibility_off</span> ${hidden} boş para birimi gizlendi — Göster`;
    let showing=false;
    toggleBtn.addEventListener('click',()=>{
      showing=!showing;
      hiddenCards.forEach(c=>{c.style.display=showing?'':'none';});
      toggleBtn.innerHTML=showing
        ?`<span class="material-symbols-outlined" style="font-size:14px;vertical-align:middle">visibility</span> ${hidden} boş para birimi gösteriliyor — Gizle`
        :`<span class="material-symbols-outlined" style="font-size:14px;vertical-align:middle">visibility_off</span> ${hidden} boş para birimi gizlendi — Göster`;
      toggleBtn.style.background=showing?'#eff6ff':'#f1f5f9';
      toggleBtn.style.borderColor=showing?'#93c5fd':'#e2e8f0';
      toggleBtn.style.color=showing?'#2563eb':'#64748b';
    });
    toggleBtn.addEventListener('mouseenter',()=>{toggleBtn.style.background=showing?'#dbeafe':'#e2e8f0';});
    toggleBtn.addEventListener('mouseleave',()=>{toggleBtn.style.background=showing?'#eff6ff':'#f1f5f9';});
    // H2'nin parent'ı (balances-header) sonrasına veya grid'den önce ekle
    if(grid){
      grid.parentElement.insertBefore(toggleBtn,grid);
    }else{
      bakiyeleriH2.parentElement.insertBefore(toggleBtn,bakiyeleriH2.nextSibling);
    }
  }
}

// Vaults listesinde Active badge'leri Türkçeye çevir
function fixVaultStatusBadges(){
  // CSS fix'i kaybolmuşsa tekrar enjekte et (SPA navigasyonda gerekli)
  if(!document.getElementById('akm-vault-css')){
    const s=document.createElement('style');
    s.id='akm-vault-css';
    s.textContent=`
    .vault-status{font-size:0!important;display:inline-flex!important;align-items:center!important;gap:4px!important}
    .vault-status .status-dot{font-size:0!important;width:8px!important;height:8px!important;display:inline-block!important}
    .vault-status.active::after{content:'Aktif';font-size:13px!important;color:#16a34a;font-weight:500}
    .vault-status:not(.active)::after{content:'Pasif';font-size:13px!important;color:#dc2626;font-weight:500}
    .card-status.st-active{font-size:0!important}
    .card-status.st-active::after{content:'AKTİF';font-size:11px!important;font-weight:600;color:#16a34a}
    .card-status:not(.st-active){font-size:0!important}
    .card-status:not(.st-active)::after{content:'PASİF';font-size:11px!important;font-weight:600;color:#dc2626}
    `;
    (document.head||document.documentElement).appendChild(s);
  }
  // Ayrıca text node bazlı düzeltme (fallback)
  document.querySelectorAll('.vault-status').forEach(el=>{
    el.childNodes.forEach(node=>{
      if(node.nodeType===3){
        const t=node.textContent.trim();
        if(t==='Active')node.textContent=' Aktif';
        else if(t==='Inactive')node.textContent=' Pasif';
      }
    });
  });
}

// Vaults listesinde kartlara K/Z bilgisi ekle
async function enhanceVaultsList(){
  if(!isVaultsListPage())return;
  fixVaultStatusBadges();
  if(document.querySelector('.akm-vl-kz'))return;

  try{
    const dashData=await apiFetch('/exchange/dashboard');
    if(!dashData?.offices)return;

    const officeMap={};
    dashData.offices.forEach(o=>{officeMap[o.officeName]=o;});

    // Kart başlıklarını bul
    document.querySelectorAll('.vault-card-header').forEach(header=>{
      const nameEl=header.querySelector('.vault-name');
      if(!nameEl)return;

      // Ofis adını bul (kart altındaki p elemanı)
      const officeEl=header.querySelector('.vault-office');
      const officeName=officeEl?.textContent?.trim();
      const office=officeMap[officeName];
      if(!office)return;

      const card=header.closest('.vault-card');
      if(!card||card.querySelector('.akm-vl-kz'))return;

      // K/Z bilgi bandı ekle
      const kzBand=document.createElement('div');
      kzBand.className='akm-vl-kz';
      kzBand.style.cssText='display:flex;gap:8px;padding:8px 12px;background:#f8fafc;border-top:1px solid #e2e8f0;font-size:11px';
      const dpl=office.dailyProfitLoss||0;
      const mpl=office.monthlyProfitLoss||0;
      kzBand.innerHTML=`
        <span style="color:#64748b">Günlük:</span>
        <span style="color:${plColor(dpl)};font-weight:700">${plSign(dpl)}${fmt(dpl)}</span>
        <span style="color:#cbd5e1;margin:0 2px">│</span>
        <span style="color:#64748b">Aylık:</span>
        <span style="color:${plColor(mpl)};font-weight:700">${plSign(mpl)}${fmt(mpl)}</span>`;

      // vault-actions'dan önce ekle
      const actions=card.querySelector('.vault-actions');
      if(actions){
        card.insertBefore(kzBand,actions);
      }else{
        card.appendChild(kzBand);
      }
    });
  }catch(e){console.warn('[AKM] Vaults list enhance error:',e)}
}

// ═══════════════════════════════════════════════════════
// INIT
// ═══════════════════════════════════════════════════════
function init(){
  injectStyles();

  // Ana Kasa dashboard modu
  if(injectButtons()){
    return;
  }

  // Şube detay sayfası modu
  if(isVaultDetailPage()){
    setTimeout(enhanceVaultDetail,800);
    return;
  }

  // Döviz işlemleri sayfası modu
  if(isExchangePage()){
    setTimeout(enhanceExchangeSidebar,800);
    // Sidebar async yüklenirse yakalamak için observer
    const exObs=new MutationObserver(()=>{enhanceExchangeSidebar();});
    exObs.observe(document.body,{childList:true,subtree:true});
    setTimeout(()=>exObs.disconnect(),15000);
    return;
  }

  // Kasa listesi modu
  if(isVaultsListPage()){
    setTimeout(enhanceVaultsList,800);
    // Kartlar async yüklendiğinde badge'leri düzelt
    const vlObs=new MutationObserver(()=>{fixVaultStatusBadges();});
    vlObs.observe(document.body,{childList:true,subtree:true});
    setTimeout(()=>vlObs.disconnect(),15000);
    return;
  }

  // Fallback: ANA KASA dashboard için MutationObserver
  const obs=new MutationObserver(()=>{
    if(document.querySelector('.ak-empty')){
      if(injectButtons()){obs.disconnect()}
    }
  });
  obs.observe(document.body,{childList:true,subtree:true});
  setTimeout(()=>obs.disconnect(),30000);
}

// SPA navigasyon desteği: URL değişikliklerini izle
let _lastPath=window.location.pathname;
function watchNavigation(){
  setInterval(()=>{
    const curr=window.location.pathname;
    if(curr!==_lastPath){
      _lastPath=curr;
      // SPA navigasyonunda vault ID'leri hemen preload et (XHR interceptor için)
      if(isVaultDetailPage()&&!_vaultIds){
        const _t=getToken()||localStorage.getItem('apiToken');
        if(_t)_getVaultIds('Bearer '+_t);
      }
      setTimeout(()=>{
        if(isVaultDetailPage())enhanceVaultDetail();
        else if(isVaultsListPage()){fixVaultStatusBadges();enhanceVaultsList();}
        else if(isExchangePage()){
          let _exRetry=0;
          const _tryEx=()=>{if(document.getElementById('akm-ruble-btn')||_exRetry>12)return;enhanceExchangeSidebar();_exRetry++;setTimeout(_tryEx,500);};
          _tryEx();
        }
      },800);
    }
  },500);
}

if(document.readyState==='complete'){setTimeout(init,1000);watchNavigation();}
else window.addEventListener('load',()=>{setTimeout(init,1000);watchNavigation();});
})();
