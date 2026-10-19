import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiLaCentral, mensajeDeError } from '../../nucleo/api';
import { SesionActual } from '../../nucleo/sesion';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  template: `
    <form class="tarjeta login" (ngSubmit)="entrar()">
      <h1>La Central</h1>
      <label>Usuario
        <input name="usuario" [(ngModel)]="usuario" autocomplete="username" required />
      </label>
      <label>Contraseña
        <input name="contrasena" type="password" [(ngModel)]="contrasena" autocomplete="current-password" required />
      </label>
      @if (error()) { <p class="error">{{ error() }}</p> }
      <button type="submit" [disabled]="enviando()">Entrar</button>
    </form>
  `,
})
export class Login {
  private readonly api = inject(ApiLaCentral);
  private readonly sesion = inject(SesionActual);

  usuario = '';
  contrasena = '';
  readonly error = signal<string | null>(null);
  readonly enviando = signal(false);

  entrar() {
    this.error.set(null);
    this.enviando.set(true);
    this.api.login(this.usuario, this.contrasena).subscribe({
      next: s => this.sesion.iniciar(s),
      error: e => {
        this.error.set(mensajeDeError(e));
        this.enviando.set(false);
      },
    });
  }
}
