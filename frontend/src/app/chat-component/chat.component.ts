import { Component, OnInit, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
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
  
  @ViewChild('chatContent') chatContent!: ElementRef;

  constructor(private chatService: ChatService) {}

  ngOnInit(): void {
    this.chatService.getMessages().subscribe(data => {
      this.messages = data;
      console.log(data);
    });
  }

  exportPdf(): void {
    if (!this.chatContent) return;
    this.chatService.generatePdf(this.chatContent.nativeElement.outerHTML);
  }

}