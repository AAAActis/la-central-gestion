import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AVISO_SESION_VENCIDA, CLAVE_SESION, Sesion, SesionActual, agregarToken } from './sesion';

const SESION: Sesion = { token: 'token-de-prueba', nombreUsuario: 'prueba', rol: 'OPERADOR', sucursalId: 1 };

function configurar() {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(withInterceptors([agregarToken])), provideHttpClientTesting()],
  });
}

/** Lo que ve la aplicación después de un F5: un inyector nuevo que lee lo guardado. */
function recargar(): SesionActual {
  TestBed.resetTestingModule();
  configurar();
  return TestBed.inject(SesionActual);
}

function guardadoEn(almacen: Storage): string {
  return Array.from({ length: almacen.length }, (_, i) => almacen.getItem(almacen.key(i)!)).join('\n');
}

describe('SesionActual · la sesión sobrevive a F5 y muere al salir (HU-UI-03)', () => {
  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    configurar();
  });

  it('F5 · la sesión iniciada sigue después de recargar la página', () => {
    TestBed.inject(SesionActual).iniciar(SESION);
    expect(recargar().sesion()).toEqual(SESION);
  });

  it('se guarda en sessionStorage, no en localStorage: cerrar la pestaña la termina', () => {
    TestBed.inject(SesionActual).iniciar(SESION);
    expect(guardadoEn(sessionStorage)).toContain('token-de-prueba');
    expect(guardadoEn(localStorage)).not.toContain('token-de-prueba');
  });

  it('CA-004 · Salir descarta el token: no queda guardado y una recarga no lo recupera', () => {
    const sesion = TestBed.inject(SesionActual);
    sesion.iniciar(SESION);
    sesion.cerrar();
    expect(guardadoEn(sessionStorage)).not.toContain('token-de-prueba');
    expect(recargar().sesion()).toBeNull();
  });

  it('un valor guardado ilegible se trata como sin sesión, sin romper la aplicación', () => {
    sessionStorage.setItem(CLAVE_SESION, '{roto');
    expect(recargar().sesion()).toBeNull();
  });

  it('iniciar una sesión nueva borra el aviso de la anterior', () => {
    const sesion = TestBed.inject(SesionActual);
    sesion.cerrar(AVISO_SESION_VENCIDA);
    sesion.iniciar(SESION);
    expect(sesion.avisoAlCerrar()).toBeNull();
  });
});

describe('agregarToken · el token viaja solo y el vencimiento cierra la sesión (HU-UI-03)', () => {
  let http: HttpClient;
  let servidor: HttpTestingController;
  let sesion: SesionActual;

  beforeEach(() => {
    sessionStorage.clear();
    configurar();
    http = TestBed.inject(HttpClient);
    servidor = TestBed.inject(HttpTestingController);
    sesion = TestBed.inject(SesionActual);
  });

  afterEach(() => servidor.verify());

  function pedir(url = '/api/articulos') {
    http.get(url).subscribe({ error: () => {} });
    return servidor.expectOne(url);
  }

  it('CA-001 · con sesión activa, toda llamada a la API lleva el token sin intervención del usuario', () => {
    sesion.iniciar(SESION);
    expect(pedir().request.headers.get('Authorization')).toBe('Bearer token-de-prueba');
  });

  it('CA-001 · el login no lleva token', () => {
    sesion.iniciar(SESION);
    http.post('/api/acceso/login', {}).subscribe();
    expect(servidor.expectOne('/api/acceso/login').request.headers.has('Authorization')).toBeFalse();
  });

  it('CA-002 · un 401 con la sesión vigente la cierra y deja el aviso de sesión vencida', () => {
    sesion.iniciar(SESION);
    pedir().flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(sesion.sesion()).toBeNull();
    expect(sesion.avisoAlCerrar()).toBe(AVISO_SESION_VENCIDA);
    expect(guardadoEn(sessionStorage)).not.toContain('token-de-prueba');
  });

  it('CA-002 · un 403 no cierra la sesión: la sesión es válida, el rol no alcanza', () => {
    sesion.iniciar(SESION);
    pedir().flush(null, { status: 403, statusText: 'Forbidden' });
    expect(sesion.sesion()).toEqual(SESION);
  });

  it('CA-002 · el 401 de un token anterior no cierra la sesión nueva', () => {
    sesion.iniciar(SESION);
    const vieja = pedir();
    const nueva = { ...SESION, token: 'token-nuevo' };
    sesion.iniciar(nueva);
    vieja.flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(sesion.sesion()).toEqual(nueva);
  });

  it('CA-004 · después de Salir, las llamadas salen sin token', () => {
    sesion.iniciar(SESION);
    sesion.cerrar();
    expect(pedir().request.headers.has('Authorization')).toBeFalse();
  });
});
