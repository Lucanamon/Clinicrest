import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
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
  readonly isSubmitting = signal(false);
  readonly maxDateOfBirth = new Date().toISOString().split('T')[0];

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(200)]],
    lastName: ['', [Validators.required, Validators.maxLength(200)]],
    dateOfBirth: ['', Validators.required],
    gender: ['', [Validators.required, Validators.maxLength(32)]],
    phoneNumber: ['', [Validators.required, Validators.maxLength(32), Validators.pattern(/^\d+$/)]],
    underlyingDisease: ['', Validators.maxLength(1000)]
  });

  constructor() {
    const prefill = this.readPrefillFromNavigationState();
    if (prefill) {
      this.form.patchValue({
        firstName: prefill.firstName,
        lastName: prefill.lastName,
        phoneNumber: prefill.phoneNumber
      });
    }
  }

  onPhoneInput(): void {
    const phoneControl = this.form.controls.phoneNumber;
    const numericOnly = phoneControl.value.replace(/\D/g, '');
    if (phoneControl.value !== numericOnly) {
      phoneControl.setValue(numericOnly, { emitEvent: false });
    }
  }

  submit(): void {
    if (this.form.invalid || this.isSubmitting()) {
      this.form.markAllAsTouched();
      return;
    }

    this.saveError.set(null);
    this.isSubmitting.set(true);
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
      error: (err) => {
        const message = typeof err?.error === 'string'
          ? err.error
          : err?.error?.message ?? 'Could not create patient. Check your input and try again.';
        this.saveError.set(message);
        this.isSubmitting.set(false);
      },
      complete: () => this.isSubmitting.set(false)
    });
  }

  private toApiDate(htmlDate: string): string {
    const d = new Date(htmlDate + 'T00:00:00.000Z');
    return d.toISOString();
  }

  cancel(): void {
    void this.router.navigateByUrl('/patients');
  }

  getPhoneError(control: AbstractControl): string {
    if (control.hasError('required')) {
      return 'Required';
    }

    if (control.hasError('pattern')) {
      return 'Phone number must be numeric only';
    }

    if (control.hasError('maxlength')) {
      return 'Max 32 characters';
    }

    return 'Invalid phone number';
  }

  getDateOfBirthError(control: AbstractControl): string {
    if (control.hasError('required')) {
      return 'Required';
    }

    return 'Date of birth cannot be in the future';
  }

  private readPrefillFromNavigationState(): { firstName: string; lastName: string; phoneNumber: string } | null {
    const state = (this.router.getCurrentNavigation()?.extras.state ?? history.state) as
      | {
          patient_name?: string;
          phone_number?: string;
          prefillPatient?: { firstName?: string; lastName?: string; phoneNumber?: string };
        }
      | undefined;

    const queryParams = this.router.parseUrl(this.router.url).queryParams;
    const queryPatientName = String(queryParams['patient_name'] ?? '').trim();
    const queryPhoneNumber = String(queryParams['phone_number'] ?? '').trim();

    const patientName = (state?.patient_name ?? queryPatientName).trim();
    const navigationPhone = (state?.phone_number ?? queryPhoneNumber).trim();
    const [derivedFirstName, ...derivedLastNameParts] = patientName.split(/\s+/).filter(Boolean);
    const derivedLastName = derivedLastNameParts.join(' ');

    if (!state?.prefillPatient && !patientName && !navigationPhone) {
      return null;
    }

    return {
      firstName: (state?.prefillPatient?.firstName ?? derivedFirstName ?? '').trim(),
      lastName: (state?.prefillPatient?.lastName ?? derivedLastName).trim(),
      phoneNumber: (state?.prefillPatient?.phoneNumber ?? navigationPhone).replace(/\D/g, '')
    };
  }
}
