import { Component, inject } from '@angular/core';
import { Articulos } from './articulos';
import { Login } from './login';
import { SesionActual } from './sesion';

@Component({
  selector: 'app-root',
  imports: [Login, Articulos],
  template: `
    @if (sesion.sesion(); as s) {
      <header>
        <strong>La Central · Artículos</strong>
        <span>{{ s.nombreUsuario }} ({{ s.rol }}) <button class="secundario" (click)="sesion.cerrar()">Salir</button></span>
      </header>
      <main><app-articulos /></main>
    } @else {
      <main><app-login /></main>
    }
  `,
})
export class App {
  protected readonly sesion = inject(SesionActual);
}
