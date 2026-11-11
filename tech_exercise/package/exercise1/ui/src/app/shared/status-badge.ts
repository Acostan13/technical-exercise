import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

import { AstronautStatus, STATUS_LABELS } from './astronaut-status';

@Component({
  selector: 'app-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<span [class]="'badge badge--' + status()">{{ label() }}</span>`,
  styles: `
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 2px 10px;
      border-radius: 999px;
      font: var(--mat-sys-label-medium);
      white-space: nowrap;
    }
    .badge::before {
      content: '';
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: currentColor;
    }
    .badge--active {
      background: #e3f6ec;
      color: #146c43;
    }
    .badge--retired {
      background: #fff1dc;
      color: #8a4b00;
    }
    .badge--civilian {
      background: var(--mat-sys-surface-container-high);
      color: var(--mat-sys-on-surface-variant);
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<AstronautStatus>();

  protected readonly label = computed(() => STATUS_LABELS[this.status()]);
}
