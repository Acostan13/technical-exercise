import { filterPeople, PersonRow } from './people-page';

const row = (
  name: string,
  status: PersonRow['status'],
  rank: string | null,
  title: string | null,
): PersonRow => ({
  personId: 0,
  name,
  status,
  currentRank: rank,
  currentDutyTitle: title,
  careerStartDate: null,
  careerEndDate: null,
  careerStart: null,
  careerEnd: null,
});

describe('filterPeople', () => {
  const people = [
    row('Avery Lindqvist', 'active', 'MAJ', 'Commander'),
    row('Jordan Okafor', 'retired', 'LTC', 'RETIRED'),
    row('Samuel Brooks', 'civilian', null, null),
  ];

  it('returns everyone with no search or status filter', () => {
    expect(filterPeople(people, '', 'all')).toHaveLength(3);
  });

  it('matches name, rank or duty title, case-insensitively', () => {
    expect(filterPeople(people, 'okafor', 'all').map((p) => p.name)).toEqual(['Jordan Okafor']);
    expect(filterPeople(people, 'maj', 'all').map((p) => p.name)).toEqual(['Avery Lindqvist']);
    expect(filterPeople(people, 'COMMANDER', 'all').map((p) => p.name)).toEqual([
      'Avery Lindqvist',
    ]);
  });

  it('filters by status', () => {
    expect(filterPeople(people, '', 'civilian').map((p) => p.name)).toEqual(['Samuel Brooks']);
  });

  it('combines the search with the status filter', () => {
    expect(filterPeople(people, 'avery', 'retired')).toHaveLength(0);
  });
});
