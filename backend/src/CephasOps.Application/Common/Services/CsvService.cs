using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace CephasOps.Application.Common.Services;

/// <summary>
/// Generic CSV import/export service
/// </summary>
public interface ICsvService
{
    /// <summary>
    /// Export records to CSV string
    /// </summary>
    string ExportToCsv<T>(IEnumerable<T> records);
    
    /// <summary>
    /// Export records to CSV bytes for file download
    /// </summary>
    byte[] ExportToCsvBytes<T>(IEnumerable<T> records);
    
    /// <summary>
    /// Import records from CSV string
    /// </summary>
    List<T> ImportFromCsv<T>(string csvContent);
    
    /// <summary>
    /// Import records from CSV stream
    /// </summary>
    List<T> ImportFromCsv<T>(Stream csvStream);
    
    /// <summary>
    /// Generate an empty template CSV with headers
    /// </summary>
    string GenerateTemplate<T>();
    
    /// <summary>
    /// Generate template CSV bytes for file download
    /// </summary>
    byte[] GenerateTemplateBytes<T>();
}

public class CsvService : ICsvService
{
    public string ExportToCsv<T>(IEnumerable<T> records)
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });
        
        csv.WriteRecords(records);
        return writer.ToString();
    }

    public byte[] ExportToCsvBytes<T>(IEnumerable<T> records)
    {
        var csvString = ExportToCsv(records);
        return Encoding.UTF8.GetBytes(csvString);
    }

    public List<T> ImportFromCsv<T>(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        return ImportFromReader<T>(reader);
    }

    public List<T> ImportFromCsv<T>(Stream csvStream)
    {
        using var reader = new StreamReader(csvStream);
        return ImportFromReader<T>(reader);
    }

    private List<T> ImportFromReader<T>(TextReader reader)
    {
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            HeaderValidated = null,
            MissingFieldFound = null,
            BadDataFound = null
        });
        
        return csv.GetRecords<T>().ToList();
    }

    public string GenerateTemplate<T>()
    {
        using var writer = new StringWriter();
        using var csv = new CsvWriter(writer, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });
        
        // Write header only
        csv.WriteHeader<T>();
        csv.NextRecord();
        
        // Write one example row with default values
        var example = Activator.CreateInstance<T>();
        csv.WriteRecord(example);
        csv.NextRecord();
        
        return writer.ToString();
    }

    public byte[] GenerateTemplateBytes<T>()
    {
        var csvString = GenerateTemplate<T>();
        return Encoding.UTF8.GetBytes(csvString);
    }
}

