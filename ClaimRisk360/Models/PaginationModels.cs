namespace ClaimRisk360.Models;

/// <summary>
/// Generic paginated result container
/// </summary>
/// <typeparam name="T">Type of items in the result</typeparam>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }

    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
    public int PreviousPage => CurrentPage - 1;
    public int NextPage => CurrentPage + 1;
}

/// <summary>
/// Pagination parameters for queries
/// </summary>
public class PaginationParams
{
    private int _pageNumber = 1;
    private int _pageSize = 50;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = Math.Max(1, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Min(Math.Max(1, value), 100); // Cap at 100
    }

    public int Skip => (PageNumber - 1) * PageSize;
    public int Take => PageSize;
}
