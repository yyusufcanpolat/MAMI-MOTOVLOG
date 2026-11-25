'use strict';
if ('serviceWorker' in navigator) window.addEventListener('load', () => {
  navigator.serviceWorker.register('/rider-sw.js').catch(() => {/* PWA is optional */});
});
