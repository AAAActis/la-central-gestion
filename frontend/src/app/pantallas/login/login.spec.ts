import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { AVISO_SESION_VENCIDA, SesionActual } from '../../nucleo/sesion';
import { Login } from './login';

describe('Login · pantalla de acceso (HU-UI-03)', () => {
  let fixture: ComponentFixture<Login>;
  let servidor: HttpTestingController;
  const raiz = () => fixture.nativeElement as HTMLElement;
  const aviso = () => raiz().querySelector<HTMLElement>('.aviso-flotante');
  const ingresar = () => raiz().querySelector<HTMLButtonElement>('button[type=submit]')!;

  /** El control asociado a la etiqueta, por su texto: lo que encuentra quien usa la pantalla. */
  function campo(etiqueta: string): HTMLInputElement {
    const label = Array.from(raiz().querySelectorAll('label')).find(l => l.textContent?.trim().startsWith(etiqueta))!;
    return (label.control ?? label.querySelector('input')) as HTMLInputElement;
  }

  function montar() {
    fixture = TestBed.createComponent(Login);
    fixture.detectChanges();
  }

  function intentar(usuario = 'admin', contrasena = 'mala') {
    for (const [input, valor] of [[campo('Usuario'), usuario], [campo('Contraseña'), contrasena]] as const) {
      input.value = valor;
      input.dispatchEvent(new Event('input'));
    }
    ingresar().click();
    fixture.detectChanges();
  }

  function responder(status: number, cuerpo: object | null = null) {
    servidor.expectOne('/api/acceso/login').flush(cuerpo, { status, statusText: 'x' });
    fixture.detectChanges();
  }

  // El aviso escribe su texto en el ciclo siguiente (aviso-flotante.ts).
  function escrito() {
    tick(0);
    fixture.detectChanges();
  }

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ imports: [Login], providers: [provideHttpClient(), provideHttpClientTesting()] });
    servidor = TestBed.inject(HttpTestingController);
  });

  afterEach(() => servidor.verify());

  it('un ingreso fallido va al aviso flotante de error y no dice cuál de los dos datos falló', fakeAsync(() => {
    montar();
    intentar();
    // Aunque el servidor dijera de más, la pantalla no confirma que el usuario existe.
    responder(401, { status: 401, detail: 'La contraseña de admin es incorrecta.' });
    escrito();

    expect(aviso()?.getAttribute('role')).toBe('alert');
    expect(aviso()?.textContent?.trim()).toBe('Usuario o contraseña incorrectos.');
    expect(raiz().querySelector('.error:not(.aviso-flotante)')).withContext('sin error suelto en la tarjeta').toBeNull();
  }));

  it('dos ingresos fallidos iguales dejan un solo aviso y lo vuelven a anunciar', fakeAsync(() => {
    montar();
    intentar();
    responder(401);
    escrito();

    intentar();
    responder(401);
    expect(aviso()?.textContent?.trim()).withContext('la región se vacía antes de reescribirse').toBe('');
    escrito();

    expect(raiz().querySelectorAll('.aviso-flotante').length).toBe(1);
    expect(aviso()?.textContent?.trim()).toBe('Usuario o contraseña incorrectos.');
  }));

  it('un error que no es de credenciales dice su motivo', fakeAsync(() => {
    montar();
    intentar();
    responder(0);
    escrito();

    expect(aviso()?.textContent).toContain('No hay conexión con la API');
  }));

  it('Ingresar es la única acción principal de la pantalla', () => {
    montar();
    const primarios = Array.from(raiz().querySelectorAll('button.primario'));
    expect(primarios).toEqual([ingresar()]);
    expect(ingresar().textContent?.trim()).toBe('Ingresar');
  });

  it('mientras ingresa, el botón queda deshabilitado y ocupado, y no envía dos veces', fakeAsync(() => {
    montar();
    intentar();
    expect(ingresar().disabled).toBeTrue();
    expect(ingresar().getAttribute('aria-busy')).toBe('true');

    ingresar().click();
    fixture.detectChanges();
    expect(servidor.match('/api/acceso/login').length).withContext('pedidos de login').toBe(1);
  }));

  it('un ingreso correcto inicia la sesión con lo que devuelve la API', () => {
    montar();
    intentar('admin', 'buena');
    const sesion = { token: 't', nombreUsuario: 'admin', rol: 'ADMINISTRADOR', sucursalId: 1 };
    responder(200, sesion);
    expect(TestBed.inject(SesionActual).sesion()).toEqual(sesion);
  });

  it('CA-002 · si la sesión anterior venció, la pantalla de acceso lo informa', fakeAsync(() => {
    TestBed.inject(SesionActual).cerrar(AVISO_SESION_VENCIDA);
    montar();
    escrito();

    expect(aviso()?.textContent?.trim()).toBe(AVISO_SESION_VENCIDA);
  }));

  it('la tarjeta lleva el panel con la marca en su variante completa', () => {
    montar();
    expect(raiz().querySelector('app-marca .logo.completa')).not.toBeNull();
  });
});
