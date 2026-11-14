import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FacturaCompraResumen } from '../../nucleo/api';
import { ComprasRegistradas } from './compras-registradas';

/** HU-FCO-02 (026): consulta de facturas de compra, por el camino real hasta la API. */
describe('ComprasRegistradas · HU-FCO-02', () => {
  let fixture: ComponentFixture<ComprasRegistradas>;
  let c: ComprasRegistradas;
  let servidor: HttpTestingController;
  const raiz = () => fixture.nativeElement as HTMLElement;

  const factura = (id: number, numero: string, anulada = false): FacturaCompraResumen => ({
    id, proveedorId: 7, proveedor: 'Lubricantes del Sur', numero, tipoComprobante: 'A',
    fechaEmision: '2026-10-01', fechaVencimiento: '2026-10-31', anulada,
  });
  const pagina = (items: FacturaCompraResumen[], total = items.length, numero = 1) =>
    ({ items, total, pagina: numero, tamanoPagina: 20 });

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ComprasRegistradas], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ComprasRegistradas);
    c = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  function buscar() {
    raiz().querySelector<HTMLFormElement>('form.filtros')!.dispatchEvent(new Event('submit'));
  }

  function elegirProveedor() {
    c.textoProveedor = 'lubri';
    c.buscarProveedor();
    servidor.expectOne(r => r.url === '/api/proveedores').flush({
      items: [{ id: 7, codigo: '00035', razonSocial: 'Lubricantes del Sur', cuit: null, urlReferencia: null, activo: true, tipoComprobanteHabitual: null }],
      total: 1, pagina: 1, tamanoPagina: 20,
    });
    fixture.detectChanges();
    Array.from(raiz().querySelectorAll<HTMLButtonElement>('button.opcion')).find(b => b.textContent?.includes('Lubricantes'))!.click();
    fixture.detectChanges();
  }

  // CA-001: número y proveedor.
  it('CA-001: busca por número y proveedor, y solo manda los criterios con valor', () => {
    elegirProveedor();
    c.numero = ' A-0001-00000001 ';
    buscar();

    const pedido = servidor.expectOne(r => r.url === '/api/facturas-compra');
    expect(pedido.request.params.get('proveedorId')).toBe('7');
    expect(pedido.request.params.get('numero')).toBe('A-0001-00000001');
    expect(pedido.request.params.get('pagina')).toBe('1');
    expect(pedido.request.params.get('tamanoPagina')).toBe('20');
    expect(pedido.request.params.has('emisionDesde')).toBeFalse();
    expect(pedido.request.params.has('vencimientoHasta')).toBeFalse();
    pedido.flush(pagina([factura(1, 'A-0001-00000001')]));
    fixture.detectChanges();

    const filas = raiz().querySelectorAll('tbody tr');
    expect(filas.length).toBe(1);
    expect(filas[0].textContent).toContain('A-0001-00000001');
    expect(filas[0].textContent).toContain('Lubricantes del Sur');
  });

  // CA-002: por proveedor, la lista completa.
  it('CA-002: por proveedor muestra todas sus facturas', () => {
    elegirProveedor();
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra' && r.params.get('proveedorId') === '7' && !r.params.has('numero'))
      .flush(pagina([factura(1, 'A-1'), factura(2, 'A-2'), factura(3, 'B-3')]));
    fixture.detectChanges();

    expect(raiz().querySelectorAll('tbody tr').length).toBe(3);
  });

  it('CA-002: quitar el proveedor elegido deja de filtrar por él', () => {
    elegirProveedor();
    Array.from(raiz().querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.includes('Quitar'))!.click();
    fixture.detectChanges();
    buscar();

    servidor.expectOne(r => r.url === '/api/facturas-compra' && !r.params.has('proveedorId')).flush(pagina([]));
  });

  // CA-003: rangos de emisión y de vencimiento.
  it('CA-003: manda los rangos de emisión y de vencimiento', () => {
    c.emisionDesde = '2026-10-01';
    c.emisionHasta = '2026-10-31';
    c.vencimientoDesde = '2026-11-01';
    c.vencimientoHasta = '2026-11-30';
    buscar();

    const pedido = servidor.expectOne(r => r.url === '/api/facturas-compra');
    expect(['emisionDesde', 'emisionHasta', 'vencimientoDesde', 'vencimientoHasta'].map(p => pedido.request.params.get(p)))
      .toEqual(['2026-10-01', '2026-10-31', '2026-11-01', '2026-11-30']);
    pedido.flush(pagina([]));
  });

  // CA-004: sin coincidencias.
  it('CA-004: sin coincidencias muestra «Sin resultados»', () => {
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([]));
    fixture.detectChanges();

    expect(raiz().querySelector('[role="status"]')?.textContent).toContain('Sin resultados');
    expect(raiz().querySelector('tbody')).toBeNull();
  });

  // CA-005: las anuladas, marcadas.
  it('CA-005: las anuladas aparecen marcadas como anuladas', () => {
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([factura(1, 'A-1'), factura(2, 'A-2', true)]));
    fixture.detectChanges();

    const [activa, anulada] = Array.from(raiz().querySelectorAll('tbody tr'));
    expect(anulada.classList).toContain('anulada');
    expect(anulada.textContent).toContain('Anulada');
    expect(activa.classList).not.toContain('anulada');
    expect(activa.textContent).not.toContain('Anulada');
  });

  it('el error del servidor se muestra y no deja la tabla anterior', () => {
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([factura(1, 'A-1')]));
    fixture.detectChanges();
    expect(raiz().querySelector('tbody')).not.toBeNull();

    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra')
      .flush({ detail: 'El rango de emisión empieza después de terminar.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(raiz().querySelector('[role="alert"]')?.textContent).toContain('El rango de emisión empieza después de terminar.');
    expect(raiz().querySelector('tbody')).toBeNull();

    // Verificador ronda 2: al volver a buscar, mientras el pedido está en vuelo, no reaparece la tabla vieja.
    buscar();
    fixture.detectChanges();
    expect(raiz().querySelector('tbody')).toBeNull();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([]));
  });

  // Verificador ronda 1: los extremos de la paginación.
  it('pagina: en la primera no se puede ir atrás y en la última no se puede ir adelante', () => {
    const boton = (texto: string) => Array.from(raiz().querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.includes(texto))!;
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([factura(1, 'A-1')], 21));
    fixture.detectChanges();
    expect([boton('Anterior').disabled, boton('Siguiente').disabled]).toEqual([true, false]);

    boton('Siguiente').click();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([factura(2, 'A-2')], 21, 2));
    fixture.detectChanges();
    expect([boton('Anterior').disabled, boton('Siguiente').disabled]).toEqual([false, true]);
  });

  it('pagina: «Siguiente» pide la página 2 con los mismos criterios', () => {
    c.numero = 'A-1';
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([factura(1, 'A-1')], 25));
    fixture.detectChanges();

    Array.from(raiz().querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.includes('Siguiente'))!.click();

    const pedido = servidor.expectOne(r => r.url === '/api/facturas-compra');
    expect([pedido.request.params.get('pagina'), pedido.request.params.get('numero')]).toEqual(['2', 'A-1']);
    pedido.flush(pagina([factura(2, 'A-2')], 25, 2));
  });
});
