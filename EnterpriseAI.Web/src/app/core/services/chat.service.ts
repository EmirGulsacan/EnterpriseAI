import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
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
  private getToken(apiUrl: string, clientId: string, clientSecret: string): Observable<string> {
    const currentToken = this.jwtToken();
    if (currentToken) {
      return of(currentToken);
    }

    const payload = {
      clientId: clientId,
      clientSecret: clientSecret
    };

    return this.http.post<TokenResponse>(`${apiUrl}/Auth/token`, payload).pipe(
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
  askQuestion(userPrompt: string, apiUrl: string, clientId: string, clientSecret: string): Observable<string> {
    return this.getToken(apiUrl, clientId, clientSecret).pipe(
      switchMap(token => {
        const headers = new HttpHeaders({
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        });

        const requestPayload: AiRequest = {
          applicationCode: 'DEMO',
          systemInstruction: 'Sen kibar bir "Kurumsal Yapay Zeka Asistanısın". Eğer kullanıcı sadece "Merhaba", "Nasılsın", "Günaydın" gibi günlük selamlaşma ifadeleri kullanıyorsa, ona kibarca, kurumsal bir dille cevap ver ve "Size kurumsal dokümanlar ve kılavuzlar konusunda nasıl yardımcı olabilirim?" diye sor. ANCAK EN ÖNEMLİ KURAL: Kullanıcı selamlaşma DIŞINDA, bağlamda (context) bulunmayan herhangi bir bilgi, genel kültür, kod yazımı veya alakasız bir konu sorarsa KESİNLİKLE CEVAP VERME. Sadece "Bu bilgiye kurumsal dokümanlarda ulaşılamıyor." de. Asla kendi içsel bilgini kullanma.',
          contextData: '',
          userPrompt: userPrompt
        };

        return this.http.post<AiResponse>(`${apiUrl}/Ai/ask`, requestPayload, { headers });
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
