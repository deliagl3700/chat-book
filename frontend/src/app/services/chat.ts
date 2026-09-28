import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class ChatService {

  private apiUrl = 'http://localhost:5196/api/chat';

  constructor(private http: HttpClient) {}

  getMessages(): Observable<any[]> {
    return this.http.get<any[]>(this.apiUrl);
  }

  async generatePdf(html: string): Promise<void> { 
    try {
      const compressed = await this.compressHtml(html);
      this.http.post('http://localhost:5196/api/chat/pdf', compressed, {
        responseType: 'blob',
        headers: {
          'Content-Type': 'application/octet-stream', 
          'X-Content-Encoding': 'gzip'
        }
      }).subscribe(blob => {
        const url = window.URL.createObjectURL(blob);
        window.open(url);
      }, error => {
        console.error('Error generando PDF', error);
      });
    } catch (error) {
      console.warn('No se pudo comprimir la petición, se envía sin comprimir.', error);
      this.http.post('http://localhost:5196/api/chat/pdf', html, {
        responseType: 'blob',
        headers: {
          'Content-Type': 'text/plain; charset=utf-8'
        }
      }).subscribe(blob => {
        const url = window.URL.createObjectURL(blob);
        window.open(url);
      }, err => {
        console.error('Error generando PDF', err);
      });
    }
  }

  private async compressHtml(html: string): Promise<Blob> {
    if (typeof CompressionStream === 'undefined') {
      throw new Error('CompressionStream no disponible en este navegador');
    }

    const encoder = new TextEncoder();
    const encoded = encoder.encode(html);

    const source = new ReadableStream<Uint8Array>({
      start(controller) {
        controller.enqueue(encoded);
        controller.close();
      }
    });

    const compressedStream = source.pipeThrough(new CompressionStream('gzip') as unknown as TransformStream<Uint8Array, Uint8Array>);
    const reader = compressedStream.getReader();
    const chunks: Uint8Array[] = [];
    let result = await reader.read();

    while (!result.done) {
      if (result.value) {
        chunks.push(result.value);
      }
      result = await reader.read();
    }

    const totalSize = chunks.reduce((sum, chunk) => sum + chunk.byteLength, 0);
    const buffer = new Uint8Array(totalSize);
    let offset = 0;
    for (const chunk of chunks) {
      buffer.set(chunk, offset);
      offset += chunk.byteLength;
    }

    return new Blob([buffer], { type: 'application/gzip' });
  }
}
