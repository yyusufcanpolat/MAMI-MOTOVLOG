import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import vm from 'node:vm';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../../MamiMotovlog');
const source=readFileSync(path.join(root,'wwwroot/js/rider-moto-picker.js'),'utf8');
class FakeOption {
  constructor(text, value) { this.textContent=text;this.value=String(value);this.dataset={}; }
}
class Element {
  constructor(){this.children=[];this.handlers={};this.value='';this.disabled=false;this.dataset={};this.classList={toggle(){},remove(){}};this.textContent='';}
  replaceChildren(...list){this.children=list;this.value='';}
  add(o){this.children.push(o);}
  get selectedOptions(){return this.children.filter(e=>e.value===this.value).slice(0,1);}
  addEventListener(name,cb){this.handlers[name]=cb;}
  async fire(name){return await this.handlers[name]?.({preventDefault(){}});}
  dispatchEvent(e){this.handlers[e.type]?.(e);}
}
const ids=['planner-brand','planner-model','planner-year','planner-variant','planner-garage-bike','route-save-bike','fuel-consumption','fuel-source-status','planner-catalog-controls','planner-custom-brand','planner-custom-model'];
const el=Object.fromEntries(ids.map(id=>[id,new Element()]));
el['planner-garage-bike'].add(new FakeOption('Katalog', ''));
const bike=new FakeOption('CFMOTO 450SR · 2024','7');bike.dataset.fuel='3.8';bike.dataset.source='Gerçek yakıt kayıtları';
el['planner-garage-bike'].add(bike);
el['route-save-bike'].add(new FakeOption('Seçilmedi', ''));
el['route-save-bike'].add(new FakeOption('450SR','7'));
const payloads={
 '/garaj/katalog/markalar':[{id:1,name:'CFMOTO'},{id:2,name:'Honda'}],
 '/garaj/katalog/modeller/1':[{id:10,name:'450SR'},{id:11,name:'450NK'}],
 '/garaj/katalog/modeller/2':[{id:12,name:'CB650R'}],
 '/garaj/katalog/versiyonlar/10':[{id:101,year:2024,edition:'Standart'}],
 '/garaj/katalog/versiyonlar/12':[],
 '/data/fuel-references.json':{references:[{brand:'CFMOTO',model:'450SR',litresPer100Km:4.5,referenceYear:2024,sourceLabel:'450SR 2024 el kitabı'}]}
};
const fetch=async url=>({ok:payloads[url]!==undefined,json:async()=>payloads[url]});
let ready;
const ctx={window:{},document:{getElementById:id=>el[id],addEventListener:(name,cb)=>{if(name==='DOMContentLoaded')ready=cb;}},
  Option:FakeOption,Event:class {constructor(type){this.type=type;}},Date,fetch,console};
vm.runInNewContext(source,ctx,{filename:'rider-moto-picker.js'});
assert.equal(ctx.window.MamiFuelPicker.findReference(payloads['/data/fuel-references.json'].references,'cfmoto','450SR').litresPer100Km,4.5);
assert.equal(ctx.window.MamiFuelPicker.findReference(payloads['/data/fuel-references.json'].references,'Honda','Unknown'),undefined);
assert.equal(ctx.window.MamiFuelPicker.validFuel(0),false);
assert.equal(ctx.window.MamiFuelPicker.validFuel(4.5),true);
await ready();
assert.equal(el['planner-brand'].children.length,3);
el['planner-brand'].value='1';await el['planner-brand'].fire('change');
assert.equal(el['planner-model'].children.length,3);
el['planner-model'].value='10';await el['planner-model'].fire('change');
assert.equal(el['fuel-consumption'].value,'','No auto fuel before year selection');
el['planner-year'].value='2024';await el['planner-year'].fire('change');
assert.equal(el['fuel-consumption'].value,'4.5','Verified 2024 CFMOTO data used as reference only');
el['planner-year'].value='2025';await el['planner-year'].fire('change');
assert.equal(el['fuel-consumption'].value,'','Do not reuse 2024 spec for another year');
assert.match(el['fuel-source-status'].textContent,/Model yılı farklı/);
el['fuel-consumption'].value='6.1';await el['fuel-consumption'].fire('input');
assert.match(el['fuel-source-status'].textContent,/Kişisel tüketim/);
el['planner-brand'].value='2';await el['planner-brand'].fire('change');
el['planner-model'].value='12';await el['planner-model'].fire('change');
assert.equal(el['fuel-consumption'].value,'','No fake reference for unverified Honda model');
el['planner-garage-bike'].value='7';await el['planner-garage-bike'].fire('change');
assert.equal(el['fuel-consumption'].value,'3.8','Measured garage fuel prioritized');
assert.equal(el['route-save-bike'].value,'7','Selected garage bike is used for saved route');
console.log('PASS: public catalog, brand > model > year, reference with model-year caution, unknown manual fuel, garage measured fuel and save bike');
