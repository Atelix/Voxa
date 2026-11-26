using Voxa.Abstractions.Models;

namespace Voxa.Abstractions.Logic;

/// <summary>The club this installation serves. Until the board saves settings, neutral defaults apply.</summary>
public interface IClubLogic
{
    Task<ClubBrandModel> BrandAsync(CancellationToken cancellationToken);

    Task<ClubSettingsModel> GetAsync(CancellationToken cancellationToken);

    /// <exception cref="DomainValidationException">A font is not bundled, or the logo is not an image data URL.</exception>
    Task<ClubSettingsModel> UpdateAsync(ClubSettingsRequest request, CancellationToken cancellationToken);
}
