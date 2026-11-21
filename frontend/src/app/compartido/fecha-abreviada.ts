import { InjectionToken } from '@angular/core';

/** El reloj de la pantalla: en las pruebas se reemplaza por un día fijo. */
export const RELOJ = new InjectionToken<() => Date>('RELOJ', { providedIn: 'root', factory: () => () => new Date() });

const dos = (n: number) => String(n).padStart(2, '0');

/** aaaa-mm-dd de la fecha LOCAL (toISOString daría la de UTC: de noche, mañana). */
export const hoyIso = (hoy: Date) => `${hoy.getFullYear()}-${dos(hoy.getMonth() + 1)}-${dos(hoy.getDate())}`;

/** aaaa-mm-dd → dd/mm/aaaa. */
export const aTextoFecha = (iso: string) => iso ? iso.split('-').reverse().join('/') : '';

const bisiesto = (a: number) => a % 4 === 0 && (a % 100 !== 0 || a % 400 === 0);
const diasDelMes = (a: number, m: number) => m === 2 ? (bisiesto(a) ? 29 : 28) : [4, 6, 9, 11].includes(m) ? 30 : 31;

/**
 * Fix A: d o dd (día; mes y año actuales), ddmm (año actual), ddmmaaaa, dd/mm (año actual) o dd/mm/aaaa.
 * El separador puede ser /, - o ., el mismo en toda la fecha. Cualquier otra forma es inválida. Vacío es «sin fecha».
 */
export function interpretarFecha(texto: string, hoy: Date): { iso: string } | { error: string } {
  const t = texto.trim();
  if (!t) return { iso: '' };
  const partes = /^(\d{1,2})$/.exec(t) ?? /^(\d{2})(\d{2})(\d{4})?$/.exec(t)
    ?? /^(\d{1,2})([/.-])(\d{1,2})(?:\2(\d{4}))?$/.exec(t)?.filter((_, i) => i !== 2);
  if (!partes) return { error: `«${t}» no es una fecha: escribí d, ddmm, ddmmaaaa, dd/mm o dd/mm/aaaa (con /, - o .).` };
  const d = Number(partes[1]);
  const m = partes[2] ? Number(partes[2]) : hoy.getMonth() + 1;
  const a = partes[3] ? Number(partes[3]) : hoy.getFullYear();
  if (m < 1 || m > 12 || d < 1 || d > diasDelMes(a, m)) return { error: `El ${dos(d)}/${dos(m)}/${a} no existe.` };
  return { iso: `${a}-${dos(m)}-${dos(d)}` };
}
