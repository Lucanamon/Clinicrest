import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  {
    path: 'patients/:id',
    renderMode: RenderMode.Server
  },
  {
    path: 'request/:id/edit',
    renderMode: RenderMode.Server
  },
  {
    path: 'request/:id',
    renderMode: RenderMode.Server
  },
  {
    path: 'backlog/:id/edit',
    renderMode: RenderMode.Server
  },
  {
    path: 'backlog/:id',
    renderMode: RenderMode.Server
  },
  {
    path: 'users',
    renderMode: RenderMode.Server
  },
  {
    path: 'users/new',
    renderMode: RenderMode.Server
  },
  {
    path: 'audit-logs',
    renderMode: RenderMode.Server
  },
  {
    path: 'schedule',
    renderMode: RenderMode.Client
  },
  {
    path: 'booking',
    renderMode: RenderMode.Client
  },
  {
    path: 'register',
    renderMode: RenderMode.Client
  },
  {
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
