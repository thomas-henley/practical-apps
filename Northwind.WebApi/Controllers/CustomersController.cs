using Microsoft.AspNetCore.Mvc; // [Route] [ApiController] ControllerBase
using Northwind.Common;
using Northwind.WebApi.Repositories;

namespace Northwind.WebApi.Controllers;

// base address: api/customers
[Route("api/[controller]")]
[ApiController]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _repo;
    
    // constructor injects repository registered in Startup
    public CustomersController(ICustomerRepository repo)
    {
        _repo = repo;
    }
    
    // GET: api/customers
    // GET: api/customers/?country=[country]
    // this will always return a list of customers (but it might be empty)
    [HttpGet]
    [ProducesResponseType(200, Type = typeof(IEnumerable<Customer>))]
    public async Task<IEnumerable<Customer>> GetCustomers(string? country)
    {
        if (string.IsNullOrWhiteSpace(country))
        {
            return await _repo.RetrieveAllAsync();
        }
        else
        {
            return (await _repo.RetrieveAllAsync()).Where(customer => customer.Country == country);
        }
    }
    
    // GET: api/customer/[id]
    [HttpGet("{id}", Name = nameof(GetCustomer))] // named route
    [ProducesResponseType(200, Type = typeof(Customer))]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCustomer(string id)
    {
        Customer? c = await _repo.RetrieveAsync(id);

        if (c == null) return NotFound();
        return Ok(c);
    }
    
    // POST: api/customers
    // BODY: Customer (JSON, XML)
    [HttpPost]
    [ProducesResponseType(201, Type = typeof(Customer))]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] Customer c)
    {
        if (c is null) return BadRequest();

        Customer? addedCustomer = await _repo.CreateAsync(c);

        if (addedCustomer is null) return BadRequest("Repository failed to create customer.");

        return CreatedAtRoute(
            routeName: nameof(GetCustomer),
            routeValues: new { id = addedCustomer.CustomerId.ToLower() },
            value: addedCustomer);
    }
    
    // PUT: api/customers/[id]
    // BODY: Customer (JSON, XML)
    [HttpPut("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] Customer c)
    {
        id = id.ToUpper();
        c.CustomerId = c.CustomerId.ToUpper();

        if (c is null || c.CustomerId != id) return BadRequest();
        
        Customer? existing = await _repo.RetrieveAsync(id);

        if (existing is null) return NotFound();
        
        await _repo.UpdateAsync(id, c);
        return new NoContentResult();
    }
    
    // DELETE: api/customer/[id]
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (id.Equals("bad"))
        {
            ProblemDetails problemDetails = new()
            {
                Status = StatusCodes.Status400BadRequest,
                Type = "https://localhost:5001/customers/failed-to-delete",
                Title = $"Customer ID {id} found but failed to delete.",
                Detail = "More details like Company Name, Country, and so on.",
                Instance = HttpContext.Request.Path
            };
            return BadRequest(problemDetails);
        }
            
        id = id.ToUpper();

        if (id is null) return BadRequest();

        Customer? existing = await _repo.RetrieveAsync(id);

        if (existing is null) return NotFound();

        bool? deleted = await _repo.DeleteAsync(id);
        if (deleted.HasValue && deleted.Value)
        {
            return new NoContentResult();
        }
        else
        {
            return BadRequest($"Customer {id} was found but failed to delete.");
        }
    }
}