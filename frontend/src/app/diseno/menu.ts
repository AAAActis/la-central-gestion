import { Component } from '@angular/core';

/** El menú de módulos: solo los que el rol de la sesión tiene habilitados. */
@Component({
  selector: 'app-menu',
  template: `<nav aria-label="Módulos"></nav>`,
})
export class Menu {}
