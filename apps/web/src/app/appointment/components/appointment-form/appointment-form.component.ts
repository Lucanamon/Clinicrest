import { isPlatformBrowser } from '@angular/common';
import { Component, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AppointmentService } from '../../appointment.service';
import { AuthService } from '../../../services/auth';
import { PatientDto, PatientService } from '../../../patient/patient.service';
import { DoctorListItemDto, UsersService } from '../../../users/users.service';

@Component({
  selector: 'app-appointment-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './appointment-form.component.html',
  styleUrl: './appointment-form.component.scss'
})
export class AppointmentFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly appointmentService = inject(AppointmentService);
  private readonly patientService = inject(PatientService);
  private readonly usersService = inject(UsersService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  readonly auth = inject(AuthService);

  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly isEdit = signal(false);
  readonly patients = signal<PatientDto[]>([]);
  readonly doctors = signal<DoctorListItemDto[]>([]);
  readonly lookupsLoading = signal(true);

  readonly form = this.fb.nonNullable.group({
    patientId: ['', [Validators.required]],
    doctorId: [''],
    appointmentDate: ['', Validators.required],
    status: ['Scheduled', [Validators.required, Validators.maxLength(32)]],
    notes: ['', [Validators.maxLength(2000)]]
  });

  ngOnInit(): void {
    if (this.auth.canSelectAppointmentDoctor()) {
      this.form.controls.doctorId.setValidators([Validators.required]);
    }
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit.set(true);
    }
    if (!isPlatformBrowser(this.platformId)) {
      this.lookupsLoading.set(false);
      return;
    }
    this.loadLookups(id);
  }

  private loadLookups(editId: string | null): void {
    this.lookupsLoading.set(true);
    this.patientService.getPaged({ pageNumber: 1, pageSize: 100 }).subscribe({
      next: (res) => {
        this.patients.set(res.items);
        if (this.auth.canSelectAppointmentDoctor()) {
          this.usersService.getDoctors().subscribe({
            next: (docs) => {
              this.doctors.set(docs);
              this.lookupsLoading.set(false);
              if (editId) {
                this.loadAppointment(editId);
              }
            },
            error: () => {
              this.doctors.set([]);
              this.lookupsLoading.set(false);
              if (editId) {
                this.loadAppointment(editId);
              }
            }
          });
        } else {
          this.lookupsLoading.set(false);
          if (editId) {
            this.loadAppointment(editId);
          }
        }
      },
      error: () => {
        this.patients.set([]);
        this.lookupsLoading.set(false);
      }
    });
  }

  private loadAppointment(id: string): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.appointmentService.getById(id).subscribe({
      next: (a) => {
        this.form.patchValue({
          patientId: a.patientId,
          doctorId: this.auth.canSelectAppointmentDoctor() ? a.doctorId : '',
          appointmentDate: this.toDatetimeLocal(a.appointmentDate),
          status: a.status,
          notes: a.notes ?? ''
        });
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set('Appointment not found or could not be loaded.');
        this.loading.set(false);
      }
    });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saveError.set(null);
    const v = this.form.getRawValue();
    const base = {
      patientId: v.patientId.trim(),
      appointmentDate: this.toApiDateTime(v.appointmentDate),
      status: v.status.trim(),
      notes: v.notes.trim() ? v.notes.trim() : null
    };

    const id = this.route.snapshot.paramMap.get('id');
    if (this.auth.canSelectAppointmentDoctor()) {
      const payload = { ...base, doctorId: v.doctorId.trim() };
      if (id) {
        this.appointmentService.update(id, payload).subscribe({
          next: () => void this.router.navigateByUrl('/request'),
          error: () => this.saveError.set('Could not save changes. Check your input and try again.')
        });
      } else {
        this.appointmentService.create(payload).subscribe({
          next: () => void this.router.navigateByUrl('/request'),
          error: () => this.saveError.set('Could not create appointment. Check your input and try again.')
        });
      }
    } else {
      const payload = { ...base, doctorId: null };
      if (id) {
        this.appointmentService.update(id, payload).subscribe({
          next: () => void this.router.navigateByUrl('/request'),
          error: () => this.saveError.set('Could not save changes. Check your input and try again.')
        });
      } else {
        this.appointmentService.create(payload).subscribe({
          next: () => void this.router.navigateByUrl('/request'),
          error: () => this.saveError.set('Could not create appointment. Check your input and try again.')
        });
      }
    }
  }

  private toDatetimeLocal(iso: string): string {
    const d = new Date(iso);
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
  }

  private toApiDateTime(local: string): string {
    return new Date(local).toISOString();
  }

  cancel(): void {
    void this.router.navigateByUrl('/request');
  }

  showDoctorField(): boolean {
    return this.auth.canSelectAppointmentDoctor();
  }
}
