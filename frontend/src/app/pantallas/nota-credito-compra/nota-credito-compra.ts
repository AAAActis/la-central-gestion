import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ApiLaCentral, ArticuloResultado, FacturaCompraResumen, NotaCreditoCompraNueva, ProveedorResumen, TotalesNotaCreditoCompra,
  mensajeDeError,
} from '../../nucleo/api';

/** 057 CA-005: un artículo devuelto y su origen, elegido por línea (D9). */
interface LineaDevuelta { articulo: ArticuloResultado; sucursalId: number | null; cantidad: number | null; }
interface Origen { id: number; nombre: string; }

/**
 * HU-FCO-07 (057) · carga manual de la nota de crédito de proveedor. Es siempre A: no se elige el
 * tipo, el número se copia y el IVA (21 %) lo calcula la API. No cambia el costo de ningún artículo.
 * Referencia al menos una factura activa del proveedor; la devolución baja el stock del origen de
 * cada línea (una sucursal o Reclamos).
 */
@Component({
  selector: 'app-nota-credito-compra',
  imports: [FormsModule, DecimalPipe],
  template: `
    @if (registrada(); as numeroRegistrado) {
      <section class="tarjeta">
        <h2>Nota de crédito {{ numeroRegistrado }} registrada</h2>
        <p class="tenue">No cambió el costo de ningún artículo.</p>
        <button type="button" (click)="nueva()">Cargar otra nota de crédito</button>
      </section>
    } @else {
      <form (ngSubmit)="registrar()" (input)="invalidarTotales()" (change)="invalidarTotales()">
        <section class="tarjeta">
          <h2>Nota de crédito de proveedor</h2>
          <p class="ayuda">Es siempre tipo A: el número se copia tal cual, sin 99, y se suma 21 % de IVA. No cambia el costo de los artículos.</p>
          @if (proveedor(); as p) {
            <p>Proveedor: <strong>{{ p.razonSocial }}</strong> <span class="tenue mono">({{ p.codigo }})</span>
              <button type="button" class="secundario" (click)="cambiarProveedor()">Cambiar</button></p>
          } @else {
            <div class="fila">
              <input name="textoProveedor" [(ngModel)]="textoProveedor" placeholder="Código o nombre del proveedor"
                aria-label="Código o nombre del proveedor" (keydown.enter)="$event.preventDefault(); buscarProveedor()" />
              <button type="button" class="secundario" (click)="buscarProveedor()">Buscar proveedor</button>
            </div>
            @if (errorProveedor()) { <p class="error">{{ errorProveedor() }}</p> }
            @for (p of proveedores(); track p.id) {
              <button type="button" class="opcion" (click)="elegirProveedor(p)"><span class="mono">{{ p.codigo }}</span> {{ p.razonSocial }}</button>
            }
          }
          <div class="campos">
            <label>Número <input name="numero" [(ngModel)]="numero" maxlength="30" required class="mono" /></label>
            <label>Emisión <input type="date" name="emision" [(ngModel)]="fechaEmision" required /></label>
            <label>Clase
              <select name="clase" [(ngModel)]="clase">
                <option value="BONIFICACION">Bonificación o diferencia de precio · no mueve stock</option>
                <option value="DEVOLUCION">Devolución de mercadería · baja el stock del origen</option>
              </select>
            </label>
            <label>Importe neto <input type="number" min="0.01" step="0.01" name="importeNeto" [(ngModel)]="importeNeto" required /></label>
          </div>
          <fieldset>
            <legend>Percepciones del comprobante</legend>
            <div class="campos">
              <label>IVA <input type="number" min="0" step="0.01" name="pIva" [(ngModel)]="percepciones.iva" /></label>
              <label>Ingresos Brutos <input type="number" min="0" step="0.01" name="pIibb" [(ngModel)]="percepciones.iibb" /></label>
              <label>Municipal <input type="number" min="0" step="0.01" name="pMunicipal" [(ngModel)]="percepciones.municipal" /></label>
              <label>Ganancias <input type="number" min="0" step="0.01" name="pGanancias" [(ngModel)]="percepciones.ganancias" /></label>
            </div>
          </fieldset>
        </section>

        @if (proveedor()) {
          <section class="tarjeta">
            <h2>Facturas a las que corresponde</h2>
            @if (errorFacturas()) {
              <p class="error">{{ errorFacturas() }}</p>
            } @else if (facturas().length === 0) {
              <p class="vacio">El proveedor no tiene facturas de compra activas: sin al menos una, la nota de crédito no se registra.</p>
            }
            @for (f of facturas(); track f.id) {
              <label class="factura"><input type="checkbox" [checked]="marcadas().has(f.id)" (change)="alternarFactura(f.id)" />
                <span class="mono">{{ f.numero }}</span> <span class="tenue">· {{ f.fechaEmision }}</span></label>
            }
          </section>
        }

        @if (clase === 'DEVOLUCION') {
          <section class="tarjeta">
            <h2>Mercadería devuelta</h2>
            @if (lineas.length === 0) {
              <p class="vacio">Todavía no hay artículos. Buscalos por código o nombre.</p>
            } @else {
              <div class="tabla-caja">
                <table class="sin-seleccion lineas">
                  <thead><tr><th>Artículo</th><th>Cantidad</th><th>Sale de</th><th></th></tr></thead>
                  <tbody>
                    @for (l of lineas; track $index; let i = $index) {
                      <tr>
                        <td><span class="mono">{{ l.articulo.codigoInterno }}</span> {{ l.articulo.nombre }}</td>
                        <td><input type="number" min="0.001" step="0.001" [name]="'cantidad' + i" [(ngModel)]="l.cantidad" aria-label="Cantidad" /></td>
                        <td>
                          <select [name]="'origen' + i" [(ngModel)]="l.sucursalId" aria-label="Sale de">
                            <option [ngValue]="null" disabled>Elegí</option>
                            @for (o of origenes(); track o.id) { <option [ngValue]="o.id">{{ o.nombre }}</option> }
                          </select>
                        </td>
                        <td><button type="button" class="secundario" (click)="quitarLinea(i)">Quitar</button></td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
            <div class="fila">
              <input name="textoArticulo" [(ngModel)]="textoArticulo" placeholder="Código o nombre del artículo"
                aria-label="Código o nombre del artículo" (keydown.enter)="$event.preventDefault(); buscarArticulo()" />
              <button type="button" class="secundario" (click)="buscarArticulo()">Buscar artículo</button>
            </div>
            @for (a of articulos(); track a.id) {
              <button type="button" class="opcion" (click)="agregarArticulo(a)"><span class="mono">{{ a.codigoInterno }}</span> {{ a.nombre }}</button>
            }
          </section>
        }

        <section class="tarjeta">
          @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
          @if (totales(); as t) {
            <table class="sin-seleccion" data-totales>
              <tbody>
                <tr><td>Neto</td><td>$ {{ t.neto | number: '1.2-2' }}</td></tr>
                <tr><td>IVA 21 %</td><td>$ {{ t.iva | number: '1.2-2' }}</td></tr>
                <tr><td>Percepciones</td><td>$ {{ t.percepciones | number: '1.2-2' }}</td></tr>
                <tr><th>Total</th><th>$ {{ t.total | number: '1.2-2' }}</th></tr>
              </tbody>
            </table>
          }
          <div class="fila">
            <button type="button" class="secundario" name="calcular" (click)="calcular()" [disabled]="!nota()">Calcular el total</button>
            <button type="submit" name="registrar" [disabled]="!totales() || enviando()">Registrar nota de crédito</button>
          </div>
        </section>
      </form>
    }
  `,
})
export class NotaCreditoCompra {
  private readonly api = inject(ApiLaCentral);

  textoProveedor = '';
  readonly proveedores = signal<ProveedorResumen[]>([]);
  readonly errorProveedor = signal<string | null>(null);
  readonly proveedor = signal<ProveedorResumen | null>(null);
  readonly facturas = signal<FacturaCompraResumen[]>([]);
  readonly errorFacturas = signal<string | null>(null);
  readonly marcadas = signal<ReadonlySet<number>>(new Set());

  numero = '';
  fechaEmision = '';
  clase: 'BONIFICACION' | 'DEVOLUCION' = 'BONIFICACION';
  importeNeto: number | null = null;
  percepciones: { iva: number | null; iibb: number | null; municipal: number | null; ganancias: number | null } =
    { iva: null, iibb: null, municipal: null, ganancias: null };

  textoArticulo = '';
  readonly articulos = signal<ArticuloResultado[]>([]);
  lineas: LineaDevuelta[] = [];
  /** Los orígenes salen de las existencias de la ficha: las sucursales y Reclamos. */
  readonly origenes = signal<Origen[]>([]);

  readonly totales = signal<TotalesNotaCreditoCompra | null>(null);
  readonly error = signal<string | null>(null);
  readonly enviando = signal(false);
  readonly registrada = signal<string | null>(null);

  buscarProveedor() {
    this.errorProveedor.set(null);
    const texto = this.textoProveedor;
    this.api.proveedores(texto).subscribe({
      next: p => {
        this.proveedores.set(p);
        if (p.length === 0) this.errorProveedor.set(`No hay proveedores activos para «${texto}».`);
      },
      error: e => {
        this.proveedores.set([]);
        this.errorProveedor.set(mensajeDeError(e));
      },
    });
  }

  /** CA-001: solo las facturas activas de ESTE proveedor. */
  elegirProveedor(p: ProveedorResumen) {
    this.proveedor.set(p);
    this.proveedores.set([]);
    this.marcadas.set(new Set());
    this.facturas.set([]);
    this.errorFacturas.set(null);
    this.invalidarTotales();
    // shortcut: las 100 facturas más recientes del proveedor; con más, hace falta filtrar por número o fecha.
    this.api.facturasCompra({ proveedorId: p.id }, 1, 100).subscribe({
      next: pagina => this.facturas.set(pagina.items.filter(f => !f.anulada)),
      error: e => this.errorFacturas.set(mensajeDeError(e)),
    });
  }

  cambiarProveedor() {
    this.proveedor.set(null);
    this.facturas.set([]);
    this.marcadas.set(new Set());
    this.invalidarTotales();
  }

  /** CA-002: una o varias facturas. */
  alternarFactura(id: number) {
    const marcadas = new Set(this.marcadas());
    if (!marcadas.delete(id)) marcadas.add(id);
    this.marcadas.set(marcadas);
  }

  buscarArticulo() {
    this.api.buscar(this.textoArticulo, false, 1, 20).subscribe({
      next: r => this.articulos.set(r.articulos),
      error: e => this.error.set(mensajeDeError(e)),
    });
  }

  agregarArticulo(a: ArticuloResultado) {
    this.lineas = [...this.lineas, { articulo: a, sucursalId: null, cantidad: 1 }];
    this.articulos.set([]);
    this.textoArticulo = '';
    this.invalidarTotales();
    if (this.origenes().length === 0)
      this.api.ficha(a.id).subscribe(f => this.origenes.set(f.existencias.map(e => ({ id: e.sucursalId, nombre: e.sucursal }))));
  }

  quitarLinea(i: number) {
    this.lineas = this.lineas.filter((_, j) => j !== i);
    this.invalidarTotales();
  }

  /** Lo que viaja a la API, o null si falta algo para calcular. La bonificación va sin líneas. */
  nota(): NotaCreditoCompraNueva | null {
    const p = this.proveedor();
    const devolucion = this.clase === 'DEVOLUCION';
    if (!p || !this.numero.trim() || !this.fechaEmision || !this.importeNeto || this.importeNeto <= 0 || this.marcadas().size === 0)
      return null;
    if (devolucion && (this.lineas.length === 0 || this.lineas.some(l => l.sucursalId === null || !l.cantidad || l.cantidad <= 0)))
      return null;
    return {
      proveedorId: p.id,
      numero: this.numero.trim(),
      fechaEmision: this.fechaEmision,
      clase: this.clase,
      importeNeto: this.importeNeto,
      percepcionIva: this.percepciones.iva,
      percepcionIibb: this.percepciones.iibb,
      percepcionMunicipal: this.percepciones.municipal,
      percepcionGanancias: this.percepciones.ganancias,
      facturaIds: [...this.marcadas()],
      lineas: devolucion ? this.lineas.map(l => ({ articuloId: l.articulo.id, sucursalId: l.sucursalId!, cantidad: l.cantidad! })) : [],
    };
  }

  /** CA-007: el total lo calcula la API, antes de guardar nada. */
  calcular() {
    const nota = this.nota();
    if (!nota) return;
    this.error.set(null);
    this.api.calcularNotaCreditoCompra(nota).subscribe({
      next: t => this.totales.set(t),
      error: e => this.error.set(mensajeDeError(e)),
    });
  }

  /** CA-007: cambiar algo después de ver el total obliga a recalcularlo. */
  invalidarTotales() {
    this.totales.set(null);
  }

  registrar() {
    const nota = this.nota();
    if (!this.totales() || !nota) return;
    this.enviando.set(true);
    this.error.set(null);
    this.api.registrarNotaCreditoCompra(nota).subscribe({
      next: () => {
        this.enviando.set(false);
        this.registrada.set(nota.numero);
      },
      error: e => {
        this.enviando.set(false);
        this.error.set(mensajeDeError(e));
      },
    });
  }

  nueva() {
    this.registrada.set(null);
    this.cambiarProveedor();
    this.numero = '';
    this.fechaEmision = '';
    this.clase = 'BONIFICACION';
    this.importeNeto = null;
    this.percepciones = { iva: null, iibb: null, municipal: null, ganancias: null };
    this.lineas = [];
  }
}
