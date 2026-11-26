import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

/** 050 · HU-UI-12 · esqueleto: rojo. */
@Component({
  selector: 'app-comparador',
  imports: [FormsModule],
  template: `<form class="filtros" (ngSubmit)="buscar()"><input name="texto" [(ngModel)]="texto" /><button type="submit">Buscar</button></form>`,
})
export class Comparador {
  texto = '';
  buscar() {}
}
