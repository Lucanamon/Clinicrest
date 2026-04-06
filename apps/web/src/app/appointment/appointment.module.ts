import { NgModule } from '@angular/core';
import { AppointmentFormComponent } from './components/appointment-form/appointment-form.component';
import { AppointmentListComponent } from './components/appointment-list/appointment-list.component';

@NgModule({
  imports: [AppointmentListComponent, AppointmentFormComponent],
  exports: [AppointmentListComponent, AppointmentFormComponent]
})
export class AppointmentModule {}
