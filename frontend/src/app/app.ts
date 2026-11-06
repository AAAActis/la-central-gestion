import { Component, computed, inject } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map, skip } from 'rxjs';
import { Marca } from './diseno/marca';
import { Menu } from './diseno/menu';
import { MODULOS } from './nucleo/acceso';
import { SesionActual } from './nucleo/sesion';

/** El rol como se lee en el encabezado (referencias 02 y 09); uno desconocido se muestra tal cual. */
const ROLES: Record<string, string> = { ADMINISTRADOR: 'Administrador', OPERADOR: 'Operador' };

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Marca, Menu],
  // El pie queda abajo aunque la pantalla tenga poco contenido.
  styles: `
    :host { display: flex; flex-direction: column; min-height: 100vh; }
    main { flex: 1 0 auto; width: 100%; box-sizing: border-box; }
    header { border-bottom: var(--filete) solid var(--rojo); }
    .encabezado-izquierda { display: flex; flex-wrap: wrap; align-items: stretch; gap: 0 var(--esp-24); min-width: 0; }
    app-marca { align-self: center; padding: var(--esp-8) 0; }
    .usuario { display: flex; align-items: center; gap: var(--esp-16); margin-left: auto; }
    .usuario-datos { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
    .usuario-nombre { color: var(--blanco); font-weight: 500; overflow-wrap: anywhere; }
    .usuario-rol { font-size: var(--t-ayuda); }
    .pantalla-encabezado { margin: var(--esp-8) 0 var(--esp-20); }
    .pantalla-encabezado h1 { margin: 0 0 var(--esp-8); }
    .bajada { margin: 0; color: var(--texto-2); }
    footer {
      width: 100%; max-width: var(--ancho-max); margin: 0 auto; box-sizing: border-box;
      padding: var(--esp-8) var(--esp-16) var(--esp-20); color: var(--texto-2); font-size: var(--t-ayuda);
    }
  `,
  template: `
    @if (sesion.sesion(); as s) {
      <header>
        <div class="encabezado-izquierda">
          <app-marca variante="compacta" />
          <app-menu />
        </div>
        <div class="usuario">
          <span class="usuario-datos">
            <span class="usuario-nombre">{{ s.nombreUsuario }}</span>
            <span class="usuario-rol">{{ rolLegible(s.rol) }}</span>
          </span>
          <button class="oscuro" (click)="sesion.cerrar()">Salir</button>
        </div>
      </header>
    }
    <main>
      @if (sesion.sesion() && moduloActual(); as m) {
        <div class="pantalla-encabezado">
          <h1>{{ m.titulo }}</h1>
          @if (m.bajada) { <p class="bajada">{{ m.bajada }}</p> }
        </div>
      }
      <router-outlet />
    </main>
    @if (sesion.sesion()) {
      <footer>La Central Repuestos</footer>
    }
  `,
})
export class App {
  protected readonly sesion = inject(SesionActual);
  private readonly router = inject(Router);
  private readonly modulos = inject(MODULOS);

  /** El título y la bajada salen del módulo de la URL: una sola fuente, la misma que el menú. */
  private readonly url = toSignal(
    this.router.events.pipe(filter(e => e instanceof NavigationEnd), map(() => this.router.url)),
    { initialValue: this.router.url });
  protected readonly moduloActual = computed(() => {
    const ruta = this.url().split(/[?#]/)[0].split('/')[1];
    return this.modulos.find(m => m.ruta === ruta) ?? null;
  });

  protected rolLegible(rol: string) {
    return ROLES[rol] ?? rol;
  }

  constructor() {
    // Entrar o salir cambia qué rutas existen: se vuelve a resolver la URL actual, sin recargar.
    toObservable(this.sesion.sesion).pipe(skip(1)).subscribe(() =>
      this.router.navigateByUrl(this.router.url, { onSameUrlNavigation: 'reload' }));
  }
}
