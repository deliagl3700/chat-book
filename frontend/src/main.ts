import { bootstrapApplication } from '@angular/platform-browser';
import { App } from './app/app';
import { provideHttpClient } from '@angular/common/http';
import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';

registerLocaleData(localeEs, 'es');

bootstrapApplication(App, {
  providers: [
    provideHttpClient(),
    { provide: LOCALE_ID, useValue: 'es' }
  ]
});
