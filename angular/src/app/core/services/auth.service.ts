import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, Subscription } from 'rxjs';
import { map, tap, delay, finalize } from 'rxjs/operators';
import { ApplicationUser } from '../models/application-user';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}api/account`;
  private timer: Subscription;
  private _user = new BehaviorSubject<ApplicationUser>(null);
  public user$: Observable<ApplicationUser> = this._user.asObservable();

  constructor(private router: Router, private http: HttpClient) {}

  public get currentUser(): ApplicationUser {
    return this._user.value;
  }

  login(username: string, password: string) {
    return this.http
      .post<ApplicationUser>(`${this.apiUrl}/login`, { username, password })
      .pipe(
        map((user) => {
          this._user.next(user);
          this.setLocalStorage(user);
          this.startTokenTimer();
          return user;
        })
      );
  }

  logout() {
    this.http
      .post<unknown>(`${this.apiUrl}/logout`, {})
      .pipe(
        finalize(() => {
          this.clearLocalStorage();
          this.stopTokenTimer();
          this._user.next(null);
          this.router.navigate(['']);
        })
      )
      .subscribe();
  }

  refreshToken() {
    const refreshToken = localStorage.getItem('refresh_token');
    if (!refreshToken) {
      this.clearLocalStorage();
      return of(null);
    }

    return this.http
      .post<ApplicationUser>(`${this.apiUrl}/refresh-token`, { refreshToken })
      .pipe(
        map((user) => {
          this._user.next(user);
          this.setLocalStorage(user);
          this.startTokenTimer();
          return user;
        })
      );
  }

  setLocalStorage(user: ApplicationUser) {
    localStorage.setItem('access_token', user.accessToken);
    localStorage.setItem('refresh_token', user.refreshToken);
  }

  clearLocalStorage() {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  }

  private getTokenRemainingTime() {
    const accessToken = localStorage.getItem('access_token');
    if (!accessToken) {
      return 0;
    }
    const jwtToken = JSON.parse(atob(accessToken.split('.')[1]));
    const expires = new Date(jwtToken.exp * 1000);
    return expires.getTime() - Date.now();
  }

  private startTokenTimer() {
    const timeout = this.getTokenRemainingTime();
    this.timer = of(true)
      .pipe(
        delay(timeout),
        tap(() => this.refreshToken().subscribe())
      )
      .subscribe();
  }

  private stopTokenTimer() {
    this.timer?.unsubscribe();
  }
}
