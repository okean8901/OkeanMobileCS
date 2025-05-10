using System;

namespace Okean_Mobile.Models
{
    public class VNPayRequestModel
    {
        public string OrderId { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string PaymentUrl { get; set; }
        public string PaymentMethod { get; set; }
        public string Status { get; set; }
        public string TxnRef { get; set; }
    }
} 