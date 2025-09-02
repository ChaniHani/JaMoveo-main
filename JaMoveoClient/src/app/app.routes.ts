import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { SignupComponent } from './components/signup/signup.component';
import { MainComponent } from './components/main/main.component';
import { authGuard } from './guards/auth.guard';
import { ResultsComponent } from './components/results/results.component';
import { adminGuard } from './guards/admin.guard';
import { LiveComponent } from './components/live/live.component';

export const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'signup', component: SignupComponent },
  { path: 'signup-admin', component: SignupComponent },
  { path: 'main', component: MainComponent, canActivate: [authGuard] },
  { path: 'results', component: ResultsComponent, canActivate: [adminGuard] },
  { path: 'live', component: LiveComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '/login' }
];
