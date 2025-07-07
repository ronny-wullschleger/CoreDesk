using CoreDesk.App.Models;

namespace CoreDesk.App.Services;

public class TicketFilterService
{
    private readonly MockErpService _erpService;

    public TicketFilterService(MockErpService erpService)
    {
        _erpService = erpService;
    }

    public List<Ticket> ApplyFilters(
        List<Ticket> tickets, 
        string searchTerm, 
        TicketStatus? statusFilter, 
        string customerTypeFilter, 
        string dateRangeFilter)
    {
        return tickets.Where(ticket => 
        {
            // Search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                if (!ticket.Subject.ToLower().Contains(searchLower) &&
                    !ticket.CustomerEmail.ToLower().Contains(searchLower) &&
                    !(ticket.OrderId?.ToLower().Contains(searchLower) ?? false))
                {
                    return false;
                }
            }

            // Status filter
            if (statusFilter.HasValue && ticket.Status != statusFilter.Value)
            {
                return false;
            }

            // Customer type filter
            if (!string.IsNullOrWhiteSpace(customerTypeFilter))
            {
                var customerInfo = GetCustomerInfoSync(ticket.CustomerEmail);
                if (customerInfo.CustomerType != customerTypeFilter)
                {
                    return false;
                }
            }

            // Date range filter
            if (!string.IsNullOrWhiteSpace(dateRangeFilter))
            {
                if (!IsTicketInDateRange(ticket, dateRangeFilter))
                {
                    return false;
                }
            }

            return true;
        }).OrderByDescending(t => t.CreatedAt).ToList();
    }

    public bool HasActiveFilters(string searchTerm, TicketStatus? statusFilter, string customerTypeFilter, string dateRangeFilter)
    {
        return !string.IsNullOrWhiteSpace(searchTerm) ||
               statusFilter.HasValue ||
               !string.IsNullOrWhiteSpace(customerTypeFilter) ||
               !string.IsNullOrWhiteSpace(dateRangeFilter);
    }

    private bool IsTicketInDateRange(Ticket ticket, string dateRangeFilter)
    {
        var now = DateTime.Now;
        var ticketDate = ticket.CreatedAt.Date;
        
        return dateRangeFilter switch
        {
            "today" => ticketDate == now.Date,
            "week" => IsInCurrentWeek(ticketDate, now),
            "month" => ticketDate.Year == now.Year && ticketDate.Month == now.Month,
            _ => true
        };
    }

    private static bool IsInCurrentWeek(DateTime ticketDate, DateTime now)
    {
        var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);
        return ticketDate >= startOfWeek && ticketDate <= now.Date;
    }

    private (string CustomerName, string CustomerType) GetCustomerInfoSync(string email)
    {
        // This is a simplified sync version for filtering. In a real app, you'd cache this data.
        var mockData = new Dictionary<string, (string, string)>
        {
            { "anna.meier@privat.com", ("Anna Meier", "Privat") },
            { "john.doe@business.com", ("John Doe", "Business") },
            { "support@acme.inc", ("ACME Inc. Support", "Business") },
            { "maria.garcia@privat.com", ("Maria Garcia", "Privat") },
            { "tech@innovate.corp", ("Innovate Corp", "Business") },
            { "customer@retail.shop", ("Retail Shop GmbH", "Business") },
            { "peter.mueller@home.de", ("Peter Müller", "Privat") },
            { "info@startup.tech", ("StartUp Tech", "Business") },
            { "support@global.enterprise", ("Global Enterprise", "Business") },
            { "sarah.weber@email.de", ("Sarah Weber", "Privat") }
        };
        
        return mockData.TryGetValue(email, out var customer) ? customer : ("Unbekannter Kunde", "Unbekannt");
    }
}
