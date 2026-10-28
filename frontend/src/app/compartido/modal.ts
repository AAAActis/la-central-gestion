import { Component, input, model } from '@angular/core';

/**
 * Modal propio (SISTEMA-VISUAL.md §8): `<app-modal titulo="…" [(abierto)]="…">`. El contenido
 * va al cuerpo; lo marcado con el atributo `pie`, al pie. Escape cierra, el foco queda adentro
 * y vuelve al elemento que lo tenía al abrir.
 */
@Component({
  selector: 'app-modal',
  template: `
    <dialog class="modal">
      <header class="modal-cabecera"><h2>{{ titulo() }}</h2></header>
      <div class="modal-cuerpo"><ng-content /></div>
      <footer class="modal-pie"><ng-content select="[pie]" /></footer>
    </dialog>
  `,
})
export class Modal {
  readonly titulo = input.required<string>();
  readonly abierto = model(false);
}
