using System.ComponentModel.DataAnnotations;

namespace UniMart_App.Models
{
    public class AcademicYear
    {
        public AcademicYear()
        {
            Users = new HashSet<ApplicationUser>();
        }

        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Year { get; set; } = string.Empty;

        public virtual ICollection<ApplicationUser> Users { get; set; }
    }
}
