import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { catchError, map, switchMap, tap } from 'rxjs/operators';
import { Observable, of, throwError } from 'rxjs';

export interface TokenResponse {
  token: string;
}

export interface AiRequest {
  applicationCode: string;
  systemInstruction: string;
  contextData: string;
  userPrompt: string;
}

export interface AiResponse {
  answer: string;
}

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  private http = inject(HttpClient);
  
  // Cache the JWT token using a signal
  private jwtToken = signal<string | null>(null);

  /**
   * Authenticates with the API using ClientId and ClientSecret to retrieve a JWT token.
   * If a token is already cached, returns it immediately.
   */
  private getToken(): Observable<string> {
    const currentToken = this.jwtToken();
    if (currentToken) {
      return of(currentToken);
    }

    const payload = {
      clientId: environment.auth.clientId,
      clientSecret: environment.auth.clientSecret
    };

    return this.http.post<TokenResponse>(`${environment.apiUrl}/Auth/token`, payload).pipe(
      map(response => response.token),
      tap(token => this.jwtToken.set(token)),
      catchError(error => {
        console.error('Auth API Error:', error);
        return throwError(() => new Error('Yetkilendirme hatası. Lütfen token servisini kontrol edin.'));
      })
    );
  }

  /**
   * Sends a user prompt to the RAG API, automatically handling the JWT Bearer token attachment.
   */
  askQuestion(userPrompt: string): Observable<string> {
    return this.getToken().pipe(
      switchMap(token => {
        const headers = new HttpHeaders({
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        });

        const requestPayload: AiRequest = {
          applicationCode: 'DEMO',
          systemInstruction: 'Sen kurumsal bir asistansın. SADECE sana sağlanan bağlam (context) verilerini kullanarak cevap ver. Eğer sorunun cevabı sana verilen bağlamda KESİNLİKLE YOKSA, kendi genel kültürünü kullanma ve yalnızca "Bu bilgiye kurumsal dokümanlarda ulaşılamıyor." yanıtını ver.',
          contextData: '',
          userPrompt: userPrompt
        };

        return this.http.post<AiResponse>(`${environment.apiUrl}/Ai/ask`, requestPayload, { headers });
      }),
      map(response => response.answer),
      catchError(error => {
        console.error('RAG API Error:', error);
        
        // Handle 401 Unauthorized by clearing the cached token so the next request gets a fresh one
        if (error.status === 401) {
          this.jwtToken.set(null);
        }

        return throwError(() => new Error('Yapay zeka servisine erişilemedi. Lütfen daha sonra tekrar deneyin.'));
      })
    );
  }
}
