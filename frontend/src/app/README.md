# frontend/src/app · dónde va cada cosa

- `app.ts`, `app.config.ts`, `app.routes.ts` · la raíz: el marco común y las rutas.
- `modulos.ts` · la lista de módulos con los roles que los habilitan. De ahí salen el menú y
  las rutas: un módulo nuevo se agrega ahí.
- `nucleo/` · lo que no es pantalla: sesión, cliente de la API, acceso por rol.
- `diseno/` · el marco visual compartido: el menú y la marca (`marca.ts`, provisoria).
- `compartido/` · componentes con comportamiento que usan varias pantallas: botón en carga,
  modal y aviso flotante (después, el listado de HU-UI-04). Lo puramente visual (avisos,
  chips, etiquetas, tarjeta, tabla) son clases globales de `src/styles.css`.
- `pantallas/<pantalla>/` · una carpeta por pantalla, con su `.ts` y su `.spec.ts`.

Tests: `npm test` (Karma + Jasmine, lo que trae Angular). Sin navegador abierto:
`npx ng test --watch=false --browsers=ChromeHeadless`.
