// In Models/RecentOrderViewModel.cs
namespace Okean_Mobile.Models
{
    public class RecentOrderViewModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
    }
}