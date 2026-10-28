import { Component, input } from '@angular/core';

/**
 * La marca de La Central. PROVISORIA: el logo se va a reemplazar, y por eso vive solo acá
 * (SISTEMA-VISUAL.md §6). Completa en la pantalla de acceso, compacta en el encabezado.
 * La «C» de CENTRAL es un arco en --rojo-marca, nunca en --rojo.
 */
@Component({
  selector: 'app-marca',
  template: `
    <span class="logo" [class.completa]="variante() === 'completa'" [class.compacta]="variante() === 'compacta'"
          role="img" aria-label="La Central Repuestos">
      <span class="logo-repuestos" aria-hidden="true">REPUESTOS</span>
      <span class="logo-nombre" aria-hidden="true">LA&nbsp;<svg class="logo-c" viewBox="0 0 24 24" fill="none"
          stroke="currentColor" stroke-width="4.5" focusable="false"><path d="M18.5 6.2A8.5 8.5 0 1 0 18.5 17.8" /></svg>ENTRAL</span>
    </span>
  `,
})
export class Marca {
  readonly variante = input<'completa' | 'compacta'>('compacta');
}
