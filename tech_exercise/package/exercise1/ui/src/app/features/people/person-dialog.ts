import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Observable } from 'rxjs';

import { toApiError } from '../../core/api/api-error';
import { StargateApiService } from '../../core/api/stargate-api.service';
import { notBlank } from '../../shared/validators';

export const PERSON_NAME_MAX_LENGTH = 100;

export interface PersonDialogData {
  /** When set, the dialog renames this person; otherwise it adds a new person. */
  currentName?: string;
}

/** Adds a person, or updates (renames) an existing one. Closes with the saved name. */
@Component({
  selector: 'app-person-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ isRename ? 'Update person' : 'Add person' }}</h2>

    <form [formGroup]="form" (ngSubmit)="save()" novalidate>
      <mat-dialog-content>
        <p class="hint">
          {{
            isRename
              ? 'Names are unique. Astronaut records stay with the person.'
              : 'Names are unique. Astronaut duties can be added once the person exists.'
          }}
        </p>

        <mat-form-field appearance="outline" class="full-width">
          <mat-label>Full name</mat-label>
          <input
            matInput
            formControlName="name"
            [maxlength]="maxLength"
            autocomplete="off"
            cdkFocusInitial
            required
          />
          <mat-hint align="end">{{ name.value.length }} / {{ maxLength }}</mat-hint>
          @if (name.hasError('required') || name.hasError('blank')) {
            <mat-error>Name is required.</mat-error>
          }
        </mat-form-field>

        @if (serverError(); as error) {
          <p class="form-error" role="alert">{{ error }}</p>
        }
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close [disabled]="saving()">Cancel</button>
        <button mat-flat-button type="submit" [disabled]="saving()">
          @if (saving()) {
            <mat-spinner diameter="18" />
          }
          {{ isRename ? 'Save' : 'Add person' }}
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .full-width {
      width: 100%;
      min-width: min(420px, 70vw);
    }
    .hint {
      margin-top: 0;
      color: var(--mat-sys-on-surface-variant);
    }
    .form-error {
      margin: 0;
      color: var(--mat-sys-error);
    }
    mat-spinner {
      display: inline-block;
      margin-right: 8px;
      vertical-align: middle;
    }
  `,
})
export class PersonDialog {
  private readonly api = inject(StargateApiService);
  private readonly dialogRef = inject(MatDialogRef<PersonDialog, string>);
  private readonly data = inject<PersonDialogData>(MAT_DIALOG_DATA, { optional: true }) ?? {};

  protected readonly isRename = !!this.data.currentName;
  protected readonly maxLength = PERSON_NAME_MAX_LENGTH;
  protected readonly saving = signal(false);
  protected readonly serverError = signal<string | null>(null);

  protected readonly name = new FormControl(this.data.currentName ?? '', {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(PERSON_NAME_MAX_LENGTH), notBlank],
  });

  // A FormGroup on the <form> lets Angular handle submit; without it the browser would do a full-page submit.
  protected readonly form = new FormGroup({ name: this.name });

  protected save(): void {
    this.name.markAsTouched();
    if (this.name.invalid || this.saving()) {
      return;
    }

    const name = this.name.value.trim();
    if (this.isRename && name === this.data.currentName) {
      this.dialogRef.close(name);
      return;
    }

    const request: Observable<number> = this.isRename
      ? this.api.updatePerson(this.data.currentName!, name)
      : this.api.createPerson(name);

    this.saving.set(true);
    this.serverError.set(null);
    this.dialogRef.disableClose = true;

    request.subscribe({
      next: () => this.dialogRef.close(name),
      error: (error: unknown) => {
        this.serverError.set(toApiError(error).details.join(' '));
        this.saving.set(false);
        this.dialogRef.disableClose = false;
      },
    });
  }
}
