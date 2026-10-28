import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Aviso, AvisoFlotante, duracionAvisoMs } from './aviso-flotante';

@Component({
  imports: [AvisoFlotante],
  template: `<app-aviso-flotante [(mensaje)]="mensaje" [tipo]="tipo()" />`,
})
class Anfitrion {
  readonly mensaje = signal<Aviso | null>(null);
  readonly tipo = signal<'ok' | 'error'>('ok');
}

describe('AvisoFlotante', () => {
  let fixture: ComponentFixture<Anfitrion>;
  const aviso = () => (fixture.nativeElement as HTMLElement).querySelector<HTMLElement>('.aviso-flotante');
  const mostrar = (texto: string) => {
    fixture.componentInstance.mensaje.set({ texto });
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

    expect(duracionAvisoMs()).withContext('--dur-aviso').toBeGreaterThan(0);
    tick(duracionAvisoMs() - 1);
    fixture.detectChanges();
    expect(aviso()).withContext('un instante antes de --dur-aviso, sigue a la vista').not.toBeNull();

    tick(1);
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
    tick(duracionAvisoMs());
  }));

  it('un aviso nuevo reemplaza al anterior: tres errores seguidos dejan un solo cartel, el último', () => {
    fixture.componentInstance.tipo.set('error');
    for (const intento of ['Intento 1 rechazado', 'Intento 2 rechazado', 'Intento 3 rechazado']) mostrar(intento);

    const avisos = (fixture.nativeElement as HTMLElement).querySelectorAll('.aviso-flotante');
    expect(avisos.length).toBe(1);
    expect(avisos[0].textContent).toContain('Intento 3 rechazado');
  });

  it('el aviso nuevo dura su tiempo completo: no se lo lleva el temporizador del anterior', fakeAsync(() => {
    mostrar('Guardado');
    tick(duracionAvisoMs() - 1);
    mostrar('Guardado otra vez');

    tick(1);
    fixture.detectChanges();
    expect(aviso()?.textContent).toContain('Guardado otra vez');

    tick(duracionAvisoMs());
    fixture.detectChanges();
    expect(aviso()).toBeNull();
  }));

  it('un error se anuncia como alerta y no se va solo: queda hasta que el usuario lo cierra', fakeAsync(() => {
    fixture.componentInstance.tipo.set('error');
    mostrar('No se pudo guardar');
    expect(aviso()?.getAttribute('role')).toBe('alert');

    tick(duracionAvisoMs() * 3);
    fixture.detectChanges();
    expect(aviso()).withContext('pasado --dur-aviso, el error sigue a la vista').not.toBeNull();

    aviso()?.querySelector<HTMLButtonElement>('button[aria-label="Cerrar aviso"]')?.click();
    fixture.detectChanges();
    expect(aviso()).toBeNull();
  }));
});
