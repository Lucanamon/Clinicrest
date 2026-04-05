import { Component, OnInit, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { PatientService } from '../../patient.service';

@Component({
  selector: 'app-patient-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './patient-form.component.html',
  styleUrl: './patient-form.component.scss'
})
export class PatientFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly isEdit = signal(false);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(200)]],
    lastName: ['', [Validators.required, Validators.maxLength(200)]],
    dateOfBirth: ['', Validators.required],
    gender: ['', [Validators.required, Validators.maxLength(32)]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(32)]]
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit.set(true);
      this.loadPatient(id);
    }
  }

  private loadPatient(id: string): void {
    this.loading.set(true);
    this.loadError.set(null);
    this.patientService.getById(id).subscribe({
      next: (p) => {
        const dob = p.dateOfBirth.slice(0, 10);
        this.form.patchValue({
          firstName: p.firstName,
          lastName: p.lastName,
          dateOfBirth: dob,
          gender: p.gender,
          phoneNumber: p.phoneNumber
        });
        this.loading.set(false);
      },
      error: () => {
        this.loadError.set('Patient not found or could not be loaded.');
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
    const payload = {
      firstName: v.firstName.trim(),
      lastName: v.lastName.trim(),
      dateOfBirth: this.toApiDate(v.dateOfBirth),
      gender: v.gender.trim(),
      phoneNumber: v.phoneNumber.trim()
    };

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.patientService.update(id, payload).subscribe({
        next: () => this.router.navigateByUrl('/patients'),
        error: () => this.saveError.set('Could not save changes. Check your input and try again.')
      });
    } else {
      this.patientService.create(payload).subscribe({
        next: () => this.router.navigateByUrl('/patients'),
        error: () => this.saveError.set('Could not create patient. Check your input and try again.')
      });
    }
  }

  private toApiDate(htmlDate: string): string {
    const d = new Date(htmlDate + 'T00:00:00.000Z');
    return d.toISOString();
  }

  cancel(): void {
    void this.router.navigateByUrl('/patients');
  }
}
