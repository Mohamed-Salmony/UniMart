using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using UniMart_App.Models;

namespace UniMart_App.Models
{
    public class EditUserViewModel
    {
        public EditUserViewModel()
        {
            Id = string.Empty;
            Email = string.Empty;
            FullName = string.Empty;
            UserRoles = new List<string>();
            AllRoles = new List<IdentityRole>();
            Faculties = new List<Faculty>();
            AcademicYears = new List<AcademicYear>();
        }
        
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public int FacultyId { get; set; }
        public int AcademicYearId { get; set; }
        public List<string> UserRoles { get; set; }
        public IEnumerable<IdentityRole> AllRoles { get; set; }
        public IEnumerable<Faculty> Faculties { get; set; }
        public IEnumerable<AcademicYear> AcademicYears { get; set; }
    }

    public class CreateUserViewModel
    {
        public CreateUserViewModel()
        {
            Email = string.Empty;
            Password = string.Empty;
            ConfirmPassword = string.Empty;
            FullName = string.Empty;
            Faculties = new List<Faculty>();
            AcademicYears = new List<AcademicYear>();
            Roles = new List<IdentityRole>();
        }
        
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public int FacultyId { get; set; }

        [Required]
        public int AcademicYearId { get; set; }

        public IEnumerable<Faculty> Faculties { get; set; }
        public IEnumerable<AcademicYear> AcademicYears { get; set; }
        public IEnumerable<IdentityRole> Roles { get; set; }
    }
}
