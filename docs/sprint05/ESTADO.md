# ESTADO · Sprint 5

> Única fuente de verdad del estado del sprint. Se actualiza **antes** de cada push, en el
> mismo PR que el código que describe.

**Sprint 5 · 02/11/2026 → 13/11/2026.** Demo a Sergio: **viernes 06/11/2026**.
Instrucciones de arranque: `docs/sprint05/21-INSTRUCCIONES-CLAUDE-CODE-04-11.md`.
Cierre del Sprint 4 y arrastre: `docs/sprint04/ESTADO.md` (PR #139).

---

## Decisiones (04/11/2026)

| # | Decisión | Motivo |
|---|---|---|
| S5-01 | **`lacentral_demo` se arma con el esquema de `develop`** (Parte A de `base_completa.sql` + 01 a 04). El 07 se le aplica **después**, junto con el código que lo usa. | Que la demo del viernes no dependa del re-scaffold. |
| S5-02 | **`stock.ubicacion_deposito` pasa de `VARCHAR(30)` a `VARCHAR(60)`** en las dos bases (Santi, con el superusuario). Versionado en `08_Ubicacion_Deposito_60.sql`. La validación de la API no cambia. | El 30 del 07 fue un número sin fundamento; la validación ya estaba en 60 y la columna de origen, `articulo.ubicacion_deposito`, era de 60. |
| S5-03 | **La ubicación sale del alta y de la modificación del artículo.** Se carga desde la ficha, **por sucursal**, igual que se ve la cantidad. | El estante se asigna cuando la mercadería llega; en el alta no hay existencia en ninguna sucursal. Copiarla a todas afirmaría que la pieza está en el mismo estante en las dos: la redundancia que N-9 eliminó. |
| S5-04 | **Numeración de scripts:** la normalización llegó como `05_…` y queda **07**. El 05 es `05_rol_tests.sql`; el **06 sigue reservado para C2**. | Primer número libre. |
| S5-05 | **La comparación de esquemas `lacentral_test` ↔ `lacentral` la hace Santi** con el superusuario: el rol de desarrollo no tiene `CONNECT` sobre `lacentral` y no se le va a dar. 04/11: md5 de columnas idéntico. | Seguridad de los datos reales. |

---

## Fases

| Fase | Qué | Rama | PR | Estado |
|---|---|---|---|---|
| a · C3 | `lacentral_demo` creada (owner `lacentral_santi`), con Parte A + 01 a 04, y **sembrada** con `backend/db/demo/sembrar_demo.cs` contra la API de `develop` | `feature/c3-lacentral-demo` | ver abajo | **Hecha 04/11.** Esquema igual al de `develop` (huella comparada). 2 usuarios, 5 proveedores, 30 artículos, 90 códigos, 15 facturas, 180 líneas, 15 fechas. Login de `operador.demo` y búsqueda verificados por HTTP |
| b · re-scaffold | 07 versionado, re-scaffold, arreglos del modelo, ubicación por sucursal (backend y ficha) | `feature/normalizacion-3fn-rescaffold` | ver abajo | **Hecha 04/11.** Sin veredicto de `verificador-tdd` ni `auditor-persistencia` |
| c · tests | Los 7 del esquema y la corrida completa | idem | idem | **Hecha.** Ver «Tests» |
| d · 07 en demo | Aplicar 07 y 08 a `lacentral_demo` | — | — | **Pendiente de confirmación de Santi** (ver PRÓXIMA SESIÓN) |

**C3 (compuerta del Sprint 4): cerrada el 04/11/2026.** La clave de los usuarios de demo no
está en el repo.

### Qué cambió con el modelo nuevo (fase b)

- **`usuario_baja_id` de `cliente` y `usuario` (N-11):** sale de `*_historial_baja`. El cliente
  lee su **última** baja (reactivado la conserva, como motivo y fecha, D10); el usuario, la
  **abierta** (su reactivación la limpia). Sin cambio de comportamiento. `usuario_alta_id`
  salió de las entidades: nadie lo leía.
- **`provincia_id` (N-5):** ningún código leía ni escribía la provincia (las direcciones se
  mapean solo con `Calle`). No hubo JOIN que agregar. La pantalla que la muestre lo va a
  necesitar.
- **`factura_compra_detalle` con clave de tres columnas:** el repositorio solo inserta, no
  busca por clave. La guarda del caso de uso decía «una línea por artículo: es la clave
  primaria»; pasa a **(artículo, sucursal)**. El test `ArticuloRepetidoEnDosLineas_Invalido`
  afirmaba la regla vieja con dos sucursales distintas: pasó a la misma sucursal. El recálculo
  de precios recibe cada artículo una vez.
- **Ubicación (S5-03):** `PUT /api/articulos/{id}/ubicaciones/{sucursalId}` (ADMINISTRADOR y
  OPERADOR, máximo 60, vacío borra; sin fila de stock la crea en cero; dado de baja → 400). La
  ficha trae la ubicación en cada existencia; en la pantalla, la tabla de stock tiene un campo y
  un Guardar por sucursal.
- **`factura_venta.medio_cobro` NOT NULL:** rompía el dato de un test de restricciones únicas.
  Producción no escribe facturas de venta todavía: **la HU de ventas tiene que pedir el medio de
  cobro.**

### Tests

- **Backend: 456** (línea de base 436 − 5 borrados + 25 nuevos). Una sola corrida completa:
  **455/456**, 0 omitidos, 0 warnings, 0 `53300`, 3 min 6 s. El que falló
  (`RestriccionesUnicasPostgreSqlTests.TelefonosYFacturaDeVentaDuplicados_…`) era el
  `medio_cobro` de arriba; arreglado, su clase dio 5/5. **La suite completa no se volvió a
  correr después del arreglo.**
- Borrados (5): los que afirmaban la ubicación en el alta y la modificación; la regla del máximo
  de 60 se volvió a probar en la operación nueva.
- Nuevos (25): 9 del caso de uso de ubicación (Moq), 5 de su persistencia (PostgreSQL, releídos
  con contexto nuevo), 2 HTTP, 2 de la compra con el mismo artículo en dos sucursales (Moq y
  PostgreSQL), y **los 7 del esquema 07** en `EsquemaNormalizacionPostgreSqlTests`: cada FALLA
  verifica el código SQL y el nombre de la restricción.
- Rojo por la razón correcta antes de producción, en commits separados (`f2bb5a9`, `694f53c`).
  Los 7 del esquema nacieron en verde: el 07 ya estaba aplicado.
- **Frontend: 94/94** (88 + 6, primer spec de la pantalla de artículos). Solo Chromium.
- **Mutaciones: no se corrieron.** Falta el veredicto de `verificador-tdd` y de
  `auditor-persistencia`.

---

## Deuda

- **D-S5-01 · `medio_cobro`:** la HU de ventas tiene que pedirlo; la columna es NOT NULL.
- **D-S5-02 · `provincia_id`:** sin JOIN ni combo todavía; aparece cuando una pantalla muestre
  direcciones.
- **D-S5-03 · costo con dos líneas del mismo artículo:** si llegan a precios distintos, el
  costo vigente queda en el de la última línea. No hay regla de negocio que diga otra cosa.
- **D-S5-04 · entorno:** `dotnet-ef` necesita `DOTNET_ROOT=$HOME/.dotnet` y `~/.dotnet/tools`
  en el PATH. No hay `psql` en la máquina. `CHROME_BIN=/usr/bin/chromium`.
- Sigue de antes: D-27a, D-27c, el campo en el `ProblemDetails`, el largo del motivo (200/255),
  `RestablecerContrasenaUseCase.cs:43`.

---

## PRÓXIMA SESIÓN

1. **Decisión pendiente (fase d):** aplicar 07 y 08 a `lacentral_demo`. Con eso, la demo **tiene
   que correr con esta rama** (`develop` deja de funcionar contra esa base, porque lee
   `articulo.ubicacion_deposito`). Si la rama no entra antes del viernes, conviene no aplicarlos
   hasta después de la demo.
2. Veredicto de `verificador-tdd` y `auditor-persistencia` sobre la fase b.
3. Pendientes del 04/11, en este orden: `git merge develop` en la 044 (resolver
   `formulario.ts` sin perder `password` ni `autocompletar`); aviso flotante con foco devuelto y
   Escape.
