using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Okean_Mobile.Data;

namespace Okean_Mobile.Services
{
    public interface IChatbotService
    {
        Task<string> ProcessMessageAsync(string message);
        Task<string> ProcessSpeechToTextAsync(Stream audioStream);
    }

    public class ChatbotService : IChatbotService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly SpeechConfig _speechConfig;

        public ChatbotService(IConfiguration configuration, ApplicationDbContext context)
        {
            _configuration = configuration;
            _context = context;

            var speechKey = configuration["AzureSpeech:Key"];
            var speechRegion = configuration["AzureSpeech:Region"];
            _speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            _speechConfig.SpeechRecognitionLanguage = "vi-VN";
        }

        public async Task<string> ProcessMessageAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return "Xin lỗi, tôi không hiểu tin nhắn của bạn.";

            message = message.ToLower();

            // Tìm kiếm sản phẩm trong database
            if (Regex.IsMatch(message, @"(tìm|search|sản phẩm|product|điện thoại|phone)"))
            {
                var products = await _context.Products
                    .Where(p => p.Name.ToLower().Contains(message) || 
                              p.Description.ToLower().Contains(message))
                    .Take(5)
                    .ToListAsync();

                if (products.Any())
                {
                    var response = "Tôi tìm thấy các sản phẩm sau:\n";
                    foreach (var product in products)
                    {
                        response += $"- {product.Name}: {product.Price:N0}đ\n";
                    }
                    return response;
                }
                return "Xin lỗi, tôi không tìm thấy sản phẩm nào phù hợp.";
            }

            // Kiểm tra đơn hàng
            if (Regex.IsMatch(message, @"(đơn hàng|order|tracking|theo dõi)"))
            {
                var orderNumber = Regex.Match(message, @"\d+").Value;
                if (!string.IsNullOrEmpty(orderNumber))
                {
                    var order = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .ThenInclude(od => od.Product)
                        .FirstOrDefaultAsync(o => o.Id.ToString() == orderNumber);

                    if (order != null)
                    {
                        var response = $"Thông tin đơn hàng #{orderNumber}:\n";
                        response += $"Trạng thái: {order.Status}\n";
                        response += $"Ngày đặt: {order.OrderDate:dd/MM/yyyy}\n";
                        response += "Sản phẩm:\n";
                        foreach (var detail in order.OrderDetails)
                        {
                            response += $"- {detail.Product.Name} x{detail.Quantity}\n";
                        }
                        return response;
                    }
                }
                return "Xin lỗi, tôi không tìm thấy thông tin đơn hàng.";
            }

            // Kiểm tra giỏ hàng
            if (Regex.IsMatch(message, @"(giỏ hàng|cart|giỏ)"))
            {
                var cartItems = await _context.CartItems
                    .Include(ci => ci.Product)
                    .ToListAsync();

                if (cartItems.Any())
                {
                    var response = "Giỏ hàng của bạn:\n";
                    foreach (var item in cartItems)
                    {
                        response += $"- {item.Product.Name}: {item.Quantity} x {item.Product.Price:N0}đ\n";
                    }
                    var total = cartItems.Sum(ci => ci.Quantity * ci.Product.Price);
                    response += $"Tổng cộng: {total:N0}đ";
                    return response;
                }
                return "Giỏ hàng của bạn đang trống.";
            }

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
                var products = await _context.Products
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                if (products.Any())
                {
                    var response = "Các sản phẩm mới nhất của chúng tôi:\n";
                    foreach (var product in products)
                    {
                        response += $"- {product.Name}: {product.Price:N0}đ\n";
                    }
                    return response;
                }
                return "Xin lỗi, hiện tại chúng tôi chưa có sản phẩm nào.";
            }

            // Đặt hàng và thanh toán
            if (Regex.IsMatch(message, @"(đặt hàng|mua|thanh toán|order|buy|payment|purchase)"))
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
                var products = await _context.Products
                    .Where(p => p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                if (products.Any())
                {
                    var response = "Thông tin các sản phẩm mới nhất:\n";
                    foreach (var product in products)
                    {
                        response += $"- {product.Name}:\n";
                        response += $"  + Mô tả: {product.Description}\n";
                        response += $"  + Giá: {product.Price:N0}đ\n";
                    }
                    return response;
                }
                return "Xin lỗi, hiện tại chúng tôi chưa có thông tin chi tiết về sản phẩm.";
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
                       "- Sản phẩm và giá cả\n" +
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
            return "Xin lỗi, tôi chưa hiểu rõ yêu cầu của bạn. Bạn có thể hỏi về sản phẩm, đơn hàng hoặc giỏ hàng.";
        }

        public async Task<string> ProcessSpeechToTextAsync(Stream audioStream)
        {
            // Lưu audio vào file tạm
            var tempFilePath = Path.GetTempFileName();
            using (var fileStream = File.Create(tempFilePath))
            {
                await audioStream.CopyToAsync(fileStream);
            }

            try
            {
                using var audioConfig = AudioConfig.FromWavFileInput(tempFilePath);
                using var recognizer = new SpeechRecognizer(_speechConfig, audioConfig);

                var result = await recognizer.RecognizeOnceAsync();
                return result.Text;
            }
            finally
            {
                // Xóa file tạm
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
        }
    }
} 