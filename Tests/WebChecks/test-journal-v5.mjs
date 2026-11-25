/* MAMI V5: route alternatives, ordered stops, duration, online fuel quote, 3-day weather. Zero npm dependencies. */
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {fileURLToPath} from 'node:url';
import path from 'node:path';
import vm from 'node:vm';
const base = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../MamiMotovlog');
const script = readFileSync(path.join(base,'wwwroot/js/rider-map.js'), 'utf8');
function el(id='') { return {
  id, disabled:false, hidden:false, value:'', textContent:'', className:'', children:[],dataset:{},handlers:{},attrs:{},
  classList:{add(){},toggle(){}},addEventListener(type,cb){this.handlers[type]=cb;},
  replaceChildren(...items){this.children=items;}, appendChild(child){this.children.push(child)},
  setAttribute(k,v){this.attrs[k]=v}, scrollIntoView(){},
  async click(){return await this.handlers.click?.({preventDefault(){}});},
  async input(){return await this.handlers.input?.({preventDefault(){}});}
};}
const ids=['rider-map','route-status','weather-status','add-stop','build-route','weather-button','save-trip',
 'save-route','reset-pins','route-km','route-time','route-cost','fuel-consumption','fuel-price',
 'planner-results','route-choices','weather-details','weather-current','weather-days','stop-summary',
 'fuel-price-status','fuel-side','fuel-type','fuel-refresh'];
const dom = Object.fromEntries(ids.map(id=>[id,el(id)]));
dom['fuel-consumption'].value = '5';
dom['fuel-side'].value='anadolu';dom['fuel-type'].value='benzin';
dom['save-trip'].elements={namedItem:()=>el('test')};
let ready;
const preset=el('preset');preset.dataset.routePreset='41,29|41.2,29.6';
const doc={getElementById:id=>dom[id],createElement:()=>el('option'),
 addEventListener:(name,cb)=>{if(name==='DOMContentLoaded')ready=cb;},
 querySelectorAll:selector=>selector==='[data-route-preset]'?[preset]:[]};
let clickedMap;
const map={fitBounds(){},invalidateSize(){},removeLayer(){},on(name,cb){if(name==='click')clickedMap=cb;}};
const L={map:()=>map,tileLayer(){return{addTo(){return this;},on(){return this;}}},latLngBounds:p=>p,
 circleMarker(){return{addTo(){return this;},bindTooltip(){return this;}}},
 geoJSON(){return{addTo(){return this;},getBounds(){return [[40,29],[41,30]]}}}};
const calls=[];
const geometry={type:'LineString',coordinates:[[29,41],[29.6,41.2]]};
const altGeom={type:'LineString',coordinates:[[29,41],[29.2,41.1],[29.6,41.2]]};
let currentPrice=68.20;
const fetch=async url=>{
 calls.push(url);
 if(url.startsWith('/rota-planlayici/istanbul-yakit'))return {ok:true,async json(){return {
   price:currentPrice,isRecent:true,checkedAt:'2026-11-24T08:00:00Z'}}};
 if(url.startsWith('/rota-planlayici/hava'))return {ok:true,async json(){return {
   temperatureC:14,precipitationMm:0,windKmh:8,weatherCode:2,time:'2026-11-24T15:00',
   days:[{date:'2026-11-24',minC:9,maxC:17,rainProbability:20,code:2},
         {date:'2026-11-25',minC:8,maxC:13,rainProbability:65,code:61}]}}};
 if(url.startsWith('/rota-planlayici/hesapla'))return {ok:true,async json(){return {
  distanceKm:146.6,durationMinutes:1000,geometry,
  alternatives:[{id:0,label:'İlk güzergâh',distanceKm:146.6,durationMinutes:1000,geometry},
  {id:1,label:'Alternatif 1',distanceKm:155,durationMinutes:156,geometry:altGeom}]}}};
 throw new Error('Unexpected URL '+url);
};
const scope={document:doc,window:{L,addEventListener(){}},L,fetch,console,
 setTimeout:cb=>cb(),requestAnimationFrame:cb=>cb(),URLSearchParams,Date};
vm.runInNewContext(script,scope,{filename:'rider-map.js'});
assert.equal(scope.window.MamiRouteUtils.formatDuration(1000),'16 saat 40 dk');
assert.equal(scope.window.MamiRouteUtils.formatDuration(156),'2 saat 36 dk');
assert.equal(scope.window.MamiRouteUtils.formatDuration(60),'1 saat');
assert.equal(scope.window.MamiRouteUtils.calculateFuelCost(100,5,68.2),341);
ready();
await new Promise(resolve=>setTimeout(resolve,0));
assert.equal(dom['fuel-price'].value,'68.20','Verified current Istanbul fuel price should auto fill');
await preset.click();
await dom['build-route'].click();
assert.equal(dom['route-time'].textContent,'16 saat 40 dk','Time is hours+minutes, not total minutes');
assert.equal(dom['planner-results'].hidden,false,'Route summary opens');
assert.equal(dom['route-choices'].children.length,2,'Alternative selector displayed');
assert.match(dom['route-choices'].children[1].textContent,/2 saat 36 dk/);
await dom['route-choices'].children[1].click();
assert.equal(dom['route-km'].textContent,'155');
assert.equal(dom['route-time'].textContent,'2 saat 36 dk');
assert.equal(dom['route-choices'].children[1].attrs['aria-pressed'],'true');
await dom['weather-button'].click();
assert.equal(dom['weather-details'].hidden,false,'Weather panel opens');
assert.equal(dom['weather-days'].children.length,2,'Forecast days show');
assert.match(dom['weather-status'].textContent,/14/);
await dom['reset-pins'].click();
clickedMap({latlng:{lat:41,lng:29}});clickedMap({latlng:{lat:41.3,lng:29.8}});
await dom['add-stop'].click();clickedMap({latlng:{lat:41.2,lng:29.4}});
await dom['build-route'].click();
assert.ok(calls.some(url=>url.includes('waypoints=41.2%2C29.4')),'All via points must be passed server-side');
assert.match(dom['stop-summary'].textContent,/Durak 1/);
console.log('PASS V5: 1000 min format; alternatives; selected path; dynamic fuel; cost; weather panel; 2-day forecast; ordered stops.');
