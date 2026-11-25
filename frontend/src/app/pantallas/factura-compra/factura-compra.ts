import { DecimalPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule, NgModel } from '@angular/forms';
import { Subject, catchError, debounceTime, defaultIfEmpty, filter, forkJoin, map, of, switchMap } from 'rxjs';
import {
  ApiLaCentral, ArticuloResultado, FacturaCompraNueva, FichaArticulo, LineaPdfInterpretada, ProveedorResumen, TotalesFacturaCompra,
  mensajeDeError,
} from '../../nucleo/api';
import { SesionActual } from '../../nucleo/sesion';
import { RELOJ, aTextoFecha, hoyIso, interpretarFecha } from '../../compartido/fecha-abreviada';
import { exactoArticulo, exactoProveedor } from '../../compartido/codigo-exacto';

interface LineaEnCarga {
  articulo: ArticuloResultado;
  sucursalId: number | null;
  cantidad: number | null;
  precioUnitario: number | null;
  descuentoPorcentaje: number;
  descuento2Porcentaje: number;
  descuento3Porcentaje: number;
  /** D-S6-15: el externo por marca o por artículo (el 5 % de pago contado de ETMAN). */
  descuento4Porcentaje: number;
  /** HU-UI-11 (049) CA-002: el PDF no la pudo afirmar. */
  revisar?: boolean;
  /** 049 CA-002: el cuarto descuento lo propuso el PDF sin traerlo. */
  revisarDescuento4?: boolean;
}

interface Sucursal { id: number; nombre: string; }
type CampoFecha = 'fechaEmision' | 'fechaVencimiento' | 'fechaImputacion';
const VENCIMIENTO_ANTERIOR = 'El vencimiento no puede ser anterior a la emisión.';

/** Los destinos de una línea: las existencias de la ficha, sin Reclamos (057 CA-008). */
const destinos = (f: FichaArticulo): Sucursal[] =>
  f.existencias.filter(e => !e.esReclamos).map(e => ({ id: e.sucursalId, nombre: e.sucursal }));

/** HU-UI-11 (049) CA-003: una línea del PDF cuyo código no tiene artículo. No se registra hasta resolverla. */
interface LineaSinResolver extends Omit<LineaPdfInterpretada, 'articuloId'> { revisar: boolean; revisarDescuento4: boolean; }

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
          <label>Cargar desde el PDF del proveedor
            <input type="file" accept="application/pdf" (change)="elegirPdf($event)" [disabled]="leyendoPdf()" />
          </label>
          <p class="ayuda">Se precarga este mismo formulario: revisá lo señalado antes de confirmar. Nada se guarda hasta confirmar.</p>
          @if (leyendoPdf()) { <p class="aviso info" role="status">Leyendo el PDF…</p> }
          @if (errorPdf()) { <p class="error" role="alert">{{ errorPdf() }}</p> }
          @if (proveedor(); as p) {
            <p>Proveedor: <strong>{{ p.razonSocial }}</strong> <span class="tenue mono">({{ p.codigo }})</span>
              @if (revisar('proveedorId')) {
                <span class="etiqueta revisar">Revisar</span> <span class="tenue">El PDF no trae el CUIT: se reconoció por el nombre.</span>
                <button type="button" class="secundario" (click)="confirmarProveedor()">Es este</button>
              }
              <button type="button" class="secundario" (click)="proveedor.set(null)">Cambiar</button></p>
          } @else {
            @if (revisar('proveedorId')) {
              <p><span class="etiqueta revisar">Revisar</span> El PDF no trae un proveedor activo que el sistema reconozca: buscalo.</p>
            }
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
            <label>Tipo @if (revisar('tipoComprobante')) { <span class="etiqueta revisar">Revisar</span> }
              <select name="tipo" [ngModel]="tipoComprobante" (ngModelChange)="cambiarTipo($event)" required
                [attr.aria-invalid]="revisar('tipoComprobante') || null">
                <option value="" disabled>Elegí</option>
                <option value="A">A · suma 21 % de IVA</option>
                <option value="B">B · sin IVA</option>
              </select>
            </label>
            <label>Número @if (revisar('numero')) { <span class="etiqueta revisar">Revisar</span> }
              <input name="numero" [(ngModel)]="numero" maxlength="30" required class="mono" [attr.aria-invalid]="revisar('numero') || null" /></label>
            @for (f of camposFecha; track f.campo) {
              <label>{{ f.etiqueta }} @if (revisar(f.campo)) { <span class="etiqueta revisar">Revisar</span> }
                <input [name]="f.nombre" [attr.name]="f.nombre" [(ngModel)]="textoFecha[f.campo]" placeholder="dd/mm/aaaa" inputmode="numeric" class="mono"
                  [required]="f.campo === 'fechaEmision'" (keydown.enter)="$event.preventDefault(); fijarFecha(f.campo)" (blur)="fijarFecha(f.campo)"
                  [attr.aria-invalid]="revisar(f.campo) || !!errorFecha()[f.campo] || null" [attr.aria-describedby]="errorFecha()[f.campo] ? 'error-' + f.nombre : null" />
                @if (errorFecha()[f.campo]; as e) { <span class="error" [id]="'error-' + f.nombre">{{ e }}</span> }
              </label>
            }
          </div>
          <p class="ayuda">El tipo se propone según lo que suele emitir el proveedor; se puede cambiar. El B lleva 99 en el primer tramo; el A se copia tal cual. Fechas: 5 es el día 5 de este mes; 2010, el 20/10 de este año. La imputación arranca en hoy; sin imputación, va la emisión.</p>
          @if (sucursales().length > 0) {
            <label>Destino de las líneas nuevas @if (revisar('sucursalSugeridaId')) { <span class="etiqueta revisar">Revisar</span> }
              <select name="sucursalCabecera" [(ngModel)]="sucursalCabecera" (ngModelChange)="quitarRevisar('sucursalSugeridaId')">
                @for (s of sucursales(); track s.id) { <option [ngValue]="s.id">{{ s.nombre }}</option> }
              </select>
            </label>
          }
        </section>

        <section class="tarjeta">
          <h2>Artículos comprados</h2>
          @if (lineas.length === 0) {
            <p class="vacio">Todavía no hay líneas. Escribí el código o el nombre de un artículo.</p>
          } @else {
            <div class="tabla-caja">
            <table class="sin-seleccion lineas">
              <thead><tr><th>Artículo</th><th>Destino</th><th>Cantidad</th><th>Precio unitario</th><th>Desc. 1 %</th><th>Desc. 2 %</th><th>Desc. 3 %</th><th>Desc. 4 %</th><th></th></tr></thead>
              <tbody>
                @for (l of lineas; track $index; let i = $index) {
                  <tr [class.elegido]="reemplazando() === i">
                    <td>@if (l.revisar) { <span class="etiqueta revisar">Revisar</span> }
                      <span class="mono">{{ l.articulo.codigoInterno }}</span> {{ l.articulo.nombre }}</td>
                    <td>
                      <select [name]="'destino' + i" [ngModel]="l.sucursalId" #destino="ngModel" (ngModelChange)="cambiarDestino(i, $event, destino)" required>
                        <option [ngValue]="null" disabled>Elegí</option>
                        @for (s of sucursales(); track s.id) { <option [ngValue]="s.id">{{ s.nombre }}</option> }
                      </select>
                    </td>
                    <td><input type="number" min="0" step="any" class="corto" [name]="'cantidad' + i" [(ngModel)]="l.cantidad" required /></td>
                    <td><input type="number" min="0" step="0.01" class="corto" [name]="'precio' + i" [(ngModel)]="l.precioUnitario" required placeholder="Sin costo" /></td>
                    <td><input type="number" min="0" max="100" step="0.01" class="corto" [name]="'descuento' + i" [(ngModel)]="l.descuentoPorcentaje" /></td>
                    <td><input type="number" min="0" max="100" step="0.01" class="corto" [name]="'descuento2' + i" [(ngModel)]="l.descuento2Porcentaje" /></td>
                    <td><input type="number" min="0" max="100" step="0.01" class="corto" [name]="'descuento3' + i" [(ngModel)]="l.descuento3Porcentaje" /></td>
                    <td>@if (l.revisarDescuento4) { <span class="etiqueta revisar">Revisar</span> }
                      <input type="number" min="0" max="100" step="0.01" class="corto" [name]="'descuento4' + i" [(ngModel)]="l.descuento4Porcentaje"
                        [attr.aria-invalid]="l.revisarDescuento4 || null" /></td>
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

          @if (sinResolver.length > 0) {
            <div data-sin-resolver>
              <p class="aviso alerta" role="status">{{ faltan() }}: elegí el artículo de cada una, dalo de alta o quitala. El sistema no crea artículos.</p>
              <div class="tabla-caja">
              <table class="sin-seleccion">
                <thead><tr><th>Código del proveedor</th><th>Descripción</th><th>Cantidad</th><th>Precio unitario</th><th></th></tr></thead>
                <tbody>
                  @for (p of sinResolver; track $index; let i = $index) {
                    <tr [class.elegido]="resolviendo() === i">
                      <td><span class="mono">{{ p.codigoProveedor }}</span></td>
                      <td>{{ p.descripcion }}</td>
                      <td>{{ p.cantidad }}</td>
                      <td>$ {{ p.precioUnitario | number: '1.2-2' }}</td>
                      <td class="fila">
                        <button type="button" class="secundario" (click)="resolver(i)" [attr.aria-pressed]="resolviendo() === i">Elegir artículo</button>
                        <a href="/articulos" target="_blank" rel="noopener">Dar de alta el artículo</a>
                        <button type="button" class="secundario" (click)="quitarSinResolver(i)">Quitar</button>
                      </td>
                    </tr>
                  }
                </tbody>
              </table>
              </div>
            </div>
          }
          @if (resolviendo() !== null) {
            <p class="aviso info" role="status">Elegí el artículo para el código {{ sinResolver[resolviendo()!].codigoProveedor }} del proveedor.
              <button type="button" class="secundario" (click)="resolviendo.set(null)">No elegir</button></p>
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

        <section class="tarjeta">
          <fieldset>
            <legend>Percepciones del comprobante</legend>
            <div class="campos">
              <label>IVA @if (revisar('percepcionIva')) { <span class="etiqueta revisar">Revisar</span> }
                <input type="number" min="0" step="0.01" name="pIva" [(ngModel)]="percepciones.iva" [attr.aria-invalid]="revisar('percepcionIva') || null" /></label>
              <label>Ingresos Brutos @if (revisar('percepcionIibb')) { <span class="etiqueta revisar">Revisar</span> }
                <input type="number" min="0" step="0.01" name="pIibb" [(ngModel)]="percepciones.iibb" [attr.aria-invalid]="revisar('percepcionIibb') || null" /></label>
              <label>Municipal @if (revisar('percepcionMunicipal')) { <span class="etiqueta revisar">Revisar</span> }
                <input type="number" min="0" step="0.01" name="pMunicipal" [(ngModel)]="percepciones.municipal" [attr.aria-invalid]="revisar('percepcionMunicipal') || null" /></label>
              <label>Ganancias @if (revisar('percepcionGanancias')) { <span class="etiqueta revisar">Revisar</span> }
                <input type="number" min="0" step="0.01" name="pGanancias" [(ngModel)]="percepciones.ganancias" [attr.aria-invalid]="revisar('percepcionGanancias') || null" /></label>
            </div>
          </fieldset>
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
            <button type="submit" [disabled]="registrando() || sinResolver.length > 0">Ver total</button>
            @if (sinResolver.length > 0) { <span class="tenue">{{ faltan() }}.</span> }
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

  private readonly reloj = inject(RELOJ);

  tipoComprobante = '';
  numero = '';
  /** Las fechas viajan a la API como aaaa-mm-dd; en pantalla se tipean abreviadas (Fix A). */
  fechaEmision = '';
  fechaVencimiento = '';
  fechaImputacion = hoyIso(this.reloj());
  readonly camposFecha = [
    { campo: 'fechaEmision', nombre: 'emision', etiqueta: 'Emisión' },
    { campo: 'fechaVencimiento', nombre: 'vencimiento', etiqueta: 'Vencimiento' },
    { campo: 'fechaImputacion', nombre: 'imputacion', etiqueta: 'Imputación' },
  ] as const;
  textoFecha: Record<CampoFecha, string> = { fechaEmision: '', fechaVencimiento: '', fechaImputacion: aTextoFecha(this.fechaImputacion) };
  readonly errorFecha = signal<Partial<Record<CampoFecha, string>>>({});
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

  /** HU-UI-11 (049): la carga desde el PDF precarga este mismo formulario. */
  readonly leyendoPdf = signal(false);
  readonly errorPdf = signal<string | null>(null);
  readonly paraRevisar = signal<string[]>([]);
  sinResolver: LineaSinResolver[] = [];
  /** CA-003: la línea sin resolver a la que se le asigna el próximo artículo que se elija. */
  readonly resolviendo = signal<number | null>(null);
  private desdePdf = false;
  /** El PDF cargado: si el proveedor se elige a mano, se vuelve a leer con él para resolver códigos. */
  private archivoPdf: File | null = null;
  /** Cada formulario nuevo cambia la versión: un total pedido para el anterior no se muestra. */
  private version = 0;

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
    const texto = this.textoProveedor;
    this.buscarProveedor(p => {
      const exacto = exactoProveedor(p, texto);
      if (exacto) this.elegirProveedor(exacto);
      return !!exacto;
    });
  }

  /** Fix E: el proveedor reconocido por su razón social lo confirma el operador; nunca queda confirmado solo. */
  confirmarProveedor() {
    this.quitarRevisar('proveedorId');
  }

  quitarRevisar(campo: string) {
    this.paraRevisar.update(r => r.filter(x => x !== campo));
  }

  elegirProveedor(p: ProveedorResumen) {
    this.quitarRevisar('proveedorId');
    this.proveedor.set(p);
    this.proveedores.set([]);
    // CA-004: solo una propuesta; el usuario la cambia cuando quiera. Con el formulario del PDF
    // (049), el tipo es el del comprobante y no se propone el habitual.
    if (!this.desdePdf) this.cambiarTipo(p.tipoComprobanteHabitual ?? '');
    else if (this.sinResolver.length > 0) this.buscarCodigos(this.archivoPdf!, p.id);
  }

  /** 049 CA-003: el PDF no trajo un proveedor reconocible; se buscan los códigos sin resolver en el elegido. */
  private buscarCodigos(archivo: File, proveedorId: number) {
    const version = this.version;
    this.errorPdf.set(null);
    this.api.interpretarFacturaPdf(archivo, proveedorId).pipe(
      filter(() => version === this.version),
      switchMap(f => {
        const halladas = new Map(f.lineas.filter(l => l.articuloId !== null).map(l => [l.codigoProveedor, l.articuloId!]));
        return forkJoin([...new Set(halladas.values())].map(id => this.api.ficha(id))).pipe(
          defaultIfEmpty([]), map(fichas => ({ halladas, fichas })));
      }),
    ).subscribe({
      next: ({ halladas, fichas }) => {
        if (version !== this.version) return;
        if (fichas.length > 0 && this.sucursales().length === 0)
          this.sucursales.set(destinos(fichas[0]));
        const porId = new Map(fichas.map(x => [x.id, x]));
        this.sinResolver = this.sinResolver.filter(({ revisar, revisarDescuento4, ...l }) => {
          const ficha = porId.get(halladas.get(l.codigoProveedor)!);
          if (!ficha) return true;
          this.lineas.push({
            articulo: { id: ficha.id, codigoInterno: ficha.codigoInterno, nombre: ficha.nombre, activo: ficha.activo, codigosProveedor: [] },
            sucursalId: this.sucursalCabecera, cantidad: l.cantidad, precioUnitario: l.precioUnitario,
            descuentoPorcentaje: l.descuentoPorcentaje, descuento2Porcentaje: l.descuento2Porcentaje,
            descuento3Porcentaje: l.descuento3Porcentaje, descuento4Porcentaje: l.descuento4Porcentaje, revisar, revisarDescuento4,
          });
          return false;
        });
        this.paraRevisar.update(r => r.filter(x => x !== 'proveedorId'));
      },
      error: e => { if (version === this.version) this.errorPdf.set(mensajeDeError(e)); },
    });
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
    const texto = this.textoArticulo;
    this.buscarArticulo(a => {
      const exacto = exactoArticulo(a, texto);
      if (exacto) this.elegirArticulo(exacto);
      return !!exacto;
    });
  }

  elegirArticulo(articulo: ArticuloResultado) {
    this.articulos.set([]);
    const reemplazo = this.reemplazando();
    // #169 (D9): repetido es el mismo artículo en el mismo destino; a otra sucursal es otra línea.
    const destino = reemplazo !== null ? this.lineas[reemplazo].sucursalId : this.sucursalCabecera;
    if (this.lineas.some((l, i) => l.articulo.id === articulo.id && l.sucursalId === destino && i !== reemplazo)) {
      this.errorArticulo.set(`${articulo.codigoInterno} ya está en la factura: corregí su línea.`);
      return;
    }
    this.textoArticulo = '';
    this.reemplazando.set(null);
    this.invalidarTotales();
    // CA-010: reemplazar cambia solo el artículo; cantidad, destino y descuentos quedan.
    // 049 CA-003: si se está resolviendo una línea del PDF, la línea nueva lleva sus datos.
    const i = this.resolviendo();
    const p = i !== null ? this.sinResolver.splice(i, 1)[0] : null;
    this.resolviendo.set(null);
    const linea: LineaEnCarga = reemplazo !== null ? this.lineas[reemplazo] : {
      articulo, sucursalId: this.sucursalCabecera, cantidad: p?.cantidad ?? null, precioUnitario: p?.precioUnitario ?? null,
      descuentoPorcentaje: p?.descuentoPorcentaje ?? 0, descuento2Porcentaje: p?.descuento2Porcentaje ?? 0,
      descuento3Porcentaje: p?.descuento3Porcentaje ?? 0, descuento4Porcentaje: p?.descuento4Porcentaje ?? 0,
      revisar: p?.revisar, revisarDescuento4: p?.revisarDescuento4,
    };
    linea.articulo = articulo;
    if (reemplazo === null) this.lineas.push(linea);
    this.api.ficha(articulo.id).subscribe(f => {
      if (this.sucursales().length === 0)
        this.sucursales.set(destinos(f));
      // CA-011: se propone el costo vigente; sin costo no se propone nada (nunca cero, D1).
      if (linea.precioUnitario === null && linea.articulo.id === articulo.id) linea.precioUnitario = f.costoVigente;
    });
  }

  // #170: la regla de la #169 también al cambiar el destino a mano; si repite, el select vuelve al anterior.
  cambiarDestino(i: number, sucursalId: number, control: NgModel) {
    const linea = this.lineas[i];
    // Sin excluir la propia línea: nunca llega su mismo destino (reelegir la opción no dispara change y el rechazo deja el select igual al modelo).
    if (this.lineas.some(l => l.articulo.id === linea.articulo.id && l.sucursalId === sucursalId)) {
      this.errorArticulo.set(`${linea.articulo.codigoInterno} ya está en la factura: corregí su línea.`);
      control.control.setValue(linea.sucursalId, { emitViewToModelChange: false });
      return;
    }
    linea.sucursalId = sucursalId;
  }

  reemplazar(i: number) {
    this.resolviendo.set(null);
    this.reemplazando.set(this.reemplazando() === i ? null : i);
  }

  resolver(i: number) {
    this.reemplazando.set(null);
    this.resolviendo.set(this.resolviendo() === i ? null : i);
  }

  quitarSinResolver(i: number) {
    this.sinResolver.splice(i, 1);
    this.resolviendo.set(null);
    this.invalidarTotales();
  }

  /** 049 CA-004: cuántas líneas del PDF faltan resolver. */
  faltan() {
    const n = this.sinResolver.length;
    return n === 1 ? 'Falta resolver 1 línea' : `Faltan resolver ${n} líneas`;
  }

  quitarLinea(i: number) {
    this.lineas.splice(i, 1);
    this.reemplazando.set(null);
    this.invalidarTotales();
  }

  invalidarTotales() {
    this.totales.set(null);
  }

  /** Fix A: interpreta lo tipeado; si no es una fecha, lo deja como está, lo dice y el campo queda sin fecha. */
  fijarFecha(campo: CampoFecha) {
    const r = interpretarFecha(this.textoFecha[campo], this.reloj());
    this.errorFecha.update(e => ({ ...e, [campo]: 'error' in r ? r.error : undefined }));
    this[campo] = 'iso' in r ? r.iso : '';
    if ('iso' in r) this.textoFecha[campo] = aTextoFecha(r.iso);
    this.revisarOrdenFechas();
  }

  /** El vencimiento nunca es anterior a la emisión (Javi, 21/11): se avisa en el vencimiento y bloquea el registro. */
  private revisarOrdenFechas() {
    const actual = this.errorFecha().fechaVencimiento;
    if (actual && actual !== VENCIMIENTO_ANTERIOR) return; // lo mal tipeado se corrige primero
    const fuera = !!this.fechaEmision && !!this.fechaVencimiento && this.fechaVencimiento < this.fechaEmision;
    this.errorFecha.update(e => ({ ...e, fechaVencimiento: fuera ? VENCIMIENTO_ANTERIOR : undefined }));
  }

  /** Lo que muestra cada campo de fecha, a partir de las fechas de la factura (precarga del PDF y formulario nuevo). */
  private mostrarFechas() {
    this.textoFecha = {
      fechaEmision: aTextoFecha(this.fechaEmision),
      fechaVencimiento: aTextoFecha(this.fechaVencimiento),
      fechaImputacion: aTextoFecha(this.fechaImputacion),
    };
    this.errorFecha.set({});
  }

  private factura(): FacturaCompraNueva | null {
    const proveedor = this.proveedor();
    if (!proveedor) {
      this.errorRegistro.set('Elegí el proveedor de la factura.');
      return null;
    }
    // Una fecha mal tipeada no viaja vacía en silencio: se corrige antes. La precarga del PDF no pasa por los campos.
    this.revisarOrdenFechas();
    if (this.camposFecha.some(f => this.errorFecha()[f.campo])) {
      this.errorRegistro.set('Corregí las fechas señaladas.');
      return null;
    }
    if (this.sinResolver.length > 0) {
      this.errorRegistro.set(`${this.faltan()}.`);
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
        descuento4Porcentaje: Number(l.descuento4Porcentaje) || 0,
      })),
      ...(this.desdePdf ? { desdePdf: true } : {}),
    };
  }

  /** CA-015: el total (lo calcula la API, con el IVA del tipo de comprobante) antes de guardar nada. */
  registrar() {
    const factura = this.factura();
    if (!factura) return;
    this.errorRegistro.set(null);
    this.registrando.set(true);
    const version = this.version;
    this.api.calcularFacturaCompra(factura).subscribe({
      next: t => {
        this.registrando.set(false);
        if (version === this.version) this.totales.set(t);
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

  elegirPdf(evento: Event) {
    const entrada = evento.target as HTMLInputElement;
    const archivo = entrada.files?.[0];
    // Vaciarlo permite volver a elegir el mismo archivo.
    entrada.value = '';
    if (archivo) this.cargarPdf(archivo);
  }

  /** 049 CA-001 y CA-005: interpreta el PDF (029) y precarga el formulario; nada se guarda. */
  cargarPdf(archivo: File) {
    this.errorPdf.set(null);
    this.leyendoPdf.set(true);
    this.api.interpretarFacturaPdf(archivo).pipe(
      switchMap(f => forkJoin({
        f: of(f),
        proveedor: f.proveedorId === null ? of(null) : this.api.fichaTercero('proveedores', f.proveedorId),
        fichas: forkJoin(f.lineas.filter(l => l.articuloId !== null).map(l => this.api.ficha(l.articuloId!))).pipe(defaultIfEmpty([])),
      })),
    ).subscribe({
      next: ({ f, proveedor, fichas }) => {
        this.nueva();
        // Fix E: la sucursal sugerida por el PDF es el destino de las líneas; el operador la cambia.
        if (f.sucursalSugeridaId != null) this.sucursalCabecera = f.sucursalSugeridaId;
        if (proveedor) this.proveedor.set({
          id: f.proveedorId!, codigo: proveedor.codigo, razonSocial: proveedor.razonSocial, cuit: proveedor.cuit,
          activo: proveedor.activo, tipoComprobanteHabitual: proveedor.tipoComprobanteHabitual ?? null,
        });
        // El tipo es el del comprobante, no el habitual del proveedor; un B sin número propone el 99.
        this.numero = f.numero ?? '';
        this.cambiarTipo(f.tipoComprobante ?? '');
        this.fechaEmision = f.fechaEmision ?? '';
        this.fechaVencimiento = f.fechaVencimiento ?? '';
        this.mostrarFechas();
        this.percepciones = {
          iva: f.percepcionIva ?? 0, iibb: f.percepcionIibb ?? 0,
          municipal: f.percepcionMunicipal ?? 0, ganancias: f.percepcionGanancias ?? 0,
        };
        if (fichas.length > 0 && this.sucursales().length === 0)
          this.sucursales.set(destinos(fichas[0]));
        const porId = new Map(fichas.map(x => [x.id, x]));
        f.lineas.forEach(({ articuloId, ...l }, i) => {
          const revisar = f.paraRevisar.includes(`lineas[${i}]`);
          const revisarDescuento4 = f.paraRevisar.includes(`lineas[${i}].descuento4`);
          const ficha = articuloId === null ? undefined : porId.get(articuloId);
          if (!ficha) {
            this.sinResolver.push({ ...l, revisar, revisarDescuento4 });
            return;
          }
          this.lineas.push({
            articulo: { id: ficha.id, codigoInterno: ficha.codigoInterno, nombre: ficha.nombre, activo: ficha.activo, codigosProveedor: [] },
            sucursalId: this.sucursalCabecera, cantidad: l.cantidad, precioUnitario: l.precioUnitario,
            descuentoPorcentaje: l.descuentoPorcentaje, descuento2Porcentaje: l.descuento2Porcentaje,
            descuento3Porcentaje: l.descuento3Porcentaje, descuento4Porcentaje: l.descuento4Porcentaje, revisar, revisarDescuento4,
          });
        });
        this.paraRevisar.set(f.paraRevisar);
        this.desdePdf = true;
        this.archivoPdf = archivo;
        this.leyendoPdf.set(false);
      },
      error: e => {
        this.leyendoPdf.set(false);
        this.errorPdf.set(mensajeDeError(e));
      },
    });
  }

  /** 049 CA-002: el campo que el PDF no pudo afirmar. */
  revisar(campo: string) {
    return this.paraRevisar().includes(campo);
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
    this.fechaImputacion = hoyIso(this.reloj());
    this.mostrarFechas();
    this.percepciones = { iva: 0, iibb: 0, municipal: 0, ganancias: 0 };
    this.lineas = [];
    this.totales.set(null);
    this.sinResolver = [];
    this.resolviendo.set(null);
    this.paraRevisar.set([]);
    this.desdePdf = false;
    this.errorPdf.set(null);
    this.version++;
  }
}
