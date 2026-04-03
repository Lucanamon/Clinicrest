import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { ApiService, PatientDto } from '../../services/api';

type PatientStatus = 'stable' | 'critical';

@Component({
  selector: 'app-patients-page',
  standalone: true,
  imports: [NgFor, NgIf],
  templateUrl: './patients.page.html',
  styleUrl: './patients.page.scss'
})
export class PatientsPage implements OnInit {
  private readonly api = inject(ApiService);

  readonly patients = signal<PatientDto[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly totalPatients = computed(() => this.patients().length);

  ngOnInit(): void {
    this.api.getPatients().subscribe({
      next: (patients) => {
        this.patients.set(patients);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to load patients. Please try again.');
        this.loading.set(false);
      }
    });
  }

  getStatus(patient: PatientDto): PatientStatus {
    return patient.age >= 65 ? 'critical' : 'stable';
  }

  getStatusLabel(patient: PatientDto): string {
    return this.getStatus(patient) === 'critical' ? 'Critical' : 'Stable';
  }
}
