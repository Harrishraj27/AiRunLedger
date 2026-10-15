namespace AiRunLedger.Api.Domain;

public enum RunStatus
{
    Succeeded,
    Failed
}

/// <summary>Where the numbers came from. Sample data is invented; Manual is typed in by a person.</summary>
public enum DataSource
{
    Sample,
    Manual
}
