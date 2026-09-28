import { Component, OnInit, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs/operators';
import { ChatService } from '../services/chat';


@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chat.component.html'
})
export class ChatComponent implements OnInit {

  messages: any[] = [];
  dateHeader: Date = new Date();
  loading = true;
  errorMessage = '';
  
  @ViewChild('chatContent') chatContent!: ElementRef;

  constructor(private chatService: ChatService,private cdr: ChangeDetectorRef ) {}

  ngOnInit(): void {
    this.chatService.getMessages()
      .pipe(finalize(() => {
        this.loading = false;
      }))
      .subscribe({
        next: data => {
          const loadedMessages = data ?? [];
          this.messages = loadedMessages.map((month: any) => ({
            ...month,
            dayGroup: month.dayGroup.map((day: any) => ({
              ...day,
              messageGroups: this.groupMessages(day.messages)
            }))
          }));

          this.cdr.detectChanges();
          this.loading = false;
          console.log('Mensajes recibidos', this.messages);
        },
        error: error => {
          console.error('Error cargando mensajes', error);
          this.loading = false;
          this.errorMessage = 'No se pudieron cargar los mensajes. Revisa la consola para más detalles.';
        }
      });
  }

  private groupMessages(messages: any[]): any[] {
    const groups: any[] = [];
    let currentRow: any = null;

    for (const msg of messages) {
      const isSticker = !!msg.stickerUrl;

      if (isSticker && currentRow && currentRow.type === 'stickerRow' && currentRow.isMe === msg.isMe) {
        currentRow.messages.push(msg);
      } else if (isSticker) {
        currentRow = {
          type: 'stickerRow',
          isMe: msg.isMe,
          messages: [msg]
        };
        groups.push(currentRow);
      } else {
        currentRow = null;
        groups.push({ type: 'message', message: msg });
      }
    }

    return groups;
  }

  exportPdf(): void {
    if (!this.chatContent) return;
    this.chatService.generatePdf(this.chatContent.nativeElement.outerHTML);
  }

}