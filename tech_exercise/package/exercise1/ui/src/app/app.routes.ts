import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'people' },
  {
    path: 'people',
    title: 'Personnel · Stargate ACTS',
    loadComponent: () => import('./features/people/people-page').then((m) => m.PeoplePage),
  },
  {
    path: 'people/:name',
    title: 'Astronaut · Stargate ACTS',
    loadComponent: () => import('./features/astronaut/astronaut-page').then((m) => m.AstronautPage),
  },
  {
    path: 'activity',
    title: 'Activity · Stargate ACTS',
    loadComponent: () => import('./features/activity/activity-page').then((m) => m.ActivityPage),
  },
  { path: '**', redirectTo: 'people' },
];
