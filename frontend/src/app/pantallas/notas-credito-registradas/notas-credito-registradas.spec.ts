import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NotaCreditoCompraResumen } from '../../nucleo/api';
import { NotasCreditoRegistradas } from './notas-credito-registradas';

/** HU-FCO-07 (057) CA-012 · listado de notas de crédito y anulación, por el camino real hasta la API. */
describe('NotasCreditoRegistradas · 057 CA-012', () => {
  let fixture: ComponentFixture<NotasCreditoRegistradas>;
  let c: NotasCreditoRegistradas;
  let servidor: HttpTestingController;
  const raiz = () => fixture.nativeElement as HTMLElement;
  const URL = '/api/notas-credito-compra';

  const nota = (id: number, numero: string, anulada = false, clase: NotaCreditoCompraResumen['clase'] = 'BONIFICACION'): NotaCreditoCompraResumen => ({
    id, proveedorId: 7, proveedor: 'Lubricantes del Sur', numero, clase, fechaEmision: '2026-11-05', importeNeto: 1234.5, anulada,
  });
  const pagina = (items: NotaCreditoCompraResumen[], total = items.length, numero = 1) => ({ items, total, pagina: numero, tamanoPagina: 20 });

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [NotasCreditoRegistradas], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(NotasCreditoRegistradas);
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

  it('filtra por proveedor y rango de emisión, y solo manda los criterios con valor', () => {
    elegirProveedor();
    c.emisionDesde = '2026-11-01';
    c.emisionHasta = '2026-11-30';
    buscar();

    const pedido = servidor.expectOne(r => r.url === URL);
    expect(pedido.request.method).toBe('GET');
    expect(['proveedorId', 'emisionDesde', 'emisionHasta', 'pagina', 'tamanoPagina'].map(p => pedido.request.params.get(p)))
      .toEqual(['7', '2026-11-01', '2026-11-30', '1', '20']);
    pedido.flush(pagina([nota(1, '0010-00004321', false, 'DEVOLUCION')]));
    fixture.detectChanges();

    const filas = raiz().querySelectorAll('tbody tr');
    expect(filas.length).toBe(1);
    expect(filas[0].textContent).toContain('0010-00004321');
    expect(filas[0].textContent).toContain('Lubricantes del Sur');
    expect(filas[0].textContent).toContain('Devolución');
    expect(filas[0].textContent).toContain('1,234.50');
  });

  it('sin criterios no manda proveedor ni fechas; quitar el proveedor deja de filtrar por él', () => {
    elegirProveedor();
    Array.from(raiz().querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.includes('Quitar'))!.click();
    fixture.detectChanges();
    buscar();

    const pedido = servidor.expectOne(r => r.url === URL);
    expect(['proveedorId', 'emisionDesde', 'emisionHasta'].map(p => pedido.request.params.has(p))).toEqual([false, false, false]);
    pedido.flush(pagina([]));
  });

  it('sin coincidencias muestra «Sin resultados»', () => {
    buscar();
    servidor.expectOne(r => r.url === URL).flush(pagina([]));
    fixture.detectChanges();

    expect(raiz().querySelector('[role="status"]')?.textContent).toContain('Sin resultados');
    expect(raiz().querySelector('tbody')).toBeNull();
  });

  it('las anuladas aparecen marcadas y no ofrecen «Anular»; las vigentes, sí', () => {
    buscar();
    servidor.expectOne(r => r.url === URL).flush(pagina([nota(1, 'N-1'), nota(2, 'N-2', true)]));
    fixture.detectChanges();

    const [vigente, anulada] = Array.from(raiz().querySelectorAll('tbody tr'));
    expect(anulada.classList).toContain('anulada');
    expect(anulada.textContent).toContain('Anulada');
    expect(vigente.classList).not.toContain('anulada');
    expect(vigente.textContent).toContain('Vigente');
    expect(vigente.textContent).toContain('Bonificación');
    expect(botonEn(vigente, 'Anular')).toBeDefined();
    expect(botonEn(anulada, 'Anular')).toBeUndefined();
  });

  it('el error del servidor se muestra y no deja la tabla anterior', () => {
    buscar();
    servidor.expectOne(r => r.url === URL).flush(pagina([nota(1, 'N-1')]));
    fixture.detectChanges();
    expect(raiz().querySelector('tbody')).not.toBeNull();

    buscar();
    servidor.expectOne(r => r.url === URL)
      .flush({ detail: 'El rango de emisión empieza después de terminar.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(raiz().querySelector('[role="alert"]')?.textContent).toContain('El rango de emisión empieza después de terminar.');
    expect(raiz().querySelector('tbody')).toBeNull();

    // Al volver a buscar, mientras el pedido está en vuelo, no reaparece la tabla vieja.
    buscar();
    fixture.detectChanges();
    expect(raiz().querySelector('tbody')).toBeNull();
    servidor.expectOne(r => r.url === URL).flush(pagina([]));
  });

  it('una fecha borrada no viaja como criterio vacío', () => {
    c.emisionDesde = '2026-11-01';
    c.emisionHasta = '2026-11-30';
    buscar();
    servidor.expectOne(r => r.url === URL).flush(pagina([]));
    c.emisionDesde = '';
    c.emisionHasta = '  ';
    buscar();

    const pedido = servidor.expectOne(r => r.url === URL);
    expect([pedido.request.params.has('emisionDesde'), pedido.request.params.has('emisionHasta')]).toEqual([false, false]);
    pedido.flush(pagina([]));
  });

  it('pagina: «Siguiente» pide la página 2 con los criterios de la búsqueda, no con lo que se escribió después', () => {
    const boton = (texto: string) => Array.from(raiz().querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.includes(texto))!;
    c.emisionDesde = '2026-11-01';
    buscar();
    servidor.expectOne(r => r.url === URL).flush(pagina([nota(1, 'N-1')], 21));
    fixture.detectChanges();
    expect([boton('Anterior').disabled, boton('Siguiente').disabled]).toEqual([true, false]);
    c.emisionDesde = '2026-12-01';

    boton('Siguiente').click();
    const pedido = servidor.expectOne(r => r.url === URL);
    expect([pedido.request.params.get('pagina'), pedido.request.params.get('emisionDesde')]).toEqual(['2', '2026-11-01']);
    pedido.flush(pagina([nota(2, 'N-2')], 21, 2));
    fixture.detectChanges();
    expect([boton('Anterior').disabled, boton('Siguiente').disabled]).toEqual([false, true]);
  });

  // ── la anulación, como la 028 (D-S6-23) ───────────────────────────────────────────────
  const dialogo = () => raiz().querySelector<HTMLDialogElement>('dialog[open]');
  const botonEn = (dentro: ParentNode, texto: string) =>
    Array.from(dentro.querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.includes(texto));
  const campo = (etiqueta: string) =>
    Array.from(dialogo()!.querySelectorAll('label')).find(l => l.textContent?.trim() === etiqueta)?.control as HTMLInputElement;
  const escribir = (control: HTMLInputElement, valor: string) => {
    control.value = valor;
    control.dispatchEvent(new Event('input'));
  };
  function listar(items: NotaCreditoCompraResumen[], total = items.length, numero = 1) {
    c.emisionDesde = '2026-11-01';
    buscar();
    servidor.expectOne(r => r.url === URL).flush(pagina(items, total, numero));
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

  it('anula con motivo sin autorización de otra persona (D-S7-04), y el listado la muestra anulada', () => {
    listar([nota(21, 'N-21'), nota(22, 'N-22')], 25, 2);
    abrirAnulacion(0);
    expect(dialogo()!.querySelector('h2')?.textContent).toContain('N-21');
    // D-S7-04: sin doble certificación, el diálogo pide solo el motivo.
    expect(Array.from(dialogo()!.querySelectorAll('label')).map(l => l.textContent!.trim())).toEqual(['Motivo']);

    completarYConfirmar('Cargada dos veces');

    const anulacion = servidor.expectOne(`${URL}/21/anulacion`);
    expect(anulacion.request.method).toBe('POST');
    expect(anulacion.request.body).toEqual({ motivo: 'Cargada dos veces' });
    expect(botonEn(dialogo()!, 'Anular')?.disabled).withContext('confirmar deshabilitado mientras envía').toBeTrue();
    anulacion.flush(null);
    fixture.detectChanges();

    expect(dialogo()).withContext('el diálogo se cierra').toBeNull();
    const recarga = servidor.expectOne(r => r.url === URL);
    expect([recarga.request.params.get('pagina'), recarga.request.params.get('emisionDesde')]).withContext('recarga la misma página').toEqual(['2', '2026-11-01']);
    recarga.flush(pagina([nota(21, 'N-21', true), nota(22, 'N-22')], 25, 2));
    fixture.detectChanges();
    expect(raiz().querySelector('tbody tr')!.textContent).toContain('Anulada');
    expect(raiz().querySelector('[role="status"]')?.textContent).toContain('Nota de crédito N-21 anulada.');

    // La siguiente anulación arranca con el botón habilitado.
    abrirAnulacion(1);
    expect(botonEn(dialogo()!, 'Anular')?.disabled).toBeFalse();
    botonEn(dialogo()!, 'Cancelar')!.click();
    fixture.detectChanges();

    // Una búsqueda nueva borra el aviso.
    buscar();
    fixture.detectChanges();
    expect(raiz().querySelector('[role="status"]')).toBeNull();
    servidor.expectOne(r => r.url === URL).flush(pagina([nota(22, 'N-22')]));
  });

  it('una devolución avisa en el diálogo que la mercadería vuelve a su origen; una bonificación, no', () => {
    listar([nota(1, 'N-1', false, 'DEVOLUCION'), nota(2, 'N-2')]);
    abrirAnulacion(0);
    expect(dialogo()!.textContent).toContain('vuelve al stock');
    botonEn(dialogo()!, 'Cancelar')!.click();
    fixture.detectChanges();

    abrirAnulacion(1);
    expect(dialogo()!.textContent).not.toContain('vuelve al stock');
  });

  it('si la API rechaza la anulación (un OPERADOR recibe 403) o la nota ya estaba anulada, muestra su mensaje en el diálogo y no recarga', () => {
    listar([nota(1, 'N-1')]);
    abrirAnulacion(0);
    completarYConfirmar('Error de carga');

    servidor.expectOne(`${URL}/1/anulacion`).flush(
      { detail: 'Solo un ADMINISTRADOR puede anular.' },
      { status: 403, statusText: 'Forbidden' });
    fixture.detectChanges();

    expect(dialogo()?.querySelector('[role="alert"]')?.textContent).toContain('Solo un ADMINISTRADOR puede anular.');
    expect(botonEn(dialogo()!, 'Anular')?.disabled).withContext('se puede reintentar').toBeFalse();
    servidor.expectNone(r => r.url === URL);
  });

  it('al abrir la anulación de otra nota, el diálogo no arrastra el motivo ni el error anteriores', () => {
    listar([nota(1, 'N-1'), nota(2, 'N-2')]);
    abrirAnulacion(0);
    completarYConfirmar('Error de carga');
    servidor.expectOne(`${URL}/1/anulacion`).flush({ detail: 'Solo un ADMINISTRADOR puede anular.' }, { status: 403, statusText: 'Forbidden' });
    fixture.detectChanges();
    botonEn(dialogo()!, 'Cancelar')!.click();
    fixture.detectChanges();
    expect(dialogo()).withContext('Cancelar cierra').toBeNull();

    abrirAnulacion(1);
    expect(dialogo()!.querySelector('h2')?.textContent).toContain('N-2');
    expect(campo('Motivo').value).toBe('');
    expect(dialogo()!.querySelector('[role="alert"]')).toBeNull();
  });

  it('anular la segunda fila manda el id de esa nota', () => {
    listar([nota(1, 'N-1'), nota(2, 'N-2')]);
    abrirAnulacion(1);
    completarYConfirmar('Error de carga');

    servidor.expectOne(`${URL}/2/anulacion`).flush(null);
    servidor.expectOne(r => r.url === URL).flush(pagina([nota(1, 'N-1'), nota(2, 'N-2', true)]));
  });

  it('sin motivo no manda la anulación', () => {
    listar([nota(1, 'N-1')]);
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
