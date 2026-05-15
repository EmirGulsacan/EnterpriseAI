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

  private jwtToken = signal<string | null>(null);

  
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

  
  askQuestion(userPrompt: string, apiUrl: string, clientId: string, clientSecret: string, applicationCode: string): Observable<string> {
    return this.getToken(apiUrl, clientId, clientSecret).pipe(
      switchMap(token => {
        const headers = new HttpHeaders({
          'Authorization': `Bearer ${token}`,
          'Content-Type': 'application/json'
        });

        const requestPayload: AiRequest = {
          applicationCode: applicationCode,
          systemInstruction: 'Sen kibar bir "Kurumsal Yapay Zeka Asistanısın". Eğer kullanıcı sadece "Merhaba", "Nasılsın", "Günaydın" gibi günlük selamlaşma ifadeleri kullanıyorsa, ona kibarca, kurumsal bir dille cevap ver ve "Size kurumsal dokümanlar ve kılavuzlar konusunda nasıl yardımcı olabilirim?" diye sor. ANCAK EN ÖNEMLİ KURAL: Kullanıcı selamlaşma DIŞINDA, bağlamda (context) bulunmayan herhangi bir bilgi, genel kültür, kod yazımı veya alakasız bir konu sorarsa KESİNLİKLE CEVAP VERME. Sadece "Bu bilgiye kurumsal dokümanlarda ulaşılamıyor." de. Asla kendi içsel bilgini kullanma.',
          contextData: '',
          userPrompt: userPrompt
        };

        return this.http.post<AiResponse>(`${apiUrl}/Ai/ask`, requestPayload, { headers });
      }),
      map(response => response.answer),
      catchError(error => {
        console.error('RAG API Error:', error);

        if (error.status === 401) {
          this.jwtToken.set(null);
        }

        return throwError(() => new Error('Yapay zeka servisine erişilemedi. Lütfen daha sonra tekrar deneyin.'));
      })
    );
  }

  
  askQuestionStream(userPrompt: string, apiUrl: string, clientId: string, clientSecret: string, applicationCode: string): Observable<any> {
    return this.getToken(apiUrl, clientId, clientSecret).pipe(
      switchMap(token => {
        return new Observable<any>(observer => {
          const requestPayload: AiRequest = {
            applicationCode: applicationCode,
            systemInstruction: 'Sen kibar bir "Kurumsal Yapay Zeka Asistanısın". Eğer kullanıcı sadece "Merhaba", "Nasılsın", "Günaydın" gibi günlük selamlaşma ifadeleri kullanıyorsa, ona kibarca, kurumsal bir dille cevap ver ve "Size kurumsal dokümanlar ve kılavuzlar konusunda nasıl yardımcı olabilirim?" diye sor. ANCAK EN ÖNEMLİ KURAL: Kullanıcı selamlaşma DIŞINDA, bağlamda (context) bulunmayan herhangi bir bilgi, genel kültür, kod yazımı veya alakasız bir konu sorarsa KESİNLİKLE CEVAP VERME. Sadece "Bu bilgiye kurumsal dokümanlarda ulaşılamıyor." de. Asla kendi içsel bilgini kullanma.',
            contextData: '',
            userPrompt: userPrompt
          };

          fetch(`${apiUrl}/Ai/stream`, {
            method: 'POST',
            headers: {
              'Authorization': `Bearer ${token}`,
              'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestPayload)
          }).then(async response => {
            if (!response.ok) {
              if (response.status === 401) {
                this.jwtToken.set(null);
              }
              observer.error(new Error('Yapay zeka servisine erişilemedi. Lütfen daha sonra tekrar deneyin.'));
              return;
            }

            const reader = response.body?.getReader();
            const decoder = new TextDecoder('utf-8');
            let buffer = '';

            if (reader) {
              while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });
                const lines = buffer.split('\n\n');
                
                for (let i = 0; i < lines.length - 1; i++) {
                  const line = lines[i].trim();
                  if (line.startsWith('data: ')) {
                    const dataStr = line.substring(6);
                    try {
                      const dataObj = JSON.parse(dataStr);
                      observer.next(dataObj);
                      if (dataObj.type === 'done') {
                         observer.complete();
                         return;
                      }
                    } catch (e) {
                      console.error('Error parsing SSE JSON', e, dataStr);
                    }
                  }
                }
                
                buffer = lines[lines.length - 1];
              }
            }
            observer.complete();
          }).catch(err => {
            observer.error(err);
          });
        });
      })
    );
  }
}


