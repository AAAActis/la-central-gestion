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
    ['29/02/2000', '2000-02-29'], // divisible por 400: bisiesto
    ['30', '2026-11-30'],
    ['30/04', '2026-04-30'],
    ['31/10', '2026-10-31'],
    ['15/01', '2026-01-15'], // año actual: no se adivina el siguiente
    [' 20/10 ', '2026-10-20'],
    // Respuesta de Javi (21/11): también - y . como separadores, y ddmmaaaa.
    ['20-10', '2026-10-20'],
    ['20.10', '2026-10-20'],
    ['5-3-2025', '2025-03-05'],
    ['20.10.2026', '2026-10-20'],
    ['20102026', '2026-10-20'],
    ['29022028', '2028-02-29'],
  ];
  for (const [texto, iso] of validas) {
    it(`«${texto}» es ${iso}`, () => expect(interpretarFecha(texto, hoy)).toEqual({ iso }));
  }

  const inexistentes = ['31', '31/04', '31/02', '29/02', '29/02/2027', '00/10', '0', '20/13', '3200', '29/02/2100', '29022027', '31-04', '00.10.2026'];
  for (const texto of inexistentes) {
    it(`«${texto}» es una fecha que no existe`, () =>
      expect(interpretarFecha(texto, hoy)).toEqual({ error: jasmine.stringContaining('no existe') }));
  }

  const invalidas = ['123', '201026', '2010202', '201020261', '20/10/26', '20-10/2026', '20.10-2026', '20/10.2026', '20,10', '20/10/2026/1', 'hoy', '1a'];
  for (const texto of invalidas) {
    it(`«${texto}» no es un formato aceptado`, () =>
      expect(interpretarFecha(texto, hoy)).toEqual({ error: jasmine.stringContaining('dd/mm/aaaa') }));
  }

  const fueraDeRango = ['20/10/0000', '20/10/0999', '20/10/1999', '20/10/2101', '20102101', '29/02/1900'];
  for (const texto of fueraDeRango) {
    it(`«${texto}» tiene el año fuera de 2000 a 2100`, () =>
      expect(interpretarFecha(texto, hoy)).toEqual({ error: 'El año tiene que estar entre 2000 y 2100.' }));
  }

  it('los años de borde, 2000 y 2100, se aceptan con el ISO de cuatro dígitos', () => {
    expect(interpretarFecha('20/10/2000', hoy)).toEqual({ iso: '2000-10-20' });
    expect(interpretarFecha('20/10/2100', hoy)).toEqual({ iso: '2100-10-20' });
  });

  it('29/02/2000 existe (siglo bisiesto) y 29/02/2100 no', () => {
    expect(interpretarFecha('29/02/2000', hoy)).toEqual({ iso: '2000-02-29' });
    expect(interpretarFecha('29/02/2100', hoy)).toEqual({ error: jasmine.stringContaining('no existe') });
  });

  it('vacío es sin fecha', () => expect(interpretarFecha('  ', hoy)).toEqual({ iso: '' }));

  it('cambio de año: en enero, el día corto es de enero del año nuevo', () =>
    expect(interpretarFecha('5', new Date(2027, 0, 2))).toEqual({ iso: '2027-01-05' }));

  it('hoy y el texto salen de la fecha local, no de UTC', () => {
    // Una de las dos cruza el día en UTC en cualquier huso distinto de UTC+0.
    expect(hoyIso(hoy)).toBe('2026-11-19');
    expect(hoyIso(new Date(2026, 10, 19, 0, 30))).toBe('2026-11-19');
    expect(aTextoFecha('2026-03-05')).toBe('05/03/2026');
    expect(aTextoFecha('')).toBe('');
  });
});
