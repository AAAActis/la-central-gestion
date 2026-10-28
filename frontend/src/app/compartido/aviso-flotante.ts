import { Component, effect, input, model } from '@angular/core';

/** Cuánto queda a la vista. shortcut: la §8 no fija el tiempo; confirmarlo con Javi. */
export const DURACION_AVISO_MS = 5000;

/**
 * Aviso flotante (SISTEMA-VISUAL.md §8): `<app-aviso-flotante [(mensaje)]="…" tipo="ok" />`.
 * Aparece con el mensaje, se va solo y se puede cerrar. Un error se anuncia como alerta.
 */
@Component({
  selector: 'app-aviso-flotante',
  template: `
    @if (mensaje(); as texto) {
      <div class="aviso aviso-flotante" [class]="tipo()" [attr.role]="tipo() === 'error' ? 'alert' : 'status'">
        <span>{{ texto }}</span>
        <button type="button" aria-label="Cerrar aviso" (click)="mensaje.set(null)">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"
               stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M6 6l12 12M18 6L6 18" /></svg>
        </button>
      </div>
    }
  `,
})
export class AvisoFlotante {
  readonly mensaje = model<string | null>(null);
  readonly tipo = input<'info' | 'ok' | 'alerta' | 'error'>('info');

  constructor() {
    effect(alLimpiar => {
      if (!this.mensaje()) return;
      const temporizador = setTimeout(() => this.mensaje.set(null), DURACION_AVISO_MS);
      alLimpiar(() => clearTimeout(temporizador));
    });
  }
}
