import { TestBed } from '@angular/core/testing';

import { DutyTimeline } from './duty-timeline';
import { DutyView } from './duty-view';

describe('DutyTimeline', () => {
  const duties: DutyView[] = [
    {
      id: 2,
      rank: 'MAJ',
      dutyTitle: 'Commander',
      start: new Date(2022, 5, 1),
      end: null,
      isCurrent: true,
      isRetirement: false,
      duration: '1 yr',
    },
    {
      id: 1,
      rank: 'CPT',
      dutyTitle: 'Pilot',
      start: new Date(2020, 0, 15),
      end: new Date(2022, 4, 31),
      isCurrent: false,
      isRetirement: false,
      duration: '2 yrs 4 mos',
    },
  ];

  async function render(highlightId: number | null = null): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(DutyTimeline);
    fixture.componentRef.setInput('duties', duties);
    fixture.componentRef.setInput('highlightId', highlightId);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('renders each duty in order with its rank and date range', async () => {
    const element = await render();
    const entries = element.querySelectorAll('li');

    expect(entries.length).toBe(2);
    expect(entries[0].textContent).toContain('Commander');
    expect(entries[0].textContent).toContain('MAJ');
    expect(entries[0].textContent).toContain('Present');
    expect(entries[1].textContent).toContain('Jan 15, 2020');
    expect(entries[1].textContent).toContain('May 31, 2022');
    expect(entries[1].textContent).toContain('2 yrs 4 mos');
  });

  it('marks the current duty and highlights a newly added one', async () => {
    const element = await render(2);
    const [current, previous] = Array.from(element.querySelectorAll('li'));

    expect(current.classList).toContain('entry--current');
    expect(current.classList).toContain('entry--new');
    expect(current.textContent).toContain('Current');
    expect(previous.classList).not.toContain('entry--current');
  });

  it('exposes machine-readable dates', async () => {
    const element = await render();

    expect(element.querySelector('time')?.getAttribute('datetime')).toBe('2022-06-01');
  });
});
