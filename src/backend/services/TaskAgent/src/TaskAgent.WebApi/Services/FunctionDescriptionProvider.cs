using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TaskAgent.WebApi.Services;

/// <summary>
/// Provides dynamic status messages derived from function [Description] attributes.
/// Caches descriptions for performance and supports multi-agent scenarios.
/// </summary>
public partial class FunctionDescriptionProvider
{
    private readonly ConcurrentDictionary<string, string> _descriptionCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _statusMessageCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Type> _registeredTypes = [];

    /// <summary>
    /// Registers a type containing function methods with [Description] attributes.
    /// Call this during startup for each agent's function class.
    /// </summary>
    public void RegisterFunctionType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (_registeredTypes.Contains(type))
        {
            return;
        }

        _registeredTypes.Add(type);

        // Pre-cache all descriptions from the type
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
        {
            DescriptionAttribute? descAttr = method.GetCustomAttribute<DescriptionAttribute>();
            if (descAttr != null)
            {
                string methodName = NormalizeFunctionName(method.Name);
                _descriptionCache.TryAdd(methodName, descAttr.Description);
                _statusMessageCache.TryAdd(methodName, GenerateStatusMessage(descAttr.Description, method.Name));
            }
        }
    }

    /// <summary>
    /// Gets the full description from the [Description] attribute of a function.
    /// </summary>
    public string? GetDescription(string functionName)
    {
        string normalizedName = NormalizeFunctionName(functionName);
        return _descriptionCache.TryGetValue(normalizedName, out string? description) ? description : null;
    }

    /// <summary>
    /// Gets a user-friendly status message for a function, derived from its [Description] attribute.
    /// Returns a generic message if the function is not registered.
    /// </summary>
    public string GetStatusMessage(string? functionName)
    {
        if (string.IsNullOrWhiteSpace(functionName))
        {
            return "Processing...";
        }

        string normalizedName = NormalizeFunctionName(functionName);
        
        if (_statusMessageCache.TryGetValue(normalizedName, out string? cachedMessage))
        {
            return cachedMessage;
        }

        // Fallback: generate from function name
        return GenerateStatusMessageFromName(functionName);
    }

    /// <summary>
    /// Generates a concise status message from a full description.
    /// Transforms "Create a new task with title, description, and priority level." 
    /// into "Creating task..."
    /// </summary>
    private static string GenerateStatusMessage(string description, string methodName)
    {
        // Strategy 1: Extract the main verb and object from description
        string? extracted = ExtractActionFromDescription(description);
        if (!string.IsNullOrEmpty(extracted))
        {
            return extracted;
        }

        // Strategy 2: Fallback to method name transformation
        return GenerateStatusMessageFromName(methodName);
    }

    /// <summary>
    /// Extracts the main action from a description like "Create a new task" → "Creating task..."
    /// </summary>
    private static string? ExtractActionFromDescription(string description)
    {
        // Common patterns: "Verb a/an/the noun..." or "Verbs noun..."
        Match match = ActionPatternRegex().Match(description);

        if (match.Success)
        {
            string verb = match.Groups["verb"].Value;
            string noun = match.Groups["noun"].Value;

            // Convert verb to gerund (-ing form)
            string gerund = ConvertToGerund(verb);

            // Clean and singularize the noun
            string cleanNoun = CleanNoun(noun);

            return $"{gerund} {cleanNoun}...";
        }

        return null;
    }

    /// <summary>
    /// Converts a verb to its -ing form (gerund).
    /// </summary>
    private static string ConvertToGerund(string verb)
    {
        string lowerVerb = verb.ToUpperInvariant();

        // Handle special cases (compare in upper case)
        return lowerVerb switch
        {
            "GET" => "Getting",
            "DELETE" => "Deleting",
            "CREATE" => "Creating",
            "UPDATE" => "Updating",
            "LIST" => "Listing",
            "SEARCH" => "Searching",
            "GENERATE" => "Generating",
            "RETRIEVE" => "Retrieving",
            "FETCH" => "Fetching",
            "FIND" => "Finding",
            "LOAD" => "Loading",
            "PROCESS" => "Processing",
            _ => ConvertToGerundGeneric(verb)
        };
    }

    private static string ConvertToGerundGeneric(string verb)
    {
        // Generic rules for gerund conversion
        if (verb.EndsWith('e'))
        {
            return char.ToUpper(verb[0], CultureInfo.InvariantCulture) + verb[1..^1] + "ing";
        }

        // Double consonant for short verbs ending in consonant
        if (verb.Length <= 4 && IsConsonant(verb[^1]) && !IsConsonant(verb[^2]))
        {
            return char.ToUpper(verb[0], CultureInfo.InvariantCulture) + verb[1..] + verb[^1] + "ing";
        }

        return char.ToUpper(verb[0], CultureInfo.InvariantCulture) + verb[1..] + "ing";
    }

    private static bool IsConsonant(char c)
    {
        return char.IsLetter(c) && !"aeiouAEIOU".Contains(c);
    }

    /// <summary>
    /// Cleans a noun phrase for status display.
    /// </summary>
    private static string CleanNoun(string noun)
    {
        // Remove articles and simplify
        string cleaned = noun
            .Replace("a new ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("an ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("the ", "", StringComparison.OrdinalIgnoreCase)
            .Replace("all ", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        // Take first word or two if it's a simple phrase
        string[] words = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 2)
        {
            cleaned = string.Join(" ", words.Take(2));
        }

#pragma warning disable CA1308 // Normalize strings to uppercase - intentional lowercase for display
        return cleaned.ToLower(CultureInfo.InvariantCulture);
#pragma warning restore CA1308
    }

    /// <summary>
    /// Generates a status message from a method name like "CreateTaskAsync" → "Creating task..."
    /// </summary>
    private static string GenerateStatusMessageFromName(string methodName)
    {
        // Remove common suffixes
        string cleanName = methodName
            .Replace("Async", "", StringComparison.OrdinalIgnoreCase)
            .Replace("Task", " task", StringComparison.OrdinalIgnoreCase);

        // Split PascalCase and convert first word to gerund
        string[] parts = SplitPascalCase(cleanName);
        if (parts.Length == 0)
        {
            return "Processing...";
        }

        string verb = parts[0];
        string gerund = ConvertToGerund(verb);
#pragma warning disable CA1308 // Normalize strings to uppercase - intentional lowercase for display
        string rest = parts.Length > 1 ? " " + string.Join(" ", parts.Skip(1)).ToLower(CultureInfo.InvariantCulture) : "";
#pragma warning restore CA1308

        return $"{gerund}{rest}...";
    }

    private static string[] SplitPascalCase(string input)
    {
        return PascalCaseSplitRegex()
            .Split(input)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
    }

    /// <summary>
    /// Normalizes function names by removing common suffixes for consistent lookup.
    /// </summary>
    private static string NormalizeFunctionName(string functionName)
    {
        return functionName
            .Replace("Async", "", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    [GeneratedRegex(@"^(?<verb>\w+)\s+(?:a\s+(?:new\s+)?|an\s+|the\s+|all\s+)?(?<noun>[\w\s]+?)(?:\s+(?:with|from|by|for|using|based|to)\b.*)?[,.]?$", RegexOptions.IgnoreCase)]
    private static partial Regex ActionPatternRegex();

    [GeneratedRegex(@"(?<!^)(?=[A-Z])")]
    private static partial Regex PascalCaseSplitRegex();
}
