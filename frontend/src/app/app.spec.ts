import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { App } from './app';
import { appConfig } from './app.config';
import { rutasDe } from './app.routes';
import { MODULOS_DEL_SISTEMA } from './modulos';
import { MODULOS, Modulo } from './nucleo/acceso';
import { AVISO_SESION_VENCIDA, SesionActual, agregarToken } from './nucleo/sesion';

@Component({ template: '<p id="pantalla-admin">Pantalla de administrador</p>' })
class PantallaDeAdministrador {}

/** Un módulo exclusivo de ADMINISTRADOR. Solo existe en el test: no hay pantalla de usuarios. */
const SOLO_ADMINISTRADOR: Modulo = {
  ruta: 'solo-administrador', titulo: 'Solo administrador', roles: ['ADMINISTRADOR'], componente: PantallaDeAdministrador,
};

async function armar(modulos: readonly Modulo[]): Promise<ComponentFixture<App>> {
  TestBed.configureTestingModule({
    imports: [App],
    providers: [
      provideRouter(rutasDe(modulos)),
      { provide: MODULOS, useValue: modulos },
      provideHttpClient(withInterceptors([agregarToken])),
      provideHttpClientTesting(),
    ],
  });
  const fixture = TestBed.createComponent(App);
  fixture.autoDetectChanges();
  await fixture.whenStable();
  return fixture;
}

function entrarComo(rol: string, nombreUsuario = 'prueba') {
  TestBed.inject(SesionActual).iniciar({ token: 'token-de-prueba', nombreUsuario, rol, sucursalId: 1 });
}

async function ir(fixture: ComponentFixture<App>, url: string) {
  await TestBed.inject(Router).navigateByUrl(url);
  await fixture.whenStable();
}

function enlacesDelMenu(fixture: ComponentFixture<App>): HTMLAnchorElement[] {
  return Array.from((fixture.nativeElement as HTMLElement).querySelectorAll<HTMLAnchorElement>('header nav a'));
}

function enlace(fixture: ComponentFixture<App>, titulo: string): HTMLAnchorElement | undefined {
  return enlacesDelMenu(fixture).find(a => a.textContent?.trim() === titulo);
}

function pantalla(fixture: ComponentFixture<App>, selector: string): Element | null {
  return (fixture.nativeElement as HTMLElement).querySelector(`main ${selector}`);
}

describe('App · marco común y navegación (HU-UI-01)', () => {
  // La sesión se guarda en sessionStorage (HU-UI-03): sin limpiarlo, un test hereda la del anterior.
  beforeEach(() => sessionStorage.clear());

  describe('con los módulos del sistema', () => {
    let fixture: ComponentFixture<App>;

    beforeEach(async () => {
      fixture = await armar(MODULOS_DEL_SISTEMA);
    });

    it('sin sesión muestra el login y no muestra el menú', async () => {
      await ir(fixture, '/factura-compra');
      expect(pantalla(fixture, 'app-login')).not.toBeNull();
      expect(pantalla(fixture, 'app-factura-compra')).toBeNull();
      expect((fixture.nativeElement as HTMLElement).querySelector('header')).toBeNull();
    });

    it('al iniciar sesión abre el primer módulo habilitado', async () => {
      await ir(fixture, '/');
      entrarComo('OPERADOR');
      await fixture.whenStable();
      expect(pantalla(fixture, 'app-articulos')).not.toBeNull();
      expect(TestBed.inject(Router).url).toBe('/articulos');
    });

    it('CA1 · con sesión, el menú muestra los módulos habilitados con su ruta', async () => {
      entrarComo('OPERADOR');
      await ir(fixture, '/articulos');
      const enlaces = enlacesDelMenu(fixture);
      expect(enlaces.map(a => a.textContent?.trim())).toEqual(['Artículos', 'Factura de compra']);
      expect(enlaces.map(a => a.getAttribute('href'))).toEqual(['/articulos', '/factura-compra']);
    });

    it('CA1 · marca el módulo en el que está el usuario', async () => {
      entrarComo('OPERADOR');
      await ir(fixture, '/factura-compra');
      const activos = enlacesDelMenu(fixture).filter(a => a.getAttribute('aria-current') === 'page');
      expect(activos.map(a => a.textContent?.trim())).toEqual(['Factura de compra']);
    });

    // Sin valores de color (CLAUDE.md, Frontend): lo que importa es que el activo se distinga.
    it('CA1 · la marca se ve: solo el módulo activo lleva la raya debajo, y se distingue del resto', async () => {
      entrarComo('OPERADOR');
      await ir(fixture, '/factura-compra');
      const activo = enlace(fixture, 'Factura de compra')!;
      const otro = enlace(fixture, 'Artículos')!;
      expect(activo.classList).toContain('activo');
      expect(otro.classList).not.toContain('activo');
      expect(parseFloat(getComputedStyle(activo).borderBottomWidth)).toBeGreaterThan(0);
      expect(getComputedStyle(activo).borderBottomColor).not.toBe(getComputedStyle(otro).borderBottomColor);
    });

    it('CA3 · cambiar de sección por el menú no recarga la aplicación ni pierde la sesión', async () => {
      entrarComo('OPERADOR');
      await ir(fixture, '/articulos');
      const encabezado = (fixture.nativeElement as HTMLElement).querySelector('header');

      const aFactura = enlace(fixture, 'Factura de compra');
      expect(aFactura).withContext('enlace a Factura de compra').toBeDefined();
      aFactura?.click();
      await fixture.whenStable();

      expect(TestBed.inject(Router).url).toBe('/factura-compra');
      expect(pantalla(fixture, 'app-factura-compra')).not.toBeNull();
      expect(pantalla(fixture, 'app-articulos')).toBeNull();
      expect((fixture.nativeElement as HTMLElement).querySelector('header')).toBe(encabezado);
      expect(TestBed.inject(SesionActual).sesion()?.token).toBe('token-de-prueba');

      enlace(fixture, 'Artículos')?.click();
      await fixture.whenStable();
      expect(pantalla(fixture, 'app-articulos')).not.toBeNull();
      expect(TestBed.inject(SesionActual).sesion()?.token).toBe('token-de-prueba');
    });

    it('una ruta que no existe lleva al primer módulo habilitado', async () => {
      entrarComo('OPERADOR');
      await ir(fixture, '/no-existe');
      expect(TestBed.inject(Router).url).toBe('/articulos');
    });

    it('Salir cierra la sesión y vuelve al login', async () => {
      entrarComo('OPERADOR');
      await ir(fixture, '/articulos');
      const salir = Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('header button'))
        .find(b => b.textContent?.trim() === 'Salir') as HTMLButtonElement;

      salir.click();
      await fixture.whenStable();

      expect(TestBed.inject(SesionActual).sesion()).toBeNull();
      expect(pantalla(fixture, 'app-login')).not.toBeNull();
      expect(pantalla(fixture, 'app-articulos')).toBeNull();
      // El botón Salir desaparece con el marco: el foco no puede quedar en la nada.
      expect(document.activeElement).toBe(pantalla(fixture, 'app-login input[name=usuario]'));
    });

    it('el encabezado lleva la marca compacta, el usuario con su rol y Salir; abajo va el pie', async () => {
      entrarComo('OPERADOR', 'mostrador');
      await ir(fixture, '/articulos');
      const encabezado = (fixture.nativeElement as HTMLElement).querySelector('header')!;
      expect(encabezado.querySelector('app-marca .logo.compacta')).not.toBeNull();
      expect(encabezado.textContent).toContain('mostrador');
      expect(encabezado.textContent).toContain('OPERADOR');
      expect((fixture.nativeElement as HTMLElement).querySelector('footer')).not.toBeNull();
    });

    for (const ancho of [1024, 768, 480]) {
      it(`CA4 · a ${ancho} px de ancho, el menú y el contenido no desbordan a lo ancho`, async () => {
        const raiz = fixture.nativeElement as HTMLElement;
        raiz.style.display = 'block';
        raiz.style.width = `${ancho}px`;
        entrarComo('ADMINISTRADOR', 'un nombre de usuario bastante largo');
        for (const url of ['/articulos', '/factura-compra']) {
          await ir(fixture, url);
          expect(raiz.scrollWidth).withContext(url).toBeLessThanOrEqual(raiz.clientWidth);
          expect(raiz.querySelector('header')!.scrollWidth).withContext(url).toBeLessThanOrEqual(ancho);
        }
      });
    }
  });

  describe('CA2 · con un módulo exclusivo de ADMINISTRADOR', () => {
    let fixture: ComponentFixture<App>;

    beforeEach(async () => {
      fixture = await armar([...MODULOS_DEL_SISTEMA, SOLO_ADMINISTRADOR]);
    });

    it('el administrador lo ve en el menú y puede abrirlo', async () => {
      entrarComo('ADMINISTRADOR');
      await ir(fixture, '/solo-administrador');
      expect(enlacesDelMenu(fixture).map(a => a.textContent?.trim())).toContain('Solo administrador');
      expect(pantalla(fixture, '#pantalla-admin')).not.toBeNull();
    });

    it('el operador no lo ve en el menú, y si escribe la ruta queda en su primer módulo', async () => {
      entrarComo('OPERADOR');
      await ir(fixture, '/solo-administrador');
      expect(enlacesDelMenu(fixture).map(a => a.textContent?.trim())).not.toContain('Solo administrador');
      expect(pantalla(fixture, '#pantalla-admin')).toBeNull();
      expect(TestBed.inject(Router).url).toBe('/articulos');
    });
  });
});

describe('App · control de sesión (HU-UI-03)', () => {
  // La sesión se guarda en sessionStorage (HU-UI-03): sin limpiarlo, un test hereda la del anterior.
  beforeEach(() => sessionStorage.clear());

  let fixture: ComponentFixture<App>;

  beforeEach(async () => {
    fixture = await armar(MODULOS_DEL_SISTEMA);
  });

  function enLogin() {
    return pantalla(fixture, 'app-login') !== null && (fixture.nativeElement as HTMLElement).querySelector('header') === null;
  }

  it('F5 · recargar la página no pierde la sesión ni la pantalla', async () => {
    entrarComo('OPERADOR');
    await ir(fixture, '/factura-compra');

    TestBed.resetTestingModule();
    const recargada = await armar(MODULOS_DEL_SISTEMA);
    await ir(recargada, '/factura-compra');

    expect((recargada.nativeElement as HTMLElement).querySelector('main app-factura-compra')).not.toBeNull();
    expect((recargada.nativeElement as HTMLElement).querySelector('header')).not.toBeNull();
  });

  it('CA-002 · si la API rechaza el token vencido, vuelve al ingreso e informa que la sesión venció', async () => {
    entrarComo('OPERADOR');
    await ir(fixture, '/articulos');

    TestBed.inject(HttpClient).get('/api/articulos').subscribe({ error: () => {} });
    TestBed.inject(HttpTestingController).expectOne('/api/articulos').flush(null, { status: 401, statusText: 'Unauthorized' });
    await fixture.whenStable();

    expect(enLogin()).toBeTrue();
    expect(pantalla(fixture, '.aviso-flotante')?.textContent?.trim()).toBe(AVISO_SESION_VENCIDA);
  });

  it('CA-003 · sin sesión, escribir la dirección de una pantalla interna lleva al ingreso', async () => {
    await ir(fixture, '/articulos');
    expect(enLogin()).toBeTrue();
    expect(pantalla(fixture, 'app-articulos')).toBeNull();
  });

  it('CA-004 · después de Salir, volver a la pantalla anterior no deja seguir operando', async () => {
    entrarComo('OPERADOR');
    await ir(fixture, '/factura-compra');
    TestBed.inject(SesionActual).cerrar();
    await fixture.whenStable();

    await ir(fixture, '/factura-compra');
    expect(enLogin()).toBeTrue();
    expect(pantalla(fixture, 'app-factura-compra')).toBeNull();
  });
});

describe('appConfig · el cableado real de la aplicación', () => {
  // La sesión se guarda en sessionStorage (HU-UI-03): sin limpiarlo, un test hereda la del anterior.
  beforeEach(() => sessionStorage.clear());

  it('con los providers de producción, el menú del operador lleva a Artículos y a Factura de compra', async () => {
    TestBed.configureTestingModule({ imports: [App], providers: [...appConfig.providers, provideHttpClientTesting()] });
    const fixture = TestBed.createComponent(App);
    fixture.autoDetectChanges();
    entrarComo('OPERADOR');
    await ir(fixture, '/factura-compra');

    expect(enlacesDelMenu(fixture).map(a => a.getAttribute('href'))).toEqual(['/articulos', '/factura-compra']);
    expect(pantalla(fixture, 'app-factura-compra')).not.toBeNull();
  });
});
