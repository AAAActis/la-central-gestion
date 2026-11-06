import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { appConfig } from './app.config';
import { ArticuloResultado, FichaArticulo } from './nucleo/api';
import { Articulos } from './pantallas/articulos/articulos';

/**
 * #148 · montos y cantidades en formato argentino: punto de miles y coma decimal.
 * Se arma con los providers de producción (`appConfig`): lo que se prueba es la configuración
 * con la que arranca la aplicación, no una que el test se inventa.
 */
describe('Formato es-AR', () => {
  const FICHA: FichaArticulo = {
    id: 21, codigoInterno: 'LC-000021', nombre: 'Amortiguador trasero gas', activo: true,
    costoVigente: 80204.04, ultimoProveedorId: 5, codigos: [], motivoBaja: null, totalExistencias: 1234.5,
    precios: [{ modalidad: 'Lista', base: 'CostoArticulo', porcentaje: 50, precio: 120306.06, calculoManual: false,
      fechaCalculo: null, usuarioId: null, razonAusencia: null }],
    existencias: [{ sucursalId: 1, sucursal: 'Fragueiro', cantidad: 1234.5, ubicacionDeposito: null }],
  } as FichaArticulo;

  it('la ficha muestra costo, precio y cantidades con punto de miles y coma decimal', () => {
    TestBed.configureTestingModule({ imports: [Articulos], providers: [...appConfig.providers, provideHttpClientTesting()] });
    const servidor = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(Articulos);
    fixture.detectChanges();

    fixture.componentInstance.verFicha({ id: 21 } as ArticuloResultado);
    servidor.expectOne('/api/articulos/21').flush(FICHA);
    fixture.detectChanges();

    const texto = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(texto).toContain('$ 80.204,04');
    expect(texto).toContain('$ 120.306,06');
    expect(texto).toContain('1.234,5');
    expect(texto).not.toContain('80,204.04');
    servidor.verify();
  });
});
