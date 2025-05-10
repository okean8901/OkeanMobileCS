using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Okean_Mobile.Models;

namespace Okean_Mobile.Services
{
    public class VNPayService
    {
        public string CreatePaymentUrl(string orderId, string fullName, string description, double amount, HttpContext context)
        {
            var model = new VNPayRequestModel
            {
                OrderId = orderId,
                FullName = fullName,
                Description = description,
                Amount = amount,
                CreatedDate = DateTime.Now
            };

            var timeZoneById = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            var timeNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZoneById);
            var tick = DateTime.Now.Ticks.ToString();
            var pay = new VnPayLibrary();
            var urlCallBack = VNPayConfig.ReturnUrl;

            pay.AddRequestData("vnp_Version", VNPayConfig.Version);
            pay.AddRequestData("vnp_Command", VNPayConfig.Command);
            pay.AddRequestData("vnp_TmnCode", VNPayConfig.TmnCode);
            pay.AddRequestData("vnp_Amount", ((int)(model.Amount * 100)).ToString());
            pay.AddRequestData("vnp_CreateDate", timeNow.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", VNPayConfig.CurrCode);
            pay.AddRequestData("vnp_IpAddr", GetIpAddress(context));
            pay.AddRequestData("vnp_Locale", VNPayConfig.Locale);
            pay.AddRequestData("vnp_OrderInfo", $"{model.OrderId} - {model.Description}");
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_ReturnUrl", urlCallBack);
            pay.AddRequestData("vnp_TxnRef", tick);

            var paymentUrl = pay.CreateRequestUrl(VNPayConfig.BaseUrl, VNPayConfig.HashSecret);
            return paymentUrl;
        }

        public bool ValidatePayment(string queryString)
        {
            var vnpay = new VnPayLibrary();
            var parameters = queryString.Split('&')
                .Select(p => p.Split('='))
                .Where(p => p.Length == 2)
                .ToDictionary(p => p[0], p => p[1]);

            foreach (var (key, value) in parameters)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value);
                }
            }

            if (!parameters.TryGetValue("vnp_SecureHash", out var vnp_SecureHash) ||
                !parameters.TryGetValue("vnp_ResponseCode", out var vnp_ResponseCode))
            {
                return false;
            }

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, VNPayConfig.HashSecret);
            return checkSignature && vnp_ResponseCode == "00";
        }

        public VNPayRequestModel PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            var orderId = vnpay.GetResponseData("vnp_TxnRef");
            var vnPayTranId = vnpay.GetResponseData("vnp_TransactionNo");
            var vnpResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnpSecureHash = collections.FirstOrDefault(k => k.Key == "vnp_SecureHash").Value.ToString();
            var orderInfo = vnpay.GetResponseData("vnp_OrderInfo");

            bool checkSignature = vnpay.ValidateSignature(vnpSecureHash, VNPayConfig.HashSecret);

            if (!checkSignature)
            {
                return new VNPayRequestModel()
                {
                    Status = "Invalid Signature",
                    OrderId = orderId,
                    TxnRef = vnPayTranId
                };
            }

            return new VNPayRequestModel()
            {
                Status = vnpResponseCode == "00" ? "Success" : "Failed",
                OrderId = orderId,
                TxnRef = vnPayTranId,
                Description = orderInfo
            };
        }

        private string GetIpAddress(HttpContext context)
        {
            var ipAddress = string.Empty;
            try
            {
                ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();

                if (string.IsNullOrEmpty(ipAddress) || (ipAddress.ToLower() == "unknown"))
                    ipAddress = context.Connection.RemoteIpAddress?.ToString();
            }
            catch (Exception ex)
            {
                ipAddress = "Invalid IP:" + ex.Message;
            }

            return ipAddress;
        }
    }

    public class VnPayLibrary
    {
        private SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public void AddResponseData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _responseData.Add(key, value);
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var retValue) ? retValue : string.Empty;
        }

        public string CreateRequestUrl(string baseUrl, string vnp_HashSecret)
        {
            var data = new StringBuilder();

            foreach (var (key, value) in _requestData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                data.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
            }

            var querystring = data.ToString();

            baseUrl += "?" + querystring;
            var signData = querystring;
            if (signData.Length > 0)
            {
                signData = signData.Remove(data.Length - 1, 1);
            }

            var vnp_SecureHash = HmacSHA512(vnp_HashSecret, signData);
            baseUrl += "vnp_SecureHash=" + vnp_SecureHash;

            return baseUrl;
        }

        public bool ValidateSignature(string inputHash, string secretKey)
        {
            var rspRaw = GetResponseData();
            var myChecksum = HmacSHA512(secretKey, rspRaw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private string GetResponseData()
        {
            var data = new StringBuilder();
            if (_responseData.ContainsKey("vnp_SecureHash"))
            {
                _responseData.Remove("vnp_SecureHash");
            }

            foreach (var (key, value) in _responseData.Where(kv => !string.IsNullOrEmpty(kv.Value)))
            {
                data.Append(WebUtility.UrlEncode(key) + "=" + WebUtility.UrlEncode(value) + "&");
            }

            if (data.Length > 0)
            {
                data.Remove(data.Length - 1, 1);
            }

            return data.ToString();
        }

        private string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                var hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
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