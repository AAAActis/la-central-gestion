import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { appConfig } from '../../app.config';
import { FacturaPdfInterpretada } from '../../nucleo/api';
import { SesionActual } from '../../nucleo/sesion';
import { FacturaCompra } from './factura-compra';

/** HU-UI-11 (049) · la carga desde el PDF precarga el formulario de la 056. Datos inventados. */
describe('FacturaCompra · HU-UI-11 · carga desde PDF', () => {
  let fixture: ComponentFixture<FacturaCompra>;
  let servidor: HttpTestingController;
  let c: FacturaCompra;
  const raiz = () => fixture.nativeElement as HTMLElement;
  const pdf = () => new File(['%PDF-1.7'], 'factura.pdf', { type: 'application/pdf' });

  const ficha = (id: number, costo: number | null = 999) => ({
    id, codigoInterno: `A${id}`, nombre: `Artículo ${id}`, activo: true, costoVigente: costo, precios: [], codigos: [],
    existencias: [{ sucursalId: 1, sucursal: 'Fragueiro', cantidad: 0 }, { sucursalId: 2, sucursal: 'San Vicente', cantidad: 0 }],
  });
  const linea = (codigo: string, articuloId: number | null, cantidad = 2, precio = 4000) => ({
    codigoProveedor: codigo, descripcion: `Descripción ${codigo}`, articuloId, cantidad, precioUnitario: precio,
    descuentoPorcentaje: 0, descuento2Porcentaje: 5, descuento3Porcentaje: 20,
  });
  const interpretada = (cambios: Partial<FacturaPdfInterpretada> = {}): FacturaPdfInterpretada => ({
    proveedorId: 7, tipoComprobante: 'A', numero: '0042-00001234', fechaEmision: '2027-02-03', fechaVencimiento: '2027-03-10',
    percepcionIva: 2000.25, percepcionIibb: 1000.5, percepcionMunicipal: null, percepcionGanancias: null,
    lineas: [linea('AB-100', 50)], paraRevisar: [], ...cambios,
  });

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [FacturaCompra], providers: [...appConfig.providers, provideHttpClientTesting()] });
    TestBed.inject(SesionActual).sesion.set({ token: 't', nombreUsuario: 'u', rol: 'OPERADOR', sucursalId: 1 });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(FacturaCompra);
    c = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  /** Carga el PDF y contesta la interpretación, el proveedor y la ficha de cada línea resuelta. */
  function cargar(respuesta: FacturaPdfInterpretada, habitual: 'A' | 'B' | null = 'B') {
    c.cargarPdf(pdf());
    servidor.expectOne('/api/facturas-compra/pdf').flush(respuesta);
    if (respuesta.proveedorId !== null)
      servidor.expectOne(`/api/proveedores/${respuesta.proveedorId}`).flush({
        codigo: 'PRV-7', razonSocial: 'Repuestos Ficticios', cuit: '30-11111111-2', activo: true, telefonos: [], direcciones: [],
        tipoComprobanteHabitual: habitual,
      });
    for (const l of respuesta.lineas.filter(x => x.articuloId !== null))
      servidor.expectOne(`/api/articulos/${l.articuloId}`).flush(ficha(l.articuloId!));
    fixture.detectChanges();
  }

  const campo = (nombre: string) => raiz().querySelector(`[name="${nombre}"]`) as HTMLInputElement;
  const etiquetaDe = (nombre: string) => campo(nombre).closest('label')!.querySelector('.etiqueta.revisar');
  const verTotal = () => Array.from(raiz().querySelectorAll('button')).find(b => b.textContent!.includes('Ver total')) as HTMLButtonElement;

  it('CA-005: mientras se lee el PDF muestra el avance y no deja elegir otro; después lo saca', () => {
    c.cargarPdf(pdf());
    fixture.detectChanges();
    const pedido = servidor.expectOne('/api/facturas-compra/pdf');
    expect((pedido.request.body as FormData).get('archivo')).toEqual(jasmine.any(File));
    expect(raiz().querySelector('[role="status"]')?.textContent).toContain('Leyendo el PDF');
    expect((raiz().querySelector('input[type="file"]') as HTMLInputElement).disabled).toBeTrue();

    pedido.flush({ message: 'x' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();
    expect(raiz().textContent).not.toContain('Leyendo el PDF');
    expect((raiz().querySelector('input[type="file"]') as HTMLInputElement).disabled).toBeFalse();
  });

  it('CA-005: elegir el archivo en el campo lo manda a leer', () => {
    const entrada = raiz().querySelector('input[type="file"]') as HTMLInputElement;
    const archivos = new DataTransfer();
    archivos.items.add(pdf());
    entrada.files = archivos.files;
    entrada.dispatchEvent(new Event('change'));
    servidor.expectOne('/api/facturas-compra/pdf').flush({}, { status: 400, statusText: 'Bad Request' });
  });

  it('CA-001: precarga el mismo formulario con lo que leyó, todo editable', async () => {
    cargar(interpretada());
    // ngModel escribe en el DOM en una tarea posterior.
    await fixture.whenStable();
    fixture.detectChanges();
    await fixture.whenStable();

    expect(raiz().textContent).toContain('Repuestos Ficticios');
    expect(c.tipoComprobante).toBe('A'); // el del PDF, no el habitual del proveedor (B)
    expect(campo('numero').value).toBe('0042-00001234');
    expect(campo('emision').value).toBe('2027-02-03');
    expect(campo('vencimiento').value).toBe('2027-03-10');
    expect(campo('pIva').valueAsNumber).toBe(2000.25);
    expect(campo('pIibb').valueAsNumber).toBe(1000.5);
    expect(campo('pMunicipal').valueAsNumber).toBe(0);
    expect(c.lineas.length).toBe(1);
    // Entradas de la fila: cantidad, precio, tres descuentos. El precio es el del comprobante, no el costo de la ficha.
    const fila = Array.from(raiz().querySelectorAll<HTMLInputElement>('table.lineas tbody tr')[0].querySelectorAll('input'));
    expect(fila.map(i => i.valueAsNumber)).toEqual([2, 4000, 0, 5, 20]);
    expect(fila.some(i => i.disabled) || campo('numero').disabled).toBeFalse();
    expect(c.lineas[0].sucursalId).toBe(1);
  });

  it('CA-005: al terminar de leer bien el PDF saca el avance y vuelve a habilitar el campo', () => {
    cargar(interpretada());

    expect(raiz().textContent).not.toContain('Leyendo el PDF');
    expect((raiz().querySelector('input[type="file"]') as HTMLInputElement).disabled).toBeFalse();
  });

  it('CA-001: un segundo PDF reemplaza lo que dejó el primero, no se le suma', () => {
    cargar(interpretada({ lineas: [linea('AB-100', 50), linea('X1', null)], paraRevisar: ['numero'] }));
    cargar(interpretada({ numero: '0042-00009999', lineas: [linea('CC-3', 51)], paraRevisar: [] }));

    expect(c.lineas.map(l => l.articulo.id)).toEqual([51]);
    expect(c.sinResolver.length).toBe(0);
    expect(c.paraRevisar()).toEqual([]);
    expect(c.numero).toBe('0042-00009999');
  });

  it('CA-001: un rechazo del servidor se muestra y el formulario queda como estaba', () => {
    c.numero = 'escrito a mano';
    c.cargarPdf(pdf());
    servidor.expectOne('/api/facturas-compra/pdf').flush(
      { detail: 'El PDF está escaneado, sin texto: el sistema no lo puede leer. Cargá la factura por la vía manual.' },
      { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(raiz().querySelector('[role="alert"]')?.textContent).toContain('vía manual');
    expect(c.numero).toBe('escrito a mano');
  });

  it('CA-002: los campos que el PDF no pudo afirmar se señalan; los demás, no', () => {
    cargar(interpretada({ tipoComprobante: null, numero: null, paraRevisar: ['tipoComprobante', 'numero', 'lineas[0]'] }));

    expect(etiquetaDe('numero')?.textContent).toContain('Revisar');
    expect(etiquetaDe('tipo')).not.toBeNull();
    expect(etiquetaDe('emision')).toBeNull();
    expect(raiz().querySelectorAll('tbody tr')[0].querySelector('.etiqueta.revisar')).not.toBeNull();
    expect(campo('numero').getAttribute('aria-invalid')).toBe('true');
    expect(campo('emision').getAttribute('aria-invalid')).toBeNull();
  });

  it('CA-002: sin proveedor reconocido, lo señala y deja buscarlo a mano', () => {
    cargar(interpretada({ proveedorId: null, lineas: [], paraRevisar: ['proveedorId'] }));

    expect(c.proveedor()).toBeNull();
    expect(raiz().querySelector('[name="textoProveedor"]')).not.toBeNull();
    expect(raiz().querySelector('.etiqueta.revisar')?.textContent).toContain('Revisar');
  });

  it('CA-003: la línea sin artículo queda sin resolver, con su código y el enlace al alta', () => {
    cargar(interpretada({ lineas: [linea('AB-100', 50), linea('ZZ9', null, 1, 1250.5)] }));

    expect(c.lineas.length).toBe(1);
    const pendiente = raiz().querySelector('[data-sin-resolver]')!;
    expect(pendiente.textContent).toContain('ZZ9');
    expect(pendiente.textContent).toContain('Descripción ZZ9');
    const alta = pendiente.querySelector('a') as HTMLAnchorElement;
    expect(alta.getAttribute('href')).toBe('/articulos');
    expect(alta.target).toBe('_blank');
  });

  it('CA-003: elegir el artículo de una línea sin resolver la pasa a las líneas con los datos del PDF', () => {
    cargar(interpretada({ lineas: [linea('ZZ9', null, 3, 1250.5)] }));
    c.resolviendo.set(0);
    c.textoArticulo = 'A60';
    c.enterArticulo();
    servidor.expectOne(r => r.url === '/api/articulos').flush({
      articulos: [{ id: 60, codigoInterno: 'A60', nombre: 'Artículo 60', activo: true, codigosProveedor: [] }],
      total: 1, pagina: 1, tamanoPagina: 10, conflictos: [],
    });
    servidor.expectOne('/api/articulos/60').flush(ficha(60));
    fixture.detectChanges();

    expect(c.sinResolver.length).toBe(0);
    expect(c.resolviendo()).toBeNull();
    expect(c.lineas.map(l => [l.articulo.id, l.cantidad, l.precioUnitario, l.descuento3Porcentaje])).toEqual([[60, 3, 1250.5, 20]]);
  });

  it('CA-003: quitar una línea sin resolver la saca', () => {
    cargar(interpretada({ lineas: [linea('ZZ9', null)] }));
    const quitar = Array.from(raiz().querySelectorAll('[data-sin-resolver] button')).find(b => b.textContent!.includes('Quitar')) as HTMLButtonElement;
    quitar.click();
    fixture.detectChanges();

    expect(c.sinResolver.length).toBe(0);
    expect(raiz().querySelector('[data-sin-resolver]')).toBeNull();
  });

  it('CA-004: con líneas sin resolver, no deja ver el total ni confirmar, y dice cuántas faltan', () => {
    cargar(interpretada({ lineas: [linea('X1', null), linea('X2', null)] }));

    expect(verTotal().disabled).toBeTrue();
    expect(raiz().textContent).toContain('Faltan resolver 2 líneas');

    c.registrar();
    servidor.expectNone('/api/facturas-compra/calculo');
    c.confirmar();
    servidor.expectNone('/api/facturas-compra');
  });

  it('CA-004: al resolver la última, se habilita', () => {
    cargar(interpretada({ lineas: [linea('X1', null)] }));
    c.sinResolver.splice(0, 1);
    fixture.detectChanges();

    expect(verTotal().disabled).toBeFalse();
    expect(raiz().textContent).not.toContain('Faltan resolver');
  });

  it('CA-004: un total pedido antes de cargar el PDF y que llega después no se muestra sobre el formulario del PDF', () => {
    c.proveedor.set({ id: 7, codigo: 'PRV-7', razonSocial: 'R', cuit: null, activo: true });
    c.registrar();
    const calculoViejo = servidor.expectOne('/api/facturas-compra/calculo');
    cargar(interpretada({ lineas: [linea('X1', null)] }));

    calculoViejo.flush({ neto: 1, iva: 0, percepciones: 0, total: 1 });
    fixture.detectChanges();

    expect(c.totales()).toBeNull();
    expect(raiz().querySelector('[data-totales]')).toBeNull();
    expect(verTotal().disabled).toBeTrue();
  });

  it('029 CA-006: lo cargado desde el PDF se registra con la constancia; lo manual, no', () => {
    cargar(interpretada());
    c.confirmar();
    expect(servidor.expectOne('/api/facturas-compra').request.body.desdePdf).toBeTrue();
  });

  it('029 CA-006: «Cargar otra factura» borra la constancia y lo pendiente del PDF anterior', () => {
    cargar(interpretada({ lineas: [linea('X1', null)], paraRevisar: ['numero'] }));
    c.nueva();
    fixture.detectChanges();

    expect(c.sinResolver.length).toBe(0);
    expect(c.paraRevisar()).toEqual([]);
    c.proveedor.set({ id: 7, codigo: 'PRV-7', razonSocial: 'R', cuit: null, activo: true });
    c.confirmar();
    expect(servidor.expectOne('/api/facturas-compra').request.body.desdePdf).toBeFalsy();
  });
});
