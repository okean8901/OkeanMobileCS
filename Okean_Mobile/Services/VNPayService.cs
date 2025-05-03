using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Okean_Mobile.Services
{
    public class VNPayService
    {
        private readonly IConfiguration _configuration;
        private readonly string _vnp_Url;
        private readonly string _vnp_ReturnUrl;
        private readonly string _vnp_TmnCode;
        private readonly string _vnp_HashSecret;

        public VNPayService(IConfiguration configuration)
        {
            _configuration = configuration;
            _vnp_Url = _configuration["VNPay:Url"];
            _vnp_ReturnUrl = _configuration["VNPay:ReturnUrl"];
            _vnp_TmnCode = _configuration["VNPay:TmnCode"];
            _vnp_HashSecret = _configuration["VNPay:HashSecret"];
        }

        public string CreatePaymentUrl(int orderId, decimal amount, string orderInfo)
        {
            // Build URL for VNPay
            var vnpay = new VnPayLibrary();
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", ((int)amount * 100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1"); // Replace with actual IP
            vnpay.AddRequestData("vnp_Locale", "vn");
            vnpay.AddRequestData("vnp_OrderInfo", orderInfo);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", _vnp_ReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", orderId.ToString());

            string paymentUrl = vnpay.CreateRequestUrl(_vnp_Url, _vnp_HashSecret);
            return paymentUrl;
        }

        public bool ValidatePayment(string queryString)
        {
            var vnpay = new VnPayLibrary();
            var parameters = HttpUtility.ParseQueryString(queryString);
            foreach (string key in parameters)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, parameters[key]);
                }
            }

            string vnp_SecureHash = parameters["vnp_SecureHash"];
            string vnp_TxnRef = parameters["vnp_TxnRef"];
            string vnp_ResponseCode = parameters["vnp_ResponseCode"];

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _vnp_HashSecret);
            return checkSignature && vnp_ResponseCode == "00";
        }
    }

    public class VnPayLibrary
    {
        private SortedList<String, String> _requestData = new SortedList<String, String>(new VnPayCompare());
        private SortedList<String, String> _responseData = new SortedList<String, String>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            string retValue;
            if (_responseData.TryGetValue(key, out retValue))
            {
                return retValue;
            }
            else
            {
                return string.Empty;
            }
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            StringBuilder data = new StringBuilder();
            foreach (KeyValuePair<string, string> kv in _requestData)
            {
                if (!String.IsNullOrEmpty(kv.Value))
                {
                    data.Append(kv.Key + "=" + HttpUtility.UrlEncode(kv.Value) + "&");
                }
            }
            string queryString = data.ToString();

            baseUrl += "?" + queryString;
            string signData = queryString;
            if (signData.Length > 0)
            {
                signData = signData.Remove(data.Length - 1, 1);
            }

            string vnp_SecureHash = HmacSHA512(hashSecret, signData);
            baseUrl += "vnp_SecureHash=" + vnp_SecureHash;

            return baseUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            string rspRaw = GetResponseData();
            string myChecksum = HmacSHA512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseData()
        {
            StringBuilder data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHashType"))
            {
                _responseData.Remove("vnp_SecureHashType");
            }
            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }
            foreach (KeyValuePair<string, string> kv in _responseData)
            {
                if (!String.IsNullOrEmpty(kv.Value))
                {
                    data.Append(kv.Key + "=" + kv.Value + "&");
                }
            }
            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }
            return data.ToString();
        }

        public string HmacSHA512(string key, string inputData)
        {
            byte[] keyByte = Encoding.UTF8.GetBytes(key);
            byte[] messageBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmacsha512 = new HMACSHA512(keyByte))
            {
                byte[] hashmessage = hmacsha512.ComputeHash(messageBytes);
                string hex = BitConverter.ToString(hashmessage);
                hex = hex.Replace("-", "").ToLower();
                return hex;
            }
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var vnpCompare = CompareInfo.GetCompareInfo("en-US");
            return vnpCompare.Compare(x, y, CompareOptions.Ordinal);
        }
    }
} 