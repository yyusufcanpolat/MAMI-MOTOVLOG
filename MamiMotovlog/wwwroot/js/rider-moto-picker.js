/* MAMI MOTOVLOG — Rota için motosiklet seçimi. Verisi olmayan model için tüketim uydurulmaz. */
'use strict';
(function () {
  const normal = value => String(value || '').trim().toLocaleUpperCase('tr-TR');
  const validFuel = value => Number.isFinite(Number(value)) && Number(value) > 0 && Number(value) <= 50;
  // Unit-testable pure helpers. All DOM rendering uses textContent / Option, never external HTML.
  const findReference = (records, brand, model) => (records || []).find(
    r => normal(r.brand) === normal(brand) && normal(r.model) === normal(model));
  window.MamiFuelPicker = { findReference, validFuel };

  document.addEventListener('DOMContentLoaded', async () => {
    const brand = document.getElementById('planner-brand');
    if (!brand) return;
    const model = document.getElementById('planner-model');
    const year = document.getElementById('planner-year');
    const variant = document.getElementById('planner-variant');
    const manualVariant = document.getElementById('planner-manual-variant');
    const garage = document.getElementById('planner-garage-bike');
    const saveBike = document.getElementById('route-save-bike');
    const fuel = document.getElementById('fuel-consumption');
    const source = document.getElementById('fuel-source-status');
    const catalog = document.getElementById('planner-catalog-controls');
    const customBrand = document.getElementById('planner-custom-brand');
    const customModel = document.getElementById('planner-custom-model');
    let references = [];
    let currentVariants = [];
    let filling = false;
    let modelRequest = 0;
    let variantRequest = 0;
    const blank = (element, label) => {
      element.replaceChildren(new Option(label, ''));
      element.disabled = true;
    };
    const getJson = async url => {
      const response = await fetch(url, { credentials: 'same-origin' });
      if (!response.ok) throw new Error('Katalog bağlantısı kullanılamıyor.');
      return response.json();
    };
    const setFuel = (value, label) => {
      filling = true;
      fuel.value = validFuel(value) ? String(value) : '';
      source.textContent = label;
      fuel.dispatchEvent(new Event('input', { bubbles: true }));
      filling = false;
    };
    const catalogLabel = () => {
      const selected = findReference(references, brand.selectedOptions[0]?.textContent, model.selectedOptions[0]?.textContent);
      if (!selected) {
        setFuel('', 'Bu marka/model için doğrulanmış ortalama bulunamadı. L/100 km değerini kendin yazabilirsin.');
        return;
      }
      const pickedYear = Number(year.value);
      if (!pickedYear) {
        setFuel('', `Model yılını seç. ${selected.sourceLabel} yalnızca ${selected.referenceYear} yılı için kayıtlıdır.`);
        return;
      }
      if (pickedYear !== Number(selected.referenceYear)) {
        setFuel('', `Model yılı farklı (${pickedYear}); ${selected.referenceYear} yılı tüketimini otomatik uygulamıyoruz. Kendi L/100 km değerini girebilirsin.`);
        return;
      }
      setFuel(selected.litresPer100Km,
        `${selected.sourceLabel}. Bu üretici kılavuzundaki üst sınır örneğidir, gerçek sürüş tüketimin değildir.`);
    };

    fuel.addEventListener('input', () => {
      if (!filling) source.textContent = validFuel(fuel.value)
        ? 'Kişisel tüketim değerini kullanıyoruz. Yol, hız ve hava koşulları gerçek tüketimi değiştirebilir.'
        : 'Yakıt maliyeti için geçerli bir L/100 km değeri gir.';
    });
    for (const el of [customBrand, customModel]) {
      el?.addEventListener('input', () => {
        if (el.value.trim()) {
          if (garage) garage.value = '';
          if (saveBike) saveBike.value = '';
          source.textContent = 'Manuel motosiklet: ortalama tüketimini kendin gir.';
        }
      });
    }

    if (garage) garage.addEventListener('change', () => {
      const option = garage.selectedOptions[0];
      if (saveBike) saveBike.value = garage.value || '';
      catalog.classList.toggle('planner-muted-controls', Boolean(garage.value));
      if (garage.value) {
        setFuel(option.dataset.fuel || '', validFuel(option.dataset.fuel)
          ? `Garajındaki motorun verisi kullanılıyor (${option.dataset.source}). Değeri değiştirebilirsin.`
          : 'Garajındaki motor için tüketim bilgisi bulunamadı; L/100 km değerini kendin gir.');
      } else catalogLabel();
    });

    try {
      const [brands, refData] = await Promise.all([
        getJson('/garaj/katalog/markalar'),
        getJson('/data/fuel-references.json').catch(() => ({ references: [] }))
      ]);
      references = Array.isArray(refData.references) ? refData.references : [];
      if (!Array.isArray(brands)) throw new Error('Katalog biçimi geçersiz.');
      blank(brand, 'Marka seç');
      for (const item of brands) brand.add(new Option(item.name, item.id));
      brand.disabled = false;
    } catch (error) {
      blank(brand, 'Katalog şu an erişilemiyor');
      source.textContent = 'Motosiklet kataloğuna erişilemiyor. Yakıt tüketimini elle girerek rotanı planlayabilirsin.';
    }

    brand.addEventListener('change', async () => {
      if (garage) garage.value = '';
      if (saveBike) saveBike.value = '';
      catalog.classList.remove('planner-muted-controls');
      blank(model, brand.value ? 'Yükleniyor…' : 'Önce marka seç');
      blank(year, 'Önce model seç');
      blank(variant, 'Standart / belirtilmemiş');
      currentVariants = []; modelRequest++; variantRequest++;
      if (manualVariant) manualVariant.value = '';
      setFuel('', 'Model seçtiğinde varsa doğrulanmış tüketim değeri gösterilecek.');
      if (!brand.value) return;
      const currentModelRequest = modelRequest;
      try {
        const models = await getJson('/garaj/katalog/modeller/' + encodeURIComponent(brand.value));
        if (currentModelRequest !== modelRequest) return;
        blank(model, models.length ? 'Model seç' : 'Model bulunamadı — elle yazabilirsin');
        for (const item of models) model.add(new Option(item.name, item.id));
        model.disabled = !models.length;
      } catch { if (currentModelRequest === modelRequest) blank(model, 'Modeller alınamadı'); }
    });

    model.addEventListener('change', async () => {
      if (garage) garage.value = '';
      if (saveBike) saveBike.value = '';
      blank(year, 'Önce model seç');
      blank(variant, 'Standart / belirtilmemiş');
      currentVariants = []; variantRequest++;
      if (manualVariant) manualVariant.value = '';
      if (!model.value) { setFuel('', 'Lütfen model seç.'); return; }
      const currentYear = new Date().getFullYear();
      year.replaceChildren(new Option('Model yılı seç (isteğe bağlı)', ''));
      for (let yr = currentYear + 1; yr >= 1990; yr--) year.add(new Option(String(yr), String(yr)));
      year.disabled = false;
      catalogLabel();
      const requested = variantRequest;
      try {
        const available = await getJson('/garaj/katalog/versiyonlar/' + encodeURIComponent(model.value));
        if (requested === variantRequest) {
          currentVariants = Array.isArray(available) ? available : [];
          refreshVariants(); // Year may have been selected before the network call finished.
        }
      } catch { if (requested === variantRequest) { currentVariants = []; refreshVariants(); } }
    });

    function refreshVariants() {
      blank(variant, 'Versiyon belirtilmedi / listede yok');
      if (!year.value) return;
      const matching = currentVariants.filter(item => Number(item.year) === Number(year.value));
      for (const item of matching) variant.add(new Option(item.edition || 'Standart', String(item.id)));
      // A missing catalogue variant must never block choosing a motorcycle.
      variant.disabled = false;
    }
    year.addEventListener('change', () => {
      refreshVariants();
      if (model.value) catalogLabel();
    });
  });
})();
