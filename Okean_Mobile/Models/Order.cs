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

        public ICollection<OrderDetail> OrderDetails { get; set; }
    }
}
