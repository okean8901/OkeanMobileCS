using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Okean_Mobile.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User User { get; set; }

        public DateTime OrderDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } // VD: "Pending", "Shipped", "Completed"

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [StringLength(200, ErrorMessage = "Địa chỉ giao hàng không được vượt quá 200 ký tự")]
        public string ShippingAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(10, ErrorMessage = "Số điện thoại không được vượt quá 10 ký tự")]
        public string PhoneNumber { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
        public string Note { get; set; }

        public string PaymentMethod { get; set; } // COD or VNPay

        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
