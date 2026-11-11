import { HttpErrorResponse } from '@angular/common/http';

/** A failed API call, normalised from the API's error envelope so components handle one shape. */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly errors: readonly string[] = [],
  ) {
    super(message);
    this.name = 'ApiError';
  }

  get isNotFound(): boolean {
    return this.status === 404;
  }

  /** Every message worth showing: the individual validation errors, or the single summary message. */
  get details(): readonly string[] {
    return this.errors.length > 0 ? this.errors : [this.message];
  }
}

export const API_UNREACHABLE_MESSAGE =
  'Unable to reach the Stargate API. Check that it is running and try again.';

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) {
    return error;
  }

  if (error instanceof HttpErrorResponse) {
    // Status 0: network failure, CORS or the API is down. 502-504: the dev proxy could not reach the API.
    if (
      error.status === 0 ||
      error.status === 502 ||
      error.status === 503 ||
      error.status === 504
    ) {
      return new ApiError(error.status, API_UNREACHABLE_MESSAGE);
    }

    const body = error.error as { message?: unknown; errors?: unknown } | null;
    const message =
      typeof body?.message === 'string' ? body.message : 'An unexpected error occurred.';
    const errors = Array.isArray(body?.errors)
      ? body.errors.filter((e): e is string => typeof e === 'string')
      : [];

    return new ApiError(error.status, message, errors);
  }

  return new ApiError(0, 'An unexpected error occurred.');
}
