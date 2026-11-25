document.addEventListener('DOMContentLoaded',()=>{
 const form=document.getElementById('save-trip'),button=document.getElementById('save-route'),feedback=document.getElementById('save-feedback');
 if(!form||!button||!feedback)return;
 form.addEventListener('submit',async e=>{
  e.preventDefault();if(button.disabled)return;button.disabled=true;feedback.textContent='Rota kaydediliyor…';
  try{
   const headers={'X-Mami-Async':'1','Accept':'application/json'};
   const csrf=form.querySelector?.('input[name="__RequestVerificationToken"]')?.value;
   if(csrf)headers.RequestVerificationToken=csrf;
   const response=await fetch(form.action,{method:'POST',body:new FormData(form),headers,credentials:'same-origin'});
   if(response.redirected){const url=new URL(response.url,location.origin);if(url.origin===location.origin && (url.pathname==='/uye/giris'||/^\/garaj\/rotalar\/\d+$/.test(url.pathname))){location.assign(url.href);return;}throw Error('Beklenmeyen yönlendirme. Garajı kontrol edin.');}
   const data=await response.json().catch(()=>null);
   if(!data) {
    if(response.status===400) throw Error('İstek doğrulanamadı. Sayfayı yenileyip yeniden giriş yaparak tekrar deneyin.');
    if(response.status===401||response.status===403) throw Error('Oturumunuzu kontrol edip tekrar giriş yapın.');
    if(response.status===429) throw Error('Çok fazla istek. Bir süre sonra yeniden deneyin.');
    throw Error('Sunucudan kayıt yanıtı alınamadı (HTTP '+response.status+'). Garajı kontrol edin.');
   }
   if(!response.ok)throw Error(data.error||'Rota kaydedilemedi.');
   if(typeof data.url!=='string'||!/^\/garaj\/rotalar\/\d+$/.test(data.url))throw Error('Kayıt yanıtı alınamadı. Garajdaki rotaları kontrol edin.');
   location.assign(data.url);
  }catch(error){feedback.textContent=error.message||'Rota kaydedilemedi. Tekrar deneyin.';button.disabled=false;}
 });
});