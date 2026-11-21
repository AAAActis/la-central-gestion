import { aTextoFecha, hoyIso, interpretarFecha } from './fecha-abreviada';

/** Fix A (cierre del Sprint 6): fechas abreviadas, con hoy = 19/11/2026. */
describe('interpretarFecha', () => {
  const hoy = new Date(2026, 10, 19, 23, 30); // de noche: en UTC ya es 20/11

  const validas: [string, string][] = [
    ['5', '2026-11-05'],
    ['19', '2026-11-19'],
    ['2010', '2026-10-20'],
    ['20/10', '2026-10-20'],
    ['5/3', '2026-03-05'],
    ['20/10/2025', '2025-10-20'],
    ['29/02/2028', '2028-02-29'],
    ['30', '2026-11-30'],
    ['30/04', '2026-04-30'],
    ['31/10', '2026-10-31'],
    ['15/01', '2026-01-15'], // año actual: no se adivina el siguiente
    [' 20/10 ', '2026-10-20'],
  ];
  for (const [texto, iso] of validas) {
    it(`«${texto}» es ${iso}`, () => expect(interpretarFecha(texto, hoy)).toEqual({ iso }));
  }

  const inexistentes = ['31', '31/04', '31/02', '29/02', '29/02/2027', '00/10', '0', '20/13', '3200'];
  for (const texto of inexistentes) {
    it(`«${texto}» es una fecha que no existe`, () =>
      expect(interpretarFecha(texto, hoy)).toEqual({ error: jasmine.stringContaining('no existe') }));
  }

  const invalidas = ['123', '201026', '20102026', '20/10/26', '20-10', '20.10.2026', '20/10/2026/1', 'hoy', '1a'];
  for (const texto of invalidas) {
    it(`«${texto}» no es un formato aceptado`, () =>
      expect(interpretarFecha(texto, hoy)).toEqual({ error: jasmine.stringContaining('dd/mm/aaaa') }));
  }

  it('vacío es sin fecha', () => expect(interpretarFecha('  ', hoy)).toEqual({ iso: '' }));

  it('cambio de año: en enero, el día corto es de enero del año nuevo', () =>
    expect(interpretarFecha('5', new Date(2027, 0, 2))).toEqual({ iso: '2027-01-05' }));

  it('hoy y el texto salen de la fecha local, no de UTC', () => {
    expect(hoyIso(hoy)).toBe('2026-11-19');
    expect(aTextoFecha('2026-03-05')).toBe('05/03/2026');
    expect(aTextoFecha('')).toBe('');
  });
});
