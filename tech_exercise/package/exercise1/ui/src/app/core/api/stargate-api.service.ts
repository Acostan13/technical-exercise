import { HttpClient, HttpContext, HttpParams } from '@angular/common/http';
import { inject, Injectable, InjectionToken } from '@angular/core';
import { catchError, map, Observable, throwError } from 'rxjs';

import { SILENT_REQUEST } from '../loading/loading.interceptor';
import { toApiError } from './api-error';
import {
  AstronautDuty,
  CreateAstronautDutyRequest,
  GetAstronautDutiesByNameResult,
  GetPeopleResult,
  GetPersonByNameResult,
  GetProcessLogsResult,
  IdResult,
  PersonAstronaut,
  ProcessLog,
} from './models';

/** Base URL of the Stargate API. In development `/api` is proxied to the API (see proxy.conf.json). */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});

export interface AstronautRecord {
  person: PersonAstronaut;
  duties: AstronautDuty[];
}

/**
 * Typed access to every Stargate API endpoint. Errors are normalised to {@link ApiError}, so callers never
 * deal with raw HTTP responses.
 */
@Injectable({ providedIn: 'root' })
export class StargateApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = inject(API_BASE_URL);

  getPeople(): Observable<PersonAstronaut[]> {
    return this.http.get<GetPeopleResult>(`${this.baseUrl}/Person`).pipe(
      map((result) => result.people),
      catchError(rethrowAsApiError),
    );
  }

  getPerson(name: string): Observable<PersonAstronaut> {
    return this.http
      .get<GetPersonByNameResult>(`${this.baseUrl}/Person/${encodeURIComponent(name)}`)
      .pipe(
        map((result) => result.person),
        catchError(rethrowAsApiError),
      );
  }

  createPerson(name: string): Observable<number> {
    return this.http.post<IdResult>(`${this.baseUrl}/Person`, { name }).pipe(
      map((result) => result.id),
      catchError(rethrowAsApiError),
    );
  }

  /** Updates (renames) the person currently called `currentName`. */
  updatePerson(currentName: string, name: string): Observable<number> {
    return this.http
      .put<IdResult>(`${this.baseUrl}/Person/${encodeURIComponent(currentName)}`, { name })
      .pipe(
        map((result) => result.id),
        catchError(rethrowAsApiError),
      );
  }

  /** Retrieves a person's current astronaut information and their duty history, most recent first. */
  getAstronautDuties(name: string): Observable<AstronautRecord> {
    return this.http
      .get<GetAstronautDutiesByNameResult>(
        `${this.baseUrl}/AstronautDuty/${encodeURIComponent(name)}`,
      )
      .pipe(
        map((result) => ({ person: result.person, duties: result.astronautDuties })),
        catchError(rethrowAsApiError),
      );
  }

  createAstronautDuty(request: CreateAstronautDutyRequest): Observable<number> {
    return this.http.post<IdResult>(`${this.baseUrl}/AstronautDuty`, request).pipe(
      map((result) => result.id),
      catchError(rethrowAsApiError),
    );
  }

  /** @param silent when true the request does not drive the global progress bar (used for polling). */
  getProcessLogs(take: number, silent = false): Observable<ProcessLog[]> {
    return this.http
      .get<GetProcessLogsResult>(`${this.baseUrl}/ProcessLog`, {
        params: new HttpParams().set('take', take),
        context: new HttpContext().set(SILENT_REQUEST, silent),
      })
      .pipe(
        map((result) => result.processLogs),
        catchError(rethrowAsApiError),
      );
  }
}

function rethrowAsApiError(error: unknown): Observable<never> {
  return throwError(() => toApiError(error));
}
