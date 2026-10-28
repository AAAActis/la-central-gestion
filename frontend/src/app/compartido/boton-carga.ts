import { Component, input } from '@angular/core';

/**
 * Botón en carga (SISTEMA-VISUAL.md §7.5): `<button [cargando]="enviando()">Guardar</button>`.
 * En carga: deshabilitado, aria-busy, indicador y texto de carga, mismo ancho, sin doble envío.
 * Si el botón también tiene `[disabled]`, se respeta.
 */
@Component({
  selector: 'button[cargando]',
  template: `<ng-content />`,
})
export class BotonCarga {
  readonly cargando = input(false);
  readonly textoCarga = input('Guardando…');
  readonly disabled = input(false);
}
