import { AuthService } from './auth.service';

export function appInitializer(authService: AuthService) {
  return () =>
    new Promise((resolve) => {
      console.log('refresh token on app start up')
      authService.refreshToken().subscribe().add(resolve);
    });
}
