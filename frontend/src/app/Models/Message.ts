
interface ChatMessage {
  text?: string;
  author: string;
  date: string;
  isMe: boolean;
  stickerUrl?: string;
  imageUrl?: string;
  audioUrl?: string;
  videoUrl?: string;
  qrCode?: string;
}

interface DayGroup {
  date: string;
  messages: ChatMessage[];
}

interface MessagesByDate {
  year: number;
  month: number;
  monthName: string;
  dayGroup: DayGroup[];
}