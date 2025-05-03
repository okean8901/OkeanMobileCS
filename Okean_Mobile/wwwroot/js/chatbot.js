document.addEventListener('DOMContentLoaded', function() {
    // Tạo icon chatbot
    const chatbotIcon = document.createElement('div');
    chatbotIcon.className = 'chatbot-icon';
    chatbotIcon.innerHTML = '<i class="fas fa-comments"></i>';
    document.body.appendChild(chatbotIcon);

    // Tạo container chatbot
    const chatbotContainer = document.createElement('div');
    chatbotContainer.className = 'chatbot-container';
    chatbotContainer.innerHTML = `
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
    document.body.appendChild(chatbotContainer);

    // Xử lý sự kiện
    const sendBtn = chatbotContainer.querySelector('.send-btn');
    const input = chatbotContainer.querySelector('input');
    const closeBtn = chatbotContainer.querySelector('.close-btn');
    const voiceBtn = chatbotContainer.querySelector('.voice-btn');
    const messagesContainer = chatbotContainer.querySelector('.chatbot-messages');

    // Mở chatbot khi click vào icon
    chatbotIcon.addEventListener('click', () => {
        chatbotContainer.style.display = 'flex';
        chatbotIcon.style.display = 'none';
        input.focus();
    });

    // Đóng chatbot
    closeBtn.addEventListener('click', () => {
        chatbotContainer.style.display = 'none';
        chatbotIcon.style.display = 'flex';
    });

    // Gửi tin nhắn
    function sendMessage(message) {
        if (!message.trim()) return;

        addMessage(message, 'user');
        input.value = '';

        fetch('/api/chatbot/chat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ message: message })
        })
        .then(response => response.json())
        .then(data => {
            addMessage(data.message, 'bot');
        })
        .catch(error => {
            console.error('Error:', error);
            addMessage('Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại sau.', 'bot');
        });
    }

    // Thêm tin nhắn vào giao diện
    function addMessage(text, sender) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${sender}`;
        messageDiv.textContent = text;
        messagesContainer.appendChild(messageDiv);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;
    }

    // Xử lý ghi âm
    function startVoiceRecording() {
        navigator.mediaDevices.getUserMedia({ audio: true })
            .then(stream => {
                const mediaRecorder = new MediaRecorder(stream);
                const audioChunks = [];

                mediaRecorder.addEventListener('dataavailable', event => {
                    audioChunks.push(event.data);
                });

                mediaRecorder.addEventListener('stop', () => {
                    const audioBlob = new Blob(audioChunks);
                    const formData = new FormData();
                    formData.append('audioFile', audioBlob);

                    fetch('/api/chatbot/speech-to-text', {
                        method: 'POST',
                        body: formData
                    })
                    .then(response => response.json())
                    .then(data => {
                        if (data.text) {
                            sendMessage(data.text);
                        }
                    })
                    .catch(error => {
                        console.error('Error:', error);
                    });
                });

                mediaRecorder.start();
                setTimeout(() => mediaRecorder.stop(), 5000);
            })
            .catch(error => {
                console.error('Error accessing microphone:', error);
            });
    }

    // Gắn sự kiện
    sendBtn.addEventListener('click', () => sendMessage(input.value));
    input.addEventListener('keypress', (e) => {
        if (e.key === 'Enter') {
            sendMessage(input.value);
        }
    });
    voiceBtn.addEventListener('click', startVoiceRecording);
}); 