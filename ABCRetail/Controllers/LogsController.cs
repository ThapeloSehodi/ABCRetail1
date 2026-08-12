using ABCRetail.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetail.Controllers
{
    public class LogsController : Controller
    {
        private readonly LogStorageService _logStorageService;

        public LogsController(
            LogStorageService logStorageService)
        {
            _logStorageService = logStorageService;
        }

        // GET: Logs
        public async Task<IActionResult> Index()
        {
            ViewBag.PdfFiles =
                await _logStorageService.GetPdfFilesAsync();

            var logs =
                await _logStorageService.ReadLogAsync();

            return View(model: logs);
        }

        // GET: Logs/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Logs/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                ModelState.AddModelError(
                    "",
                    "Please enter a log message.");

                return View();
            }

            await _logStorageService.WriteLogAsync(
                message);

            return RedirectToAction(nameof(Index));
        }

        // Download the log file
        public async Task<IActionResult> Download()
        {
            var stream =
                await _logStorageService.DownloadLogAsync();

            if (stream == null)
            {
                return NotFound();
            }

            return File(
                stream,
                "text/plain",
                "application.log");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPdf(
    IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                TempData["Error"] =
                    "Please select a PDF file.";

                return RedirectToAction(nameof(Index));
            }

            if (Path.GetExtension(pdfFile.FileName)
                .ToLower() != ".pdf")
            {
                TempData["Error"] =
                    "Only PDF files are allowed.";

                return RedirectToAction(nameof(Index));
            }

            await _logStorageService.UploadPdfAsync(pdfFile);

            TempData["Success"] =
                "PDF uploaded successfully to Azure Files.";

            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> DownloadPdf(
    string fileName)
        {
            var stream =
                await _logStorageService
                    .DownloadPdfAsync(fileName);

            if (stream == null)
            {
                return NotFound();
            }

            return File(
                stream,
                "application/pdf",
                Path.GetFileName(fileName));
        }
    }
}