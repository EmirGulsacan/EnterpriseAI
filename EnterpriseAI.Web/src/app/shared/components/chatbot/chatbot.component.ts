import { Component, ElementRef, ViewChild, inject, signal, effect, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MarkdownModule } from 'ngx-markdown';
import { ChatService } from '../../../core/services/chat.service';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  references?: any[];
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule, MarkdownModule],
  templateUrl: './chatbot.component.html',
  styleUrls: ['./chatbot.component.scss']
})
export class ChatbotComponent {
  private chatService = inject(ChatService);

  @ViewChild('scrollContainer') private scrollContainer!: ElementRef;

  apiUrl = input<string>('');
  clientId = input<string>('');
  clientSecret = input<string>('');
  applicationCode = input<string>('DEMO');

  isOpen = signal<boolean>(false);
  isLoading = signal<boolean>(false);
  selectedImageUrl = signal<string | null>(null);
  messages = signal<ChatMessage[]>([
    { role: 'assistant', content: 'Merhaba! Enterprise RAG sistemine hoş geldiniz. Size nasıl yardımcı olabilirim?' }
  ]);
  
  userInput = signal<string>('');

  constructor() {

    effect(() => {
      this.messages(); // track dependency
      setTimeout(() => this.scrollToBottom(), 50);
    });
  }

  toggleChat(): void {
    this.isOpen.update(state => !state);
  }

  sendMessage(): void {
    const prompt = this.userInput().trim();
    if (!prompt || this.isLoading()) return;

    this.messages.update(msgs => [...msgs, { role: 'user', content: prompt }]);
    this.userInput.set(''); // Clear input

    const assistantMessageIndex = this.messages().length;
    this.messages.update(msgs => [...msgs, { role: 'assistant', content: '', references: [] }]);
    
    this.isLoading.set(true);

    this.chatService.askQuestionStream(prompt, this.apiUrl(), this.clientId(), this.clientSecret(), this.applicationCode()).subscribe({
      next: (event) => {
        if (event.type === 'references') {
           this.messages.update(msgs => {
              const updated = [...msgs];
              updated[assistantMessageIndex].references = event.data;
              return updated;
           });
        } else if (event.type === 'content') {
           this.messages.update(msgs => {
              const updated = [...msgs];
              updated[assistantMessageIndex].content += event.data;
              return updated;
           });
        } else if (event.type === 'error') {
           this.messages.update(msgs => {
              const updated = [...msgs];
              updated[assistantMessageIndex].content += `\n\n**Hata:** ${event.data}`;
              return updated;
           });
           this.isLoading.set(false);
        } else if (event.type === 'done') {
           this.isLoading.set(false);
        }
      },
      error: (err) => {
        this.messages.update(msgs => {
          const updated = [...msgs];
          updated[assistantMessageIndex].content = `**Hata:** ${err.message}`;
          return updated;
        });
        this.isLoading.set(false);
      },
      complete: () => {
        this.isLoading.set(false);
      }
    });
  }

  handleKeyPress(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  onMessageClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (target.tagName.toLowerCase() === 'img') {
      const img = target as HTMLImageElement;
      this.selectedImageUrl.set(img.src);
    }
  }

  closeImageModal(): void {
    this.selectedImageUrl.set(null);
  }

  getBaseUrl(): string {
    try {
      const url = new URL(this.apiUrl());
      return `${url.protocol}//${url.host}`;
    } catch {
      return '';
    }
  }

  private scrollToBottom(): void {
    if (this.scrollContainer && this.scrollContainer.nativeElement) {
      const element = this.scrollContainer.nativeElement;
      element.scrollTop = element.scrollHeight;
    }
  }
}

