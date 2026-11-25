import { exactoArticulo, exactoProveedor } from './codigo-exacto';

describe('Código exacto · HU-FCO-06 CA-002, CA-003 y CA-009', () => {
  const p = (codigo: string) => ({ codigo });
  const a = (codigoInterno: string) => ({ codigoInterno });

  it('«35» elige al proveedor 00035 aunque venga detrás de otros que lo contienen', () => {
    expect(exactoProveedor([p('00350'), p('00135'), p('00035')], ' 35 ')).toEqual(p('00035'));
  });

  it('escrito con ceros, elige igual al proveedor guardado sin ellos', () => {
    expect(exactoProveedor([p('35')], '00035')).toEqual(p('35'));
  });

  it('con 035 y 00035 no elige a ninguno', () => {
    expect(exactoProveedor([p('035'), p('00035')], '35')).toBeNull();
  });

  it('sin proveedor exacto no elige', () => {
    expect(exactoProveedor([p('00350')], '35')).toBeNull();
    expect(exactoProveedor([], '35')).toBeNull();
  });

  it('elige el artículo de código interno exacto, sin espacios ni mayúsculas de por medio', () => {
    expect(exactoArticulo([a('A-1000'), a(' A-100 ')], ' a-100 ')).toEqual(a(' A-100 '));
  });

  it('sin código interno exacto no elige artículo', () => {
    expect(exactoArticulo([a('A-1000')], 'A-100')).toBeNull();
  });

  it('en artículos los ceros a la izquierda sí cuentan', () => {
    expect(exactoArticulo([a('00035')], '35')).toBeNull();
  });
});
