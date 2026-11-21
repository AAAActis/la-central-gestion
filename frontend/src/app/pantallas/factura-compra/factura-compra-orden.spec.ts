import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FacturaCompra } from './factura-compra';

/** Fix B (cierre del Sprint 6): datos generales, detalle de artículos, percepciones, totales y acciones. */
describe('FacturaCompra · orden del formulario', () => {
  it('el detalle de artículos va entre los datos generales y las percepciones; las acciones, al final', () => {
    TestBed.configureTestingModule({ imports: [FacturaCompra], providers: [provideHttpClient(), provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(FacturaCompra);
    fixture.detectChanges();
    const raiz = fixture.nativeElement as HTMLElement;
    const h2 = (texto: string) => [...raiz.querySelectorAll('h2')].find(h => h.textContent?.includes(texto))!;

    const orden = [
      raiz.querySelector('input[name="numero"]'),
      h2('Artículos comprados'),
      raiz.querySelector('input[name="pIva"]'),
      raiz.querySelector('button[type="submit"]'),
    ];
    orden.forEach((nodo, i) => expect(nodo).withContext(`bloque ${i + 1}`).not.toBeNull());
    for (let i = 1; i < orden.length; i++)
      expect(orden[i - 1]!.compareDocumentPosition(orden[i]!) & Node.DOCUMENT_POSITION_FOLLOWING)
        .withContext(`bloque ${i} antes del ${i + 1}`).toBeTruthy();
  });
});
