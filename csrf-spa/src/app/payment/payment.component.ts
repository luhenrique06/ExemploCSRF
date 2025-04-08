import { Component } from '@angular/core';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms'; 
import { CsrfTokenService } from '../csrf-token.service';



@Component({
  selector: 'app-payment',
  standalone: true,
  imports: [HttpClientModule, FormsModule], 
  templateUrl: './payment.component.html',
})
export class PaymentComponent {
  balance: number = 0;
  to: string = '';
  amount: number = 0;

  constructor(private http: HttpClient, private csrfService: CsrfTokenService) {}// atualizar aqui


  loadAntiforgeryToken() {
    this.http.get<any>('/antiforgery-token', { withCredentials: true }).subscribe({
      next: (res) => {
        this.csrfService.token = res.requestToken; // ← aqui
      },
      error: err => console.error('Erro ao carregar token', err)
    });
  }
  


  login() {
    this.http.post('/login', {}, { withCredentials: true }).subscribe({
      next: () => {
        this.loadAntiforgeryToken(); 
        this.loadBalance();
      },
      error: err => console.error('Erro no login', err)
    });
  }

  loadBalance() {
    this.http.get<any>('/balance', { withCredentials: true }).subscribe({
      next: res => this.balance = res.balance,
      error: err => console.error('Erro ao obter saldo', err)
    });
  }

  pay(event: Event) {
    event.preventDefault();

    this.http.post<any>(
      '/pay',
      { to: this.to, amount: this.amount },
      { withCredentials: true }
    ).subscribe({
      next: res => {
        alert(res.message);
        this.balance = res.newBalance;
      },
      error: err => alert('Erro no pagamento: ' + err.error)
    });
  }
}
