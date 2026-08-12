namespace ABCRetail.Models
{
    public class OrderMessage
    {
        public string OrderId { get; set; } = string.Empty;

        public string CustomerId { get; set; } = string.Empty;

        public string ProductId { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string Action { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
