import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AltaArticulo, ApiLaCentral, ArticuloResultado, FichaArticulo, Modalidad, ModificacionArticulo, PaginaArticulos,
  RazonAusencia, mensajeDeError,
} from './api';
import { SesionActual } from './sesion';

const NOMBRE_MODALIDAD: Record<Modalidad, string> = {
  Lista: 'Lista',
  Tarjeta3: 'Tarjeta 3 cuotas',
  Tarjeta6: 'Tarjeta 6 cuotas',
  EfectivoTransferencia: 'Efectivo / transferencia',
  Costo: 'Costo',
  CasoEspecial: 'Caso especial',
};

const TEXTO_RAZON: Record<RazonAusencia, string> = {
  SinConfiguracion: 'sin configuración',
  SinCosto: 'sin costo',
  CasoEspecialNoFijado: 'caso especial no fijado',
  TasaSinDefinir: 'tasa sin definir',
};

@Component({
  selector: 'app-articulos',
  imports: [FormsModule, DecimalPipe],
  template: `
    <section class="tarjeta">
      <form class="fila" (ngSubmit)="buscar(1)">
        <input name="texto" [(ngModel)]="texto" placeholder="Código interno, código de proveedor o nombre (alternativas con *)" />
        <label class="check"><input type="checkbox" name="inactivos" [(ngModel)]="incluirInactivos" /> incluir inactivos</label>
        <button type="submit">Buscar</button>
        <button type="button" class="secundario" (click)="abrirAlta()">Nuevo artículo</button>
      </form>
      @if (errorBusqueda()) { <p class="error">{{ errorBusqueda() }}</p> }

      @if (pagina(); as p) {
        @if (p.articulos.length === 0) {
          <p class="vacio">Sin resultados para «{{ ultimoTexto }}».</p>
        } @else {
          <table>
            <thead><tr><th>Código interno</th><th>Nombre</th><th>Códigos de proveedor</th></tr></thead>
            <tbody>
              @for (a of p.articulos; track a.id) {
                <tr (click)="verFicha(a)" [class.elegido]="ficha()?.id === a.id" [class.inactivo]="!a.activo">
                  <td class="mono">{{ a.codigoInterno }}</td>
                  <td>{{ a.nombre }} @if (!a.activo) { <span class="etiqueta">inactivo</span> }</td>
                  <td>
                    @for (c of a.codigosProveedor; track c.proveedorId) {
                      <span class="codigo" [title]="c.razonSocial">{{ c.codigo }}</span>
                    } @empty { <span class="tenue">—</span> }
                  </td>
                </tr>
              }
            </tbody>
          </table>
          <div class="fila paginado">
            <button class="secundario" [disabled]="p.pagina <= 1" (click)="buscar(p.pagina - 1)">‹ Anterior</button>
            <span>Página {{ p.pagina }} de {{ paginas(p) }} · {{ p.total }} artículos</span>
            <button class="secundario" [disabled]="p.pagina >= paginas(p)" (click)="buscar(p.pagina + 1)">Siguiente ›</button>
          </div>
        }
      }
    </section>

    @if (alta()) {
      <section class="tarjeta">
        <h2>Nuevo artículo</h2>
        <form (ngSubmit)="crear()">
          <label>Nombre <input name="nombre" [(ngModel)]="nuevo.nombre" maxlength="200" required /></label>
          <label>Ubicación en depósito <input name="ubicacion" [(ngModel)]="nuevo.ubicacionDeposito" maxlength="60" /></label>
          <fieldset>
            <legend>Códigos de proveedor</legend>
            @for (c of nuevo.codigosProveedor; track $index; let i = $index) {
              <div class="fila">
                <input type="number" min="1" [name]="'prov' + i" [(ngModel)]="c.proveedorId" placeholder="Id proveedor" class="corto" />
                <input [name]="'cod' + i" [(ngModel)]="c.codigo" maxlength="60" placeholder="Código" />
                <button type="button" class="secundario" (click)="quitarCodigo(i)">Quitar</button>
              </div>
            }
            <button type="button" class="secundario" (click)="agregarCodigo()">+ Código</button>
          </fieldset>
          @if (errorAlta()) { <p class="error">{{ errorAlta() }}</p> }
          <div class="fila">
            <button type="submit">Guardar</button>
            <button type="button" class="secundario" (click)="alta.set(false)">Cancelar</button>
          </div>
        </form>
      </section>
    }

    @if (errorFicha()) { <p class="error">{{ errorFicha() }}</p> }
    @if (ficha(); as f) {
      <section class="tarjeta">
        <h2><span class="mono">{{ f.codigoInterno }}</span> · {{ f.nombre }}
          @if (!f.activo) { <span class="etiqueta">inactivo</span> }</h2>
        <p>Costo vigente: @if (f.costoVigente !== null) { $ {{ f.costoVigente | number: '1.2-2' }} } @else { <span class="tenue">sin costo</span> }</p>

        <h3>Precios por modalidad</h3>
        <table>
          <thead><tr><th>Modalidad</th><th>Precio</th><th>Regla</th></tr></thead>
          <tbody>
            @for (p of f.precios; track p.modalidad) {
              <tr>
                <td>{{ nombreModalidad(p.modalidad) }}</td>
                <td>
                  @if (p.precio !== null) { $ {{ p.precio | number: '1.2-2' }} }
                  @else { <span class="razon">{{ textoRazon(p.razonAusencia) }}</span> }
                </td>
                <td class="tenue">
                  @if (p.calculoManual) { manual }
                  @else if (p.base) { {{ p.base === 'CostoArticulo' ? 'costo' : 'lista' }} {{ p.porcentaje! >= 0 ? '+' : '' }}{{ p.porcentaje }} % }
                  @else { — }
                </td>
              </tr>
            }
          </tbody>
        </table>

        <h3>Códigos de proveedor</h3>
        @for (c of f.codigos; track c.proveedorId) {
          <p><span class="codigo">{{ c.codigoOriginal }}</span> {{ c.razonSocial }}
            @if (esAdministrador() && f.activo) {
              <button type="button" class="secundario" (click)="retirarCodigo(f, c.proveedorId)">Retirar</button>
            }
          </p>
        } @empty { <p class="tenue">Sin códigos de proveedor.</p> }
        @if (errorCodigos()) { <p class="error">{{ errorCodigos() }}</p> }

        <h3>Stock</h3>
        <table>
          <tbody>
            @for (e of f.existencias; track e.sucursalId) {
              <tr><td>{{ e.sucursal }}</td><td [class.error]="e.cantidad < 0">{{ e.cantidad | number: '1.0-3' }}</td></tr>
            }
            <tr><th>Total</th><th [class.error]="f.totalExistencias < 0">{{ f.totalExistencias | number: '1.0-3' }}</th></tr>
          </tbody>
        </table>

        @if (f.activo) {
          <h3>Modificar</h3>
          <form (ngSubmit)="modificar(f)">
            <label>Nombre <input name="mNombre" [(ngModel)]="edicion.nombre" maxlength="200" required /></label>
            <label>Ubicación en depósito <input name="mUbicacion" [(ngModel)]="edicion.ubicacionDeposito" maxlength="60" /></label>
            <label>Código interno
              <input name="mCodigoInterno" [(ngModel)]="edicion.codigoInterno" maxlength="40" [disabled]="!esAdministrador()" />
              @if (!esAdministrador()) { <span class="tenue">solo un administrador lo cambia</span> }
            </label>
            <fieldset>
              <legend>Agregar códigos de proveedor (los existentes se conservan)</legend>
              @for (c of edicion.codigosProveedor; track $index; let i = $index) {
                <div class="fila">
                  <input type="number" min="1" [name]="'mProv' + i" [(ngModel)]="c.proveedorId" placeholder="Id proveedor" class="corto" />
                  <input [name]="'mCod' + i" [(ngModel)]="c.codigo" maxlength="60" placeholder="Código" />
                  <button type="button" class="secundario" (click)="edicion.codigosProveedor.splice(i, 1)">Quitar</button>
                </div>
              }
              <button type="button" class="secundario" (click)="edicion.codigosProveedor.push({ proveedorId: $any(null), codigo: '' })">+ Código</button>
            </fieldset>
            @if (errorModificacion()) { <p class="error">{{ errorModificacion() }}</p> }
            <button type="submit">Guardar cambios</button>
          </form>

          <h3>Dar de baja</h3>
          <form class="fila" (ngSubmit)="darDeBaja(f)">
            <input name="motivo" [(ngModel)]="motivoBaja" maxlength="200" placeholder="Motivo de la baja" required />
            <button type="submit" class="secundario">Dar de baja</button>
          </form>
        } @else {
          <h3>Dado de baja</h3>
          <p>Motivo: {{ f.motivoBaja ?? '—' }}. Para modificarlo, primero hay que reactivarlo.</p>
          <button type="button" (click)="reactivar(f)">Reactivar</button>
        }
        @if (errorEstado()) { <p class="error">{{ errorEstado() }}</p> }
      </section>
    }
  `,
})
export class Articulos {
  private readonly api = inject(ApiLaCentral);
  private static readonly TAMANO_PAGINA = 20;

  texto = '';
  ultimoTexto = '';
  incluirInactivos = false;
  readonly pagina = signal<PaginaArticulos | null>(null);
  readonly errorBusqueda = signal<string | null>(null);

  readonly ficha = signal<FichaArticulo | null>(null);
  readonly errorFicha = signal<string | null>(null);

  readonly alta = signal(false);
  readonly errorAlta = signal<string | null>(null);
  nuevo: AltaArticulo = Articulos.altaVacia();

  buscar(numero: number) {
    this.errorBusqueda.set(null);
    this.api.buscar(this.texto, this.incluirInactivos, numero, Articulos.TAMANO_PAGINA).subscribe({
      next: p => {
        this.ultimoTexto = this.texto;
        this.pagina.set(p);
      },
      error: e => {
        this.pagina.set(null);
        this.errorBusqueda.set(mensajeDeError(e));
      },
    });
  }

  paginas(p: PaginaArticulos) {
    return Math.max(1, Math.ceil(p.total / p.tamanoPagina));
  }

  verFicha(articulo: ArticuloResultado) {
    this.cargarFicha(articulo.id);
  }

  private cargarFicha(id: number) {
    this.errorFicha.set(null);
    this.api.ficha(id).subscribe({
      next: f => {
        this.ficha.set(f);
        this.edicion = Articulos.edicionDe(f);
        this.motivoBaja = '';
        this.errorModificacion.set(null);
        this.errorCodigos.set(null);
        this.errorEstado.set(null);
      },
      error: e => {
        this.ficha.set(null);
        this.errorFicha.set(mensajeDeError(e));
      },
    });
  }

  nombreModalidad(m: Modalidad) {
    return NOMBRE_MODALIDAD[m];
  }

  textoRazon(r: RazonAusencia | null) {
    return r ? TEXTO_RAZON[r] : 'sin precio';
  }

  abrirAlta() {
    this.nuevo = Articulos.altaVacia();
    this.errorAlta.set(null);
    this.alta.set(true);
  }

  agregarCodigo() {
    this.nuevo.codigosProveedor.push({ proveedorId: null as unknown as number, codigo: '' });
  }

  quitarCodigo(i: number) {
    this.nuevo.codigosProveedor.splice(i, 1);
  }

  crear() {
    this.errorAlta.set(null);
    const alta: AltaArticulo = {
      nombre: this.nuevo.nombre,
      ubicacionDeposito: this.nuevo.ubicacionDeposito?.trim() || null,
      codigosProveedor: this.nuevo.codigosProveedor
        .filter(c => c.codigo.trim() !== '')
        .map(c => ({ proveedorId: Number(c.proveedorId), codigo: c.codigo })),
    };
    this.api.crear(alta).subscribe({
      next: creado => {
        this.alta.set(false);
        this.cargarFicha(creado.id);
      },
      error: e => this.errorAlta.set(mensajeDeError(e)),
    });
  }

  private static altaVacia(): AltaArticulo {
    return { nombre: '', ubicacionDeposito: '', codigosProveedor: [] };
  }

  // ---- HU-ART-03 ----

  private readonly sesion = inject(SesionActual);
  /** Solo para mostrar u ocultar controles: los permisos los decide la API con la sesión. */
  readonly esAdministrador = computed(() => this.sesion.sesion()?.rol === 'ADMINISTRADOR');

  edicion: ModificacionArticulo = { nombre: '', ubicacionDeposito: null, codigoInterno: null, codigosProveedor: [] };
  motivoBaja = '';
  readonly errorModificacion = signal<string | null>(null);
  readonly errorCodigos = signal<string | null>(null);
  readonly errorEstado = signal<string | null>(null);

  private static edicionDe(f: FichaArticulo): ModificacionArticulo {
    return { nombre: f.nombre, ubicacionDeposito: f.ubicacionDeposito ?? '', codigoInterno: f.codigoInterno, codigosProveedor: [] };
  }

  modificar(f: FichaArticulo) {
    this.errorModificacion.set(null);
    const cambios: ModificacionArticulo = {
      nombre: this.edicion.nombre,
      ubicacionDeposito: this.edicion.ubicacionDeposito?.trim() || null,
      // Un operador no lo puede cambiar: se manda null y la API conserva el actual.
      codigoInterno: this.esAdministrador() ? this.edicion.codigoInterno : null,
      codigosProveedor: this.edicion.codigosProveedor
        .filter(c => c.codigo.trim() !== '')
        .map(c => ({ proveedorId: Number(c.proveedorId), codigo: c.codigo })),
    };
    this.api.modificar(f.id, cambios).subscribe({
      next: () => this.cargarFicha(f.id),
      error: e => this.errorModificacion.set(mensajeDeError(e)),
    });
  }

  retirarCodigo(f: FichaArticulo, proveedorId: number) {
    this.errorCodigos.set(null);
    this.api.retirarCodigo(f.id, proveedorId).subscribe({
      next: () => this.cargarFicha(f.id),
      error: e => this.errorCodigos.set(mensajeDeError(e)),
    });
  }

  darDeBaja(f: FichaArticulo) {
    this.errorEstado.set(null);
    this.api.darDeBaja(f.id, this.motivoBaja).subscribe({
      next: () => this.cargarFicha(f.id),
      error: e => this.errorEstado.set(mensajeDeError(e)),
    });
  }

  reactivar(f: FichaArticulo) {
    this.errorEstado.set(null);
    this.api.reactivar(f.id).subscribe({
      next: () => this.cargarFicha(f.id),
      error: e => this.errorEstado.set(mensajeDeError(e)),
    });
  }
}
