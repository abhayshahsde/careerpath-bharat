namespace CareerPath.Contracts.V1.Common;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage,
    bool HasPreviousPage
)
{
    public static PagedResponse<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount) =>
        new(
            Items: items,
            Page: page,
            PageSize: pageSize,
            TotalCount: totalCount,
            HasNextPage: page * pageSize < totalCount,
            HasPreviousPage: page > 1
        );
}

public sealed record PaginationRequest(
    int Page = 1,
    int PageSize = 20
)
{
    public int Page { get; init; } = Math.Max(1, Page);
    public int PageSize { get; init; } = Math.Clamp(PageSize, 1, 100);
    public int Offset => (Page - 1) * PageSize;
}
