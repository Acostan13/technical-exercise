import { DatePipe, formatDate } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { toApiError } from '../../core/api/api-error';
import { StargateApiService } from '../../core/api/stargate-api.service';
import { isRetiredTitle, RETIRED_DUTY_TITLE } from '../../shared/astronaut-status';
import { addDays, toApiDate } from '../../shared/dates';
import { notBlank } from '../../shared/validators';

export interface AddDutyDialogData {
  name: string;
  /** Start date of the person's current duty, if any. A new duty must start after it. */
  currentDutyStart: Date | null;
  currentDutyTitle: string | null;
}

/** Adds an astronaut duty. Closes with the new duty's id. */
@Component({
  selector: 'app-add-duty-dialog',
  imports: [
    DatePipe,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Add astronaut duty</h2>

    <form [formGroup]="form" (ngSubmit)="save()" novalidate>
      <mat-dialog-content class="content">
        <p class="intro">
          For <strong>{{ data.name }}</strong>
        </p>

        <mat-form-field appearance="outline">
          <mat-label>Duty title</mat-label>
          <input
            matInput
            formControlName="dutyTitle"
            maxlength="100"
            autocomplete="off"
            cdkFocusInitial
          />
          <mat-hint>Enter {{ retiredTitle }} to retire this astronaut.</mat-hint>
          <mat-error>Duty title is required.</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Rank</mat-label>
          <input matInput formControlName="rank" maxlength="50" autocomplete="off" />
          <mat-error>Rank is required.</mat-error>
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Start date</mat-label>
          <input
            matInput
            [matDatepicker]="picker"
            formControlName="dutyStartDate"
            [min]="minStartDate"
          />
          <mat-datepicker-toggle matIconSuffix [for]="picker" />
          <mat-datepicker #picker />
          @if (minStartDate) {
            <mat-hint>Must be after {{ data.currentDutyStart | date: 'mediumDate' }}.</mat-hint>
          }
          @if (form.controls.dutyStartDate.hasError('required')) {
            <mat-error>Start date is required.</mat-error>
          } @else if (
            form.controls.dutyStartDate.hasError('tooEarly') ||
            form.controls.dutyStartDate.hasError('matDatepickerMin')
          ) {
            <mat-error>Must be after the current duty's start date.</mat-error>
          } @else {
            <mat-error>Enter a valid date.</mat-error>
          }
        </mat-form-field>

        @if (consequences().length > 0) {
          <aside class="consequences" aria-live="polite">
            <mat-icon aria-hidden="true">info</mat-icon>
            <ul>
              @for (line of consequences(); track line) {
                <li>{{ line }}</li>
              }
            </ul>
          </aside>
        }

        @if (serverErrors().length > 0) {
          <div class="form-error" role="alert">
            @for (error of serverErrors(); track error) {
              <p>{{ error }}</p>
            }
          </div>
        }
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close [disabled]="saving()">Cancel</button>
        <button mat-flat-button type="submit" [disabled]="saving()">
          @if (saving()) {
            <mat-spinner diameter="18" />
          }
          Add duty
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .content {
      display: flex;
      flex-direction: column;
      gap: 10px;
      min-width: min(440px, 75vw);
    }
    .intro {
      margin-top: 0;
      color: var(--mat-sys-on-surface-variant);
    }
    .consequences {
      display: flex;
      gap: 10px;
      padding: 12px 14px;
      border-radius: 12px;
      background: var(--mat-sys-primary-container);
      color: var(--mat-sys-on-primary-container);
    }
    .consequences ul {
      margin: 0;
      padding-left: 16px;
    }
    .form-error p {
      margin: 8px 0 0;
      color: var(--mat-sys-error);
    }
    mat-spinner {
      display: inline-block;
      margin-right: 8px;
      vertical-align: middle;
    }
  `,
})
export class AddDutyDialog {
  private readonly api = inject(StargateApiService);
  private readonly dialogRef = inject(MatDialogRef<AddDutyDialog, number>);
  protected readonly data = inject<AddDutyDialogData>(MAT_DIALOG_DATA);

  protected readonly retiredTitle = RETIRED_DUTY_TITLE;
  protected readonly minStartDate = this.data.currentDutyStart
    ? addDays(this.data.currentDutyStart, 1)
    : null;
  protected readonly saving = signal(false);
  protected readonly serverErrors = signal<readonly string[]>([]);

  protected readonly form = new FormGroup({
    dutyTitle: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, notBlank],
    }),
    rank: new FormControl('', { nonNullable: true, validators: [Validators.required, notBlank] }),
    dutyStartDate: new FormControl<Date | null>(null, [
      Validators.required,
      startsAfter(this.minStartDate),
    ]),
  });

  private readonly formValue = toSignal(this.form.valueChanges, {
    initialValue: this.form.getRawValue(),
  });

  /** Previews what the business rules will do, before the user commits. */
  protected readonly consequences = computed(() => {
    const { dutyTitle, dutyStartDate } = this.formValue();
    const lines: string[] = [];

    // Only preview a valid date: an invalid one would describe something the API will reject.
    if (!dutyStartDate || (this.minStartDate && dutyStartDate < this.minStartDate)) {
      return lines;
    }

    if (this.data.currentDutyTitle) {
      lines.push(
        `${this.data.currentDutyTitle} will end on ${formatShort(addDays(dutyStartDate, -1))}.`,
      );
    }
    if (dutyTitle && isRetiredTitle(dutyTitle)) {
      lines.push(`Career will end on ${formatShort(addDays(dutyStartDate, -1))}.`);
    }
    return lines;
  });

  protected save(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving()) {
      return;
    }

    const { dutyTitle, rank, dutyStartDate } = this.form.getRawValue();

    this.saving.set(true);
    this.serverErrors.set([]);
    this.dialogRef.disableClose = true;

    this.api
      .createAstronautDuty({
        name: this.data.name,
        dutyTitle: dutyTitle.trim(),
        rank: rank.trim(),
        dutyStartDate: toApiDate(dutyStartDate!),
      })
      .subscribe({
        next: (id) => this.dialogRef.close(id),
        error: (error: unknown) => {
          this.serverErrors.set(toApiError(error).details);
          this.saving.set(false);
          this.dialogRef.disableClose = false;
        },
      });
  }
}

function startsAfter(min: Date | null): ValidatorFn {
  return (control) => {
    const value = control.value as Date | null;
    return min && value && value < min ? { tooEarly: true } : null;
  };
}

function formatShort(date: Date): string {
  return formatDate(date, 'mediumDate', 'en-US');
}
