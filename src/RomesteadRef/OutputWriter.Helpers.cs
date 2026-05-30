using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RomesteadRef;

internal static partial class OutputWriter
{
    private static string MetricCard(string label, string value) =>
        $"<article class=\"metric\"><span>{Encode(label)}</span><strong>{Encode(value)}</strong></article>";

    private static string FormatHashChange(string? oldHash, string? newHash)
    {
        var oldShort = string.IsNullOrWhiteSpace(oldHash) ? "none" : oldHash[..Math.Min(8, oldHash.Length)];
        var newShort = string.IsNullOrWhiteSpace(newHash) ? "none" : newHash[..Math.Min(8, newHash.Length)];
        return $"{oldShort} -> {newShort}";
    }

    private static string BuildAnchor(string id)
    {
        var builder = new StringBuilder(id.Length);
        foreach (var character in id)
        {
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        }

        return builder.ToString();
    }

    private static string GetAssemblyFileName(string assemblyName) =>
        $"{GetDirectorySafeName(assemblyName)}.html";

    private static string GetTypeFileName(TypeCatalog type) =>
        $"{GetDirectorySafeName(type.Id)}.html";

    private static string GetTypeJsonFileName(TypeCatalog type) =>
        $"{GetDirectorySafeName(type.Id)}.json";

    private static string GetDirectorySafeName(string name)
    {
        var builder = new StringBuilder(name.Length);
        foreach (var character in name)
        {
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        }

        return builder.ToString();
    }

    private static string Encode(string value) => WebUtility.HtmlEncode(value);

    private static void DeleteDirectoryIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
