import { Component, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { PatientService } from '../../patient.service';

@Component({
  selector: 'app-patient-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './patient-form.component.html',
  styleUrl: './patient-form.component.scss'
})
export class PatientFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly router = inject(Router);

  readonly saveError = signal<string | null>(null);

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(200)]],
    lastName: ['', [Validators.required, Validators.maxLength(200)]],
    dateOfBirth: ['', Validators.required],
    gender: ['', [Validators.required, Validators.maxLength(32)]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(32)]],
    underlyingDisease: ['', Validators.maxLength(1000)]
  });

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
      phoneNumber: v.phoneNumber.trim(),
      underlyingDisease: v.underlyingDisease.trim() || null
    };

    this.patientService.create(payload).subscribe({
      next: () => this.router.navigateByUrl('/patients'),
      error: () => this.saveError.set('Could not create patient. Check your input and try again.')
    });
  }

  private toApiDate(htmlDate: string): string {
    const d = new Date(htmlDate + 'T00:00:00.000Z');
    return d.toISOString();
  }

  cancel(): void {
    void this.router.navigateByUrl('/patients');
  }
}
