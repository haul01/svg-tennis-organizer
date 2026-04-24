# Angular-Projektstruktur

Frontend mit Angular (aktuelle Version), Standalone Components, Signals, Angular Material.

## Vollständige Struktur

```
frontend/
├── src/
│   ├── app/
│   │   ├── app.config.ts              ← Bootstrap, Providers, Interceptors
│   │   ├── app.routes.ts              ← Top-Level Routing (Lazy Loading)
│   │   ├── app.component.ts           ← Shell (Toolbar + Router-Outlet)
│   │   ├── app.component.html
│   │   │
│   │   ├── core/                      ← Singletons, genau einmal pro App
│   │   │   ├── auth/
│   │   │   │   ├── auth.service.ts
│   │   │   │   ├── auth.interceptor.ts
│   │   │   │   ├── auth.guard.ts
│   │   │   │   └── admin.guard.ts
│   │   │   ├── http/
│   │   │   │   └── error.interceptor.ts
│   │   │   └── models/
│   │   │       ├── current-user.model.ts
│   │   │       ├── result.model.ts
│   │   │       └── auth-response.model.ts
│   │   │
│   │   ├── features/                  ← Vertical Slices
│   │   │   ├── auth/
│   │   │   │   ├── login/
│   │   │   │   │   ├── login.component.ts
│   │   │   │   │   ├── login.component.html
│   │   │   │   │   └── login.component.scss
│   │   │   │   ├── forgot-password/
│   │   │   │   ├── set-password/
│   │   │   │   └── auth.routes.ts
│   │   │   │
│   │   │   ├── reservations/
│   │   │   │   ├── week-grid/         ← Haupt-Ansicht (Wochengrid)
│   │   │   │   ├── booking-dialog/    ← Modal zum Buchen
│   │   │   │   ├── my-reservations/
│   │   │   │   ├── reservations.service.ts
│   │   │   │   ├── reservations.api.ts
│   │   │   │   ├── reservation.model.ts
│   │   │   │   └── reservations.routes.ts
│   │   │   │
│   │   │   ├── admin/
│   │   │   │   ├── dashboard/
│   │   │   │   ├── members/
│   │   │   │   ├── courts/
│   │   │   │   ├── court-blocks/
│   │   │   │   ├── settings/
│   │   │   │   ├── season/
│   │   │   │   ├── guest-billing/
│   │   │   │   └── admin.routes.ts
│   │   │   │
│   │   │   └── profile/
│   │   │       ├── profile.component.ts
│   │   │       └── profile.routes.ts
│   │   │
│   │   └── shared/                    ← Wiederverwendbare UI & Helpers
│   │       ├── components/
│   │       │   ├── loading-spinner/
│   │       │   ├── confirm-dialog/
│   │       │   └── empty-state/
│   │       ├── pipes/
│   │       │   └── duration.pipe.ts
│   │       ├── directives/
│   │       └── validators/
│   │           └── custom-validators.ts
│   │
│   ├── environments/
│   │   ├── environment.ts
│   │   └── environment.prod.ts
│   ├── assets/
│   ├── styles/
│   │   ├── _theme.scss                ← Angular Material Theme
│   │   └── styles.scss
│   ├── index.html
│   └── main.ts
│
├── angular.json
├── package.json
├── tsconfig.json
└── tsconfig.app.json
```

## Core vs. Shared — wichtige Unterscheidung

Das ist der Punkt, wo Projekte gerne chaotisch werden.

**Core:** Dinge, die EXAKT EINMAL pro Applikation existieren dürfen. `AuthService`, `HttpInterceptors`, globale `Guards`. Ein zweiter `AuthService` wäre ein Bug. Werden in `app.config.ts` einmal registriert und via `inject()` überall verwendet.

**Shared:** Dinge, die VIELE INSTANZEN haben können und einfach wiederverwendbar sind. Ein `LoadingSpinner`, eine `ConfirmDialog`-Component, eine `DurationPipe`. Werden pro Component importiert, wo sie gebraucht werden.

**Warum diese Trennung wichtig ist:** Ohne diese Trennung legt irgendwann jemand einen Service im Shared-Ordner an, der State hält — und plötzlich hat jedes Feature seine eigene Kopie davon. Das ist ein Bug, der sehr schwer zu finden ist.

## Bootstrap: app.config.ts

```typescript
import { ApplicationConfig } from '@angular/core';
import { provideRouter, withComponentInputBinding } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MAT_DATE_LOCALE } from '@angular/material/core';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { errorInterceptor } from './core/http/error.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes, withComponentInputBinding()),
    provideHttpClient(withInterceptors([authInterceptor, errorInterceptor])),
    provideAnimationsAsync(),
    provideNativeDateAdapter(),
    { provide: MAT_DATE_LOCALE, useValue: 'de-AT' },
  ]
};
```

`withComponentInputBinding()` bindet Route-Parameter automatisch als `@Input` — kein `paramMap.subscribe` in jeder Detail-Component.

## Top-Level Routing mit Lazy Loading

```typescript
export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.component')
      .then(m => m.LoginComponent)
  },
  {
    path: 'forgot-password',
    loadComponent: () => import('./features/auth/forgot-password/forgot-password.component')
      .then(m => m.ForgotPasswordComponent)
  },
  {
    path: 'set-password',
    loadComponent: () => import('./features/auth/set-password/set-password.component')
      .then(m => m.SetPasswordComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    component: ShellComponent,
    children: [
      {
        path: 'reservations',
        loadChildren: () => import('./features/reservations/reservations.routes')
          .then(m => m.reservationsRoutes)
      },
      {
        path: 'profile',
        loadChildren: () => import('./features/profile/profile.routes')
          .then(m => m.profileRoutes)
      },
      {
        path: 'admin',
        canActivate: [adminGuard],
        loadChildren: () => import('./features/admin/admin.routes')
          .then(m => m.adminRoutes)
      },
      { path: '', redirectTo: 'reservations', pathMatch: 'full' }
    ]
  }
];
```

**Warum Lazy Loading wichtig ist:** Ein normales Mitglied sieht nie den Admin-Bereich, also muss dessen Code auch nicht in den initialen Bundle. Initialer JS-Download bleibt typischerweise unter 500 KB. Der Admin-Chunk kommt erst dazu, wenn ein Admin sich einloggt. Für Mobilnutzer auf schwachen Verbindungen real spürbar.

## State mit Signals im Service

**State immer im Service, nicht in der Component.** Grund: Der User navigiert durch die App — zum Profil, zurück zur Wochenansicht. Die Daten sollen noch da sein. State in der Component wird beim Navigieren zerstört. Im Service bleibt er.

```typescript
@Injectable({ providedIn: 'root' })
export class ReservationsService {
  private readonly api = inject(ReservationsApi);

  private readonly _reservations = signal<Reservation[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  // Public readonly — State-Mutation nur über Service-Methoden
  readonly reservations = this._reservations.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  async loadWeek(startDate: Date): Promise<void> {
    this._loading.set(true);
    this._error.set(null);
    try {
      const result = await firstValueFrom(this.api.getWeek(startDate));
      this._reservations.set(result);
    } catch (err: any) {
      this._error.set(err.message ?? 'Unbekannter Fehler');
    } finally {
      this._loading.set(false);
    }
  }

  async create(req: CreateReservationRequest): Promise<Result<string>> {
    const result = await firstValueFrom(this.api.create(req));
    if (result.success) await this.loadWeek(new Date(req.startsAt));
    return result;
  }
}
```

**Warum `asReadonly()`?** Damit Components den State lesen, aber nicht direkt mutieren können. Sonst passieren irgendwann Stellen wie `service.reservations.set([])` mitten in Components — State-Management wird unnachvollziehbar. Mutation nur über definierte Methoden.

**Warum kein NgRx?** NgRx ist sinnvoll für Enterprise-Dashboards mit vielen unabhängigen, quervernetzten State-Bereichen. Für ein Buchungssystem wäre es dreifacher Boilerplate-Aufwand pro Feature (Actions, Reducer, Effects, Selectors) ohne Gewinn. Signals + Services reichen.

## API-Layer separat vom Service-Layer

Pro Feature zwei Dateien: `<feature>.api.ts` (reines HTTP) und `<feature>.service.ts` (State + Orchestrierung).

```typescript
@Injectable({ providedIn: 'root' })
export class ReservationsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/reservations`;

  getWeek(startDate: Date): Observable<Reservation[]> {
    const params = new HttpParams().set('startDate', startDate.toISOString());
    return this.http.get<Reservation[]>(this.baseUrl, { params });
  }

  create(req: CreateReservationRequest): Observable<Result<string>> {
    return this.http.post<Result<string>>(this.baseUrl, req);
  }

  cancel(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
```

**Warum die Trennung?** API-Layer ist reiner HTTP-Code — Input, Output, kein State, keine Seiteneffekte. Service-Layer macht State-Management und ruft den API-Layer auf. Dadurch:
- Unit-Tests des Service mit gemocktem API-Layer sind trivial
- Wenn später WebSockets oder GraphQL reinkommen, tauscht du nur den API-Layer
- Components hängen nur am Service, nicht an HTTP-Details

## Components mit OnPush + Signals

```typescript
@Component({
  selector: 'app-week-grid',
  standalone: true,
  imports: [MatButtonModule, MatCardModule, DatePipe],
  templateUrl: './week-grid.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WeekGridComponent {
  private readonly service = inject(ReservationsService);

  readonly reservations = this.service.reservations;
  readonly loading = this.service.loading;

  readonly selectedWeek = signal(startOfWeek(new Date()));

  readonly reservationsByDay = computed(() =>
    groupBy(this.reservations(), r => format(r.startsAt, 'yyyy-MM-dd')));

  constructor() {
    effect(() => {
      this.service.loadWeek(this.selectedWeek());
    });
  }

  previousWeek(): void { this.selectedWeek.update(w => subWeeks(w, 1)); }
  nextWeek(): void { this.selectedWeek.update(w => addWeeks(w, 1)); }
}
```

**Warum OnPush?** Default-Change-Detection checkt jede Component bei jedem Event (Mouse-Move, HTTP-Response, Timer). Eine Wochenansicht mit 500+ Slot-Zellen wird damit spürbar langsam. OnPush checkt nur bei Signal-Änderungen und expliziten Triggern. Signals + OnPush ist der moderne Angular-Standard.

**Warum `effect()`?** Moderne Alternative zu `ngOnInit` + manuellen Subscriptions: „Wenn sich `selectedWeek` ändert, lade Daten neu." Keine `.subscribe()`, keine `ngOnDestroy`-Cleanup-Pflicht — Angular managed das.

## Reactive Forms für alle Formulare

Template-Driven (`[(ngModel)]`) reicht nicht für komplexe Formulare. Buchungsdialog wird wachsen: Gastspieler hinzufügen, async Validierung. Template-driven zwingt Logik ins HTML. Reactive Forms halten es im TypeScript.

```typescript
export class BookingDialogComponent {
  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.group({
    courtId: [null as number | null, Validators.required],
    startsAt: [null as Date | null, Validators.required],
    endsAt: [null as Date | null, Validators.required],
    guestPlayerId: [null as string | null],
  });

  async submit(): Promise<void> {
    if (this.form.invalid) return;
    const value = this.form.getRawValue();
    // …
  }
}
```

## package.json (relevante Dependencies)

```json
{
  "dependencies": {
    "@angular/animations": "^19.0.0",
    "@angular/cdk": "^19.0.0",
    "@angular/common": "^19.0.0",
    "@angular/compiler": "^19.0.0",
    "@angular/core": "^19.0.0",
    "@angular/forms": "^19.0.0",
    "@angular/material": "^19.0.0",
    "@angular/platform-browser": "^19.0.0",
    "@angular/router": "^19.0.0",
    "date-fns": "^4.0.0",
    "rxjs": "~7.8.0"
  }
}
```

**Bewusst NICHT drin:**
- `@ngrx/*` — Signals + Services reichen
- `lodash` — Native JS/TS reicht, gegebenenfalls einzelne Funktionen selbst schreiben
- `moment`, `luxon` — `date-fns` ist kompakter
- `primeng`, `ngx-bootstrap` — Angular Material allein

## Zwei Fallen, auf die zu achten ist

**Zirkuläre Dependencies zwischen Features.** Wenn `features/reservations/` Code aus `features/admin/` importiert, ist das ein Design-Problem. Features importieren nie untereinander — gemeinsamer Code gehört nach `core/` oder `shared/`.

**Modelle verstreuen sich.** Wenn `Reservation`-Interface in drei Dateien leicht unterschiedlich existiert, wird Refactoring schmerzhaft. Regel: jedes Modell in genau einer Datei (typischerweise im Feature, das am meisten davon weiß), alle anderen Stellen importieren daraus.

## Commands für die Entwicklung

```bash
# Angular CLI installieren
npm install -g @angular/cli

# Projekt anlegen mit Standalone + Routing
ng new frontend --standalone --routing --style scss --skip-git

# Angular Material hinzufügen
cd frontend
ng add @angular/material

# date-fns
npm install date-fns

# Dev-Server
ng serve

# Production-Build
ng build --configuration production

# Tests
ng test --watch=false

# Lint
ng lint
```
