using System.Text.Json;
using POS.Models;

namespace POS.Authorization;

public static class SectionPermissionService
{
    public static IReadOnlyList<string> ParseAllowedSections(User? user)
    {
        if (user == null || string.IsNullOrWhiteSpace(user.AllowedSectionsJson))
            return Array.Empty<string>();

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(user.AllowedSectionsJson);
            return SectionDefinitions.NormalizeAssignable(list);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    public static string? SerializeAllowedSections(IEnumerable<string>? keys)
    {
        var normalized = SectionDefinitions.NormalizeAssignable(keys);
        if (normalized.Count == 0) return null;
        return JsonSerializer.Serialize(normalized);
    }

    public static bool UserCanAccessSection(User user, string sectionKey, string? traditionalRolesCsv)
    {
        if (user == null || string.IsNullOrWhiteSpace(sectionKey))
            return false;

        var section = sectionKey.Trim();
        if (!SectionDefinitions.IsAssignable(section))
            return false;

        if (!string.IsNullOrWhiteSpace(traditionalRolesCsv))
        {
            var allowedRoles = traditionalRolesCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (allowedRoles.Any(r => string.Equals(r, user.Role, StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        if (string.Equals(user.Role, SectionDefinitions.ManagerRole, StringComparison.OrdinalIgnoreCase))
        {
            var sections = ParseAllowedSections(user);
            return sections.Any(s => string.Equals(s, section, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    public static bool UserCanAccessAnySection(
        User user,
        IEnumerable<string> sectionKeys,
        string? traditionalRolesCsv)
    {
        if (sectionKeys == null) return false;
        foreach (var key in sectionKeys)
        {
            if (UserCanAccessSection(user, key, traditionalRolesCsv))
                return true;
        }
        return false;
    }

    public static (bool Ok, string? ErrorMessage, string? Json) ResolveManagerSectionsForSave(
        string? role,
        string? allowedSectionsJson)
    {
        if (!string.Equals(role, SectionDefinitions.ManagerRole, StringComparison.OrdinalIgnoreCase))
            return (true, null, null);

        List<string>? raw = null;
        if (!string.IsNullOrWhiteSpace(allowedSectionsJson))
        {
            var trimmed = allowedSectionsJson.Trim();
            try
            {
                if (trimmed.StartsWith("["))
                    raw = JsonSerializer.Deserialize<List<string>>(trimmed);
                else
                    raw = trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();
            }
            catch
            {
                return (false, "invalidSectionsFormat", null);
            }
        }

        var normalized = SectionDefinitions.NormalizeAssignable(raw);
        if (normalized.Count == 0)
            return (false, "selectAtLeastOneSection", null);

        return (true, null, SerializeAllowedSections(normalized));
    }
}
