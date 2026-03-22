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
}