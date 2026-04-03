import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { DashboardLayoutComponent } from './layout/dashboard-layout.component';
import { LoginPage } from './pages/login/login.page';
import { PatientsPage } from './pages/patients/patients.page';

export const routes: Routes = [
  { path: 'login', component: LoginPage },
  {
    path: '',
    component: DashboardLayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'patients', pathMatch: 'full' },
      { path: 'patients', component: PatientsPage }
    ]
  },
  { path: '**', redirectTo: '' }
];
