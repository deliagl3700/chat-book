# Chat Book Generator

Turn your WhatsApp conversations into a realistic, printable chat book.

This project allows you to take an exported WhatsApp chat and transform it into a visually accurate, app-style conversation that can be exported as a high-quality PDF — perfect for creating meaningful, personal gifts.

---

## ✨ Features

* 📱 Realistic WhatsApp-style UI (bubbles, timestamps, ticks)
* ❤️ Emoji support (including large emoji messages)
* 🖼️ Image support from exported chats
* 📅 Automatic date grouping
* 📄 PDF generation ready for printing
* 🔒 100% local processing (no data upload)

---

## 🧱 Tech Stack

* **Backend:** .NET (C#)
* **Frontend:** Angular
* **PDF Generation:** 
* **Styling:** Custom CSS (WhatsApp-inspired)

---

## 🚀 Getting Started

### 1. Export your chat

From WhatsApp:

* Open chat
* Tap “Export chat”
* Choose **without media** (or include media if you want images)

Place the file in:

```
/data/chat.txt
```

---

### 2. Run the backend (parser)

```bash
cd backend
dotnet run
```

This will parse the chat and generate structured data.

---

### 3. Run the frontend

```bash
cd frontend
npm install
ng serve
```

Open:

```
http://localhost:4200
```

---

### 4. Generate PDF

Use _ to render the chat view and export it as a PDF.

---

## 📁 Project Structure

```
chat-book/
├── backend/          # .NET parser
├── frontend/         # Angular app
├── data/             # (ignored) chat files
├── assets/           # (ignored) media files
├── README.md
```

---

## 🔒 Privacy

This project is designed with privacy in mind:

* No external APIs
* No cloud uploads
* All processing is done locally

**Important:**
Your chat data and media files are ignored via `.gitignore` and are not included in this repository.

---

## 💡 Future Improvements

* 📚 Chapter/section generation
* ⭐ Highlight important messages
* 🎵 Support for audio/messages
* 🎨 Custom themes
* 📊 Chat statistics (most used words, etc.)
* 
---

## 📜 License

MIT

---

## ❤️ Motivation

This project was built as a personal way to preserve meaningful conversations in a tangible format — something more lasting than a chat history.

---


## 📌 Notes

* WhatsApp export format may vary slightly depending on device/language
* Large chats may require filtering for better print results
* Image paths must match exported filenames

---

Enjoy building something meaningful 💚
