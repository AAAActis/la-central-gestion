import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Comparador as DatosComparador, FichaArticulo, FilaComparador } from '../../nucleo/api';
import { Comparador } from './comparador';

/** 050 · HU-UI-12 (con 030, 032, 033, 034 y 054 CA4): el comparador, por el camino real hasta la API. */
describe('Comparador · HU-UI-12 (050)', () => {
  let fixture: ComponentFixture<Comparador>;
  let servidor: HttpTestingController;
  const raiz = () => fixture.nativeElement as HTMLElement;

  const fila = (proveedorId: number, cambios: Partial<FilaComparador> = {}): FilaComparador => ({
    proveedorId, proveedor: `Proveedor ${proveedorId}`, urlReferencia: null, codigo: `C-${proveedorId}`, nombre: 'Bomba',
    marca: 'SKF', precioNeto: 100, precioLista: 200, diferenciaConMasBarato: 0, estadoStock: 'HAY', stock: null,
    urlImagen: null, ultimaLectura: '2026-11-20T10:00:00Z', ultimoComprado: false, ultimaConsultaFallida: false, ...cambios,
  });

  const ficha = (precioLista: number | null): FichaArticulo => ({
    id: 5, codigoInterno: 'ART-5', nombre: 'Bomba de agua', activo: true, costoVigente: 80, ultimoProveedorId: null, codigos: [],
    existencias: [{ sucursalId: 1, sucursal: 'Casa central', cantidad: 3, ubicacionDeposito: 'A-1' },
      { sucursalId: 2, sucursal: 'Depósito 60', cantidad: 0, ubicacionDeposito: null }],
    totalExistencias: 3, motivoBaja: null,
    precios: [{ modalidad: 'Lista', base: 'CostoArticulo', porcentaje: 50, precio: precioLista, calculoManual: false,
      fechaCalculo: null, usuarioId: null, razonAusencia: precioLista === null ? 'SinConfiguracion' : null }],
  });

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Comparador], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Comparador);
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  function elegir(datos: DatosComparador, conPrecio: number | null = 150) {
    fixture.componentInstance.texto = 'bomba';
    raiz().querySelector<HTMLFormElement>('form.filtros')!.dispatchEvent(new Event('submit'));
    servidor.expectOne(r => r.url === '/api/articulos' && r.params.get('texto') === 'bomba').flush({
      articulos: [{ id: 5, codigoInterno: 'ART-5', nombre: 'Bomba de agua', activo: true, codigosProveedor: [] }],
      total: 1, pagina: 1, tamanoPagina: 20, conflictos: [],
    });
    fixture.detectChanges();
    raiz().querySelector<HTMLButtonElement>('button.opcion')!.click();
    servidor.expectOne('/api/articulos/5/comparador').flush(datos);
    servidor.expectOne('/api/articulos/5').flush(ficha(conPrecio));
    fixture.detectChanges();
  }

  const proveedores = () => Array.from(raiz().querySelectorAll('tr.proveedor')).map(tr => Number(tr.getAttribute('data-proveedor')));

  it('CA-001 (050) / CA-002 (030): muestra los proveedores en el orden de la API, el más barato primero', () => {
    elegir({ articuloId: 5, filas: [fila(2, { precioNeto: 90 }), fila(1, { precioNeto: 120, diferenciaConMasBarato: 30 })],
      existenciaPorProveedor: [], mensaje: null });

    expect(proveedores()).toEqual([2, 1]);
    const diferencias = Array.from(raiz().querySelectorAll('td.diferencia')).map(td => td.textContent!.trim());
    expect(diferencias[0]).toBe('El más barato');
    expect(diferencias[1]).toContain('30');
  });

  it('CA-003 (032): señala al último proveedor comprado', () => {
    elegir({ articuloId: 5, filas: [fila(1), fila(2, { ultimoComprado: true })], existenciaPorProveedor: [], mensaje: null });

    const filas = raiz().querySelectorAll('tr.proveedor');
    expect(filas[0].querySelector('.ultimo-comprado')).toBeNull();
    expect(filas[1].querySelector('.ultimo-comprado')).not.toBeNull();
  });

  it('CA-003 (050): si la última consulta a un proveedor falló, lo informa y muestra su último precio con la fecha', () => {
    elegir({ articuloId: 5, filas: [fila(1), fila(2, { ultimaConsultaFallida: true, precioNeto: 130 })], existenciaPorProveedor: [], mensaje: null });

    const filas = raiz().querySelectorAll('tr.proveedor');
    expect(filas[0].querySelector('.consulta-fallida')).toBeNull();
    const aviso = filas[1].querySelector('.consulta-fallida');
    expect(aviso).not.toBeNull();
    expect(aviso!.textContent).toContain('20/11/2026');
    expect(filas[1].textContent).toContain('130');
  });

  it('CA-004 (032): el proveedor que publica un solo precio lo muestra aclarando que no informa bonificado', () => {
    elegir({ articuloId: 5, filas: [fila(1), fila(2, { precioNeto: null, diferenciaConMasBarato: null })], existenciaPorProveedor: [], mensaje: null });

    const filas = raiz().querySelectorAll('tr.proveedor');
    expect(filas[0].querySelector('.sin-bonificado')).toBeNull();
    expect(filas[1].querySelector('.sin-bonificado')).not.toBeNull();
  });

  it('CA-001/CA-002 (034) y CA-005 (050): ofrece abrir el sitio solo si hay URL, en otra pestaña', () => {
    elegir({ articuloId: 5, filas: [fila(1, { urlReferencia: 'https://sur.example/' }), fila(2)], existenciaPorProveedor: [], mensaje: null });

    const filas = raiz().querySelectorAll('tr.proveedor');
    const enlace = filas[0].querySelector<HTMLAnchorElement>('a.sitio')!;
    expect(enlace.getAttribute('href')).toBe('https://sur.example/');
    expect(enlace.target).toBe('_blank');
    expect(enlace.rel).toContain('noopener');
    expect(filas[1].querySelector('a.sitio')).toBeNull();
  });

  it('CA4 (054): el stock por proveedor es el que manda la API (el máximo)', () => {
    elegir({ articuloId: 5, filas: [fila(1)], existenciaPorProveedor: [{ proveedorId: 1, estadoStock: 'A_CONFIRMAR', stock: 4 }], mensaje: null });

    expect(raiz().querySelector('.stock-proveedor[data-proveedor="1"]')!.textContent).toContain('A confirmar (4)');
  });

  it('CA-004 (050) / CA-001 y CA-003 (033): la vista Venta muestra el precio estimado y la existencia por sucursal', () => {
    elegir({ articuloId: 5, filas: [fila(1)], existenciaPorProveedor: [], mensaje: null });
    expect(raiz().querySelector('.precio-estimado')).toBeNull();

    raiz().querySelector<HTMLButtonElement>('button.vista-venta')!.click();
    fixture.detectChanges();

    expect(raiz().querySelector('button.vista-venta')!.getAttribute('aria-pressed')).toBe('true');
    expect(raiz().querySelector('button.vista-compra')!.getAttribute('aria-pressed')).toBe('false');
    expect(raiz().querySelector('.precio-estimado')!.textContent).toContain('150');
    expect(raiz().querySelectorAll('tr.existencia').length).toBe(2);
    expect(proveedores()).toEqual([1]);   // junto a los precios de los proveedores
  });

  it('CA-004 (033): sin márgenes, la vista Venta dice que no hay precio estimado', () => {
    elegir({ articuloId: 5, filas: [fila(1)], existenciaPorProveedor: [], mensaje: null }, null);
    raiz().querySelector<HTMLButtonElement>('button.vista-venta')!.click();
    fixture.detectChanges();

    expect(raiz().querySelector('.precio-estimado')).toBeNull();
    expect(raiz().querySelector('.sin-estimado')).not.toBeNull();
  });

  it('CA-004 (030): sin proveedores vinculados muestra el mensaje y no la tabla', () => {
    elegir({ articuloId: 5, filas: [], existenciaPorProveedor: [], mensaje: 'El artículo no tiene filas de catálogo vinculadas de ningún proveedor activo.' });

    expect(raiz().querySelector('[role="status"]')!.textContent).toContain('no tiene filas de catálogo vinculadas');
    expect(raiz().querySelector('tr.proveedor')).toBeNull();
  });
});
