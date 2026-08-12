using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ABCRetail.Controllers
{
    public class OrdersController : Controller
    {
        private readonly QueueStorageService _queueStorageService;

        public OrdersController(
            QueueStorageService queueStorageService)
        {
            _queueStorageService = queueStorageService;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var messages =
                await _queueStorageService.PeekMessagesAsync();

            return View(messages);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            OrderMessage order)
        {
            if (!ModelState.IsValid)
            {
                return View(order);
            }

            order.Action = "Processing order";
            order.CreatedAt = DateTime.UtcNow;

            await _queueStorageService
                .SendMessageAsync(order);

            return RedirectToAction(nameof(Index));
        }
        // GET: Orders/Inventory
        public IActionResult Inventory()
        {
            return View();
        }
        // POST: Orders/Inventory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Inventory(
            InventoryMessage inventory)
        {
            if (!ModelState.IsValid)
            {
                return View(inventory);
            }

            inventory.Action = "Inventory update";
            inventory.CreatedAt = DateTime.UtcNow;

            await _queueStorageService
                .SendMessageAsync(inventory);

            return RedirectToAction(nameof(Index));
        }
    }
}