import { Component, computed, input, model, output } from '@angular/core';
import { CampoFormulario, Formulario, ValoresFormulario } from './formulario/formulario';
import { Modal } from './modal';

/**
 * Confirmación con motivo (HU-UI-06): el modal con el formulario de HU-UI-05 adentro.
 * El motivo es obligatorio siempre. La clave a reescribir es opcional: exigirla es una regla de
 * negocio (proforma 044 v1.1, pendiente del equipo), así que la decide la pantalla.
 * Confirmar es `peligro`: el punto sin retorno (§7). Escape y Cancelar cierran sin emitir nada.
 */
@Component({
  selector: 'app-confirmacion-motivo',
  imports: [Modal, Formulario],
  template: `
    <app-modal [titulo]="titulo()" [(abierto)]="abierto">
      <app-formulario [campos]="campos()" [valores]="valoresIniciales()" varianteEnviar="peligro"
        [textoEnviar]="textoConfirmar()" [enviando]="enviando()" [errorGeneral]="errorGeneral()"
        (enviar)="alConfirmar($event)">
        <button type="button" acciones (click)="abierto.set(false)">Cancelar</button>
      </app-formulario>
    </app-modal>
  `,
})
export class ConfirmacionMotivo {
  readonly titulo = input.required<string>();
  readonly abierto = model(false);
  readonly maximoMotivo = input.required<number>();
  readonly clave = input<string | null>(null);
  readonly textoConfirmar = input('Confirmar');
  readonly enviando = input(false);
  readonly errorGeneral = input<string | null>(null);
  readonly confirmar = output<string>();

  /** El motivo va primero: el modal enfoca el primer campo al abrirse. */
  protected readonly campos = computed<CampoFormulario[]>(() => {
    const clave = this.clave();
    return [
      { nombre: 'motivo', etiqueta: 'Motivo', obligatorio: true, maximo: this.maximoMotivo(),
        ayuda: `Hasta ${this.maximoMotivo()} caracteres.` },
      ...(clave ? [{ nombre: 'clave', etiqueta: `Para confirmar, escribí ${clave}`, obligatorio: true, coincideCon: clave }] : []),
    ];
  });

  /** Un objeto nuevo en cada apertura: el formulario se rearma vacío y no arrastra el motivo anterior. */
  protected readonly valoresIniciales = computed<ValoresFormulario>(() => (this.abierto(), {}));

  protected alConfirmar(valores: ValoresFormulario) {
    this.confirmar.emit(String(valores['motivo']).trim());
  }
}
