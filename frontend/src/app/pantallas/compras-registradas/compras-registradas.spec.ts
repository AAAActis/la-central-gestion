import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FacturaCompraResumen } from '../../nucleo/api';
import { ComprasRegistradas } from './compras-registradas';

/** HU-FCO-02 (026): consulta de facturas de compra, por el camino real hasta la API. */
describe('ComprasRegistradas · HU-FCO-02', () => {
  let fixture: ComponentFixture<ComprasRegistradas>;
  let c: ComprasRegistradas;
  let servidor: HttpTestingController;
  const raiz = () => fixture.nativeElement as HTMLElement;

  const factura = (id: number, numero: string, anulada = false): FacturaCompraResumen => ({
    id, proveedorId: 7, proveedor: 'Lubricantes del Sur', numero, tipoComprobante: 'A',
    fechaEmision: '2026-10-01', fechaVencimiento: '2026-10-31', anulada,
  });
  const pagina = (items: FacturaCompraResumen[], total = items.length, numero = 1) =>
    ({ items, total, pagina: numero, tamanoPagina: 20 });

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [ComprasRegistradas], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ComprasRegistradas);
    c = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  function buscar() {
    raiz().querySelector<HTMLFormElement>('form.filtros')!.dispatchEvent(new Event('submit'));
  }

  function elegirProveedor() {
    c.textoProveedor = 'lubri';
    c.buscarProveedor();
    servidor.expectOne(r => r.url === '/api/proveedores').flush({
      items: [{ id: 7, codigo: '00035', razonSocial: 'Lubricantes del Sur', cuit: null, urlReferencia: null, activo: true, tipoComprobanteHabitual: null }],
      total: 1, pagina: 1, tamanoPagina: 20,
    });
    fixture.detectChanges();
    Array.from(raiz().querySelectorAll<HTMLButtonElement>('button.opcion')).find(b => b.textContent?.includes('Lubricantes'))!.click();
    fixture.detectChanges();
  }

  // CA-001: número y proveedor.
  it('CA-001: busca por número y proveedor, y solo manda los criterios con valor', () => {
    elegirProveedor();
    c.numero = ' A-0001-00000001 ';
    buscar();

    const pedido = servidor.expectOne(r => r.url === '/api/facturas-compra');
    expect(pedido.request.params.get('proveedorId')).toBe('7');
    expect(pedido.request.params.get('numero')).toBe('A-0001-00000001');
    expect(pedido.request.params.get('pagina')).toBe('1');
    expect(pedido.request.params.get('tamanoPagina')).toBe('20');
    expect(pedido.request.params.has('emisionDesde')).toBeFalse();
    expect(pedido.request.params.has('vencimientoHasta')).toBeFalse();
    pedido.flush(pagina([factura(1, 'A-0001-00000001')]));
    fixture.detectChanges();

    const filas = raiz().querySelectorAll('tbody tr');
    expect(filas.length).toBe(1);
    expect(filas[0].textContent).toContain('A-0001-00000001');
    expect(filas[0].textContent).toContain('Lubricantes del Sur');
  });

  // CA-002: por proveedor, la lista completa.
  it('CA-002: por proveedor muestra todas sus facturas', () => {
    elegirProveedor();
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra' && r.params.get('proveedorId') === '7' && !r.params.has('numero'))
      .flush(pagina([factura(1, 'A-1'), factura(2, 'A-2'), factura(3, 'B-3')]));
    fixture.detectChanges();

    expect(raiz().querySelectorAll('tbody tr').length).toBe(3);
  });

  it('CA-002: quitar el proveedor elegido deja de filtrar por él', () => {
    elegirProveedor();
    Array.from(raiz().querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.includes('Quitar'))!.click();
    fixture.detectChanges();
    buscar();

    servidor.expectOne(r => r.url === '/api/facturas-compra' && !r.params.has('proveedorId')).flush(pagina([]));
  });

  // CA-003: rangos de emisión y de vencimiento.
  it('CA-003: manda los rangos de emisión y de vencimiento', () => {
    c.emisionDesde = '2026-10-01';
    c.emisionHasta = '2026-10-31';
    c.vencimientoDesde = '2026-11-01';
    c.vencimientoHasta = '2026-11-30';
    buscar();

    const pedido = servidor.expectOne(r => r.url === '/api/facturas-compra');
    expect(['emisionDesde', 'emisionHasta', 'vencimientoDesde', 'vencimientoHasta'].map(p => pedido.request.params.get(p)))
      .toEqual(['2026-10-01', '2026-10-31', '2026-11-01', '2026-11-30']);
    pedido.flush(pagina([]));
  });

  // CA-004: sin coincidencias.
  it('CA-004: sin coincidencias muestra «Sin resultados»', () => {
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([]));
    fixture.detectChanges();

    expect(raiz().querySelector('[role="status"]')?.textContent).toContain('Sin resultados');
    expect(raiz().querySelector('tbody')).toBeNull();
  });

  // CA-005: las anuladas, marcadas.
  it('CA-005: las anuladas aparecen marcadas como anuladas', () => {
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([factura(1, 'A-1'), factura(2, 'A-2', true)]));
    fixture.detectChanges();

    const [activa, anulada] = Array.from(raiz().querySelectorAll('tbody tr'));
    expect(anulada.classList).toContain('anulada');
    expect(anulada.textContent).toContain('Anulada');
    expect(activa.classList).not.toContain('anulada');
    expect(activa.textContent).not.toContain('Anulada');
  });

  it('el error del servidor se muestra y no deja la tabla anterior', () => {
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([factura(1, 'A-1')]));
    fixture.detectChanges();
    expect(raiz().querySelector('tbody')).not.toBeNull();

    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra')
      .flush({ detail: 'El rango de emisión empieza después de terminar.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(raiz().querySelector('[role="alert"]')?.textContent).toContain('El rango de emisión empieza después de terminar.');
    expect(raiz().querySelector('tbody')).toBeNull();

    // Verificador ronda 2: al volver a buscar, mientras el pedido está en vuelo, no reaparece la tabla vieja.
    buscar();
    fixture.detectChanges();
    expect(raiz().querySelector('tbody')).toBeNull();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([]));
  });

  // Verificador ronda 1: los extremos de la paginación.
  it('pagina: en la primera no se puede ir atrás y en la última no se puede ir adelante', () => {
    const boton = (texto: string) => Array.from(raiz().querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.includes(texto))!;
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([factura(1, 'A-1')], 21));
    fixture.detectChanges();
    expect([boton('Anterior').disabled, boton('Siguiente').disabled]).toEqual([true, false]);

    boton('Siguiente').click();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([factura(2, 'A-2')], 21, 2));
    fixture.detectChanges();
    expect([boton('Anterior').disabled, boton('Siguiente').disabled]).toEqual([false, true]);
  });

  it('pagina: «Siguiente» pide la página 2 con los mismos criterios', () => {
    c.numero = 'A-1';
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([factura(1, 'A-1')], 25));
    fixture.detectChanges();

    Array.from(raiz().querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.includes('Siguiente'))!.click();

    const pedido = servidor.expectOne(r => r.url === '/api/facturas-compra');
    expect([pedido.request.params.get('pagina'), pedido.request.params.get('numero')]).toEqual(['2', 'A-1']);
    pedido.flush(pagina([factura(2, 'A-2')], 25, 2));
  });
  // ── HU-FCO-04 (028) parte 2: la anulación desde la consulta ─────────────────────────────
  const dialogo = () => raiz().querySelector<HTMLDialogElement>('dialog[open]');
  const botonEn = (dentro: ParentNode, texto: string) =>
    Array.from(dentro.querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.includes(texto));
  const campo = (etiqueta: string) =>
    Array.from(dialogo()!.querySelectorAll('label')).find(l => l.textContent?.trim() === etiqueta)?.control as HTMLInputElement;
  const escribir = (control: HTMLInputElement, valor: string) => {
    control.value = valor;
    control.dispatchEvent(new Event('input'));
  };
  function listar(items: FacturaCompraResumen[], total = items.length, numero = 1) {
    c.numero = 'A-1';
    buscar();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina(items, total, numero));
    fixture.detectChanges();
  }
  function abrirAnulacion(fila: number) {
    const anular = botonEn(raiz().querySelectorAll('tbody tr')[fila], 'Anular');
    expect(anular).withContext('botón «Anular» de la fila').toBeDefined();
    anular?.click();
    fixture.detectChanges();
    expect(dialogo()).withContext('diálogo de anulación').not.toBeNull();
  }
  function completarYConfirmar(motivo: string) {
    escribir(campo('Motivo'), motivo);
    fixture.detectChanges();
    botonEn(dialogo()!, 'Anular')!.click();
    fixture.detectChanges();
  }

  it('028 CA-005: una factura anulada no ofrece «Anular»; una activa, sí', () => {
    listar([factura(1, 'A-1'), factura(2, 'A-2', true)]);

    const [activa, anulada] = Array.from(raiz().querySelectorAll('tbody tr'));
    expect(botonEn(activa, 'Anular')).toBeDefined();
    expect(botonEn(anulada, 'Anular')).toBeUndefined();
  });

  it('028 CA-001, CA-006: anula con motivo sin autorización de otra persona (D-S7-04), y la consulta la muestra anulada', () => {
    listar([factura(21, 'A-21'), factura(22, 'A-22')], 25, 2);
    abrirAnulacion(0);
    // D-S7-04: sin doble certificación, el diálogo pide solo el motivo.
    expect(Array.from(dialogo()!.querySelectorAll('label')).map(l => l.textContent!.trim())).toEqual(['Motivo']);

    completarYConfirmar('Cargada dos veces');

    const anulacion = servidor.expectOne('/api/facturas-compra/21/anulacion');
    expect(anulacion.request.method).toBe('POST');
    expect(anulacion.request.body).toEqual({ motivo: 'Cargada dos veces' });
    expect(botonEn(dialogo()!, 'Anular')?.disabled).withContext('confirmar deshabilitado mientras envía').toBeTrue();
    anulacion.flush(null);
    fixture.detectChanges();

    expect(dialogo()).withContext('el diálogo se cierra').toBeNull();
    const recarga = servidor.expectOne(r => r.url === '/api/facturas-compra');
    expect([recarga.request.params.get('pagina'), recarga.request.params.get('numero')]).withContext('recarga la misma página').toEqual(['2', 'A-1']);
    recarga.flush(pagina([factura(21, 'A-21', true), factura(22, 'A-22')], 25, 2));
    fixture.detectChanges();
    expect(raiz().querySelector('tbody tr')!.textContent).toContain('Anulada');
    expect(raiz().querySelector('[role="status"]')?.textContent).toContain('Factura A-21 anulada.');

    // La siguiente anulación arranca con el botón habilitado.
    abrirAnulacion(1);
    expect(botonEn(dialogo()!, 'Anular')?.disabled).toBeFalse();
    botonEn(dialogo()!, 'Cancelar')!.click();
    fixture.detectChanges();

    // Una búsqueda nueva borra el aviso.
    buscar();
    fixture.detectChanges();
    expect(raiz().querySelector('[role="status"]')).toBeNull();
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([]));
  });

  it('028 CA-006: si la API rechaza la anulación (un OPERADOR recibe 403), muestra su mensaje en el diálogo y no recarga', () => {
    listar([factura(1, 'A-1')]);
    abrirAnulacion(0);
    completarYConfirmar('Error de carga');

    servidor.expectOne('/api/facturas-compra/1/anulacion').flush(
      { detail: 'Solo un ADMINISTRADOR puede anular.' },
      { status: 403, statusText: 'Forbidden' });
    fixture.detectChanges();

    expect(dialogo()?.querySelector('[role="alert"]')?.textContent).toContain('Solo un ADMINISTRADOR puede anular.');
    expect(botonEn(dialogo()!, 'Anular')?.disabled).withContext('se puede reintentar').toBeFalse();
    servidor.expectNone(r => r.url === '/api/facturas-compra');
  });

  it('028: al abrir la anulación de otra factura, el diálogo no arrastra el motivo ni el error anteriores', () => {
    listar([factura(1, 'A-1'), factura(2, 'A-2')]);
    abrirAnulacion(0);
    completarYConfirmar('Error de carga');
    servidor.expectOne('/api/facturas-compra/1/anulacion')
      .flush({ detail: 'Solo un ADMINISTRADOR puede anular.' }, { status: 403, statusText: 'Forbidden' });
    fixture.detectChanges();
    botonEn(dialogo()!, 'Cancelar')!.click();
    fixture.detectChanges();
    expect(dialogo()).withContext('Cancelar cierra').toBeNull();

    abrirAnulacion(1);
    expect(dialogo()!.querySelector('h2')?.textContent).toContain('A-2');
    expect(campo('Motivo').value).toBe('');
    expect(dialogo()!.querySelector('[role="alert"]')).toBeNull();
  });

  // Verificador ronda 1: la anulación pega al id de la fila elegida, no a la primera.
  it('028: anular la segunda fila manda el id de esa factura', () => {
    listar([factura(1, 'A-1'), factura(2, 'A-2')]);
    abrirAnulacion(1);
    completarYConfirmar('Error de carga');

    servidor.expectOne('/api/facturas-compra/2/anulacion').flush(null);
    servidor.expectOne(r => r.url === '/api/facturas-compra').flush(pagina([factura(1, 'A-1'), factura(2, 'A-2', true)]));
  });
  it('sin motivo no manda la anulación', () => {
    listar([factura(1, 'A-1')]);
    abrirAnulacion(0);
    completarYConfirmar('');

    servidor.expectNone(r => r.url.endsWith('/anulacion'));
    expect(dialogo()).withContext('el diálogo sigue abierto').not.toBeNull();
  });

  describe('Enter sobre un código exacto de proveedor lo elige', () => {
    const proveedor = (id: number, codigo: string) =>
      ({ id, codigo, razonSocial: `Proveedor ${codigo}`, cuit: null, urlReferencia: null, activo: true, tipoComprobanteHabitual: null });
    const opciones = () => raiz().querySelectorAll('button.opcion').length;
    const campo = () => raiz().querySelector<HTMLInputElement>('input[name="textoProveedor"]');

    function enter(texto: string) {
      c.textoProveedor = texto;
      campo()!.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    }

    function responder(items: ReturnType<typeof proveedor>[]) {
      servidor.expectOne(r => r.url === '/api/proveedores').flush({ items, total: items.length, pagina: 1, tamanoPagina: 20 });
      fixture.detectChanges();
    }

    it('«35» elige al 00035 sin dejar la lista ni el texto', () => {
      enter(' 35 ');
      responder([proveedor(8, '00350'), proveedor(7, '00035')]);

      expect(c.proveedor()?.id).toBe(7);
      expect(c.textoProveedor).toBe('');
      expect(opciones()).toBe(0);
      expect(campo()).toBeNull();

      // La lista está oculta mientras hay proveedor: al quitarlo no tiene que volver.
      c.proveedor.set(null);
      fixture.detectChanges();
      expect(opciones()).toBe(0);
    });

    it('con 035 y 00035 los lista y no elige', () => {
      enter('35');
      responder([proveedor(7, '00035'), proveedor(9, '035')]);

      expect(c.proveedor()).toBeNull();
      expect(opciones()).toBe(2);
    });

    it('sin código exacto lista como siempre', () => {
      enter('35');
      responder([proveedor(8, '00350')]);

      expect(c.proveedor()).toBeNull();
      expect(opciones()).toBe(1);
    });

    it('el botón Buscar proveedor lista aunque el código sea exacto', () => {
      c.textoProveedor = '35';
      Array.from(raiz().querySelectorAll('button')).find(b => b.textContent?.trim() === 'Buscar proveedor')!.click();
      responder([proveedor(7, '00035')]);

      expect(c.proveedor()).toBeNull();
      expect(opciones()).toBe(1);
    });

    it('elige según el texto buscado, aunque se siga escribiendo antes de la respuesta', () => {
      enter('35');
      c.textoProveedor = '350';
      responder([proveedor(8, '00350'), proveedor(7, '00035')]);

      expect(c.proveedor()?.id).toBe(7);
    });

    it('con el campo vacío no consulta', () => {
      enter('   ');

      expect(servidor.match(r => r.url === '/api/proveedores').length).toBe(0);
    });
  });
});
