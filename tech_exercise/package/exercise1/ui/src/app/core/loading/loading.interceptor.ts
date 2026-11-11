import { HttpContextToken, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';

import { LoadingService } from './loading.service';

/** Marks a request (e.g. background polling) that should not drive the global progress indicator. */
export const SILENT_REQUEST = new HttpContextToken<boolean>(() => false);

export const loadingInterceptor: HttpInterceptorFn = (request, next) => {
  if (request.context.get(SILENT_REQUEST)) {
    return next(request);
  }

  const loading = inject(LoadingService);
  loading.start();

  return next(request).pipe(finalize(() => loading.stop()));
};
