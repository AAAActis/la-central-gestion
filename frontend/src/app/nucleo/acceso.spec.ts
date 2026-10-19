import { Component, EnvironmentInjector, runInInjectionContext } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Route } from '@angular/router';
import { MODULOS_DEL_SISTEMA } from '../modulos';
import { Modulo, habilitadoPara, modulosPara } from './acceso';
import { SesionActual } from './sesion';

@Component({ template: '' })
class PantallaDePrueba {}

/** Un módulo exclusivo de ADMINISTRADOR. Solo existe en el test: no hay pantalla de usuarios. */
const SOLO_ADMINISTRADOR: Modulo = {
  ruta: 'solo-administrador', titulo: 'Solo administrador', roles: ['ADMINISTRADOR'], componente: PantallaDePrueba,
};
const PARA_TODOS: Modulo = {
  ruta: 'para-todos', titulo: 'Para todos', roles: ['ADMINISTRADOR', 'OPERADOR'], componente: PantallaDePrueba,
};
const DE_PRUEBA = [PARA_TODOS, SOLO_ADMINISTRADOR];

describe('modulosPara · CA1 y CA2', () => {
  it('al administrador le muestra el módulo exclusivo de ADMINISTRADOR', () => {
    expect(modulosPara('ADMINISTRADOR', DE_PRUEBA)).toEqual([PARA_TODOS, SOLO_ADMINISTRADOR]);
  });

  it('al operador no le muestra el módulo exclusivo de ADMINISTRADOR', () => {
    expect(modulosPara('OPERADOR', DE_PRUEBA)).toEqual([PARA_TODOS]);
  });

  it('sin rol no muestra ningún módulo', () => {
    expect(modulosPara(undefined, DE_PRUEBA)).toEqual([]);
  });

  it('los dos roles de la base ven Artículos y Factura de compra, en ese orden', () => {
    for (const rol of ['ADMINISTRADOR', 'OPERADOR']) {
      expect(modulosPara(rol, MODULOS_DEL_SISTEMA).map(m => m.ruta)).toEqual(['articulos', 'factura-compra']);
    }
  });
});

describe('habilitadoPara · la ruta solo entra con sesión y rol habilitado', () => {
  const ruta: Route = {};

  function entra(roles: readonly string[]) {
    const inyector = TestBed.inject(EnvironmentInjector);
    return runInInjectionContext(inyector, () => habilitadoPara(roles)(ruta, []));
  }

  function conRol(rol: string) {
    TestBed.inject(SesionActual).iniciar({ token: 't', nombreUsuario: 'prueba', rol, sucursalId: 1 });
  }

  it('sin sesión no entra', () => {
    expect(entra(['ADMINISTRADOR', 'OPERADOR'])).toBeFalse();
  });

  it('con un rol habilitado entra', () => {
    conRol('OPERADOR');
    expect(entra(['ADMINISTRADOR', 'OPERADOR'])).toBeTrue();
  });

  it('con un rol no habilitado no entra', () => {
    conRol('OPERADOR');
    expect(entra(['ADMINISTRADOR'])).toBeFalse();
  });
});
