namespace Diguifi.Api.Configuration;

public static class DotEnvConfigurationLoader
{
    public static IReadOnlyDictionary<string, string?> Load(string contentRootPath)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var envFilePath = FindEnvFile(contentRootPath);
        if (envFilePath is null)
        {
            return values;
        }

        foreach (var rawLine in File.ReadAllLines(envFilePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
            {
                line = line[7..].TrimStart();
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = NormalizeKey(line[..separatorIndex].Trim());
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = line[(separatorIndex + 1)..].Trim();
            values[key] = Unquote(value);
        }

        return values;
    }

    private static string? FindEnvFile(string contentRootPath)
    {
        var directory = new DirectoryInfo(contentRootPath);

        while (directory is not null)
        {
            var envFilePath = Path.Combine(directory.FullName, ".env");
            if (File.Exists(envFilePath))
            {
                return envFilePath;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string NormalizeKey(string key)
    {
        return key.Replace("__", ":", StringComparison.Ordinal);
    }

    private static string? Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value.StartsWith('"') && value.EndsWith('"')) ||
             (value.StartsWith('\'') && value.EndsWith('\''))))
        {
            return value[1..^1];
        }

        return value;
    }
}
