import { Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { EInstrument } from '../../models/user.model';

@Component({
  selector: 'app-signup',
  imports: [ReactiveFormsModule],
  templateUrl: './signup.component.html',
  styleUrls: ['./signup.component.scss']
})
export class SignupComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  signupForm!: FormGroup;
  instruments = Object.keys(EInstrument).filter(key => isNaN(Number(key)));
  isAdmin = false;
  loading = false;
  error = '';

  ngOnInit(): void {
    this.signupForm = this.formBuilder.group({
      username: ['', [Validators.required, Validators.minLength(3)]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      instrument: ['', Validators.required]
    });
    this.isAdmin = this.route.snapshot.url.some(segment => segment.path === 'signup-admin');
  }

  onSubmit(): void {
    if (this.signupForm.invalid) return;

    this.loading = true;
    this.error = '';

    const { username, password, instrument } = this.signupForm.value;
    const userData = {
      username,
      password,
      instrument: EInstrument[instrument as keyof typeof EInstrument]
    };

    const signupMethod = this.isAdmin
      ? this.authService.signupAdmin(userData)
      : this.authService.signup(userData);

    signupMethod.subscribe({
      next: () => this.router.navigate(['/login']),
      error: (error) => {
        this.error = error.error?.message || 'Signup failed';
        this.loading = false;
      }
    });
  }

  navigate() {
    this.router.navigate(['/login']);
  }
}
