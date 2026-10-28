import { Component, input, model, output } from '@angular/core';

/**
 * Confirmación con motivo (HU-UI-06): un modal con el formulario de HU-UI-05.
 * El motivo es obligatorio siempre; la clave a reescribir es opcional y la decide la pantalla.
 */
@Component({
  selector: 'app-confirmacion-motivo',
  template: ``,
})
export class ConfirmacionMotivo {
  readonly titulo = input.required<string>();
  readonly abierto = model(false);
  readonly maximoMotivo = input.required<number>();
  readonly clave = input<string | null>(null);
  readonly textoConfirmar = input('Confirmar');
  readonly enviando = input(false);
  readonly errorGeneral = input<string | null>(null);
  readonly confirmar = output<string>();
}
