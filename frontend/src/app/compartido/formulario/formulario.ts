import { Component, computed, input, output } from '@angular/core';
import {
  AbstractControl, FormArray, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators,
} from '@angular/forms';
import { BotonCarga } from '../boton-carga';

/** Un campo del formulario. `nombre` es la clave del valor y de los errores del servidor. */
export interface CampoFormulario {
  nombre: string;
  etiqueta: string;
  tipo?: 'text' | 'number' | 'date' | 'email' | 'tel';
  obligatorio?: boolean;
  /** Formato, como el atributo pattern: la expresión tiene que cubrir todo el valor. */
  patron?: string;
  mensajeFormato?: string;
  ayuda?: string;
  /** Una lista que se agrega y se quita (teléfonos, direcciones, códigos). */
  multivaluado?: boolean;
}

export type ValoresFormulario = Record<string, unknown>;

type Lista = FormArray<FormControl<string | null>>;

const OBLIGATORIO = 'Este campo es obligatorio.';
const FORMATO = 'El formato no es válido.';
let siguienteId = 0;

/** Obligatorio sin aceptar solo espacios; en una lista, al menos un valor. */
function obligatorio(control: AbstractControl): ValidationErrors | null {
  const valor = control.value;
  const vacio = (v: unknown) => v === null || v === undefined || String(v).trim() === '';
  return (Array.isArray(valor) ? valor.every(vacio) : vacio(valor)) ? { obligatorio: true } : null;
}

/**
 * Formulario reutilizable (HU-UI-05). Anatomía del campo: SISTEMA-VISUAL.md §8; botón: §7.
 *
 * Los errores del servidor entran por dos vías separadas, sin traducir texto: `erroresPorCampo`
 * (campo → mensaje) y `errorGeneral`. Hoy el mapa sale de los `errors` por propiedad de un 400
 * de validación; cuando el backend diga el campo de un rechazo de negocio, cambia lo que la
 * pantalla pasa, no este componente.
 *
 * Valida en el cliente solo para no llamar a la API de más: la API valida siempre.
 */
@Component({
  selector: 'app-formulario',
  imports: [ReactiveFormsModule, BotonCarga],
  styles: `
    .campo { display: flex; flex-direction: column; gap: var(--esp-8); margin: 0 0 var(--esp-16); }
    .campo > label, .campo > legend { margin: 0; padding: 0; font-size: var(--t-etiqueta); }
    .campo > p { margin: 0; }
    fieldset.campo { border: 0; padding: 0; min-width: 0; }
    .campo-item { display: flex; gap: var(--esp-8); }
    .campo-item > input { flex: 1; min-width: 0; }
    .campo-agregar { align-self: flex-start; }
    .formulario-avisos > p { margin: 0 0 var(--esp-16); }
    .formulario-pie { display: flex; justify-content: flex-end; }
  `,
  template: `
    <form [formGroup]="grupo()" (ngSubmit)="intentarEnviar()" novalidate>
      <div class="formulario-avisos">
        @if (errorGeneral(); as mensaje) { <p class="aviso error" role="alert">{{ mensaje }}</p> }
        @if (confirmacion(); as mensaje) { <p class="aviso ok" role="status">{{ mensaje }}</p> }
      </div>

      @for (c of campos(); track c.nombre) {
        @if (c.multivaluado) {
          <fieldset class="campo">
            <legend>{{ c.etiqueta }}</legend>
            @for (item of lista(c).controls; track item; let i = $index) {
              <div class="campo-item">
                <input [id]="id(c) + '-' + i" [type]="c.tipo ?? 'text'" [formControl]="item"
                       [attr.aria-label]="c.etiqueta + ' ' + (i + 1)"
                       [attr.aria-invalid]="error(c) ? 'true' : null" [attr.aria-describedby]="descriptores(c)" />
                <button type="button" class="chico" (click)="lista(c).removeAt(i)"
                        [attr.aria-label]="'Quitar ' + c.etiqueta + ' ' + (i + 1)">Quitar</button>
              </div>
            }
            <button type="button" class="chico campo-agregar" (click)="lista(c).push(nuevoItem(c))"
                    [attr.aria-label]="'Agregar a ' + c.etiqueta">Agregar</button>
            @if (c.ayuda) { <p class="ayuda" [id]="id(c) + '-ayuda'">{{ c.ayuda }}</p> }
            @if (error(c); as mensaje) { <p class="aviso error" role="alert" [id]="id(c) + '-error'">{{ mensaje }}</p> }
          </fieldset>
        } @else {
          <div class="campo">
            <label [for]="id(c)">{{ c.etiqueta }}</label>
            <input [id]="id(c)" [type]="c.tipo ?? 'text'" [formControlName]="c.nombre"
                   [attr.aria-required]="c.obligatorio ? 'true' : null"
                   [attr.aria-invalid]="error(c) ? 'true' : null" [attr.aria-describedby]="descriptores(c)" />
            @if (c.ayuda) { <p class="ayuda" [id]="id(c) + '-ayuda'">{{ c.ayuda }}</p> }
            @if (error(c); as mensaje) { <p class="aviso error" role="alert" [id]="id(c) + '-error'">{{ mensaje }}</p> }
          </div>
        }
      }

      <div class="formulario-pie">
        <button type="submit" class="primario" [cargando]="enviando()" [textoCarga]="textoEnviando()">{{ textoEnviar() }}</button>
      </div>
    </form>
  `,
})
export class Formulario {
  readonly campos = input.required<CampoFormulario[]>();
  readonly valores = input<ValoresFormulario>({});
  readonly enviando = input(false);
  readonly textoEnviar = input('Guardar');
  readonly textoEnviando = input('Guardando…');
  readonly erroresPorCampo = input<Record<string, string>>({});
  readonly errorGeneral = input<string | null>(null);
  readonly confirmacion = input<string | null>(null);
  readonly enviar = output<ValoresFormulario>();

  private readonly prefijo = `formulario-${++siguienteId}`;

  /** Se rearma cuando cambian los campos o los valores iniciales (modificar otra entidad). */
  protected readonly grupo = computed(() => {
    const valores = this.valores();
    const controles: Record<string, AbstractControl> = {};
    for (const c of this.campos()) {
      const validadores = [...(c.obligatorio ? [obligatorio] : [])];
      if (c.multivaluado) {
        const items = Array.isArray(valores[c.nombre]) ? (valores[c.nombre] as unknown[]) : [];
        controles[c.nombre] = new FormArray(items.map(v => this.nuevoItem(c, v)), validadores);
      } else {
        if (c.patron) validadores.push(Validators.pattern(c.patron));
        const inicial = valores[c.nombre];
        controles[c.nombre] = new FormControl(inicial === null || inicial === undefined ? '' : String(inicial), validadores);
      }
    }
    return new FormGroup(controles);
  });

  protected id(c: CampoFormulario) {
    return `${this.prefijo}-${c.nombre}`;
  }

  protected lista(c: CampoFormulario) {
    return this.grupo().get(c.nombre) as Lista;
  }

  protected nuevoItem(c: CampoFormulario, valor: unknown = '') {
    return new FormControl<string | null>(String(valor ?? ''), c.patron ? [Validators.pattern(c.patron)] : []);
  }

  /** El error a mostrar: primero el del cliente (si ya corresponde mostrarlo), si no el del servidor. */
  protected error(c: CampoFormulario): string | null {
    const control = this.grupo().get(c.nombre)!;
    const items = c.multivaluado ? (control as Lista).controls : [control];
    // Intentar enviar marca todo como tocado: desde ahí se ven los errores de los campos sin tocar.
    const mostrar = control.touched || items.some(i => i.touched);
    if (mostrar && control.hasError('obligatorio')) return OBLIGATORIO;
    if (mostrar && items.some(i => i.hasError('pattern'))) return c.mensajeFormato ?? FORMATO;
    return this.erroresPorCampo()[c.nombre] ?? null;
  }

  protected descriptores(c: CampoFormulario): string | null {
    const ids = [c.ayuda ? `${this.id(c)}-ayuda` : null, this.error(c) ? `${this.id(c)}-error` : null].filter(Boolean);
    return ids.length ? ids.join(' ') : null;
  }

  protected intentarEnviar() {
    const grupo = this.grupo();
    grupo.markAllAsTouched();
    if (this.enviando() || grupo.invalid) return;
    this.enviar.emit(this.valoresDe(grupo));
  }

  /** Los campos numéricos salen como número (o null si quedaron vacíos), no como el texto del input. */
  private valoresDe(grupo: FormGroup): ValoresFormulario {
    const crudos = grupo.getRawValue() as ValoresFormulario;
    for (const c of this.campos())
      if (c.tipo === 'number' && !c.multivaluado) {
        const texto = String(crudos[c.nombre] ?? '').trim();
        crudos[c.nombre] = texto === '' ? null : Number(texto);
      }
    return crudos;
  }
}
