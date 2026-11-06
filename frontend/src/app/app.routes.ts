import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { PurchaseBill } from './pages/purchase-bill/purchase-bill';
import { authGuard } from './guards/auth-guard';
import { ProductsComponent } from './pages/products/products';
import { DashboardComponent } from './pages/dashboard/dashboard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    component: Login
  },
  {
    path: 'purchase-bill',
    component: PurchaseBill,
    canActivate: [authGuard]
  },

  {
  path: 'products-practice',
  component: ProductsComponent
  },

  {
  path: 'dashboard',
  component: DashboardComponent,
  canActivate: [authGuard]
  },

  {
    path: '**',
    redirectTo: 'login'
  }
];
