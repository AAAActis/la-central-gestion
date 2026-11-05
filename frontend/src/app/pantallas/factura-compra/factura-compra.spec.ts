import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { FacturaCompra } from './factura-compra';

/**
 * #147 · sin coincidencias, GET /api/proveedores responde 200 con una página vacía (antes, 404
 * con el mensaje). La pantalla tiene que seguir diciendo que no hay proveedores.
 */
describe('FacturaCompra · búsqueda de proveedor', () => {
  it('una página vacía muestra que no hay proveedores activos para lo buscado', () => {
    TestBed.configureTestingModule({ imports: [FacturaCompra], providers: [provideHttpClient(), provideHttpClientTesting()] });
    const servidor = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(FacturaCompra);
    fixture.detectChanges();

    fixture.componentInstance.textoProveedor = 'zzz';
    fixture.componentInstance.buscarProveedor();
    servidor.expectOne(r => r.url === '/api/proveedores').flush({ items: [], total: 0, pagina: 1, tamanoPagina: 20 });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('p.error')?.textContent).toContain('No hay proveedores activos para «zzz».');
    servidor.verify();
  });
});
