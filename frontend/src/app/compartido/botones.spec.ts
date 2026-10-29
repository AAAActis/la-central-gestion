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
