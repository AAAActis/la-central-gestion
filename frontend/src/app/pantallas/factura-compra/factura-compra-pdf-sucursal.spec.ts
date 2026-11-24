import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { appConfig } from '../../app.config';
import { FacturaPdfInterpretada } from '../../nucleo/api';
import { SesionActual } from '../../nucleo/sesion';
import { FacturaCompra } from './factura-compra';

/** Fix E · la sucursal y el proveedor que sugiere el PDF llegan marcados «Revisar» y se pueden cambiar. Datos inventados. */
describe('FacturaCompra · Fix E · sucursal y proveedor sugeridos por el PDF', () => {
  let fixture: ComponentFixture<FacturaCompra>;
  let servidor: HttpTestingController;
  let c: FacturaCompra;
  const raiz = () => fixture.nativeElement as HTMLElement;

  const ficha = (id: number) => ({
    id, codigoInterno: `A${id}`, nombre: `Artículo ${id}`, activo: true, costoVigente: 999, precios: [], codigos: [],
    existencias: [{ sucursalId: 1, sucursal: 'Fragueiro', cantidad: 0 }, { sucursalId: 2, sucursal: 'San Vicente', cantidad: 0 }],
  });
  const interpretada = (cambios: Partial<FacturaPdfInterpretada> = {}): FacturaPdfInterpretada => ({
    proveedorId: 7, tipoComprobante: 'A', numero: '0042-00001234', fechaEmision: '2027-02-03', fechaVencimiento: null,
    percepcionIva: null, percepcionIibb: null, percepcionMunicipal: null, percepcionGanancias: null,
    lineas: [{
      codigoProveedor: 'AB-100', descripcion: 'Descripción', articuloId: 50, cantidad: 2, precioUnitario: 4000,
      descuentoPorcentaje: 0, descuento2Porcentaje: 0, descuento3Porcentaje: 0, descuento4Porcentaje: 0,
    }],
    paraRevisar: [], ...cambios,
  });

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [FacturaCompra], providers: [...appConfig.providers, provideHttpClientTesting()] });
    // La sesión es de la sucursal 1: la sugerencia del PDF tiene que poder cambiarla.
    TestBed.inject(SesionActual).sesion.set({ token: 't', nombreUsuario: 'u', rol: 'OPERADOR', sucursalId: 1 });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(FacturaCompra);
    c = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  /** NgModel escribe el valor del select en la microtarea siguiente: se espera antes de mirar el DOM. */
  async function cargar(respuesta: FacturaPdfInterpretada) {
    c.cargarPdf(new File(['%PDF-1.7'], 'factura.pdf', { type: 'application/pdf' }));
    servidor.expectOne('/api/facturas-compra/pdf').flush(respuesta);
    servidor.expectOne(`/api/proveedores/${respuesta.proveedorId}`).flush({
      codigo: 'PRV-7', razonSocial: 'Repuestos Ficticios', cuit: '30-11111111-2', activo: true, telefonos: [], direcciones: [],
      tipoComprobanteHabitual: null,
    });
    for (const l of respuesta.lineas.filter(x => x.articuloId !== null))
      servidor.expectOne(`/api/articulos/${l.articuloId}`).flush(ficha(l.articuloId!));
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  const destino = () => raiz().querySelector('[name="sucursalCabecera"]') as HTMLSelectElement;
  const etiquetaDestino = () => destino().closest('label')!.querySelector('.etiqueta.revisar');
  const botones = (texto: string) => Array.from(raiz().querySelectorAll('button')).filter(b => b.textContent!.includes(texto));
  const proveedorMostrado = () => botones('Cambiar')[0].closest('p')!;

  it('el destino de las líneas nuevas arranca en la sucursal sugerida, marcado «Revisar», y las líneas del PDF la heredan', async () => {
    await cargar(interpretada({ sucursalSugeridaId: 2, paraRevisar: ['sucursalSugeridaId'] }));

    expect(c.sucursalCabecera).toBe(2);
    expect(destino().selectedOptions[0].textContent).toContain('San Vicente');
    expect(etiquetaDestino()).not.toBeNull();
    expect(c.lineas[0].sucursalId).toBe(2);
  });

  it('el operador cambia la sugerencia: la marca se va y lo elegido queda', async () => {
    await cargar(interpretada({ sucursalSugeridaId: 2, paraRevisar: ['sucursalSugeridaId'] }));
    expect(etiquetaDestino()).not.toBeNull();

    destino().value = destino().options[0].value;
    destino().dispatchEvent(new Event('change'));
    await fixture.whenStable();
    fixture.detectChanges();

    expect(c.sucursalCabecera).toBe(1);
    expect(etiquetaDestino()).toBeNull();
  });

  it('sin sugerencia el destino queda como hoy: la sucursal de la sesión, sin marca', async () => {
    await cargar(interpretada({ sucursalSugeridaId: null }));

    expect(c.sucursalCabecera).toBe(1);
    expect(destino().selectedOptions[0].textContent).toContain('Fragueiro');
    expect(etiquetaDestino()).toBeNull();
  });

  it('un proveedor reconocido por su razón social se muestra marcado «Revisar» y se confirma con «Es este»', async () => {
    await cargar(interpretada({ paraRevisar: ['proveedorId'] }));

    expect(proveedorMostrado().textContent).toContain('Repuestos Ficticios');
    expect(proveedorMostrado().querySelector('.etiqueta.revisar')).not.toBeNull();

    expect(botones('Es este').length).toBe(1);
    botones('Es este')[0]?.click();
    fixture.detectChanges();

    expect(proveedorMostrado().querySelector('.etiqueta.revisar')).toBeNull();
    expect(c.proveedor()?.id).toBe(7);
  });

  it('un proveedor reconocido por su CUIT no lleva marca ni botón de confirmar', async () => {
    await cargar(interpretada());

    expect(proveedorMostrado().querySelector('.etiqueta.revisar')).toBeNull();
    expect(botones('Es este').length).toBe(0);
  });
});
