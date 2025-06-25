using System.Collections.Generic;
using UniMart_App.Models;

namespace UniMart_App.ViewModels
{
    public class CatalogViewModel
    {
        public List<Product> Products { get; set; }
        public List<Faculty> Faculties { get; set; }
        public string CurrentFaculty { get; set; }
        public string SearchQuery { get; set; }
        public string SortOption { get; set; }
        
        public CatalogViewModel()
        {
            Products = new List<Product>();
            Faculties = new List<Faculty>();
            CurrentFaculty = string.Empty;
            SearchQuery = string.Empty;
            SortOption = string.Empty;
        }
    }

    public class ProductDetailsViewModel
    {
        public Product? Product { get; set; }
        public List<Product> RelatedProducts { get; set; }
        public RatingViewModel Rating { get; set; }
        public bool IsFavorite { get; set; }
        
        public ProductDetailsViewModel()
        {
            RelatedProducts = new List<Product>();
            Rating = new RatingViewModel();
        }
    }

    public class RatingViewModel
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
    }
}
