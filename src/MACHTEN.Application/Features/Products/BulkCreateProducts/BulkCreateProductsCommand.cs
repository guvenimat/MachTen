using MACHTEN.Application.DTOs;

namespace MACHTEN.Application.Features.Products.BulkCreateProducts;

public sealed record BulkCreateProductsCommand(IReadOnlyList<BulkProductInsertDto> Products);
