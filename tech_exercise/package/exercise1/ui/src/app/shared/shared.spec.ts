import { astronautStatus, isRetiredTitle } from './astronaut-status';
import { addDays, describeDuration, parseApiDate, toApiDate } from './dates';

describe('dates', () => {
  it('parses API dates as local calendar dates, never shifting the day', () => {
    const date = parseApiDate('2020-01-15T00:00:00');

    expect(date.getFullYear()).toBe(2020);
    expect(date.getMonth()).toBe(0);
    expect(date.getDate()).toBe(15);
  });

  it('formats local dates for the API without converting through UTC', () => {
    // 23:30 local time must still be sent as that calendar day.
    expect(toApiDate(new Date(2024, 2, 1, 23, 30))).toBe('2024-03-01');
  });

  it('round-trips a date unchanged', () => {
    expect(toApiDate(parseApiDate('2023-12-31T00:00:00'))).toBe('2023-12-31');
  });

  it('adds days across month and year boundaries', () => {
    expect(toApiDate(addDays(new Date(2024, 0, 1), -1))).toBe('2023-12-31');
    expect(toApiDate(addDays(new Date(2024, 1, 28), 1))).toBe('2024-02-29');
  });

  it.each([
    ['2020-01-15', '2022-05-31', '2 yrs 4 mos'],
    ['2020-01-01', '2020-12-31', '1 yr'],
    ['2020-01-01', '2020-01-31', '1 mo'],
    ['2020-01-01', '2020-01-12', '12 days'],
    ['2020-01-01', '2020-01-01', '1 day'],
  ])('describes %s to %s (inclusive) as "%s"', (start, end, expected) => {
    expect(describeDuration(parseApiDate(start), parseApiDate(end))).toBe(expected);
  });
});

describe('astronaut status', () => {
  it('classifies a person with no astronaut assignment as a civilian', () => {
    expect(astronautStatus({ currentDutyTitle: null })).toBe('civilian');
  });

  it('classifies a RETIRED duty title as retired, regardless of case', () => {
    expect(astronautStatus({ currentDutyTitle: 'RETIRED' })).toBe('retired');
    expect(astronautStatus({ currentDutyTitle: 'retired' })).toBe('retired');
  });

  it('classifies any other duty as active', () => {
    expect(astronautStatus({ currentDutyTitle: 'Commander' })).toBe('active');
    expect(isRetiredTitle('Retired Commander')).toBe(false);
  });
});
