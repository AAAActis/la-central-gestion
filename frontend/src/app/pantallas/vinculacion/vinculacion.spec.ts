import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CandidatoCatalogo, ResultadoCandidatos } from '../../nucleo/api';
import { Vinculacion } from './vinculacion';

/** 053 · HU-CMP-06: candidatos de vinculación, por el camino real hasta la API. */
describe('Vinculacion · HU-CMP-06 (053)', () => {
  let fixture: ComponentFixture<Vinculacion>;
  let servidor: HttpTestingController;
  const raiz = () => fixture.nativeElement as HTMLElement;

  const fila = (id: number, parecido: number | null = null, vinculado: CandidatoCatalogo['vinculado'] = null): CandidatoCatalogo => ({
    id, proveedorId: 7, proveedor: 'Expoyer', codigo: `C-${id}`, nombre: `Bomba ${id}`,
    numeroOriginal: null, marca: 'SKF', parecido, vinculado,
  });

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [Vinculacion], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(Vinculacion);
    fixture.detectChanges();
  });

  afterEach(() => servidor.verify());

  function buscar(texto: string, respuesta: ResultadoCandidatos) {
    fixture.componentInstance.texto = texto;
    raiz().querySelector<HTMLFormElement>('form.filtros')!.dispatchEvent(new Event('submit'));
    const pedido = servidor.expectOne(r => r.url === '/api/catalogo/candidatos');
    expect(pedido.request.params.get('texto')).toBe(texto.trim());
    expect(pedido.request.params.has('articuloId')).toBeFalse();
    pedido.flush(respuesta);
    fixture.detectChanges();
  }

  const ids = (seccion: string) =>
    Array.from(raiz().querySelectorAll(`section.${seccion} tbody tr`)).map(tr => Number(tr.getAttribute('data-id')));

  it('CA1: muestra las exactas y, en una sección aparte, las aproximadas en el orden que vienen, con su parecido', () => {
    buscar(' bomba agua ', { texto: 'bomba agua', exactas: [fila(1)], aproximadas: [fila(2, 0.82), fila(3, 0.41)], mensaje: null });

    expect(ids('exactas')).toEqual([1]);
    expect(ids('aproximadas')).toEqual([2, 3]);
    const aproximadas = raiz().querySelector('section.aproximadas')!.textContent!;
    expect(aproximadas).toContain('82 %');
    expect(aproximadas).toContain('41 %');
    expect(raiz().querySelector('section.exactas thead')!.textContent).not.toContain('Parecido');
  });

  it('CA4: la fila ya vinculada dice a qué artículo; las otras, que no están vinculadas', () => {
    buscar('termostato', {
      texto: 'termostato', exactas: [fila(1, null, { id: 9, codigoInterno: 'ART-9', nombre: 'Termostato' })],
      aproximadas: [fila(2, 0.5)], mensaje: null,
    });

    const vinculada = raiz().querySelector('section.exactas tbody tr')!;
    expect(vinculada.querySelector('.vinculada')).not.toBeNull();
    expect(vinculada.textContent).toContain('ART-9');
    expect(raiz().querySelector('section.aproximadas tbody tr .vinculada')).toBeNull();
    expect(raiz().querySelector('section.aproximadas tbody tr')!.textContent).toContain('Sin vincular');
  });

  it('CA6: sin candidatos, muestra el mensaje con el texto buscado en lugar de tablas vacías', () => {
    buscar('amortiguador', { texto: 'amortiguador', exactas: [], aproximadas: [], mensaje: 'No hay candidatos para «amortiguador».' });

    expect(raiz().querySelector('[role="status"]')!.textContent).toContain('No hay candidatos para «amortiguador».');
    expect(raiz().querySelector('table')).toBeNull();
  });

  it('con texto vacío no consulta', () => {
    fixture.componentInstance.texto = '   ';
    raiz().querySelector<HTMLFormElement>('form.filtros')!.dispatchEvent(new Event('submit'));
    servidor.expectNone('/api/catalogo/candidatos');
  });

  it('el botón queda deshabilitado mientras busca, y un error se muestra como alerta', () => {
    fixture.componentInstance.texto = 'bomba';
    raiz().querySelector<HTMLFormElement>('form.filtros')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
    expect(raiz().querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBeTrue();

    servidor.expectOne(r => r.url === '/api/catalogo/candidatos')
      .flush({ detail: 'Ingresá un texto o elegí un artículo para buscar candidatos.' }, { status: 400, statusText: 'Bad Request' });
    fixture.detectChanges();

    expect(raiz().querySelector<HTMLButtonElement>('button[type="submit"]')!.disabled).toBeFalse();
    expect(raiz().querySelector('[role="alert"]')).not.toBeNull();
  });
});
