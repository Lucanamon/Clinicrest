import {
  Component,
  DestroyRef,
  ElementRef,
  HostListener,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, debounceTime, distinctUntilChanged, map, of, switchMap, tap } from 'rxjs';
import { AppointmentDto } from '../../appointment/appointment.service';
import { BacklogDto } from '../../backlog/backlog.service';
import { PatientDto } from '../../patient/patient.service';
import { GlobalSearchResult, GlobalSearchService } from './global-search.service';

@Component({
  selector: 'app-global-search',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './global-search.component.html',
  styleUrl: './global-search.component.scss'
})
export class GlobalSearchComponent {
  private readonly globalSearch = inject(GlobalSearchService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly host = inject(ElementRef<HTMLElement>);

  readonly queryControl = new FormControl('', { nonNullable: true });

  readonly loading = signal(false);
  readonly results = signal<GlobalSearchResult | null>(null);
  readonly panelOpen = signal(false);

  constructor() {
    this.queryControl.valueChanges
      .pipe(
        debounceTime(300),
        map((v) => v.trim()),
        distinctUntilChanged(),
        tap((trimmed) => {
          if (!trimmed) {
            this.results.set(null);
            this.loading.set(false);
            this.panelOpen.set(false);
          }
        }),
        switchMap((trimmed) => {
          if (!trimmed) {
            return of(null);
          }
          this.loading.set(true);
          return this.globalSearch.search(trimmed).pipe(
            catchError(() =>
              of<GlobalSearchResult>({
                patients: [],
                appointments: [],
                backlogs: []
              })
            )
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((data) => {
        this.loading.set(false);
        if (data === null) {
          return;
        }
        this.results.set(data);
        this.panelOpen.set(true);
      });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.panelOpen()) {
      return;
    }
    const target = event.target as Node | null;
    if (target && this.host.nativeElement.contains(target)) {
      return;
    }
    this.panelOpen.set(false);
  }

  onInputFocus(): void {
    const q = this.queryControl.value.trim();
    if (q && this.results() !== null) {
      this.panelOpen.set(true);
    }
  }

  patientLabel(p: PatientDto): string {
    return `${p.firstName} ${p.lastName}`.trim();
  }

  appointmentLabel(a: AppointmentDto): string {
    const d = new Date(a.appointmentDate);
    const datePart = d.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
    return `${a.patientName} - ${datePart}`;
  }

  backlogLabel(b: BacklogDto): string {
    return b.title;
  }

  goToPatient(p: PatientDto): void {
    void this.router.navigate(['/patients', p.id]);
    this.closeAfterNavigate();
  }

  goToAppointment(a: AppointmentDto): void {
    void this.router.navigate(['/appointments', a.id]);
    this.closeAfterNavigate();
  }

  goToBacklog(b: BacklogDto): void {
    void this.router.navigate(['/backlog', b.id]);
    this.closeAfterNavigate();
  }

  private closeAfterNavigate(): void {
    this.panelOpen.set(false);
    this.queryControl.setValue('', { emitEvent: false });
    this.results.set(null);
  }
}
