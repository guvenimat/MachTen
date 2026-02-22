namespace MACHTEN.Application.DTOs;

/// <summary>
/// Readonly record struct for bulk insert payloads. Lives on the stack when used
/// in spans/arrays, dramatically reducing GC pressure during high-volume ingestion.
/// </summary>
public readonly record struct BulkProductInsertDto(
    string Name,
    string Description,
    decimal Price);
