import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { DashboardLayoutComponent } from './layout/dashboard-layout.component';
import { PatientFormComponent } from './patient/components/patient-form/patient-form.component';
import { PatientListComponent } from './patient/components/patient-list/patient-list.component';
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
      {
        path: 'patients',
        component: PatientsPage,
        children: [
          { path: '', component: PatientListComponent },
          { path: 'new', component: PatientFormComponent },
          { path: ':id/edit', component: PatientFormComponent }
        ]
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
