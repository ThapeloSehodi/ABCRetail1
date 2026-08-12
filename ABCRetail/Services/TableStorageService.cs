using ABCRetail.Models;
using Azure;
using Azure.Data.Tables;

namespace ABCRetail.Services
{
    public class TableStorageService
    {
        private readonly string _connectionString;

        public TableStorageService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AzureStorage")
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");
        }

        
        // CUSTOMER TABLE
        

        private TableClient GetCustomerTable()
        {
            var tableClient = new TableClient(
                _connectionString,
                "Customers");

            tableClient.CreateIfNotExists();

            return tableClient;
        }

        public async Task AddCustomerAsync(Customer customer)
        {
            var tableClient = GetCustomerTable();

            await tableClient.AddEntityAsync(customer);
        }

        public async Task<List<Customer>> GetCustomersAsync()
        {
            var tableClient = GetCustomerTable();

            var customers = new List<Customer>();

            await foreach (var customer in tableClient.QueryAsync<Customer>())
            {
                customers.Add(customer);
            }

            return customers;
        }

        public async Task<Customer?> GetCustomerAsync(string rowKey)
        {
            var tableClient = GetCustomerTable();

            try
            {
                var response = await tableClient.GetEntityAsync<Customer>(
                    "Customers",
                    rowKey);

                return response.Value;
            }
            catch
            {
                return null;
            }
        }
        public async Task UpdateCustomerAsync(Customer customer)
        {
            var tableClient = GetCustomerTable();

            await tableClient.UpdateEntityAsync(
                customer,
                 ETag.All,
                TableUpdateMode.Replace);
        }

        public async Task DeleteCustomerAsync(string rowKey)
        {
            var tableClient = GetCustomerTable();

            await tableClient.DeleteEntityAsync(
                "Customers",
                rowKey);
        }


        
        // PRODUCT TABLE
      
        private TableClient GetProductTable()
        {
            var tableClient = new TableClient(
                _connectionString,
                "Products");

            tableClient.CreateIfNotExists();

            return tableClient;
        }

        public async Task AddProductAsync(Product product)
        {
            var tableClient = GetProductTable();

            await tableClient.AddEntityAsync(product);
        }

        public async Task<List<Product>> GetProductsAsync()
        {
            var tableClient = GetProductTable();

            var products = new List<Product>();

            await foreach (var product in tableClient.QueryAsync<Product>())
            {
                products.Add(product);
            }

            return products;
        }

        public async Task<Product?> GetProductAsync(string rowKey)
        {
            var tableClient = GetProductTable();

            try
            {
                var response = await tableClient.GetEntityAsync<Product>(
                    "Products",
                    rowKey);

                return response.Value;
            }
            catch
            {
                return null;
            }
        }
        public async Task UpdateProductAsync(Product product)
        {
            var tableClient = GetProductTable();

            await tableClient.UpdateEntityAsync(
                product,
                 ETag.All,
                TableUpdateMode.Replace);
        }

        public async Task DeleteProductAsync(string rowKey)
        {
            var tableClient = GetProductTable();

            await tableClient.DeleteEntityAsync(
                "Products",
                rowKey);
        }
    }
}
