/**
 * Variantes de botón de styles.css (SISTEMA-VISUAL.md §7). Sin valores de color (CLAUDE.md,
 * Frontend): lo que se afirma es que el primario se distingue y que comparte los estados de los demás.
 */
describe('Botones · la variante primario (§7)', () => {
  let caja: HTMLElement;

  function boton(clase: string | null, estado: { deshabilitado?: boolean; cargando?: boolean } = {}) {
    const b = document.createElement('button');
    if (clase) b.className = clase;
    b.textContent = 'Ingresar';
    b.disabled = !!(estado.deshabilitado || estado.cargando);
    if (estado.cargando) b.setAttribute('aria-busy', 'true');
    caja.appendChild(b);
    return b;
  }
  const estilo = (b: HTMLElement, pseudo?: string) => getComputedStyle(b, pseudo);

  beforeEach(() => {
    caja = document.createElement('div');
    document.body.appendChild(caja);
  });

  afterEach(() => caja.remove());

  it('se distingue del secundario en el relleno, el borde y el texto', () => {
    const primario = estilo(boton('primario'));
    const secundario = estilo(boton(null));
    expect(primario.backgroundColor).not.toBe(secundario.backgroundColor);
    expect(primario.borderTopColor).not.toBe(secundario.borderTopColor);
    expect(primario.color).not.toBe(secundario.color);
  });

  it('al apoyar el cursor, la capa que entra se distingue del relleno en reposo y de la del secundario', () => {
    const primario = boton('primario');
    expect(estilo(primario, '::before').backgroundColor).not.toBe(estilo(primario).backgroundColor);
    expect(estilo(primario, '::before').backgroundColor).not.toBe(estilo(boton(null), '::before').backgroundColor);
  });

  it('deshabilitado se ve igual que cualquier botón deshabilitado', () => {
    const primario = estilo(boton('primario', { deshabilitado: true }));
    const secundario = estilo(boton(null, { deshabilitado: true }));
    expect(primario.backgroundColor).toBe(secundario.backgroundColor);
    expect(primario.color).toBe(secundario.color);
  });

  it('en carga conserva su variante', () => {
    expect(estilo(boton('primario', { cargando: true })).backgroundColor).toBe(estilo(boton('primario')).backgroundColor);
    expect(estilo(boton('primario', { cargando: true })).backgroundColor).not.toBe(estilo(boton(null, { cargando: true })).backgroundColor);
  });
});

/** Luminancia relativa de un color calculado (WCAG 2.x): se mide el contraste, no se afirma un valor. */
function luminancia(color: string): number {
  const [r, g, b] = (color.match(/[\d.]+/g) ?? []).slice(0, 3).map(Number).map(c => {
    const v = c / 255;
    return v <= 0.03928 ? v / 12.92 : ((v + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}
function contraste(a: string, b: string): number {
  const [claro, oscuro] = [luminancia(a), luminancia(b)].sort((x, y) => y - x);
  return (claro + 0.05) / (oscuro + 0.05);
}
const transparente = (color: string) => color === 'transparent' || /rgba\(.*,\s*0\)$/.test(color);

describe('Botones · las cinco variantes de la §7', () => {
  // `oscuro` va sobre grafito (§7): su relleno es transparente y lo que se lee es el fondo.
  const VARIANTES = [
    { clase: 'primario', fondo: null },
    { clase: 'peligro', fondo: null },
    { clase: 'secundario', fondo: null },
    { clase: 'peligro-borde', fondo: null },
    { clase: 'oscuro', fondo: 'var(--grafito)' },
  ] as const;
  let caja: HTMLElement;

  function medir(clase: string, fondo: string | null) {
    const contenedor = document.createElement('div');
    if (fondo) contenedor.style.background = fondo;
    const b = document.createElement('button');
    b.className = clase;
    b.textContent = 'Acción';
    contenedor.appendChild(b);
    caja.appendChild(contenedor);
    const e = getComputedStyle(b);
    const relleno = transparente(e.backgroundColor) ? getComputedStyle(contenedor).backgroundColor : e.backgroundColor;
    return { relleno, borde: e.borderTopColor, texto: e.color };
  }

  beforeEach(() => {
    caja = document.createElement('div');
    document.body.appendChild(caja);
  });

  afterEach(() => caja.remove());

  for (const { clase, fondo } of VARIANTES) {
    it(`${clase} · se distingue de las otras cuatro y su texto se lee sobre su relleno (AA, 4,5:1)`, () => {
      const propia = medir(clase, fondo);
      for (const otra of VARIANTES.filter(v => v.clase !== clase)) {
        const ajena = medir(otra.clase, otra.fondo);
        expect([propia.relleno, propia.borde, propia.texto]).withContext(`contra ${otra.clase}`)
          .not.toEqual([ajena.relleno, ajena.borde, ajena.texto]);
      }
      // Un texto heredado de la base (--tinta) sobre un relleno oscuro no llega: es el defecto de peligro.
      expect(contraste(propia.texto, propia.relleno)).withContext('contraste texto / relleno').toBeGreaterThanOrEqual(4.5);
      // Su propio color de texto: un button sin clase es secundario (§7), así que solo él lleva el de la base.
      const base = medir('', null).texto;
      if (clase === 'secundario') expect(propia.texto).withContext('texto de la base').toBe(base);
      else expect(propia.texto).withContext('texto propio, no el de la base').not.toBe(base);
    });
  }
});
