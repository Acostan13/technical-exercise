import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterLink } from '@angular/router';

import { ApiError, toApiError } from '../../core/api/api-error';
import { PersonAstronaut } from '../../core/api/models';
import { StargateApiService } from '../../core/api/stargate-api.service';
import { NotificationService } from '../../core/notifications/notification.service';
import { AstronautStatus, astronautStatus } from '../../shared/astronaut-status';
import { parseApiDate } from '../../shared/dates';
import { StatusBadge } from '../../shared/status-badge';
import { PersonDialog } from './person-dialog';

export type StatusFilter = 'all' | AstronautStatus;

export interface PersonRow extends PersonAstronaut {
  status: AstronautStatus;
  careerStart: Date | null;
  careerEnd: Date | null;
}

@Component({
  selector: 'app-people-page',
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatTableModule,
    MatTooltipModule,
    StatusBadge,
  ],
  templateUrl: './people-page.html',
  styleUrl: './people-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PeoplePage implements OnInit {
  private readonly api = inject(StargateApiService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  protected readonly columns = ['name', 'status', 'rank', 'title', 'careerStart', 'careerEnd'];

  protected readonly loading = signal(true);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly people = signal<PersonRow[]>([]);
  protected readonly search = signal('');
  protected readonly statusFilter = signal<StatusFilter>('all');

  protected readonly stats = computed(() => {
    const people = this.people();
    const count = (status: AstronautStatus) => people.filter((p) => p.status === status).length;

    return [
      { label: 'Total personnel', value: people.length, icon: 'groups', tone: 'neutral' },
      { label: 'Active astronauts', value: count('active'), icon: 'rocket_launch', tone: 'active' },
      { label: 'Retired', value: count('retired'), icon: 'military_tech', tone: 'retired' },
      {
        label: 'No assignments',
        value: count('civilian'),
        icon: 'person_outline',
        tone: 'civilian',
      },
    ];
  });

  protected readonly filtered = computed(() =>
    filterPeople(this.people(), this.search(), this.statusFilter()),
  );

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getPeople().subscribe({
      next: (people) => {
        this.people.set(people.map(toRow));
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(toApiError(error));
        this.loading.set(false);
      },
    });
  }

  protected onSearch(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected onStatusFilter(value: StatusFilter | undefined): void {
    this.statusFilter.set(value ?? 'all');
  }

  protected addPerson(): void {
    this.dialog
      .open(PersonDialog, { data: {}, autoFocus: 'dialog' })
      .afterClosed()
      .subscribe((name?: string) => {
        if (name) {
          this.notifications.success(`${name} was added.`);
          this.router.navigate(['/people', name]);
        }
      });
  }
}

function toRow(person: PersonAstronaut): PersonRow {
  return {
    ...person,
    status: astronautStatus(person),
    careerStart: person.careerStartDate ? parseApiDate(person.careerStartDate) : null,
    careerEnd: person.careerEndDate ? parseApiDate(person.careerEndDate) : null,
  };
}

/** Case-insensitive match on name, rank or duty title, combined with the status filter. */
export function filterPeople(
  people: PersonRow[],
  search: string,
  status: StatusFilter,
): PersonRow[] {
  const term = search.trim().toLowerCase();

  return people.filter(
    (person) =>
      (status === 'all' || person.status === status) &&
      (!term ||
        person.name.toLowerCase().includes(term) ||
        person.currentRank?.toLowerCase().includes(term) ||
        person.currentDutyTitle?.toLowerCase().includes(term)),
  );
}
