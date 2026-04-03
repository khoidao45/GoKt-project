namespace Gokt.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string resource, object id)
        : base("NOT_FOUND", $"{resource} with id '{id}' was not found.") { }
}

public class ConflictException : DomainException
{
    public ConflictException(string message)
        : base("CONFLICT", message) { }
}

public class UnauthorizedException : DomainException
{
    public UnauthorizedException(string message = "Invalid credentials.")
        : base("UNAUTHORIZED", message) { }
}

public class ForbiddenException : DomainException
{
    public ForbiddenException(string message)
        : base("FORBIDDEN", message) { }
}

public class TooManyRequestsException : DomainException
{
    public TooManyRequestsException(string message)
        : base("TOO_MANY_REQUESTS", message) { }
}

public class ValidationException : DomainException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("VALIDATION_ERROR", "One or more validation errors occurred.")
    {
        Errors = errors;
    }
}
