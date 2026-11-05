import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, Subject, of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { Listado, Pagina } from './listado';

interface Item { id: number; nombre: string; activo: boolean; }

@Component({
  imports: [Listado],
  template: `
    <app-listado [consultar]="consultar" [columnas]="['Nombre']" etiqueta="Buscar cosas" nombrePlural="ítems">
      <ng-template #fila let-item><td>{{ item.nombre }}</td></ng-template>
    </app-listado>
  `,
})
class Anfitrion {
  respuesta: () => Observable<Pagina<Item>> = () => of(pagina([]));
  readonly llamadas: [string, boolean, number, number][] = [];
  readonly consultar = (texto: string, inactivos: boolean, numero: number, tamano: number) => {
    this.llamadas.push([texto, inactivos, numero, tamano]);
    return this.respuesta();
  };
}

function pagina(items: Item[], total = items.length, numero = 1, tamano = 20): Pagina<Item> {
  return { items, total, pagina: numero, tamanoPagina: tamano };
}

const items = (n: number, desde = 1): Item[] =>
  Array.from({ length: n }, (_, i) => ({ id: desde + i, nombre: `Cosa ${desde + i}`, activo: true }));

describe('Listado (HU-UI-04)', () => {
  let fixture: ComponentFixture<Anfitrion>;
  const raiz = () => fixture.nativeElement as HTMLElement;
  const anfitrion = () => fixture.componentInstance;
  const campo = () => raiz().querySelector<HTMLInputElement>('input[type="search"]')!;
  const casilla = () => raiz().querySelector<HTMLInputElement>('input[type="checkbox"]')!;
  const boton = (texto: string) =>
    Array.from(raiz().querySelectorAll<HTMLButtonElement>('button')).find(b => b.textContent?.trim().includes(texto));
  const filas = () => Array.from(raiz().querySelectorAll('tbody tr'));
  const texto = () => raiz().textContent!.replace(/\s+/g, ' ');

  function escribir(valor: string) {
    campo().value = valor;
    campo().dispatchEvent(new Event('input'));
  }
  function buscar(valor: string) {
    escribir(valor);
    raiz().querySelector('form')!.dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  beforeEach(() => {
    fixture = TestBed.createComponent(Anfitrion);
    fixture.autoDetectChanges();
  });

  it('al entrar hay una sola cosa que hacer: el campo de búsqueda, enfocado, sin tabla ni consulta', async () => {
    await fixture.whenStable();
    expect(campo()).not.toBeNull();
    expect(document.activeElement).toBe(campo());
    expect(raiz().querySelector('table')).toBeNull();
    expect(anfitrion().llamadas).toEqual([]);
  });

  it('el campo tiene su etiqueta asociada', () => {
    const etiqueta = raiz().querySelector<HTMLLabelElement>(`label[for="${campo().id}"]`);
    expect(etiqueta?.textContent).toContain('Buscar cosas');
  });

  it('CA-001 · buscar pide la primera página con el texto y muestra las filas de la pantalla', () => {
    anfitrion().respuesta = () => of(pagina(items(2)));
    buscar('bujía');

    expect(anfitrion().llamadas).toEqual([['bujía', false, 1, 20]]);
    expect(filas().map(f => f.querySelector('td')?.textContent)).toEqual(['Cosa 1', 'Cosa 2']);
    expect(texto()).toContain('2 ítems encontrados');
  });

  it('CA-002 · sin coincidencias muestra el mensaje con lo buscado y no una tabla vacía', () => {
    buscar('zx999');

    expect(raiz().querySelector('table')).toBeNull();
    expect(texto()).toContain('Sin resultados para «zx999»');
  });

  it('CA-002 · sin coincidencias y sin inactivos, sugiere incluirlos; con inactivos, no', () => {
    buscar('zx999');
    expect(texto()).toContain('Incluir inactivos');
    const sugerencia = raiz().querySelector('.vacio')?.textContent;
    expect(sugerencia).toContain('dado de baja');

    casilla().click();
    buscar('zx999');
    expect(raiz().querySelector('.vacio')?.textContent).not.toContain('dado de baja');
  });

  it('CA-002 · «Volver a buscar» devuelve el foco al campo', () => {
    buscar('zx999');
    boton('Volver a buscar')?.click();
    expect(document.activeElement).toBe(campo());
  });

  it('CA-003 · con «Incluir inactivos» los pide y los diferencia con clase y con texto', () => {
    anfitrion().respuesta = () => of(pagina([
      { id: 1, nombre: 'Activa', activo: true },
      { id: 2, nombre: 'De baja', activo: false },
    ]));
    casilla().click();
    buscar('cosa');

    expect(anfitrion().llamadas).toEqual([['cosa', true, 1, 20]]);
    const [activa, inactiva] = filas();
    expect(inactiva?.classList).toContain('inactivo');
    expect(inactiva?.textContent).toContain('Inactivo');
    expect(activa?.classList).not.toContain('inactivo');
    expect(activa?.textContent).not.toContain('Inactivo');
  });

  it('CA-004 · pagina en el servidor: pide solo la página y dice cuál es de cuántas', () => {
    anfitrion().respuesta = () => of(pagina(items(20), 45));
    buscar('cosa');

    expect(filas().length).toBe(20);
    expect(texto()).toContain('Mostrando 1–20 de 45 ítems');
    expect(texto()).toContain('1 de 3');
    expect(boton('Anterior')?.disabled).toBeTrue();
    expect(boton('Siguiente')?.disabled).toBeFalse();
  });

  it('CA-004 · «Siguiente» pide la página 2 de la última búsqueda, aunque el campo haya cambiado', () => {
    anfitrion().respuesta = () => of(pagina(items(20), 45));
    casilla().click();
    buscar('cosa');
    escribir('otra cosa');
    casilla().click();

    anfitrion().respuesta = () => of(pagina(items(20, 21), 45, 2));
    boton('Siguiente')?.click();
    fixture.detectChanges();

    expect(anfitrion().llamadas.at(-1)).toEqual(['cosa', true, 2, 20]);
    expect(texto()).toContain('Mostrando 21–40 de 45 ítems');
    expect(boton('Anterior')?.disabled).toBeFalse();
  });

  it('CA-004 · en la última página «Siguiente» queda deshabilitado', () => {
    anfitrion().respuesta = () => of(pagina(items(5, 41), 45, 3));
    buscar('cosa');

    expect(texto()).toContain('Mostrando 41–45 de 45 ítems');
    expect(boton('Siguiente')?.disabled).toBeTrue();
  });

  it('si la API falla, muestra su mensaje como alerta y no deja una tabla vieja', () => {
    anfitrion().respuesta = () => of(pagina(items(2)));
    buscar('cosa');
    anfitrion().respuesta = () => throwError(() =>
      new HttpErrorResponse({ status: 400, error: { detail: 'El texto es demasiado largo.' } }));
    buscar('cosa larga');

    const alerta = raiz().querySelector('[role="alert"]');
    expect(alerta?.textContent).toContain('El texto es demasiado largo.');
    expect(raiz().querySelector('table')).toBeNull();
  });

  it('mientras busca, el botón queda en carga y no se puede volver a enviar', () => {
    const pendiente = new Subject<Pagina<Item>>();
    anfitrion().respuesta = () => pendiente;
    buscar('cosa');

    expect(boton('Buscando')?.getAttribute('aria-busy')).toBe('true');
    expect(boton('Buscando')?.disabled).toBeTrue();
  });

  it('una respuesta vieja no pisa a la de la búsqueda nueva', () => {
    const vieja = new Subject<Pagina<Item>>();
    const nueva = new Subject<Pagina<Item>>();
    const cola = [vieja, nueva];
    anfitrion().respuesta = () => cola.shift()!;
    buscar('vieja');
    buscar('nueva');

    nueva.next(pagina([{ id: 2, nombre: 'Nueva', activo: true }]));
    vieja.next(pagina([{ id: 1, nombre: 'Vieja', activo: true }]));
    fixture.detectChanges();

    expect(filas().map(f => f.querySelector('td')?.textContent)).toEqual(['Nueva']);
  });
});
