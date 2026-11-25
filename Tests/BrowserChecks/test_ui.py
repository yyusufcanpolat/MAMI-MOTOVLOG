"""Optional fixture browser test; not a running Razor application. Requires playwright and Chromium."""
from pathlib import Path
from playwright.sync_api import sync_playwright
import os
root = Path(__file__).resolve().parents[2] / 'MamiMotovlog' / 'wwwroot'
with sync_playwright() as pw:
    browser = pw.chromium.launch(executable_path=os.getenv('CHROMIUM_PATH') or None, args=['--no-sandbox'])
    page = browser.new_page(viewport={'width': 1280,'height':800})
    page.set_content('''<!doctype html><body class="admin-body studio-body admin-v52"><aside class="platform-sidebar"><nav><a href="#">Kullanıcılar</a></nav></aside><header class="platform-top"><button class="platform-menu" aria-expanded="false">Menü</button><span class="platform-user">Yönetici</span></header><main id="main-content"><section class="shell admin-shell"><div class="admin-stats"><a>Üyeler 1</a><a>Motorlar 2</a></div><form action="/yonetim/fotograf/1/sil" method="post"><button id="delete">Sil</button></form><input id="file" type="file"><form><label for="required">Başlık</label><input id="required" required></form></section></main></body>''')
    for name in ['site','studio','rider','mono','v52-ui','admin-v52']:
        page.add_style_tag(path=root / 'css' / (name+'.css'))
    page.add_script_tag(path=root/'js/v52-ui.js')
    page.evaluate("window.submitted=0;document.addEventListener('submit',e=>{if(!e.defaultPrevented)window.submitted++;e.preventDefault()})")
    page.click('#delete'); assert page.locator('dialog').is_visible()
    page.click('[data-cancel]'); assert not page.locator('dialog').is_visible(); assert page.evaluate('window.submitted')==0
    page.click('#delete'); page.click('[data-ok]'); assert page.evaluate('window.submitted')==1
    page.locator('#file').set_input_files({'name':'test.jpg','mimeType':'image/jpeg','buffer':b'test fixture'})
    assert page.locator('.v52-file small').inner_text()=='test.jpg'
    assert page.locator('.v52-required').count()==1
    page.set_viewport_size({'width':390,'height':844})
    assert page.evaluate('document.documentElement.scrollWidth <= window.innerWidth')
    page.click('.platform-menu'); assert page.locator('.platform-sidebar').evaluate("e=>e.classList.contains('open')")
    page.keyboard.press('Escape'); assert page.locator('.platform-menu').get_attribute('aria-expanded')=='false'
    browser.close()
print('PASS Chromium fixture: delete confirm/cancel, selected filename, required mark, 390px viewport and mobile navigation')
