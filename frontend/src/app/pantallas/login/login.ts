import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Aviso, AvisoFlotante } from '../../compartido/aviso-flotante';
import { BotonCarga } from '../../compartido/boton-carga';
import { Marca } from '../../diseno/marca';
import { ApiLaCentral, mensajeDeError } from '../../nucleo/api';
import { SesionActual } from '../../nucleo/sesion';

/** Un ingreso fallido no dice cuál de los dos datos falló: no se confirma que un usuario existe. */
const CREDENCIALES = 'Usuario o contraseña incorrectos.';

/**
 * Pantalla de acceso: tarjeta partida (formulario y panel de marca) sobre el fondo hexagonal (§5).
 * shortcut: los campos son propios y no de compartido/formulario/, porque CampoFormulario no admite
 * tipo password ni autocomplete; se migra cuando el formulario los tenga (ESTADO.md, HU-UI-03).
 */
@Component({
  selector: 'app-login',
  imports: [FormsModule, AvisoFlotante, BotonCarga, Marca],
  styles: `
    :host {
      position: fixed; inset: 0; overflow-y: auto;
      display: grid; place-items: center; padding: var(--esp-20) var(--esp-16);
    }
    .acceso-tarjeta {
      display: grid; grid-template-columns: 1fr 1fr; width: min(var(--ancho-form), 100%);
      margin: 0; overflow: hidden; color: var(--tinta); box-shadow: var(--sombra);
    }
    form { display: flex; flex-direction: column; }
    h1 { margin: 0 0 var(--esp-20); }
    label { margin-bottom: var(--esp-16); }
    button.primario { align-self: flex-start; margin-top: var(--esp-8); }
    .acceso-marca {
      display: grid; place-items: center; padding: var(--esp-24);
      background: var(--grafito); border-left: var(--grosor-foco) solid var(--rojo);
    }
    @media (max-width: 760px) {
      .acceso-tarjeta { grid-template-columns: 1fr; }
      .acceso-marca { border-left: 0; border-top: var(--grosor-foco) solid var(--rojo); }
    }
  `,
  host: { class: 'fondo-hex' },
  template: `
    <div class="tarjeta acceso-tarjeta">
      <form class="tarjeta-cuerpo" (ngSubmit)="entrar()">
        <h1>Acceso al sistema</h1>
        <label>Usuario
          <input #usuario name="usuario" [(ngModel)]="usuario" autocomplete="username" required />
        </label>
        <label>Contraseña
          <input name="contrasena" type="password" [(ngModel)]="contrasena" autocomplete="current-password" required />
        </label>
        <button type="submit" class="primario" [cargando]="enviando()" textoCarga="Ingresando…">Ingresar</button>
      </form>
      <div class="acceso-marca"><app-marca variante="completa" /></div>
    </div>
    <app-aviso-flotante [(mensaje)]="aviso" tipo="error" />
  `,
})
export class Login {
  private readonly api = inject(ApiLaCentral);
  private readonly sesion = inject(SesionActual);

  usuario = '';
  contrasena = '';
  readonly enviando = signal(false);
  /** Un objeto por intento: dos errores iguales seguidos se vuelven a anunciar (aviso-flotante.ts). */
  protected readonly aviso = signal<Aviso | null>(null);

  constructor() {
    const motivo = this.sesion.avisoAlCerrar();
    if (motivo) this.aviso.set({ texto: motivo });
  }

  entrar() {
    this.enviando.set(true);
    this.api.login(this.usuario, this.contrasena).subscribe({
      next: s => this.sesion.iniciar(s),
      error: e => {
        this.aviso.set({ texto: e instanceof HttpErrorResponse && e.status === 401 ? CREDENCIALES : mensajeDeError(e) });
        this.enviando.set(false);
      },
    });
  }
}
