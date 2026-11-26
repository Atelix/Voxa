using System.Globalization;
using Voxa.Abstractions;
using Voxa.Abstractions.Services;

namespace Voxa.Services;

/// <summary>
/// Reads a member list from CSV, as exported from Keeva or a spreadsheet. The first row names the columns; Dutch and
/// English names are recognised (see docs/voxa.md). Separators may be comma, semicolon or tab, and values may be quoted.
/// </summary>
public static class MemberCsv
{
    private static readonly string[] s_name = ["naam", "name", "volledige naam", "full name"];
    private static readonly string[] s_firstName = ["voornaam", "first name", "firstname"];
    private static readonly string[] s_infix = ["tussenvoegsel", "infix"];
    private static readonly string[] s_lastName = ["achternaam", "last name", "lastname", "surname"];
    private static readonly string[] s_email = ["email", "e-mail", "e-mailadres", "emailadres", "email address"];
    private static readonly string[] s_birthDate = ["geboortedatum", "birth date", "birthdate", "date of birth"];
    private static readonly string[] s_guardianName = ["ouder", "ouder naam", "naam ouder", "verzorger", "guardian", "parent", "parent name"];
    private static readonly string[] s_guardianEmail = ["ouder email", "ouder e-mail", "e-mail ouder", "email ouder", "guardian email", "parent email"];
    private static readonly string[] s_status = ["status", "geschorst", "suspended"];
    private static readonly string[] s_suspended = ["geschorst", "suspended", "ja", "yes", "true", "1"];
    private static readonly string[] s_dateFormats = ["yyyy-MM-dd", "d-M-yyyy", "dd-MM-yyyy", "d/M/yyyy", "dd/MM/yyyy", "d.M.yyyy"];

    public static IReadOnlyList<ImportedMember> Parse(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        var lines = csv.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Where(static l => !string.IsNullOrWhiteSpace(l)).ToList();
        if (lines.Count < 2)
        {
            throw new DomainValidationException("The CSV needs a header row and at least one member.");
        }

        var separator = new[] { ';', '\t', ',' }.OrderByDescending(s => lines[0].Count(c => c == s)).First();
        var header = Split(lines[0], separator).Select(static h => h.Trim()).ToList();
        int Column(string[] names) => header.FindIndex(h => names.Contains(h, StringComparer.OrdinalIgnoreCase));

        var name = Column(s_name);
        var first = Column(s_firstName);
        var infix = Column(s_infix);
        var last = Column(s_lastName);
        if (name < 0 && (first < 0 || last < 0))
        {
            throw new DomainValidationException("The CSV needs a column 'naam', or 'voornaam' and 'achternaam'.");
        }

        var email = Column(s_email);
        var birth = Column(s_birthDate);
        var guardianName = Column(s_guardianName);
        var guardianEmail = Column(s_guardianEmail);
        var status = Column(s_status);

        var members = new List<ImportedMember>();
        foreach (var line in lines.Skip(1))
        {
            var cells = Split(line, separator);
            string? Cell(int index) => index >= 0 && index < cells.Count && !string.IsNullOrWhiteSpace(cells[index]) ? cells[index].Trim() : null;

            var fullName = Cell(name) ?? string.Join(' ', new[] { Cell(first), Cell(infix), Cell(last) }.Where(static p => p is not null));
            if (string.IsNullOrWhiteSpace(fullName))
            {
                continue;
            }

            var statusValue = Cell(status);
            members.Add(new ImportedMember
            {
                Name = fullName,
                Email = Cell(email),
                BirthDate = ParseDate(Cell(birth)),
                GuardianName = Cell(guardianName),
                GuardianEmail = Cell(guardianEmail),
                Suspended = statusValue is not null && s_suspended.Contains(statusValue, StringComparer.OrdinalIgnoreCase),
            });
        }

        return members;
    }

    private static DateOnly? ParseDate(string? value) =>
        value is not null && DateOnly.TryParseExact(value, s_dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    /// <summary>Splits one CSV line, honouring double quotes ("" inside quotes is a quote).</summary>
    internal static List<string> Split(string line, char separator)
    {
        var cells = new List<string>();
        var cell = new System.Text.StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    _ = cell.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    _ = cell.Append(c);
                }
            }
            else if (c == '"')
            {
                quoted = true;
            }
            else if (c == separator)
            {
                cells.Add(cell.ToString());
                _ = cell.Clear();
            }
            else
            {
                _ = cell.Append(c);
            }
        }

        cells.Add(cell.ToString());
        return cells;
    }
}
