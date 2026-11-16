import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { Subject, catchError, debounceTime, filter, forkJoin, map, of, switchMap } from 'rxjs';
import {
  ApiLaCentral, ArticuloResultado, FacturaCompraNueva, FichaArticulo, ProveedorResumen, TotalesFacturaCompra, mensajeDeError,
} from '../../nucleo/api';
import { SesionActual } from '../../nucleo/sesion';

interface LineaEnCarga {
  articulo: ArticuloResultado;
  sucursalId: number | null;
  cantidad: number | null;
  precioUnitario: number | null;
  descuentoPorcentaje: number;
  descuento2Porcentaje: number;
  descuento3Porcentaje: number;
}

interface Sucursal { id: number; nombre: string; }

/**
 * HU-FCO-01 · carga manual de la factura de compra. La pantalla no calcula costos ni
 * stock: después de registrar, muestra lo que quedó en cada artículo releyendo su ficha.
 * HU-FCO-06 · la mecánica del negocio: proveedor y artículo por código o nombre (Enter sobre
 * el código exacto elige), tipo A/B propuesto y editable, 99 del B, tres descuentos, destino
 * heredado de la cabecera y el total con IVA y percepciones (lo calcula la API) antes de guardar.
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
      <form (ngSubmit)="registrar()" (input)="invalidarTotales()" (change)="invalidarTotales()">
        <section class="tarjeta">
          <h2>Factura de compra</h2>
          @if (proveedor(); as p) {
            <p>Proveedor: <strong>{{ p.razonSocial }}</strong> <span class="tenue mono">({{ p.codigo }})</span>
              <button type="button" class="secundario" (click)="proveedor.set(null)">Cambiar</button></p>
          } @else {
            <div class="fila">
              <input name="textoProveedor" [(ngModel)]="textoProveedor" placeholder="Código o nombre del proveedor"
                aria-label="Código o nombre del proveedor" (keydown.enter)="$event.preventDefault(); enterProveedor()" />
              <button type="button" class="secundario" (click)="buscarProveedor()">Buscar proveedor</button>
            </div>
            @if (errorProveedor()) { <p class="error">{{ errorProveedor() }}</p> }
            @for (p of proveedores(); track p.id) {
              <button type="button" class="opcion" (click)="elegirProveedor(p)"><span class="mono">{{ p.codigo }}</span> {{ p.razonSocial }}</button>
            }
          }

          <div class="campos">
            <label>Tipo
              <select name="tipo" [ngModel]="tipoComprobante" (ngModelChange)="cambiarTipo($event)" required>
                <option value="" disabled>Elegí</option>
                <option value="A">A · suma 21 % de IVA</option>
                <option value="B">B · sin IVA</option>
              </select>
            </label>
            <label>Número <input name="numero" [(ngModel)]="numero" maxlength="30" required class="mono" /></label>
            <label>Emisión <input type="date" name="emision" [(ngModel)]="fechaEmision" required /></label>
            <label>Vencimiento <input type="date" name="vencimiento" [(ngModel)]="fechaVencimiento" /></label>
            <label>Imputación <input type="date" name="imputacion" [(ngModel)]="fechaImputacion" /></label>
          </div>
          <p class="ayuda">El tipo se propone según lo que suele emitir el proveedor; se puede cambiar. El B lleva 99 en el primer tramo; el A se copia tal cual. Sin imputación, va la emisión.</p>
          @if (sucursales().length > 0) {
            <label>Destino de las líneas nuevas
              <select name="sucursalCabecera" [(ngModel)]="sucursalCabecera">
                @for (s of sucursales(); track s.id) { <option [ngValue]="s.id">{{ s.nombre }}</option> }
              </select>
            </label>
          }
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
            <p class="vacio">Todavía no hay líneas. Escribí el código o el nombre de un artículo.</p>
          } @else {
            <div class="tabla-caja">
            <table class="sin-seleccion lineas">
              <thead><tr><th>Artículo</th><th>Destino</th><th>Cantidad</th><th>Precio unitario</th><th>Desc. 1 %</th><th>Desc. 2 %</th><th>Desc. 3 %</th><th></th></tr></thead>
              <tbody>
                @for (l of lineas; track $index; let i = $index) {
                  <tr [class.elegido]="reemplazando() === i">
                    <td><span class="mono">{{ l.articulo.codigoInterno }}</span> {{ l.articulo.nombre }}</td>
                    <td>
                      <select [name]="'destino' + i" [(ngModel)]="l.sucursalId" required>
                        <option [ngValue]="null" disabled>Elegí</option>
                        @for (s of sucursales(); track s.id) { <option [ngValue]="s.id">{{ s.nombre }}</option> }
                      </select>
                    </td>
                    <td><input type="number" min="0" step="any" class="corto" [name]="'cantidad' + i" [(ngModel)]="l.cantidad" required /></td>
                    <td><input type="number" min="0" step="0.01" class="corto" [name]="'precio' + i" [(ngModel)]="l.precioUnitario" required placeholder="Sin costo" /></td>
                    <td><input type="number" min="0" max="100" step="0.01" class="corto" [name]="'descuento' + i" [(ngModel)]="l.descuentoPorcentaje" /></td>
                    <td><input type="number" min="0" max="100" step="0.01" class="corto" [name]="'descuento2' + i" [(ngModel)]="l.descuento2Porcentaje" /></td>
                    <td><input type="number" min="0" max="100" step="0.01" class="corto" [name]="'descuento3' + i" [(ngModel)]="l.descuento3Porcentaje" /></td>
                    <td class="fila">
                      <button type="button" class="secundario" (click)="reemplazar(i)" [attr.aria-pressed]="reemplazando() === i">Reemplazar</button>
                      <button type="button" class="secundario" (click)="quitarLinea(i)">Quitar</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
            </div>
          }

          @if (reemplazando() !== null) {
            <p class="aviso info" role="status">Elegí el artículo que reemplaza al de la línea {{ reemplazando()! + 1 }}.
              <button type="button" class="secundario" (click)="reemplazando.set(null)">No reemplazar</button></p>
          }
          <div class="fila agregar">
            <input name="textoArticulo" [ngModel]="textoArticulo" (ngModelChange)="escribirArticulo($event)"
              placeholder="Código o nombre del artículo" aria-label="Código o nombre del artículo"
              (keydown.enter)="$event.preventDefault(); enterArticulo()" />
            <button type="button" class="secundario" (click)="buscarArticulo()">Buscar artículo</button>
          </div>
          @if (errorArticulo()) { <p class="error">{{ errorArticulo() }}</p> }
          @for (a of articulos(); track a.id) {
            <button type="button" class="opcion" (click)="elegirArticulo(a)"><span class="mono">{{ a.codigoInterno }}</span> {{ a.nombre }}</button>
          }
        </section>

        @if (errorRegistro()) { <p class="error">{{ errorRegistro() }}</p> }
        @if (totales(); as t) {
          <section class="tarjeta" data-totales>
            <h2>Total de la factura</h2>
            <table class="sin-seleccion">
              <tbody>
                <tr><td>Neto (con descuentos)</td><td>$ {{ t.neto | number: '1.2-2' }}</td></tr>
                <tr><td>IVA</td><td>$ {{ t.iva | number: '1.2-2' }}</td></tr>
                <tr><td>Percepciones</td><td>$ {{ t.percepciones | number: '1.2-2' }}</td></tr>
                <tr><th>Total</th><th>$ {{ t.total | number: '1.2-2' }}</th></tr>
              </tbody>
            </table>
            <div class="fila">
              <button type="button" (click)="confirmar()" [disabled]="registrando()">Confirmar y registrar</button>
              <span class="tenue">Se suma el stock en el destino de cada línea y se actualiza el costo de cada artículo.</span>
            </div>
          </section>
        } @else {
          <div class="fila">
            <button type="submit" [disabled]="registrando()">Ver total</button>
            <span class="tenue">Antes de guardar, se muestra el total con IVA y percepciones.</span>
          </div>
        }
      </form>
    }
  `,
})
export class FacturaCompra {
  private readonly api = inject(ApiLaCentral);
  private readonly sesion = inject(SesionActual);
  private readonly escrito = new Subject<string>();

  textoProveedor = '';
  readonly proveedores = signal<ProveedorResumen[]>([]);
  readonly proveedor = signal<ProveedorResumen | null>(null);
  readonly errorProveedor = signal<string | null>(null);

  tipoComprobante = '';
  numero = '';
  fechaEmision = '';
  fechaVencimiento = '';
  fechaImputacion = '';
  percepciones = { iva: 0, iibb: 0, municipal: 0, ganancias: 0 };

  textoArticulo = '';
  readonly articulos = signal<ArticuloResultado[]>([]);
  readonly errorArticulo = signal<string | null>(null);
  /** Las sucursales salen de la ficha del primer artículo: son las de su stock. */
  readonly sucursales = signal<Sucursal[]>([]);
  /** CA-013 (D9): el destino que heredan las líneas nuevas. Arranca en la sucursal de la sesión. */
  sucursalCabecera: number | null = this.sesion.sesion()?.sucursalId ?? null;
  lineas: LineaEnCarga[] = [];
  /** CA-010: la línea cuyo artículo se va a reemplazar con el próximo que se elija. */
  readonly reemplazando = signal<number | null>(null);

  readonly totales = signal<TotalesFacturaCompra | null>(null);
  readonly registrando = signal(false);
  readonly errorRegistro = signal<string | null>(null);
  readonly registrada = signal<{ numero: string; fichas: { ficha: FichaArticulo; sucursalId: number }[] } | null>(null);

  constructor() {
    // CA-008: con tres caracteres o más, coincidencias aproximadas mientras se escribe.
    this.escrito.pipe(
      debounceTime(250),
      filter(texto => texto.length >= 3),
      switchMap(texto => this.api.buscar(texto, false, 1, 10, true).pipe(
        map(p => ({ texto, articulos: p.articulos })),
        catchError(() => of({ texto, articulos: [] as ArticuloResultado[] })))),
      takeUntilDestroyed(),
    ).subscribe(r => {
      // Si mientras tanto se eligió un artículo o se siguió escribiendo, la respuesta es vieja.
      if (r.texto === this.textoArticulo.trim()) this.articulos.set(r.articulos);
    });
  }

  buscarProveedor(alTerminar?: (p: ProveedorResumen[]) => boolean) {
    this.errorProveedor.set(null);
    const texto = this.textoProveedor;
    this.api.proveedores(texto).subscribe({
      next: p => {
        if (alTerminar?.(p)) return;
        this.proveedores.set(p);
        if (p.length === 0) this.errorProveedor.set(`No hay proveedores activos para «${texto}».`);
      },
      error: e => {
        this.proveedores.set([]);
        this.errorProveedor.set(mensajeDeError(e));
      },
    });
  }

  /** CA-002 y CA-003: Enter sobre un código exacto (sin ceros a la izquierda) lo elige sin listar.
   *  Si hay más de uno (035 y 00035), lista: elegir uno solo sería adivinar. */
  enterProveedor() {
    const codigo = sinCeros(this.textoProveedor);
    this.buscarProveedor(p => {
      const exactos = p.filter(x => sinCeros(x.codigo) === codigo);
      if (exactos.length === 1) this.elegirProveedor(exactos[0]);
      return exactos.length === 1;
    });
  }

  elegirProveedor(p: ProveedorResumen) {
    this.proveedor.set(p);
    this.proveedores.set([]);
    // CA-004: solo una propuesta; el usuario la cambia cuando quiera.
    this.cambiarTipo(p.tipoComprobanteHabitual ?? '');
  }

  /** CA-005: el B lleva 99 en el primer tramo; el A se copia tal cual (no se precarga nada). */
  cambiarTipo(tipo: string) {
    this.tipoComprobante = tipo;
    if (tipo === 'B' && this.numero.trim() === '') this.numero = '99-';
    if (tipo !== 'B' && this.numero === '99-') this.numero = '';
    this.invalidarTotales();
  }

  escribirArticulo(texto: string) {
    this.textoArticulo = texto;
    this.escrito.next(texto.trim());
  }

  buscarArticulo(alTerminar?: (a: ArticuloResultado[]) => boolean) {
    this.errorArticulo.set(null);
    const texto = this.textoArticulo;
    this.api.buscar(texto, false, 1, 10, !!alTerminar).subscribe({
      next: p => {
        if (alTerminar?.(p.articulos)) return;
        this.articulos.set(p.articulos);
        if (p.articulos.length === 0) this.errorArticulo.set(`No hay artículos activos para «${texto}».`);
      },
      error: e => {
        this.articulos.set([]);
        this.errorArticulo.set(mensajeDeError(e));
      },
    });
  }

  /** CA-009: Enter sobre el código exacto de un artículo lo elige sin listar. */
  enterArticulo() {
    const codigo = this.textoArticulo.trim().toUpperCase();
    this.buscarArticulo(a => {
      const exacto = a.find(x => x.codigoInterno.trim().toUpperCase() === codigo);
      if (exacto) this.elegirArticulo(exacto);
      return !!exacto;
    });
  }

  elegirArticulo(articulo: ArticuloResultado) {
    this.articulos.set([]);
    const reemplazo = this.reemplazando();
    if (this.lineas.some((l, i) => l.articulo.id === articulo.id && i !== reemplazo)) {
      this.errorArticulo.set(`${articulo.codigoInterno} ya está en la factura: corregí su línea.`);
      return;
    }
    this.textoArticulo = '';
    this.reemplazando.set(null);
    this.invalidarTotales();
    // CA-010: reemplazar cambia solo el artículo; cantidad, destino y descuentos quedan.
    const linea: LineaEnCarga = reemplazo !== null ? this.lineas[reemplazo] : {
      articulo, sucursalId: this.sucursalCabecera, cantidad: null, precioUnitario: null,
      descuentoPorcentaje: 0, descuento2Porcentaje: 0, descuento3Porcentaje: 0,
    };
    linea.articulo = articulo;
    if (reemplazo === null) this.lineas.push(linea);
    this.api.ficha(articulo.id).subscribe(f => {
      if (this.sucursales().length === 0)
        this.sucursales.set(f.existencias.map(e => ({ id: e.sucursalId, nombre: e.sucursal })));
      // CA-011: se propone el costo vigente; sin costo no se propone nada (nunca cero, D1).
      if (linea.precioUnitario === null && linea.articulo.id === articulo.id) linea.precioUnitario = f.costoVigente;
    });
  }

  reemplazar(i: number) {
    this.reemplazando.set(this.reemplazando() === i ? null : i);
  }

  quitarLinea(i: number) {
    this.lineas.splice(i, 1);
    this.reemplazando.set(null);
    this.invalidarTotales();
  }

  invalidarTotales() {
    this.totales.set(null);
  }

  private factura(): FacturaCompraNueva | null {
    const proveedor = this.proveedor();
    if (!proveedor) {
      this.errorRegistro.set('Elegí el proveedor de la factura.');
      return null;
    }
    return {
      proveedorId: proveedor.id,
      numero: this.numero,
      tipoComprobante: this.tipoComprobante,
      fechaEmision: this.fechaEmision,
      fechaVencimiento: this.fechaVencimiento || null,
      fechaImputacion: this.fechaImputacion || null,
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
        descuento2Porcentaje: Number(l.descuento2Porcentaje) || 0,
        descuento3Porcentaje: Number(l.descuento3Porcentaje) || 0,
      })),
    };
  }

  /** CA-015: el total (lo calcula la API, con el IVA del tipo de comprobante) antes de guardar nada. */
  registrar() {
    const factura = this.factura();
    if (!factura) return;
    this.errorRegistro.set(null);
    this.registrando.set(true);
    this.api.calcularFacturaCompra(factura).subscribe({
      next: t => {
        this.registrando.set(false);
        this.totales.set(t);
      },
      error: e => {
        this.registrando.set(false);
        this.errorRegistro.set(mensajeDeError(e));
      },
    });
  }

  confirmar() {
    const factura = this.factura();
    if (!factura) return;
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
    this.tipoComprobante = '';
    this.numero = '';
    this.fechaEmision = '';
    this.fechaVencimiento = '';
    this.fechaImputacion = '';
    this.percepciones = { iva: 0, iibb: 0, municipal: 0, ganancias: 0 };
    this.lineas = [];
    this.totales.set(null);
  }
}

/** CA-003: 35 y 00035 son el mismo código. */
function sinCeros(codigo: string) {
  return codigo.trim().toUpperCase().replace(/^0+(?=.)/, '');
}
