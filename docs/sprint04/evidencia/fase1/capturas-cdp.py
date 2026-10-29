# Capturas CDP. Arnés: la respuesta del login se intercepta (sesión de prueba); el resto de /api
# responde 503, así que las pantallas muestran su estado de error real, sin datos inventados.
import base64, json, sys, time, urllib.request, websocket
SALIDA, BASE = sys.argv[1], 'http://localhost:4300'
tabs = json.load(urllib.request.urlopen('http://127.0.0.1:9333/json'))
ws = websocket.create_connection(next(t for t in tabs if t['type'] == 'page')['webSocketDebuggerUrl'], suppress_origin=True)
n = 0
def cdp(metodo, **p):
    global n; n += 1; ws.send(json.dumps({'id': n, 'method': metodo, 'params': p}))
    while True:
        m = json.loads(ws.recv())
        if m.get('method') == 'Fetch.requestPaused': atender(m['params'])
        elif m.get('id') == n: return m.get('result', {})
def atender(p):
    url = p['request']['url']
    if url.endswith('/api/acceso/login'):
        cuerpo = json.dumps({'token': 't', 'nombreUsuario': 'un nombre de usuario bastante largo', 'rol': 'ADMINISTRADOR', 'sucursalId': 1})
        ws.send(json.dumps({'id': 0, 'method': 'Fetch.fulfillRequest', 'params': {'requestId': p['requestId'], 'responseCode': 200,
            'responseHeaders': [{'name': 'Content-Type', 'value': 'application/json'}], 'body': base64.b64encode(cuerpo.encode()).decode()}}))
    else:
        ws.send(json.dumps({'id': 0, 'method': 'Fetch.fulfillRequest', 'params': {'requestId': p['requestId'], 'responseCode': 503, 'body': ''}}))
def js(expr): return cdp('Runtime.evaluate', expression=expr, awaitPromise=True, returnByValue=True).get('result', {}).get('value')
def esperar(seg):
    fin = time.time() + seg
    while time.time() < fin: js('1'); time.sleep(.1)
def captura(nombre, clip=None):
    p = {'format': 'png', 'captureBeyondViewport': False}
    if clip: p['clip'] = clip
    open(f'{SALIDA}/{nombre}.png', 'wb').write(base64.b64decode(cdp('Page.captureScreenshot', **p)['data']))
def desborde():
    return js('[document.documentElement.scrollWidth, document.documentElement.clientWidth]')

cdp('Page.enable'); cdp('Runtime.enable'); cdp('DOM.enable'); cdp('CSS.enable')
cdp('Fetch.enable', patterns=[{'urlPattern': '*/api/*'}])
for ancho, alto in [(1024, 768), (768, 1024), (480, 800)]:
    cdp('Emulation.setDeviceMetricsOverride', width=ancho, height=alto, deviceScaleFactor=1, mobile=False)
    cdp('Page.navigate', url=BASE + '/'); esperar(2.5)
    print(ancho, 'login', desborde()); captura(f'fase1-{ancho}x{alto}-login')
    js("""(() => { const i = document.querySelectorAll('input');
        for (const [el, v] of [[i[0], 'admin'], [i[1], 'x']]) { el.value = v; el.dispatchEvent(new Event('input')); }
        document.querySelector('button[type=submit]').click(); })()"""); esperar(2)
    for ruta in ['articulos', 'factura-compra']:
        js(f"document.querySelector('nav a[href=\"/{ruta}\"]').click()"); esperar(1.5)
        print(ancho, ruta, desborde()); captura(f'fase1-{ancho}x{alto}-{ruta}')

# Botones, a 1024 en artículos: reposo, cursor encima (capa completa) y foco de teclado.
cdp('Emulation.setDeviceMetricsOverride', width=1024, height=768, deviceScaleFactor=2, mobile=False)
js("document.querySelector('nav a[href=\"/articulos\"]').click()"); esperar(1)
caja = js("(() => { const r = document.querySelector('form.fila').getBoundingClientRect(); return {x: r.x, y: r.y - 6, width: r.width, height: r.height + 12, scale: 1}; })()")
doc = cdp('DOM.getDocument')['root']['nodeId']
botones = cdp('DOM.querySelectorAll', nodeId=doc, selector='form.fila button')['nodeIds']
captura('fase1-botones-reposo', caja)
for b in botones: cdp('CSS.forcePseudoState', nodeId=b, forcedPseudoClasses=['hover'])
esperar(.6); captura('fase1-botones-cursor-encima', caja)
for b in botones: cdp('CSS.forcePseudoState', nodeId=b, forcedPseudoClasses=['focus', 'focus-visible'])
esperar(.6); captura('fase1-botones-foco', caja)
for b in botones: cdp('CSS.forcePseudoState', nodeId=b, forcedPseudoClasses=[])
regla = "getComputedStyle(document.querySelector('form.fila button'), '::before').transitionDuration"
print('transición de la capa:', js(regla))
cdp('Emulation.setEmulatedMedia', features=[{'name': 'prefers-reduced-motion', 'value': 'reduce'}])
print('con movimiento reducido:', js(regla))
# Fuentes: load() pide el archivo; check() es true solo si la cara está cargada. Control negativo:
# una cara que apunta a un archivo inexistente tiene que dar error, si no la medición no discrimina.
js("""(() => { const c = document.createElement('span'); c.className = 'codigo'; c.textContent = 'LC-000123';
    document.querySelector('main').prepend(c); })()""")
fuentes = ['500 19px "Barlow Semi Condensed"', '600 19px "Barlow Semi Condensed"', '700 30px "Barlow Semi Condensed"',
           'italic 800 30px "Barlow Semi Condensed"', '400 14px "Noto Sans"', '600 14px "Noto Sans"',
           '400 12px "JetBrains Mono"', '500 12px "JetBrains Mono"']
for f in fuentes:
    print('fuente', f, js(f"document.fonts.load('{f}').then(c => [c.length, c.map(x => x.status).join(','), document.fonts.check('{f}')])"))
print('chip de código:', js("(() => { const c = document.querySelector('main .codigo'); return [getComputedStyle(c).fontFamily, document.fonts.check('12px ' + getComputedStyle(c).fontFamily.split(',')[0])]; })()"))
print('control negativo:', js("""(() => { const f = new FontFace('Fuente Inexistente', "url('/fonts/no-existe.woff2')"); document.fonts.add(f);
    return f.load().then(() => 'cargó (MAL)', () => 'error: ' + f.status).then(r => [r, document.fonts.check('12px "Fuente Inexistente"')]); })()"""))
print('fuentes cargadas:', js("document.fonts.ready.then(() => [...document.fonts].filter(f => f.status === 'loaded').map(f => f.family + ' ' + f.weight + ' ' + f.style))"))
