using Azure;
using Azure.Data.Tables;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace ABCRetail.Functions1
{
    public class ProductTableFunction
    {
        private readonly string _connectionString;

        private const string TableName = "Products";
        private const string PartitionKey = "Products";

        public ProductTableFunction()
        {
            _connectionString =
                Environment.GetEnvironmentVariable("AzureWebJobsStorage")
                ?? throw new InvalidOperationException(
                    "Azure Storage connection string is missing.");
        }

        private TableClient GetTableClient()
        {
            var tableClient = new TableClient(
                _connectionString,
                TableName);

            tableClient.CreateIfNotExists();

            return tableClient;
        }

        // CREATE PRODUCT
        [Function("CreateProduct")]
        public async Task<HttpResponseData> CreateProduct(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post")]
            HttpRequestData req)
        {
            var tableClient = GetTableClient();

            var body =
                await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Product data is required.");

                return response;
            }

            ProductData product;

            try
            {
                product =
                    JsonSerializer.Deserialize<ProductData>(
                        body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
            }
            catch (JsonException)
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Invalid JSON product data.");

                return response;
            }

            if (product == null ||
                string.IsNullOrWhiteSpace(product.ProductId))
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "ProductId is required.");

                return response;
            }

            var escapedProductId =
                product.ProductId.Replace("'", "''");

            var existingProducts =
                tableClient.Query<TableEntity>(
                    $"ProductId eq '{escapedProductId}'");

            foreach (var existing in existingProducts)
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.Conflict);

                await response.WriteAsJsonAsync(new
                {
                    message = "Product already exists.",
                    productId = product.ProductId
                });

                return response;
            }

            var entity =
                new TableEntity(
                    PartitionKey,
                    Guid.NewGuid().ToString())
                {
                    ["ProductId"] = product.ProductId,
                    ["ProductName"] = product.ProductName,
                    ["Description"] = product.Description,
                    ["Price"] = product.Price,
                    ["StockQuantity"] = product.StockQuantity,
                    ["ImageName"] = product.ImageName
                };

            await tableClient.AddEntityAsync(entity);

            var successResponse =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await successResponse.WriteAsJsonAsync(new
            {
                message = "Product created successfully.",
                productId = product.ProductId,
                rowKey = entity.RowKey
            });

            return successResponse;
        }

        // GET ONE PRODUCT
        [Function("GetProduct")]
        public async Task<HttpResponseData> GetProduct(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get")]
            HttpRequestData req)
        {
            var tableClient = GetTableClient();

            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var productId = query["productId"];

            if (string.IsNullOrWhiteSpace(productId))
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Provide a productId query parameter.");

                return response;
            }

            var escapedProductId =
                productId.Replace("'", "''");

            var products =
                tableClient.Query<TableEntity>(
                    $"ProductId eq '{escapedProductId}'");

            foreach (var entity in products)
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.OK);

                await response.WriteAsJsonAsync(new
                {
                    productId =
                        entity.GetString("ProductId"),

                    productName =
                        entity.GetString("ProductName"),

                    description =
                        entity.GetString("Description"),

                    price =
                        entity.GetDouble("Price"),

                    stockQuantity =
                        entity.GetInt32("StockQuantity"),

                    imageName =
                        entity.GetString("ImageName"),

                    rowKey =
                        entity.RowKey
                });

                return response;
            }

            var notFoundResponse =
                req.CreateResponse(
                    HttpStatusCode.NotFound);

            await notFoundResponse.WriteStringAsync(
                "Product not found.");

            return notFoundResponse;
        }

        // GET ALL PRODUCTS
        [Function("GetAllProducts")]
        public async Task<HttpResponseData> GetAllProducts(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get")]
            HttpRequestData req)
        {
            var tableClient = GetTableClient();

            var products =
                new List<object>();

            foreach (var entity in
                     tableClient.Query<TableEntity>())
            {
                products.Add(new
                {
                    productId =
                        entity.GetString("ProductId"),

                    productName =
                        entity.GetString("ProductName"),

                    description =
                        entity.GetString("Description"),

                    price =
                        entity.GetDouble("Price"),

                    stockQuantity =
                        entity.GetInt32("StockQuantity"),

                    imageName =
                        entity.GetString("ImageName"),

                    rowKey =
                        entity.RowKey
                });
            }

            var response =
                req.CreateResponse(
                    HttpStatusCode.OK);

            await response.WriteAsJsonAsync(
                products);

            return response;
        }

        // UPDATE PRODUCT
        [Function("UpdateProduct")]
        public async Task<HttpResponseData> UpdateProduct(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "put")]
            HttpRequestData req)
        {
            var tableClient = GetTableClient();

            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var productId = query["productId"];

            if (string.IsNullOrWhiteSpace(productId))
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Provide a productId query parameter.");

                return response;
            }

            var body =
                await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Updated product data is required.");

                return response;
            }

            ProductData product;

            try
            {
                product =
                    JsonSerializer.Deserialize<ProductData>(
                        body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
            }
            catch (JsonException)
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Invalid JSON product data.");

                return response;
            }

            if (product == null)
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Invalid product data.");

                return response;
            }

            var escapedProductId =
                productId.Replace("'", "''");

            var existingProducts =
                tableClient.Query<TableEntity>(
                    $"ProductId eq '{escapedProductId}'");

            foreach (var existing in existingProducts)
            {
                existing["ProductId"] = productId;
                existing["ProductName"] = product.ProductName;
                existing["Description"] = product.Description;
                existing["Price"] = product.Price;
                existing["StockQuantity"] = product.StockQuantity;
                existing["ImageName"] = product.ImageName;

                await tableClient.UpdateEntityAsync(
                    existing,
                    ETag.All,
                    TableUpdateMode.Replace);

                var response =
                    req.CreateResponse(
                        HttpStatusCode.OK);

                await response.WriteAsJsonAsync(new
                {
                    message =
                        "Product updated successfully.",

                    productId = productId
                });

                return response;
            }

            var notFoundResponse =
                req.CreateResponse(
                    HttpStatusCode.NotFound);

            await notFoundResponse.WriteStringAsync(
                "Product not found.");

            return notFoundResponse;
        }

        // DELETE PRODUCT
        [Function("DeleteProduct")]
        public async Task<HttpResponseData> DeleteProduct(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "delete")]
            HttpRequestData req)
        {
            var tableClient = GetTableClient();

            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var productId = query["productId"];

            if (string.IsNullOrWhiteSpace(productId))
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Provide a productId query parameter.");

                return response;
            }

            var escapedProductId =
                productId.Replace("'", "''");

            var products =
                tableClient.Query<TableEntity>(
                    $"ProductId eq '{escapedProductId}'");

            foreach (var entity in products)
            {
                await tableClient.DeleteEntityAsync(
                    entity.PartitionKey,
                    entity.RowKey);

                var response =
                    req.CreateResponse(
                        HttpStatusCode.OK);

                await response.WriteAsJsonAsync(new
                {
                    message =
                        "Product deleted successfully.",

                    productId = productId
                });

                return response;
            }

            var notFoundResponse =
                req.CreateResponse(
                    HttpStatusCode.NotFound);

            await notFoundResponse.WriteStringAsync(
                "Product not found.");

            return notFoundResponse;
        }
    }

    public class ProductData
    {
        public string ProductId { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public double Price { get; set; }

        public int StockQuantity { get; set; }

        public string ImageName { get; set; } = string.Empty;
    }
}