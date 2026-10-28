import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { AvisoFlotante, DURACION_AVISO_MS } from './aviso-flotante';

@Component({
  imports: [AvisoFlotante],
  template: `<app-aviso-flotante [(mensaje)]="mensaje" [tipo]="tipo()" />`,
})
class Anfitrion {
  readonly mensaje = signal<string | null>(null);
  readonly tipo = signal<'ok' | 'error'>('ok');
}

describe('AvisoFlotante', () => {
  let fixture: ComponentFixture<Anfitrion>;
  const aviso = () => (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.aviso-flotante');
  const mostrar = (texto: string) => {
    fixture.componentInstance.mensaje.set(texto);
    fixture.detectChanges();
  };

  beforeEach(() => {
    fixture = TestBed.createComponent(Anfitrion);
    fixture.detectChanges();
  });

  it('aparece con el mensaje y se va solo', fakeAsync(() => {
    mostrar('Artículo guardado');
    expect(aviso()?.textContent).toContain('Artículo guardado');
    expect(aviso()?.getAttribute('role')).toBe('status');

    tick(DURACION_AVISO_MS);
    fixture.detectChanges();

    expect(fixture.componentInstance.mensaje()).toBeNull();
    expect(aviso()).toBeNull();
  }));

  it('se puede cerrar antes de que se vaya', fakeAsync(() => {
    mostrar('Artículo guardado');

    const cerrar = aviso()?.querySelector<HTMLButtonElement>('button[aria-label="Cerrar aviso"]');
    expect(cerrar).withContext('botón Cerrar aviso').toBeTruthy();
    cerrar?.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.mensaje()).toBeNull();
    expect(aviso()).toBeNull();
    tick(DURACION_AVISO_MS);
  }));

  it('un error se anuncia como alerta', fakeAsync(() => {
    fixture.componentInstance.tipo.set('error');
    mostrar('No se pudo guardar');

    expect(aviso()?.getAttribute('role')).toBe('alert');
    tick(DURACION_AVISO_MS);
  }));
});
