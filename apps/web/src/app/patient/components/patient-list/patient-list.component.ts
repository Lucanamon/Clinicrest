import { Component, OnInit, inject, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { PatientDto, PatientService } from '../../patient.service';

@Component({
  selector: 'app-patient-list',
  standalone: true,
  imports: [NgFor, NgIf, RouterLink],
  templateUrl: './patient-list.component.html',
  styleUrl: './patient-list.component.scss'
})
export class PatientListComponent implements OnInit {
  private readonly patientService = inject(PatientService);

  readonly patients = signal<PatientDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.loadPatients();
  }

  loadPatients(): void {
    this.loading.set(true);
    this.error.set(null);
    this.patientService.getAll().subscribe({
      next: (rows) => {
        this.patients.set(rows);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load patients. Please try again.');
        this.loading.set(false);
      }
    });
  }

  confirmDelete(patient: PatientDto): void {
    const label = `${patient.firstName} ${patient.lastName}`.trim();
    const ok = window.confirm(`Delete patient "${label}"? This cannot be undone.`);
    if (!ok) {
      return;
    }

    this.patientService.delete(patient.id).subscribe({
      next: () => this.loadPatients(),
      error: () => this.error.set('Delete failed. Please try again.')
    });
  }

  formatDate(iso: string): string {
    const d = new Date(iso);
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(d);
  }
}
