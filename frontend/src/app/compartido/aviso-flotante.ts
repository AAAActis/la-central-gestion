import { Component, input, model } from '@angular/core';

/** Cuánto queda a la vista. shortcut: la §8 no fija el tiempo; confirmarlo con Javi. */
export const DURACION_AVISO_MS = 0;

/**
 * Aviso flotante (SISTEMA-VISUAL.md §8): `<app-aviso-flotante [(mensaje)]="…" tipo="ok" />`.
 * Aparece con el mensaje, se va solo y se puede cerrar.
 */
@Component({
  selector: 'app-aviso-flotante',
  template: ``,
})
export class AvisoFlotante {
  readonly mensaje = model<string | null>(null);
  readonly tipo = input<'info' | 'ok' | 'alerta' | 'error'>('info');
}
