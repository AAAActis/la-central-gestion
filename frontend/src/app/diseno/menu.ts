import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { MODULOS, modulosPara } from '../nucleo/acceso';
import { SesionActual } from '../nucleo/sesion';

/** El menú de módulos: solo los que el rol de la sesión tiene habilitados. */
@Component({
  selector: 'app-menu',
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav aria-label="Módulos">
      <strong class="marca">La Central</strong>
      @for (m of modulos(); track m.ruta) {
        <a [routerLink]="'/' + m.ruta" routerLinkActive="activo" ariaCurrentWhenActive="page">{{ m.titulo }}</a>
      }
    </nav>
  `,
})
export class Menu {
  private readonly todos = inject(MODULOS);
  private readonly sesion = inject(SesionActual);
  protected readonly modulos = computed(() => modulosPara(this.sesion.sesion()?.rol ?? '', this.todos));
}
