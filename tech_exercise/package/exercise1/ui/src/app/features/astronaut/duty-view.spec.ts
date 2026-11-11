import { AstronautDuty, PersonAstronaut } from '../../core/api/models';
import { AstronautRecord } from '../../core/api/stargate-api.service';
import { describeDutyChanges, toDutyViews } from './duty-view';

const person = (overrides: Partial<PersonAstronaut> = {}): PersonAstronaut => ({
  personId: 1,
  name: 'Avery Lindqvist',
  currentRank: null,
  currentDutyTitle: null,
  careerStartDate: null,
  careerEndDate: null,
  ...overrides,
});

const duty = (
  id: number,
  dutyTitle: string,
  start: string,
  end: string | null = null,
): AstronautDuty => ({
  id,
  personId: 1,
  rank: 'CPT',
  dutyTitle,
  dutyStartDate: `${start}T00:00:00`,
  dutyEndDate: end ? `${end}T00:00:00` : null,
});

describe('toDutyViews', () => {
  it('marks the open-ended duty as current and measures it up to today', () => {
    const [view] = toDutyViews([duty(1, 'Pilot', '2020-01-01')], new Date(2021, 0, 1));

    expect(view.isCurrent).toBe(true);
    expect(view.end).toBeNull();
    expect(view.duration).toBe('1 yr');
  });

  it('flags a current RETIRED duty as a retirement without a running duration', () => {
    const [view] = toDutyViews([duty(2, 'RETIRED', '2023-06-01')]);

    expect(view.isRetirement).toBe(true);
    expect(view.duration).toBe('');
  });
});

describe('describeDutyChanges', () => {
  it('explains a first assignment', () => {
    const before: AstronautRecord = { person: person(), duties: [] };
    const after: AstronautRecord = {
      person: person({ currentDutyTitle: 'Pilot', careerStartDate: '2020-01-15T00:00:00' }),
      duties: [duty(1, 'Pilot', '2020-01-15')],
    };

    expect(describeDutyChanges(before, after, 1)).toEqual([
      'Pilot (CPT) is now the current duty, starting Jan 15, 2020.',
      'First astronaut assignment: career started Jan 15, 2020.',
    ]);
  });

  it('explains that the previous duty was closed the day before', () => {
    const before: AstronautRecord = {
      person: person({ careerStartDate: '2020-01-15T00:00:00' }),
      duties: [duty(1, 'Pilot', '2020-01-15')],
    };
    const after: AstronautRecord = {
      person: person({ careerStartDate: '2020-01-15T00:00:00' }),
      duties: [duty(2, 'Commander', '2022-06-01'), duty(1, 'Pilot', '2020-01-15', '2022-05-31')],
    };

    expect(describeDutyChanges(before, after, 2)).toContain(
      'Previous duty Pilot ended May 31, 2022, the day before.',
    );
  });

  it('explains a retirement and the resulting career end date', () => {
    const before: AstronautRecord = {
      person: person({ careerStartDate: '2020-01-15T00:00:00' }),
      duties: [duty(1, 'Pilot', '2020-01-15')],
    };
    const after: AstronautRecord = {
      person: person({
        careerStartDate: '2020-01-15T00:00:00',
        careerEndDate: '2023-05-31T00:00:00',
      }),
      duties: [duty(2, 'RETIRED', '2023-06-01'), duty(1, 'Pilot', '2020-01-15', '2023-05-31')],
    };

    expect(describeDutyChanges(before, after, 2)).toContain('Retired: career ended May 31, 2023.');
  });

  it('explains a return from retirement', () => {
    const before: AstronautRecord = {
      person: person({
        careerStartDate: '2020-01-15T00:00:00',
        careerEndDate: '2023-05-31T00:00:00',
      }),
      duties: [duty(2, 'RETIRED', '2023-06-01')],
    };
    const after: AstronautRecord = {
      person: person({ careerStartDate: '2020-01-15T00:00:00' }),
      duties: [
        duty(3, 'Flight Director', '2024-01-01'),
        duty(2, 'RETIRED', '2023-06-01', '2023-12-31'),
      ],
    };

    expect(describeDutyChanges(before, after, 3)).toContain(
      'Returned from retirement: career end date cleared.',
    );
  });

  it('returns nothing when the new duty is not in the refreshed record', () => {
    const record: AstronautRecord = { person: person(), duties: [] };

    expect(describeDutyChanges(record, record, 99)).toEqual([]);
  });
});
