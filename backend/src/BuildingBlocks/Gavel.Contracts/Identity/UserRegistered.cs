namespace Gavel.Contracts.Identity;

[IntegrationEvent("identity.user-registered.v1")]
public sealed record UserRegistered(Guid UserId, string Email, string DisplayName) : IIntegrationEvent;
