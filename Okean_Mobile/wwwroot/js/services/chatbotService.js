export async function sendMessage(message) {
    try {
        const response = await fetch('/api/chatbot/chat', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({ message })
        });
        return await response.json();
    } catch (error) {
        console.error('Error sending message:', error);
        throw error;
    }
}

export async function convertSpeechToText(audioBlob) {
    try {
        const formData = new FormData();
        formData.append('audioFile', audioBlob);

        const response = await fetch('/api/chatbot/speech-to-text', {
            method: 'POST',
            body: formData
        });
        return await response.json();
    } catch (error) {
        console.error('Error converting speech to text:', error);
        throw error;
    }
} 