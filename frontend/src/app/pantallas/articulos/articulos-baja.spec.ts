import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ArticuloResultado, FichaArticulo } from '../../nucleo/api';
import { Articulos } from './articulos';

/** 014 · HU-ART-04: la baja reescribiendo el código interno (CA-004) y el rechazo por existencia (CA-002). */
describe('Articulos · baja (014)', () => {
  let fixture: ComponentFixture<Articulos>;
  let servidor: HttpTestingController;
  const raiz = () => fixture.nativeElement as HTMLElement;

  const FICHA: FichaArticulo = {
    id: 7, codigoInterno: 'LC-000007', nombre: 'Bujía', activo: true, costoVigente: null, ultimoProveedorId: null,
    codigos: [], precios: [], motivoBaja: null, totalExistencias: 0, existencias: [],
  };

  const dialogo = () => raiz().querySelector<HTMLDialogElement>('dialog[open]');
  const control = (etiqueta: string) => {
    const label = Array.from(dialogo()?.querySelectorAll('label') ?? []).find(l => l.textContent?.trim() === etiqueta);
    expect(label?.control).withContext(`campo «${etiqueta}» en el diálogo`).toBeTruthy();
    return (label?.control ?? document.createElement('input')) as HTMLInputElement;
  };
  const escribir = (campo: HTMLInputElement, valor: string) => {
    campo.value = valor;
    campo.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };

  function abrirBaja() {
    fixture.componentInstance.verFicha({ id: 7 } as ArticuloResultado);
    servidor.expectOne('/api/articulos/7').flush(FICHA);
    fixture.detectChanges();
    Array.from(raiz().querySelectorAll('button')).find(b => b.textContent?.trim() === 'Dar de baja')!.click();
    fixture.detectChanges();
    expect(dialogo()).withContext('diálogo de baja').toBeTruthy();
  }

  function confirmar(motivo: string, clave: string) {
    escribir(control('Motivo'), motivo);
    escribir(control('Para confirmar, escribí LC-000007'), clave);
    dialogo()?.querySelector<HTMLButtonElement>('button[type=submit]')?.click();
    fixture.detectChanges();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Articulos], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Articulos);
    fixture.detectChanges();
  });

  // 016: la ficha también pide su historial de compras; lo prueba articulos-historial.spec.ts.
  afterEach(() => {
    servidor.match(r => r.url.endsWith('/historial-compras'));
    servidor.verify();
  });

  it('CA-004 · pide el motivo y el código interno reescrito, y los manda juntos', () => {
    abrirBaja();
    confirmar('Discontinuado', 'lc-000007');

    const baja = servidor.expectOne('/api/articulos/7/baja');
    expect(baja.request.method).toBe('POST');
    expect(baja.request.body).toEqual({ motivo: 'Discontinuado', confirmacion: 'LC-000007' });
    baja.flush(null);
    servidor.expectOne('/api/articulos/7').flush({ ...FICHA, activo: false, motivoBaja: 'Discontinuado' });
  });

  it('CA-004 · con otro código no se envía', () => {
    abrirBaja();
    confirmar('Discontinuado', 'LC-000008');

    servidor.expectNone('/api/articulos/7/baja');
  });

  it('CA-002 · si queda existencia, muestra lo que informa la API y el diálogo sigue abierto', () => {
    abrirBaja();
    confirmar('Discontinuado', 'LC-000007');

    servidor.expectOne('/api/articulos/7/baja').flush(
      { status: 409, detail: 'No se puede dar de baja: el artículo todavía tiene existencia (Fragueiro: 5).' },
      { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    expect(dialogo()?.querySelector('[role="alert"]')?.textContent).toContain('Fragueiro: 5');
    servidor.expectNone('/api/articulos/7');
  });
});
