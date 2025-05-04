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
            <h3><i class="fas fa-robot"></i> Okean Mobile Assistant</h3>
            <button class="close-btn"><i class="fas fa-times"></i></button>
        </div>
        <div class="chatbot-messages"></div>
        <div class="chatbot-input">
            <input type="text" placeholder="Nhập tin nhắn của bạn...">
            <button class="voice-btn"><i class="fas fa-microphone"></i></button>
            <button class="send-btn"><i class="fas fa-paper-plane"></i></button>
        </div>
    `;
    document.body.appendChild(chatbotContainer);

    // Tạo overlay
    const chatbotOverlay = document.createElement('div');
    chatbotOverlay.className = 'chatbot-overlay';
    document.body.appendChild(chatbotOverlay);

    // Lấy các elements
    const sendBtn = chatbotContainer.querySelector('.send-btn');
    const input = chatbotContainer.querySelector('input');
    const closeBtn = chatbotContainer.querySelector('.close-btn');
    const voiceBtn = chatbotContainer.querySelector('.voice-btn');
    const messagesContainer = chatbotContainer.querySelector('.chatbot-messages');

    let isChatbotVisible = false;

    // Hiển thị chatbot
    function showChatbot() {
        if (!isChatbotVisible) {
            isChatbotVisible = true;
            chatbotContainer.classList.add('active');
            chatbotOverlay.classList.add('active');
            document.body.style.overflow = 'hidden';
            input.focus();
        }
    }

    // Ẩn chatbot
    function hideChatbot() {
        if (isChatbotVisible) {
            isChatbotVisible = false;
            chatbotContainer.classList.remove('active');
            chatbotOverlay.classList.remove('active');
            document.body.style.overflow = '';
        }
    }

    // Xử lý sự kiện click vào icon
    chatbotIcon.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        if (!isChatbotVisible) {
            showChatbot();
        }
    });

    // Xử lý sự kiện click vào nút đóng
    closeBtn.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        hideChatbot();
    });

    // Xử lý sự kiện click vào overlay
    chatbotOverlay.addEventListener('click', function(e) {
        e.preventDefault();
        hideChatbot();
    });

    // Xử lý sự kiện click vào container để ngăn chặn việc đóng khi click vào nội dung
    chatbotContainer.addEventListener('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
    });

    // Xử lý sự kiện phím ESC
    document.addEventListener('keydown', function(e) {
        if (e.key === 'Escape' && isChatbotVisible) {
            hideChatbot();
        }
    });

    // Gửi tin nhắn
    function sendMessage(message) {
        if (!message.trim()) return;

        addMessage(message, 'user');
        input.value = '';

        // Hiển thị typing indicator
        const typingIndicator = document.createElement('div');
        typingIndicator.className = 'typing-indicator';
        typingIndicator.innerHTML = '<span></span><span></span><span></span>';
        messagesContainer.appendChild(typingIndicator);
        messagesContainer.scrollTop = messagesContainer.scrollHeight;

        // Gửi tin nhắn đến server
        fetch('/api/chatbot/chat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
            },
            body: JSON.stringify({ message: message })
        })
        .then(response => {
            if (!response.ok) {
                throw new Error('Network response was not ok');
            }
            return response.json();
        })
        .then(data => {
            typingIndicator.remove();
            addMessage(data.message, 'bot');
        })
        .catch(error => {
            console.error('Error:', error);
            typingIndicator.remove();
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
    let mediaRecorder;
    let audioChunks = [];

    async function startVoiceRecording() {
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            mediaRecorder = new MediaRecorder(stream);
            
            mediaRecorder.ondataavailable = (event) => {
                audioChunks.push(event.data);
            };

            mediaRecorder.onstop = async () => {
                const audioBlob = new Blob(audioChunks, { type: 'audio/wav' });
                const formData = new FormData();
                formData.append('audioFile', audioBlob);

                try {
                    const response = await fetch('/api/chatbot/speech-to-text', {
                        method: 'POST',
                        body: formData
                    });

                    if (!response.ok) {
                        throw new Error('Speech to text failed');
                    }

                    const data = await response.json();
                    if (data.text) {
                        sendMessage(data.text);
                    }
                } catch (error) {
                    console.error('Error:', error);
                    addMessage('Xin lỗi, không thể xử lý giọng nói của bạn. Vui lòng thử lại sau.', 'bot');
                }

                audioChunks = [];
                stream.getTracks().forEach(track => track.stop());
            };

            mediaRecorder.start();
            addMessage('Đang ghi âm...', 'bot');
            
            // Dừng ghi âm sau 5 giây
            setTimeout(() => {
                if (mediaRecorder.state === 'recording') {
                    mediaRecorder.stop();
                }
            }, 5000);

        } catch (error) {
            console.error('Error accessing microphone:', error);
            addMessage('Xin lỗi, không thể truy cập microphone của bạn. Vui lòng kiểm tra quyền truy cập.', 'bot');
        }
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