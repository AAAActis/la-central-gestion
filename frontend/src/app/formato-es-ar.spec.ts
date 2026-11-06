import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { appConfig } from './app.config';
import { Listado, Pagina } from './compartido/listado';
import { ArticuloResultado, FichaArticulo } from './nucleo/api';
import { Articulos } from './pantallas/articulos/articulos';

/** Un listado cualquiera: el servidor devuelve la página 51 de 2108. */
@Component({
  imports: [Listado],
  template: `
    <app-listado [consultar]="consultar" [columnas]="['Nombre']" etiqueta="Buscar cosas" nombrePlural="ítems">
      <ng-template #fila let-item><td>{{ item.nombre }}</td></ng-template>
    </app-listado>
  `,
})
class ListadoLargo {
  readonly consultar = () => of(paginaDelServidor);
}

type Cosa = { nombre: string; activo: boolean };
let paginaDelServidor: Pagina<Cosa>;

function pagina(total: number, numero: number): Pagina<Cosa> {
  const desde = (numero - 1) * 20 + 1;
  return { items: Array.from({ length: 20 }, (_, i) => ({ nombre: `Cosa ${desde + i}`, activo: true })), total, pagina: numero, tamanoPagina: 20 };
}

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
    // 016: la ficha también pide su historial de compras.
    servidor.match(r => r.url.endsWith('/historial-compras'));
    servidor.verify();
  });

  function verFicha(ficha: FichaArticulo): string {
    TestBed.configureTestingModule({ imports: [Articulos], providers: [...appConfig.providers, provideHttpClientTesting()] });
    const servidor = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(Articulos);
    fixture.detectChanges();
    fixture.componentInstance.verFicha({ id: ficha.id } as ArticuloResultado);
    servidor.expectOne(`/api/articulos/${ficha.id}`).flush(ficha);
    fixture.detectChanges();
    servidor.match(r => r.url.endsWith('/historial-compras'));
    servidor.verify();
    return ((fixture.nativeElement as HTMLElement).textContent ?? '').replace(/\s+/g, ' ');
  }

  it('#148 · el porcentaje de cada modalidad sale con coma decimal', () => {
    const texto = verFicha({ ...FICHA, precios: [{ ...FICHA.precios[0], porcentaje: 12.5 }] });
    expect(texto).toContain('costo +12,5 %');
    expect(texto).not.toContain('12.5');
  });

  // La columna es NUMERIC(6,2): los dos decimales se muestran, no se redondean a uno.
  it('#148 · un porcentaje con dos decimales sale completo', () => {
    const texto = verFicha({ ...FICHA, precios: [{ ...FICHA.precios[0], porcentaje: 12.25 }] });
    expect(texto).toContain('costo +12,25 %');
  });

  async function listar(p: Pagina<Cosa>): Promise<string> {
    paginaDelServidor = p;
    TestBed.configureTestingModule({ imports: [ListadoLargo], providers: [...appConfig.providers] });
    const fixture = TestBed.createComponent(ListadoLargo);
    fixture.autoDetectChanges();
    await fixture.whenStable(); // ngModel registra sus controles en una microtarea.
    (fixture.nativeElement as HTMLElement).querySelector('form')!.dispatchEvent(new Event('submit'));
    await fixture.whenStable();
    return ((fixture.nativeElement as HTMLElement).textContent ?? '').replace(/\s+/g, ' ');
  }

  it('#148 · el listado dice el rango y el total con punto de miles', async () => {
    const texto = await listar(pagina(2108, 51));
    expect(texto).toContain('2.108 ítems encontrados');
    expect(texto).toContain('Mostrando 1.001–1.020 de 2.108 ítems');
  });

  // Con 20 por página, más de 20.000 resultados dan mil páginas o más.
  it('#148 · la paginación dice la página y el total de páginas con punto de miles', async () => {
    const texto = await listar(pagina(25000, 1050));
    expect(texto).toContain('1.050 de 1.250');
  });
});
