import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ArticuloResultado, ProveedorResumen } from '../../nucleo/api';
import { NotaCreditoCompra } from './nota-credito-compra';

/** HU-FCO-07 (057) · carga manual de la nota de crédito de proveedor. */
describe('NotaCreditoCompra · 057', () => {
  const expoyer: ProveedorResumen = { id: 3, codigo: '00003', razonSocial: 'Expoyer S.A.', cuit: '30-58044645-7', activo: true };
  const articulo: ArticuloResultado = { id: 100, codigoInterno: 'A-100', nombre: 'Bomba de agua', activo: true, codigosProveedor: [] };
  const factura = (id: number, numero: string, anulada = false) => ({
    id, proveedorId: 3, proveedor: 'Expoyer S.A.', numero, tipoComprobante: 'A', fechaEmision: '2026-10-27', fechaVencimiento: null, anulada,
  });

  let fixture: ComponentFixture<NotaCreditoCompra>;
  let servidor: HttpTestingController;
  let raiz: HTMLElement;
  const c = () => fixture.componentInstance;
  const boton = (nombre: string) => raiz.querySelector(`button[name="${nombre}"]`) as HTMLButtonElement | null;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [NotaCreditoCompra], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(NotaCreditoCompra);
    raiz = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  /** Elige a Expoyer y responde sus facturas: dos activas y una anulada. */
  function conProveedorYFacturas() {
    c().elegirProveedor(expoyer);
    servidor.expectOne(r => r.url === '/api/facturas-compra' && r.params.get('proveedorId') === '3')
      .flush({ items: [factura(501, '0011-00044774'), factura(502, '0011-00044869'), factura(503, '0011-00000001', true)], total: 3, pagina: 1, tamanoPagina: 100 });
    fixture.detectChanges();
  }

  function cabecera() {
    c().numero = ' 0010-00021495 ';
    c().fechaEmision = '2026-11-05';
    c().importeNeto = 115040;
  }

  function marcar(numero: string) {
    const etiqueta = [...raiz.querySelectorAll('label')].find(l => l.textContent?.includes(numero));
    const casilla = etiqueta?.querySelector('input[type="checkbox"]') as HTMLInputElement | null;
    expect(casilla).withContext(`casilla de la factura ${numero}`).not.toBeNull();
    casilla?.click();
    fixture.detectChanges();
  }

  it('CA-003: no se elige el tipo y el número no se precarga', () => {
    expect(raiz.querySelector('select[name="tipo"]')).toBeNull();
    const numero = raiz.querySelector('input[name="numero"]') as HTMLInputElement | null;
    expect(numero).not.toBeNull();
    expect(numero?.value).toBe('');
    expect(raiz.textContent).toContain('siempre tipo A');
  });

  it('busca proveedores y, sin coincidencias, dice que no hay', () => {
    c().textoProveedor = 'zzz';
    c().buscarProveedor();
    servidor.expectOne(r => r.url === '/api/proveedores').flush({ items: [] });
    fixture.detectChanges();

    expect(raiz.querySelector('p.error')?.textContent).toContain('No hay proveedores activos para «zzz».');
  });

  it('busca proveedores y lista los que encuentra', () => {
    c().textoProveedor = 'expo';
    c().buscarProveedor();
    servidor.expectOne(r => r.url === '/api/proveedores').flush({ items: [expoyer] });
    fixture.detectChanges();

    expect(raiz.querySelector('p.error')).toBeNull();
    expect(raiz.textContent).toContain('Expoyer S.A.');
  });

  it('CA-001: al elegir el proveedor lista solo sus facturas activas', () => {
    conProveedorYFacturas();

    expect(raiz.textContent).toContain('0011-00044774');
    expect(raiz.textContent).toContain('0011-00044869');
    expect(raiz.textContent).not.toContain('0011-00000001');
  });

  it('CA-001: si el proveedor no tiene facturas activas, avisa que la nota no se registra', () => {
    c().elegirProveedor(expoyer);
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush({ items: [factura(503, '0011-00000001', true)], total: 1, pagina: 1, tamanoPagina: 100 });
    fixture.detectChanges();

    expect(raiz.querySelector('p.vacio')?.textContent).toContain('sin al menos una');
  });

  it('CA-001: sin una factura marcada no se puede calcular', () => {
    conProveedorYFacturas();
    cabecera();
    fixture.detectChanges();

    expect(boton('calcular')).not.toBeNull();
    expect(boton('calcular')?.disabled).toBeTrue();
  });

  it('CA-002, CA-003 y CA-004: una bonificación sobre dos facturas manda las dos, el número tal cual y sin líneas', () => {
    conProveedorYFacturas();
    cabecera();
    c().percepciones.iibb = 12.5;
    marcar('0011-00044774');
    marcar('0011-00044869');
    fixture.detectChanges();
    boton('calcular')?.click();

    const pedido = servidor.expectOne('/api/notas-credito-compra/calculo');
    expect(pedido.request.method).toBe('POST');
    expect(pedido.request.body).toEqual({
      proveedorId: 3, numero: '0010-00021495', fechaEmision: '2026-11-05', clase: 'BONIFICACION', importeNeto: 115040,
      percepcionIva: null, percepcionIibb: 12.5, percepcionMunicipal: null, percepcionGanancias: null,
      facturaIds: [501, 502], lineas: [],
    });
    pedido.flush({ neto: 115040, iva: 24158.4, percepciones: 12.5, total: 139210.9 });
  });

  it('CA-002: desmarcar una factura la saca de la nota', () => {
    conProveedorYFacturas();
    cabecera();
    marcar('0011-00044774');
    marcar('0011-00044869');
    marcar('0011-00044774');
    boton('calcular')?.click();

    expect(servidor.expectOne('/api/notas-credito-compra/calculo').request.body.facturaIds).toEqual([502]);
  });

  /** Deja la bonificación lista con el total calculado. */
  function conTotal() {
    conProveedorYFacturas();
    cabecera();
    marcar('0011-00044774');
    boton('calcular')?.click();
    servidor.expectOne('/api/notas-credito-compra/calculo').flush({ neto: 115040, iva: 24158.4, percepciones: 0, total: 139198.4 });
    fixture.detectChanges();
  }

  it('CA-007: antes de calcular no se puede registrar; con el total, sí, y se muestra', () => {
    conProveedorYFacturas();
    cabecera();
    marcar('0011-00044774');
    expect(boton('registrar')?.disabled).withContext('sin total').toBeTrue();

    boton('calcular')?.click();
    servidor.expectOne('/api/notas-credito-compra/calculo').flush({ neto: 115040, iva: 24158.4, percepciones: 0, total: 139198.4 });
    fixture.detectChanges();

    expect(raiz.querySelector('[data-totales]')).not.toBeNull();
    expect(boton('registrar')?.disabled).withContext('con total').toBeFalse();
  });

  it('CA-007: cambiar algo después de calcular borra el total y hay que recalcular', () => {
    conTotal();
    const numero = raiz.querySelector('input[name="numero"]') as HTMLInputElement;
    numero.value = '0010-00021496';
    numero.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();

    expect(raiz.querySelector('[data-totales]')).toBeNull();
    expect(boton('registrar')?.disabled).toBeTrue();
  });

  it('registrar manda la nota, deshabilita el botón mientras tanto y muestra que quedó registrada', () => {
    conTotal();
    boton('registrar')?.click();
    fixture.detectChanges();
    expect(boton('registrar')?.disabled).withContext('mientras registra').toBeTrue();

    const pedido = servidor.expectOne(r => r.url === '/api/notas-credito-compra' && r.method === 'POST');
    expect(pedido.request.body.facturaIds).toEqual([501]);
    pedido.flush({ id: 9 });
    fixture.detectChanges();

    expect(raiz.querySelector('h2')?.textContent).toContain('Nota de crédito 0010-00021495 registrada');
  });

  it('un error de la API al registrar se muestra y se puede reintentar', () => {
    conTotal();
    boton('registrar')?.click();
    servidor.expectOne(r => r.url === '/api/notas-credito-compra' && r.method === 'POST')
      .flush({ detail: 'El proveedor Expoyer S.A. ya tiene la nota de crédito 0010-00021495.' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    expect(raiz.querySelector('p.error')?.textContent).toContain('ya tiene la nota de crédito');
    expect(boton('registrar')?.disabled).toBeFalse();
  });

  it('cargar otra nota vuelve al formulario vacío', () => {
    conTotal();
    boton('registrar')?.click();
    servidor.expectOne(r => r.url === '/api/notas-credito-compra' && r.method === 'POST').flush({ id: 9 });
    fixture.detectChanges();
    ([...raiz.querySelectorAll('button')].find(b => b.textContent?.includes('Cargar otra')) as HTMLButtonElement | undefined)?.click();
    fixture.detectChanges();

    expect((raiz.querySelector('input[name="numero"]') as HTMLInputElement | null)?.value).toBe('');
    expect(c().proveedor()).toBeNull();
    expect(c().importeNeto).toBeNull();
  });

  /** Pasa a devolución y agrega el artículo 100, con los orígenes de su ficha (FR, SV y Reclamos). */
  function devolucionConArticulo() {
    conProveedorYFacturas();
    cabecera();
    marcar('0011-00044774');
    c().clase = 'DEVOLUCION';
    fixture.detectChanges();
    c().textoArticulo = 'bomba';
    c().buscarArticulo();
    servidor.expectOne(r => r.url === '/api/articulos' && r.params.get('texto') === 'bomba')
      .flush({ articulos: [articulo], total: 1, pagina: 1, tamanoPagina: 20, conflictos: [] });
    fixture.detectChanges();
    ([...raiz.querySelectorAll('button.opcion')].find(b => b.textContent?.includes('Bomba de agua')) as HTMLButtonElement | undefined)?.click();
    servidor.expectOne('/api/articulos/100').flush({
      id: 100, codigoInterno: 'A-100', nombre: 'Bomba de agua', activo: true, costoVigente: 10, ultimoProveedorId: 3, codigos: [],
      existencias: [
        { sucursalId: 1, sucursal: 'Fragueiro (Casa Central)', cantidad: 4, ubicacionDeposito: null },
        { sucursalId: 2, sucursal: 'San Vicente (Sucursal)', cantidad: 0, ubicacionDeposito: null },
        { sucursalId: 3, sucursal: 'Reclamos (depósito)', cantidad: 1, ubicacionDeposito: null },
      ],
      totalExistencias: 5, precios: [], motivoBaja: null,
    });
    fixture.detectChanges();
  }

  it('CA-004: la bonificación no muestra artículos', () => {
    conProveedorYFacturas();
    expect(raiz.textContent).not.toContain('Mercadería devuelta');
  });

  it('CA-005: la devolución ofrece como origen las sucursales y Reclamos', () => {
    devolucionConArticulo();

    const opciones = [...raiz.querySelectorAll('select[aria-label="Sale de"] option')].map(o => o.textContent?.trim());
    expect(opciones).toContain('Reclamos (depósito)');
    expect(opciones).toContain('Fragueiro (Casa Central)');
  });

  it('CA-005: sin origen elegido en una línea no se puede calcular', () => {
    devolucionConArticulo();

    expect(boton('calcular')?.disabled).toBeTrue();
  });

  it('CA-005: la devolución manda cada artículo con su cantidad y el origen elegido', () => {
    devolucionConArticulo();
    c().lineas[0].sucursalId = 3;
    c().lineas[0].cantidad = 2;
    fixture.detectChanges();
    boton('calcular')?.click();

    const cuerpo = servidor.expectOne('/api/notas-credito-compra/calculo').request.body;
    expect(cuerpo.clase).toBe('DEVOLUCION');
    expect(cuerpo.lineas).toEqual([{ articuloId: 100, sucursalId: 3, cantidad: 2 }]);
  });

  it('CA-005: quitar la única línea deja la devolución sin poder calcular', () => {
    devolucionConArticulo();
    c().lineas[0].sucursalId = 1;
    ([...raiz.querySelectorAll('button')].find(b => b.textContent?.trim() === 'Quitar') as HTMLButtonElement | undefined)?.click();
    fixture.detectChanges();

    expect(c().lineas.length).toBe(0);
    expect(boton('calcular')?.disabled).toBeTrue();
  });

  it('CA-004: al volver de devolución a bonificación, las líneas no viajan', () => {
    devolucionConArticulo();
    c().lineas[0].sucursalId = 1;
    c().clase = 'BONIFICACION';
    fixture.detectChanges();
    boton('calcular')?.click();

    expect(servidor.expectOne('/api/notas-credito-compra/calculo').request.body.lineas).toEqual([]);
  });

  // ---- CA-013 (D-S6-24): aviso de stock negativo antes de confirmar ----

  function calcularDevolucion(origen: number, cantidad: number) {
    devolucionConArticulo();
    c().lineas[0].sucursalId = origen;
    c().lineas[0].cantidad = cantidad;
    fixture.detectChanges();
    boton('calcular')?.click();
    servidor.expectOne('/api/notas-credito-compra/calculo').flush({ neto: 115040, iva: 24158.4, percepciones: 0, total: 139198.4 });
    fixture.detectChanges();
  }

  const aviso = () => raiz.querySelector('[data-aviso-stock]');

  it('CA-013: si la devolución deja el origen en negativo, avisa qué artículo, dónde y cuánto queda, y se puede registrar igual', () => {
    calcularDevolucion(1, 6);

    expect(aviso()?.textContent).toContain('Bomba de agua');
    expect(aviso()?.textContent).toContain('Fragueiro (Casa Central)');
    expect(aviso()?.textContent).toContain('-2');
    expect(boton('registrar')?.disabled).toBeFalse();
  });

  it('CA-013: si el origen queda en cero o más, no avisa', () => {
    calcularDevolucion(1, 4);

    expect(aviso()).toBeNull();
  });

  it('CA-013: el aviso usa las existencias del origen elegido', () => {
    calcularDevolucion(3, 2);

    expect(aviso()?.textContent).toContain('Reclamos (depósito)');
    expect(aviso()?.textContent).toContain('-1');
  });

  it('CA-013: cada artículo agregado trae su ficha, para saber su stock', () => {
    devolucionConArticulo();
    c().agregarArticulo({ ...articulo, id: 200, codigoInterno: 'A-200', nombre: 'Correa' });

    servidor.expectOne('/api/articulos/200');
  });
});
