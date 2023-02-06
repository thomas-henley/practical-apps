using Microsoft.EntityFrameworkCore.ChangeTracking; // EntityEntry<T>
using Northwind.Common;
using System.Collections.Concurrent;

namespace Northwind.WebApi.Repositories;

public class CustomerRepository : ICustomerRepository
{
    // use a static thread-safe dictionary field to cache the customers
    private static ConcurrentDictionary<string, Customer>? _customerCache;
    
    // use an instance data context field because it should not be cached due to their internal caching
    private NorthwindContext _db;

    public CustomerRepository(NorthwindContext injectedContext)
    {
        _db = injectedContext;
        
        // pre-load customers from database as a normal
        // Dictionary with CustomerId as the key,
        // then convert to a thread-safe ConcurrentDictionary
        if (_customerCache is null)
        {
            _customerCache = new ConcurrentDictionary<string, Customer>(
                _db.Customers.ToDictionary(c => c.CustomerId));
        }
    }

    private Customer UpdateCache(string id, Customer c)
    {
        Customer? old;
        if (_customerCache is not null)
        {
            if (_customerCache.TryGetValue(id, out old))
            {
                if (_customerCache.TryUpdate(id, c, old))
                {
                    return c;
                }
            }
        }

        return null;
    }
    
    public async Task<Customer?> CreateAsync(Customer c)
    {
        c.CustomerId = c.CustomerId.ToUpper();

        EntityEntry<Customer> added = await _db.Customers.AddAsync(c);
        int affected = await _db.SaveChangesAsync();

        if (affected == 1)
        {
            if (_customerCache is null) return c;
            return _customerCache.AddOrUpdate(c.CustomerId, c, UpdateCache);
        }
        else
        {
            return null;
        }
    }

    public Task<IEnumerable<Customer>> RetrieveAllAsync()
    {
        // for performance, get from cache
        return Task.FromResult(_customerCache?.Values ?? Enumerable.Empty<Customer>());
    }

    public Task<Customer?> RetrieveAsync(string id)
    {
        // for performance, get from cache
        
        if (_customerCache is null) return null!;
        _customerCache.TryGetValue(id, out Customer? c);
        return Task.FromResult(c);
    }

    public async Task<Customer?> UpdateAsync(string id, Customer c)
    {
        // normalize customer id
        id = id.ToUpper();
        c.CustomerId = c.CustomerId.ToUpper();
        
        // update in database
        _db.Customers.Update(c);
        int affected = await _db.SaveChangesAsync();

        if (affected == 1)
        {
            // update in cache
            return UpdateCache(id, c);
        }

        return null;
    }

    public async Task<bool?> DeleteAsync(string id)
    {
        id = id.ToUpper();
        
        // remove from database
        Customer? c = _db.Customers.Find(id);

        if (c is null) return null;

        _db.Customers.Remove(c);

        int affected = await _db.SaveChangesAsync();

        if (affected == 1)
        {
            if (_customerCache is null) return null;
            // remove from cache
            return _customerCache.TryRemove(id, out c);
        }
        else
        {
            return null;
        }
    }
}