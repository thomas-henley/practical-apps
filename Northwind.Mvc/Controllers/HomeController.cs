using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Northwind.Mvc.Models;
using Northwind.Common;
using Microsoft.EntityFrameworkCore;

namespace Northwind.Mvc.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly NorthwindContext _db;

    public HomeController(ILogger<HomeController> logger, NorthwindContext injectedContext)
    {
        _logger = logger;
        _db = injectedContext;
    }

    [ResponseCache(Duration = 10, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Index()
    {
        HomeIndexViewModel model = new(
            VisitorCount: (new Random()).Next(1, 1001),
            Categories: await _db.Categories.ToListAsync(),
            Products: await _db.Products.ToListAsync()
        );
        
        return View(model);
    }

    [Route("privacy")]
    public IActionResult Privacy()
    {
        return View();
    }

    public async Task<IActionResult> ProductDetail(int? id)
    {
        if (!id.HasValue)
        {
            return BadRequest("You must pass a product ID in the route, for example, /Home/ProductDetail/21");
        }

        Product? model = await _db.Products.SingleOrDefaultAsync(p => p.ProductId == id);

        if (model is null)
        {
            return NotFound($"ProductId {id} not found.");
        }

        return View(model);
    }

    public IActionResult ProductsThatCostMoreThan(decimal? price)
    {
        if (!price.HasValue)
        {
            return BadRequest(
                "You must pass a product price in the query string, for example, /Home/ProductsThatCostMoreThan?price=50");
        }

        IEnumerable<Product> model = _db.Products
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Where(p => p.UnitPrice > price);

        if (!model.Any())
        {
            return NotFound($"No products cost more than {price:C}.");
        }

        ViewData["MaxPrice"] = price.Value.ToString("C");
        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
