import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

/** Rejects values that contain only whitespace (the API trims input, so they would be empty). */
export const notBlank: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  typeof control.value === 'string' && control.value.length > 0 && control.value.trim().length === 0
    ? { blank: true }
    : null;
