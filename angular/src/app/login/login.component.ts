import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../core';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
})
export class LoginComponent implements OnInit {
  busy = false;
  username = '';
  password = '';
  loginError = false;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService
  ) {
    if (this.authService.currentUser) {
      this.router.navigate(['']);
    }
  }

  ngOnInit(): void {}

  login() {
    if (!this.username || !this.password) {
      return;
    }
    this.busy = true;
    const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '';
    this.authService
      .login(this.username, this.password)
      .pipe(finalize(() => (this.busy = false)))
      .subscribe(
        () => {
          this.router.navigate([returnUrl]);
        },
        () => {
          this.loginError = true;
        }
      );
  }
}
