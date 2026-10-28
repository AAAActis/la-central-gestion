import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BotonCarga } from './boton-carga';

@Component({
  imports: [BotonCarga],
  template: `<button [cargando]="cargando()" [disabled]="deshabilitado()" (click)="envios = envios + 1">OK</button>`,
})
class Anfitrion {
  readonly cargando = signal(false);
  readonly deshabilitado = signal(false);
  envios = 0;
}

describe('BotonCarga', () => {
  let fixture: ComponentFixture<Anfitrion>;
  const boton = () => (fixture.nativeElement as HTMLElement).querySelector('button')!;
  const cargar = (valor: boolean) => {
    fixture.componentInstance.cargando.set(valor);
    fixture.detectChanges();
  };

  beforeEach(() => {
    fixture = TestBed.createComponent(Anfitrion);
    fixture.detectChanges();
  });

  it('en carga queda deshabilitado, con aria-busy y el texto de carga a la vista', () => {
    expect(boton().disabled).toBeFalse();
    expect(boton().getAttribute('aria-busy')).toBeNull();

    cargar(true);

    expect(boton().disabled).toBeTrue();
    expect(boton().getAttribute('aria-busy')).toBe('true');
    expect(boton().querySelector('.girando')).not.toBeNull();
    expect(boton().innerText.trim()).toBe('Guardando…');
  });

  it('en carga, un segundo clic no vuelve a enviar', () => {
    boton().click();
    cargar(true);
    boton().click();
    boton().dispatchEvent(new MouseEvent('click', { bubbles: true }));

    expect(fixture.componentInstance.envios).toBe(1);
  });

  it('al entrar en carga conserva el ancho, aunque el texto de carga sea más largo', () => {
    const antes = boton().getBoundingClientRect().width;
    cargar(true);

    expect(boton().innerText.trim()).toBe('Guardando…');
    expect(boton().getBoundingClientRect().width).toBe(antes);
  });

  it('al salir de la carga vuelve a su texto y se puede usar; un [disabled] propio se respeta', () => {
    cargar(true);
    cargar(false);
    expect(boton().disabled).toBeFalse();
    expect(boton().innerText.trim()).toBe('OK');

    fixture.componentInstance.deshabilitado.set(true);
    fixture.detectChanges();
    expect(boton().disabled).toBeTrue();
    expect(boton().getAttribute('aria-busy')).toBeNull();
  });
});
