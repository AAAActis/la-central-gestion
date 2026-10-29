// Capturas y mediciones CDP de HU-UI-03. Arnés: /api/acceso/login se intercepta (401 con la
// contraseña "mala", 200 con cualquier otra); el resto de /api responde 503, salvo cuando se
// pide el 401 de sesión vencida. Sin datos inventados en las pantallas: muestran su estado real.
import { writeFileSync } from 'node:fs';
const [SALIDA, BASE] = [process.argv[2], 'http://localhost:4300'];
const tabs = await (await fetch('http://127.0.0.1:9333/json')).json();
const ws = new WebSocket(tabs.find(t => t.type === 'page').webSocketDebuggerUrl);
await new Promise(r => ws.addEventListener('open', r, { once: true }));
let n = 0, apiVencida = false;
const pendientes = new Map();
ws.addEventListener('message', ({ data }) => {
  const m = JSON.parse(data);
  if (m.method === 'Fetch.requestPaused') return atender(m.params);
  pendientes.get(m.id)?.(m.result ?? {}); pendientes.delete(m.id);
});
const cdp = (method, params = {}) => new Promise(r => { pendientes.set(++n, r); ws.send(JSON.stringify({ id: n, method, params })); });
const b64 = s => Buffer.from(s).toString('base64');
function atender(p) {
  const url = p.request.url, json = [{ name: 'Content-Type', value: 'application/json' }];
  let codigo = 503, cuerpo = '';
  if (url.endsWith('/api/acceso/login')) {
    const mala = JSON.parse(p.request.postData ?? '{}').contrasena === 'mala';
    codigo = mala ? 401 : 200;
    cuerpo = mala ? JSON.stringify({ status: 401, detail: 'Usuario o contraseña incorrectos.' })
      : JSON.stringify({ token: 't', nombreUsuario: 'un nombre de usuario bastante largo', rol: 'ADMINISTRADOR', sucursalId: 1 });
  } else if (apiVencida) codigo = 401;
  cdp('Fetch.fulfillRequest', { requestId: p.requestId, responseCode: codigo, responseHeaders: json, body: b64(cuerpo) });
}
const js = async expr => (await cdp('Runtime.evaluate', { expression: expr, awaitPromise: true, returnByValue: true })).result?.value;
const esperar = ms => new Promise(r => setTimeout(r, ms));
const captura = async nombre => writeFileSync(`${SALIDA}/${nombre}.png`, Buffer.from((await cdp('Page.captureScreenshot', { format: 'png' })).data, 'base64'));
const desborde = () => js('[document.documentElement.scrollWidth, document.documentElement.clientWidth]');
const foco = () => js(`(() => { const e = document.activeElement; return e === document.body ? 'body' :
  e.tagName.toLowerCase() + (e.getAttribute('aria-label') ? '[' + e.getAttribute('aria-label') + ']' : '') + ' ' + (e.name || e.textContent.trim().slice(0, 30)); })()`);
const tecla = async (key, code, keyCode) => { for (const type of ['keyDown', 'keyUp']) await cdp('Input.dispatchKeyEvent', { type, key, code, windowsVirtualKeyCode: keyCode, ...(type === 'keyDown' && key === 'Enter' ? { text: String.fromCharCode(13) } : {}) }); await esperar(150); };
const tab = () => tecla('Tab', 'Tab', 9), escribir = async t => { await cdp('Input.insertText', { text: t }); await esperar(100); };
const log = (...a) => console.log(...a);

await cdp('Page.enable'); await cdp('Runtime.enable');
await cdp('Fetch.enable', { patterns: [{ urlPattern: '*/api/*' }] });

for (const [ancho, alto] of [[1024, 768], [768, 1024], [480, 800]]) {
  await cdp('Emulation.setDeviceMetricsOverride', { width: ancho, height: alto, deviceScaleFactor: 1, mobile: false });
  await js('sessionStorage.clear()');
  await cdp('Page.navigate', { url: BASE + '/' }); await esperar(2500);
  log(ancho, 'acceso', await desborde()); await captura(`hu-ui-03-${ancho}x${alto}-acceso`);
  await js(`(() => { const i = document.querySelectorAll('input');
    for (const [el, v] of [[i[0], 'admin'], [i[1], 'mala']]) { el.value = v; el.dispatchEvent(new Event('input')); }
    document.querySelector('button[type=submit]').click(); })()`); await esperar(1200);
  log(ancho, 'acceso con error', await desborde()); await captura(`hu-ui-03-${ancho}x${alto}-acceso-error`);
  await js(`(() => { const i = document.querySelectorAll('input'); i[1].value = 'buena'; i[1].dispatchEvent(new Event('input'));
    document.querySelector('button[type=submit]').click(); })()`); await esperar(1500);
  for (const ruta of ['articulos', 'factura-compra']) {
    await js(`document.querySelector('nav a[href="/${ruta}"]').click()`); await esperar(1200);
    log(ancho, ruta, await desborde()); await captura(`hu-ui-03-${ancho}x${alto}-${ruta}`);
  }
}

log('\n# F5 en el navegador real, a 1024');
await cdp('Emulation.setDeviceMetricsOverride', { width: 1024, height: 768, deviceScaleFactor: 1, mobile: false });
await cdp('Page.reload'); await esperar(2500);
log('después de F5:', await js(`[location.pathname, !!document.querySelector('header'), !!document.querySelector('app-login')]`));
log('guardado:', await js(`Object.keys(sessionStorage).join(',') + ' | localStorage: ' + Object.keys(localStorage).length + ' claves'`));

log('\n# Recorrido con teclado por el marco');
await js('document.activeElement?.blur(); window.scrollTo(0, 0)');
const marco = [];
for (let i = 0; i < 3; i++) { await tab(); marco.push(await foco()); }
log('Tab en el marco:', marco.join(' → '));
log('aria-current:', await js(`[...document.querySelectorAll('nav a')].map(a => a.textContent.trim() + '=' + a.getAttribute('aria-current')).join(', ')`));
log('Enter sobre', await foco()); await tecla('Enter', 'Enter', 13); await esperar(1200);
log('  después:', await js(`[location.pathname, !!document.querySelector('header'), !!document.querySelector('app-login'), Object.keys(sessionStorage).length]`), 'foco:', await foco());
await js('history.back()'); await esperar(1200);
log('history.back() después de Salir:', await js(`[location.pathname, !!document.querySelector('header'), !!document.querySelector('app-login')]`));

log('\n# Recorrido con teclado por el acceso');
await js('document.activeElement?.blur()');
const acceso = [];
for (let i = 0; i < 4; i++) { await tab(); acceso.push(await foco()); }
log('Tab en el acceso:', acceso.join(' → '));
await js(`document.querySelector('input[name=usuario]').focus()`);
await escribir('admin'); await tab(); await escribir('mala'); await tecla('Enter', 'Enter', 13); await esperar(1200);
log('Enter con contraseña mala → aviso:', await js(`document.querySelector('.aviso-flotante')?.getAttribute('role') + ' · ' + document.querySelector('.aviso-flotante')?.textContent.trim()`), '· foco:', await foco());
await tecla('Escape', 'Escape', 27);
log('Escape: aviso sigue:', await js(`!!document.querySelector('.aviso-flotante')`), '· foco:', await foco());
await js(`document.querySelector('[aria-label="Cerrar aviso"]').focus()`); await tecla('Enter', 'Enter', 13); await esperar(300);
log('Enter sobre Cerrar aviso: aviso sigue:', await js(`!!document.querySelector('.aviso-flotante')`), '· foco:', await foco());

log('\n# CA-002 · sesión vencida en el navegador');
await js(`(() => { const i = document.querySelectorAll('input'); i[1].value = 'buena'; i[1].dispatchEvent(new Event('input'));
  document.querySelector('button[type=submit]').click(); })()`); await esperar(1500);
apiVencida = true;
await js(`(() => { const i = document.querySelector('main input'); i.value = 'filtro'; i.dispatchEvent(new Event('input'));
  i.form.requestSubmit(); })()`); await esperar(1500);
apiVencida = false;
log('después del 401:', await js(`[location.pathname, !!document.querySelector('header'), !!document.querySelector('app-login'), document.querySelector('.aviso-flotante')?.textContent.trim()]`));
await captura('hu-ui-03-1024x768-sesion-vencida');

log('\n# Movimiento reducido');
await cdp('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'reduce' }] });
log('capa de Ingresar:', await js(`getComputedStyle(document.querySelector('button.primario'), '::before').transitionDuration`),
  '· aviso:', await js(`getComputedStyle(document.querySelector('.aviso-flotante')).animationName`));
await cdp('Emulation.setEmulatedMedia', { features: [{ name: 'prefers-reduced-motion', value: 'no-preference' }] });
log('sin reducir: capa:', await js(`getComputedStyle(document.querySelector('button.primario'), '::before').transitionDuration`),
  '· aviso:', await js(`getComputedStyle(document.querySelector('.aviso-flotante')).animationName`));
log('fondo del acceso:', await js(`getComputedStyle(document.querySelector('app-login')).backgroundImage`));
ws.close();
