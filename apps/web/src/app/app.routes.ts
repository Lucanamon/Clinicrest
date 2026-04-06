import { Routes } from '@angular/router';
import { adminGuard } from './guards/admin.guard';
import { authGuard } from './guards/auth.guard';
import { AppointmentFormComponent } from './appointment/components/appointment-form/appointment-form.component';
import { AppointmentListComponent } from './appointment/components/appointment-list/appointment-list.component';
import { BacklogFormComponent } from './backlog/components/backlog-form/backlog-form.component';
import { BacklogListComponent } from './backlog/components/backlog-list/backlog-list.component';
import { DashboardLayoutComponent } from './layout/dashboard-layout.component';
import { PatientFormComponent } from './patient/components/patient-form/patient-form.component';
import { PatientListComponent } from './patient/components/patient-list/patient-list.component';
import { AppointmentsPage } from './pages/appointments/appointments.page';
import { BacklogPage } from './pages/backlog/backlog.page';
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
          { path: 'new', component: PatientFormComponent, canActivate: [adminGuard] },
          { path: ':id/edit', component: PatientFormComponent, canActivate: [adminGuard] }
        ]
      },
      {
        path: 'appointments',
        component: AppointmentsPage,
        children: [
          { path: '', component: AppointmentListComponent },
          { path: 'new', component: AppointmentFormComponent },
          { path: ':id/edit', component: AppointmentFormComponent }
        ]
      },
      {
        path: 'backlog',
        component: BacklogPage,
        children: [
          { path: '', component: BacklogListComponent },
          { path: 'new', component: BacklogFormComponent, canActivate: [adminGuard] },
          { path: ':id/edit', component: BacklogFormComponent }
        ]
      }
    ]
  },
  { path: '**', redirectTo: '' }
];
