import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class CsrfTokenService {
  token: string = '';
}
