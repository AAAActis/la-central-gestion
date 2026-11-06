# Guion de la demo · 06/11/2026

Ensayado de punta a punta el 05/11/2026 por el navegador (Chromium, los pasos 1 a 9; el 7 se dejó otra vez en `Estante A3`) sobre `develop` (`dd22a7d`: #141, #144 y #145
adentro) contra `lacentral_demo`, con los scripts 07 y 08 aplicados. Cada artículo, código y
número de este guion sale de ese ensayo: no hay que buscar nada mientras el dueño mira.

## Antes de empezar (10 minutos antes)

1. `scripts/probar-demo.sh` desde la raíz del repo (ver `COMO-PROBARLO.md`). Está listo cuando
   la consola muestra `Now listening on: http://localhost:5181` y `Local: http://localhost:4200/`.
2. Abrí `http://localhost:4200` y entrá una vez con `operador.demo`, para comprobar que la clave
   anda. Si no anda, restablecela (sección de `COMO-PROBARLO.md`). Después tocá **Salir**.
3. Ventana del navegador maximizada, zoom al 100 %.

## Qué se muestra, en orden

| # | Usuario | Qué se hace | Qué se ve | Qué se cuenta |
|---|---|---|---|---|
| 1 | `operador.demo` | Entrar | Arriba a la derecha: `operador.demo (OPERADOR)`. Pestañas **Artículos** y **Factura de compra** | Cada usuario tiene un rol y una sucursal. El operador es de San Vicente |
| 2 | `operador.demo` | Buscar `amortiguador` | 2 artículos: `LC-000020` (delantero) y `LC-000021` (trasero), con los códigos de cada proveedor | Se busca por nombre, por código interno o por código de proveedor |
| 3 | `operador.demo` | Buscar `AC-02877` | Solo `LC-000021` | Es el código con el que lo vende Autopartes del Centro: el mostrador encuentra la pieza con el código que trae el cliente o la factura |
| 4 | `operador.demo` | Buscar `filtro * aceite` | 6 filtros de aceite | El `*` busca alternativas |
| 5 | `operador.demo` | Buscar `LC-000021` → **Ver ficha** | Costo vigente **$ 80.204,04**. Lista **$ 120.306,06** (costo +50 %). Efectivo / transferencia **$ 108.275,45** (lista −10 %). Costo **$ 80.204,04**. Tarjeta 3 y 6 cuotas: *tasa sin definir*. Caso especial: *no fijado* | Los montos se ven en formato argentino (punto de miles, coma decimal), desde el #149. Los precios salen del costo de la última compra. **Las tasas de tarjeta las tiene que definir Sergio**: el sistema no inventa un número (D1) |
| 6 | `operador.demo` | En la misma ficha, bajar a **Códigos de proveedor** y **Stock** | 4 códigos (`AC-02877`, `RM03886`, `CRS/04895`, `FEC06913`). Stock: Fragueiro **54**, San Vicente **0**, total **54** | Un artículo, cuatro proveedores, cada uno con su código. La existencia se ve por sucursal |
| 7 | `operador.demo` | Buscar `amortiguador` → **Ver ficha** de `LC-000020`. En la fila de **San Vicente** se ve `Estante A3`: cambiarlo a `Estante A4` → **Guardar** | La ficha se recarga con `Estante A4`. Stock: Fragueiro **19**, San Vicente **16** | La ubicación en el depósito es **por sucursal**: la misma pieza está en estantes distintos en cada local |
| 8 | `operador.demo` | En la ficha, mirar **Código interno** en Modificar | Campo deshabilitado, con «solo un administrador lo cambia» | Permisos por rol |
| 9 | `admin.demo` | **Salir**, entrar como `admin.demo`, `LC-000021` → **Ver ficha** | `admin.demo (ADMINISTRADOR)`. El código interno ya se puede editar | **Solo mostrarlo, no guardar.** El cambio de código interno no se ensayó |

## Qué NO se muestra, y por qué

- **El historial de compras del artículo.** Los datos están en la base: `LC-000021` tiene 8
  compras a 4 proveedores entre el 06/08 y el 26/10, de $ 64.385,30 a $ 83.318,76. Pero ninguna
  pantalla ni endpoint los muestra todavía: es la HU-ART-06, pendiente. Lo único que se ve en la
  ficha es el resultado, el costo vigente de la última compra (Frenos y Embragues, 26/10). Si el
  dueño lo pide, se dice así.
- **El comparador de proveedores y los reportes.** No existen todavía.
- **Las diferencias visuales de la Fase 2.** Esperan a que Javi las priorice. Una de ellas se ve
  en la pantalla de entrada: el fondo hexagonal no carga (`/fondo-hex.jpg` da 404) y queda el
  color liso de respaldo.
- **Registrar una factura de compra.** La pantalla existe, pero no se ensayó contra la demo. No se
  muestra en vivo.
- **Clientes y proveedores.** No tienen pantalla: es la 045, parada hasta después de la demo
  (S5-07 del `ESTADO.md`).

## Si algo falla en vivo

- **La ficha da error:** el esquema de la demo no coincide con el código. Ver «El esquema de la
  demo y el código tienen que coincidir» en `COMO-PROBARLO.md`.
- **No entra nadie:** restablecer la clave (`COMO-PROBARLO.md`). Tarda un minuto.
- **Reiniciaste la API:** la clave JWT cambia en cada arranque, así que hay que volver a entrar.
