# La Central · frontend

Pantallas de artículos (HU-ART-01 alta, HU-ART-02 búsqueda y ficha, HU-ART-03 modificación, códigos, baja y reactivación) y de factura de compra (HU-FCO-01). Angular 20, sin
librería de estado ni framework de UI.

## Cómo levantarla

Hacen falta dos terminales: la API y el frontend.

```bash
# 1 · la API, en el puerto 5181 (perfil http). Necesita la conexión y la clave JWT:
#     ConnectionStrings__Postgres y Jwt__Key en el entorno (ver docs/entorno-local.md).
cd backend
dotnet run --project LaCentral.Api --launch-profile http

# 2 · el frontend, en el puerto 4200 (la primera vez, antes: npm install)
cd frontend
npm start
```

Abrí **http://localhost:4200** y entrá con un usuario de la base (rol ADMINISTRADOR u
OPERADOR).

- `npm start` corre `ng serve` con `proxy.conf.json`: todo `/api` va a
  `http://localhost:5181`. Por eso no hace falta CORS en la API.
- El token vive **solo en memoria**. Si recargás la página, tenés que volver a entrar.

## Qué hay

| Archivo | Qué hace |
|---|---|
| `src/app/sesion.ts` | sesión en memoria y el interceptor que agrega el `Bearer` |
| `src/app/api.ts` | las llamadas a `/api/acceso/login`, `/api/articulos`, `/api/proveedores` y `/api/facturas-compra` |
| `src/app/login.ts` | el login |
| `src/app/articulos.ts` | buscador, lista paginada, ficha con las seis modalidades y su razón, alta, y en la ficha (HU-ART-03): modificar nombre y ubicación, agregar códigos de proveedor (los existentes se conservan), cambiar el código interno y retirar un código (solo ADMINISTRADOR; la API lo decide con el rol de la sesión), dar de baja con motivo y reactivar |
| `src/app/factura-compra.ts` | carga manual de la factura de compra: proveedor activo, número, fechas, percepciones y líneas con destino; al registrar muestra el costo y el stock que quedaron, releídos de la ficha |

La ficha muestra **siempre** las seis modalidades. Si una no tiene precio, muestra la
razón que manda la API (`sin configuración`, `sin costo`, `caso especial no fijado`,
`tasa sin definir`), nunca un cero.
