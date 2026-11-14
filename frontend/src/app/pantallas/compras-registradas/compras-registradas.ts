import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiLaCentral, FacturaCompraResumen, FiltroFacturasCompra, ProveedorResumen, mensajeDeError } from '../../nucleo/api';
import type { Pagina } from '../../compartido/listado';

const TAMANO_PAGINA = 20;

/**
 * HU-FCO-02 (026) · Consulta de facturas de compra registradas: por proveedor, número y
 * rangos de emisión y de vencimiento. Las anuladas se muestran marcadas (CA-005).
 */
@Component({
  selector: 'app-compras-registradas',
  imports: [FormsModule],
  template: `
    <section class="tarjeta">
      <h2>Compras registradas</h2>
      <form class="filtros" (ngSubmit)="buscar()">
        @if (proveedor(); as p) {
          <p>Proveedor: <strong>{{ p.razonSocial }}</strong> <span class="tenue mono">({{ p.codigo }})</span>
            <button type="button" class="secundario" (click)="proveedor.set(null)">Quitar</button></p>
        } @else {
          <div class="fila">
            <label>Proveedor (código o nombre)
              <input name="textoProveedor" [(ngModel)]="textoProveedor" (keydown.enter)="$event.preventDefault(); buscarProveedor()" autocomplete="off" />
            </label>
            <button type="button" class="secundario" (click)="buscarProveedor()">Buscar proveedor</button>
          </div>
          @for (p of proveedores(); track p.id) {
            <button type="button" class="opcion" (click)="elegirProveedor(p)"><span class="mono">{{ p.codigo }}</span> {{ p.razonSocial }}</button>
          }
        }
        <div class="campos">
          <label>Número <input name="numero" [(ngModel)]="numero" maxlength="30" class="mono" /></label>
          <label>Emisión desde <input type="date" name="emisionDesde" [(ngModel)]="emisionDesde" /></label>
          <label>Emisión hasta <input type="date" name="emisionHasta" [(ngModel)]="emisionHasta" /></label>
          <label>Vencimiento desde <input type="date" name="vencimientoDesde" [(ngModel)]="vencimientoDesde" /></label>
          <label>Vencimiento hasta <input type="date" name="vencimientoHasta" [(ngModel)]="vencimientoHasta" /></label>
        </div>
        <button type="submit" class="primario">Buscar</button>
      </form>
    </section>

    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (resultado(); as r) {
      @if (r.items.length === 0) {
        <div class="estado-vacio vacio" role="status"><h2>Sin resultados</h2><p>Probá con otro proveedor, número o rango de fechas.</p></div>
      } @else {
        <section class="tarjeta">
          <div class="tabla-caja">
            <table>
              <thead><tr><th>Emisión</th><th>Proveedor</th><th>Número</th><th>Tipo</th><th>Vencimiento</th><th>Estado</th></tr></thead>
              <tbody>
                @for (f of r.items; track f.id) {
                  <tr [class.anulada]="f.anulada" [class.inactivo]="f.anulada">
                    <td>{{ f.fechaEmision }}</td>
                    <td>{{ f.proveedor }}</td>
                    <td class="mono">{{ f.numero }}</td>
                    <td>{{ f.tipoComprobante }}</td>
                    <td>{{ f.fechaVencimiento ?? '—' }}</td>
                    <td>{{ f.anulada ? 'Anulada' : 'Activa' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
          <p class="tenue">Mostrando {{ (r.pagina - 1) * r.tamanoPagina + 1 }}–{{ (r.pagina - 1) * r.tamanoPagina + r.items.length }} de {{ r.total }}</p>
          <div class="fila">
            <button type="button" class="secundario" [disabled]="r.pagina <= 1" (click)="irA(r.pagina - 1)">Anterior</button>
            <button type="button" class="secundario" [disabled]="r.pagina * r.tamanoPagina >= r.total" (click)="irA(r.pagina + 1)">Siguiente</button>
          </div>
        </section>
      }
    }
  `,
})
export class ComprasRegistradas {
  private readonly api = inject(ApiLaCentral);

  textoProveedor = '';
  numero = '';
  emisionDesde = '';
  emisionHasta = '';
  vencimientoDesde = '';
  vencimientoHasta = '';
  readonly proveedor = signal<ProveedorResumen | null>(null);
  readonly proveedores = signal<ProveedorResumen[]>([]);
  readonly resultado = signal<Pagina<FacturaCompraResumen> | null>(null);
  readonly error = signal<string | null>(null);
  /** Los criterios de la última búsqueda: la paginación no toma lo que se escribió después. */
  private buscado: FiltroFacturasCompra = {};

  buscarProveedor() {
    const texto = this.textoProveedor.trim();
    if (texto === '') return;
    this.api.proveedores(texto).subscribe({ next: p => this.proveedores.set(p), error: e => this.error.set(mensajeDeError(e)) });
  }

  elegirProveedor(p: ProveedorResumen) {
    this.proveedor.set(p);
    this.proveedores.set([]);
    this.textoProveedor = '';
  }

  buscar() {
    this.buscado = {
      proveedorId: this.proveedor()?.id ?? null, numero: this.numero,
      emisionDesde: this.emisionDesde, emisionHasta: this.emisionHasta,
      vencimientoDesde: this.vencimientoDesde, vencimientoHasta: this.vencimientoHasta,
    };
    this.irA(1);
  }

  irA(pagina: number) {
    this.error.set(null);
    this.api.facturasCompra(this.buscado, pagina, TAMANO_PAGINA).subscribe({
      next: r => this.resultado.set(r),
      error: e => { this.resultado.set(null); this.error.set(mensajeDeError(e)); },
    });
  }
}
