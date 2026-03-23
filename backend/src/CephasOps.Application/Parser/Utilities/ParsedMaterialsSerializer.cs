using System.Text.Json;
using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser.Utilities;

public static class ParsedMaterialsSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string Serialize(IEnumerable<ParsedDraftMaterialDto> materials)
    {
        return JsonSerializer.Serialize(materials, SerializerOptions);
    }

    public static List<ParsedDraftMaterialDto> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<ParsedDraftMaterialDto>();
        }

        try
        {
            var materials = JsonSerializer.Deserialize<List<ParsedDraftMaterialDto>>(json, SerializerOptions);
            return materials ?? new List<ParsedDraftMaterialDto>();
        }
        catch
        {
            return new List<ParsedDraftMaterialDto>();
        }
    }
}


