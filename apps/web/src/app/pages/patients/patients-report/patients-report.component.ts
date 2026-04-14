import { HttpClient } from '@angular/common/http';
import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { environment } from '../../../../environments/environment';

interface PatientExportResult {
  downloadUrl: string;
}

interface PatientPreviewItem {
  name: string;
  age: number;
  phone: string;
  disease?: string | null;
  createdAt: string;
}

interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

@Component({
  selector: 'app-patients-report',
  standalone: true,
  imports: [FormsModule, NgIf, NgFor, DatePipe],
  templateUrl: './patients-report.component.html',
  styleUrl: './patients-report.component.scss'
})
export class PatientsReportComponent implements OnInit {
  private readonly http = inject(HttpClient);

  search = '';
  sortBy: 'name' | 'createdAt' = 'name';
  sortDirection: 'asc' | 'desc' = 'asc';
  pageNumber = 1;
  pageSize = 10;
  totalCount = 0;
  data: PatientPreviewItem[] = [];

  ngOnInit(): void {
    this.loadPreview();
  }

  loadPreview(): void {
    this.http
      .get<PagedResult<PatientPreviewItem>>(`${environment.apiUrl}/patients/preview`, {
        params: {
          searchTerm: this.search.trim(),
          sortBy: this.sortBy,
          sortDirection: this.sortDirection,
          pageNumber: String(this.pageNumber),
          pageSize: String(this.pageSize)
        }
      })
      .subscribe((res) => {
        this.data = res.items;
        this.totalCount = res.totalCount;
        this.pageNumber = res.pageNumber;
        this.pageSize = res.pageSize;
      });
  }

  applyFilters(): void {
    this.pageNumber = 1;
    this.loadPreview();
  }

  previousPage(): void {
    if (this.pageNumber <= 1) {
      return;
    }

    this.pageNumber--;
    this.loadPreview();
  }

  nextPage(): void {
    if (this.pageNumber * this.pageSize >= this.totalCount) {
      return;
    }

    this.pageNumber++;
    this.loadPreview();
  }

  downloadExcel(): void {
    this.http
      .post<PatientExportResult>(`${environment.apiUrl}/patients/export`, {
        name: this.search.trim(),
        sortBy: this.sortBy
      })
      .subscribe((res) => {
        window.open(res.downloadUrl, '_blank');
      });
  }
}
