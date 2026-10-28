import { Component, effect, input, model } from '@angular/core';

/** Cuánto queda a la vista un aviso que se va solo: el token --dur-aviso de styles.css. */
export function duracionAvisoMs(): number {
  const valor = getComputedStyle(document.documentElement).getPropertyValue('--dur-aviso').trim();
  return parseFloat(valor) * (valor.endsWith('ms') ? 1 : 1000);
}

/**
 * Aviso flotante (SISTEMA-VISUAL.md §8): `<app-aviso-flotante [(mensaje)]="…" tipo="ok" />`.
 * Aparece con el mensaje y se puede cerrar. Éxito, información y alerta se van solos a los
 * --dur-aviso; un error se anuncia como alerta y queda hasta que el usuario lo cierra.
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
      // Un error no se va solo: el operador puede estar mirando a un cliente y no a la pantalla.
      if (!this.mensaje() || this.tipo() === 'error') return;
      const temporizador = setTimeout(() => this.mensaje.set(null), duracionAvisoMs());
      alLimpiar(() => clearTimeout(temporizador));
    });
  }
}
