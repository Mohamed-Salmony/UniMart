using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using UniMart_App.Models;

namespace UniMart_App.ViewModels
{
    public class FacultyManagementViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public required string Name { get; set; }

        [StringLength(500)]        public string? Description { get; set; }

        public string? CurrentImageUrl { get; set; }

        [Display(Name = "Faculty Image")]
        public IFormFile? ImageFile { get; set; }
    }
}
