import { NgModule } from '@angular/core';
import { BacklogFormComponent } from './components/backlog-form/backlog-form.component';
import { BacklogListComponent } from './components/backlog-list/backlog-list.component';

@NgModule({
  imports: [BacklogListComponent, BacklogFormComponent],
  exports: [BacklogListComponent, BacklogFormComponent]
})
export class BacklogModule {}
