import { InjectionToken, Type, inject } from '@angular/core';
import { CanMatchFn } from '@angular/router';
import { SesionActual } from './sesion';

/** Un módulo de la aplicación: su ruta, su nombre en el menú y los roles que lo habilitan. */
export interface Modulo {
  ruta: string;
  titulo: string;
  roles: readonly string[];
  componente: Type<unknown>;
}

/** La lista de la que salen el menú y las rutas. Se provee en app.config. */
export const MODULOS = new InjectionToken<readonly Modulo[]>('MODULOS');

/** El rol de la sesión, o '' si no hay sesión: '' no habilita ningún módulo. */
export function rolActual(): string {
  return inject(SesionActual).sesion()?.rol ?? '';
}

/** Los módulos que el rol tiene habilitados, en el orden de la lista. */
export function modulosPara(rol: string, modulos: readonly Modulo[]): Modulo[] {
  return modulos.filter(m => m.roles.includes(rol));
}

/**
 * Deja entrar a la ruta solo con sesión y con un rol habilitado. Es para no mostrar lo que el
 * rol no usa: ocultar no es autorizar, los permisos los sigue decidiendo la API.
 */
export function habilitadoPara(roles: readonly string[]): CanMatchFn {
  return () => roles.includes(rolActual());
}
