namespace Gavel.SharedKernel;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? Id { get; }

    string? DisplayName { get; }

    Guid RequiredId => Id ?? throw new UnauthorizedAccessException("The request is not authenticated.");
}
