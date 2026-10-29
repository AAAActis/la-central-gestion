import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, afterNextRender, inject, signal } from '@angular/core';
import { Aviso, AvisoFlotante } from '../../compartido/aviso-flotante';
import { CampoFormulario, Formulario, ValoresFormulario } from '../../compartido/formulario/formulario';
import { Marca } from '../../diseno/marca';
import { ApiLaCentral, mensajeDeError } from '../../nucleo/api';
import { SesionActual } from '../../nucleo/sesion';

/** Un ingreso fallido no dice cuál de los dos datos falló: no se confirma que un usuario existe. */
const CREDENCIALES = 'Usuario o contraseña incorrectos.';

const CAMPOS: CampoFormulario[] = [
  { nombre: 'nombreUsuario', etiqueta: 'Usuario', obligatorio: true, autocompletar: 'username' },
  { nombre: 'contrasena', etiqueta: 'Contraseña', tipo: 'password', obligatorio: true, autocompletar: 'current-password' },
];

/**
 * Pantalla de acceso: tarjeta partida (formulario y panel de marca) sobre el fondo hexagonal (§5).
 * Los errores no van al formulario sino al aviso flotante: el acceso es donde el usuario reintenta.
 */
@Component({
  selector: 'app-login',
  imports: [AvisoFlotante, Formulario, Marca],
  styles: `
    :host {
      position: fixed; inset: 0; overflow-y: auto;
      display: grid; place-items: center; padding: var(--esp-20) var(--esp-16);
    }
    .acceso-tarjeta {
      display: grid; grid-template-columns: 1fr 1fr; width: min(var(--ancho-form), 100%);
      margin: 0; overflow: hidden; color: var(--tinta); box-shadow: var(--sombra);
    }
    h1 { margin: 0 0 var(--esp-20); }
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
      <div class="tarjeta-cuerpo">
        <h1>Acceso al sistema</h1>
        <app-formulario [campos]="campos" [enviando]="enviando()" textoEnviar="Ingresar" textoEnviando="Ingresando…"
                        (enviar)="entrar($event)" />
      </div>
      <div class="acceso-marca"><app-marca variante="completa" /></div>
    </div>
    <app-aviso-flotante [(mensaje)]="aviso" tipo="error" />
  `,
})
export class Login {
  private readonly api = inject(ApiLaCentral);
  private readonly sesion = inject(SesionActual);

  private readonly anfitrion = inject<ElementRef<HTMLElement>>(ElementRef);

  protected readonly campos = CAMPOS;
  readonly enviando = signal(false);
  /** Un objeto por intento: dos errores iguales seguidos se vuelven a anunciar (aviso-flotante.ts). */
  protected readonly aviso = signal<Aviso | null>(null);

  constructor() {
    const motivo = this.sesion.avisoAlCerrar();
    if (motivo) this.aviso.set({ texto: motivo });
    // Al salir o vencer la sesión, el botón que tenía el foco desaparece con el marco.
    afterNextRender(() => this.anfitrion.nativeElement.querySelector('input')?.focus());
  }

  entrar(valores: ValoresFormulario) {
    this.enviando.set(true);
    this.api.login(String(valores['nombreUsuario']), String(valores['contrasena'])).subscribe({
      next: s => this.sesion.iniciar(s),
      error: e => {
        this.aviso.set({ texto: e instanceof HttpErrorResponse && e.status === 401 ? CREDENCIALES : mensajeDeError(e) });
        this.enviando.set(false);
      },
    });
  }
}
