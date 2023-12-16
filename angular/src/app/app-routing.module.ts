import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { authGuard } from './core';
import { HomeComponent } from './home/home.component';
import { LoginComponent } from './login/login.component';
import { DemoApisComponent } from './demo-apis/demo-apis.component';

const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: HomeComponent,
    canMatch: [authGuard],
  },
  { path: 'login', component: LoginComponent },
  { path: 'demo-apis', component: DemoApisComponent, canMatch: [authGuard] },
  {
    path: 'management',
    loadChildren: () =>
      import('./management/management.module').then((m) => m.ManagementModule),
    canMatch: [authGuard],
  },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
