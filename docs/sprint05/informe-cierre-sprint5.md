# Informe de cierre · Sprint 5

Fecha: 2026-11-09. Lo escribió un agente, sin supervisión en vivo (instrucción en `BITACORA.md`,
bloque «09/11/2026 08:32»). **El sprint no está cerrado:** este informe deja el estado con el que
se cierra cuando se cumplan las dos condiciones de la §1 de `CLAUDE.md`.

Base:
- `origin/develop` = `144c325` (Merge #153). No compila el frontend sin #154.
- Línea de base de cierre: la rama local `prueba/cierre-s5` = `0bd5b67`, que es `develop` más
  #154, #155, #156 y #158. Los cuatro merges entraron sin conflictos.

Fuentes: `git log --first-parent origin/develop`, la API pública de GitHub (09/11, sin
autenticar) y las corridas de este cierre. Los SP salen del tablero del 05/11 en `ESTADO.md`:
GitHub no los tiene. Ningún número de este informe es una estimación.

---

## 1 · Entregable

> Al cerrar este sprint, una persona puede entrar al sistema con cualquiera de los dos roles y
> buscar artículos, ver en su ficha la existencia y la ubicación por sucursal y el historial de
> compras, darlos de alta, de baja con confirmación y reactivarlos, gestionar clientes y
> proveedores, y registrar una factura de compra, con montos y cantidades en formato argentino,
> sobre `lacentral_demo`.

| Condición | Estado |
|---|---|
| 1 · Recorrido en el navegador contra `lacentral_demo` | **Pendiente.** El recorrido por script de este cierre (C3) no se pudo hacer: en la máquina no están `LACENTRAL_DEMO_PG` ni `LACENTRAL_DEMO_CLAVE`, y las credenciales no se piden |
| 2 · El dueño lo usó | **Pendiente.** No hay registro en el repo de que Sergio haya usado el sistema. La demo del 06/11 estaba agendada (`GUION-DEMO.md`) y su resultado no quedó escrito |

El detalle parte por parte está en `ESTADO.md`, sección «CIERRE DEL SPRINT 5».

---

## 2 · Qué entró

### 2.1 · Las HU del tablero

| Issue | HU | SP | PR | Mergeado | Verificadores (rondas hasta APTO) | Entregada |
|---|---|---|---|---|---|---|
| #43 | 042 · HU-UI-04 · listado reutilizable | 5 | #145 | 05/11 | — (no registrado en el ESTADO) | Sí · issue cerrado |
| #148 | Formato es-AR | 1 | #149 | 06/11 | verificador r1; auditor no aplica | Sí · la deuda (porcentaje y total) va en #156, abierto |
| #147 | Paginación de clientes y proveedores | 2 | #150 | 06/11 | verificador r1; auditor r3 | Sí |
| #46 | 045 · HU-UI-07 · clientes y proveedores | 3 | #151 | 06/11 | verificador r2; auditor r1 | Sí · el motivo de baja del cliente va en #158, abierto |
| #23 | 016 · HU-ART-06 · historial de compras | 5 | #152 | 06/11 | verificador r3; auditor r1 | **Parcial:** CA-003 y CA-004 |
| #21 | 014 · HU-ART-04 · baja de artículo | 3 | #153 | 06/11 | verificador r1; auditor r2 | **Parcial:** CA-003 |

### 2.2 · El resto de lo mergeado a `develop` en el sprint

Por `git log --first-parent origin/develop`, del 04/11 al 06/11: #132 (referencias de diseño),
#138 (HU-UI-03, control de sesión), #141 (normalización a 3FN y re-scaffold), #139 (cierre del
Sprint 4), #143 (`COMO-PROBARLO.md`), #140 (`lacentral_demo`), #144 (HU-UI-06, confirmación con
motivo), #142 y #146 (bitácora y guion de la demo). Quince merges en total, sumando los seis de
2.1.

### 2.3 · Abiertos al cierre, sin mergear

| PR | Qué | Verificadores | Depende de |
|---|---|---|---|
| #154 | Arreglo del merge de #152 y #153 en `develop` | verificador r1 | — |
| #155 | Fase 2 visual del acceso y el marco (Refs #42) | verificador r2 | #154 |
| #156 | es-AR: porcentaje y total del listado (Refs #148) | verificador r2 | #154 |
| #157 | Preguntas para Sergio, ESTADO y bitácora | verificador r1 (solo docs) | #154 |
| #158 | Motivo de baja en la ficha de cliente (Refs #151) | verificador r2; auditor r1 | #154 |
| este | Cierre del Sprint 5 (documentación) | ver el cuerpo del PR | #154 y #157 |

---

## 3 · Métricas

| Métrica | Valor | Fuente |
|---|---|---|
| SP comprometidos | 19 (seis issues) | tablero del 05/11 |
| SP entregados completos | 11 (#43, #148, #147, #46) | GitHub + criterios |
| SP mergeados con CA pendientes | 8 (#23, #21) | ídem |
| PR mergeados a `develop` en el sprint | 15 | `git log --first-parent` |
| PR abiertos al cierre | 5 (#154 a #158) + este | API de GitHub |
| Backend sobre `0bd5b67` | `Correctas! - Con error:     0, Superado:   497, Omitido:     0, Total:   497, Duración: 11 m 12 s - LaCentral.Tests.dll (net10.0)` | corrida de este cierre |
| Frontend sobre `0bd5b67` | `TOTAL: 167 SUCCESS` | corrida de este cierre |
| Rondas NO APTO del `verificador-tdd` | 6: #151 (1), #152 (2), #155 (1), #156 (1), #158 (1) | tablas 2.1 y 2.3 |
| Rondas NO APTO del `auditor-persistencia` | 3: #150 (2), #153 (1) | ídem |

Nota sobre las dos últimas filas: una «rN» cuenta N − 1 rondas NO APTO, y «r1» quiere decir que dio
APTO a la primera. El ESTADO no registra las rondas de #145.

---

## 4 · Arrastre al Sprint 6

- **014 · CA-003** (#21, sigue abierto): la baja de un artículo con existencia por error de
  stock. Necesita `articulo_historial_baja.error_de_stock` (script 09). Es cambio de esquema: lo
  decide el equipo. Pregunta 2 de `PREGUNTAS-SERGIO.md`.
- **016 · CA-003 y CA-004** (#23, sigue abierto): dependen de la anulación de compras (028, #9,
  backlog).

---

## 5 · Decisiones del sprint

Las de la tabla «Decisiones» de `ESTADO.md` (S5-01 a S5-08) y las de la deuda D-S5-01 a D-S5-09.
Las que quedan para decidir:

- `error_de_stock` (014 CA-003).
- D-S5-02, la provincia en las direcciones.
- Las siete preguntas de `PREGUNTAS-SERGIO.md`. Ninguna está respondida.

---

## 6 · Lecciones

1. **Mergear varias ramas verdes no garantiza un `develop` verde.** #152 y #153 pasaban cada una
   por separado, y juntas rompieron el frontend de `develop` (`144c325`). Lo arregla #154. La rama
   local de integración de este cierre (`prueba/cierre-s5`) prueba los merges pendientes antes de
   hacerlos. Conviene repetirla antes de cada tanda de merges.
2. **Una deuda registrada no prueba que algo falte.** D-S5-09 decía que faltaba el test de API de
   OPERADOR en la ubicación. El test existía desde cuatro horas antes (`f2bb5a9`). Antes de
   escribir un test «que falta», hay que buscarlo.
3. **Una mutación sobre el rastro de D10 sobrevivió al primer test del #158.** El test leía la
   ficha del dado de baja, pero no la del reactivado. Los estados intermedios de un ciclo también
   se afirman.
4. **El recorrido del entregable depende del entorno de quien lo corre.** Sin las variables de
   la demo, no hay condición 1. Tienen que estar antes de la sesión de cierre, no durante.
5. **La condición 2 no se registró.** La demo a Sergio estaba agendada para el 06/11 y no quedó
   escrito lo que dijo. Lo que diga el dueño se anota el mismo día.
