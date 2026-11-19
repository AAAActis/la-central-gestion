import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotaCreditoPdfInterpretada } from '../../nucleo/api';
import { NotaCreditoCompra } from './nota-credito-compra';

/** HU-FCO-07 (057) CA-010 · el PDF de la nota de crédito precarga la carga manual. Datos inventados. */
describe('NotaCreditoCompra · 057 · carga desde PDF', () => {
  const pdf = () => new File(['%PDF-1.7'], 'nota.pdf', { type: 'application/pdf' });
  const factura = (id: number, numero: string) => ({
    id, proveedorId: 7, proveedor: 'Repuestos Ficticios', numero, tipoComprobante: 'A', fechaEmision: '2027-01-10', fechaVencimiento: null, anulada: false,
  });
  const interpretada = (cambios: Partial<NotaCreditoPdfInterpretada> = {}): NotaCreditoPdfInterpretada => ({
    proveedorId: 7, numero: '0010-00004321', fechaEmision: '2027-02-04', clase: 'BONIFICACION', importeNeto: 1000,
    facturaIds: [501], facturasSinEncontrar: [], paraRevisar: [], ...cambios,
  });

  let fixture: ComponentFixture<NotaCreditoCompra>;
  let servidor: HttpTestingController;
  let raiz: HTMLElement;
  const c = () => fixture.componentInstance;

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [NotaCreditoCompra], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(NotaCreditoCompra);
    raiz = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  /** Carga el PDF y contesta la interpretación, la ficha del proveedor y sus facturas activas. */
  function cargar(respuesta: NotaCreditoPdfInterpretada) {
    c().cargarPdf(pdf());
    const pedido = servidor.expectOne('/api/notas-credito-compra/pdf');
    expect((pedido.request.body as FormData).get('archivo')).toBeInstanceOf(File);
    pedido.flush(respuesta);
    if (respuesta.proveedorId !== null) {
      servidor.expectOne(`/api/proveedores/${respuesta.proveedorId}`).flush({
        codigo: 'PRV-7', razonSocial: 'Repuestos Ficticios', cuit: '30-11111111-2', activo: true, telefonos: [], direcciones: [],
      });
      servidor.expectOne(r => r.url === '/api/facturas-compra' && r.params.get('proveedorId') === `${respuesta.proveedorId}`)
        .flush({ items: [factura(501, '0011-00001111'), factura(502, '0011-00003333')], total: 2, pagina: 1, tamanoPagina: 100 });
    }
    fixture.detectChanges();
  }

  const campo = (nombre: string) => raiz.querySelector(`[name="${nombre}"]`) as HTMLInputElement;
  const casilla = (numero: string) =>
    [...raiz.querySelectorAll('label')].find(l => l.textContent?.includes(numero))?.querySelector('input[type="checkbox"]') as HTMLInputElement;

  it('CA-010: precarga proveedor, número, fecha, clase, importe neto y marca las facturas de «Doc.Asoc.»', async () => {
    cargar(interpretada());
    await fixture.whenStable();
    fixture.detectChanges();

    expect(c().proveedor()?.id).toBe(7);
    expect(raiz.textContent).toContain('Repuestos Ficticios');
    expect(campo('numero').value).toBe('0010-00004321');
    expect(campo('emision').value).toBe('2027-02-04');
    expect(c().clase).toBe('BONIFICACION');
    expect(c().importeNeto).toBe(1000);
    expect(casilla('0011-00001111').checked).toBeTrue();
    expect(casilla('0011-00003333').checked).toBeFalse();
    expect(raiz.querySelector('.etiqueta.revisar')).toBeNull();
  });

  it('CA-010: la nota precargada viaja con desdePdf, para que quede con origen PDF', () => {
    cargar(interpretada());

    expect(c().nota()).toEqual(jasmine.objectContaining({ proveedorId: 7, numero: '0010-00004321', facturaIds: [501], desdePdf: true }));
  });

  it('la carga manual no manda desdePdf', () => {
    cargar(interpretada());
    c().nueva();
    c().elegirProveedor({ id: 7, codigo: 'PRV-7', razonSocial: 'Repuestos Ficticios', cuit: null, activo: true });
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush({ items: [factura(501, '0011-00001111')], total: 1, pagina: 1, tamanoPagina: 100 });
    c().numero = '0010-1';
    c().fechaEmision = '2027-02-04';
    c().importeNeto = 10;
    c().alternarFactura(501);

    expect(c().nota()).not.toBeNull();
    expect(c().nota()!.desdePdf).toBeUndefined();
  });

  it('CA-010: lo que el PDF no afirmó queda señalado para revisar, con las facturas que no encontró', () => {
    cargar(interpretada({ importeNeto: null, clase: null, facturaIds: [], facturasSinEncontrar: ['0011-00002222'], paraRevisar: ['clase', 'importeNeto', 'facturaIds'] }));

    expect(campo('importeNeto').closest('label')!.querySelector('.etiqueta.revisar')).not.toBeNull();
    expect(campo('clase').closest('label')!.querySelector('.etiqueta.revisar')).not.toBeNull();
    expect(campo('numero').closest('label')!.querySelector('.etiqueta.revisar')).toBeNull();
    const aviso = raiz.querySelector('[data-sin-encontrar]');
    expect(aviso?.textContent).toContain('0011-00002222');
    expect(aviso?.querySelector('.etiqueta.revisar')).not.toBeNull();
  });

  it('CA-010: sin proveedor reconocido, señala que hay que buscarlo y no pide ficha ni facturas', () => {
    cargar(interpretada({ proveedorId: null, facturaIds: [], facturasSinEncontrar: ['0011-00001111'], paraRevisar: ['proveedorId', 'facturaIds'] }));

    expect(c().proveedor()).toBeNull();
    expect(raiz.textContent).toContain('El PDF no trae un proveedor activo');
    expect(campo('numero').value).toBe('0010-00004321');
  });

  it('CA-010: un layout desconocido o un escaneo muestra el pedido de carga manual y no precarga nada', () => {
    c().cargarPdf(pdf());
    servidor.expectOne('/api/notas-credito-compra/pdf').flush(
      { detail: 'El sistema no reconoce el formato de esta nota de crédito. Cargá la nota de crédito por la vía manual.' },
      { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(raiz.querySelector('[role="alert"]')?.textContent).toContain('por la vía manual');
    expect(campo('numero').value).toBe('');
    expect(c().leyendoPdf()).toBeFalse();
  });

  it('elegir el archivo en el selector lo manda a interpretar', () => {
    const entrada = raiz.querySelector('input[type="file"]') as HTMLInputElement;
    const archivos = new DataTransfer();
    archivos.items.add(pdf());
    entrada.files = archivos.files;
    entrada.dispatchEvent(new Event('change'));

    servidor.expectOne('/api/notas-credito-compra/pdf').flush(interpretada({ proveedorId: null }));
    expect(entrada.value).toBe('');
  });

  it('mientras lee el PDF, el selector de archivo queda deshabilitado', () => {
    c().cargarPdf(pdf());
    fixture.detectChanges();

    expect((raiz.querySelector('input[type="file"]') as HTMLInputElement).disabled).toBeTrue();
    servidor.expectOne('/api/notas-credito-compra/pdf').flush(interpretada({ proveedorId: null }));
  });
});
