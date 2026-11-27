import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import {
  ApiLaCentral, ArticuloResultado, Comparador as DatosComparador, FichaArticulo, FilaComparador, mensajeDeError,
} from '../../nucleo/api';

type Vista = 'compra' | 'venta';

const TEXTO_STOCK: Record<string, string> = { HAY: 'Hay', NO_HAY: 'No hay', A_CONFIRMAR: 'A confirmar' };

/**
 * 050 · HU-UI-12 · Comparador de precios. Se busca un artículo por nombre y, al elegirlo, se ven sus
 * proveedores del más barato al más caro por precio neto (030 CA-002, D-S7-09).
 * Vista Compra (032): lista y bonificado (neto), la diferencia con el más barato, el último comprado
 * y el stock máximo por proveedor (054 CA4). Vista Venta (033): precio estimado propio (Lista) y la
 * existencia por sucursal, junto a los precios de los proveedores.
 * El sitio del proveedor se abre en otra pestaña, sin perder la búsqueda (034 CA-003).
 * Solo clases del sistema visual (docs/diseno/SISTEMA-VISUAL.md): no agrega estilos.
 */
@Component({
  selector: 'app-comparador',
  imports: [FormsModule, DatePipe, DecimalPipe],
  template: `
    <section class="tarjeta">
      <h2>Comparador de precios</h2>
      <form class="filtros" (ngSubmit)="buscar()">
        <div class="fila">
          <label>Artículo (nombre o código)
            <input name="texto" [(ngModel)]="texto" autocomplete="off" />
          </label>
          <button type="submit" class="primario">Buscar</button>
        </div>
      </form>
      @for (a of articulos(); track a.id) {
        <button type="button" class="opcion" [attr.aria-pressed]="elegido()?.id === a.id" (click)="elegir(a)">
          <span class="mono">{{ a.codigoInterno }}</span> {{ a.nombre }}
        </button>
      }
    </section>

    @if (error()) { <p class="error" role="alert">{{ error() }}</p> }

    @if (elegido() && datos(); as d) {
      <section class="tarjeta">
        <h3>{{ elegido()!.nombre }} <span class="tenue mono">{{ elegido()!.codigoInterno }}</span></h3>
        <div class="fila" role="group" aria-label="Vista">
          <button type="button" class="secundario vista-compra" [attr.aria-pressed]="vista() === 'compra'" (click)="vista.set('compra')">Compra</button>
          <button type="button" class="secundario vista-venta" [attr.aria-pressed]="vista() === 'venta'" (click)="vista.set('venta')">Venta</button>
        </div>

        @if (d.mensaje) {
          <div class="estado-vacio vacio" role="status"><h2>Sin proveedores</h2><p>{{ d.mensaje }}</p></div>
        } @else {
          @if (vista() === 'venta') {
            <div class="venta">
              @if (precioEstimado() !== null) {
                <p>Precio de venta estimado: <strong class="precio-estimado">{{ precioEstimado() | number: '1.2-2' }}</strong></p>
              } @else {
                <p class="sin-estimado">No hay precio estimado disponible: el artículo no tiene márgenes cargados.</p>
              }
              <table>
                <thead><tr><th>Sucursal</th><th>Existencia</th><th>Ubicación</th></tr></thead>
                <tbody>
                  @for (e of ficha()?.existencias ?? []; track e.sucursalId) {
                    <tr class="existencia"><td>{{ e.sucursal }}</td><td>{{ e.cantidad }}</td><td>{{ e.ubicacionDeposito ?? '—' }}</td></tr>
                  }
                </tbody>
              </table>
            </div>
          }
          <div class="tabla-caja">
            <table>
              <thead><tr>
                <th>Proveedor</th><th>Código</th><th>Marca</th>
                @if (vista() === 'compra') { <th>Lista</th> }
                <th>Neto (bonificado)</th>
                @if (vista() === 'compra') { <th>Diferencia</th><th>Stock</th> }
                <th>Leído</th><th></th>
              </tr></thead>
              <tbody>
                @for (f of d.filas; track f.proveedorId + f.codigo) {
                  <tr class="proveedor" [attr.data-proveedor]="f.proveedorId">
                    <td>{{ f.proveedor }} @if (f.ultimoComprado) { <span class="etiqueta activa ultimo-comprado">Último comprado</span> }</td>
                    <td class="mono">{{ f.codigo }}</td>
                    <td>{{ f.marca ?? '—' }}</td>
                    @if (vista() === 'compra') { <td>{{ f.precioLista === null ? '—' : (f.precioLista | number: '1.2-2') }}</td> }
                    <td>
                      @if (f.precioNeto !== null) { {{ f.precioNeto | number: '1.2-2' }} }
                      @if (unSoloPrecio(f)) { <span class="tenue sin-bonificado">No informa precio bonificado</span> }
                    </td>
                    @if (vista() === 'compra') {
                      <td class="diferencia">{{ f.diferenciaConMasBarato === null ? '—' : (f.diferenciaConMasBarato === 0 ? 'El más barato' : '+ ' + (f.diferenciaConMasBarato | number: '1.2-2')) }}</td>
                      <td>{{ textoStock(f.estadoStock) }}</td>
                    }
                    <td>{{ f.ultimaLectura | date: 'dd/MM/yyyy HH:mm' }}
                      @if (f.ultimaConsultaFallida) {
                        <span class="etiqueta revisar consulta-fallida">Falló la última consulta: precio del {{ f.ultimaLectura | date: 'dd/MM/yyyy' }}</span>
                      }
                    </td>
                    <td>
                      @if (f.urlReferencia) {
                        <a class="sitio" [href]="f.urlReferencia" target="_blank" rel="noopener noreferrer">Abrir sitio</a>
                      }
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          @if (vista() === 'compra') {
            <p class="tenue">Stock por proveedor (el máximo entre sus códigos, nunca la suma):
              @for (e of d.existenciaPorProveedor; track e.proveedorId) {
                <span class="stock-proveedor" [attr.data-proveedor]="e.proveedorId">{{ nombreProveedor(d, e.proveedorId) }}: {{ textoStock(e.estadoStock) }}@if (e.stock !== null) { ({{ e.stock }}) }</span>
              }
            </p>
          }
        }
      </section>
    }
  `,
})
export class Comparador {
  private readonly api = inject(ApiLaCentral);

  texto = '';
  readonly articulos = signal<ArticuloResultado[]>([]);
  readonly elegido = signal<ArticuloResultado | null>(null);
  readonly datos = signal<DatosComparador | null>(null);
  readonly ficha = signal<FichaArticulo | null>(null);
  readonly vista = signal<Vista>('compra');
  readonly error = signal<string | null>(null);

  /** 033: el precio de venta estimado es el de la modalidad Lista; null si no hay márgenes (CA-004). */
  readonly precioEstimado = computed(() => this.ficha()?.precios.find(p => p.modalidad === 'Lista')?.precio ?? null);

  buscar() {
    const texto = this.texto.trim();
    if (texto === '') return;
    this.error.set(null);
    this.api.buscar(texto, false, 1, 20).subscribe({
      next: p => this.articulos.set(p.articulos),
      error: e => this.error.set(mensajeDeError(e)),
    });
  }

  elegir(a: ArticuloResultado) {
    this.elegido.set(a);
    this.datos.set(null);
    this.error.set(null);
    forkJoin({ comparador: this.api.comparador(a.id), ficha: this.api.ficha(a.id) }).subscribe({
      next: r => { this.datos.set(r.comparador); this.ficha.set(r.ficha); },
      error: e => this.error.set(mensajeDeError(e)),
    });
  }

  /** 032 CA-004: el proveedor publica un solo precio. */
  protected unSoloPrecio(f: FilaComparador): boolean {
    return (f.precioNeto === null) !== (f.precioLista === null);
  }

  protected textoStock(estado: string | null): string {
    return estado === null ? '—' : TEXTO_STOCK[estado] ?? estado;
  }

  protected nombreProveedor(d: DatosComparador, proveedorId: number): string {
    return d.filas.find(f => f.proveedorId === proveedorId)?.proveedor ?? String(proveedorId);
  }
}
