import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmacionMotivo } from './confirmacion-motivo';

@Component({
  imports: [ConfirmacionMotivo],
  template: `
    <button id="abrir" class="peligro-borde" (click)="abierto.set(true)">Dar de baja</button>
    <app-confirmacion-motivo titulo="Dar de baja el artículo" [(abierto)]="abierto" [maximoMotivo]="200"
      [clave]="clave()" textoConfirmar="Dar de baja" (confirmar)="confirmados.push($event)" />
  `,
})
class Anfitrion {
  readonly abierto = signal(false);
  readonly clave = signal<string | null>(null);
  readonly confirmados: string[] = [];
}

describe('ConfirmacionMotivo (HU-UI-06)', () => {
  let fixture: ComponentFixture<Anfitrion>;
  const raiz = () => fixture.nativeElement as HTMLElement;
  const anfitrion = () => fixture.componentInstance;
  const dialogo = () => raiz().querySelector('dialog');
  const abridor = () => raiz().querySelector<HTMLButtonElement>('#abrir')!;
  const oFallar = <T extends HTMLElement>(elemento: T | null, motivo: string, suelto: () => T) =>
    elemento ?? (fail(motivo), suelto());
  const control = (etiqueta: string) => {
    const label = Array.from(raiz().querySelectorAll('dialog label')).find(l => l.textContent?.trim() === etiqueta) as HTMLLabelElement | undefined;
    return oFallar(label?.htmlFor ? raiz().querySelector<HTMLInputElement>('#' + CSS.escape(label.htmlFor)) : null,
      `No hay un campo «${etiqueta}» en la confirmación.`, () => document.createElement('input'));
  };
  const motivo = () => control('Motivo');
  const confirmar = () => oFallar(raiz().querySelector<HTMLButtonElement>('dialog button[type=submit]'),
    'No hay botón de confirmar.', () => document.createElement('button'));
  const cancelar = () => oFallar(
    Array.from(raiz().querySelectorAll<HTMLButtonElement>('dialog button')).find(b => b.textContent?.trim() === 'Cancelar') ?? null,
    'No hay botón Cancelar.', () => document.createElement('button'));
  const escribir = (campo: HTMLInputElement, valor: string) => {
    campo.value = valor;
    campo.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };
  const abrir = () => {
    abridor().focus();
    abridor().click();
    fixture.detectChanges();
  };
  const errorDe = (campo: Element) => (campo.getAttribute('aria-describedby') ?? '').split(' ').filter(Boolean)
    .map(id => document.getElementById(id)).find(e => e?.getAttribute('role') === 'alert') ?? null;

  beforeEach(() => {
    fixture = TestBed.createComponent(Anfitrion);
    document.body.appendChild(fixture.nativeElement);
    fixture.detectChanges();
  });

  afterEach(() => (fixture.nativeElement as HTMLElement).remove());

  it('al abrirse, el foco arranca en el campo de motivo', () => {
    abrir();

    expect(dialogo()?.open).withContext('abierto').toBeTrue();
    expect(document.activeElement).toBe(motivo());
  });

  it('CA-002 · sin motivo no se confirma: no ejecuta nada y el error queda junto al motivo', () => {
    abrir();
    escribir(motivo(), '   ');
    confirmar().click();
    fixture.detectChanges();

    expect(anfitrion().confirmados).toEqual([]);
    expect(errorDe(motivo())?.textContent?.trim()).toBe('Este campo es obligatorio.');
    expect(dialogo()?.open).withContext('sigue abierto').toBeTrue();
  });

  it('con motivo, confirma una vez con el motivo sin espacios en los bordes', () => {
    abrir();
    escribir(motivo(), '  Discontinuado por el fabricante  ');
    confirmar().click();
    fixture.detectChanges();

    expect(anfitrion().confirmados).toEqual(['Discontinuado por el fabricante']);
  });

  it('el botón de confirmar es peligro (punto sin retorno), con el texto de la operación', () => {
    abrir();

    expect(confirmar().classList).toContain('peligro');
    expect(confirmar().classList).not.toContain('primario');
    expect(confirmar().innerText.trim()).withContext('texto visible').toBe('Dar de baja');
  });

  it('muestra el límite del motivo que fija el endpoint', () => {
    abrir();

    expect(motivo().maxLength).toBe(200);
    const ayuda = (motivo().getAttribute('aria-describedby') ?? '').split(' ')
      .map(id => document.getElementById(id)).find(e => e?.classList.contains('ayuda'));
    expect(ayuda?.textContent).toContain('200');
  });

  it('CA-003 · Escape cancela sin ejecutar nada y el foco vuelve al botón que abrió', () => {
    abrir();
    escribir(motivo(), 'Discontinuado');

    dialogo()?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();

    expect(anfitrion().confirmados).toEqual([]);
    expect(anfitrion().abierto()).toBeFalse();
    expect(document.activeElement).toBe(abridor());
  });

  it('CA-003 · Cancelar cierra sin ejecutar nada y el foco vuelve al botón que abrió', () => {
    abrir();
    escribir(motivo(), 'Discontinuado');

    cancelar().click();
    fixture.detectChanges();

    expect(anfitrion().confirmados).toEqual([]);
    expect(anfitrion().abierto()).toBeFalse();
    expect(document.activeElement).toBe(abridor());
  });

  it('al reabrir, el motivo de la vez anterior no queda cargado', () => {
    abrir();
    escribir(motivo(), 'Discontinuado');
    cancelar().click();
    fixture.detectChanges();

    abrir();

    expect(motivo().value).toBe('');
  });

  it('sin clave, no pide reescribir nada', () => {
    abrir();

    expect(raiz().querySelectorAll('dialog input').length).toBe(1);
  });

  it('CA-002 · con clave, si no coincide no se confirma y lo indica junto a ese campo', () => {
    anfitrion().clave.set('LC-000123');
    abrir();
    escribir(motivo(), 'Error de carga');
    const clave = control('Para confirmar, escribí LC-000123');
    escribir(clave, 'LC-000124');
    confirmar().click();
    fixture.detectChanges();

    expect(anfitrion().confirmados).toEqual([]);
    expect(errorDe(clave)?.textContent?.trim()).toBe('No coincide con LC-000123.');
  });

  it('con clave, la compara sin espacios en los bordes y sin distinguir mayúsculas, como el buscador', () => {
    anfitrion().clave.set('LC-000123');
    abrir();
    escribir(motivo(), 'Error de carga');
    escribir(control('Para confirmar, escribí LC-000123'), '  lc-000123 ');
    confirmar().click();
    fixture.detectChanges();

    expect(anfitrion().confirmados).toEqual(['Error de carga']);
  });
});
