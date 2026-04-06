import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  {
    path: 'patients/:id/edit',
    renderMode: RenderMode.Server
  },
  {
    path: 'patients/:id',
    renderMode: RenderMode.Server
  },
  {
    path: 'appointments/:id/edit',
    renderMode: RenderMode.Server
  },
  {
    path: 'appointments/:id',
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
    path: '**',
    renderMode: RenderMode.Prerender
  }
];
