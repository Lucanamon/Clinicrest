import { Component, OnInit, inject, signal } from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PatientService } from '../../patient.service';
import { AuthService } from '../../../services/auth';

@Component({
  selector: 'app-patient-detail',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './patient-detail.component.html',
  styleUrl: './patient-detail.component.scss'
})
export class PatientDetailComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly patientService = inject(PatientService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  readonly auth = inject(AuthService);

  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  readonly editing = signal(false);
  selectedGender = '';

  readonly form = this.fb.nonNullable.group({
    firstName: ['', [Validators.required, Validators.maxLength(200)]],
    lastName: ['', [Validators.required, Validators.maxLength(200)]],
    dateOfBirth: ['', Validators.required],
    gender: ['', [Validators.required, Validators.maxLength(32)]],
    age: this.fb.nonNullable.control({ value: 0, disabled: true }),
    phoneNumber: ['', [Validators.required, Validators.maxLength(32)]],
    email: ['', [Validators.maxLength(500), Validators.email]],
    allowSms: true,
    allowEmail: true,
    underlyingDisease: ['', Validators.maxLength(1000)]
  });

  goBack(): void {
    void this.router.navigate(['/patients']);
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      void this.router.navigateByUrl('/patients');
      return;
    }
    this.loadPatient(id, { withPageLoading: true });
  }

  private loadPatient(id: string, options?: { withPageLoading?: boolean }): void {
    const withPageLoading = options?.withPageLoading ?? false;
    if (withPageLoading) {
      this.loading.set(true);
    }
    this.loadError.set(null);
    this.patientService.getById(id).subscribe({
      next: (p) => {
        const dob = p.dateOfBirth.slice(0, 10);
        const trimmedGender = (p.gender ?? '').trim();
        const isBinaryGender = trimmedGender === 'Male' || trimmedGender === 'Female';
        this.selectedGender = isBinaryGender ? trimmedGender : 'Other';
        this.form.patchValue({
          firstName: p.firstName,
          lastName: p.lastName,
          dateOfBirth: dob,
          gender: trimmedGender,
          phoneNumber: p.phoneNumber,
          email: p.email ?? '',
          allowSms: p.allowSms,
          allowEmail: p.allowEmail,
          underlyingDisease: p.underlyingDisease ?? ''
        });
        this.onDateChange(new Date(dob + 'T12:00:00.000Z'));
        if (withPageLoading) {
          this.loading.set(false);
        }
      },
      error: () => {
        this.loadError.set('Patient not found or could not be loaded.');
        if (withPageLoading) {
          this.loading.set(false);
        }
      }
    });
  }

  startEdit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }
    this.saveError.set(null);
    this.editing.set(true);
    this.loadPatient(id, { withPageLoading: false });
  }

  cancelEdit(): void {
    this.saveError.set(null);
    this.editing.set(false);
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadPatient(id, { withPageLoading: false });
    }
  }

  onDateChange(dob: Date): void {
    if (Number.isNaN(dob.getTime())) {
      return;
    }
    const today = new Date();
    let age = today.getFullYear() - dob.getFullYear();
    const m = today.getMonth() - dob.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < dob.getDate())) {
      age--;
    }
    this.form.patchValue({ age });
  }

  onDateOfBirthInput(value: string): void {
    if (!value) {
      return;
    }
    this.onDateChange(new Date(value + 'T12:00:00.000Z'));
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      return;
    }

    this.saveError.set(null);
    const v = this.form.getRawValue();
    const typedGender = v.gender.trim();
    const finalGender =
      this.selectedGender === 'Other'
        ? typedGender
        : (this.selectedGender || typedGender).trim();

    if (this.selectedGender === 'Other' && !typedGender) {
      this.saveError.set('Please specify gender.');
      this.form.controls.gender.markAsTouched();
      return;
    }

    const payload = {
      firstName: v.firstName.trim(),
      lastName: v.lastName.trim(),
      dateOfBirth: this.toApiDate(v.dateOfBirth),
      gender: finalGender || 'Other',
      phoneNumber: v.phoneNumber.trim(),
      email: v.email.trim() || null,
      allowSms: v.allowSms,
      allowEmail: v.allowEmail,
      underlyingDisease: v.underlyingDisease.trim() || null
    };

    this.patientService.update(id, payload).subscribe({
      next: () => {
        this.editing.set(false);
        this.loadPatient(id, { withPageLoading: false });
      },
      error: () => this.saveError.set('Could not save changes. Check your input and try again.')
    });
  }

  onGenderChoiceChange(choice: string): void {
    this.selectedGender = choice;
    if (choice === 'Male' || choice === 'Female') {
      this.form.patchValue({ gender: choice });
      this.form.controls.gender.markAsTouched();
      return;
    }

    this.form.patchValue({ gender: '' });
  }

  private toApiDate(htmlDate: string): string {
    const d = new Date(htmlDate + 'T00:00:00.000Z');
    return d.toISOString();
  }

  displayName(): string {
    const v = this.form.getRawValue();
    const name = `${v.firstName} ${v.lastName}`.trim();
    const age = v.age;
    if (!name) {
      return 'Patient';
    }
    return `${name} (${age} years old)`;
  }

  formatDob(): string {
    const raw = this.form.getRawValue().dateOfBirth;
    if (!raw) {
      return '—';
    }
    const d = new Date(raw + 'T12:00:00.000Z');
    return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(d);
  }

  phoneDisplay(): string {
    const v = this.form.getRawValue().phoneNumber?.trim();
    return v || '—';
  }

  diseaseDisplay(): string {
    const v = this.form.getRawValue().underlyingDisease?.trim();
    return v || '—';
  }

  emailDisplay(): string {
    const v = this.form.getRawValue().email?.trim();
    return v || '—';
  }

  genderDisplay(): string {
    const v = this.form.getRawValue().gender?.trim();
    return v || '—';
  }
}
