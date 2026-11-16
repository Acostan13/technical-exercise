import { formatDate } from '@angular/common';

import { AstronautDuty } from '../../core/api/models';
import { AstronautRecord } from '../../core/api/stargate-api.service';
import { isRetiredTitle } from '../../shared/astronaut-status';
import { describeDuration, parseApiDate } from '../../shared/dates';

export interface DutyView {
  id: number;
  rank: string;
  dutyTitle: string;
  start: Date;
  end: Date | null;
  isCurrent: boolean;
  isRetirement: boolean;
  /** Length of the duty; for the current duty, measured up to `today`. */
  duration: string;
}

export function toDutyViews(duties: AstronautDuty[], today = new Date()): DutyView[] {
  return duties.map((duty) => {
    const start = parseApiDate(duty.dutyStartDate);
    const end = duty.dutyEndDate ? parseApiDate(duty.dutyEndDate) : null;
    const isRetirement = isRetiredTitle(duty.dutyTitle);

    return {
      id: duty.id,
      rank: duty.rank,
      dutyTitle: duty.dutyTitle,
      start,
      end,
      isCurrent: end === null,
      isRetirement,
      duration:
        isRetirement && end === null
          ? ''
          : describeDuration(start, end ?? (today < start ? start : today)),
    };
  });
}

/**
 * Explains, in plain language, what the business rules did when a duty was added, by comparing the
 * astronaut's record before and after. This is the "result" shown to the user once the process completes.
 */
export function describeDutyChanges(
  before: AstronautRecord,
  after: AstronautRecord,
  newDutyId: number,
): string[] {
  const added = after.duties.find((d) => d.id === newDutyId);
  if (!added) {
    return [];
  }

  const changes: string[] = [];
  const format = (value: string) => formatDate(parseApiDate(value), 'mediumDate', 'en-US');

  changes.push(
    `${added.dutyTitle} (${added.rank}) is now the current duty, starting ${format(added.dutyStartDate)}.`,
  );

  const previous = before.duties.find((d) => d.dutyEndDate === null);
  const closed = previous && after.duties.find((d) => d.id === previous.id);
  if (closed?.dutyEndDate) {
    changes.push(
      `Previous duty ${closed.dutyTitle} ended ${format(closed.dutyEndDate)}, the day before.`,
    );
  }

  if (!before.person.careerStartDate && after.person.careerStartDate) {
    changes.push(
      `First astronaut assignment: career started ${format(after.person.careerStartDate)}.`,
    );
  }

  if (after.person.careerEndDate && after.person.careerEndDate !== before.person.careerEndDate) {
    changes.push(`Retired: career ended ${format(after.person.careerEndDate)}.`);
  } else if (before.person.careerEndDate && !after.person.careerEndDate) {
    changes.push('Returned from retirement: career end date cleared.');
  }

  return changes;
}
