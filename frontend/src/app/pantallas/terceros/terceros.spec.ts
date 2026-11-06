import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Type } from '@angular/core';
import { Pagina } from '../../compartido/listado';
import { FichaTercero, TerceroResumen } from '../../nucleo/api';
import { Clientes, Proveedores } from './terceros';

/** 045 · las pantallas de clientes y proveedores, por el camino real hasta la API (HTTP de prueba). */
function preparar<T>(componente: Type<T>) {
  const h = {} as {
    fixture: ComponentFixture<T>;
    servidor: HttpTestingController;
    raiz: () => HTMLElement;
    buscar: (texto: string) => void;
    control: (etiqueta: string, dentro?: Element) => HTMLInputElement | HTMLSelectElement;
    escribir: (control: HTMLInputElement | HTMLSelectElement, valor: string) => void;
    boton: (texto: string, dentro?: Element) => HTMLButtonElement;
    dialogo: () => HTMLDialogElement;
  };

  beforeEach(async () => {
    TestBed.configureTestingModule({ imports: [componente], providers: [provideHttpClient(), provideHttpClientTesting()] });
    h.servidor = TestBed.inject(HttpTestingController);
    h.fixture = TestBed.createComponent(componente);
    h.fixture.autoDetectChanges();
    await h.fixture.whenStable();
  });

  afterEach(() => h.servidor.verify());

  h.raiz = () => h.fixture.nativeElement as HTMLElement;
  h.buscar = texto => {
    const campo = h.raiz().querySelector<HTMLInputElement>('input[type="search"]');
    expect(campo).withContext('campo de búsqueda').toBeTruthy();
    if (!campo) return;
    campo.value = texto;
    campo.dispatchEvent(new Event('input'));
    campo.form!.dispatchEvent(new Event('submit'));
  };
  h.control = (etiqueta, dentro = h.raiz()) => {
    const label = Array.from(dentro.querySelectorAll('label')).find(l => l.textContent?.trim() === etiqueta);
    expect(label?.control).withContext(`campo «${etiqueta}»`).toBeTruthy();
    return (label?.control ?? document.createElement('input')) as HTMLInputElement;
  };
  h.escribir = (control, valor) => {
    control.value = valor;
    control.dispatchEvent(new Event(control instanceof HTMLSelectElement ? 'change' : 'input'));
    h.fixture.detectChanges();
  };
  h.boton = (texto, dentro = h.raiz()) => {
    // El nombre accesible: aria-label, o el texto visible (un botón en carga esconde el otro texto con .oculta).
    const visible = (b: HTMLButtonElement) => {
      const copia = b.cloneNode(true) as HTMLElement;
      copia.querySelectorAll('.oculta').forEach(o => o.remove());
      return copia.textContent?.trim();
    };
    const boton = Array.from(dentro.querySelectorAll('button')).find(b => (b.getAttribute('aria-label') ?? visible(b)) === texto);
    expect(boton).withContext(`botón «${texto}»`).toBeTruthy();
    return boton ?? document.createElement('button');
  };
  h.dialogo = () => h.raiz().querySelector<HTMLDialogElement>('dialog[open]') ?? (fail('No hay un diálogo abierto.'), document.createElement('dialog'));
  return h;
}

const pagina = (items: TerceroResumen[]): Pagina<TerceroResumen> => ({ items, total: items.length, pagina: 1, tamanoPagina: 20 });

describe('Clientes (045)', () => {
  const h = preparar(Clientes);
  const FILA: TerceroResumen = { id: 4, codigo: 'CLI-001', razonSocial: 'Taller San Vicente', cuit: '30-12345678-9', activo: true };
  const FICHA: FichaTercero = {
    codigo: 'CLI-001', razonSocial: 'Taller San Vicente', cuit: '30-12345678-9', condicionFiscal: 'MONOTRIBUTO',
    condicionPago: 'Contado', activo: true, telefonos: ['351 111-1111', '351 222-2222'],
    direcciones: ['Av. Colón 100', 'Bv. San Juan 200'],
  };

  function abrirFicha(ficha: FichaTercero = FICHA) {
    h.buscar('taller');
    h.servidor.expectOne(r => r.url === '/api/clientes').flush(pagina([{ ...FILA, activo: ficha.activo }]));
    h.fixture.detectChanges();
    h.boton('Ver ficha').click();
    h.servidor.expectOne('/api/clientes/4').flush(ficha);
    h.fixture.detectChanges();
  }

  it('CA-001 · consulta en /api/clientes paginado y lista código, razón social y CUIT', () => {
    h.buscar('taller');
    const pedido = h.servidor.expectOne(r => r.url === '/api/clientes');
    expect(pedido.request.params.get('texto')).toBe('taller');
    expect(pedido.request.params.get('pagina')).toBe('1');
    expect(pedido.request.params.get('tamanoPagina')).toBe('20');
    pedido.flush(pagina([FILA]));
    h.fixture.detectChanges();

    const fila = h.raiz().querySelector('tbody tr')!;
    expect(fila.textContent).toContain('CLI-001');
    expect(fila.textContent).toContain('Taller San Vicente');
    expect(fila.textContent).toContain('30-12345678-9');
  });

  it('CA-001 · da de alta: sin condición fiscal la manda vacía (null) y confirma', () => {
    h.boton('Nuevo cliente').click();
    h.fixture.detectChanges();
    h.escribir(h.control('Código'), 'CLI-002');
    h.escribir(h.control('Razón social'), 'Gomería Norte');
    h.escribir(h.control('Condición de pago'), 'Contado');
    h.boton('Agregar a Teléfonos').click();
    h.fixture.detectChanges();
    h.escribir(h.raiz().querySelector<HTMLInputElement>('input[aria-label="Teléfonos 1"]')!, '351 333-3333');
    expect(h.control('Condición fiscal').tagName).toBe('SELECT');

    h.boton('Guardar').click();
    const alta = h.servidor.expectOne(r => r.method === 'POST' && r.url === '/api/clientes');
    expect(alta.request.body).toEqual({
      codigo: 'CLI-002', razonSocial: 'Gomería Norte', cuit: null, condicionFiscal: null, condicionPago: 'Contado',
      telefonos: ['351 333-3333'], direcciones: [],
    });
    alta.flush({ codigo: 'CLI-002' });
    h.fixture.detectChanges();

    expect(h.raiz().querySelector('[role="status"]')?.textContent).toContain('CLI-002');
  });

  it('CA-004 · si la API rechaza el alta (CUIT repetido), muestra su mensaje y no cierra el formulario', () => {
    h.boton('Nuevo cliente').click();
    h.fixture.detectChanges();
    h.escribir(h.control('Código'), 'CLI-003');
    h.escribir(h.control('Razón social'), 'Otro taller');
    h.escribir(h.control('CUIT'), '30-12345678-9');
    h.escribir(h.control('Condición de pago'), 'Contado');

    h.boton('Guardar').click();
    h.servidor.expectOne(r => r.method === 'POST' && r.url === '/api/clientes').flush(
      { status: 409, detail: 'Ya existe un cliente con el CUIT 30-12345678-9.' }, { status: 409, statusText: 'Conflict' });
    h.fixture.detectChanges();

    expect(h.raiz().querySelector('[role="alert"]')?.textContent).toContain('Ya existe un cliente con el CUIT 30-12345678-9.');
    expect(h.control('Código').value).toBe('CLI-003');
  });

  it('CA-002 · la ficha muestra todos los teléfonos y direcciones, y se modifican por separado', () => {
    abrirFicha();
    for (const dato of [...FICHA.telefonos, ...FICHA.direcciones]) expect(h.raiz().textContent).toContain(dato);

    h.boton('Modificar').click();
    h.fixture.detectChanges();
    expect(h.control('Condición fiscal').value).toBe('MONOTRIBUTO');
    h.raiz().querySelector<HTMLButtonElement>('button[aria-label="Quitar Teléfonos 1"]')!.click();
    h.fixture.detectChanges();

    h.boton('Guardar').click();
    const cambio = h.servidor.expectOne(r => r.method === 'PUT' && r.url === '/api/clientes/4');
    expect(cambio.request.body).toEqual({
      codigo: 'CLI-001', razonSocial: 'Taller San Vicente', cuit: '30-12345678-9', condicionFiscal: 'MONOTRIBUTO',
      condicionPago: 'Contado', telefonos: ['351 222-2222'], direcciones: ['Av. Colón 100', 'Bv. San Juan 200'],
    });
    cambio.flush(null);
    h.servidor.expectOne('/api/clientes/4').flush(FICHA);
  });

  // DarDeBajaClienteUseCase compara con el CUIT si el cliente lo tiene, y si no con el código.
  for (const [caso, ficha, clave] of [
    ['con CUIT, reescribiendo el CUIT', FICHA, '30-12345678-9'],
    ['sin CUIT, reescribiendo el código', { ...FICHA, cuit: null }, 'CLI-001'],
  ] as const) {
    it(`CA-001 · da de baja ${caso} (044), con motivo, y recarga la ficha`, () => {
      abrirFicha(ficha);
      h.boton('Dar de baja').click();
      h.fixture.detectChanges();
      h.escribir(h.control('Motivo', h.dialogo()), 'Cerró el local');
      h.escribir(h.control(`Para confirmar, escribí ${clave}`, h.dialogo()), clave.toLowerCase());

      h.boton('Dar de baja', h.dialogo()).click();
      const baja = h.servidor.expectOne('/api/clientes/4/baja');
      expect(baja.request.method).toBe('POST');
      expect(baja.request.body).toEqual({ confirmacion: clave, motivo: 'Cerró el local' });
      baja.flush(null);
      h.servidor.expectOne('/api/clientes/4').flush({ ...ficha, activo: false });
    });
  }

  // verificador-tdd, 05/11/2026: los errores de baja, reactivación y ficha no los afirmaba ningún caso.
  it('si la API rechaza la baja, muestra su mensaje en el diálogo y no recarga la ficha', () => {
    abrirFicha({ ...FICHA, cuit: null });
    h.boton('Dar de baja').click();
    h.fixture.detectChanges();
    h.escribir(h.control('Motivo', h.dialogo()), 'Cerró el local');
    h.escribir(h.control('Para confirmar, escribí CLI-001', h.dialogo()), 'CLI-001');

    h.boton('Dar de baja', h.dialogo()).click();
    h.servidor.expectOne('/api/clientes/4/baja').flush(
      { status: 400, detail: 'El valor reescrito no coincide con el código o CUIT del cliente.' }, { status: 400, statusText: 'Bad Request' });
    h.fixture.detectChanges();

    expect(h.dialogo().querySelector('[role="alert"]')?.textContent).toContain('El valor reescrito no coincide');
    h.servidor.expectNone('/api/clientes/4');
  });

  it('si la API rechaza la reactivación, muestra su mensaje', () => {
    abrirFicha({ ...FICHA, activo: false });
    h.boton('Reactivar').click();
    h.servidor.expectOne('/api/clientes/4/reactivacion').flush(
      { status: 400, detail: 'El cliente ya está activo.' }, { status: 400, statusText: 'Bad Request' });
    h.fixture.detectChanges();

    expect(h.raiz().querySelector('[role="alert"]')?.textContent).toContain('El cliente ya está activo.');
  });

  it('si la ficha no se puede abrir, muestra el mensaje de la API', () => {
    h.buscar('taller');
    h.servidor.expectOne(r => r.url === '/api/clientes').flush(pagina([FILA]));
    h.fixture.detectChanges();
    h.boton('Ver ficha').click();
    h.servidor.expectOne('/api/clientes/4').flush(
      { status: 404, detail: 'El cliente indicado no existe.' }, { status: 404, statusText: 'Not Found' });
    h.fixture.detectChanges();

    expect(h.raiz().querySelector('[role="alert"]')?.textContent).toContain('El cliente indicado no existe.');
  });

  it('un cliente dado de baja no se modifica: se reactiva', () => {
    abrirFicha({ ...FICHA, activo: false });
    expect(Array.from(h.raiz().querySelectorAll('button')).some(b => b.textContent?.trim() === 'Modificar')).toBeFalse();

    h.boton('Reactivar').click();
    const pedido = h.servidor.expectOne('/api/clientes/4/reactivacion');
    expect(pedido.request.method).toBe('POST');
    pedido.flush(null);
    h.servidor.expectOne('/api/clientes/4').flush(FICHA);
  });
});

describe('Proveedores (045)', () => {
  const h = preparar(Proveedores);
  const FICHA: FichaTercero = {
    codigo: 'PRV-001', razonSocial: 'Autopartes del Centro', cuit: '30-11111111-1',
    urlReferencia: 'https://autopartes.example/catalogo', activo: true, motivoBaja: null,
    telefonos: ['351 444-4444'], direcciones: ['Ruta 9 km 5'],
  };

  function abrirFicha() {
    h.buscar('autopartes');
    h.servidor.expectOne(r => r.url === '/api/proveedores').flush(
      pagina([{ id: 8, codigo: 'PRV-001', razonSocial: 'Autopartes del Centro', cuit: '30-11111111-1', activo: true }]));
    h.fixture.detectChanges();
    h.boton('Ver ficha').click();
    h.servidor.expectOne('/api/proveedores/8').flush(FICHA);
    h.fixture.detectChanges();
  }

  it('CA-003 · la ficha muestra la URL de referencia y la abre en otra pestaña', () => {
    abrirFicha();
    const enlace = h.raiz().querySelector<HTMLAnchorElement>('a[href="https://autopartes.example/catalogo"]');
    expect(enlace?.textContent).toContain('https://autopartes.example/catalogo');
    expect(enlace?.target).toBe('_blank');
    expect(enlace?.rel).toContain('noopener');
  });

  it('la modificación no manda el código: el endpoint no lo cambia', () => {
    abrirFicha();
    h.boton('Modificar').click();
    h.fixture.detectChanges();
    h.escribir(h.control('URL de referencia'), '');

    h.boton('Guardar').click();
    const cambio = h.servidor.expectOne(r => r.method === 'PUT' && r.url === '/api/proveedores/8');
    expect(cambio.request.body).toEqual({
      razonSocial: 'Autopartes del Centro', cuit: '30-11111111-1', urlReferencia: null,
      telefonos: ['351 444-4444'], direcciones: ['Ruta 9 km 5'],
    });
    cambio.flush(null);
    h.servidor.expectOne('/api/proveedores/8').flush(FICHA);
  });

  it('CA-001 · la baja manda el motivo como motivoBaja, que es lo que pide el endpoint de proveedores', () => {
    abrirFicha();
    h.boton('Dar de baja').click();
    h.fixture.detectChanges();
    h.escribir(h.control('Motivo', h.dialogo()), 'Dejó de operar');
    h.escribir(h.control('Para confirmar, escribí PRV-001', h.dialogo()), 'PRV-001');

    h.boton('Dar de baja', h.dialogo()).click();
    const baja = h.servidor.expectOne('/api/proveedores/8/baja');
    expect(baja.request.body).toEqual({ confirmacion: 'PRV-001', motivoBaja: 'Dejó de operar' });
    baja.flush(null);
    h.servidor.expectOne('/api/proveedores/8').flush({ ...FICHA, activo: false, motivoBaja: 'Dejó de operar' });
  });
});
