# frontend/src/app · dónde va cada cosa

- `app.ts`, `app.config.ts`, `app.routes.ts` · la raíz: el marco común y las rutas.
- `modulos.ts` · la lista de módulos con los roles que los habilitan. De ahí salen el menú y
  las rutas: un módulo nuevo se agrega ahí.
- `nucleo/` · lo que no es pantalla: sesión, cliente de la API, acceso por rol.
- `diseno/` · el marco visual compartido: el menú.
- `compartido/` · componentes que usan varias pantallas (el listado reutilizable de HU-UI-04).
- `pantallas/<pantalla>/` · una carpeta por pantalla, con su `.ts` y su `.spec.ts`.

Tests: `npm test` (Karma + Jasmine, lo que trae Angular). Sin navegador abierto:
`npx ng test --watch=false --browsers=ChromeHeadless`.
