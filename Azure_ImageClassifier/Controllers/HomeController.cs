using Azure_ImageClassifier.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace Azure_ImageClassifier.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;
        private readonly IComputerVisionClient _computerVision;

        public HomeController(ILogger<HomeController> logger, IConfiguration config, IComputerVisionClient computerVision)
        {
            _logger = logger;
            _config = config;
            _computerVision = computerVision;
        }

        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                try
                {
                    using (var ms = new MemoryStream())
                    {
                        await imageFile.CopyToAsync(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        var detectObjectsResults = await _computerVision.DetectObjectsInStreamAsync(ms);
                        ViewBag.Results = detectObjectsResults.Objects;

                        var processedImage = DrawRectanglesOnImage(ms.ToArray(), detectObjectsResults.Objects);
                        ViewBag.ProcessedImage = Convert.ToBase64String(processedImage);

                    }
                }
                catch (Exception ex)
                {
                    return RedirectToAction("Error", "Home", new { errorMessage = ex.Message });
                }
            }
            return View("Index");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(string errorMessage)
        {
            ViewBag.ErrorMessage = errorMessage;
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private byte[] DrawRectanglesOnImage(byte[] imageBytes, IList<DetectedObject> objects)
        {
            // Load the image from byte array
            using (var ms = new MemoryStream(imageBytes))
            {
                using (var originalImage = Image.FromStream(ms))
                {
                    using (var graphics = Graphics.FromImage(originalImage))
                    {
                        foreach (var objectInfo in objects)
                        {
                            var rect = new Rectangle(objectInfo.Rectangle.X, objectInfo.Rectangle.Y, objectInfo.Rectangle.W,
                                                      objectInfo.Rectangle.H);
                            graphics.DrawRectangle(new Pen(Color.Red, 3), rect);

                            // Draw the object's name
                            using var font = new Font("Arial", 16);
                            using var brush = new SolidBrush(Color.Red);

                            graphics.DrawString(objectInfo.ObjectProperty, font, brush, objectInfo.Rectangle.X,
                                                objectInfo.Rectangle.Y - 20);
                        }
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        originalImage.Save(memoryStream, ImageFormat.Jpeg);
                        return memoryStream.ToArray();
                    }
                }
            }
        }
    }
}