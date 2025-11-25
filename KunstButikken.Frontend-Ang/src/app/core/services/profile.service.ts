import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { UserProfile } from '../models/user-profile.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ProfileService {

  private get apiBase() {
    return (environment.apis?.gateway || '').replace(/\/$/, '');
  }

  constructor(private http: HttpClient) {
    console.log('[ProfileService] Initialized');
    console.log('[ProfileService] Environment:', environment);
    console.log('[ProfileService] APIs configured:', environment.apis);
  }

  getProfile(): Observable<UserProfile> {
    const endpoint = `${this.apiBase}/api/profile/me`;
    console.log('[ProfileService] GET profile from:', endpoint);
    console.log('[ProfileService] Full URL will be:', endpoint);

    return this.http.get<UserProfile>(endpoint).pipe(
      catchError((error) => {
        console.error('[ProfileService] Failed to fetch profile:', error);
        console.error('[ProfileService] Error status:', error.status);
        console.error('[ProfileService] Error message:', error.message);
        console.error('[ProfileService] Error URL:', error.url);

        // If status is 0, it's likely a CORS or network issue
        if (error.status === 0) {
          console.error('[ProfileService] Status 0 indicates:');
          console.error('  - Service may not be running');
          console.error('  - CORS configuration issue');
          console.error('  - Network connectivity problem');
          console.error('  - URL:', endpoint);
          console.error('[ProfileService] Check if UserService is running and accessible');
        }

        throw error;
      })
    );
  }

  updateProfile(profile: UserProfile): Observable<void> {
    const endpoint = `${this.apiBase}/api/profile`;
    console.log('[ProfileService] PUT profile to:', endpoint);
    return this.http.put<void>(endpoint, profile);
  }
}
