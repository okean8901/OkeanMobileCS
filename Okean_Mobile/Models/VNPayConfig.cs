using System;

namespace Okean_Mobile.Models
{
    public class VNPayConfig
    {
        public static string ConfigName => "VNPay";
        public static string Version => "2.1.0";
        public static string Command => "pay";
        public static string CurrCode => "VND";
        public static string Locale => "vn";
        public static string ReturnUrl => "https://localhost:5001/Payment/VNPayCallback";
        public static string TmnCode => "YOUR_TMN_CODE"; // Mã website tại VNPAY 
        public static string HashSecret => "YOUR_HASH_SECRET"; // Chuỗi bí mật
        public static string BaseUrl => "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"; // URL thanh toán VNPAY
    }
} 