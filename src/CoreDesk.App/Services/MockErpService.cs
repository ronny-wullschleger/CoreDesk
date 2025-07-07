// src/CoreDesk.App/Services/MockErpService.cs

namespace CoreDesk.App.Services;

// Simuliert den ERP Connector Service
public class MockErpService
{
    private readonly Dictionary<string, (string CustomerName, string CustomerType)> _customers = new()
    {
        { "anna.meier@privat.com", ("Anna Meier", "Privat") },
        { "john.doe@business.com", ("John Doe", "Business") },
        { "support@acme.inc", ("ACME Inc. Support", "Business") }
    };

    public Task<(string CustomerName, string CustomerType)> GetCustomerInfoByEmailAsync(string email)
    {
        if (_customers.TryGetValue(email, out var customer))
        {
            return Task.FromResult(customer);
        }
        return Task.FromResult(("Unbekannter Kunde", "Unbekannt"));
    }
}