// ESSE CARA E MAIS FACIL COPIAR E COLAR AQUI
//DEPOIS DEVEMOS CHAMAR ESSE INTERCEPTOR NA MAIN

import { Injectable } from '@angular/core';
import {
  HttpInterceptor,
  HttpRequest,
  HttpHandler,
  HttpEvent
} from '@angular/common/http';
import { Observable } from 'rxjs';
import { CsrfTokenService } from './csrf-token.service';

@Injectable()
export class CsrfInterceptor implements HttpInterceptor {
  constructor(private csrfService: CsrfTokenService) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const csrfToken = this.csrfService.token;

    if (csrfToken && ['POST', 'PUT', 'DELETE'].includes(req.method.toUpperCase())) {
      const cloned = req.clone({
        headers: req.headers.set('X-CSRF-TOKEN', csrfToken)
      });
      return next.handle(cloned);
    }

    return next.handle(req);
  }
}
