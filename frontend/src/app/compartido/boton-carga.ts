import { Component, ElementRef, inject, input } from '@angular/core';

/**
 * Botón en carga (SISTEMA-VISUAL.md §7.5): `<button [cargando]="enviando()">Guardar</button>`.
 * En carga: deshabilitado, aria-busy, indicador y texto de carga, mismo ancho, sin doble envío.
 * Si el botón también tiene `[disabled]`, se respeta.
 */
@Component({
  selector: 'button[cargando]',
  host: {
    '[disabled]': 'cargando() || disabled()',
    '[attr.aria-busy]': 'cargando() ? "true" : null',
  },
  // Las dos etiquetas comparten celda (.boton-etiquetas): el ancho no cambia al entrar en carga.
  template: `
    <span class="boton-etiquetas">
      <span [class.oculta]="cargando()"><ng-content /></span>
      <span [class.oculta]="!cargando()"><span class="girando" aria-hidden="true"></span>{{ textoCarga() }}</span>
    </span>
  `,
})
export class BotonCarga {
  readonly cargando = input(false);
  readonly textoCarga = input('Guardando…');
  readonly disabled = input(false);

  constructor() {
    // disabled no frena un click despachado por código: en captura se corta antes que el (click) de la pantalla.
    inject<ElementRef<HTMLButtonElement>>(ElementRef).nativeElement.addEventListener('click', evento => {
      if (!this.cargando()) return;
      evento.preventDefault();
      evento.stopImmediatePropagation();
    }, { capture: true });
  }
}
