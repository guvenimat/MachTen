using MACHTEN.Application.DTOs;

namespace MACHTEN.Api.Features.Products.BulkCreateProducts;

public sealed record BulkCreateProductsCommand(IReadOnlyList<BulkProductInsertDto> Products);
