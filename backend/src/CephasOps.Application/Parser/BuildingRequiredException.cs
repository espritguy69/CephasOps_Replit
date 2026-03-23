using CephasOps.Application.Parser.DTOs;

namespace CephasOps.Application.Parser;

/// <summary>
/// Thrown when draft approval fails because a building is required (not found or not selected).
/// Controller returns HTTP 400 with errorCode BUILDING_NOT_FOUND and buildingDetection for the client.
/// </summary>
public class BuildingRequiredException : InvalidOperationException
{
    public const string ErrorCodeValue = "BUILDING_NOT_FOUND";

    public string ErrorCode => ErrorCodeValue;

    public BuildingDetectionResult? BuildingDetection { get; }

    public BuildingRequiredException(string message, BuildingDetectionResult? buildingDetection = null)
        : base(message)
    {
        BuildingDetection = buildingDetection;
    }
}
