import { Routes } from '@angular/router';
import { AuthGuard } from './core/services/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent)
  },
  {
    path: 'art/:id',
    loadComponent: () =>
      import('./features/art-detail/art-detail.component').then((m) => m.ArtDetailComponent),
    data: {
      titleKey: 'nav.artForSale'
    }
  },
  {
    path: 'art',
    loadComponent: () =>
      import('./features/art-for-sale/art-for-sale.component').then((m) => m.ArtForSaleComponent),
    data: {
      titleKey: 'nav.artForSale'
    }
  },
  {
    path: 'auctions',
    loadComponent: () =>
      import('./features/placeholder/placeholder.component').then((m) => m.PlaceholderComponent),
    data: {
      titleKey: 'nav.auctions'
    }
  },
  {
    path: 'profile',
    loadComponent: () =>
      import('./features/profile/profile.component').then((m) => m.ProfileComponent),
    canActivate: [AuthGuard],
    data: {
      titleKey: 'nav.profile'
    }
  },
  {
    path: 'sell',
    loadComponent: () =>
      import('./features/placeholder/placeholder.component').then((m) => m.PlaceholderComponent),
    data: {
      titleKey: 'nav.sellArt',
      descriptionKey: 'placeholder.sellArt'
    }
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/placeholder/placeholder.component').then((m) => m.PlaceholderComponent),
    data: {
      titleKey: 'nav.register',
      descriptionKey: 'placeholder.register'
    }
  },
  {
    path: 'admin',
    loadComponent: () =>
      import('./features/placeholder/placeholder.component').then((m) => m.PlaceholderComponent),
    data: {
      titleKey: 'nav.admin',
      descriptionKey: 'placeholder.admin'
    }
  },
  {
    path: '**',
    redirectTo: ''
  }
];
