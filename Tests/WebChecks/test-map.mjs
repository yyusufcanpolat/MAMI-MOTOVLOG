/* No npm dependencies. Map UI unit/smoke test in Node 22 using a Leaflet mock. */
import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import vm from 'node:vm';
import {fileURLToPath} from 'node:url';
import path from 'node:path';
const root=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'../../MamiMotovlog');
const script=readFileSync(path.join(root,'wwwroot/js/rider-map.js'),'utf8');

function elem(id) {
 return {
  id, disabled:false, value:'', textContent:'', classList:{add(){},toggle(){}},dataset:{},handlers:{},
  addEventListener(type,cb){this.handlers[type]=cb;},
  async click(){return this.handlers.click?.({preventDefault(){}});},
  async input(){return this.handlers.input?.({preventDefault(){}});}
 };
}
const elements=Object.fromEntries(['rider-map','route-status','weather-status','add-stop','build-route',
 'weather-button','save-trip','save-route','reset-pins','route-km','route-time','route-cost',
 'fuel-consumption','fuel-price'].map(id=>[id,elem(id)]));
elements['fuel-consumption'].value='4';
elements['fuel-price'].value='40';
const saveInputs=Object.fromEntries(['StartLat','StartLon','EndLat','EndLon','DistanceKm','DurationMinutes','GeoJson'].map(id=>[id,elem(id)]));
elements['save-trip'].elements={namedItem:key=>saveInputs[key]};
let domReady=()=>{};
const quick=elem('quick');quick.dataset.routePreset='41,29|41.2,29.6';
const document={getElementById:id=>elements[id],addEventListener:(name,cb)=>{if(name==='DOMContentLoaded')domReady=cb;},
 querySelectorAll:sel=>sel==='[data-route-preset]'?[quick]:[]};
let mapClick=()=>{};
const map={fitBounds(){},invalidateSize(){},removeLayer(){},on(name,cb){if(name==='click')mapClick=cb;}};
const L={
 map(){return { ...map, fitBounds:map.fitBounds.bind(map) };},
 tileLayer(){return {addTo(){return this;},on(){return this;}};},
 latLngBounds:points=>points,
 circleMarker(){return {addTo(){return this;},bindTooltip(){return this;}};},
 geoJSON(){return {addTo(){return this;},getBounds(){return [[39,29],[41,32]];}};}
};
const calls=[];
let simulateRouteUnavailable=false;
const fetch=async (url,opts) => {
 calls.push(url);
 if(url.startsWith('/rota-planlayici/hava'))return {ok:true,async json(){return {temperatureC:19,windKmh:10,precipitationMm:0};}};
 if(url.startsWith('/rota-planlayici/hesapla') && simulateRouteUnavailable) return {ok:false,async json(){return {error:'Rota servisi 502: geçici hata'};}};
 if(url.startsWith('/rota-planlayici/hesapla'))return {ok:true,async json(){return {distanceKm:150,durationMinutes:180,
 geometry:{type:'LineString',coordinates:[[29,41],[30,41]]}};}};
 throw new Error('Unexpected fetch '+url);
};
const scope={document,window:{L,addEventListener(){}},L,fetch,setTimeout:cb=>cb(),
 requestAnimationFrame:cb=>cb(),console,URLSearchParams};
vm.runInNewContext(script,scope,{filename:'rider-map.js'});
domReady();
assert.ok(!elements['rider-map'].classList._error,'Leaflet should be available');
await quick.click();
assert.equal(elements['build-route'].disabled,false,'preset enables routing');
await elements['build-route'].click();
assert.equal(elements['route-km'].textContent,'150');
assert.equal(elements['route-cost'].textContent,'240');
assert.equal(saveInputs['DistanceKm'].value,150);
assert.equal(elements['save-route'].disabled,false);
await elements['weather-button'].click();
assert.match(elements['weather-status'].textContent,/19/);
await elements['reset-pins'].click();
assert.equal(elements['build-route'].disabled,true,'reset should disable routing');
mapClick({latlng:{lat:41,lng:29}});
mapClick({latlng:{lat:41.4,lng:29.7}});
await elements['add-stop'].click();
mapClick({latlng:{lat:41.2,lng:29.4}});
await elements['build-route'].click();
assert.ok(calls.some(u=>u.includes('waypoints=41.2%2C29.4')),'route request must preserve intermediate waypoints');
mapClick({latlng:{lat:50,lng:29}});
assert.match(elements['route-status'].textContent,/Türkiye/,'foreign coordinates should be rejected');
await quick.click();
simulateRouteUnavailable=true;
await elements['build-route'].click();
assert.match(elements['route-status'].textContent,/Rota servisi 502/,'Real server errors should be explained');
assert.equal(elements['route-km'].textContent,'—','Do not display stale estimate after a server failure');
assert.equal(elements['save-route'].disabled,true,'Do not allow saving a route without verified geometry');
console.log('PASS map route / preset / stop / reset / weather / fuel cost / save / Turkey bounds / API failure state');
