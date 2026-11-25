/* MAMI JOURNAL V5 | Real motorcycle route planner: full stop chain, alternatives, duration, live prices, weather. */
'use strict';
(function () {
  const formatDuration = minutes => {
    const total = Math.max(0, Math.round(Number(minutes) || 0));
    const hours = Math.floor(total / 60), rem = total % 60;
    return hours ? `${hours} saat${rem ? ` ${rem} dk` : ''}` : `${rem} dk`;
  };
  const calculateFuelCost = (km, fuel, price) => {
    const d = Number(km), c = Number(fuel), p = Number(price);
    return d > 0 && c > 0 && c <= 50 && p > 0 && p <= 1000 ? d * c / 100 * p : null;
  };
  const formatWeather = code => {
    if (code === 0) return 'Açık';
    if ([1, 2, 3].includes(code)) return 'Parçalı bulutlu';
    if ([45, 48].includes(code)) return 'Sisli';
    if ([51, 53, 55, 56, 57].includes(code)) return 'Çiseleme';
    if ([61, 63, 65, 66, 67, 80, 81, 82].includes(code)) return 'Yağışlı';
    if ([71, 73, 75, 77, 85, 86].includes(code)) return 'Karlı';
    if ([95, 96, 99].includes(code)) return 'Fırtınalı';
    return 'Değişken';
  };
  window.MamiRouteUtils = {formatDuration, calculateFuelCost, formatWeather};

  document.addEventListener('DOMContentLoaded', () => {
    const $ = id => document.getElementById(id);
    const el = $('rider-map');
    if (!el) return;
    const status = $('route-status'), weatherStatus = $('weather-status');
    const addStop = $('add-stop'), build = $('build-route'), weather = $('weather-button');
    const saveForm = $('save-trip'), saveButton = $('save-route');
    const resultPanel = $('planner-results'), altButtons = $('route-choices'), altWrapper = $('route-options-wrap');
    const weatherPanel = $('weather-details'), weatherCurrent = $('weather-current'), weatherDays = $('weather-days');
    const stopSummary = $('stop-summary');
    const priceStatus = $('fuel-price-status'), priceInput = $('fuel-price');
    const priceSide = $('fuel-side'), priceType = $('fuel-type');
    const refreshPrice = $('fuel-refresh');
    const turkeyBounds = [[35.7, 25.5], [42.3, 45.0]];
    const inBounds = ({lat, lng}) => lat >= 35.7 && lat <= 42.3 && lng >= 25.5 && lng <= 45;
    const format = value => Number(value).toLocaleString('tr-TR', {maximumFractionDigits:1});
    const setStatus = (message, error = false) => {
      status.textContent = message;
      status.classList.toggle('rider-status-error', error);
    };

    let positions = [], pins = [], poly = null, route = null, choosingStop = false, routeGeneration = 0, weatherGeneration = 0, calculating = false;
    // Prices are retrieved server-side from an online, attributed price feed. Manual input always works.
    let priceEdited = false, priceRequest = 0;
    priceInput.addEventListener('input', () => { priceEdited = true; updateCost(); });
    async function loadIstanbulFuelPrice(force = false) {
      if (!priceStatus) return;
      const request = ++priceRequest;
      priceStatus.textContent = 'İstanbul akaryakıt fiyatı kontrol ediliyor…';
      if (refreshPrice) refreshPrice.disabled = true;
      try {
        const side = priceSide?.value || 'anadolu', type = priceType?.value || 'benzin';
        const response = await fetch(`/rota-planlayici/istanbul-yakit?${new URLSearchParams({side, type, refresh: force ? 'true' : 'false'})}`);
        const data = await response.json();
        if(request !== priceRequest) return;
        if (!response.ok) throw Error(data.error || 'Fiyat alınamadı.');
        const price = Number(data.price);
        const fetched = data.checkedAt ? new Date(data.checkedAt).toLocaleDateString('tr-TR') : '—';
        const sideText = side === 'anadolu' ? 'Anadolu Yakası' : 'Avrupa Yakası';
        if (data.isRecent && price > 0 && !priceEdited) {
          priceInput.value = price.toFixed(2);
          priceStatus.textContent = `${sideText} · ${format(price)} ₺/L · Kaynak kontrolü: ${fetched}. ${data.attribution || ""} İstasyonda değişebilir.`;
          updateCost();
        } else if (data.isRecent) {
          priceStatus.textContent = `Kaynak: ${sideText} ${format(price)} ₺/L (${fetched}). ${data.attribution || ""} Manuel girdiğin fiyat korundu.`;
        } else {
          if(!priceEdited){priceInput.value="";updateCost();}
          priceStatus.textContent = `Kaynak verisi güncel değil (son kontrol: ${fetched}). Fiyat otomatik uygulanmadı; güncel litre fiyatını elle girebilirsin.`;
        }
      } catch (err) {
        if(request !== priceRequest) return;
        if(!priceEdited){priceInput.value="";updateCost();}
        priceStatus.textContent = 'Canlı fiyat şu an alınamadı. Güncel litre fiyatını elle girerek hesaplamaya devam edebilirsin.';
      } finally { if (refreshPrice && request===priceRequest) refreshPrice.disabled = false; }
    }
    priceSide?.addEventListener('change', () => loadIstanbulFuelPrice());
    priceType?.addEventListener('change', () => loadIstanbulFuelPrice());
    refreshPrice?.addEventListener('click', () => { priceEdited = false; loadIstanbulFuelPrice(true); });
    if (priceStatus) loadIstanbulFuelPrice();

    if (typeof window.L === 'undefined') {
      el.classList.add('rider-map-unavailable');
      setStatus('Harita yüklenemedi. Harita kütüphanesi veya internet bağlantısı kontrol edilmeli.', true);
      return;
    }
    const map = L.map(el, {scrollWheelZoom:false, maxBounds:turkeyBounds,
      maxBoundsViscosity:1, minZoom:5, zoomControl:true});
    map.fitBounds(turkeyBounds, {padding:[6,6]});
    const tiles = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
      minZoom:5,maxZoom:18, attribution:'© <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
    }).addTo(map);
    let tileFailures = 0;
    tiles.on('tileerror', () => {
      tileFailures++;
      if (tileFailures === 5) setStatus('Harita görsellerine erişilemiyor. Ağ bağlantısını kontrol et.', true);
    });
    if ('ResizeObserver' in window) new ResizeObserver(() => map.invalidateSize()).observe(el);
    window.addEventListener('resize', () => map.invalidateSize());
    requestAnimationFrame(() => map.invalidateSize());
    setTimeout(() => map.invalidateSize(), 250);


    const clearMetrics = () => ['route-km','route-time','route-cost'].forEach(id => { if ($(id)) $(id).textContent = '—'; });
    const clearRoute = () => {
      ++routeGeneration; calculating = false;
      route = null;
      if (poly) { map.removeLayer(poly); poly = null; }
      clearMetrics();
      if (resultPanel) resultPanel.hidden = true;
      if (altButtons) altButtons.replaceChildren();
      if (altWrapper) altWrapper.hidden = true;
      if (saveButton) saveButton.disabled = true;
    };
    const renderStops = () => {
      if (!stopSummary) return;
      if (!positions.length) { stopSummary.textContent = 'Henüz konum seçilmedi.'; return; }
      const labels = positions.map((_, i) => i === 0 ? 'Başlangıç' : i === positions.length - 1 ? 'Varış' : `Durak ${i}`);
      stopSummary.textContent = labels.join('  →  ');
    };
    const reset = () => {
      positions = []; choosingStop = false; ++weatherGeneration; clearRoute();
      pins.forEach(pin => map.removeLayer(pin)); pins = [];
      build.disabled = true; addStop.disabled = true; weather.disabled = true;
      weatherStatus.textContent = '';
      if (weatherPanel) weatherPanel.hidden = true;
      renderStops();
      setStatus('Haritada önce başlangıç, sonra varış noktasını seç. İstersen ara durak ekle.');
    };
    $('reset-pins').addEventListener('click', reset);
    const drawPins = () => {
      pins.forEach(pin => map.removeLayer(pin)); pins = [];
      positions.forEach((point, index) => {
        const label = index === 0 ? 'Başlangıç' : index === positions.length - 1 ? 'Varış' : `Ara durak ${index}`;
        pins.push(L.circleMarker(point, {radius:9,weight:3,color:'#ffffff',fillColor:'#246b71',fillOpacity:1})
          .addTo(map).bindTooltip(label,{permanent:true,direction:'top',offset:[0,-10]}));
      });
      renderStops();
    };
    const updateButtons = () => {
      build.disabled = positions.length < 2 || choosingStop || calculating;
      addStop.disabled = positions.length < 2 || positions.length >= 7;
      weather.disabled = positions.length < 2;
    };
    const place = point => {
      if (!inBounds(point)) { setStatus('Yalnızca Türkiye harita alanındaki noktaları seçebilirsin.',true); return; }
      if (positions.length >= 2 && !choosingStop) reset();
      if (choosingStop) { positions.splice(positions.length - 1, 0, point); choosingStop = false; }
      else positions.push(point);
      clearRoute(); drawPins(); updateButtons();
      setStatus(positions.length === 1 ? 'Şimdi varış noktasını seç.' :
        positions.length === 2 ? 'Başlangıç ve varış seçildi. Rotanı hesapla.' :
        `${positions.length - 2} ara durak eklendi. Rota bütün duraklardan sırayla geçecek.`);
    };
    map.on('click', e => place(e.latlng));
    addStop.addEventListener('click', () => {
      if (positions.length < 2 || positions.length >= 7) return;
      choosingStop = true; clearRoute(); updateButtons();
      setStatus('Haritada ara durak eklemek istediğin konuma dokun. Rota bu noktadan da geçecek.');
    });
    document.querySelectorAll('[data-route-preset]').forEach(button => button.addEventListener('click', () => {
      const coords = button.dataset.routePreset.split('|').map(s => {
        const [lat,lng] = s.split(',').map(Number); return {lat,lng};
      });
      if (coords.length < 2 || !coords.every(inBounds)) return;
      reset(); positions = coords; drawPins(); updateButtons();
      map.fitBounds(L.latLngBounds(coords), {padding:[44,44],maxZoom:9});
      setStatus('Örnek noktalar seçildi. Gerçek yolu hesaplamak için düğmeye bas.');
    }));

    function updateCost() {
      if (!route) return;
      const price = calculateFuelCost(route.distanceKm, $('fuel-consumption').value, priceInput.value);
      $('route-cost').textContent = price === null ? '—' : price.toLocaleString('tr-TR',{maximumFractionDigits:2});
    }
    $('fuel-consumption').addEventListener('input', updateCost);
    const scrollToResults = node => { if (node?.scrollIntoView) setTimeout(() => node.scrollIntoView({behavior:'smooth',block:'nearest'}), 120); };
    const chooseRoute = (item, index, allRoutes) => {
      route = item;
      if (poly) map.removeLayer(poly);
      poly = L.geoJSON(item.geometry,{style:{color:'#246b71',weight:5,opacity:.96}}).addTo(map);
      map.fitBounds(poly.getBounds(), {padding:[30,30],maxZoom:13});
      $('route-km').textContent = format(item.distanceKm);
      $('route-time').textContent = formatDuration(item.durationMinutes);
      if (resultPanel) resultPanel.hidden = false;
      if (altButtons) {
        if (altWrapper) altWrapper.hidden = false;
        altButtons.replaceChildren();
        allRoutes.forEach((option, optionIndex) => {
          const btn = document.createElement('button');
          btn.type = 'button';
          btn.className = 'route-option' + (optionIndex === index ? ' active' : '');
          btn.setAttribute('aria-pressed', optionIndex === index ? 'true' : 'false');
          btn.textContent = `${option.label || `Rota ${optionIndex+1}`} · ${format(option.distanceKm)} km · ${formatDuration(option.durationMinutes)}`;
          btn.addEventListener('click', () => { chooseRoute(option,optionIndex,allRoutes); scrollToResults(resultPanel); });
          altButtons.appendChild(btn);
        });
      }
      updateCost();
      if (saveForm) {
        const a = positions[0], b = positions[positions.length-1];
        for (const [key,value] of Object.entries({StartLat:a.lat,StartLon:a.lng,
          EndLat:b.lat,EndLon:b.lng,DistanceKm:item.distanceKm,
          DurationMinutes:item.durationMinutes,GeoJson:JSON.stringify(item.geometry)})) {
          const input = saveForm.elements.namedItem(key);
          if (input) input.value = value;
        }
        if (saveButton) saveButton.disabled = false;
      }
    };
    build.addEventListener('click', async () => {
      if (positions.length < 2 || choosingStop || calculating) return;
      clearRoute();
      const generation = ++routeGeneration; calculating = true;
      build.disabled = true;
      setStatus('Tüm ara durakları kapsayan gerçek yol güzergâhı hesaplanıyor…');
      const a=positions[0], b=positions[positions.length-1];
      const query=new URLSearchParams({lat1:String(a.lat),lon1:String(a.lng),lat2:String(b.lat),lon2:String(b.lng),
        waypoints:positions.slice(1,-1).map(pt=>`${pt.lat},${pt.lng}`).join('|')});
      try {
        const response=await fetch(`/rota-planlayici/hesapla?${query}`);
        const data=await response.json();
        if (generation !== routeGeneration) return;
        if(!response.ok) throw Error(data.error||'Rota servisi yanıt vermedi.');
        if(!data.geometry || data.geometry.type!=='LineString') throw Error('Gerçek yol geometrisi alınamadı.');
        const options=(Array.isArray(data.alternatives)&&data.alternatives.length
          ? data.alternatives : [{...data,label:'Rota 1'}]).filter(x=>x.geometry?.type==='LineString');
        if(!options.length) throw Error('Kullanılabilir rota bulunamadı.');
        chooseRoute(options[0],0,options);
        setStatus(options.length>1
          ? `${options.length} gerçek güzergâh bulundu. Alternatifleri sonuç bölümünden seçebilirsin.`
          : positions.length>2 ? 'Rota tüm ara duraklardan sırayla geçiyor. Servis başka alternatif sunmadı.'
          : 'Gerçek yol rotası hazır. Servis başka alternatif sunmadı.');
        scrollToResults(resultPanel || status);
      } catch(err) { if (generation === routeGeneration) { clearRoute(); setStatus(err.message || 'Rota hesaplanamadı.',true); } }
      finally { if (generation === routeGeneration) calculating = false; updateButtons(); }
    });

    weather.addEventListener('click', async () => {
      if (positions.length < 2) return;
      const generation = ++weatherGeneration;
      weather.disabled = true;
      weatherStatus.textContent = 'Varış noktası hava durumu getiriliyor…';
      const point=positions[positions.length-1];
      try {
        const query=new URLSearchParams({lat:String(point.lat),lon:String(point.lng)});
        const response=await fetch(`/rota-planlayici/hava?${query}`);
        const data=await response.json();
        if (generation !== weatherGeneration) return;
        if(!response.ok) throw Error(data.error||'Hava bilgisi alınamadı.');
        weatherStatus.textContent = `Varış: ${format(data.temperatureC)} °C · ${formatWeather(data.weatherCode)} · Rüzgâr ${format(data.windKmh)} km/sa.`;
        if(weatherPanel && weatherCurrent && weatherDays) {
          weatherCurrent.textContent=`${formatWeather(data.weatherCode)} · ${format(data.temperatureC)} °C · Yağış ${format(data.precipitationMm)} mm · Rüzgâr ${format(data.windKmh)} km/sa`;
          weatherDays.replaceChildren();
          for(const day of data.days||[]) {
            const item=document.createElement('div'); item.className='weather-day';
            item.textContent=`${new Date(day.date+'T12:00:00').toLocaleDateString('tr-TR',{weekday:'short',day:'numeric',month:'short'})} · ${formatWeather(day.code)} · ${format(day.minC)}° / ${format(day.maxC)}° · Yağış olasılığı %${format(day.rainProbability)}`;
            weatherDays.appendChild(item);
          }
          weatherPanel.hidden=false; scrollToResults(weatherPanel);
        }
      } catch(err) {
        if (generation !== weatherGeneration) return;
        weatherStatus.textContent=err.message||'Hava durumu alınamadı.';
        if(weatherPanel) weatherPanel.hidden=true;
      } finally { if (generation === weatherGeneration) weather.disabled=positions.length<2; }
    });

  });
})();
