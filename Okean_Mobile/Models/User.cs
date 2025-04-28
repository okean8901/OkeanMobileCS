using System.ComponentModel.DataAnnotations;

namespace Okean_Mobile.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; }

        [Required]
        [StringLength(100)]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } // "Admin" hoặc "Customer"

        public ICollection<Order> Orders { get; set; }
    }
}
