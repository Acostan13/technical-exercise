/**
 * The API stores calendar dates without a time zone (e.g. "2020-01-15T00:00:00"). They are parsed and
 * formatted as local calendar dates: converting through UTC (toISOString / Date.parse of a 'Z' string)
 * would shift them by a day for users west or east of UTC.
 */

/** Parses the date part of an API date string as a local calendar date. */
export function parseApiDate(value: string): Date {
  const [year, month, day] = value.slice(0, 10).split('-').map(Number);
  return new Date(year, month - 1, day);
}

/** Formats a local calendar date as yyyy-MM-dd for the API. */
export function toApiDate(date: Date): string {
  const yyyy = date.getFullYear();
  const mm = String(date.getMonth() + 1).padStart(2, '0');
  const dd = String(date.getDate()).padStart(2, '0');
  return `${yyyy}-${mm}-${dd}`;
}

export function addDays(date: Date, days: number): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate() + days);
}

/** Human-readable length of a period, e.g. "2 yrs 4 mos", "3 mos", "12 days". End is inclusive. */
export function describeDuration(start: Date, end: Date): string {
  const endExclusive = addDays(end, 1);
  let months =
    (endExclusive.getFullYear() - start.getFullYear()) * 12 +
    (endExclusive.getMonth() - start.getMonth());
  if (endExclusive.getDate() < start.getDate()) {
    months--;
  }

  if (months < 1) {
    const days = Math.max(1, Math.round((endExclusive.getTime() - start.getTime()) / 86_400_000));
    return `${days} ${days === 1 ? 'day' : 'days'}`;
  }

  const years = Math.floor(months / 12);
  const remainder = months % 12;
  const parts: string[] = [];
  if (years > 0) {
    parts.push(`${years} ${years === 1 ? 'yr' : 'yrs'}`);
  }
  if (remainder > 0) {
    parts.push(`${remainder} ${remainder === 1 ? 'mo' : 'mos'}`);
  }
  return parts.join(' ');
}
