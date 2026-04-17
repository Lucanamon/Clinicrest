import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { bookingInternalGuard } from './guards/booking-internal.guard';
import { rootRedirectGuard } from './guards/root-redirect.guard';
import { AppointmentFormComponent } from './appointment/components/appointment-form/appointment-form.component';
import { AppointmentListComponent } from './appointment/components/appointment-list/appointment-list.component';
import { BacklogFormComponent } from './backlog/components/backlog-form/backlog-form.component';
import { BacklogListComponent } from './backlog/components/backlog-list/backlog-list.component';
import { DashboardLayoutComponent } from './layout/dashboard-layout.component';
import { PatientDetailComponent } from './patient/components/patient-detail/patient-detail.component';
import { PatientFormComponent } from './patient/components/patient-form/patient-form.component';
import { PatientListComponent } from './patient/components/patient-list/patient-list.component';
import { AppointmentsPage } from './pages/appointments/appointments.page';
import { BacklogPage } from './pages/backlog/backlog.page';
import { LoginPage } from './pages/login/login.page';
import { PatientsPage } from './pages/patients/patients.page';
import { AuditLogPage } from './pages/audit-log/audit-log.page';
import { UsersPage } from './pages/users/users.page';
import { PatientsReportComponent } from './pages/patients/patients-report/patients-report.component';
import { BookingPage } from './pages/booking/booking.page';
import { RegisterPage } from './pages/register/register.page';
import { ProfileComponent } from './profile/profile.component';
import { UserFormComponent } from './users/components/user-form/user-form.component';
import { UserListComponent } from './users/components/user-list/user-list.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', canActivate: [rootRedirectGuard], children: [] },
  { path: 'login', component: LoginPage },
  { path: 'register', component: RegisterPage },
  {
    path: '',
    component: DashboardLayoutComponent,
    children: [
      { path: 'request', component: RegisterPage },
      { path: 'booking', component: BookingPage, canActivate: [bookingInternalGuard] },
    ]
  },
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
          { path: 'report', component: PatientsReportComponent, canActivate: [authGuard] },
          { path: ':id', component: PatientDetailComponent }
        ]
      },
      {
        path: 'appointments',
        component: AppointmentsPage,
        children: [
          { path: '', component: AppointmentListComponent },
          { path: 'new', component: AppointmentFormComponent },
          { path: ':id/edit', component: AppointmentFormComponent },
          { path: ':id', component: AppointmentFormComponent }
        ]
      },
      {
        path: 'backlog',
        component: BacklogPage,
        children: [
          { path: '', component: BacklogListComponent },
          { path: 'new', component: BacklogFormComponent },
          { path: ':id/edit', component: BacklogFormComponent },
          { path: ':id', component: BacklogFormComponent }
        ]
      },
      {
        path: 'users',
        component: UsersPage,
        canActivate: [authGuard],
        children: [
          { path: '', component: UserListComponent },
          { path: 'new', component: UserFormComponent }
        ]
      },
      {
        path: 'profile',
        component: ProfileComponent,
        canActivate: [authGuard]
      },
      {
        path: 'audit-logs',
        component: AuditLogPage,
        canActivate: [authGuard]
      }
    ]
  },
  { path: '**', redirectTo: '/login' }
];
