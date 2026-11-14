import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ComponentFixture } from '@angular/core/testing';
import { appConfig } from '../../app.config';
import { SesionActual } from '../../nucleo/sesion';
import { FacturaCompra } from './factura-compra';

/** HU-FCO-06 (056) · la carga de factura de compra con la mecánica del negocio. */
describe('FacturaCompra · HU-FCO-06', () => {
  let fixture: ComponentFixture<FacturaCompra>;
  let servidor: HttpTestingController;
  let c: FacturaCompra;
  const raiz = () => fixture.nativeElement as HTMLElement;

  const proveedor = (codigo: string, tipo: string | null = null, id = 7) =>
    ({ id, codigo, razonSocial: `Proveedor ${codigo}`, cuit: null, urlReferencia: null, activo: true, tipoComprobanteHabitual: tipo });
  const articulo = (id: number, codigo: string) => ({ id, codigoInterno: codigo, nombre: `Artículo ${codigo}`, activo: true, codigosProveedor: [] });
  const pagina = (...a: unknown[]) => ({ articulos: a, total: a.length, pagina: 1, tamanoPagina: 10, conflictos: [] });
  const ficha = (id: number, costo: number | null) => ({
    id, codigoInterno: `A${id}`, nombre: `Artículo ${id}`, activo: true, costoVigente: costo, precios: [], codigosProveedor: [],
    existencias: [{ sucursalId: 1, sucursal: 'Fragueiro', cantidad: 0 }, { sucursalId: 2, sucursal: 'San Vicente', cantidad: 0 }],
  });

  beforeEach(() => {
    // Con los providers de producción: el total se lee en es-AR.
    TestBed.configureTestingModule({ imports: [FacturaCompra], providers: [...appConfig.providers, provideHttpClientTesting()] });
    TestBed.inject(SesionActual).sesion.set({ token: 't', nombreUsuario: 'u', rol: 'OPERADOR', sucursalId: 1 });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(FacturaCompra);
    c = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  function elegirProveedor(p: ReturnType<typeof proveedor>) {
    c.textoProveedor = p.codigo;
    c.enterProveedor();
    servidor.expectOne(r => r.url === '/api/proveedores').flush({ items: [p], total: 1, pagina: 1, tamanoPagina: 20 });
    fixture.detectChanges();
  }

  function agregar(id: number, codigo: string, costo: number | null = null) {
    c.textoArticulo = codigo;
    c.enterArticulo();
    servidor.expectOne(r => r.url === '/api/articulos').flush(pagina(articulo(id, codigo)));
    servidor.expectOne(`/api/articulos/${id}`).flush(ficha(id, costo));
    fixture.detectChanges();
  }

  // ---- CA-001 a CA-003 · proveedor ----

  it('CA-001: busca proveedores por código y nombre, sin CUIT', () => {
    c.textoProveedor = 'lubricantes';
    c.buscarProveedor();
    const pedido = servidor.expectOne(r => r.url === '/api/proveedores');
    expect(pedido.request.params.get('porCuit')).toBe('false');
    pedido.flush({ items: [], total: 0, pagina: 1, tamanoPagina: 20 });
  });

  it('CA-002 y CA-003: Enter sobre 35 selecciona el 00035 directamente, sin listar', () => {
    c.textoProveedor = '35';
    c.enterProveedor();
    servidor.expectOne(r => r.url === '/api/proveedores')
      .flush({ items: [proveedor('00035'), proveedor('350', null, 8)], total: 2, pagina: 1, tamanoPagina: 20 });
    fixture.detectChanges();

    expect(c.proveedor()?.codigo).toBe('00035');
    expect(raiz().querySelectorAll('button.opcion').length).toBe(0);
    expect(c.proveedores().length).toBe(0);
  });

  it('Enter sin un código exacto lista las coincidencias y no elige ninguna', () => {
    c.textoProveedor = 'frenos';
    c.enterProveedor();
    servidor.expectOne(r => r.url === '/api/proveedores')
      .flush({ items: [proveedor('350', null, 8)], total: 1, pagina: 1, tamanoPagina: 20 });
    fixture.detectChanges();

    expect(c.proveedor()).toBeNull();
    expect(raiz().querySelectorAll('button.opcion').length).toBe(1);
  });

  it('CA-002 y CA-003: Enter con dos códigos exactos sin ceros (035 y 00035) lista los dos y no elige ninguno', () => {
    c.textoProveedor = '35';
    c.enterProveedor();
    servidor.expectOne(r => r.url === '/api/proveedores')
      .flush({ items: [proveedor('035'), proveedor('00035', null, 8)], total: 2, pagina: 1, tamanoPagina: 20 });
    fixture.detectChanges();

    expect(c.proveedor()).toBeNull();
    expect(raiz().querySelectorAll('button.opcion').length).toBe(2);
  });

  // ---- CA-004 y CA-005 · tipo de comprobante y número ----

  it('CA-004 y CA-005: el proveedor que suele emitir B propone B y precarga 99 en el número', () => {
    elegirProveedor(proveedor('00035', 'B'));

    expect(c.tipoComprobante).toBe('B');
    expect(c.numero).toBe('99-');
  });

  it('CA-004: el tipo propuesto se puede cambiar; al pasar a A no queda el 99 precargado', () => {
    elegirProveedor(proveedor('00035', 'B'));

    c.cambiarTipo('A');

    expect(c.tipoComprobante).toBe('A');
    expect(c.numero).toBe('');
  });

  it('CA-005: con tipo A no se precarga nada', () => {
    elegirProveedor(proveedor('00035', 'A'));

    expect(c.tipoComprobante).toBe('A');
    expect(c.numero).toBe('');
  });

  it('CA-005: al pasar a B a mano se precarga el 99 sin pisar lo que ya empieza con 99', () => {
    elegirProveedor(proveedor('00035'));
    c.cambiarTipo('B');
    expect(c.numero).toBe('99-');

    c.numero = '99-00001234';
    c.cambiarTipo('B');
    expect(c.numero).toBe('99-00001234');
  });

  // ---- CA-008 y CA-009 · artículos ----

  it('CA-008: con tres caracteres o más busca aproximado mientras se escribe', fakeAsync(() => {
    c.escribirArticulo('fi');
    tick(300);
    servidor.expectNone(r => r.url === '/api/articulos');

    c.escribirArticulo('fil');
    tick(300);
    const pedido = servidor.expectOne(r => r.url === '/api/articulos');
    expect(pedido.request.params.get('aproximada')).toBe('true');
    expect(pedido.request.params.get('texto')).toBe('fil');
    pedido.flush(pagina(articulo(1, 'FIL-0712')));
    fixture.detectChanges();

    expect(raiz().querySelectorAll('button.opcion').length).toBe(1);
  }));

  it('CA-008 y CA-009: Enter busca también aproximado; el botón Buscar, no', () => {
    c.textoArticulo = 'fil';
    c.enterArticulo();
    const enter = servidor.expectOne(r => r.url === '/api/articulos');
    expect(enter.request.params.get('aproximada')).toBe('true');
    enter.flush(pagina());

    c.buscarArticulo();
    const boton = servidor.expectOne(r => r.url === '/api/articulos');
    expect(boton.request.params.has('aproximada')).toBeFalse();
    boton.flush(pagina());
  });

  it('CA-009: Enter sobre el código exacto agrega la línea sin listar', () => {
    agregar(1, 'FIL-0712');

    expect(c.lineas.map(l => l.articulo.id)).toEqual([1]);
    expect(raiz().querySelectorAll('button.opcion').length).toBe(0);
  });

  // ---- CA-010 · reemplazar el artículo ----

  it('CA-010: reemplazar cambia el artículo de la línea sin borrarla', () => {
    agregar(1, 'FIL-0712');
    c.lineas[0].cantidad = 4;

    c.reemplazar(0);
    c.textoArticulo = 'PAS-0100';
    c.enterArticulo();
    servidor.expectOne(r => r.url === '/api/articulos').flush(pagina(articulo(2, 'PAS-0100')));
    servidor.expectOne('/api/articulos/2').flush(ficha(2, null));
    fixture.detectChanges();

    expect(c.lineas.length).toBe(1);
    expect(c.lineas[0].articulo.id).toBe(2);
    expect(c.lineas[0].cantidad).toBe(4);
  });

  it('CA-010: reemplazar una línea por su mismo artículo no la rechaza como repetida', () => {
    agregar(1, 'FIL-0712');
    c.lineas[0].cantidad = 4;

    c.reemplazar(0);
    c.textoArticulo = 'FIL-0712';
    c.enterArticulo();
    servidor.expectOne(r => r.url === '/api/articulos').flush(pagina(articulo(1, 'FIL-0712')));

    expect(c.errorArticulo()).toBeNull();
    expect(c.reemplazando()).toBeNull();
    servidor.expectOne('/api/articulos/1').flush(ficha(1, null));
    expect(c.lineas.length).toBe(1);
    expect(c.lineas[0].cantidad).toBe(4);
  });

  // ---- CA-011 y CA-013 · precio propuesto y sucursal heredada ----

  it('CA-011: propone el costo vigente como precio, y el valor que escribo es el que va', () => {
    agregar(1, 'FIL-0712', 1500);
    expect(c.lineas[0].precioUnitario).toBe(1500);
  });

  it('CA-011: sin costo conocido no propone nada (nunca cero)', () => {
    agregar(1, 'FIL-0712', null);
    expect(c.lineas[0].precioUnitario).toBeNull();
  });

  it('CA-011: si escribo el precio antes de que llegue la ficha, el costo propuesto no lo pisa', () => {
    c.textoArticulo = 'FIL-0712';
    c.enterArticulo();
    servidor.expectOne(r => r.url === '/api/articulos').flush(pagina(articulo(1, 'FIL-0712')));
    c.lineas[0].precioUnitario = 999;
    servidor.expectOne('/api/articulos/1').flush(ficha(1, 1500));

    expect(c.lineas[0].precioUnitario).toBe(999);
  });

  it('CA-008 y CA-009: la búsqueda aproximada que llega después de elegir por Enter no vuelve a listar', fakeAsync(() => {
    c.escribirArticulo('FIL-0712');
    c.enterArticulo();
    servidor.expectOne(r => r.url === '/api/articulos').flush(pagina(articulo(1, 'FIL-0712')));
    servidor.expectOne('/api/articulos/1').flush(ficha(1, null));

    tick(300);
    servidor.expectOne(r => r.url === '/api/articulos').flush(pagina(articulo(1, 'FIL-0712'), articulo(2, 'FIL-07120')));
    fixture.detectChanges();

    expect(c.lineas.length).toBe(1);
    expect(raiz().querySelectorAll('button.opcion').length).toBe(0);
  }));

    it('CA-013: la línea nueva hereda la sucursal de la cabecera; cambiarla no toca las demás', () => {
    agregar(1, 'FIL-0712');
    expect(c.lineas[0].sucursalId).toBe(1);

    c.sucursalCabecera = 2;
    agregar(2, 'PAS-0100');

    expect(c.lineas.map(l => l.sucursalId)).toEqual([1, 2]);
  });

  // ---- CA-012, CA-014 y CA-015 · total antes de confirmar ----

  function cargarFactura() {
    elegirProveedor(proveedor('00035', 'B'));
    c.numero = '99-00001234';
    c.fechaEmision = '2026-11-10';
    c.fechaImputacion = '2026-11-12';
    c.percepciones = { iva: 10, iibb: 2.5, municipal: 0, ganancias: 0 };
    agregar(1, 'FIL-0712', 100);
    Object.assign(c.lineas[0], { cantidad: 2, descuentoPorcentaje: 10, descuento2Porcentaje: 5, descuento3Porcentaje: 2.5 });
  }

  it('CA-015: antes de guardar pide el total, lo muestra y no registra nada', () => {
    cargarFactura();

    c.registrar();
    const calculo = servidor.expectOne('/api/facturas-compra/calculo');
    expect(calculo.request.body).toEqual(jasmine.objectContaining({ tipoComprobante: 'B', fechaImputacion: '2026-11-12' }));
    expect(calculo.request.body.lineas[0]).toEqual(jasmine.objectContaining(
      { descuentoPorcentaje: 10, descuento2Porcentaje: 5, descuento3Porcentaje: 2.5 }));
    calculo.flush({ neto: 166.73, iva: 0, percepciones: 12.5, total: 179.23 });
    fixture.detectChanges();

    servidor.expectNone('/api/facturas-compra');
    const totales = raiz().querySelector('[data-totales]')!;
    expect(totales).not.toBeNull();
    expect(totales.textContent).toContain('179,23');
    expect(totales.textContent).toContain('12,50');
  });

  it('CA-012 y CA-014: confirmar registra con el tipo, la imputación y los tres descuentos', () => {
    cargarFactura();
    c.registrar();
    servidor.expectOne('/api/facturas-compra/calculo').flush({ neto: 166.73, iva: 0, percepciones: 12.5, total: 179.23 });
    fixture.detectChanges();

    c.confirmar();
    const registro = servidor.expectOne('/api/facturas-compra');
    expect(registro.request.method).toBe('POST');
    expect(registro.request.body).toEqual(jasmine.objectContaining({ tipoComprobante: 'B', numero: '99-00001234', fechaImputacion: '2026-11-12' }));
    expect(registro.request.body.lineas[0]).toEqual(jasmine.objectContaining(
      { precioUnitario: 100, descuentoPorcentaje: 10, descuento2Porcentaje: 5, descuento3Porcentaje: 2.5, sucursalId: 1 }));
    registro.flush({ id: 9 });
    servidor.expectOne('/api/articulos/1').flush(ficha(1, 85.5));
  });

  it('CA-012: sin vencimiento ni imputación cargados, se mandan como null y no como texto vacío', () => {
    cargarFactura();
    c.fechaVencimiento = '';
    c.fechaImputacion = '';

    c.registrar();
    const calculo = servidor.expectOne('/api/facturas-compra/calculo');
    expect(calculo.request.body.fechaVencimiento).toBeNull();
    expect(calculo.request.body.fechaImputacion).toBeNull();
    calculo.flush({ neto: 166.73, iva: 0, percepciones: 12.5, total: 179.23 });
  });

  it('CA-015: si cambio algo después de ver el total, hay que volver a calcularlo', () => {
    cargarFactura();
    c.registrar();
    servidor.expectOne('/api/facturas-compra/calculo').flush({ neto: 166.73, iva: 0, percepciones: 12.5, total: 179.23 });
    fixture.detectChanges();

    c.invalidarTotales();
    fixture.detectChanges();

    expect(raiz().querySelector('[data-totales]')).toBeNull();
  });

  it('CA-004 y CA-015: elegir otro proveedor cambia el tipo propuesto y obliga a recalcular el total', () => {
    cargarFactura();
    c.registrar();
    servidor.expectOne('/api/facturas-compra/calculo').flush({ neto: 166.73, iva: 0, percepciones: 12.5, total: 179.23 });
    fixture.detectChanges();

    // Elegir el proveedor (Enter o click en la opción) no dispara el (change) del formulario: solo cambiarTipo() borra el total.
    elegirProveedor(proveedor('00040', 'A', 9));

    expect(c.tipoComprobante).toBe('A');
    expect(raiz().querySelector('[data-totales]')).toBeNull();
  });

  function conTotal() {
    cargarFactura();
    c.registrar();
    servidor.expectOne('/api/facturas-compra/calculo').flush({ neto: 166.73, iva: 0, percepciones: 12.5, total: 179.23 });
    fixture.detectChanges();
    expect(raiz().querySelector('[data-totales]')).not.toBeNull();
  }

  it('CA-015: escribir en un campo del formulario después de ver el total lo borra', () => {
    conTotal();

    raiz().querySelector<HTMLInputElement>('input[name="numero"]')!.dispatchEvent(new Event('input', { bubbles: true }));
    fixture.detectChanges();

    expect(raiz().querySelector('[data-totales]')).toBeNull();
  });

  it('CA-015: quitar una línea después de ver el total lo borra', () => {
    conTotal();

    const quitar = Array.from(raiz().querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.trim() === 'Quitar')!;
    quitar.click();
    fixture.detectChanges();

    expect(c.lineas.length).toBe(0);
    expect(raiz().querySelector('[data-totales]')).toBeNull();
  });

  it('CA-015: agregar un artículo después de ver el total lo borra', () => {
    conTotal();

    agregar(2, 'PAS-0100');

    expect(c.lineas.length).toBe(2);
    expect(raiz().querySelector('[data-totales]')).toBeNull();
  });
});
