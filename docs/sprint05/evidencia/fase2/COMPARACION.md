# Fase 2 visual · acceso y marco · comparación con las referencias 01 y 02

Capturas del 06/11/2026 sobre `feature/fase2-visual-acceso-marco`, con `ng serve` y Chrome headless
(CDP). Sin backend: la sesión del marco es una sesión de prueba en `sessionStorage`
(`admin.demo`, ADMINISTRADOR), así que las pantallas no muestran datos.

**Desplazamiento horizontal**, medido como `scrollWidth <= clientWidth` del documento:

| Pantalla | 1024 | 768 | 480 |
|---|---|---|---|
| Acceso | 1024 / 1024 | 768 / 768 | 480 / 480 |
| Artículos | 1024 / 1024 | 768 / 768 | 480 / 480 |
| Factura de compra | 1024 / 1024 | 768 / 768 | 480 / 480 |

Ninguna desborda.

## Contra la referencia 01 (acceso)

| Punto | Referencia | Ahora |
|---|---|---|
| Título | «Sistema de gestión» + «Ingresar» | Igual (`01-acceso-*.png`) |
| Bajada | «Ingresá tus datos para comenzar a trabajar.» | Igual |
| Pie | «Acceso para personal autorizado» bajo una línea; en el panel, «La Central Repuestos» | Igual |
| Panel | «Todo en orden. Listos para seguir.» y «Gestión comercial de La Central Repuestos.» | Igual |
| Botón | Primario, ancho completo, flecha a la izquierda | Igual |

## Contra la referencia 02 (marco)

| Punto | Referencia | Ahora |
|---|---|---|
| Título | «Artículos» (h1) | Igual (`02-articulos-*.png`) |
| Bajada | «Buscá repuestos por nombre o código y consultá su ficha.» | Igual |
| Usuario | Nombre arriba, rol abajo («Administrador») | Igual |
| Pie | Claro, texto chico a la izquierda | Igual en estilo; el texto es solo «La Central Repuestos» (ver abajo) |

## Diferencias que quedan, a propósito

- **Fuera de alcance (instrucción del 06/11):** el logo con la C separada, las migas y el filete
  abajo en la tarjeta del acceso. El filete del acceso sigue al costado.
- **Fondo hexagonal:** la captura muestra el color de respaldo; `fondo-hex.jpg` espera la
  confirmación de la licencia.
- **Pie del marco:** la referencia dice «La Central Repuestos · Propuesta visual» y «Datos ficticios
  para revisión». Son rótulos del prototipo, y el sistema trabaja con datos reales: no se copiaron.
  Queda como pregunta.
- **Clientes y Proveedores** no tienen referencia: llevan el título del módulo y no llevan bajada.
- **Factura de compra:** la tarjeta sigue titulada «Factura de compra» (en la referencia 09 dice
  «Proveedor»), así que el título se repite. No está entre los cinco puntos.
