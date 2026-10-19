import { Component, inject, signal } from '@angular/core';
import { Articulos } from './pantallas/articulos/articulos';
import { FacturaCompra } from './pantallas/factura-compra/factura-compra';
import { Login } from './pantallas/login/login';
import { SesionActual } from './nucleo/sesion';

@Component({
  selector: 'app-root',
  imports: [Login, Articulos, FacturaCompra],
  template: `
    @if (sesion.sesion(); as s) {
      <header>
        <nav>
          <strong>La Central</strong>
          <button class="pestana" [class.activa]="vista() === 'articulos'" (click)="vista.set('articulos')">Artículos</button>
          <button class="pestana" [class.activa]="vista() === 'compra'" (click)="vista.set('compra')">Factura de compra</button>
        </nav>
        <span>{{ s.nombreUsuario }} ({{ s.rol }}) <button class="secundario" (click)="sesion.cerrar()">Salir</button></span>
      </header>
      <main>
        @if (vista() === 'compra') { <app-factura-compra /> } @else { <app-articulos /> }
      </main>
    } @else {
      <main><app-login /></main>
    }
  `,
})
export class App {
  protected readonly sesion = inject(SesionActual);
  protected readonly vista = signal<'articulos' | 'compra'>('articulos');
}
