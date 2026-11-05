import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { map } from 'rxjs';
import { ConfirmacionMotivo } from '../../compartido/confirmacion-motivo';
import { Consulta, Listado } from '../../compartido/listado';
import {
  AltaArticulo, ApiLaCentral, ArticuloResultado, FichaArticulo, Modalidad, ModificacionArticulo,
  RazonAusencia, mensajeDeError,
} from '../../nucleo/api';
import { SesionActual } from '../../nucleo/sesion';

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
  imports: [FormsModule, DecimalPipe, Listado, ConfirmacionMotivo],
  template: `
    <app-listado [consultar]="consultar" [columnas]="['Código interno', 'Nombre', 'Códigos de proveedor', '']"
      etiqueta="Buscar artículos" nombrePlural="artículos"
      ayuda="Código interno, código de proveedor o nombre. Usá * para buscar alternativas: filtro * correa">
      <button type="button" (click)="abrirAlta()">Nuevo artículo</button>
      <ng-template #fila let-a>
        <td class="mono">{{ a.codigoInterno }}</td>
        <td>{{ a.nombre }}</td>
        <td>
          @for (c of a.codigosProveedor; track c.proveedorId) {
            <span class="codigo" [title]="c.razonSocial">{{ c.codigo }}</span>
          } @empty { <span class="tenue">—</span> }
        </td>
        <td><button type="button" class="chico" (click)="verFicha(a)">Ver ficha</button></td>
      </ng-template>
    </app-listado>

    @if (alta()) {
      <section class="tarjeta">
        <h2>Nuevo artículo</h2>
        <form (ngSubmit)="crear()">
          <label>Nombre <input name="nombre" [(ngModel)]="nuevo.nombre" maxlength="200" required /></label>
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
              <tr>
                <td>{{ e.sucursal }}</td>
                <td [class.error]="e.cantidad < 0">{{ e.cantidad | number: '1.0-3' }}</td>
                <td>
                  @if (f.activo) {
                    <input [name]="'ubicacion' + e.sucursalId" [(ngModel)]="ubicaciones[e.sucursalId]" maxlength="60"
                      placeholder="Ubicación en depósito" [attr.aria-label]="'Ubicación en ' + e.sucursal" />
                    <button type="button" class="secundario chico" (click)="guardarUbicacion(f, e.sucursalId)">Guardar</button>
                  } @else {
                    {{ e.ubicacionDeposito ?? '' }}
                  }
                </td>
              </tr>
            }
            <tr><th>Total</th><th [class.error]="f.totalExistencias < 0">{{ f.totalExistencias | number: '1.0-3' }}</th><th></th></tr>
          </tbody>
        </table>
        @if (errorUbicacion()) { <p class="error">{{ errorUbicacion() }}</p> }

        @if (f.activo) {
          <h3>Modificar</h3>
          <form (ngSubmit)="modificar(f)">
            <label>Nombre <input name="mNombre" [(ngModel)]="edicion.nombre" maxlength="200" required /></label>
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
          <button type="button" class="peligro" (click)="errorBaja.set(null); bajaAbierta.set(true)">Dar de baja</button>
          <!-- 014 · CA-004: se reescribe el código interno, como pide la 044. -->
          <app-confirmacion-motivo [titulo]="'Dar de baja ' + f.codigoInterno" [(abierto)]="bajaAbierta" [maximoMotivo]="200"
            [clave]="f.codigoInterno" textoConfirmar="Dar de baja" [enviando]="enviandoBaja()" [errorGeneral]="errorBaja()"
            (confirmar)="darDeBaja(f, $event)" />
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
  readonly consultar: Consulta<ArticuloResultado> = (texto, incluirInactivos, pagina, tamanoPagina) =>
    this.api.buscar(texto, incluirInactivos, pagina, tamanoPagina)
      .pipe(map(p => ({ items: p.articulos, total: p.total, pagina: p.pagina, tamanoPagina: p.tamanoPagina })));

  readonly ficha = signal<FichaArticulo | null>(null);
  readonly errorFicha = signal<string | null>(null);

  readonly alta = signal(false);
  readonly errorAlta = signal<string | null>(null);
  nuevo: AltaArticulo = Articulos.altaVacia();

  verFicha(articulo: ArticuloResultado) {
    this.cargarFicha(articulo.id);
  }

  private cargarFicha(id: number) {
    this.errorFicha.set(null);
    this.api.ficha(id).subscribe({
      next: f => {
        this.ficha.set(f);
        this.edicion = Articulos.edicionDe(f);
        this.ubicaciones = Object.fromEntries(f.existencias.map(e => [e.sucursalId, e.ubicacionDeposito ?? '']));
        this.errorUbicacion.set(null);
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
    return { nombre: '', codigosProveedor: [] };
  }

  // ---- HU-ART-03 ----

  private readonly sesion = inject(SesionActual);
  /** Solo para mostrar u ocultar controles: los permisos los decide la API con la sesión. */
  readonly esAdministrador = computed(() => this.sesion.sesion()?.rol === 'ADMINISTRADOR');

  edicion: ModificacionArticulo = { nombre: '', codigoInterno: null, codigosProveedor: [] };
  /** N-9 · lo que se escribe en la ubicación de cada sucursal, por Id de sucursal. */
  ubicaciones: Record<number, string> = {};
  readonly errorUbicacion = signal<string | null>(null);
  readonly bajaAbierta = signal(false);
  readonly enviandoBaja = signal(false);
  readonly errorBaja = signal<string | null>(null);
  readonly errorModificacion = signal<string | null>(null);
  readonly errorCodigos = signal<string | null>(null);
  readonly errorEstado = signal<string | null>(null);

  private static edicionDe(f: FichaArticulo): ModificacionArticulo {
    return { nombre: f.nombre, codigoInterno: f.codigoInterno, codigosProveedor: [] };
  }

  guardarUbicacion(f: FichaArticulo, sucursalId: number) {
    this.errorUbicacion.set(null);
    this.api.asignarUbicacion(f.id, sucursalId, this.ubicaciones[sucursalId]?.trim() || null).subscribe({
      next: () => this.cargarFicha(f.id),
      error: e => this.errorUbicacion.set(mensajeDeError(e)),
    });
  }

  modificar(f: FichaArticulo) {
    this.errorModificacion.set(null);
    const cambios: ModificacionArticulo = {
      nombre: this.edicion.nombre,
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

  darDeBaja(f: FichaArticulo, motivo: string) {
    this.enviandoBaja.set(true);
    this.api.darDeBaja(f.id, motivo, f.codigoInterno).subscribe({
      next: () => {
        this.enviandoBaja.set(false);
        this.bajaAbierta.set(false);
        this.cargarFicha(f.id);
      },
      // CA-002: lo que queda en cada sucursal lo informa la API; el diálogo sigue abierto.
      error: e => {
        this.enviandoBaja.set(false);
        this.errorBaja.set(mensajeDeError(e));
      },
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
