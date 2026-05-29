import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';

// Minimal well-formed JWT so persistTokens()/decodeUser() can parse it.
function makeJwt(): string {
  const b64url = (o: object) =>
    btoa(JSON.stringify(o)).replace(/=/g, '').replace(/\+/g, '-').replace(/\//g, '_');
  const payload = {
    sub: '11111111-1111-1111-1111-111111111111',
    email: 'a@b.c',
    firstName: 'A',
    lastName: 'B',
    role: ['Member'],
    exp: Math.floor(Date.now() / 1000) + 900
  };
  return `${b64url({ alg: 'HS256', typ: 'JWT' })}.${b64url(payload)}.sig`;
}

describe('AuthService refresh de-duplication', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const url = `${environment.apiUrl}/auth/refresh`;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigate: () => Promise.resolve(true) } }
      ]
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('fires a single /refresh for concurrent callers and shares the token', async () => {
    localStorage.setItem('tc.refreshToken', 'rt-1');

    const p1 = firstValueFrom(service.refresh());
    const p2 = firstValueFrom(service.refresh());

    // expectOne throws if there is not exactly one matching request: this is
    // the assertion that the rotating refresh token is only spent once.
    const req = httpMock.expectOne(url);
    req.flush({ accessToken: makeJwt(), refreshToken: 'rt-2' });

    const [a, b] = await Promise.all([p1, p2]);
    expect(a).toBe(b);
  });

  it('allows a new /refresh once the previous one has settled', async () => {
    localStorage.setItem('tc.refreshToken', 'rt-1');

    const p1 = firstValueFrom(service.refresh());
    httpMock.expectOne(url).flush({ accessToken: makeJwt(), refreshToken: 'rt-2' });
    await p1;

    const p2 = firstValueFrom(service.refresh());
    httpMock.expectOne(url).flush({ accessToken: makeJwt(), refreshToken: 'rt-3' });
    await p2;
  });
});
