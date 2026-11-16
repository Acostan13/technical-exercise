import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

import { DutyView } from './duty-view';

/** Vertical timeline of a person's astronaut duties, most recent first. */
@Component({
  selector: 'app-duty-timeline',
  imports: [DatePipe, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ol class="timeline" aria-label="Duty history, most recent first">
      @for (duty of duties(); track duty.id) {
        <li
          class="entry"
          [class.entry--current]="duty.isCurrent"
          [class.entry--retired]="duty.isRetirement"
          [class.entry--new]="duty.id === highlightId()"
        >
          <span class="marker" aria-hidden="true">
            <mat-icon>{{
              duty.isRetirement ? 'military_tech' : duty.isCurrent ? 'rocket_launch' : 'check'
            }}</mat-icon>
          </span>

          <article class="card">
            <header class="card__header">
              <h3 class="card__title">{{ duty.dutyTitle }}</h3>
              <span class="rank">{{ duty.rank }}</span>
              @if (duty.isCurrent) {
                <span class="tag">{{ duty.isRetirement ? 'Retired' : 'Current' }}</span>
              }
            </header>
            <p class="card__dates">
              <time [attr.datetime]="duty.start | date: 'yyyy-MM-dd'">{{
                duty.start | date: 'mediumDate'
              }}</time>
              @if (duty.end) {
                <span aria-hidden="true">–</span>
                <time [attr.datetime]="duty.end | date: 'yyyy-MM-dd'">{{
                  duty.end | date: 'mediumDate'
                }}</time>
              } @else if (!duty.isRetirement) {
                <span>– Present</span>
              }
              @if (duty.duration) {
                <span aria-hidden="true">·</span>
                <span>{{ duty.duration }}</span>
              }
            </p>
          </article>
        </li>
      }
    </ol>
  `,
  styles: `
    .timeline {
      position: relative;
      margin: 0;
      padding: 0;
      list-style: none;
    }
    .entry {
      position: relative;
      display: grid;
      grid-template-columns: 40px 1fr;
      gap: 16px;
      padding-bottom: 20px;
    }
    .entry:not(:last-child)::before {
      content: '';
      position: absolute;
      left: 19px;
      top: 40px;
      bottom: 0;
      width: 2px;
      background: var(--mat-sys-outline-variant);
    }
    .marker {
      display: grid;
      place-items: center;
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background: var(--mat-sys-surface-container-high);
      color: var(--mat-sys-on-surface-variant);
    }
    .marker mat-icon {
      font-size: 20px;
      width: 20px;
      height: 20px;
    }
    .entry--current .marker {
      background: var(--mat-sys-primary);
      color: var(--mat-sys-on-primary);
      box-shadow: 0 0 0 6px color-mix(in srgb, var(--mat-sys-primary) 18%, transparent);
    }
    .entry--retired .marker {
      background: #8a4b00;
      color: #fff;
      box-shadow: none;
    }
    .card {
      padding: 14px 18px;
      border-radius: 14px;
      border: 1px solid var(--mat-sys-outline-variant);
      background: var(--mat-sys-surface-container-lowest);
    }
    .entry--current .card {
      border-color: color-mix(in srgb, var(--mat-sys-primary) 45%, transparent);
    }
    .entry--new .card {
      animation: arrive 1.6s ease-out;
    }
    @keyframes arrive {
      0% {
        transform: translateY(-6px);
        box-shadow: 0 0 0 4px color-mix(in srgb, var(--mat-sys-primary) 35%, transparent);
      }
      100% {
        transform: none;
        box-shadow: 0 0 0 0 transparent;
      }
    }
    .card__header {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 8px 10px;
    }
    .card__title {
      margin: 0;
      font: var(--mat-sys-title-medium);
    }
    .rank {
      padding: 1px 8px;
      border-radius: 6px;
      font: var(--mat-sys-label-medium);
      background: var(--mat-sys-secondary-container);
      color: var(--mat-sys-on-secondary-container);
    }
    .tag {
      margin-left: auto;
      font: var(--mat-sys-label-medium);
      color: var(--mat-sys-primary);
    }
    .entry--retired .card {
      border-color: #e0b47a;
    }
    .entry--retired .tag {
      color: #8a4b00;
    }
    .card__dates {
      display: flex;
      flex-wrap: wrap;
      gap: 0 0.35em;
      margin: 6px 0 0;
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class DutyTimeline {
  readonly duties = input.required<DutyView[]>();
  /** A newly added duty to draw attention to. */
  readonly highlightId = input<number | null>(null);
}
