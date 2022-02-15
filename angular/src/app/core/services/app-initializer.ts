import { Observable } from 'rxjs';
import { AuthService } from './auth.service';

export function appInitializer(
  authService: AuthService
): () => Observable<any> {
  return () => authService.refreshToken();
}
