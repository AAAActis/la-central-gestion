import { Component, inject } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Router, RouterOutlet } from '@angular/router';
import { skip } from 'rxjs';
import { Marca } from './diseno/marca';
import { Menu } from './diseno/menu';
import { SesionActual } from './nucleo/sesion';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Marca, Menu],
  // El pie queda abajo aunque la pantalla tenga poco contenido.
  styles: `
    :host { display: flex; flex-direction: column; min-height: 100vh; }
    main { flex: 1 0 auto; width: 100%; box-sizing: border-box; }
    header { border-bottom: var(--grosor-foco) solid var(--rojo); }
    .encabezado-izquierda { display: flex; flex-wrap: wrap; align-items: stretch; gap: 0 var(--esp-24); min-width: 0; }
    app-marca { align-self: center; padding: var(--esp-8) 0; }
    .usuario { display: flex; align-items: center; gap: var(--esp-12); margin-left: auto; }
    footer { padding: var(--esp-14) var(--esp-24); background: var(--grafito-2); color: var(--linea-2); font-size: var(--t-ayuda); }
  `,
  template: `
    @if (sesion.sesion(); as s) {
      <header>
        <div class="encabezado-izquierda">
          <app-marca variante="compacta" />
          <app-menu />
        </div>
        <span class="usuario">{{ s.nombreUsuario }} ({{ s.rol }}) <button class="oscuro" (click)="sesion.cerrar()">Salir</button></span>
      </header>
    }
    <main><router-outlet /></main>
    @if (sesion.sesion()) {
      <footer>La Central Repuestos</footer>
    }
  `,
})
export class App {
  protected readonly sesion = inject(SesionActual);
  private readonly router = inject(Router);

  constructor() {
    // Entrar o salir cambia qué rutas existen: se vuelve a resolver la URL actual, sin recargar.
    toObservable(this.sesion.sesion).pipe(skip(1)).subscribe(() =>
      this.router.navigateByUrl(this.router.url, { onSameUrlNavigation: 'reload' }));
  }
}
