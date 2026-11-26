using System.ComponentModel.DataAnnotations;
using Voxa.Persistence.Entity;

namespace Voxa.Abstractions.Models;

/// <summary>The club's name, brand and contact address: public, the app themes itself with it.</summary>
public sealed record ClubBrandModel
{
    public required string ClubName { get; init; }

    public required string? ContactEmail { get; init; }

    /// <summary>The club's language (<see cref="Languages.Supported"/>), for mails and for devices whose language Voxa lacks.</summary>
    public required string Language { get; init; }

    public required string PrimaryColor { get; init; }

    public required string SecondaryColor { get; init; }

    public required string HeadingFont { get; init; }

    public required string BodyFont { get; init; }

    public required string? LogoDataUrl { get; init; }
}

/// <summary>Every club setting, for the board's settings page and to copy a club's setup to a new installation.</summary>
public sealed record ClubSettingsModel
{
    public required string ClubName { get; init; }

    public required string? ContactEmail { get; init; }

    /// <summary>The club's language (<see cref="Languages.Supported"/>), for mails and for devices whose language Voxa lacks.</summary>
    public required string Language { get; init; }

    public required string PrimaryColor { get; init; }

    public required string SecondaryColor { get; init; }

    public required string HeadingFont { get; init; }

    public required string BodyFont { get; init; }

    public required string? LogoDataUrl { get; init; }

    public required string MailFromName { get; init; }

    public required string? MailFromAddress { get; init; }

    public required string? MailReplyTo { get; init; }

    public required int AdvanceVotingMaxDays { get; init; }

    public required string? AdvanceVotingArticle { get; init; }

    public required MinorVoting MinorVoting { get; init; }

    public required int AdultAge { get; init; }

    /// <summary>
    /// Whether the server has a key for the member import API (<c>Import__ApiKey</c>), so a member administration can
    /// send the member list. The key itself is never sent.
    /// </summary>
    public required bool ImportApiConfigured { get; init; }

    /// <summary>The fonts the app bundles; the font settings must be one of these.</summary>
    public required IReadOnlyList<string> AvailableFonts { get; init; }
}

public sealed class ClubSettingsRequest
{
    [Required]
    [StringLength(200)]
    public required string ClubName { get; init; }

    [StringLength(320)]
    [EmailAddress]
    public string? ContactEmail { get; init; }

    /// <summary>One of <see cref="Languages.Supported"/>; empty keeps the default language.</summary>
    [StringLength(8)]
    public string? Language { get; init; }

    [Required]
    [RegularExpression("^#[0-9A-Fa-f]{6}$")]
    public required string PrimaryColor { get; init; }

    [Required]
    [RegularExpression("^#[0-9A-Fa-f]{6}$")]
    public required string SecondaryColor { get; init; }

    [Required]
    [StringLength(60)]
    public required string HeadingFont { get; init; }

    [Required]
    [StringLength(60)]
    public required string BodyFont { get; init; }

    /// <summary>A data URL of at most about 750 kB.</summary>
    [StringLength(1_000_000)]
    public string? LogoDataUrl { get; init; }

    [Required]
    [StringLength(200)]
    public required string MailFromName { get; init; }

    [StringLength(320)]
    [EmailAddress]
    public string? MailFromAddress { get; init; }

    [StringLength(320)]
    [EmailAddress]
    public string? MailReplyTo { get; init; }

    [Range(0, 365)]
    public int AdvanceVotingMaxDays { get; init; }

    [StringLength(100)]
    public string? AdvanceVotingArticle { get; init; }

    public MinorVoting MinorVoting { get; init; }

    [Range(12, 21)]
    public int AdultAge { get; init; } = 18;
}
