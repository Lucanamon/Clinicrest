import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-backlog-page',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />'
})
export class BacklogPage {}
