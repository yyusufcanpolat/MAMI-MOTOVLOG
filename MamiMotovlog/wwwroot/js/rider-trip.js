'use strict';
document.addEventListener('DOMContentLoaded', () => {
  const el=document.getElementById('trip-preview'), source=document.getElementById('trip-geo');
  if(!el||!source)return;
  if(typeof L==='undefined'){el.textContent='Harita yüklenemedi.';return;}
  try {
    const geometry=JSON.parse(JSON.parse(source.textContent));
    const map=L.map(el,{scrollWheelZoom:false,maxBounds:[[35.7,25.5],[42.3,45]],maxBoundsViscosity:1,minZoom:5});
    L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png',{attribution:'© OpenStreetMap contributors',maxZoom:19}).addTo(map);
    const route=L.geoJSON(geometry,{style:{color:'#171717',weight:5}}).addTo(map);
    map.fitBounds(route.getBounds(),{padding:[25,25]});
    if ('ResizeObserver' in window) new ResizeObserver(()=>map.invalidateSize()).observe(el);
    setTimeout(()=>map.invalidateSize(),150);
  } catch(e) {el.textContent='Bu rotanın harita verisi görüntülenemedi.';}
});
