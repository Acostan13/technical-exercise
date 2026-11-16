import { PersonAstronaut } from '../core/api/models';

export const RETIRED_DUTY_TITLE = 'RETIRED';

/**
 * - active:   currently serving in an astronaut duty
 * - retired:  current duty title is RETIRED (README rule)
 * - civilian: has never had an astronaut assignment, so has no astronaut records
 */
export type AstronautStatus = 'active' | 'retired' | 'civilian';

export function astronautStatus(
  person: Pick<PersonAstronaut, 'currentDutyTitle'>,
): AstronautStatus {
  if (!person.currentDutyTitle) {
    return 'civilian';
  }

  return isRetiredTitle(person.currentDutyTitle) ? 'retired' : 'active';
}

export function isRetiredTitle(dutyTitle: string): boolean {
  return dutyTitle.trim().toUpperCase() === RETIRED_DUTY_TITLE;
}

export const STATUS_LABELS: Record<AstronautStatus, string> = {
  active: 'Active astronaut',
  retired: 'Retired',
  civilian: 'No assignments',
};
