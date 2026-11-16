/** Mirrors the Stargate API response contracts. Dates are ISO strings as returned by the API. */

export interface BaseResponse {
  success: boolean;
  message: string;
  responseCode: number;
}

export interface ValidationErrorResponse extends BaseResponse {
  errors: string[];
}

/** A person with their current astronaut information (null when they have never had an assignment). */
export interface PersonAstronaut {
  personId: number;
  name: string;
  currentRank: string | null;
  currentDutyTitle: string | null;
  careerStartDate: string | null;
  careerEndDate: string | null;
}

export interface AstronautDuty {
  id: number;
  personId: number;
  rank: string;
  dutyTitle: string;
  dutyStartDate: string;
  dutyEndDate: string | null;
}

export type ProcessLogLevel = 'Information' | 'Warning' | 'Error';

export interface ProcessLog {
  id: number;
  timestampUtc: string;
  level: ProcessLogLevel;
  requestName: string;
  succeeded: boolean;
  message: string;
  requestData: string | null;
  exceptionType: string | null;
  exceptionDetail: string | null;
  durationMs: number;
}

export interface GetPeopleResult extends BaseResponse {
  people: PersonAstronaut[];
}

export interface GetPersonByNameResult extends BaseResponse {
  person: PersonAstronaut;
}

export interface GetAstronautDutiesByNameResult extends BaseResponse {
  person: PersonAstronaut;
  astronautDuties: AstronautDuty[];
}

export interface IdResult extends BaseResponse {
  id: number;
}

export interface GetProcessLogsResult extends BaseResponse {
  processLogs: ProcessLog[];
}

export interface CreateAstronautDutyRequest {
  name: string;
  rank: string;
  dutyTitle: string;
  /** Date only, formatted yyyy-MM-dd. */
  dutyStartDate: string;
}
