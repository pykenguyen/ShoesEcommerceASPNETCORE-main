using Microsoft.AspNetCore.Mvc;
using ShoesEcommerce.Services.Interfaces;

namespace ShoesEcommerce.ViewComponents
{
    public class NavbarViewComponent : ViewComponent
    {
        private readonly IProductService _productService;
        private readonly ILogger<NavbarViewComponent> _logger;

        public NavbarViewComponent(IProductService productService, ILogger<NavbarViewComponent> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var categories = await _productService.GetCategoriesForDropdownAsync();
                var brands = await _productService.GetBrandsForDropdownAsync();

                var model = new NavbarViewModel
                {
                    Categories = categories.Select(c => new NavbarItemViewModel 
                    { 
                        Id = c.Id, 
                        Name = c.Name,
                        Slug = GenerateSlug(c.Name)
                    }).ToList(),
                    Brands = brands.Select(b => new NavbarItemViewModel 
                    { 
                        Id = b.Id, 
                        Name = b.Name,
                        Slug = GenerateSlug(b.Name)
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading navbar data");
                return View(new NavbarViewModel());
            }
        }

        private static string GenerateSlug(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            
            // Simple slug generation
            var slug = name.ToLower()
                .Replace(" ", "-")
                .Replace("đ", "d")
                .Replace("á", "a").Replace("à", "a").Replace("ả", "a").Replace("ã", "a").Replace("ạ", "a")
                .Replace("ă", "a").Replace("ắ", "a").Replace("ằ", "a").Replace("ẳ", "a").Replace("ẵ", "a").Replace("ặ", "a")
                .Replace("â", "a").Replace("ấ", "a").Replace("ầ", "a").Replace("ẩ", "a").Replace("ẫ", "a").Replace("ậ", "a")
                .Replace("é", "e").Replace("è", "e").Replace("ẻ", "e").Replace("ẽ", "e").Replace("ẹ", "e")
                .Replace("ê", "e").Replace("ế", "e").Replace("ề", "e").Replace("ể", "e").Replace("ễ", "e").Replace("ệ", "e")
                .Replace("í", "i").Replace("ì", "i").Replace("ỉ", "i").Replace("ĩ", "i").Replace("ị", "i")
                .Replace("ó", "o").Replace("ò", "o").Replace("ỏ", "o").Replace("õ", "o").Replace("ọ", "o")
                .Replace("ô", "o").Replace("ố", "o").Replace("ồ", "o").Replace("ổ", "o").Replace("ỗ", "o").Replace("ộ", "o")
                .Replace("ơ", "o").Replace("ớ", "o").Replace("ờ", "o").Replace("ở", "o").Replace("ỡ", "o").Replace("ợ", "o")
                .Replace("ú", "u").Replace("ù", "u").Replace("ủ", "u").Replace("ũ", "u").Replace("ụ", "u")
                .Replace("ư", "u").Replace("ứ", "u").Replace("ừ", "u").Replace("ử", "u").Replace("ữ", "u").Replace("ự", "u")
                .Replace("ý", "y").Replace("ỳ", "y").Replace("ỷ", "y").Replace("ỹ", "y").Replace("ỵ", "y");
            
            return slug;
        }
    }

    public class NavbarViewModel
    {
        public List<NavbarItemViewModel> Categories { get; set; } = new();
        public List<NavbarItemViewModel> Brands { get; set; } = new();
    }

    public class NavbarItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
    }
}
