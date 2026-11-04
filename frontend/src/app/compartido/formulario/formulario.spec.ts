import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CampoFormulario, Formulario, ValoresFormulario } from './formulario';

const CAMPOS: CampoFormulario[] = [
  { nombre: 'nombre', etiqueta: 'Nombre', obligatorio: true, ayuda: 'Como figura en la factura del proveedor.' },
  { nombre: 'cuit', etiqueta: 'CUIT', patron: '\\d{2}-\\d{8}-\\d', mensajeFormato: 'El CUIT va como 20-12345678-3.' },
  { nombre: 'minimo', etiqueta: 'Stock mínimo', tipo: 'number' },
  { nombre: 'telefonos', etiqueta: 'Teléfonos', multivaluado: true },
];

@Component({
  imports: [Formulario],
  template: `
    <app-formulario [campos]="campos" [valores]="valores()" [enviando]="enviando()"
      [erroresPorCampo]="porCampo()" [errorGeneral]="general()" [confirmacion]="confirmacion()"
      (enviar)="enviados.push($event)" />
  `,
})
class Anfitrion {
  readonly campos = CAMPOS;
  readonly valores = signal<ValoresFormulario>({});
  readonly enviando = signal(false);
  readonly porCampo = signal<Record<string, string>>({});
  readonly general = signal<string | null>(null);
  readonly confirmacion = signal<string | null>(null);
  readonly enviados: ValoresFormulario[] = [];
}

describe('Formulario (HU-UI-05)', () => {
  let fixture: ComponentFixture<Anfitrion>;
  const raiz = () => fixture.nativeElement as HTMLElement;
  const anfitrion = () => fixture.componentInstance;
  const porId = (id: string) => raiz().querySelector<HTMLElement>('#' + CSS.escape(id));

  /** Si falta un elemento, falla por aserción y la prueba sigue con uno suelto, sin TypeError. */
  const oFallar = <T extends HTMLElement>(elemento: T | null, motivo: string, suelto: () => T) =>
    elemento ?? (fail(motivo), suelto());
  /** El control se busca por su etiqueta visible, a través del for: si no está asociado, no aparece. */
  const control = (etiqueta: string) => {
    const label = Array.from(raiz().querySelectorAll('label')).find(l => l.textContent?.trim() === etiqueta);
    return oFallar(label?.htmlFor ? (porId(label.htmlFor) as HTMLInputElement | null) : null,
      `No hay un control asociado a la etiqueta «${etiqueta}».`, () => document.createElement('input'));
  };
  const botonEnviar = () => oFallar(raiz().querySelector<HTMLButtonElement>('button[type=submit]'),
    'No hay botón de envío.', () => document.createElement('button'));
  const escribir = (campo: HTMLInputElement, valor: string) => {
    campo.value = valor;
    campo.dispatchEvent(new Event('input'));
    fixture.detectChanges();
  };
  const guardar = () => {
    botonEnviar().click();
    fixture.detectChanges();
  };
  const descriptoresDe = (campo: Element) =>
    (campo.getAttribute('aria-describedby') ?? '').split(' ').filter(Boolean).map(id => porId(id));
  /** El error de un campo: descripto por aria-describedby, con role=alert. */
  const errorDe = (campo: Element) => descriptoresDe(campo).find(d => d?.getAttribute('role') === 'alert') ?? null;
  const telefonos = () => Array.from(raiz().querySelectorAll<HTMLInputElement>('input[aria-label^="Teléfonos "]'));
  const botonPorNombre = (nombre: string) => oFallar(raiz().querySelector<HTMLButtonElement>(`button[aria-label="${nombre}"]`),
    `No hay botón «${nombre}».`, () => document.createElement('button'));

  beforeEach(() => {
    fixture = TestBed.createComponent(Anfitrion);
    fixture.detectChanges();
  });

  it('cada campo tiene su etiqueta asociada por for/id', () => {
    for (const etiqueta of ['Nombre', 'CUIT', 'Stock mínimo']) {
      const campo = control(etiqueta);
      expect(Array.from(campo.labels ?? []).map(l => l.textContent?.trim())).withContext(etiqueta).toEqual([etiqueta]);
    }
  });

  it('la ayuda va debajo del campo y queda vinculada con aria-describedby', () => {
    const nombre = control('Nombre');
    const ayuda = descriptoresDe(nombre).find(d => d?.classList.contains('ayuda'));

    expect(ayuda?.textContent?.trim()).toBe('Como figura en la factura del proveedor.');
    expect(ayuda ? nombre.compareDocumentPosition(ayuda) & Node.DOCUMENT_POSITION_FOLLOWING : 0).withContext('debajo').toBeTruthy();
  });

  it('CA-002 · obligatorio vacío: no envía, y el error queda junto al campo, vinculado y con role=alert', () => {
    guardar();

    expect(anfitrion().enviados).toEqual([]);
    const nombre = control('Nombre');
    const error = errorDe(nombre);
    expect(error?.textContent?.trim()).toBe('Este campo es obligatorio.');
    expect(error?.parentElement).withContext('junto al campo que lo produjo').toBe(nombre.parentElement);
    expect(nombre.getAttribute('aria-invalid')).toBe('true');
  });

  it('CA-002 · formato inválido: no envía y muestra el mensaje de formato del campo', () => {
    escribir(control('Nombre'), 'Bujía NGK');
    escribir(control('CUIT'), '20123456783');
    guardar();

    expect(anfitrion().enviados).toEqual([]);
    expect(errorDe(control('CUIT'))?.textContent?.trim()).toBe('El CUIT va como 20-12345678-3.');
    expect(errorDe(control('Nombre'))).withContext('el campo válido no muestra error').toBeNull();
  });

  it('CA-001 · completo y válido: envía los valores, con los números como números, y confirma', () => {
    escribir(control('Nombre'), 'Bujía NGK');
    escribir(control('CUIT'), '20-12345678-3');
    escribir(control('Stock mínimo'), '5');
    guardar();

    expect(anfitrion().enviados).toEqual([{ nombre: 'Bujía NGK', cuit: '20-12345678-3', minimo: 5, telefonos: [] }]);

    anfitrion().confirmacion.set('Proveedor guardado.');
    fixture.detectChanges();
    expect(raiz().querySelector('[role=status]')?.textContent?.trim()).toBe('Proveedor guardado.');
  });

  it('mientras envía, el botón queda en carga y un segundo envío no sale', () => {
    escribir(control('Nombre'), 'Bujía NGK');
    guardar();
    anfitrion().enviando.set(true);
    fixture.detectChanges();

    const boton = botonEnviar();
    expect(boton.disabled).toBeTrue();
    expect(boton.getAttribute('aria-busy')).toBe('true');
    raiz().querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();

    expect(anfitrion().enviados.length).toBe(1);
  });

  it('CA-003 · agregar y quitar un teléfono actualiza la lista sin tocar el resto del formulario', () => {
    escribir(control('Nombre'), 'Bujía NGK');
    botonPorNombre('Agregar a Teléfonos').click();
    fixture.detectChanges();
    botonPorNombre('Agregar a Teléfonos').click();
    fixture.detectChanges();
    const [primero, segundo] = telefonos();
    escribir(oFallar(primero ?? null, 'No apareció el primer teléfono.', () => document.createElement('input')), '351 111');
    escribir(oFallar(segundo ?? null, 'No apareció el segundo teléfono.', () => document.createElement('input')), '351 222');

    botonPorNombre('Quitar Teléfonos 1').click();
    fixture.detectChanges();

    expect(telefonos().map(t => t.value)).toEqual(['351 222']);
    expect(control('Nombre').value).toBe('Bujía NGK');
    guardar();
    expect(anfitrion().enviados[0]?.['telefonos']).toEqual(['351 222']);
    expect(anfitrion().enviados[0]?.['nombre']).toBe('Bujía NGK');
  });

  it('para modificar, carga los valores que recibe, incluidas las listas', () => {
    anfitrion().valores.set({ nombre: 'Bujía NGK', minimo: 3, telefonos: ['351 111', '351 222'] });
    fixture.detectChanges();

    expect(control('Nombre').value).toBe('Bujía NGK');
    expect(control('Stock mínimo').value).toBe('3');
    expect(telefonos().map(t => t.value)).toEqual(['351 111', '351 222']);
  });

  it('CA-004 · un error del servidor por campo se muestra junto a ese campo, vinculado y con role=alert', () => {
    anfitrion().porCampo.set({ nombre: 'El nombre no puede superar los 200 caracteres.' });
    fixture.detectChanges();

    const nombre = control('Nombre');
    const error = errorDe(nombre);
    expect(error?.textContent?.trim()).toBe('El nombre no puede superar los 200 caracteres.');
    expect(error?.parentElement).toBe(nombre.parentElement);
    expect(nombre.getAttribute('aria-invalid')).toBe('true');
  });

  it('el error general del servidor se anuncia con role=alert', () => {
    anfitrion().general.set('Ya existe un proveedor con ese CUIT.');
    fixture.detectChanges();

    const alertas = Array.from(raiz().querySelectorAll('[role=alert]')).map(a => a.textContent?.trim());
    expect(alertas).toEqual(['Ya existe un proveedor con ese CUIT.']);
  });
});

const CAMPOS_ACCESO: CampoFormulario[] = [
  { nombre: 'nombreUsuario', etiqueta: 'Usuario', obligatorio: true, autocompletar: 'username' },
  { nombre: 'contrasena', etiqueta: 'Contraseña', tipo: 'password', obligatorio: true, autocompletar: 'current-password' },
  { nombre: 'nota', etiqueta: 'Nota' },
];

@Component({
  imports: [Formulario],
  template: `<app-formulario [campos]="campos" />`,
})
class AnfitrionAcceso {
  readonly campos = CAMPOS_ACCESO;
}

describe('Formulario · contraseña y autocompletado (extensión de HU-UI-03)', () => {
  let fixture: ComponentFixture<AnfitrionAcceso>;
  const raiz = () => fixture.nativeElement as HTMLElement;
  const control = (etiqueta: string) => {
    const label = Array.from(raiz().querySelectorAll('label')).find(l => l.textContent?.trim() === etiqueta)!;
    return label.control as HTMLInputElement;
  };

  beforeEach(() => {
    fixture = TestBed.createComponent(AnfitrionAcceso);
    fixture.detectChanges();
  });

  it('una contraseña sale como input type="password"', () => {
    expect(control('Contraseña').type).toBe('password');
  });

  it('el valor escrito en la contraseña no queda expuesto en el marcado', () => {
    const contrasena = control('Contraseña');
    contrasena.value = 'secreto-de-prueba';
    contrasena.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(contrasena.hasAttribute('value')).toBeFalse();
    expect(raiz().innerHTML).not.toContain('secreto-de-prueba');
  });

  it('cada campo lleva su autocomplete, para que el gestor de contraseñas complete el acceso', () => {
    expect(control('Usuario').getAttribute('autocomplete')).toBe('username');
    expect(control('Contraseña').getAttribute('autocomplete')).toBe('current-password');
  });

  it('un campo sin autocompletar no lleva el atributo', () => {
    expect(control('Nota').hasAttribute('autocomplete')).toBeFalse();
  });
});
