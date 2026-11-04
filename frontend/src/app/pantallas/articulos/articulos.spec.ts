import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ArticuloResultado, FichaArticulo } from '../../nucleo/api';
import { Articulos } from './articulos';

/**
 * N-9 · la ubicación en depósito es de cada sucursal y se carga desde la ficha, no en el alta
 * ni en la modificación (decisión del 04/11/2026).
 */
describe('Articulos · ubicación por sucursal', () => {
  let fixture: ComponentFixture<Articulos>;
  let servidor: HttpTestingController;
  const raiz = () => fixture.nativeElement as HTMLElement;

  const FICHA: FichaArticulo = {
    id: 7, codigoInterno: 'LC-000007', nombre: 'Bujía', activo: true, costoVigente: null, ultimoProveedorId: null,
    codigos: [], precios: [], motivoBaja: null, totalExistencias: 5,
    existencias: [
      { sucursalId: 1, sucursal: 'Fragueiro', cantidad: 5, ubicacionDeposito: 'Estante 4' },
      { sucursalId: 2, sucursal: 'San Vicente', cantidad: 0, ubicacionDeposito: null },
    ],
  };

  function etiquetas() {
    return Array.from(raiz().querySelectorAll('label')).map(l => l.textContent?.trim() ?? '');
  }

  function escribir(input: HTMLInputElement, valor: string) {
    input.value = valor;
    input.dispatchEvent(new Event('input'));
  }

  // ngModel escribe en el ciclo siguiente.
  function estable() {
    fixture.detectChanges();
    tick();
    fixture.detectChanges();
  }

  function abrirFicha(ficha: FichaArticulo = FICHA) {
    fixture.componentInstance.verFicha({ id: ficha.id } as ArticuloResultado);
    servidor.expectOne(`/api/articulos/${ficha.id}`).flush(ficha);
    estable();
  }

  function filaDe(sucursal: string) {
    return Array.from(raiz().querySelectorAll('tr')).find(f => f.textContent?.includes(sucursal))!;
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Articulos], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Articulos);
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  it('el alta no pide ubicación ni la manda', fakeAsync(() => {
    fixture.componentInstance.abrirAlta();
    estable();
    expect(etiquetas().some(t => t.startsWith('Ubicación'))).toBeFalse();

    fixture.componentInstance.nuevo.nombre = 'Bujía';
    fixture.componentInstance.crear();
    const alta = servidor.expectOne(r => r.method === 'POST' && r.url === '/api/articulos');
    expect('ubicacionDeposito' in alta.request.body).toBeFalse();
    alta.flush({ id: 7, codigoInterno: 'LC-000007' });
    servidor.expectOne('/api/articulos/7').flush(FICHA);
  }));

  it('la modificación no pide ubicación ni la manda', fakeAsync(() => {
    abrirFicha();
    expect(etiquetas().some(t => t.startsWith('Ubicación en depósito'))).toBeFalse();

    fixture.componentInstance.modificar(FICHA);
    const cambio = servidor.expectOne(r => r.method === 'PUT' && r.url === '/api/articulos/7');
    expect('ubicacionDeposito' in cambio.request.body).toBeFalse();
    cambio.flush(null);
    servidor.expectOne('/api/articulos/7').flush(FICHA);
  }));

  it('la ficha muestra la ubicación de cada sucursal en la tabla de stock', fakeAsync(() => {
    abrirFicha();

    expect(filaDe('Fragueiro').querySelector('input')!.value).toBe('Estante 4');
    expect(filaDe('San Vicente').querySelector('input')!.value).toBe('');
  }));

  it('guardar la ubicación de una sucursal la manda recortada a esa sucursal y recarga la ficha', fakeAsync(() => {
    abrirFicha();
    const fila = filaDe('San Vicente');
    escribir(fila.querySelector('input')!, '  Pasillo 2 ');
    estable();

    fila.querySelector<HTMLButtonElement>('button')!.click();
    const pedido = servidor.expectOne('/api/articulos/7/ubicaciones/2');
    expect(pedido.request.method).toBe('PUT');
    expect(pedido.request.body).toEqual({ ubicacionDeposito: 'Pasillo 2' });
    pedido.flush(null);
    servidor.expectOne('/api/articulos/7').flush(FICHA);
  }));

  it('vaciar la ubicación la borra (manda null)', fakeAsync(() => {
    abrirFicha();
    const fila = filaDe('Fragueiro');
    escribir(fila.querySelector('input')!, '   ');
    estable();

    fila.querySelector<HTMLButtonElement>('button')!.click();
    const pedido = servidor.expectOne('/api/articulos/7/ubicaciones/1');
    expect(pedido.request.body).toEqual({ ubicacionDeposito: null });
    pedido.flush(null);
    servidor.expectOne('/api/articulos/7').flush(FICHA);
  }));

  it('un artículo dado de baja muestra la ubicación sin poder editarla', fakeAsync(() => {
    abrirFicha({ ...FICHA, activo: false, motivoBaja: 'Discontinuado' });

    const fila = filaDe('Fragueiro');
    expect(fila.textContent).toContain('Estante 4');
    expect(fila.querySelector('input')).toBeNull();
    expect(fila.querySelector('button')).toBeNull();
  }));
});
