import { NgModule } from '@angular/core';
import { PatientFormComponent } from './components/patient-form/patient-form.component';
import { PatientListComponent } from './components/patient-list/patient-list.component';

@NgModule({
  imports: [PatientListComponent, PatientFormComponent],
  exports: [PatientListComponent, PatientFormComponent]
})
export class PatientModule {}
