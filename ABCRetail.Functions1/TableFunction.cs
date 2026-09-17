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
    public class TableFunction
    {
        private readonly string _connectionString;
        private const string TableName = "Customers";
        private const string PartitionKey = "Customers";

        public TableFunction()
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

        
        // CREATE CUSTOMER
        // POST /api/CreateCustomer
        
        [Function("CreateCustomer")]
        public async Task<HttpResponseData> CreateCustomer(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "post")] HttpRequestData req)
        {
            var tableClient = GetTableClient();

            var body =
                await new StreamReader(req.Body).ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
            {
                var response =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Customer data is required.");

                return response;
            }

            CustomerData customer;

            try
            {
                customer =
                    JsonSerializer.Deserialize<CustomerData>(
                        body,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
            }
            catch (JsonException)
            {
                var response =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Invalid JSON customer data.");

                return response;
            }

            if (customer == null ||
                string.IsNullOrWhiteSpace(customer.CustomerId))
            {
                var response =
                    req.CreateResponse(HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "CustomerId is required.");

                return response;
            }

            // Check whether customer already exists
            var existingCustomers =
                tableClient.Query<TableEntity>(
                    $"CustomerId eq '{customer.CustomerId.Replace("'", "''")}'");

            foreach (var existing in existingCustomers)
            {
                var response =
                    req.CreateResponse(HttpStatusCode.Conflict);

                await response.WriteAsJsonAsync(new
                {
                    message = "Customer already exists.",
                    customerId = customer.CustomerId
                });

                return response;
            }

            var entity = new TableEntity(
                PartitionKey,
                Guid.NewGuid().ToString())
            {
                ["CustomerId"] = customer.CustomerId,
                ["FullName"] = customer.FullName,
                ["Email"] = customer.Email,
                ["PhoneNumber"] = customer.PhoneNumber,
                ["Address"] = customer.Address
            };

            await tableClient.AddEntityAsync(entity);

            var successResponse =
                req.CreateResponse(HttpStatusCode.OK);

            await successResponse.WriteAsJsonAsync(new
            {
                message = "Customer created successfully.",
                customerId = customer.CustomerId
            });

            return successResponse;
        }

        // GET ONE CUSTOMER
        // GET /api/GetCustomer?customerId=C001
        
        [Function("GetCustomer")]
        public async Task<HttpResponseData> GetCustomer(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get")] HttpRequestData req)
        {
            var tableClient = GetTableClient();

            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var customerId =
                query["customerId"];

            if (string.IsNullOrWhiteSpace(customerId))
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Provide a customerId query parameter.");

                return response;
            }

            var escapedCustomerId =
                customerId.Replace("'", "''");

            var customers =
                tableClient.Query<TableEntity>(
                    $"CustomerId eq '{escapedCustomerId}'");

            foreach (var entity in customers)
            {
                var response =
                    req.CreateResponse(HttpStatusCode.OK);

                await response.WriteAsJsonAsync(new
                {
                    customerId =
                        entity.GetString("CustomerId"),

                    fullName =
                        entity.GetString("FullName"),

                    email =
                        entity.GetString("Email"),

                    phoneNumber =
                        entity.GetString("PhoneNumber"),

                    address =
                        entity.GetString("Address")
                });

                return response;
            }

            var notFoundResponse =
                req.CreateResponse(
                    HttpStatusCode.NotFound);

            await notFoundResponse.WriteStringAsync(
                "Customer not found.");

            return notFoundResponse;
        }


        
        // GET ALL CUSTOMERS
        // GET /api/GetAllCustomers
        

        [Function("GetAllCustomers")]
        public async Task<HttpResponseData> GetAllCustomers(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "get")] HttpRequestData req)
        {
            var tableClient = GetTableClient();

            var customers =
                new List<object>();

            foreach (var entity in
                     tableClient.Query<TableEntity>())
            {
                customers.Add(new
                {
                    customerId =
                        entity.GetString("CustomerId"),

                    fullName =
                        entity.GetString("FullName"),

                    email =
                        entity.GetString("Email"),

                    phoneNumber =
                        entity.GetString("PhoneNumber"),

                    address =
                        entity.GetString("Address")
                });
            }

            var response =
                req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(customers);

            return response;
        }


       
        // UPDATE CUSTOMER
        // PUT /api/UpdateCustomer?customerId=C001
        

        [Function("UpdateCustomer")]
        public async Task<HttpResponseData> UpdateCustomer(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "put")] HttpRequestData req)
        {
            var tableClient = GetTableClient();

            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var customerId =
                query["customerId"];

            if (string.IsNullOrWhiteSpace(customerId))
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Provide a customerId query parameter.");

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
                    "Updated customer data is required.");

                return response;
            }

            CustomerData customer;

            try
            {
                customer =
                    JsonSerializer.Deserialize<CustomerData>(
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
                    "Invalid JSON customer data.");

                return response;
            }

            if (customer == null)
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Invalid customer data.");

                return response;
            }

            var escapedCustomerId =
                customerId.Replace("'", "''");

            var existingCustomers =
                tableClient.Query<TableEntity>(
                    $"CustomerId eq '{escapedCustomerId}'");

            foreach (var existing in existingCustomers)
            {
                existing["CustomerId"] = customerId;
                existing["FullName"] = customer.FullName;
                existing["Email"] = customer.Email;
                existing["PhoneNumber"] = customer.PhoneNumber;
                existing["Address"] = customer.Address;

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
                        "Customer updated successfully.",
                    customerId = customerId
                });

                return response;
            }

            var notFoundResponse =
                req.CreateResponse(
                    HttpStatusCode.NotFound);

            await notFoundResponse.WriteStringAsync(
                "Customer not found.");

            return notFoundResponse;
        }

        // DELETE CUSTOMER
        // DELETE /api/DeleteCustomer?customerId=C001
        

        [Function("DeleteCustomer")]
        public async Task<HttpResponseData> DeleteCustomer(
            [HttpTrigger(
                AuthorizationLevel.Function,
                "delete")] HttpRequestData req)
        {
            var tableClient = GetTableClient();

            var query =
                System.Web.HttpUtility.ParseQueryString(
                    req.Url.Query);

            var customerId =
                query["customerId"];

            if (string.IsNullOrWhiteSpace(customerId))
            {
                var response =
                    req.CreateResponse(
                        HttpStatusCode.BadRequest);

                await response.WriteStringAsync(
                    "Provide a customerId query parameter.");

                return response;
            }

            var escapedCustomerId =
                customerId.Replace("'", "''");

            var customers =
                tableClient.Query<TableEntity>(
                    $"CustomerId eq '{escapedCustomerId}'");

            foreach (var entity in customers)
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
                        "Customer deleted successfully.",
                    customerId = customerId
                });

                return response;
            }

            var notFoundResponse =
                req.CreateResponse(
                    HttpStatusCode.NotFound);

            await notFoundResponse.WriteStringAsync(
                "Customer not found.");

            return notFoundResponse;
        }
    }

   
    // CUSTOMER DATA MODEL
   
    public class CustomerData
    {
        public string CustomerId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }
}