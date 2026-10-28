import { Component, input, model } from '@angular/core';

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
