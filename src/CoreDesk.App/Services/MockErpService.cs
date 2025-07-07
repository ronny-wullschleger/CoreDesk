// src/CoreDesk.App/Services/MockErpService.cs

using CoreDesk.App.Models;

namespace CoreDesk.App.Services;

// Simuliert den ERP Connector Service mit erweiterten Funktionen
public class MockErpService
{
    private readonly Dictionary<string, Customer> _customers = new();
    private readonly Dictionary<string, Order> _orders = new();
    private readonly Dictionary<string, List<Order>> _customerOrders = new();

    public MockErpService()
    {
        InitializeMockData();
    }

    private void InitializeMockData()
    {
        // Customers
        var customers = new List<Customer>
        {
            new Customer { Email = "anna.meier@privat.com", Name = "Anna Meier", Type = CustomerType.Privat, CustomerId = "CUST-001", CreatedAt = DateTime.Now.AddYears(-2) },
            new Customer { Email = "john.doe@business.com", Name = "John Doe", Type = CustomerType.Business, CustomerId = "CUST-B001", CreatedAt = DateTime.Now.AddYears(-1) },
            new Customer { Email = "support@acme.inc", Name = "ACME Inc. Support", Type = CustomerType.Business, CustomerId = "CUST-B002", CreatedAt = DateTime.Now.AddMonths(-8) },
            new Customer { Email = "maria.garcia@privat.com", Name = "Maria Garcia", Type = CustomerType.Privat, CustomerId = "CUST-002", CreatedAt = DateTime.Now.AddMonths(-6) },
            new Customer { Email = "tech@innovate.corp", Name = "Innovate Corp", Type = CustomerType.Business, CustomerId = "CUST-B003", CreatedAt = DateTime.Now.AddMonths(-4) },
            new Customer { Email = "customer@retail.shop", Name = "Retail Shop GmbH", Type = CustomerType.Business, CustomerId = "CUST-B004", CreatedAt = DateTime.Now.AddMonths(-10) },
            new Customer { Email = "peter.mueller@home.de", Name = "Peter Müller", Type = CustomerType.Privat, CustomerId = "CUST-003", CreatedAt = DateTime.Now.AddMonths(-3) }
        };

        foreach (var customer in customers)
        {
            _customers[customer.Email] = customer;
        }

        // Orders
        var orders = new List<Order>
        {
            new Order { OrderId = "100-58273", CustomerEmail = "anna.meier@privat.com", OrderDate = DateTime.Now.AddDays(-10), Status = OrderStatus.Shipped, Total = 299.99m,
                Items = new List<OrderItem> { new OrderItem { ProductId = "PROD-001", ProductName = "Laptop Stand", Quantity = 1, Price = 299.99m } },
                ShippingAddress = "Musterstraße 123, 12345 Berlin" },
            new Order { OrderId = "200-12345", CustomerEmail = "maria.garcia@privat.com", OrderDate = DateTime.Now.AddDays(-5), Status = OrderStatus.Delivered, Total = 89.99m,
                Items = new List<OrderItem> { new OrderItem { ProductId = "PROD-002", ProductName = "Wireless Mouse", Quantity = 1, Price = 89.99m } },
                ShippingAddress = "Testweg 456, 54321 Hamburg" },
            new Order { OrderId = "300-67890", CustomerEmail = "customer@retail.shop", OrderDate = DateTime.Now.AddDays(-15), Status = OrderStatus.Processing, Total = 1299.99m,
                Items = new List<OrderItem> { new OrderItem { ProductId = "PROD-003", ProductName = "Business Monitor", Quantity = 2, Price = 649.99m } },
                ShippingAddress = "Firmenstr. 789, 98765 München" },
            new Order { OrderId = "400-11111", CustomerEmail = "anna.meier@privat.com", OrderDate = DateTime.Now.AddDays(-60), Status = OrderStatus.Delivered, Total = 49.99m,
                Items = new List<OrderItem> { new OrderItem { ProductId = "PROD-004", ProductName = "USB Cable", Quantity = 1, Price = 49.99m } },
                ShippingAddress = "Musterstraße 123, 12345 Berlin" }
        };

        foreach (var order in orders)
        {
            _orders[order.OrderId] = order;
            
            if (!_customerOrders.ContainsKey(order.CustomerEmail))
            {
                _customerOrders[order.CustomerEmail] = new List<Order>();
            }
            _customerOrders[order.CustomerEmail].Add(order);
        }
    }

    public Task<Customer?> GetCustomerByEmailAsync(string email)
    {
        _customers.TryGetValue(email, out var customer);
        return Task.FromResult(customer);
    }

    public Task<(string CustomerName, string CustomerType)> GetCustomerInfoByEmailAsync(string email)
    {
        if (_customers.TryGetValue(email, out var customer))
        {
            return Task.FromResult((customer.Name, customer.Type.ToString()));
        }
        return Task.FromResult(("Unbekannter Kunde", "Unbekannt"));
    }

    public Task<Order?> GetOrderByIdAsync(string orderId)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task<List<Order>> GetCustomerOrdersAsync(string customerEmail)
    {
        if (_customerOrders.TryGetValue(customerEmail, out var orders))
        {
            return Task.FromResult(orders.OrderByDescending(o => o.OrderDate).ToList());
        }
        return Task.FromResult(new List<Order>());
    }

    public Task<List<Customer>> GetAllCustomersAsync()
    {
        return Task.FromResult(_customers.Values.ToList());
    }

    public Task<bool> IsBusinessCustomerAsync(string email)
    {
        if (_customers.TryGetValue(email, out var customer))
        {
            return Task.FromResult(customer.Type == CustomerType.Business);
        }
        return Task.FromResult(false);
    }

    public Task<string?> ExtractOrderIdFromTextAsync(string text)
    {
        // Regex patterns for different order ID formats
        var patterns = new[]
        {
            @"\b(\d{3}-\d{5,})\b",  // 100-58273
            @"\b(ORD-\d{6,})\b",    // ORD-123456
            @"\b(B\d{8,})\b"        // B12345678
        };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern);
            if (match.Success)
            {
                var orderId = match.Value;
                if (_orders.ContainsKey(orderId))
                {
                    return Task.FromResult<string?>(orderId);
                }
            }
        }

        return Task.FromResult<string?>(null);
    }
}