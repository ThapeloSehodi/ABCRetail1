using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class CustomersController : Controller
    {
        private readonly TableStorageService _tableStorageService;

        public CustomersController(
            TableStorageService tableStorageService)
        {
            _tableStorageService = tableStorageService;
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            var customers =
                await _tableStorageService.GetCustomersAsync();

            return View(customers);
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            customer.PartitionKey = "Customers";

            await _tableStorageService.AddCustomerAsync(customer);

            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Details
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer =
                await _tableStorageService.GetCustomerAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // GET: Customers/Edit
        public async Task<IActionResult> Edit(string id)
        {
            var customer = await _tableStorageService.GetCustomerAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }


        // POST: Customers/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            string id,
            Customer customer)
        {
            if (id != customer.RowKey)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            // Keep the existing PartitionKey and RowKey
            customer.PartitionKey = "Customers";
            customer.RowKey = id;

            await _tableStorageService.UpdateCustomerAsync(customer);

            return RedirectToAction(nameof(Index));
        }

        // GET: Customers/Delete
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var customer =
                await _tableStorageService.GetCustomerAsync(id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(customer);
        }

        // POST: Customers/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            await _tableStorageService.DeleteCustomerAsync(id);

            return RedirectToAction(nameof(Index));
        }
    }
}