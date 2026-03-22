import { Component, OnInit } from '@angular/core';
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

  constructor(private chatService: ChatService) {}

  ngOnInit(): void {
    this.chatService.getMessages().subscribe(data => {
      this.messages = data;
    });
  }
}