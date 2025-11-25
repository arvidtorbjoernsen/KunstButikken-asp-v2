import { provideZonelessChangeDetection, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateLoader, TranslateModule } from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { AppComponent } from './app.component';
import { KeycloakService } from './core/services/keycloak.service';

class KeycloakServiceStub {
  private readonly authenticatedState = signal(false);
  readonly authenticated = this.authenticatedState.asReadonly();
  boot = jasmine.createSpy('boot').and.resolveTo();
  login = jasmine.createSpy('login');
  logout = jasmine.createSpy('logout');
}

class TranslateTestLoader implements TranslateLoader {
  getTranslation(_: string): Observable<Record<string, string>> {
    return of({});
  }
}

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        AppComponent,
        TranslateModule.forRoot({
          loader: {
            provide: TranslateLoader,
            useClass: TranslateTestLoader
          }
        })
      ],
      providers: [
        provideZonelessChangeDetection(),
        { provide: KeycloakService, useClass: KeycloakServiceStub }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the brand name element', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    const brand = compiled.querySelector('.kb-toolbar__brand-name');
    expect(brand).toBeTruthy();
  });
});
