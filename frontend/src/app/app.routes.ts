import { Routes } from '@angular/router';
import { Modulo, conSesion, habilitadoPara, modulosPara, rolActual } from './nucleo/acceso';
import { Login } from './pantallas/login/login';

/**
 * Las rutas salen de la lista de módulos: una por módulo, filtrada por rol. Con sesión, una
 * ruta que el rol no tiene lleva a su primer módulo (los dos roles de la base tienen alguno:
 * lo prueba acceso.spec.ts). Sin sesión, cualquier ruta muestra el login.
 */
export function rutasDe(modulos: readonly Modulo[]): Routes {
  return [
    ...modulos.map(m => ({ path: m.ruta, component: m.componente, canMatch: [habilitadoPara(m.roles)] })),
    { path: '', canMatch: [conSesion], children: [{ path: '**', redirectTo: () => '/' + modulosPara(rolActual(), modulos)[0].ruta }] },
    { path: '**', component: Login },
  ];
}
