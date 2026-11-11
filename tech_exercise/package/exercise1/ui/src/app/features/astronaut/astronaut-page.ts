import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, input, signal } from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router, RouterLink } from '@angular/router';
import { catchError, map, of, startWith, switchMap } from 'rxjs';

import { ApiError, toApiError } from '../../core/api/api-error';
import { AstronautRecord, StargateApiService } from '../../core/api/stargate-api.service';
import { NotificationService } from '../../core/notifications/notification.service';
import { astronautStatus } from '../../shared/astronaut-status';
import { describeDuration, parseApiDate } from '../../shared/dates';
import { StatusBadge } from '../../shared/status-badge';
import { PersonDialog } from '../people/person-dialog';
import { AddDutyDialog, AddDutyDialogData } from './add-duty-dialog';
import { DutyTimeline } from './duty-timeline';
import { describeDutyChanges, toDutyViews } from './duty-view';

type LoadState =
  | { status: 'loading' }
  | { status: 'loaded'; record: AstronautRecord }
  | { status: 'error'; error: ApiError };

export interface DutyResult {
  dutyId: number;
  changes: string[];
}

/**
 * An individual's astronaut record: current astronaut information and duty history, retrieved with
 * GET /AstronautDuty/{name}.
 */
@Component({
  selector: 'app-astronaut-page',
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    StatusBadge,
    DutyTimeline,
  ],
  templateUrl: './astronaut-page.html',
  styleUrl: './astronaut-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AstronautPage {
  private readonly api = inject(StargateApiService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly notifications = inject(NotificationService);

  /** Bound from the :name route parameter. */
  readonly name = input.required<string>();

  protected readonly state = signal<LoadState>({ status: 'loading' });
  protected readonly lastResult = signal<DutyResult | null>(null);

  protected readonly record = computed(() => {
    const state = this.state();
    return state.status === 'loaded' ? state.record : null;
  });

  protected readonly loadError = computed(() => {
    const state = this.state();
    return state.status === 'error' ? state.error : null;
  });

  protected readonly duties = computed(() => toDutyViews(this.record()?.duties ?? []));

  protected readonly summary = computed(() => {
    const person = this.record()?.person;
    if (!person) {
      return null;
    }

    const careerStart = person.careerStartDate ? parseApiDate(person.careerStartDate) : null;
    const careerEnd = person.careerEndDate ? parseApiDate(person.careerEndDate) : null;

    return {
      status: astronautStatus(person),
      careerStart,
      careerEnd,
      service: careerStart ? describeDuration(careerStart, careerEnd ?? new Date()) : null,
    };
  });

  constructor() {
    toObservable(this.name)
      .pipe(
        switchMap((name) =>
          this.api.getAstronautDuties(name).pipe(
            map((record): LoadState => ({ status: 'loaded', record })),
            catchError((error: unknown) =>
              of<LoadState>({ status: 'error', error: toApiError(error) }),
            ),
            startWith<LoadState>({ status: 'loading' }),
          ),
        ),
        takeUntilDestroyed(),
      )
      .subscribe((state) => {
        this.state.set(state);
        this.lastResult.set(null);
      });
  }

  protected reload(): void {
    this.state.set({ status: 'loading' });
    this.api.getAstronautDuties(this.name()).subscribe({
      next: (record) => this.state.set({ status: 'loaded', record }),
      error: (error: unknown) => this.state.set({ status: 'error', error: toApiError(error) }),
    });
  }

  protected addDuty(): void {
    const before = this.record();
    if (!before) {
      return;
    }

    const current = before.duties.find((d) => d.dutyEndDate === null) ?? null;
    const data: AddDutyDialogData = {
      name: before.person.name,
      currentDutyStart:
        before.duties.length > 0 ? parseApiDate(before.duties[0].dutyStartDate) : null,
      currentDutyTitle: current?.dutyTitle ?? null,
    };

    this.dialog
      .open<AddDutyDialog, AddDutyDialogData, number>(AddDutyDialog, { data, autoFocus: 'dialog' })
      .afterClosed()
      .subscribe((dutyId) => {
        if (dutyId === undefined) {
          return;
        }

        // Re-read the record to show exactly what the API's business rules changed.
        this.api.getAstronautDuties(before.person.name).subscribe({
          next: (after) => {
            this.state.set({ status: 'loaded', record: after });
            this.lastResult.set({ dutyId, changes: describeDutyChanges(before, after, dutyId) });
          },
          error: (error: unknown) => this.notifications.error(toApiError(error).message),
        });
      });
  }

  protected rename(): void {
    const person = this.record()?.person;
    if (!person) {
      return;
    }

    this.dialog
      .open(PersonDialog, { data: { currentName: person.name }, autoFocus: 'dialog' })
      .afterClosed()
      .subscribe((name?: string) => {
        if (name && name !== person.name) {
          this.notifications.success(`Renamed to ${name}.`);
          this.router.navigate(['/people', name], { replaceUrl: true });
        }
      });
  }

  protected dismissResult(): void {
    this.lastResult.set(null);
  }
}
