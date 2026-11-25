'use strict';
document.addEventListener('DOMContentLoaded', async () => {
  const brandSelect = document.getElementById('brand-select');
  if (!brandSelect) return;
  const modelSelect = document.getElementById('model-select');
  const variantSelect = document.getElementById('variant-select');
  const brandName = document.getElementById('bike-brand');
  const modelName = document.getElementById('bike-model');
  const variantId = document.getElementById('bike-variant');
  const catalog = document.getElementById('catalog-selection');
  const buttons = [document.getElementById('catalog-mode'), document.getElementById('manual-mode')];
  buttons.forEach((btn, idx) => btn.addEventListener('click', () => {
    catalog.hidden = idx === 1;
    buttons.forEach(x => x.classList.remove('active'));
    btn.classList.add('active');
    if (idx === 1) variantId.value = '';
  }));
  const fill = (select, rows, label) => {
    select.replaceChildren(new Option(label, ''));
    for (const item of rows) select.add(new Option(item.name ?? "", item.id));
    select.disabled = false;
  };
  const load = async url => {
    const r = await fetch(url, { credentials: 'same-origin' });
    if (!r.ok) throw Error('Katalog alınamadı');
    return await r.json();
  };
  try { fill(brandSelect, await load('/garaj/katalog/markalar'), 'Marka seç'); }
  catch { brandSelect.replaceChildren(new Option('Katalog geçici olarak erişilemiyor; manuel ekle', '')); }
  let brandRequest = 0, modelRequest = 0;
  [brandName, modelName, document.getElementById('ModelYear'), document.getElementById('Edition')].forEach(el => el?.addEventListener('input', () => { variantId.value = ''; variantSelect.value = ''; }));
  brandSelect.addEventListener('change', async () => {
    const seq = ++brandRequest; ++modelRequest;
    variantId.value = ''; modelName.value = '';
    modelSelect.replaceChildren(new Option('Model seç', '')); modelSelect.disabled = true;
    variantSelect.replaceChildren(new Option('Yıl/versiyon seç', '')); variantSelect.disabled = true;
    brandName.value = brandSelect.selectedOptions[0]?.textContent === 'Marka seç' ? '' : brandSelect.selectedOptions[0]?.textContent || '';
    if (!brandSelect.value) return;
    try { const rows = await load('/garaj/katalog/modeller/' + encodeURIComponent(brandSelect.value)); if (seq === brandRequest) fill(modelSelect, rows, 'Model seç'); }
    catch { if (seq === brandRequest) modelSelect.replaceChildren(new Option('Modeller alınamadı', '')); }
  });
  modelSelect.addEventListener('change', async () => {
    const seq = ++modelRequest;
    variantId.value = '';
    modelName.value = modelSelect.selectedOptions[0]?.textContent === 'Model seç' ? '' : modelSelect.selectedOptions[0]?.textContent || '';
    variantSelect.replaceChildren(new Option('Yıl/versiyon seç', ''));
    variantSelect.disabled = true;
    if (!modelSelect.value) return;
    try {
      const variants = await load('/garaj/katalog/versiyonlar/' + encodeURIComponent(modelSelect.value));
      if (seq !== modelRequest) return;
      if (variants.length) {
        variants.forEach(v => {
          const opt = new Option(`${v.year} · ${v.edition}${v.engineCc ? ' · ' + v.engineCc + ' cc' : ''}`, v.id);
          opt.dataset.year = v.year;
          opt.dataset.edition = v.edition;
          opt.dataset.tank = v.tankLitres ?? '';
          variantSelect.add(opt);
        });
        variantSelect.disabled = false;
      }
    } catch { /* Manual year stays available. */ }
  });
  variantSelect.addEventListener('change', () => {
    variantId.value = variantSelect.value;
    const opt = variantSelect.selectedOptions[0];
    if (opt?.dataset.year) document.getElementById('ModelYear').value = opt.dataset.year;
    if (opt?.dataset.edition) document.getElementById('Edition').value = opt.dataset.edition;
    if (opt?.dataset.tank && !document.getElementById('TankLitres').value) document.getElementById('TankLitres').value = opt.dataset.tank;
  });
});
