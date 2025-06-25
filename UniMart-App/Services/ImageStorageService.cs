using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace UniMart_App.Services
{
    public class ImageStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly string _defaultImagesFolder = "product_images";

        public ImageStorageService(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        private void EnsureDirectoryExists(string folder)
        {
            string path = Path.Combine(_environment.WebRootPath, folder);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public async Task<string?> SaveImageAsync(IFormFile? imageFile, string? folder = null)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return null;
            }

            folder = folder ?? _defaultImagesFolder;
            EnsureDirectoryExists(folder);

            // Validate file is an image
            if (!IsImageFile(imageFile))
            {
                throw new ArgumentException("The uploaded file is not a valid image.");
            }

            // Generate a unique filename
            string fileName = $"{Guid.NewGuid()}{Path.GetExtension(imageFile.FileName)}";
            string filePath = Path.Combine(_environment.WebRootPath, folder, fileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            // Return the relative URL
            return $"/{folder}/{fileName}";
        }

        public async Task DeleteImageAsync(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
            {
                return;
            }

            try
            {
                // Extract the filename from the URL
                string fileName = Path.GetFileName(imageUrl);
                string folder = Path.GetDirectoryName(imageUrl)?.TrimStart('/') ?? _defaultImagesFolder;
                string filePath = Path.Combine(_environment.WebRootPath, folder, fileName);

                // Delete the file if it exists
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                }
            }
            catch (Exception)
            {
                // Log the error if needed but don't throw
            }
        }

        private bool IsImageFile(IFormFile file)
        {
            // Check file extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (string.IsNullOrEmpty(extension) || !Array.Exists(allowedExtensions, e => e == extension))
            {
                return false;
            }

            // Check MIME type
            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            return Array.Exists(allowedMimeTypes, m => m == file.ContentType);
        }
    }
}
