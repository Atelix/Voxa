using Voxa.Abstractions.Models;

namespace Voxa.Abstractions.Logic;

/// <summary>Signing the board in with the shared board password.</summary>
public interface IAdminAuthLogic
{
    AdminSessionModel Status();

    /// <exception cref="DomainValidationException">The password is wrong.</exception>
    /// <exception cref="DomainRateLimitedException">Too many wrong passwords from this client.</exception>
    Task<AdminSessionModel> SignInAsync(SignInRequest request);

    Task<AdminSessionModel> SignOutAsync();
}
