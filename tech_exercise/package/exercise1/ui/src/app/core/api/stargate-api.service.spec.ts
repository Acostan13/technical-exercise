import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { LoadingService } from '../loading/loading.service';
import { loadingInterceptor } from '../loading/loading.interceptor';
import { API_UNREACHABLE_MESSAGE, ApiError } from './api-error';
import { StargateApiService } from './stargate-api.service';

describe('StargateApiService', () => {
  let api: StargateApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([loadingInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    api = TestBed.inject(StargateApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('retrieves an astronaut record with GET /AstronautDuty/{name}, URL-encoding the name', async () => {
    const result = firstValueFrom(api.getAstronautDuties("Clara O'Neill / Jr"));

    const request = http.expectOne(
      `/api/AstronautDuty/${encodeURIComponent("Clara O'Neill / Jr")}`,
    );
    expect(request.request.method).toBe('GET');
    request.flush({
      success: true,
      message: 'Successful',
      responseCode: 200,
      person: {
        personId: 1,
        name: "Clara O'Neill / Jr",
        currentRank: null,
        currentDutyTitle: null,
        careerStartDate: null,
        careerEndDate: null,
      },
      astronautDuties: [],
    });

    const record = await result;
    expect(record.person.name).toBe("Clara O'Neill / Jr");
    expect(record.duties).toEqual([]);
  });

  it('creates a person with a JSON body', async () => {
    const result = firstValueFrom(api.createPerson('Jane Doe'));

    const request = http.expectOne('/api/Person');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Jane Doe' });
    request.flush({ success: true, message: 'Successful', responseCode: 200, id: 7 });

    expect(await result).toBe(7);
  });

  it('updates a person with PUT /Person/{currentName}', async () => {
    const result = firstValueFrom(api.updatePerson('Jane Doe', 'Jane Smith'));

    const request = http.expectOne('/api/Person/Jane%20Doe');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({ name: 'Jane Smith' });
    request.flush({ success: true, message: 'Successful', responseCode: 200, id: 7 });

    expect(await result).toBe(7);
  });

  it('adds an astronaut duty', async () => {
    const duty = { name: 'Jane Doe', rank: '1LT', dutyTitle: 'Pilot', dutyStartDate: '2024-03-01' };
    const result = firstValueFrom(api.createAstronautDuty(duty));

    const request = http.expectOne('/api/AstronautDuty');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(duty);
    request.flush({ success: true, message: 'Successful', responseCode: 200, id: 3 });

    expect(await result).toBe(3);
  });

  it('normalises API errors, keeping the validation messages', async () => {
    const result = firstValueFrom(api.createPerson(''));

    http
      .expectOne('/api/Person')
      .flush(
        {
          success: false,
          message: 'Name is required.',
          responseCode: 400,
          errors: ['Name is required.'],
        },
        { status: 400, statusText: 'Bad Request' },
      );

    const error = await result.catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(400);
    expect((error as ApiError).details).toEqual(['Name is required.']);
  });

  it('reports an unreachable API clearly', async () => {
    const result = firstValueFrom(api.getPeople());

    http.expectOne('/api/Person').flush(null, { status: 504, statusText: 'Gateway Timeout' });

    const error = (await result.catch((e: unknown) => e)) as ApiError;
    expect(error.message).toBe(API_UNREACHABLE_MESSAGE);
  });

  it('drives the global progress indicator, except for silent (polling) requests', () => {
    const loading = TestBed.inject(LoadingService);

    api.getPeople().subscribe();
    expect(loading.isLoading()).toBe(true);
    http.expectOne('/api/Person').flush({ people: [] });
    expect(loading.isLoading()).toBe(false);

    api.getProcessLogs(50, true).subscribe();
    expect(loading.isLoading()).toBe(false);
    const poll = http.expectOne((r) => r.url === '/api/ProcessLog');
    expect(poll.request.params.get('take')).toBe('50');
    poll.flush({ processLogs: [] });
  });
});
