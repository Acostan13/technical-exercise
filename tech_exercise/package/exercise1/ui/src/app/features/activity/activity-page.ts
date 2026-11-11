import { DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTooltipModule } from '@angular/material/tooltip';
import { filter, interval, Subscription } from 'rxjs';

import { ApiError, toApiError } from '../../core/api/api-error';
import { ProcessLog, ProcessLogLevel } from '../../core/api/models';
import { StargateApiService } from '../../core/api/stargate-api.service';

export type LevelFilter = 'all' | ProcessLogLevel;

const PAGE_SIZE = 50;
const MAX_ENTRIES = 500; // API limit

const REFRESH_INTERVAL_MS = 5000;

/** Readable names for the API's MediatR request types. */
const PROCESS_NAMES: Record<string, string> = {
  CreatePerson: 'Add person',
  UpdatePerson: 'Update person',
  GetPeople: 'List personnel',
  GetPersonByName: 'Look up person',
  GetAstronautDutiesByName: 'Retrieve astronaut duties',
  CreateAstronautDuty: 'Add astronaut duty',
};

/** Processes that change data; the rest are lookups (page views). */
const CHANGE_PROCESSES = new Set(['CreatePerson', 'UpdatePerson', 'CreateAstronautDuty']);

const LEVEL_ICONS: Record<ProcessLogLevel, string> = {
  Information: 'check_circle',
  Warning: 'warning',
  Error: 'error',
};

/**
 * The server-side process log (successes and failures of every business process), so users can follow
 * what the system has done. Optionally refreshes in the background.
 */
@Component({
  selector: 'app-activity-page',
  imports: [
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatButtonToggleModule,
    MatIconModule,
    MatSlideToggleModule,
    MatTooltipModule,
  ],
  templateUrl: './activity-page.html',
  styleUrl: './activity-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ActivityPage implements OnInit {
  private readonly api = inject(StargateApiService);
  private readonly destroyRef = inject(DestroyRef);
  private autoRefresh?: Subscription;

  protected readonly loading = signal(true);
  protected readonly error = signal<ApiError | null>(null);
  protected readonly logs = signal<ProcessLog[]>([]);
  protected readonly levelFilter = signal<LevelFilter>('all');
  protected readonly live = signal(false);
  protected readonly changesOnly = signal(true);
  protected readonly take = signal(PAGE_SIZE);
  protected readonly levelIcons = LEVEL_ICONS;

  /** Entries after the "changes only" filter; the level counts and level filter apply to these. */
  protected readonly visible = computed(() =>
    this.changesOnly()
      ? this.logs().filter((log) => CHANGE_PROCESSES.has(log.requestName))
      : this.logs(),
  );

  protected readonly filtered = computed(() => {
    const level = this.levelFilter();
    return level === 'all' ? this.visible() : this.visible().filter((log) => log.level === level);
  });

  protected readonly canLoadMore = computed(
    () => this.logs().length === this.take() && this.take() < MAX_ENTRIES,
  );

  protected readonly counts = computed(() => {
    const logs = this.visible();
    return {
      Information: logs.filter((l) => l.level === 'Information').length,
      Warning: logs.filter((l) => l.level === 'Warning').length,
      Error: logs.filter((l) => l.level === 'Error').length,
    };
  });

  ngOnInit(): void {
    this.refresh();
  }

  protected refresh(silent = false): void {
    if (!silent) {
      this.loading.set(true);
    }

    this.api.getProcessLogs(this.take(), silent).subscribe({
      next: (logs) => {
        this.logs.set(logs);
        this.error.set(null);
        this.loading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(toApiError(error));
        this.loading.set(false);
      },
    });
  }

  protected loadMore(): void {
    this.take.update((take) => Math.min(take + PAGE_SIZE, MAX_ENTRIES));
    this.refresh();
  }

  protected setLive(enabled: boolean): void {
    this.live.set(enabled);
    this.autoRefresh?.unsubscribe();

    if (enabled) {
      this.autoRefresh = interval(REFRESH_INTERVAL_MS)
        .pipe(
          filter(() => document.visibilityState === 'visible'),
          takeUntilDestroyed(this.destroyRef),
        )
        .subscribe(() => this.refresh(true));
    }
  }

  /** Successes need no explanation; rejections and errors show their reason. */
  protected outcome(log: ProcessLog): string {
    return log.succeeded ? 'Succeeded' : log.message;
  }

  protected processName(requestName: string): string {
    return PROCESS_NAMES[requestName] ?? requestName;
  }

  protected formatRequest(requestData: string | null): string {
    if (!requestData) {
      return '';
    }
    try {
      return JSON.stringify(JSON.parse(requestData), null, 2);
    } catch {
      return requestData; // Truncated entries are not valid JSON; show them as stored.
    }
  }
}
