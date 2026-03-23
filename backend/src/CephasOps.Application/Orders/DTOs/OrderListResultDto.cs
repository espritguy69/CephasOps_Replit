namespace CephasOps.Application.Orders.DTOs;

/// <summary>
/// Paged result for orders list (e.g. Reports Hub, API with page/pageSize).
/// </summary>
public class OrderListResultDto
{
    public List<OrderDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
