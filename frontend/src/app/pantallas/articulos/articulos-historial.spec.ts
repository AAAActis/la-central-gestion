import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ArticuloResultado, FichaArticulo, RegistroHistorialCompra } from '../../nucleo/api';
import { Articulos } from './articulos';

/** 016 · HU-ART-06: el historial de compras en la ficha del artículo. */
describe('Articulos · historial de compras (016)', () => {
  let fixture: ComponentFixture<Articulos>;
  let servidor: HttpTestingController;
  const raiz = () => fixture.nativeElement as HTMLElement;

  const FICHA: FichaArticulo = {
    id: 7, codigoInterno: 'LC-000007', nombre: 'Filtro de aceite', activo: true, costoVigente: 120.5, ultimoProveedorId: 2,
    codigos: [], precios: [], motivoBaja: null, totalExistencias: 0, existencias: [],
  };
  // Como lo devuelve la API: del más reciente al más antiguo.
  const HISTORIAL: RegistroHistorialCompra[] = [
    { fecha: '2026-10-20T12:00:00Z', proveedorId: 2, proveedor: 'Repuestos Sur', precioCosto: 120.5, facturaCompraId: 2, numeroFactura: 'A-0002-00000020', anulado: false },
    { fecha: '2026-10-05T12:00:00Z', proveedorId: 1, proveedor: 'Distribuidora Norte', precioCosto: 100, facturaCompraId: 1, numeroFactura: 'A-0001-00000010', anulado: true },
  ];

  /** La tabla que sigue al título «Historial de compras». */
  const tabla = () => {
    const titulo = Array.from(raiz().querySelectorAll('h3')).find(h => h.textContent?.trim() === 'Historial de compras');
    expect(titulo).withContext('título «Historial de compras»').toBeTruthy();
    return titulo?.nextElementSibling ?? document.createElement('div');
  };

  function abrirFicha(historial: RegistroHistorialCompra[]) {
    fixture.componentInstance.verFicha({ id: 7 } as ArticuloResultado);
    servidor.expectOne('/api/articulos/7').flush(FICHA);
    servidor.expectOne('/api/articulos/7/historial-compras').flush(historial);
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Articulos], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Articulos);
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  it('CA-002 · al abrir la ficha muestra el historial en el orden de la API, con fecha, proveedor, factura y costo', () => {
    abrirFicha(HISTORIAL);

    const filas = Array.from(tabla().querySelectorAll('tbody tr')).map(f => f.textContent ?? '');
    expect(filas.length).toBe(2);
    expect(filas[0]).toContain('20/10/2026');
    expect(filas[0]).toContain('Repuestos Sur');
    expect(filas[0]).toContain('A-0002-00000020');
    expect(filas[0]).toContain('120');
    expect(filas[1]).toContain('05/10/2026');
    expect(filas[1]).toContain('Distribuidora Norte');
  });

  it('un registro anulado se distingue del resto', () => {
    abrirFicha(HISTORIAL);

    const [valido, anulado] = Array.from(tabla().querySelectorAll('tbody tr'));
    expect(anulado?.classList).toContain('anulado');
    expect(anulado?.textContent).toContain('anulado');
    expect(valido?.classList).not.toContain('anulado');
  });

  it('CA-005 · el historial no ofrece editar ni borrar: sin botones ni campos', () => {
    abrirFicha(HISTORIAL);

    expect(tabla().querySelectorAll('button, input, select, textarea').length).toBe(0);
  });

  it('sin compras, lo dice', () => {
    abrirFicha([]);

    expect(tabla().textContent).toContain('Sin compras registradas.');
  });

  // verificador-tdd, 05/11/2026: el camino de error y el cambio de ficha no los afirmaba ningún caso.
  it('si el historial no se puede leer, lo dice en vez de mostrar «Sin compras registradas.»', () => {
    fixture.componentInstance.verFicha({ id: 7 } as ArticuloResultado);
    servidor.expectOne('/api/articulos/7').flush(FICHA);
    servidor.expectOne('/api/articulos/7/historial-compras').flush(
      { status: 500, detail: 'No se pudo leer el historial.' }, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    const seccion = tabla();
    expect(seccion.textContent).toContain('No se pudo leer el historial.');
    expect(seccion.textContent).not.toContain('Sin compras registradas.');
  });

  it('al abrir otra ficha no queda el historial del artículo anterior', () => {
    abrirFicha(HISTORIAL);

    fixture.componentInstance.verFicha({ id: 8 } as ArticuloResultado);
    servidor.expectOne('/api/articulos/8').flush({ ...FICHA, id: 8, codigoInterno: 'LC-000008' });
    const pendiente = servidor.expectOne('/api/articulos/8/historial-compras'); // todavía sin respuesta
    fixture.detectChanges();

    expect(tabla().textContent).not.toContain('Repuestos Sur');
    pendiente.flush([]);
  });

  // verificador-tdd, ronda 2: el caso inverso. Sin limpiar el error, la ficha B mostraba el de A.
  it('el error del historial de un artículo no queda en la ficha del siguiente', () => {
    fixture.componentInstance.verFicha({ id: 7 } as ArticuloResultado);
    servidor.expectOne('/api/articulos/7').flush(FICHA);
    servidor.expectOne('/api/articulos/7/historial-compras').flush(
      { status: 500, detail: 'No se pudo leer el historial.' }, { status: 500, statusText: 'Server Error' });

    fixture.componentInstance.verFicha({ id: 8 } as ArticuloResultado);
    servidor.expectOne('/api/articulos/8').flush({ ...FICHA, id: 8, codigoInterno: 'LC-000008' });
    servidor.expectOne('/api/articulos/8/historial-compras').flush(HISTORIAL);
    fixture.detectChanges();

    expect(tabla().textContent).not.toContain('No se pudo leer el historial.');
    expect(tabla().querySelectorAll('tbody tr').length).toBe(2);
  });
});
