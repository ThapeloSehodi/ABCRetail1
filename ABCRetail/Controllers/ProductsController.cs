using ABCRetail.Models;
using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class ProductsController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly BlobStorageService _blobStorageService;

        public ProductsController(
            TableStorageService tableStorageService,
            BlobStorageService blobStorageService)
        {
            _tableStorageService = tableStorageService;
            _blobStorageService = blobStorageService;
        }
        // GET: Products
        public async Task<IActionResult> Index()
        {
            var products =
                await _tableStorageService.GetProductsAsync();

            return View(products);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
    Product product,
    IFormFile imageFile)
        {
            if (!ModelState.IsValid)
            {
                return View(product);
            }

            product.PartitionKey = "Products";

            if (imageFile != null && imageFile.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString()
                               + Path.GetExtension(imageFile.FileName);

                using var stream = imageFile.OpenReadStream();

                await _blobStorageService.UploadImageAsync(
     stream,
     fileName,
     imageFile.ContentType);

                product.ImageName = fileName;
            }

            await _tableStorageService.AddProductAsync(product);

            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Details
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product =
                await _tableStorageService.GetProductAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
        // GET: Products/Edit
        public async Task<IActionResult> Edit(string id)
        {
            var product = await _tableStorageService.GetProductAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }


        // POST: Products/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
    string id,
    Product product,
    IFormFile? imageFile)
        {
            if (id != product.RowKey)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(product);
            }

            // Get the existing product from Azure Table Storage
            var existingProduct =
                await _tableStorageService.GetProductAsync(id);

            if (existingProduct == null)
            {
                return NotFound();
            }

            // Keep the Azure Table keys
            product.PartitionKey = existingProduct.PartitionKey;
            product.RowKey = existingProduct.RowKey;

            // Upload a new image if one was selected
            if (imageFile != null && imageFile.Length > 0)
            {
                string fileName =
                    Guid.NewGuid().ToString() +
                    Path.GetExtension(imageFile.FileName);

                await _blobStorageService.UploadImageAsync(
                    imageFile.OpenReadStream(),
                    fileName,
                    imageFile.ContentType);

                // Save the new image name in Azure Table Storage
                product.ImageName = fileName;
            }
            else
            {
                // No new image selected, so keep the old one
                product.ImageName = existingProduct.ImageName;
            }

            await _tableStorageService.UpdateProductAsync(product);

            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Delete
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product =
                await _tableStorageService.GetProductAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // POST: Products/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            await _tableStorageService.DeleteProductAsync(id);

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Image(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var product =
                await _tableStorageService.GetProductAsync(id);

            if (product == null ||
                string.IsNullOrEmpty(product.ImageName))
            {
                return NotFound();
            }

            var image =
                await _blobStorageService.DownloadImageAsync(
                    product.ImageName);

            if (image == null)
            {
                return NotFound();
            }

            return File(
                image.Value.Stream,
                image.Value.ContentType);
        }
    }
}
