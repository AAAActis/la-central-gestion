import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';

/** 053 · HU-CMP-06 · esqueleto: rojo. */
@Component({
  selector: 'app-vinculacion',
  imports: [FormsModule],
  template: `<form class="filtros" (ngSubmit)="buscar()"><input name="texto" [(ngModel)]="texto" /><button type="submit">Buscar</button></form>`,
})
export class Vinculacion {
  texto = '';
  buscar() {}
}
