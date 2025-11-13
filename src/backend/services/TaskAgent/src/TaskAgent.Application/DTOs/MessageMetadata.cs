namespace TaskAgent.Application.DTOs;

/// <summary>
/// Metadata associated with a chat message
/// </summary>
public record MessageMetadata
{
    /// <summary>
    /// Function calls made during message processing
    /// </summary>
    public IReadOnlyList<FunctionCallInfo>? FunctionCalls { get; init; }

    /// <summary>
    /// Additional context or processing information
    /// </summary>
    public Dictionary<string, object>? AdditionalData { get; init; }
}

/// <summary>
/// Information about a function call made by the AI agent
/// </summary>
public record FunctionCallInfo
{
    /// <summary>
    /// Name of the function called
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Arguments passed to the function (JSON string)
    /// </summary>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>
    /// Result returned by the function
    /// </summary>
    public string? Result { get; init; }

    /// <summary>
    /// Timestamp when the function was called
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
