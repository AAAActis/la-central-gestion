import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FacturaCompra } from './factura-compra';

/** Fix B (cierre del Sprint 6): datos generales, detalle de artículos, percepciones, totales y acciones. */
describe('FacturaCompra · orden del formulario', () => {
  let fixture: ComponentFixture<FacturaCompra>;
  let raiz: HTMLElement;
  const tarjeta = (nodo: Element | null | undefined) => nodo?.closest('section.tarjeta') ?? null;
  const h2 = (texto: string) => [...raiz.querySelectorAll('h2')].find(h => h.textContent?.includes(texto));
  const percepciones = () => ['pIva', 'pIibb', 'pMunicipal', 'pGanancias'].map(n => raiz.querySelector(`input[name="${n}"]`));

  function enOrden(nodos: (Element | null)[]) {
    nodos.forEach((nodo, i) => expect(nodo).withContext(`bloque ${i + 1}`).not.toBeNull());
    for (let i = 1; i < nodos.length; i++)
      expect(nodos[i - 1]!.compareDocumentPosition(nodos[i]!) & Node.DOCUMENT_POSITION_FOLLOWING)
        .withContext(`bloque ${i} antes del ${i + 1}`).toBeTruthy();
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [FacturaCompra], providers: [provideHttpClient(), provideHttpClientTesting()] });
    fixture = TestBed.createComponent(FacturaCompra);
    fixture.detectChanges();
    raiz = fixture.nativeElement as HTMLElement;
  });

  it('las cuatro percepciones van juntas en su propia tarjeta, distinta de los datos generales y del detalle', () => {
    const propia = tarjeta(percepciones()[0]);
    expect(propia).not.toBeNull();
    percepciones().forEach((p, i) => expect(tarjeta(p)).withContext(`percepción ${i + 1}`).toBe(propia));
    expect(propia).not.toBe(tarjeta(raiz.querySelector('input[name="numero"]')));
    expect(propia).not.toBe(tarjeta(h2('Artículos comprados')));
  });

  it('el detalle de artículos va entre los datos generales y las percepciones; las acciones, al final', () => {
    enOrden([
      tarjeta(raiz.querySelector('input[name="numero"]')),
      tarjeta(h2('Artículos comprados')),
      tarjeta(percepciones()[0]),
      raiz.querySelector('button[type="submit"]'),
    ]);
  });

  it('con el total a la vista, las percepciones siguen antes del total y de «Confirmar y registrar»', () => {
    fixture.componentInstance.totales.set({ neto: 100, iva: 21, percepciones: 0, total: 121 });
    fixture.detectChanges();
    enOrden([tarjeta(h2('Artículos comprados')), tarjeta(percepciones()[0]), raiz.querySelector('[data-totales]')]);
  });
});
