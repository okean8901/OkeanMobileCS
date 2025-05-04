using Microsoft.AspNetCore.Mvc;
using Okean_Mobile.Services;
using System.Threading.Tasks;

namespace Okean_Mobile.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;

        public ChatbotController(IChatbotService chatbotService)
        {
            _chatbotService = chatbotService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> ProcessMessage([FromBody] ChatRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty");
            }

            var response = await _chatbotService.ProcessMessageAsync(request.Message);
            return Ok(new { message = response });
        }

        [HttpPost("speech-to-text")]
        public async Task<IActionResult> ProcessSpeechToText([FromForm] IFormFile audioFile)
        {
            if (audioFile == null || audioFile.Length == 0)
            {
                return BadRequest("Audio file is required");
            }

            using var stream = audioFile.OpenReadStream();
            var text = await _chatbotService.ProcessSpeechToTextAsync(stream);
            return Ok(new { text });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }
} 