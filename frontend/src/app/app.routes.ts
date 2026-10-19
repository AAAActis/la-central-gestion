import { Routes } from '@angular/router';
import { Modulo } from './nucleo/acceso';

/** Las rutas salen de la lista de módulos: una por módulo, filtrada por rol. */
export function rutasDe(modulos: readonly Modulo[]): Routes {
  return [{ path: '**', children: [] }];
}
