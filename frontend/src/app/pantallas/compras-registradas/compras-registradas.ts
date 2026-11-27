import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiLaCentral, FacturaCompraResumen, FiltroFacturasCompra, ProveedorResumen, mensajeDeError } from '../../nucleo/api';
import type { Pagina } from '../../compartido/listado';
import { exactoProveedor } from '../../compartido/codigo-exacto';
import { Modal } from '../../compartido/modal';
import { CampoFormulario, Formulario, ValoresFormulario } from '../../compartido/formulario/formulario';

const TAMANO_PAGINA = 20;

/**
 * HU-FCO-02 (026) · Consulta de facturas de compra registradas: por proveedor, número y
 * rangos de emisión y de vencimiento. Las anuladas se muestran marcadas (CA-005).
 * HU-FCO-04 (028) · Desde cada fila activa se anula con un motivo. Anula un ADMINISTRADOR solo
 * (CA-006 y CA-007 v1.2, D-S7-04); a un OPERADOR la API le responde 403 y se muestra su mensaje.
 */
const CAMPOS_ANULACION: CampoFormulario[] = [
  { nombre: 'motivo', etiqueta: 'Motivo', obligatorio: true },
];

@Component({
  selector: 'app-compras-registradas',
  imports: [FormsModule, Modal, Formulario],
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
              <input name="textoProveedor" [(ngModel)]="textoProveedor" (keydown.enter)="$event.preventDefault(); enterProveedor()" autocomplete="off" />
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

    @if (aviso()) { <p class="aviso" role="status">{{ aviso() }}</p> }
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (resultado(); as r) {
      @if (r.items.length === 0) {
        <div class="estado-vacio vacio" role="status"><h2>Sin resultados</h2><p>Probá con otro proveedor, número o rango de fechas.</p></div>
      } @else {
        <section class="tarjeta">
          <div class="tabla-caja">
            <table>
              <thead><tr><th>Emisión</th><th>Proveedor</th><th>Número</th><th>Tipo</th><th>Vencimiento</th><th>Estado</th><th>Acción</th></tr></thead>
              <tbody>
                @for (f of r.items; track f.id) {
                  <tr [class.anulada]="f.anulada" [class.inactivo]="f.anulada">
                    <td>{{ f.fechaEmision }}</td>
                    <td>{{ f.proveedor }}</td>
                    <td class="mono">{{ f.numero }}</td>
                    <td>{{ f.tipoComprobante }}</td>
                    <td>{{ f.fechaVencimiento ?? '—' }}</td>
                    <td>{{ f.anulada ? 'Anulada' : 'Activa' }}</td>
                    <td>@if (!f.anulada) { <button type="button" class="peligro" (click)="abrirAnulacion(f)">Anular</button> }</td>
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

    <app-modal [titulo]="'Anular factura ' + (aAnular()?.numero ?? '')" [(abierto)]="anulacionAbierta">
      <app-formulario [campos]="camposAnulacion" [valores]="valoresAnulacion()" varianteEnviar="peligro"
        textoEnviar="Anular" textoEnviando="Anulando…" [enviando]="enviando()" [errorGeneral]="errorAnulacion()"
        (enviar)="anular($event)">
        <button type="button" acciones (click)="anulacionAbierta.set(false)">Cancelar</button>
      </app-formulario>
    </app-modal>
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
  readonly aviso = signal<string | null>(null);
  protected readonly camposAnulacion = CAMPOS_ANULACION;
  readonly aAnular = signal<FacturaCompraResumen | null>(null);
  readonly anulacionAbierta = signal(false);
  /** Un objeto nuevo en cada apertura: el formulario se rearma vacío, sin la contraseña anterior. */
  readonly valoresAnulacion = signal<ValoresFormulario>({});
  readonly enviando = signal(false);
  readonly errorAnulacion = signal<string | null>(null);
  /** Los criterios de la última búsqueda: la paginación no toma lo que se escribió después. */
  private buscado: FiltroFacturasCompra = {};

  buscarProveedor(alTerminar?: (p: ProveedorResumen[]) => boolean) {
    const texto = this.textoProveedor.trim();
    if (texto === '') return;
    this.api.proveedores(texto).subscribe({
      next: p => { if (!alTerminar?.(p)) this.proveedores.set(p); },
      error: e => this.error.set(mensajeDeError(e)),
    });
  }

  /** HU-FCO-06 CA-002 y CA-003, como en la factura: Enter sobre un código exacto lo elige sin listar. */
  enterProveedor() {
    const texto = this.textoProveedor;
    this.buscarProveedor(p => {
      const exacto = exactoProveedor(p, texto);
      if (exacto) this.elegirProveedor(exacto);
      return !!exacto;
    });
  }

  elegirProveedor(p: ProveedorResumen) {
    this.proveedor.set(p);
    this.proveedores.set([]);
    this.textoProveedor = '';
  }

  buscar() {
    this.aviso.set(null);
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

  abrirAnulacion(f: FacturaCompraResumen) {
    this.aAnular.set(f);
    this.valoresAnulacion.set({});
    this.errorAnulacion.set(null);
    this.anulacionAbierta.set(true);
  }

  anular(v: ValoresFormulario) {
    const f = this.aAnular()!;
    this.enviando.set(true);
    this.api.anularFacturaCompra(f.id, {
      motivo: v['motivo'] as string,
    }).subscribe({
      next: () => {
        this.enviando.set(false);
        this.anulacionAbierta.set(false);
        this.aviso.set(`Factura ${f.numero} anulada.`);
        this.irA(this.resultado()?.pagina ?? 1);
      },
      error: e => { this.enviando.set(false); this.errorAnulacion.set(mensajeDeError(e)); },
    });
  }
}
