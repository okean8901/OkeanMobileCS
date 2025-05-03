using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.IO;

namespace Okean_Mobile.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _speechKey;
        private readonly string _speechRegion;

        public ChatbotController(IConfiguration configuration)
        {
            _configuration = configuration;
            _speechKey = _configuration["AzureSpeech:Key"];
            _speechRegion = _configuration["AzureSpeech:Region"];
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Chat([FromBody] ChatRequest request)
        {
            try
            {
                string responseMessage = ProcessMessage(request.Message.ToLower());
                var response = new ChatResponse
                {
                    Message = responseMessage,
                    Timestamp = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        private string ProcessMessage(string message)
        {
            // Chào hỏi và giới thiệu
            if (Regex.IsMatch(message, @"(xin chào|hello|hi|chào|alo|good morning|good afternoon)"))
            {
                return "Xin chào! Tôi là trợ lý ảo của Okean Mobile. Tôi có thể giúp gì cho bạn?\n" +
                       "Bạn có thể hỏi tôi về:\n" +
                       "- Sản phẩm và giá cả\n" +
                       "- Cách đặt hàng\n" +
                       "- Chính sách bảo hành\n" +
                       "- Thông tin liên hệ\n" +
                       "- Khuyến mãi\n" +
                       "- Địa chỉ cửa hàng";
            }

            // Giá cả và sản phẩm
            if (Regex.IsMatch(message, @"(giá|bao nhiêu|giá cả|cost|price|mắc|rẻ)"))
            {
                if (Regex.IsMatch(message, @"(iphone|apple)"))
                {
                    if (Regex.IsMatch(message, @"(15|15 pro|15 pro max)"))
                    {
                        return "iPhone 15 có các phiên bản với giá:\n" +
                               "- iPhone 15: 25-30 triệu\n" +
                               "- iPhone 15 Pro: 30-35 triệu\n" +
                               "- iPhone 15 Pro Max: 35-40 triệu\n" +
                               "Bạn quan tâm đến phiên bản nào?";
                    }
                    else if (Regex.IsMatch(message, @"(14|14 pro|14 pro max)"))
                    {
                        return "iPhone 14 có các phiên bản với giá:\n" +
                               "- iPhone 14: 20-25 triệu\n" +
                               "- iPhone 14 Pro: 25-30 triệu\n" +
                               "- iPhone 14 Pro Max: 30-35 triệu\n" +
                               "Bạn quan tâm đến phiên bản nào?";
                    }
                    return "Chúng tôi có các dòng iPhone với giá như sau:\n" +
                           "- iPhone 15 series: 25-40 triệu\n" +
                           "- iPhone 14 series: 20-35 triệu\n" +
                           "- iPhone 13 series: 15-30 triệu\n" +
                           "- iPhone SE: 10-15 triệu\n" +
                           "Bạn quan tâm đến dòng nào?";
                }
                else if (Regex.IsMatch(message, @"(samsung|galaxy)"))
                {
                    if (Regex.IsMatch(message, @"(s24|s24 ultra|s24 plus)"))
                    {
                        return "Samsung Galaxy S24 có các phiên bản với giá:\n" +
                               "- S24: 25-30 triệu\n" +
                               "- S24 Plus: 30-35 triệu\n" +
                               "- S24 Ultra: 35-40 triệu\n" +
                               "Bạn quan tâm đến phiên bản nào?";
                    }
                    return "Samsung có các dòng sản phẩm với giá:\n" +
                           "- Galaxy S24 series: 25-40 triệu\n" +
                           "- Galaxy S23 series: 20-35 triệu\n" +
                           "- Galaxy A54: 10-15 triệu\n" +
                           "- Galaxy M34: 5-10 triệu\n" +
                           "Bạn muốn xem dòng nào?";
                }
                else if (Regex.IsMatch(message, @"(xiaomi|redmi|poco)"))
                {
                    return "Xiaomi có các sản phẩm với giá:\n" +
                           "- Xiaomi 14: 15-20 triệu\n" +
                           "- Redmi Note 13: 5-10 triệu\n" +
                           "- Poco X6: 7-12 triệu\n" +
                           "Bạn quan tâm đến dòng nào?";
                }
                else if (Regex.IsMatch(message, @"(oppo|vivo|realme)"))
                {
                    return "Chúng tôi có các dòng điện thoại Trung Quốc với giá từ 5-20 triệu. Bạn muốn xem dòng nào?";
                }
                return "Bạn có thể xem giá sản phẩm tại trang sản phẩm của chúng tôi. Bạn muốn xem sản phẩm nào?";
            }

            // Đặt hàng và thanh toán
            if (Regex.IsMatch(message, @"(đặt hàng|mua|thanh toán|order|buy|payment|purchase)"))
            {
                if (Regex.IsMatch(message, @"(cách|thế nào|how|process|hướng dẫn)"))
                {
                    return "Bạn có thể đặt hàng theo các bước sau:\n" +
                           "1. Chọn sản phẩm\n" +
                           "2. Thêm vào giỏ hàng\n" +
                           "3. Điền thông tin giao hàng\n" +
                           "4. Chọn phương thức thanh toán:\n" +
                           "   - Thanh toán khi nhận hàng (COD)\n" +
                           "   - Chuyển khoản ngân hàng\n" +
                           "   - Thẻ tín dụng\n" +
                           "   - Ví điện tử (Momo, ZaloPay)\n" +
                           "5. Xác nhận đơn hàng";
                }
                return "Bạn có thể đặt hàng bằng cách thêm sản phẩm vào giỏ hàng và thanh toán. Bạn cần giúp gì thêm không?";
            }

            // Hỗ trợ và liên hệ
            if (Regex.IsMatch(message, @"(hỗ trợ|giúp|liên hệ|support|help|contact|hotline)"))
            {
                return "Chúng tôi có thể hỗ trợ bạn qua:\n" +
                       "- Điện thoại: 1900 1234\n" +
                       "- Email: support@okeanmobile.com\n" +
                       "- Zalo: 0901234567\n" +
                       "- Facebook: Okean Mobile\n" +
                       "- Thời gian làm việc: 8h-22h hàng ngày";
            }

            // Chính sách và dịch vụ
            if (Regex.IsMatch(message, @"(bảo hành|đổi trả|warranty|return|đổi|trả)"))
            {
                return "Chúng tôi có các chính sách sau:\n" +
                       "- Bảo hành 12 tháng chính hãng\n" +
                       "- Đổi trả trong 7 ngày nếu sản phẩm có lỗi\n" +
                       "- Hỗ trợ sửa chữa tại nhà\n" +
                       "- Bảo hành mở rộng có thể mua thêm\n" +
                       "Bạn cần thêm thông tin gì không?";
            }

            if (Regex.IsMatch(message, @"(giao hàng|ship|delivery|vận chuyển)"))
            {
                return "Chính sách giao hàng của chúng tôi:\n" +
                       "- Giao hàng toàn quốc\n" +
                       "- Phí ship: 0-30k tùy khu vực\n" +
                       "- Thời gian giao: 1-3 ngày\n" +
                       "- Giao hàng nhanh trong 2h tại Hà Nội và HCM\n" +
                       "- Đóng gói cẩn thận, kiểm tra trước khi nhận";
            }

            // Thông tin cửa hàng
            if (Regex.IsMatch(message, @"(cửa hàng|địa chỉ|store|address|showroom)"))
            {
                return "Chúng tôi có cửa hàng tại:\n" +
                       "- Hà Nội: 123 Trần Duy Hưng, Cầu Giấy\n" +
                       "- Hồ Chí Minh: 456 Nguyễn Văn Linh, Quận 7\n" +
                       "- Đà Nẵng: 789 Lê Duẩn, Hải Châu\n" +
                       "Giờ mở cửa: 8h-22h hàng ngày";
            }

            // Khuyến mãi
            if (Regex.IsMatch(message, @"(khuyến mãi|giảm giá|promotion|sale|discount|ưu đãi)"))
            {
                return "Chương trình khuyến mãi hiện tại:\n" +
                       "- Giảm 10% cho đơn hàng trên 10 triệu\n" +
                       "- Tặng phụ kiện khi mua iPhone\n" +
                       "- Trả góp 0% lãi suất\n" +
                       "- Tặng bảo hiểm rơi vỡ\n" +
                       "- Giảm 5% cho khách hàng thân thiết";
            }

            // Thông tin sản phẩm
            if (Regex.IsMatch(message, @"(thông tin|spec|tính năng|feature|cấu hình|ram|rom|pin)"))
            {
                if (Regex.IsMatch(message, @"(iphone|apple)"))
                {
                    if (Regex.IsMatch(message, @"(15|15 pro|15 pro max)"))
                    {
                        return "iPhone 15 có các tính năng nổi bật:\n" +
                               "- Chip A17 Pro mạnh mẽ\n" +
                               "- Camera 48MP chất lượng cao\n" +
                               "- Màn hình Dynamic Island\n" +
                               "- Pin 4000mAh, sạc nhanh 20W\n" +
                               "- Chống nước IP68\n" +
                               "- Thiết kế titan sang trọng";
                    }
                    return "iPhone có các tính năng nổi bật:\n" +
                           "- Hệ điều hành iOS mượt mà\n" +
                           "- Camera chất lượng cao\n" +
                           "- Bảo mật Face ID\n" +
                           "- Thời lượng pin tốt\n" +
                           "- Thiết kế sang trọng";
                }
                else if (Regex.IsMatch(message, @"(samsung|galaxy)"))
                {
                    if (Regex.IsMatch(message, @"(s24|s24 ultra|s24 plus)"))
                    {
                        return "Samsung Galaxy S24 có các tính năng:\n" +
                               "- Chip Snapdragon 8 Gen 3\n" +
                               "- Camera 200MP\n" +
                               "- Màn hình AMOLED 2K\n" +
                               "- Pin 5000mAh, sạc nhanh 45W\n" +
                               "- S-Pen tích hợp\n" +
                               "- Chống nước IP68";
                    }
                    return "Samsung Galaxy có các ưu điểm:\n" +
                           "- Màn hình AMOLED sắc nét\n" +
                           "- Camera đa ống kính\n" +
                           "- Pin khủng\n" +
                           "- S-Pen cho dòng Note\n" +
                           "- Chống nước IP68";
                }
            }

            // Câu hỏi về thời gian
            if (Regex.IsMatch(message, @"(mấy giờ|thời gian|giờ|time)"))
            {
                return $"Bây giờ là {DateTime.Now.ToString("HH:mm")}. Chúng tôi mở cửa từ 8h đến 22h hàng ngày.";
            }

            // Câu hỏi về ngày
            if (Regex.IsMatch(message, @"(ngày|date|hôm nay)"))
            {
                return $"Hôm nay là ngày {DateTime.Now.ToString("dd/MM/yyyy")}.";
            }

            // Câu hỏi không xác định
            if (Regex.IsMatch(message, @"(\?|gì|what|how|tại sao|vì sao)"))
            {
                return "Xin lỗi, tôi chưa hiểu rõ câu hỏi của bạn. Bạn có thể hỏi về:\n" +
                       "- Giá sản phẩm\n" +
                       "- Cách đặt hàng\n" +
                       "- Chính sách bảo hành\n" +
                       "- Thông tin liên hệ\n" +
                       "- Khuyến mãi\n" +
                       "- Địa chỉ cửa hàng";
            }

            // Cảm ơn
            if (Regex.IsMatch(message, @"(cảm ơn|thanks|thank|tks|thank you)"))
            {
                return "Không có gì! Nếu bạn cần thêm thông tin gì, cứ hỏi tôi nhé!";
            }

            // Tạm biệt
            if (Regex.IsMatch(message, @"(tạm biệt|bye|goodbye|see you)"))
            {
                return "Tạm biệt bạn! Hẹn gặp lại!";
            }

            // Mặc định
            return "Xin lỗi, tôi chưa hiểu rõ yêu cầu của bạn. Bạn có thể hỏi về sản phẩm, giá cả, đặt hàng hoặc hỗ trợ.";
        }

        [HttpPost("speech-to-text")]
        public async Task<IActionResult> SpeechToText(IFormFile audioFile)
        {
            try
            {
                var config = SpeechConfig.FromSubscription(_speechKey, _speechRegion);
                config.SpeechRecognitionLanguage = "vi-VN";

                var tempFilePath = Path.GetTempFileName();
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }

                using var audioConfig = AudioConfig.FromWavFileInput(tempFilePath);
                using var recognizer = new SpeechRecognizer(config, audioConfig);

                var result = await recognizer.RecognizeOnceAsync();

                System.IO.File.Delete(tempFilePath);

                return Ok(new { text = result.Text });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; }
    }

    public class ChatResponse
    {
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 