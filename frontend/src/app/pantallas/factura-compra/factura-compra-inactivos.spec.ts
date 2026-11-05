import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FacturaCompra } from './factura-compra';

/** 014 · CA-005: un artículo dado de baja no se ofrece para la factura de compra. */
describe('FacturaCompra · artículos inactivos (014 CA-005)', () => {
  it('la búsqueda de artículos pide solo activos', () => {
    TestBed.configureTestingModule({ imports: [FacturaCompra], providers: [provideHttpClient(), provideHttpClientTesting()] });
    const servidor = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(FacturaCompra);
    fixture.detectChanges();

    fixture.componentInstance.textoArticulo = 'bujía';
    fixture.componentInstance.buscarArticulo();

    const pedido = servidor.expectOne(r => r.url === '/api/articulos');
    expect(pedido.request.params.get('incluirInactivos')).toBe('false');
    pedido.flush({ articulos: [], total: 0, pagina: 1, tamanoPagina: 10 });
    servidor.verify();
  });
});
