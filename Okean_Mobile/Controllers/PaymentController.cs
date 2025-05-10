using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Okean_Mobile.Models;
using Okean_Mobile.Services;

namespace Okean_Mobile.Controllers
{
    public class PaymentController : Controller
    {
        private readonly VNPayService _vnPayService;

        public PaymentController(VNPayService vnPayService)
        {
            _vnPayService = vnPayService;
        }

        public IActionResult CreatePayment(string orderId, string fullName, string description, double amount)
        {
            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(fullName) || 
                string.IsNullOrEmpty(description) || amount <= 0)
            {
                return BadRequest("Invalid payment parameters");
            }

            var paymentUrl = _vnPayService.CreatePaymentUrl(
                orderId: orderId,
                fullName: fullName,
                description: description,
                amount: amount,
                context: HttpContext
            );

            return Redirect(paymentUrl);
        }

        public IActionResult VNPayCallback()
        {
            // Convert IQueryCollection to query string
            var queryString = string.Join("&", Request.Query.Select(x => $"{x.Key}={x.Value}"));
            
            // Validate payment first
            if (!_vnPayService.ValidatePayment(queryString))
            {
                TempData["Message"] = "Thanh toán không hợp lệ!";
                return RedirectToAction(nameof(PaymentFailed));
            }

            var response = _vnPayService.PaymentExecute(Request.Query);

            if (response.Status == "Success")
            {
                // Cập nhật trạng thái đơn hàng trong database
                // TODO: Implement your order status update logic here
                TempData["Message"] = "Thanh toán thành công!";
                return RedirectToAction(nameof(PaymentSuccess), new { orderId = response.OrderId });
            }
            else
            {
                TempData["Message"] = "Thanh toán thất bại!";
                return RedirectToAction(nameof(PaymentFailed), new { orderId = response.OrderId });
            }
        }

        public IActionResult PaymentSuccess(string orderId)
        {
            ViewBag.OrderId = orderId;
            ViewBag.Message = TempData["Message"]?.ToString() ?? "Thanh toán thành công!";
            return View();
        }

        public IActionResult PaymentFailed(string orderId)
        {
            ViewBag.OrderId = orderId;
            ViewBag.Message = TempData["Message"]?.ToString() ?? "Thanh toán thất bại!";
            return View();
        }
    }
} 