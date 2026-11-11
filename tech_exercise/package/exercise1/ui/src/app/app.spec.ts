import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { App } from './app';
import { LoadingService } from './core/loading/loading.service';

describe('App shell', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient()],
    }).compileComponents();
  });

  it('renders the brand and main navigation', async () => {
    const fixture = TestBed.createComponent(App);
    await fixture.whenStable();
    const element = fixture.nativeElement as HTMLElement;

    expect(element.querySelector('.brand__name')?.textContent).toContain('Stargate');
    const links = Array.from(element.querySelectorAll('nav a')).map((a) => a.textContent?.trim());
    expect(links.some((text) => text?.includes('Personnel'))).toBe(true);
    expect(links.some((text) => text?.includes('Activity'))).toBe(true);
  });

  it('shows the global progress bar only while requests are in flight', async () => {
    const fixture = TestBed.createComponent(App);
    const loading = TestBed.inject(LoadingService);
    const element = fixture.nativeElement as HTMLElement;

    await fixture.whenStable();
    expect(element.querySelector('mat-progress-bar')).toBeNull();

    loading.start();
    await fixture.whenStable();
    expect(element.querySelector('mat-progress-bar')).not.toBeNull();

    loading.stop();
    await fixture.whenStable();
    expect(element.querySelector('mat-progress-bar')).toBeNull();
  });
});
