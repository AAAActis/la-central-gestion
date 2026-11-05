import { Component, TemplateRef, contentChild, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';

/** Una página que devuelve el servidor. El listado nunca pide más que una. */
export interface Pagina<T> {
  items: T[];
  total: number;
  pagina: number;
  tamanoPagina: number;
}

export type Consulta<T> = (texto: string, incluirInactivos: boolean, pagina: number, tamanoPagina: number) => Observable<Pagina<T>>;

/**
 * Listado reutilizable (HU-UI-04): búsqueda, inactivos y paginación del servidor.
 * La pantalla pone la consulta y las celdas de cada fila:
 * `<app-listado [consultar]="..." [columnas]="[...]"><ng-template #fila let-x>...</ng-template></app-listado>`.
 */
@Component({
  selector: 'app-listado',
  imports: [FormsModule],
  template: `
    <form>
      <label for="listado-texto">{{ etiqueta() }}</label>
      <input id="listado-texto" type="search" name="texto" />
      <label class="check"><input type="checkbox" name="inactivos" /> Incluir inactivos</label>
      <button type="submit" class="primario">Buscar</button>
    </form>
  `,
})
export class Listado<T extends { activo: boolean }> {
  readonly consultar = input.required<Consulta<T>>();
  readonly columnas = input.required<string[]>();
  readonly etiqueta = input('Buscar');
  readonly ayuda = input('');
  readonly nombrePlural = input('resultados');
  readonly tamanoPagina = input(20);
  readonly fila = contentChild.required<TemplateRef<{ $implicit: T }>>('fila');
}
