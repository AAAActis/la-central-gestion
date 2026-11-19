import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiLaCentral, FiltroNotasCreditoCompra, NotaCreditoCompraResumen, ProveedorResumen, mensajeDeError } from '../../nucleo/api';
import type { Pagina } from '../../compartido/listado';
import { Modal } from '../../compartido/modal';
import { CampoFormulario, Formulario, ValoresFormulario } from '../../compartido/formulario/formulario';

const TAMANO_PAGINA = 20;

/**
 * HU-FCO-07 (057) CA-012 · Listado de notas de crédito de proveedor, por proveedor y rango de
 * emisión, con las anuladas marcadas (como la 026). Desde cada fila vigente se anula, con motivo
 * y la autorización de otro ADMINISTRADOR (como la 028, D-S6-23).
 */
const CAMPOS_ANULACION: CampoFormulario[] = [
  { nombre: 'motivo', etiqueta: 'Motivo', obligatorio: true },
  // autocomplete off: el navegador no tiene que completar con la clave de quien está en la sesión.
  { nombre: 'usuarioAutoriza', etiqueta: 'Usuario de quien autoriza', obligatorio: true, autocompletar: 'off' },
  { nombre: 'contrasenaAutoriza', etiqueta: 'Contraseña de quien autoriza', tipo: 'password', obligatorio: true, autocompletar: 'new-password' },
];

const CLASES: Record<NotaCreditoCompraResumen['clase'], string> = { BONIFICACION: 'Bonificación', DEVOLUCION: 'Devolución' };

@Component({
  selector: 'app-notas-credito-registradas',
  imports: [FormsModule, DecimalPipe, Modal, Formulario],
  template: `
    <section class="tarjeta">
      <h2>Notas de crédito registradas</h2>
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
          <label>Emisión desde <input type="date" name="emisionDesde" [(ngModel)]="emisionDesde" /></label>
          <label>Emisión hasta <input type="date" name="emisionHasta" [(ngModel)]="emisionHasta" /></label>
        </div>
        <button type="submit" class="primario">Buscar</button>
      </form>
    </section>

    @if (aviso()) { <p class="aviso" role="status">{{ aviso() }}</p> }
    @if (error()) {
      <p class="error" role="alert">{{ error() }}</p>
    } @else if (resultado(); as r) {
      @if (r.items.length === 0) {
        <div class="estado-vacio vacio" role="status"><h2>Sin resultados</h2><p>Probá con otro proveedor o rango de fechas.</p></div>
      } @else {
        <section class="tarjeta">
          <div class="tabla-caja">
            <table>
              <thead><tr><th>Emisión</th><th>Proveedor</th><th>Número</th><th>Clase</th><th>Importe neto</th><th>Estado</th><th>Acción</th></tr></thead>
              <tbody>
                @for (n of r.items; track n.id) {
                  <tr [class.anulada]="n.anulada" [class.inactivo]="n.anulada">
                    <td>{{ n.fechaEmision }}</td>
                    <td>{{ n.proveedor }}</td>
                    <td class="mono">{{ n.numero }}</td>
                    <td>{{ clase(n) }}</td>
                    <td>$ {{ n.importeNeto | number: '1.2-2' }}</td>
                    <td>{{ n.anulada ? 'Anulada' : 'Vigente' }}</td>
                    <td>@if (!n.anulada) { <button type="button" class="peligro" (click)="abrirAnulacion(n)">Anular</button> }</td>
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

    <app-modal [titulo]="'Anular nota de crédito ' + (aAnular()?.numero ?? '')" [(abierto)]="anulacionAbierta">
      @if (aAnular()?.clase === 'DEVOLUCION') {
        <p class="ayuda">Es una devolución: la mercadería vuelve al stock de donde salió (la sucursal o Reclamos).</p>
      }
      <app-formulario [campos]="camposAnulacion" [valores]="valoresAnulacion()" varianteEnviar="peligro"
        textoEnviar="Anular" textoEnviando="Anulando…" [enviando]="enviando()" [errorGeneral]="errorAnulacion()"
        (enviar)="anular($event)">
        <button type="button" acciones (click)="anulacionAbierta.set(false)">Cancelar</button>
      </app-formulario>
    </app-modal>
  `,
})
export class NotasCreditoRegistradas {
  private readonly api = inject(ApiLaCentral);

  textoProveedor = '';
  emisionDesde = '';
  emisionHasta = '';
  readonly proveedor = signal<ProveedorResumen | null>(null);
  readonly proveedores = signal<ProveedorResumen[]>([]);
  readonly resultado = signal<Pagina<NotaCreditoCompraResumen> | null>(null);
  readonly error = signal<string | null>(null);
  readonly aviso = signal<string | null>(null);
  protected readonly camposAnulacion = CAMPOS_ANULACION;
  readonly aAnular = signal<NotaCreditoCompraResumen | null>(null);
  readonly anulacionAbierta = signal(false);
  /** Un objeto nuevo en cada apertura: el formulario se rearma vacío, sin la contraseña anterior. */
  readonly valoresAnulacion = signal<ValoresFormulario>({});
  readonly enviando = signal(false);
  readonly errorAnulacion = signal<string | null>(null);
  /** Los criterios de la última búsqueda: la paginación no toma lo que se escribió después. */
  private buscado: FiltroNotasCreditoCompra = {};

  clase(n: NotaCreditoCompraResumen) {
    return CLASES[n.clase];
  }

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
    this.aviso.set(null);
    this.buscado = { proveedorId: this.proveedor()?.id ?? null, emisionDesde: this.emisionDesde, emisionHasta: this.emisionHasta };
    this.irA(1);
  }

  irA(_pagina: number) {}

  abrirAnulacion(n: NotaCreditoCompraResumen) {
    this.aAnular.set(n);
    this.valoresAnulacion.set({});
    this.errorAnulacion.set(null);
    this.anulacionAbierta.set(true);
  }

  anular(_v: ValoresFormulario) {}
}
