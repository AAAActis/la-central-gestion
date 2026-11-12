import { Component, ElementRef, effect, input, model, viewChild } from '@angular/core';

let siguienteId = 0;

/**
 * Modal propio (SISTEMA-VISUAL.md §8): `<app-modal titulo="…" [(abierto)]="…">`. El contenido
 * va al cuerpo; lo marcado con el atributo `pie`, al pie. Escape cierra, el foco queda adentro
 * (showModal vuelve inerte el resto de la página) y, al cerrar, el navegador lo devuelve al
 * elemento que lo tenía al abrir.
 */
@Component({
  selector: 'app-modal',
  template: `
    <dialog #dialogo class="modal" [attr.aria-labelledby]="idTitulo"
            (keydown.escape)="$event.preventDefault(); abierto.set(false)"
            (cancel)="$event.preventDefault(); abierto.set(false)">
      <header class="modal-cabecera"><h2 [id]="idTitulo">{{ titulo() }}</h2></header>
      <div class="modal-cuerpo"><ng-content /></div>
      <footer class="modal-pie"><ng-content select="[pie]" /></footer>
    </dialog>
  `,
})
export class Modal {
  readonly titulo = input.required<string>();
  readonly abierto = model(false);
  protected readonly idTitulo = `modal-titulo-${++siguienteId}`;
  private readonly dialogo = viewChild.required<ElementRef<HTMLDialogElement>>('dialogo');

  constructor() {
    effect(() => {
      const dialogo = this.dialogo().nativeElement;
      if (this.abierto() && !dialogo.open) dialogo.showModal();
      else if (!this.abierto() && dialogo.open) dialogo.close();
    });
  }
}
