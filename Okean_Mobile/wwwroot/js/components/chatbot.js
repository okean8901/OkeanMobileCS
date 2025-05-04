import { createElement, appendToBody, focusElement, scrollToBottom } from '../utils/dom.js';
import { sendMessage, convertSpeechToText } from '../services/chatbotService.js';

export class Chatbot {
    constructor() {
        this.container = null;
        this.icon = null;
        this.messagesContainer = null;
        this.input = null;
        this.initialize();
    }

    initialize() {
        this.createElements();
        this.setupEventListeners();
    }

    createElements() {
        // Tạo icon chatbot
        this.icon = createElement('div', 'chatbot-icon', '<i class="fas fa-comments"></i>');
        appendToBody(this.icon);

        // Tạo container chatbot
        this.container = createElement('div', 'chatbot-container');
        this.container.innerHTML = `
            <div class="chatbot-header">
                <h3>Okean Mobile Assistant</h3>
                <button class="close-btn">×</button>
            </div>
            <div class="chatbot-messages"></div>
            <div class="chatbot-input">
                <input type="text" placeholder="Nhập tin nhắn của bạn...">
                <button class="send-btn">Gửi</button>
                <button class="voice-btn">🎤</button>
            </div>
        `;
        appendToBody(this.container);

        // Lấy các phần tử
        this.messagesContainer = this.container.querySelector('.chatbot-messages');
        this.input = this.container.querySelector('input');
    }

    setupEventListeners() {
        const sendBtn = this.container.querySelector('.send-btn');
        const closeBtn = this.container.querySelector('.close-btn');
        const voiceBtn = this.container.querySelector('.voice-btn');

        // Mở chatbot
        this.icon.addEventListener('click', () => this.openChatbot());

        // Đóng chatbot
        closeBtn.addEventListener('click', () => this.closeChatbot());

        // Gửi tin nhắn
        sendBtn.addEventListener('click', () => this.handleSendMessage());
        this.input.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                this.handleSendMessage();
            }
        });

        // Ghi âm
        voiceBtn.addEventListener('click', () => this.startVoiceRecording());
    }

    openChatbot() {
        this.container.style.display = 'flex';
        this.icon.style.display = 'none';
        focusElement(this.input);
    }

    closeChatbot() {
        this.container.style.display = 'none';
        this.icon.style.display = 'flex';
    }

    async handleSendMessage() {
        const message = this.input.value.trim();
        if (!message) return;

        this.addMessage(message, 'user');
        this.input.value = '';

        try {
            const response = await sendMessage(message);
            this.addMessage(response.message, 'bot');
        } catch (error) {
            this.addMessage('Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại sau.', 'bot');
        }
    }

    addMessage(text, sender) {
        const messageDiv = createElement('div', `message ${sender}`);
        messageDiv.textContent = text;
        this.messagesContainer.appendChild(messageDiv);
        scrollToBottom(this.messagesContainer);
    }

    async startVoiceRecording() {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            const mediaRecorder = new MediaRecorder(stream);
            const audioChunks = [];

            mediaRecorder.addEventListener('dataavailable', event => {
                audioChunks.push(event.data);
            });

            mediaRecorder.addEventListener('stop', async () => {
                const audioBlob = new Blob(audioChunks);
                try {
                    const response = await convertSpeechToText(audioBlob);
                    if (response.text) {
                        this.handleSendMessage(response.text);
                    }
                } catch (error) {
                    console.error('Error:', error);
                }
            });

            mediaRecorder.start();
            setTimeout(() => mediaRecorder.stop(), 5000);
        } catch (error) {
            console.error('Error accessing microphone:', error);
        }
    }
} 