import { Component, inject, input, signal } from '@angular/core';
import { ConfirmacionMotivo } from '../../compartido/confirmacion-motivo';
import { CampoFormulario, Formulario, ValoresFormulario } from '../../compartido/formulario/formulario';
import { ErroresServidor, erroresDelServidor } from '../../compartido/formulario/errores-servidor';
import { Consulta, Listado } from '../../compartido/listado';
import { ApiLaCentral, BaseTerceros, FichaTercero, TerceroResumen, mensajeDeError } from '../../nucleo/api';

/** Lo único que distingue la pantalla de clientes de la de proveedores. */
interface ConfigTerceros {
  base: BaseTerceros;
  singular: string;
  plural: string;
  camposAlta: CampoFormulario[];
  camposModificacion: CampoFormulario[];
  /** El nombre del motivo en el cuerpo de la baja: cada endpoint lo llama distinto. */
  campoMotivo: 'motivo' | 'motivoBaja';
  /** Lo que se reescribe para confirmar la baja (044): lo que compara el caso de uso de la API. */
  claveBaja: (f: FichaTercero) => string;
}

/** Los valores que admite el CHECK cliente_condicion_fiscal_valida. Sin elegir es válido (se guarda NULL). */
const CONDICIONES_FISCALES = [
  { valor: 'RESPONSABLE_INSCRIPTO', etiqueta: 'Responsable inscripto' },
  { valor: 'MONOTRIBUTO', etiqueta: 'Monotributo' },
  { valor: 'EXENTO', etiqueta: 'Exento' },
  { valor: 'CONSUMIDOR_FINAL', etiqueta: 'Consumidor final' },
  { valor: 'NO_CATEGORIZADO', etiqueta: 'No categorizado' },
];

// Los largos son los de los DTOs de la API.
const CODIGO: CampoFormulario = { nombre: 'codigo', etiqueta: 'Código', obligatorio: true, maximo: 20 };
const RAZON_SOCIAL: CampoFormulario = { nombre: 'razonSocial', etiqueta: 'Razón social', obligatorio: true, maximo: 120 };
const CUIT: CampoFormulario = {
  nombre: 'cuit', etiqueta: 'CUIT', maximo: 13, patron: '\\d{2}-\\d{8}-\\d', mensajeFormato: 'El CUIT va como 30-12345678-9.',
};
const CONTACTO: CampoFormulario[] = [
  { nombre: 'telefonos', etiqueta: 'Teléfonos', multivaluado: true, tipo: 'tel' },
  { nombre: 'direcciones', etiqueta: 'Direcciones', multivaluado: true },
];

const CLIENTES: ConfigTerceros = (() => {
  const campos: CampoFormulario[] = [
    CODIGO, RAZON_SOCIAL, CUIT,
    { nombre: 'condicionFiscal', etiqueta: 'Condición fiscal', opciones: CONDICIONES_FISCALES },
    { nombre: 'condicionPago', etiqueta: 'Condición de pago', obligatorio: true, maximo: 60, ayuda: 'Por ejemplo: Contado, Cuenta corriente 30 días.' },
    ...CONTACTO,
  ];
  return { base: 'clientes', singular: 'cliente', plural: 'clientes', camposAlta: campos, camposModificacion: campos, campoMotivo: 'motivo',
    // DarDeBajaClienteUseCase: el CUIT si lo tiene; si no, el código.
    claveBaja: f => f.cuit || f.codigo };
})();

const URL: CampoFormulario = { nombre: 'urlReferencia', etiqueta: 'URL de referencia', maximo: 300 };
// HU-FCO-06 CA-004: solo el valor que propone la factura de compra; puede emitir A y B.
const COMPROBANTE: CampoFormulario = {
  nombre: 'tipoComprobanteHabitual', etiqueta: 'Comprobante habitual',
  opciones: [{ valor: 'A', etiqueta: 'A' }, { valor: 'B', etiqueta: 'B' }],
  ayuda: 'Lo que suele emitir. La factura lo propone y se puede cambiar.',
};
const PROVEEDORES: ConfigTerceros = {
  base: 'proveedores', singular: 'proveedor', plural: 'proveedores',
  camposAlta: [CODIGO, RAZON_SOCIAL, CUIT, URL, COMPROBANTE, ...CONTACTO],
  // PUT /api/proveedores/{id} no cambia el código y exige el CUIT.
  camposModificacion: [RAZON_SOCIAL, { ...CUIT, obligatorio: true }, URL, COMPROBANTE, ...CONTACTO],
  campoMotivo: 'motivoBaja',
  // DarDeBajaProveedorUseCase acepta el código o el CUIT: el código lo tienen todos.
  claveBaja: f => f.codigo,
};

const SIN_ERRORES: ErroresServidor = { porCampo: {}, general: null };

/** Lo que sale del formulario, como lo espera la API: vacío es null y las listas no llevan renglones en blanco. */
function cuerpo(campos: CampoFormulario[], valores: ValoresFormulario): Record<string, unknown> {
  return Object.fromEntries(campos.map(c => {
    const v = valores[c.nombre];
    if (Array.isArray(v)) return [c.nombre, v.map(x => String(x ?? '').trim()).filter(x => x !== '')];
    const texto = String(v ?? '').trim();
    return [c.nombre, texto === '' ? null : texto];
  }));
}

/**
 * 045 · HU-UI-07. Una sola pantalla configurada para clientes o proveedores, armada con los
 * componentes reutilizables: listado (HU-UI-04), formulario (HU-UI-05) y confirmación con motivo
 * (HU-UI-06, reescribiendo el código como pide la 044).
 */
@Component({
  selector: 'app-terceros',
  imports: [Listado, Formulario, ConfirmacionMotivo],
  styles: `
    .ficha-datos { display: grid; grid-template-columns: max-content 1fr; gap: var(--esp-8) var(--esp-16); margin: 0 0 var(--esp-16); }
    .ficha-datos dt { font-size: var(--t-etiqueta); }
    .ficha-datos dd { margin: 0; overflow-wrap: anywhere; }
    .ficha-datos ul { margin: 0; padding-left: var(--esp-16); }
  `,
  template: `
    <app-listado [consultar]="consultar" [columnas]="['Código', 'Razón social', 'CUIT', '']"
      [etiqueta]="'Buscar ' + config().plural" [nombrePlural]="config().plural" ayuda="Razón social (aproximada) o CUIT exacto.">
      <button type="button" (click)="abrirAlta()">Nuevo {{ config().singular }}</button>
      <ng-template #fila let-t>
        <td class="mono">{{ t.codigo }}</td>
        <td>{{ t.razonSocial }}</td>
        <td class="mono">{{ t.cuit ?? '—' }}</td>
        <td><button type="button" class="chico" (click)="verFicha(t)">Ver ficha</button></td>
      </ng-template>
    </app-listado>

    @if (aviso(); as mensaje) { <p class="aviso ok" role="status">{{ mensaje }}</p> }

    @if (alta()) {
      <section class="tarjeta">
        <h2>Nuevo {{ config().singular }}</h2>
        <app-formulario [campos]="config().camposAlta" [enviando]="enviando()"
          [erroresPorCampo]="errores().porCampo" [errorGeneral]="errores().general" (enviar)="crear($event)">
          <button type="button" acciones (click)="alta.set(false)">Cancelar</button>
        </app-formulario>
      </section>
    }

    @if (errorFicha(); as mensaje) { <p class="aviso error" role="alert">{{ mensaje }}</p> }

    @if (ficha(); as f) {
      <section class="tarjeta">
        <h2>{{ f.razonSocial }}</h2>
        @if (editando()) {
          <app-formulario [campos]="config().camposModificacion" [valores]="$any(f)" [enviando]="enviando()"
            [erroresPorCampo]="errores().porCampo" [errorGeneral]="errores().general" (enviar)="modificar($event)">
            <button type="button" acciones (click)="editando.set(false)">Cancelar</button>
          </app-formulario>
        } @else {
          <dl class="ficha-datos">
            <dt>Código</dt><dd class="mono">{{ f.codigo }}</dd>
            <dt>CUIT</dt><dd class="mono">{{ f.cuit ?? '—' }}</dd>
            @if (config().base === 'clientes') {
              <dt>Condición fiscal</dt><dd>{{ condicionFiscal(f.condicionFiscal) }}</dd>
              <dt>Condición de pago</dt><dd>{{ f.condicionPago || '—' }}</dd>
            } @else {
              <dt>URL de referencia</dt>
              <dd>
                @if (f.urlReferencia) {
                  <a [href]="f.urlReferencia" target="_blank" rel="noopener noreferrer">{{ f.urlReferencia }}</a>
                } @else { — }
              </dd>
              <dt>Comprobante habitual</dt><dd>{{ f.tipoComprobanteHabitual ?? '—' }}</dd>
            }
            <dt>Estado</dt><dd>{{ f.activo ? 'Activo' : 'Dado de baja' }}@if (!f.activo && f.motivoBaja) {: {{ f.motivoBaja }}}</dd>
            <dt>Teléfonos</dt>
            <dd>@if (f.telefonos.length) { <ul>@for (t of f.telefonos; track $index) { <li>{{ t }}</li> }</ul> } @else { — }</dd>
            <dt>Direcciones</dt>
            <dd>@if (f.direcciones.length) { <ul>@for (d of f.direcciones; track $index) { <li>{{ d }}</li> }</ul> } @else { — }</dd>
          </dl>
          <div class="fila">
            @if (f.activo) {
              <button type="button" (click)="abrirModificacion()">Modificar</button>
              <button type="button" class="peligro" (click)="errorBaja.set(null); bajaAbierta.set(true)">Dar de baja</button>
            } @else {
              <button type="button" (click)="reactivar()">Reactivar</button>
            }
          </div>
          @if (errorEstado(); as mensaje) { <p class="aviso error" role="alert">{{ mensaje }}</p> }
        }
      </section>

      <app-confirmacion-motivo [titulo]="'Dar de baja ' + f.codigo" [(abierto)]="bajaAbierta" [maximoMotivo]="255"
        [clave]="config().claveBaja(f)" textoConfirmar="Dar de baja" [enviando]="enviando()" [errorGeneral]="errorBaja()"
        (confirmar)="darDeBaja($event)" />
    }
  `,
})
export class Terceros {
  readonly config = input.required<ConfigTerceros>();
  private readonly api = inject(ApiLaCentral);

  readonly consultar: Consulta<TerceroResumen> = (texto, incluirInactivos, pagina, tamanoPagina) =>
    this.api.terceros(this.config().base, texto, incluirInactivos, pagina, tamanoPagina);

  readonly alta = signal(false);
  readonly editando = signal(false);
  readonly bajaAbierta = signal(false);
  readonly enviando = signal(false);
  readonly aviso = signal<string | null>(null);
  readonly errores = signal<ErroresServidor>(SIN_ERRORES);
  readonly ficha = signal<FichaTercero | null>(null);
  readonly errorFicha = signal<string | null>(null);
  readonly errorBaja = signal<string | null>(null);
  readonly errorEstado = signal<string | null>(null);
  private id = 0;

  condicionFiscal(valor: string | null | undefined) {
    return CONDICIONES_FISCALES.find(c => c.valor === valor)?.etiqueta ?? 'Sin informar';
  }

  verFicha(t: TerceroResumen) {
    this.id = t.id;
    this.alta.set(false);
    this.cargarFicha();
  }

  private cargarFicha() {
    this.api.fichaTercero(this.config().base, this.id).subscribe({
      next: f => {
        this.ficha.set(f);
        this.errorFicha.set(null);
        this.errorEstado.set(null);
        this.editando.set(false);
      },
      error: e => {
        this.ficha.set(null);
        this.errorFicha.set(mensajeDeError(e));
      },
    });
  }

  abrirAlta() {
    this.ficha.set(null);
    this.aviso.set(null);
    this.errores.set(SIN_ERRORES);
    this.alta.set(true);
  }

  abrirModificacion() {
    this.errores.set(SIN_ERRORES);
    this.editando.set(true);
  }

  crear(valores: ValoresFormulario) {
    const campos = this.config().camposAlta;
    this.enviar(this.api.crearTercero(this.config().base, cuerpo(campos, valores)), campos, respuesta => {
      this.alta.set(false);
      const advertencia = (respuesta as { advertencia?: string | null } | null)?.advertencia;
      this.aviso.set(`${this.config().singular[0].toUpperCase()}${this.config().singular.slice(1)} ${valores['codigo']} dado de alta.`
        + (advertencia ? ` ${advertencia}` : ''));
    });
  }

  modificar(valores: ValoresFormulario) {
    const campos = this.config().camposModificacion;
    this.enviar(this.api.modificarTercero(this.config().base, this.id, cuerpo(campos, valores)), campos, () => this.cargarFicha());
  }

  darDeBaja(motivo: string) {
    const baja = { confirmacion: this.config().claveBaja(this.ficha()!), [this.config().campoMotivo]: motivo };
    this.enviando.set(true);
    this.api.darDeBajaTercero(this.config().base, this.id, baja).subscribe({
      next: () => {
        this.enviando.set(false);
        this.bajaAbierta.set(false);
        this.cargarFicha();
      },
      error: e => {
        this.enviando.set(false);
        this.errorBaja.set(mensajeDeError(e));
      },
    });
  }

  reactivar() {
    this.api.reactivarTercero(this.config().base, this.id).subscribe({
      next: () => this.cargarFicha(),
      error: e => this.errorEstado.set(mensajeDeError(e)),
    });
  }

  private enviar<R>(pedido: import('rxjs').Observable<R>, campos: CampoFormulario[], listo: (r: R) => void) {
    this.enviando.set(true);
    this.errores.set(SIN_ERRORES);
    pedido.subscribe({
      next: r => {
        this.enviando.set(false);
        listo(r);
      },
      error: e => {
        this.enviando.set(false);
        this.errores.set(erroresDelServidor(e, campos));
      },
    });
  }
}

@Component({ selector: 'app-clientes', imports: [Terceros], template: `<app-terceros [config]="config" />` })
export class Clientes {
  readonly config = CLIENTES;
}

@Component({ selector: 'app-proveedores', imports: [Terceros], template: `<app-terceros [config]="config" />` })
export class Proveedores {
  readonly config = PROVEEDORES;
}
