import { Component, inject, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiLaCentral, ArticuloResultado, CandidatoCatalogo, ResultadoCandidatos, mensajeDeError } from '../../nucleo/api';

/**
 * 053 · HU-CMP-06 · Candidatos de vinculación: entre los catálogos de todos los proveedores, primero
 * las coincidencias exactas y, aparte, las aproximadas de mayor a menor parecido (CA1). Una fila ya
 * vinculada dice a qué artículo (CA4). Sin candidatos, se dice para qué texto (CA6).
 * 054a · HU-CMP-07: con un artículo propio elegido, cada fila sin vincular se vincula a él (CA1);
 * si el código ya es de otro artículo, se muestra lo que dice la API (CA6).
 * Solo clases del sistema visual (docs/diseno/SISTEMA-VISUAL.md): no agrega estilos.
 */
@Component({
  selector: 'app-vinculacion',
  imports: [FormsModule, NgTemplateOutlet],
  template: `
    <section class="tarjeta">
      <h2>Vincular catálogo de proveedores</h2>
      <form class="filtros" (ngSubmit)="buscar()">
        <div class="fila">
          <label>Repuesto (código, número original o nombre)
            <input name="texto" [(ngModel)]="texto" autocomplete="off" />
          </label>
          <button type="submit" class="primario" [disabled]="buscando()">{{ buscando() ? 'Buscando…' : 'Buscar' }}</button>
        </div>
      </form>
      <form class="filtros articulo" (ngSubmit)="buscarArticulo()">
        <div class="fila">
          <label>Artículo propio (código interno, código de proveedor o nombre)
            <input name="textoArticulo" [(ngModel)]="textoArticulo" autocomplete="off" />
          </label>
          <button type="submit" [disabled]="buscandoArticulo()">{{ buscandoArticulo() ? 'Buscando…' : 'Buscar artículo' }}</button>
        </div>
      </form>
      @if (elegido(); as a) {
        <p class="elegido" role="status">Vinculando a <span class="mono">{{ a.codigoInterno }}</span> · {{ a.nombre }}</p>
      } @else if (articulos().length > 0) {
        <ul>
          @for (a of articulos(); track a.id) {
            <li><button type="button" class="elegir" (click)="elegir(a)"><span class="mono">{{ a.codigoInterno }}</span> · {{ a.nombre }}</button></li>
          }
        </ul>
      }
    </section>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (resultado(); as r) {
      @if (mensajeVinculacion()) { <p class="error" role="alert">{{ mensajeVinculacion() }}</p> }
      @if (r.mensaje) {
        <div class="estado-vacio vacio" role="status"><h2>Sin candidatos</h2><p>{{ r.mensaje }}</p></div>
      } @else {
        <section class="tarjeta exactas">
          <h3>Coincidencias exactas</h3>
          @if (r.exactas.length === 0) {
            <p class="tenue">Ninguna fila coincide exacto con «{{ r.texto }}».</p>
          } @else {
            <ng-container *ngTemplateOutlet="tabla; context: { $implicit: r.exactas, conParecido: false }" />
          }
        </section>
        <section class="tarjeta aproximadas">
          <h3>Por aproximación</h3>
          @if (r.aproximadas.length === 0) {
            <p class="tenue">Ninguna fila se parece lo suficiente a «{{ r.texto }}».</p>
          } @else {
            <ng-container *ngTemplateOutlet="tabla; context: { $implicit: r.aproximadas, conParecido: true }" />
          }
        </section>
      }
    }

    <ng-template #tabla let-filas let-conParecido="conParecido">
      <div class="tabla-caja">
        <table>
          <thead><tr>
            <th>Proveedor</th><th>Código</th><th>Nombre</th><th>N.º original</th><th>Marca</th>
            @if (conParecido) { <th>Parecido</th> }
            <th>Vinculación</th>
          </tr></thead>
          <tbody>
            @for (f of asFilas(filas); track f.id) {
              <tr [attr.data-id]="f.id">
                <td>{{ f.proveedor }}</td>
                <td class="mono">{{ f.codigo }}</td>
                <td>{{ f.nombre }}</td>
                <td class="mono">{{ f.numeroOriginal ?? '—' }}</td>
                <td>{{ f.marca ?? '—' }}</td>
                @if (conParecido) { <td>{{ porcentaje(f.parecido) }}</td> }
                <td>
                  @if (f.vinculado; as v) {
                    <span class="etiqueta activa vinculada">Vinculada a <span class="mono">{{ v.codigoInterno }}</span> · {{ v.nombre }}</span>
                  } @else {
                    <span class="tenue">Sin vincular</span>
                    <button type="button" class="vincular" [disabled]="!elegido() || vinculando() === f.id" (click)="vincular(f)">
                      {{ vinculando() === f.id ? 'Vinculando…' : 'Vincular' }}
                    </button>
                  }
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </ng-template>
  `,
})
export class Vinculacion {
  private readonly api = inject(ApiLaCentral);

  texto = '';
  textoArticulo = '';
  readonly resultado = signal<ResultadoCandidatos | null>(null);
  readonly error = signal<string | null>(null);
  readonly buscando = signal(false);
  readonly articulos = signal<ArticuloResultado[]>([]);
  readonly elegido = signal<ArticuloResultado | null>(null);
  readonly buscandoArticulo = signal(false);
  readonly vinculando = signal<number | null>(null);
  /** CA6: el error de vincular no tapa los resultados, como sí lo hace el de buscar. */
  readonly mensajeVinculacion = signal<string | null>(null);

  buscar() {
    const texto = this.texto.trim();
    if (texto === '') return;
    this.error.set(null);
    this.buscando.set(true);
    this.api.candidatos(texto).subscribe({
      next: r => { this.resultado.set(r); this.buscando.set(false); },
      error: e => { this.resultado.set(null); this.error.set(mensajeDeError(e)); this.buscando.set(false); },
    });
  }

  buscarArticulo() {
    const texto = this.textoArticulo.trim();
    if (texto === '') return;
    this.elegido.set(null);
    this.buscandoArticulo.set(true);
    this.api.buscar(texto, false, 1, 10).subscribe({
      next: p => { this.articulos.set(p.articulos); this.buscandoArticulo.set(false); },
      error: e => { this.articulos.set([]); this.error.set(mensajeDeError(e)); this.buscandoArticulo.set(false); },
    });
  }

  elegir(articulo: ArticuloResultado) {
    this.elegido.set(articulo);
  }

  vincular(fila: CandidatoCatalogo) {
    const articulo = this.elegido();
    if (!articulo) return;
    this.mensajeVinculacion.set(null);
    this.vinculando.set(fila.id);
    this.api.vincular(fila.id, articulo.id).subscribe({
      next: () => {
        const vinculado = { id: articulo.id, codigoInterno: articulo.codigoInterno, nombre: articulo.nombre };
        const marcar = (filas: CandidatoCatalogo[]) => filas.map(f => f.id === fila.id ? { ...f, vinculado } : f);
        this.resultado.update(r => r && { ...r, exactas: marcar(r.exactas), aproximadas: marcar(r.aproximadas) });
        this.vinculando.set(null);
      },
      error: e => { this.mensajeVinculacion.set(mensajeDeError(e)); this.vinculando.set(null); },
    });
  }

  protected porcentaje(parecido: number | null): string {
    return parecido === null ? '—' : `${Math.round(parecido * 100)} %`;
  }

  protected asFilas(filas: unknown): CandidatoCatalogo[] {
    return filas as CandidatoCatalogo[];
  }
}
