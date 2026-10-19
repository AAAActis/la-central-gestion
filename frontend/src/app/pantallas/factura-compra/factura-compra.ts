import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import {
  ApiLaCentral, ArticuloResultado, FacturaCompraNueva, FichaArticulo, ProveedorResumen, mensajeDeError,
} from '../../nucleo/api';

interface LineaEnCarga {
  articulo: ArticuloResultado;
  sucursalId: number | null;
  cantidad: number | null;
  precioUnitario: number | null;
  descuentoPorcentaje: number;
}

interface Sucursal { id: number; nombre: string; }

/**
 * HU-FCO-01 · carga manual de la factura de compra. La pantalla no calcula costos ni
 * stock: después de registrar, muestra lo que quedó en cada artículo releyendo su ficha.
 */
@Component({
  selector: 'app-factura-compra',
  imports: [FormsModule, DecimalPipe],
  template: `
    @if (registrada(); as r) {
      <section class="tarjeta">
        <h2>Factura {{ r.numero }} registrada</h2>
        <p class="tenue">Así quedaron los artículos comprados, según la base:</p>
        <table class="sin-seleccion">
          <thead><tr><th>Artículo</th><th>Costo vigente</th><th>Stock en el destino</th></tr></thead>
          <tbody>
            @for (f of r.fichas; track f.ficha.id) {
              <tr>
                <td><span class="mono">{{ f.ficha.codigoInterno }}</span> {{ f.ficha.nombre }}</td>
                <td>@if (f.ficha.costoVigente !== null) { $ {{ f.ficha.costoVigente | number: '1.2-2' }} } @else { <span class="tenue">sin costo</span> }</td>
                <td>{{ stockEn(f.ficha, f.sucursalId) }}</td>
              </tr>
            }
          </tbody>
        </table>
        <button type="button" (click)="nueva()">Cargar otra factura</button>
      </section>
    } @else {
      <form (ngSubmit)="registrar()">
        <section class="tarjeta">
          <h2>Factura de compra</h2>
          @if (proveedor(); as p) {
            <p>Proveedor: <strong>{{ p.razonSocial }}</strong> <span class="tenue">({{ p.codigo }})</span>
              <button type="button" class="secundario" (click)="proveedor.set(null)">Cambiar</button></p>
          } @else {
            <div class="fila">
              <input name="textoProveedor" [(ngModel)]="textoProveedor" placeholder="Razón social o CUIT del proveedor" />
              <button type="button" class="secundario" (click)="buscarProveedor()">Buscar proveedor</button>
            </div>
            @if (errorProveedor()) { <p class="error">{{ errorProveedor() }}</p> }
            @for (p of proveedores(); track p.id) {
              <button type="button" class="opcion" (click)="elegirProveedor(p)">{{ p.razonSocial }} <span class="tenue">{{ p.cuit ?? p.codigo }}</span></button>
            }
          }

          <div class="campos">
            <label>Número <input name="numero" [(ngModel)]="numero" maxlength="30" required /></label>
            <label>Emisión <input type="date" name="emision" [(ngModel)]="fechaEmision" required /></label>
            <label>Vencimiento <input type="date" name="vencimiento" [(ngModel)]="fechaVencimiento" /></label>
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

        <section class="tarjeta">
          <h2>Artículos comprados</h2>
          @if (lineas.length === 0) {
            <p class="vacio">Todavía no hay líneas. Buscá un artículo para agregarlo.</p>
          } @else {
            <table class="sin-seleccion lineas">
              <thead><tr><th>Artículo</th><th>Destino</th><th>Cantidad</th><th>Precio unitario</th><th>Descuento %</th><th></th></tr></thead>
              <tbody>
                @for (l of lineas; track l.articulo.id; let i = $index) {
                  <tr>
                    <td><span class="mono">{{ l.articulo.codigoInterno }}</span> {{ l.articulo.nombre }}</td>
                    <td>
                      <select [name]="'destino' + i" [(ngModel)]="l.sucursalId" required>
                        <option [ngValue]="null" disabled>Elegí</option>
                        @for (s of sucursales(); track s.id) { <option [ngValue]="s.id">{{ s.nombre }}</option> }
                      </select>
                    </td>
                    <td><input type="number" min="0" step="any" class="corto" [name]="'cantidad' + i" [(ngModel)]="l.cantidad" required /></td>
                    <td><input type="number" min="0" step="0.01" class="corto" [name]="'precio' + i" [(ngModel)]="l.precioUnitario" required /></td>
                    <td><input type="number" min="0" max="100" step="0.01" class="corto" [name]="'descuento' + i" [(ngModel)]="l.descuentoPorcentaje" /></td>
                    <td><button type="button" class="secundario" (click)="quitarLinea(i)">Quitar</button></td>
                  </tr>
                }
              </tbody>
            </table>
          }

          <div class="fila agregar">
            <input name="textoArticulo" [(ngModel)]="textoArticulo" placeholder="Código o nombre del artículo" (keydown.enter)="$event.preventDefault(); buscarArticulo()" />
            <button type="button" class="secundario" (click)="buscarArticulo()">Buscar artículo</button>
          </div>
          @if (errorArticulo()) { <p class="error">{{ errorArticulo() }}</p> }
          @for (a of articulos(); track a.id) {
            <button type="button" class="opcion" (click)="agregarLinea(a)"><span class="mono">{{ a.codigoInterno }}</span> {{ a.nombre }}</button>
          }
        </section>

        @if (errorRegistro()) { <p class="error">{{ errorRegistro() }}</p> }
        <div class="fila">
          <button type="submit" [disabled]="registrando()">Registrar factura</button>
          <span class="tenue">Se suma el stock en el destino de cada línea y se actualiza el costo de cada artículo.</span>
        </div>
      </form>
    }
  `,
})
export class FacturaCompra {
  private readonly api = inject(ApiLaCentral);

  textoProveedor = '';
  readonly proveedores = signal<ProveedorResumen[]>([]);
  readonly proveedor = signal<ProveedorResumen | null>(null);
  readonly errorProveedor = signal<string | null>(null);

  numero = '';
  fechaEmision = '';
  fechaVencimiento = '';
  percepciones = { iva: 0, iibb: 0, municipal: 0, ganancias: 0 };

  textoArticulo = '';
  readonly articulos = signal<ArticuloResultado[]>([]);
  readonly errorArticulo = signal<string | null>(null);
  /** Las sucursales salen de la ficha del primer artículo: son las de su stock. */
  readonly sucursales = signal<Sucursal[]>([]);
  lineas: LineaEnCarga[] = [];

  readonly registrando = signal(false);
  readonly errorRegistro = signal<string | null>(null);
  readonly registrada = signal<{ numero: string; fichas: { ficha: FichaArticulo; sucursalId: number }[] } | null>(null);

  buscarProveedor() {
    this.errorProveedor.set(null);
    this.api.proveedores(this.textoProveedor).subscribe({
      next: p => this.proveedores.set(p),
      error: e => {
        this.proveedores.set([]);
        this.errorProveedor.set(mensajeDeError(e));
      },
    });
  }

  elegirProveedor(p: ProveedorResumen) {
    this.proveedor.set(p);
    this.proveedores.set([]);
  }

  buscarArticulo() {
    this.errorArticulo.set(null);
    this.api.buscar(this.textoArticulo, false, 1, 10).subscribe({
      next: p => {
        this.articulos.set(p.articulos);
        if (p.articulos.length === 0) this.errorArticulo.set(`No hay artículos activos para «${this.textoArticulo}».`);
      },
      error: e => {
        this.articulos.set([]);
        this.errorArticulo.set(mensajeDeError(e));
      },
    });
  }

  agregarLinea(articulo: ArticuloResultado) {
    this.articulos.set([]);
    if (this.lineas.some(l => l.articulo.id === articulo.id)) {
      this.errorArticulo.set(`${articulo.codigoInterno} ya está en la factura: corregí su línea.`);
      return;
    }
    this.lineas.push({ articulo, sucursalId: null, cantidad: null, precioUnitario: null, descuentoPorcentaje: 0 });
    if (this.sucursales().length === 0) {
      this.api.ficha(articulo.id).subscribe(f =>
        this.sucursales.set(f.existencias.map(e => ({ id: e.sucursalId, nombre: e.sucursal }))));
    }
  }

  quitarLinea(i: number) {
    this.lineas.splice(i, 1);
  }

  registrar() {
    const proveedor = this.proveedor();
    if (!proveedor) {
      this.errorRegistro.set('Elegí el proveedor de la factura.');
      return;
    }
    const factura: FacturaCompraNueva = {
      proveedorId: proveedor.id,
      numero: this.numero,
      fechaEmision: this.fechaEmision,
      fechaVencimiento: this.fechaVencimiento || null,
      percepcionIva: Number(this.percepciones.iva) || 0,
      percepcionIibb: Number(this.percepciones.iibb) || 0,
      percepcionMunicipal: Number(this.percepciones.municipal) || 0,
      percepcionGanancias: Number(this.percepciones.ganancias) || 0,
      lineas: this.lineas.map(l => ({
        articuloId: l.articulo.id,
        sucursalId: Number(l.sucursalId),
        cantidad: Number(l.cantidad),
        precioUnitario: Number(l.precioUnitario),
        descuentoPorcentaje: Number(l.descuentoPorcentaje) || 0,
      })),
    };
    this.errorRegistro.set(null);
    this.registrando.set(true);
    this.api.registrarFacturaCompra(factura).subscribe({
      next: () => {
        const lineas = factura.lineas;
        forkJoin(lineas.map(l => this.api.ficha(l.articuloId))).subscribe(fichas => {
          this.registrando.set(false);
          this.registrada.set({ numero: factura.numero, fichas: fichas.map((f, i) => ({ ficha: f, sucursalId: lineas[i].sucursalId })) });
        });
      },
      error: e => {
        this.registrando.set(false);
        this.errorRegistro.set(mensajeDeError(e));
      },
    });
  }

  stockEn(ficha: FichaArticulo, sucursalId: number) {
    const existencia = ficha.existencias.find(e => e.sucursalId === sucursalId);
    return existencia ? `${existencia.cantidad} en ${existencia.sucursal}` : '—';
  }

  nueva() {
    this.registrada.set(null);
    this.proveedor.set(null);
    this.numero = '';
    this.fechaEmision = '';
    this.fechaVencimiento = '';
    this.percepciones = { iva: 0, iibb: 0, municipal: 0, ganancias: 0 };
    this.lineas = [];
  }
}
