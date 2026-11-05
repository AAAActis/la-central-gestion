import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PaginaArticulos } from '../../nucleo/api';
import { Articulos } from './articulos';

/** La pantalla de artículos sobre el listado reutilizable (HU-UI-04): el camino real hasta la API. */
describe('Articulos · listado', () => {
  let fixture: ComponentFixture<Articulos>;
  let servidor: HttpTestingController;
  const raiz = () => fixture.nativeElement as HTMLElement;

  const PAGINA: PaginaArticulos = {
    articulos: [
      { id: 6, codigoInterno: 'LC-000006', nombre: 'Bujía de encendido iridio', activo: true,
        codigosProveedor: [{ proveedorId: 1, razonSocial: 'Autopartes del Centro SRL', codigo: 'AC-00822' }] },
      { id: 9, codigoInterno: 'LC-000009', nombre: 'Bujía común', activo: false, codigosProveedor: [] },
    ],
    total: 25, pagina: 1, tamanoPagina: 20,
  } as PaginaArticulos;

  function buscar(texto: string) {
    const campo = raiz().querySelector<HTMLInputElement>('input[type="search"]')!;
    campo.value = texto;
    campo.dispatchEvent(new Event('input'));
    raiz().querySelector('form')!.dispatchEvent(new Event('submit'));
  }

  beforeEach(async () => {
    TestBed.configureTestingModule({ imports: [Articulos], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Articulos);
    fixture.autoDetectChanges();
    await fixture.whenStable();
  });

  afterEach(() => servidor.verify());

  it('busca en /api/articulos con la página y su tamaño, y muestra código, nombre y códigos de proveedor', () => {
    buscar('buj');
    const pedido = servidor.expectOne(r => r.url === '/api/articulos');
    expect(pedido.request.params.get('texto')).toBe('buj');
    expect(pedido.request.params.get('incluirInactivos')).toBe('false');
    expect(pedido.request.params.get('pagina')).toBe('1');
    expect(pedido.request.params.get('tamanoPagina')).toBe('20');
    pedido.flush(PAGINA);
    fixture.detectChanges();

    const filas = Array.from(raiz().querySelectorAll('tbody tr'));
    expect(filas.length).toBe(2);
    expect(filas[0].textContent).toContain('LC-000006');
    expect(filas[0].textContent).toContain('AC-00822');
    expect(filas[1].classList).toContain('inactivo');
    expect(raiz().textContent).toContain('Mostrando 1–2 de 25 artículos');
  });

  it('«Ver ficha» abre la ficha de ese artículo', () => {
    buscar('buj');
    servidor.expectOne(r => r.url === '/api/articulos').flush(PAGINA);
    fixture.detectChanges();

    const verFicha = Array.from(raiz().querySelectorAll<HTMLButtonElement>('tbody button'))
      .filter(b => b.textContent?.includes('Ver ficha'));
    verFicha[0].click();

    servidor.expectOne('/api/articulos/6');
  });

  it('«Nuevo artículo» sigue al lado de la búsqueda y no es la acción principal', () => {
    const nuevo = Array.from(raiz().querySelectorAll<HTMLButtonElement>('form button'))
      .find(b => b.textContent?.includes('Nuevo artículo'));
    expect(nuevo).toBeDefined();
    expect(nuevo!.classList).not.toContain('primario');
    expect(raiz().querySelectorAll('button.primario').length).toBe(1);
  });
});
