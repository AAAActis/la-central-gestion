import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ArticuloResultado, FichaArticulo } from '../../nucleo/api';
import { SesionActual } from '../../nucleo/sesion';
import { Articulos } from './articulos';

/**
 * D-S7-19 · con el 16 un artículo puede tener dos códigos del mismo proveedor (el segundo, por la
 * vinculación de la 054a). La ficha muestra los dos y el retiro dice cuál (HU-ART-03, solo ADMINISTRADOR).
 */
describe('Articulos · dos códigos del mismo proveedor', () => {
  let fixture: ComponentFixture<Articulos>;
  let servidor: HttpTestingController;
  const raiz = () => fixture.nativeElement as HTMLElement;

  const FICHA: FichaArticulo = {
    id: 7, codigoInterno: 'LC-000007', nombre: 'Bomba de agua', activo: true, costoVigente: null, ultimoProveedorId: null,
    codigos: [
      { proveedorId: 10, razonSocial: 'Expoyer', codigoOriginal: 'BA-SKF', claveNormalizada: 'BA-SKF' },
      { proveedorId: 10, razonSocial: 'Expoyer', codigoOriginal: 'GN/100', claveNormalizada: 'GN/100' },
    ],
    precios: [], motivoBaja: null, totalExistencias: 0, existencias: [],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Articulos], providers: [provideHttpClient(), provideHttpClientTesting()] });
    TestBed.inject(SesionActual).sesion.set({ token: 't', nombreUsuario: 'jefa', rol: 'ADMINISTRADOR', sucursalId: 1 });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Articulos);
    fixture.detectChanges();
    fixture.componentInstance.verFicha({ id: FICHA.id } as ArticuloResultado);
    servidor.expectOne(`/api/articulos/${FICHA.id}`).flush(FICHA);
    fixture.detectChanges();
  });

  afterEach(() => {
    servidor.match(r => r.url.endsWith('/historial-compras'));
    TestBed.inject(SesionActual).sesion.set(null);
  });

  it('muestra los dos códigos y retira solo el elegido, con el código en la consulta', () => {
    const codigos = Array.from(raiz().querySelectorAll('.codigo')).map(c => c.textContent!.trim());
    expect(codigos).toEqual(['BA-SKF', 'GN/100']);

    const retirar = Array.from(raiz().querySelectorAll('button')).filter(b => b.textContent?.trim() === 'Retirar');
    expect(retirar.length).toBe(2);
    retirar[1].click();

    const pedido = servidor.expectOne(r => r.method === 'DELETE');
    expect(pedido.request.url).toBe('/api/articulos/7/codigos/10');
    expect(pedido.request.params.get('codigo')).toBe('GN/100');
  });
});
