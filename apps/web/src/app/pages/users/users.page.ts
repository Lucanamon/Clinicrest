import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-users-page',
  standalone: true,
  imports: [RouterOutlet],
  template: '<router-outlet />'
})
export class UsersPage {}
