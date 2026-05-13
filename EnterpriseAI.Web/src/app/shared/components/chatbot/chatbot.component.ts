import { Component, ElementRef, ViewChild, inject, signal, effect, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MarkdownModule } from 'ngx-markdown';
import { ChatService } from '../../../core/services/chat.service';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
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

  // Configuration inputs from Web Component attributes
  apiUrl = input<string>('');
  clientId = input<string>('');
  clientSecret = input<string>('');

  // Angular 19 Signals for state management
  isOpen = signal<boolean>(false);
  isLoading = signal<boolean>(false);
  selectedImageUrl = signal<string | null>(null);
  messages = signal<ChatMessage[]>([
    { role: 'assistant', content: 'Merhaba! Enterprise RAG sistemine hoş geldiniz. Size nasıl yardımcı olabilirim?' }
  ]);
  
  userInput = signal<string>('');

  constructor() {
    // Automatically scroll to bottom whenever messages array changes
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

    // Add user message to UI
    this.messages.update(msgs => [...msgs, { role: 'user', content: prompt }]);
    this.userInput.set(''); // Clear input
    this.isLoading.set(true);

    this.chatService.askQuestion(prompt, this.apiUrl(), this.clientId(), this.clientSecret()).subscribe({
      next: (response) => {
        this.messages.update(msgs => [...msgs, { role: 'assistant', content: response }]);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.messages.update(msgs => [...msgs, { 
          role: 'assistant', 
          content: `**Hata:** ${err.message}` 
        }]);
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

  private scrollToBottom(): void {
    if (this.scrollContainer && this.scrollContainer.nativeElement) {
      const element = this.scrollContainer.nativeElement;
      element.scrollTop = element.scrollHeight;
    }
  }
}
