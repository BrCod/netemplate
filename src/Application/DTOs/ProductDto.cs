namespace Netemplate.Application.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    decimal Price,
    string Currency,
    string? Description,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsActive
);

public sealed record CreateProductRequest(
    string Name,
    decimal Price,
    string Currency,
    string? Description
);

public sealed record UpdateProductRequest(
    string? Name,
    decimal? Price,
    string? Currency,
    string? Description
);

public sealed record ProductListResponse(
    IReadOnlyList<ProductDto> Items,
    int Total,
    int Skip,
    int Take
);
