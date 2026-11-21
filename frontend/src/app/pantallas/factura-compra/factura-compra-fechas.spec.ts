import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RELOJ } from '../../compartido/fecha-abreviada';
import { FacturaCompra } from './factura-compra';

/** Fix A (cierre del Sprint 6): fechas abreviadas en la factura de compra, con hoy = 19/11/2026. */
describe('FacturaCompra · fechas abreviadas', () => {
  let fixture: ComponentFixture<FacturaCompra>;
  let servidor: HttpTestingController;
  const c = () => fixture.componentInstance;
  const campo = (nombre: string) => (fixture.nativeElement as HTMLElement).querySelector(`input[name="${nombre}"]`) as HTMLInputElement;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      imports: [FacturaCompra],
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: RELOJ, useValue: () => new Date(2026, 10, 19, 23, 30) }],
    });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(FacturaCompra);
    fixture.detectChanges();
    await fixture.whenStable(); // NgForm registra los controles en una microtarea.
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  function tipear(nombre: string, texto: string) {
    const entrada = campo(nombre);
    entrada.value = texto;
    entrada.dispatchEvent(new Event('input', { bubbles: true }));
  }

  function enter(nombre: string) {
    const evento = new KeyboardEvent('keydown', { key: 'Enter', bubbles: true, cancelable: true });
    campo(nombre).dispatchEvent(evento);
    fixture.detectChanges();
    return evento;
  }

  async function mostrado(nombre: string) {
    await fixture.whenStable();
    return campo(nombre).value;
  }

  it('la imputación arranca en hoy, según el reloj', async () => {
    expect(c().fechaImputacion).toBe('2026-11-19');
    expect(await mostrado('imputacion')).toBe('19/11/2026');
  });

  it('Enter en la emisión interpreta el día y no envía el formulario', async () => {
    tipear('emision', '5');
    expect(enter('emision').defaultPrevented).withContext('sin cancelar, el navegador envía el form').toBeTrue();
    expect(c().fechaEmision).toBe('2026-11-05');
    expect(await mostrado('emision')).toBe('05/11/2026');
  });

  it('salir del vencimiento interpreta ddmm con el año actual', async () => {
    tipear('vencimiento', '2010');
    campo('vencimiento').dispatchEvent(new Event('blur'));
    fixture.detectChanges();
    expect(c().fechaVencimiento).toBe('2026-10-20');
    expect(await mostrado('vencimiento')).toBe('20/10/2026');
  });

  it('una fecha que no existe se dice, conserva lo tipeado y no queda fecha', async () => {
    c().fechaEmision = '2026-11-01';
    tipear('emision', '31/02');
    enter('emision');
    const raiz = fixture.nativeElement as HTMLElement;
    expect(raiz.querySelector('#error-emision')?.textContent).toContain('El 31/02/2026 no existe.');
    expect(campo('emision').getAttribute('aria-invalid')).toBe('true');
    expect(campo('emision').getAttribute('aria-describedby')).toBe('error-emision');
    expect(await mostrado('emision')).toBe('31/02');
    expect(c().fechaEmision).toBe('');
  });

  it('con una fecha mal tipeada no se pide el total: no viaja vacía en silencio', () => {
    c().proveedor.set({ id: 3, codigo: 'PRV-3', razonSocial: 'Expoyer', cuit: null, activo: true });
    tipear('vencimiento', '123');
    enter('vencimiento');
    c().registrar();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Corregí las fechas señaladas.');
    servidor.expectNone('/api/facturas-compra/calculo');
  });

  it('corregida la fecha, el aviso se va y viaja como aaaa-mm-dd', () => {
    c().proveedor.set({ id: 3, codigo: 'PRV-3', razonSocial: 'Expoyer', cuit: null, activo: true });
    tipear('emision', '31/02');
    enter('emision');
    tipear('emision', '28/02');
    enter('emision');
    expect((fixture.nativeElement as HTMLElement).querySelector('#error-emision')).toBeNull();
    c().registrar();
    const cuerpo = servidor.expectOne('/api/facturas-compra/calculo').request.body;
    expect(cuerpo.fechaEmision).toBe('2026-02-28');
    expect(cuerpo.fechaImputacion).toBe('2026-11-19');
  });

  it('cargar otra factura después de una fecha mal tipeada saca el aviso y deja pedir el total', () => {
    tipear('emision', '31/02');
    enter('emision');
    c().nueva();
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('#error-emision')).toBeNull();
    c().proveedor.set({ id: 3, codigo: 'PRV-3', razonSocial: 'Expoyer', cuit: null, activo: true });
    c().registrar();
    servidor.expectOne('/api/facturas-compra/calculo');
  });

  it('la emisión es obligatoria; el vencimiento y la imputación, no', () => {
    expect(campo('emision').required).toBeTrue();
    expect(campo('vencimiento').required).toBeFalse();
    expect(campo('imputacion').required).toBeFalse();
  });

  it('cargar otra factura vuelve a poner hoy en la imputación', async () => {
    tipear('imputacion', '01/11');
    enter('imputacion');
    c().nueva();
    fixture.detectChanges();
    expect(c().fechaImputacion).toBe('2026-11-19');
    expect(await mostrado('imputacion')).toBe('19/11/2026');
  });
  // Respuesta de Javi (21/11): el vencimiento nunca es anterior a la emisión; se avisa y no se registra.
  const ORDEN = 'El vencimiento no puede ser anterior a la emisión.';
  const expoyer = { id: 3, codigo: 'PRV-3', razonSocial: 'Expoyer', cuit: null, activo: true };
  const raiz = () => fixture.nativeElement as HTMLElement;

  it('un vencimiento anterior a la emisión se avisa en el vencimiento y no se pide el total', () => {
    c().proveedor.set(expoyer);
    tipear('emision', '20/10');
    enter('emision');
    tipear('vencimiento', '19/10');
    enter('vencimiento');
    expect(raiz().querySelector('#error-vencimiento')?.textContent).toContain(ORDEN);
    expect(campo('vencimiento').getAttribute('aria-invalid')).toBe('true');
    c().registrar();
    fixture.detectChanges();
    expect(raiz().textContent).toContain('Corregí las fechas señaladas.');
    servidor.expectNone('/api/facturas-compra/calculo');
  });

  it('el vencimiento el mismo día de la emisión vale', () => {
    c().proveedor.set(expoyer);
    tipear('emision', '20/10');
    enter('emision');
    tipear('vencimiento', '20/10');
    enter('vencimiento');
    expect(raiz().querySelector('#error-vencimiento')).toBeNull();
    c().registrar();
    expect(servidor.expectOne('/api/facturas-compra/calculo').request.body.fechaVencimiento).toBe('2026-10-20');
  });

  it('corregir la emisión saca el aviso del vencimiento', () => {
    tipear('vencimiento', '19/10');
    enter('vencimiento');
    tipear('emision', '20/10');
    enter('emision');
    expect(raiz().querySelector('#error-vencimiento')?.textContent).withContext('cargada la emisión después').toContain(ORDEN);
    tipear('emision', '18/10');
    enter('emision');
    expect(raiz().querySelector('#error-vencimiento')).toBeNull();
  });

  it('el aviso de orden no tapa un vencimiento mal tipeado', () => {
    tipear('vencimiento', '123');
    enter('vencimiento');
    tipear('emision', '5');
    enter('emision');
    expect(raiz().querySelector('#error-vencimiento')?.textContent).toContain('dd/mm/aaaa');
  });

  it('fechas fuera de orden que no pasaron por el campo (precarga del PDF) tampoco se registran', () => {
    c().proveedor.set(expoyer);
    c().fechaEmision = '2027-02-03';
    c().fechaVencimiento = '2027-02-01';
    c().registrar();
    fixture.detectChanges();
    expect(raiz().querySelector('#error-vencimiento')?.textContent).toContain(ORDEN);
    servidor.expectNone('/api/facturas-compra/calculo');
  });
});
