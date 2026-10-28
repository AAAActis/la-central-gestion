import { Component, input, output } from '@angular/core';

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

/**
 * Formulario reutilizable (HU-UI-05). Anatomía del campo: SISTEMA-VISUAL.md §8; botón: §7.
 *
 * Los errores del servidor entran por dos vías separadas, sin traducir texto: `erroresPorCampo`
 * (campo → mensaje) y `errorGeneral`. Hoy el mapa sale de los `errors` por propiedad de un 400
 * de validación; cuando el backend diga el campo de un rechazo de negocio, cambia lo que la
 * pantalla pasa, no este componente.
 */
@Component({
  selector: 'app-formulario',
  template: `<form></form>`,
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
}
