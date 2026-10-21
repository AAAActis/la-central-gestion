import { Component, inject } from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { Router, RouterOutlet } from '@angular/router';
import { skip } from 'rxjs';
import { Menu } from './diseno/menu';
import { SesionActual } from './nucleo/sesion';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Menu],
  template: `
    @if (sesion.sesion(); as s) {
      <header>
        <app-menu />
        <span class="usuario">{{ s.nombreUsuario }} ({{ s.rol }}) <button class="secundario" (click)="sesion.cerrar()">Salir</button></span>
      </header>
    }
    <main><router-outlet /></main>
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
