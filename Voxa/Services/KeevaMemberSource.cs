using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using Voxa.Abstractions;
using Voxa.Abstractions.Services;

namespace Voxa.Services;

/// <summary>
/// Reads the current members from Keeva's public API (<c>GET /api/v1/members</c>, docs/public-api.md in Keeva). The key
/// needs the scopes Members, MembersContacts (e-mail addresses and parents) and MembersPersonalData (birth dates, to
/// tell minors apart). Keeva has no suspension, so suspended members are removed from the list by hand.
/// </summary>
public sealed class KeevaMemberSource(HttpClient http, IOptions<KeevaOptions> options, ClubSettingsStore club) : IMemberSource
{
    private const int PageSize = 100;

    public async Task<IReadOnlyList<ImportedMember>> FetchAsync(CancellationToken cancellationToken)
    {
        var settings = await club.GetAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(settings.KeevaUrl) || !Uri.TryCreate(settings.KeevaUrl, UriKind.Absolute, out var baseUrl))
        {
            throw new DomainValidationException("Set the club's Keeva address in the settings first.");
        }

        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new DomainValidationException("The server has no Keeva API key (Keeva__ApiKey).");
        }

        var members = new List<ImportedMember>();
        int? offset = 0;
        while (offset is { } current)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUrl, $"/api/v1/members?isFormerMember=false&limit={PageSize}&offset={current}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new DomainValidationException($"Keeva refused the member list ({(int)response.StatusCode}). Check the key and its scopes.");
            }

            var page = await response.Content.ReadFromJsonAsync(KeevaJsonContext.Default.KeevaPage, cancellationToken)
                       ?? throw new DomainValidationException("Keeva sent an empty member list.");
            members.AddRange(page.Items.Select(ToMember));
            offset = page.NextOffset;
        }

        return members;
    }

    private static ImportedMember ToMember(KeevaMember member)
    {
        var name = string.Join(' ', new[] { member.Firstname, member.Infix, member.Lastname }.Where(static p => !string.IsNullOrWhiteSpace(p)));
        var parents = (member.Accounts ?? []).Where(static a => a.IsParent).OrderByDescending(static a => a.IsPrimaryContact).ToList();
        return new ImportedMember
        {
            Name = name,
            Email = member.Email,
            BirthDate = member.BirthDate,
            GuardianEmail = parents.FirstOrDefault()?.Email,
        };
    }
}

internal sealed record KeevaPage(
    [property: JsonPropertyName("items")] IReadOnlyList<KeevaMember> Items,
    [property: JsonPropertyName("nextOffset")] int? NextOffset);

internal sealed record KeevaMember(
    [property: JsonPropertyName("firstname")] string Firstname,
    [property: JsonPropertyName("infix")] string? Infix,
    [property: JsonPropertyName("lastname")] string Lastname,
    [property: JsonPropertyName("email")] string? Email,
    [property: JsonPropertyName("birthDate")] DateOnly? BirthDate,
    [property: JsonPropertyName("accounts")] IReadOnlyList<KeevaAccount>? Accounts);

internal sealed record KeevaAccount(
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("isParent")] bool IsParent,
    [property: JsonPropertyName("isPrimaryContact")] bool IsPrimaryContact);

[JsonSerializable(typeof(KeevaPage))]
internal sealed partial class KeevaJsonContext : JsonSerializerContext;
