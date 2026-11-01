import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { switchMap } from 'rxjs';

import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {

  private http = inject(HttpClient);
  private router = inject(Router);

  showPassword = false;

  // Declare the signal here, inside the class
  message = signal('');

  loginForm = new FormGroup({
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email]
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    })
  });

  
  
  onLogin(): void {
    this.message.set('');

    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    const loginData = this.loginForm.getRawValue();

    this.message.set('Logging in...');

    // Obtain a CSRF token before logging in
    this.http.get('/api/auth/csrf', {
      responseType: 'text'
    }).pipe(

      // Authenticate using our existing backend
      switchMap(() =>
        this.http.post<{
          message: string;
          locationCount: number;
        }>('/api/auth/login', loginData)
      ),

      // Refresh the token for the authenticated user
      switchMap(() =>
        this.http.get('/api/auth/csrf', {
          responseType: 'text'
        })
      )

    ).subscribe({
      next: () => {
        this.router.navigate(['/purchase-bill']);
      },

      error: (error) => {
        console.error('Login failed:', error.status);

        this.message.set(
          error.error?.message ||
          `Login failed. HTTP Status: ${error.status}`
        );
      }
    });
  }
}
