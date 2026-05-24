import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ChatService {

  private apiUrl = 'http://localhost:5196/api/chat';

  constructor(private http: HttpClient) {}

  getMessages(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  generatePdf(html: string): void {
    this.http.post('http://localhost:5196/api/chat/pdf', html, {
      responseType: 'blob',
      headers: {
        'Content-Type': 'text/plain; charset=utf-8'
      }
    }).subscribe(blob => {
      const url = window.URL.createObjectURL(blob);
      window.open(url);
    }, error => {
      console.error('Error generando PDF', error);
    });
  }
}