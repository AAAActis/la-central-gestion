import { Component, effect, input, model, signal } from '@angular/core';

/** Cuánto queda a la vista un aviso que se va solo: el token --dur-aviso de styles.css. */
export function duracionAvisoMs(): number {
  const valor = getComputedStyle(document.documentElement).getPropertyValue('--dur-aviso').trim();
  return parseFloat(valor) * (valor.endsWith('ms') ? 1 : 1000);
}

/**
 * Una ocurrencia de un aviso. Es un objeto, no un string: dos errores con el mismo texto son dos
 * avisos distintos, y un signal de string no cambiaría al repetirse el texto.
 */
export interface Aviso {
  texto: string;
}

/**
 * Aviso flotante (SISTEMA-VISUAL.md §8): `<app-aviso-flotante [(mensaje)]="…" tipo="ok" />`,
 * con `mensaje.set({ texto })` por cada aviso.
 * Aparece con el mensaje y se puede cerrar. Éxito, información y alerta se van solos a los
 * --dur-aviso; un error se anuncia como alerta y queda hasta que el usuario lo cierra.
 * Un aviso nuevo reemplaza al anterior (hay un solo lugar: no se apilan) y reinicia el tiempo;
 * aunque repita el texto, se vuelve a anunciar.
 */
@Component({
  selector: 'app-aviso-flotante',
  template: `
    @if (mensaje(); as aviso) {
      <div class="aviso aviso-flotante" [class]="tipo()" [attr.role]="tipo() === 'error' ? 'alert' : 'status'">
        <span>{{ texto() }}</span>
        <button type="button" aria-label="Cerrar aviso" (click)="mensaje.set(null)">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"
               stroke-linejoin="round" aria-hidden="true" focusable="false"><path d="M6 6l12 12M18 6L6 18" /></svg>
        </button>
      </div>
    }
  `,
})
export class AvisoFlotante {
  readonly mensaje = model<Aviso | null>(null);
  readonly tipo = input<'info' | 'ok' | 'alerta' | 'error'>('info');
  protected readonly texto = signal('');

  constructor() {
    // Cada aviso vacía la región y la vuelve a escribir en el ciclo siguiente: un texto igual al
    // anterior se vuelve a anunciar. Sin contador invisible, que el lector de pantalla leería.
    effect(alLimpiar => {
      const aviso = this.mensaje();
      this.texto.set('');
      if (!aviso) return;
      const escribir = setTimeout(() => this.texto.set(aviso.texto));
      alLimpiar(() => clearTimeout(escribir));
    });
    effect(alLimpiar => {
      // Un error no se va solo: el operador puede estar mirando a un cliente y no a la pantalla.
      if (!this.mensaje() || this.tipo() === 'error') return;
      const temporizador = setTimeout(() => this.mensaje.set(null), duracionAvisoMs());
      alLimpiar(() => clearTimeout(temporizador));
    });
  }
}
