// Seeds the Stargate API with fictional demo data, through the public API so every business rule applies.
// Usage: npm run seed            (API at http://localhost:5204)
//        API_URL=http://host:port npm run seed

const apiUrl = (process.env.API_URL ?? 'http://localhost:5204').replace(/\/$/, '');

const people = [
  {
    name: 'Avery Lindqvist',
    duties: [
      ['2LT', 'Flight Engineer', '2012-03-01'],
      ['1LT', 'Mission Specialist', '2015-07-15'],
      ['CPT', 'Pilot', '2018-02-01'],
      ['MAJ', 'Commander', '2021-09-01'],
    ],
  },
  {
    name: 'Jordan Okafor',
    duties: [
      ['1LT', 'Payload Specialist', '2008-05-12'],
      ['CPT', 'Mission Specialist', '2011-01-03'],
      ['MAJ', 'Commander', '2016-04-18'],
      ['LTC', 'RETIRED', '2023-06-01'],
    ],
  },
  {
    name: 'Mei Tanaka',
    duties: [
      ['2LT', 'Flight Surgeon', '2019-08-20'],
      ['1LT', 'Science Officer', '2022-01-10'],
    ],
  },
  {
    name: 'Rafael Moreno',
    duties: [
      ['CPT', 'Pilot', '2010-10-01'],
      ['MAJ', 'Commander', '2014-03-15'],
      ['MAJ', 'RETIRED', '2020-12-01'],
    ],
  },
  { name: 'Priya Raman', duties: [['2LT', 'Mission Specialist', '2024-02-05']] },
  { name: 'Samuel Brooks', duties: [] },
  { name: "Clara O'Neill", duties: [] },
];

async function post(path, body) {
  const response = await fetch(`${apiUrl}${path}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });
  const result = await response.json().catch(() => ({}));
  return { ok: response.ok, status: response.status, message: result.message };
}

try {
  await fetch(`${apiUrl}/Person`);
} catch {
  console.error(
    `Cannot reach the Stargate API at ${apiUrl}. Start it with 'dotnet run --project api' first.`,
  );
  process.exit(1);
}

for (const person of people) {
  const created = await post('/Person', { name: person.name });
  console.log(
    `${created.ok ? '+' : '·'} ${person.name}${created.ok ? '' : ` (${created.message})`}`,
  );

  for (const [rank, dutyTitle, dutyStartDate] of person.duties) {
    const duty = await post('/AstronautDuty', {
      name: person.name,
      rank,
      dutyTitle,
      dutyStartDate,
    });
    console.log(
      `    ${duty.ok ? '+' : '·'} ${dutyStartDate} ${dutyTitle} (${rank})${duty.ok ? '' : ` - ${duty.message}`}`,
    );
  }
}

console.log(
  '\nDone. Re-running is safe: existing people and duties are rejected by the API and skipped.',
);
