import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Modal } from './modal';

@Component({
  imports: [Modal],
  template: `
    <button id="abrir" (click)="abierto.set(true)">Dar de baja</button>
    <button id="fuera">Otra cosa</button>
    <app-modal titulo="Confirmar baja" [(abierto)]="abierto">
      <p>¿Seguro?</p>
      <button id="cancelar" pie (click)="abierto.set(false)">Cancelar</button>
    </app-modal>
  `,
})
class Anfitrion {
  readonly abierto = signal(false);
}

describe('Modal', () => {
  let fixture: ComponentFixture<Anfitrion>;
  const raiz = () => fixture.nativeElement as HTMLElement;
  const dialogo = () => raiz().querySelector('dialog')!;
  const por = (id: string) => raiz().querySelector<HTMLButtonElement>('#' + id)!;
  const abrirDesdeElBoton = () => {
    por('abrir').focus();
    por('abrir').click();
    fixture.detectChanges();
  };

  beforeEach(() => {
    fixture = TestBed.createComponent(Anfitrion);
    document.body.appendChild(fixture.nativeElement);
    fixture.detectChanges();
  });

  afterEach(() => (fixture.nativeElement as HTMLElement).remove());

  it('al abrirse es modal y el foco entra en él', () => {
    abrirDesdeElBoton();

    expect(dialogo().open).toBeTrue();
    expect(dialogo().matches(':modal')).toBeTrue();
    expect(dialogo().contains(document.activeElement)).toBeTrue();
  });

  it('Escape lo cierra', () => {
    abrirDesdeElBoton();

    dialogo().dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();

    expect(fixture.componentInstance.abierto()).toBeFalse();
    expect(dialogo().open).toBeFalse();
  });

  it('el foco queda atrapado: lo de afuera no se puede enfocar', () => {
    abrirDesdeElBoton();

    por('fuera').focus();

    expect(document.activeElement).not.toBe(por('fuera'));
    expect(dialogo().contains(document.activeElement)).toBeTrue();
  });

  it('al cerrarse, el foco vuelve al botón que lo abrió', () => {
    abrirDesdeElBoton();
    expect(dialogo().contains(document.activeElement)).withContext('abierto, el foco está adentro').toBeTrue();
    por('cancelar').click();
    fixture.detectChanges();

    expect(dialogo().open).toBeFalse();
    expect(document.activeElement).toBe(por('abrir'));
  });
});
