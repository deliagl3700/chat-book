export interface Message {
    text: string;
    autor: string;
    date: Date;
    isMe: boolean;
    stickerUrl?: string;
    imageUrl?: string;
}
export interface MessagesByDate {
    date: Date;
    messages: Message[];
}