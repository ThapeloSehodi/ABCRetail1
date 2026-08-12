using Azure;
using Azure.Data.Tables;


namespace ABCRetail.Models
{
    public class Customer : ITableEntity
    {
        public string PartitionKey { get; set; } = "Customers";

        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }

        public ETag ETag { get; set; }

        public string CustomerId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

    }
}
