# Auth-Flow

End-to-end Authentifizierung und Autorisierung. Backend (ASP.NET Identity + JWT) und Frontend (Angular).

## Grundsatzentscheidungen

1. **Access Token (JWT, 15 Minuten) + Refresh Token (7 Tage, rotierend)** — kurze Access Tokens begrenzen Schaden bei Diebstahl; Refresh Token wird bei jeder Nutzung durch neuen ersetzt
2. **Access Token in `localStorage`** — pragmatisch; httpOnly-Cookies wären sicherer gegen XSS, erfordern aber SameSite=None + CSRF-Tokens wegen getrennter Subdomains
3. **Kein Self-Service-Registration** — Admin legt Mitglieder an, diese erhalten Welcome-Mail mit Passwort-Setzen-Link
4. **Rollen via ASP.NET Identity** — `Member`, `Trainer`, `Admin`

## Flow-Übersicht

**Login:**
1. Angular POSTet `{email, password}` an `/api/auth/login`
2. API validiert → erstellt Access Token + Refresh Token → speichert SHA-256-Hash des Refresh Tokens in DB
3. Antwort: `{accessToken, refreshToken}`
4. Angular speichert beide in `localStorage`, setzt User-State (Signal)
5. Bei API-Calls: Interceptor hängt `Authorization: Bearer <token>` an

**Auto-Refresh bei 401:**
1. Interceptor fängt 401 ab
2. Ruft `/api/auth/refresh` mit aktuellem Refresh Token
3. API validiert Hash → revoked alten Token → erstellt neuen Access + Refresh
4. Interceptor wiederholt ursprünglichen Request mit neuem Access Token

**Passwort-Reset (auch für First-Login nach Einladung):**
1. User POSTet Email an `/api/auth/forgot-password`
2. API generiert `PasswordResetToken` via `UserManager` (hat begrenzte Lebenszeit)
3. Mail mit Link `/set-password?token=...` an User
4. User klickt Link → Angular-Screen mit neuem Passwort
5. POST an `/api/auth/reset-password` mit Token + neuem Passwort
6. API ruft `UserManager.ResetPasswordAsync` auf

## Backend

### Program.cs (relevante Zeilen)

```csharp
// Identity ohne Cookies (JWT statt Cookies)
builder.Services.AddIdentityCore<Member>(opts =>
    {
        opts.Password.RequiredLength = 8;
        opts.User.RequireUniqueEmail = true;
        opts.SignIn.RequireConfirmedEmail = false;
        opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        opts.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
builder.Services.AddSingleton(jwt);
builder.Services.AddScoped<JwtTokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("Admin", p => p.RequireRole("Admin"));
    opts.AddPolicy("TrainerOrAdmin", p => p.RequireRole("Trainer", "Admin"));
});
```

`JwtSettings` (`Issuer`, `Audience`, `SigningKey`, `AccessTokenMinutes`, `RefreshTokenDays`) kommt aus `appsettings.json` in Dev, aus Container App Secrets in Produktion.

### JwtTokenService

```csharp
public sealed class JwtTokenService(JwtSettings settings, TimeProvider time)
{
    public string CreateAccessToken(Member member, IEnumerable<string> roles)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, member.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, member.Email!),
            new("firstName", member.FirstName),
            new("lastName", member.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var token = new JwtSecurityToken(
            settings.Issuer, settings.Audience, claims,
            notBefore: now,
            expires: now.AddMinutes(settings.AccessTokenMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public static string Hash(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
```

### LoginHandler

```csharp
public sealed class LoginHandler(
    UserManager<Member> users, AppDbContext db,
    JwtTokenService jwt, JwtSettings settings, TimeProvider time)
{
    public async Task<Result<AuthResponse>> HandleAsync(
        LoginRequest req, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(req.Email);
        if (user is null || !user.IsActive
            || !await users.CheckPasswordAsync(user, req.Password))
        {
            // Absichtlich generisch - Enumeration-Schutz
            return Result.Unauthorized("Login fehlgeschlagen.");
        }

        var roles = await users.GetRolesAsync(user);
        var access = jwt.CreateAccessToken(user, roles);
        var refresh = jwt.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            MemberId = user.Id,
            TokenHash = JwtTokenService.Hash(refresh),
            ExpiresAt = time.GetUtcNow().AddDays(settings.RefreshTokenDays),
            CreatedAt = time.GetUtcNow()
        });
        await db.SaveChangesAsync(ct);

        return Result.Success(new AuthResponse(access, refresh));
    }
}
```

### RefreshHandler (mit Token-Rotation)

```csharp
public sealed class RefreshHandler(
    AppDbContext db, UserManager<Member> users,
    JwtTokenService jwt, JwtSettings settings, TimeProvider time)
{
    public async Task<Result<AuthResponse>> HandleAsync(
        RefreshRequest req, CancellationToken ct)
    {
        var hash = JwtTokenService.Hash(req.RefreshToken);
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        var now = time.GetUtcNow();

        if (rt is null || rt.RevokedAt is not null || rt.ExpiresAt <= now)
            return Result.Unauthorized();

        // Alten Token sofort revoken
        rt.RevokedAt = now;

        var user = await users.FindByIdAsync(rt.MemberId.ToString());
        if (user is null || !user.IsActive) return Result.Unauthorized();

        var roles = await users.GetRolesAsync(user);
        var newAccess = jwt.CreateAccessToken(user, roles);
        var newRefresh = jwt.CreateRefreshToken();

        var replacement = new RefreshToken
        {
            Id = Guid.NewGuid(),
            MemberId = user.Id,
            TokenHash = JwtTokenService.Hash(newRefresh),
            ExpiresAt = now.AddDays(settings.RefreshTokenDays),
            CreatedAt = now
        };
        rt.ReplacedByTokenId = replacement.Id;
        db.RefreshTokens.Add(replacement);
        await db.SaveChangesAsync(ct);

        return Result.Success(new AuthResponse(newAccess, newRefresh));
    }
}
```

### Endpoints

Features-Slice `Features/Auth/`:

- `POST /api/auth/login` — LoginHandler
- `POST /api/auth/refresh` — RefreshHandler
- `POST /api/auth/logout` — revoked alle Refresh Tokens des Users
- `POST /api/auth/forgot-password` — sendet Reset-Mail via `UserManager.GeneratePasswordResetTokenAsync`
- `POST /api/auth/reset-password` — validiert Token und setzt Passwort via `UserManager.ResetPasswordAsync`

## Frontend

### AuthService mit Signals

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  readonly currentUser = signal<CurrentUser | null>(this.readInitialState());
  readonly isLoggedIn = computed(() => this.currentUser() !== null);
  readonly isAdmin = computed(() =>
    this.currentUser()?.roles.includes('Admin') ?? false);

  login(email: string, password: string): Observable<void> {
    return this.http.post<AuthResponse>('/api/auth/login', { email, password }).pipe(
      tap(res => this.persistTokens(res)),
      map(() => void 0)
    );
  }

  refresh(): Observable<string> {
    const token = localStorage.getItem('refreshToken');
    if (!token) return throwError(() => new Error('no_refresh_token'));

    return this.http.post<AuthResponse>('/api/auth/refresh', { refreshToken: token }).pipe(
      tap(res => this.persistTokens(res)),
      map(res => res.accessToken)
    );
  }

  logout(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  private persistTokens(res: AuthResponse): void {
    localStorage.setItem('accessToken', res.accessToken);
    localStorage.setItem('refreshToken', res.refreshToken);
    this.currentUser.set(this.decodeUser(res.accessToken));
  }

  private decodeUser(token: string): CurrentUser {
    const payload = JSON.parse(atob(token.split('.')[1]));
    const roles = payload[ClaimTypes.Role];
    return {
      id: payload.sub,
      email: payload.email,
      firstName: payload.firstName,
      lastName: payload.lastName,
      roles: Array.isArray(roles) ? roles : [roles].filter(Boolean)
    };
  }

  private readInitialState(): CurrentUser | null {
    const token = localStorage.getItem('accessToken');
    if (!token) return null;
    try {
      const user = this.decodeUser(token);
      const payload = JSON.parse(atob(token.split('.')[1]));
      return payload.exp * 1000 > Date.now() ? user : null;
    } catch { return null; }
  }
}
```

### HttpInterceptor (functional)

```typescript
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // Login und Refresh selbst nicht durch Auth-Check
  if (req.url.includes('/auth/login') || req.url.includes('/auth/refresh')) {
    return next(req);
  }

  const token = auth.getAccessToken();
  const authed = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authed).pipe(
    catchError(err => {
      if (err.status !== 401 || !token) return throwError(() => err);

      return auth.refresh().pipe(
        switchMap(newToken => next(req.clone({
          setHeaders: { Authorization: `Bearer ${newToken}` }
        }))),
        catchError(refreshErr => {
          auth.logout();
          return throwError(() => refreshErr);
        })
      );
    })
  );
};
```

**Wichtige Feinheit:** Bei mehreren parallelen 401-Requests kann jeder einen Refresh-Call triggern (klassischer Bug). Für den MVP pragmatisch akzeptieren, bei Problemen `shareReplay` oder cached `currentRefresh$` einbauen.

### Functional Guards

```typescript
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isLoggedIn()) return true;
  router.navigate(['/login']);
  return false;
};

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (auth.isAdmin()) return true;
  router.navigate(['/']);
  return false;
};
```

## Security-Checkliste

- [ ] **Content Security Policy** in `staticwebapp.config.json` setzen (Schutz gegen XSS → Token-Diebstahl bei localStorage-Auth)
- [ ] **CORS** auf API: nur eigene Angular-Domain erlauben, nicht `*`
- [ ] **Rate Limiting** auf `/api/auth/login` — 5 Versuche pro Minute pro IP (via ASP.NET Core `AddRateLimiter`)
- [ ] **JWT Signing Key** mind. 32 Zeichen, aus Key Vault / Container App Secret — nicht in appsettings.json
- [ ] **Lockout** nach fehlgeschlagenen Logins aktiviert (15 Minuten, 5 Versuche)
- [ ] **HTTPS-Only** in Produktion — Container App hat das per Default

## Testing-Fokus

Besonders abdecken:
- LoginHandler happy path + falsches Passwort + inaktiver User
- RefreshHandler mit gültigem, revoked, expired Token
- Token-Rotation-Chain (alter wird revoked, neuer verwiesen)
- ForgotPassword mit existierender und nicht-existierender E-Mail (beide sollen generische Antwort geben)
