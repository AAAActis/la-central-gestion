import { NgTemplateOutlet } from '@angular/common';
import {
  Component, ElementRef, TemplateRef, afterNextRender, computed, contentChild, input, signal, viewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, Subscription } from 'rxjs';
import { mensajeDeError } from '../nucleo/api';
import { BotonCarga } from './boton-carga';

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
 * Lo que se proyecta sin plantilla (por ejemplo «Nuevo artículo») va al lado de «Buscar».
 */
@Component({
  selector: 'app-listado',
  imports: [FormsModule, NgTemplateOutlet, BotonCarga],
  template: `
    <section class="tarjeta listado">
      <form class="tarjeta-cuerpo" (ngSubmit)="buscar()">
        <div class="listado-busqueda">
          <div class="listado-campo">
            <label for="listado-texto">{{ etiqueta() }}</label>
            <div class="campo-busqueda">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"
                stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" /></svg>
              <input #campo id="listado-texto" type="search" name="texto" [(ngModel)]="texto" autocomplete="off" />
            </div>
          </div>
          <label class="check"><input type="checkbox" name="inactivos" [(ngModel)]="incluirInactivos" /> Incluir inactivos</label>
          <button type="submit" class="primario" [cargando]="cargando()" textoCarga="Buscando…">Buscar</button>
          <ng-content />
        </div>
        @if (ayuda()) { <p class="ayuda listado-ayuda">{{ ayuda() }}</p> }
      </form>

      @if (error()) {
        <div class="tarjeta-pie"><p class="error" role="alert">{{ error() }}</p></div>
      } @else if (resultado(); as r) {
        @if (r.items.length === 0) {
          <div class="estado-vacio vacio aparecer" role="status">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"
              stroke-linejoin="round" aria-hidden="true" focusable="false"><circle cx="11" cy="11" r="7" /><path d="m20 20-3.5-3.5" /></svg>
            <h2>Sin resultados para «{{ buscado.texto }}»</h2>
            <p>Revisá lo que escribiste o probá con otra palabra.
              @if (!buscado.incluirInactivos) { <br />Si está dado de baja, activá «Incluir inactivos». }
            </p>
            <button type="button" (click)="volverABuscar()">Volver a buscar</button>
          </div>
        } @else {
          <div class="tarjeta-cabecera">
            <h2>Resultados de la búsqueda</h2>
            <span class="ayuda" role="status">{{ r.total }} {{ nombrePlural() }} encontrados</span>
          </div>
          <div class="tabla-caja">
            <table>
              <thead><tr>@for (c of columnas(); track c) { <th>{{ c }}</th> }<th><span class="oculto">Estado</span></th></tr></thead>
              <tbody>
                @for (item of r.items; track $index) {
                  <tr [class.inactivo]="!item.activo">
                    <ng-container *ngTemplateOutlet="fila(); context: { $implicit: item }" />
                    <td class="listado-estado">@if (!item.activo) { <span class="etiqueta">Inactivo</span> }</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <div class="tarjeta-pie listado-pie">
            <span class="ayuda">Mostrando {{ desde() }}–{{ hasta() }} de {{ r.total }} {{ nombrePlural() }}</span>
            <div class="listado-paginas">
              <button type="button" class="chico" [disabled]="r.pagina <= 1 || cargando()" (click)="irA(r.pagina - 1)">Anterior</button>
              <span class="ayuda">{{ r.pagina }} de {{ paginas() }}</span>
              <button type="button" class="chico" [disabled]="r.pagina >= paginas() || cargando()" (click)="irA(r.pagina + 1)">Siguiente</button>
            </div>
          </div>
        }
      }
    </section>
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

  private readonly campo = viewChild.required<ElementRef<HTMLInputElement>>('campo');

  texto = '';
  incluirInactivos = false;
  /** Criterio de la última búsqueda: el paginado lo usa aunque el campo haya cambiado. */
  buscado = { texto: '', incluirInactivos: false };

  readonly resultado = signal<Pagina<T> | null>(null);
  readonly error = signal<string | null>(null);
  readonly cargando = signal(false);
  private pedido?: Subscription;

  readonly paginas = computed(() => {
    const r = this.resultado();
    return r ? Math.max(1, Math.ceil(r.total / r.tamanoPagina)) : 1;
  });
  readonly desde = computed(() => {
    const r = this.resultado()!;
    return (r.pagina - 1) * r.tamanoPagina + 1;
  });
  readonly hasta = computed(() => {
    const r = this.resultado()!;
    return this.desde() + r.items.length - 1;
  });

  constructor() {
    // Al entrar, una sola cosa que hacer: escribir.
    afterNextRender(() => this.campo().nativeElement.focus());
  }

  buscar() {
    this.buscado = { texto: this.texto, incluirInactivos: this.incluirInactivos };
    this.irA(1);
  }

  irA(numero: number) {
    // Una respuesta vieja no pisa a la nueva: se corta el pedido anterior.
    this.pedido?.unsubscribe();
    this.cargando.set(true);
    this.error.set(null);
    this.pedido = this.consultar()(this.buscado.texto, this.buscado.incluirInactivos, numero, this.tamanoPagina()).subscribe({
      next: p => {
        this.resultado.set(p);
        this.cargando.set(false);
      },
      error: e => {
        this.resultado.set(null);
        this.error.set(mensajeDeError(e));
        this.cargando.set(false);
      },
    });
  }

  volverABuscar() {
    this.campo().nativeElement.focus();
    this.campo().nativeElement.select();
  }
}
