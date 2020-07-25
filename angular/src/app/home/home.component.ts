import { Component, OnInit } from '@angular/core';
import { AuthService } from '../core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'],
})
export class HomeComponent implements OnInit {
  accessToken = '';
  refreshToken = '';

  constructor(public authService: AuthService) {}

  ngOnInit(): void {
    this.accessToken = localStorage.getItem('access_token');
    this.refreshToken = localStorage.getItem('refresh_token');
  }
}
