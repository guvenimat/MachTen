namespace MACHTEN.Domain.Exceptions;

public sealed class InvalidOrderException(string message) : DomainException(message);
