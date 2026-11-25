(() => {
  const menuButton = document.querySelector('.menu-toggle');
  const menu = document.querySelector('#site-nav');
  if (menu && menuButton) {
    menuButton.addEventListener('click', () => {
      const expanded = menuButton.getAttribute('aria-expanded') === 'true';
      menuButton.setAttribute('aria-expanded', String(!expanded));
      menuButton.setAttribute('aria-label', expanded ? 'Menüyü aç' : 'Menüyü kapat');
      menu.classList.toggle('open', !expanded);
    });
    menu.querySelectorAll('a').forEach(link => link.addEventListener('click', () => {
      menu.classList.remove('open'); menuButton.setAttribute('aria-expanded', 'false');
    }));
    document.addEventListener('keydown', event => {
      if (event.key === 'Escape') { menu.classList.remove('open'); menuButton.setAttribute('aria-expanded', 'false'); }
    });
  }

  const lightbox = document.querySelector('#lightbox');
  const buttons = [...document.querySelectorAll('[data-lightbox-src]')];
  if (!lightbox || !buttons.length) return;
  const viewer = lightbox.querySelector('img');
  const caption = lightbox.querySelector('figcaption');
  let current = 0;
  let lastFocused = null;
  function show(index) {
    current = (index + buttons.length) % buttons.length;
    viewer.src = buttons[current].dataset.lightboxSrc;
    viewer.alt = buttons[current].dataset.lightboxCaption || 'Fotoğraf';
    caption.textContent = buttons[current].dataset.lightboxCaption || '';
  }
  function close() {
    lightbox.classList.remove('open');
    lightbox.setAttribute('aria-hidden', 'true');
    viewer.removeAttribute('src');
    document.body.style.overflow = '';
    if (lastFocused) lastFocused.focus();
  }
  buttons.forEach((button, i) => button.addEventListener('click', () => {
    lastFocused = document.activeElement;
    show(i);
    lightbox.classList.add('open');
    lightbox.setAttribute('aria-hidden', 'false');
    document.body.style.overflow = 'hidden';
    lightbox.querySelector('.lightbox-close').focus();
  }));
  lightbox.querySelector('.lightbox-close').addEventListener('click', close);
  lightbox.querySelector('.lightbox-prev').addEventListener('click', () => show(current - 1));
  lightbox.querySelector('.lightbox-next').addEventListener('click', () => show(current + 1));
  lightbox.addEventListener('click', event => { if (event.target === lightbox) close(); });
  document.addEventListener('keydown', event => {
    if (!lightbox.classList.contains('open')) return;
    if (event.key === 'Escape') close();
    if (event.key === 'ArrowRight') show(current + 1);
    if (event.key === 'ArrowLeft') show(current - 1);
  });
})();

// İletişim: cep telefonu alanında rakam dışında tuşlanan karakterleri temizle.
document.querySelectorAll('input[name="Phone"]').forEach(input => {
  input.addEventListener('input', () => { input.value = input.value.replace(/[^0-9]/g, '').slice(0, 11); });
});
