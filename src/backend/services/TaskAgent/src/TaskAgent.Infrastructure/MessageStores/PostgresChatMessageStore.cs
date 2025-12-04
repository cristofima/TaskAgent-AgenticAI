using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using TaskAgent.Domain.Entities;
using TaskAgent.Infrastructure.Data;

namespace TaskAgent.Infrastructure.MessageStores;

/// <summary>
/// PostgreSQL-backed <c>ChatMessageStore</c> implementation for persistent message storage.
/// </summary>
/// <remarks>
/// Follows Microsoft Agent Framework best practices:
/// <see href="https://learn.microsoft.com/en-us/agent-framework/tutorials/agents/third-party-chat-history-storage">Third-party chat history storage</see>.
/// Stores messages individually with thread serialization via <c>ThreadDbKey</c>.
/// </remarks>
public class PostgresChatMessageStore : ChatMessageStore
{
    private readonly ConversationDbContext _context;

    /// <summary>
    /// Unique thread identifier (GUID format) persisted in database.
    /// </summary>
    public string? ThreadDbKey { get; private set; }

    /// <summary>
    /// Creates a new message store for a new thread.
    /// </summary>
    public PostgresChatMessageStore(ConversationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        ThreadDbKey = null; // Will be generated on first AddMessagesAsync call
    }

    /// <summary>
    /// Creates a message store from deserialized thread state.
    /// </summary>
    public PostgresChatMessageStore(
        ConversationDbContext context,
        JsonElement serializedState,
        JsonSerializerOptions? jsonSerializerOptions = null
    )
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));

        if (serializedState.ValueKind == JsonValueKind.String)
        {
            ThreadDbKey = serializedState.Deserialize<string>(jsonSerializerOptions);
        }
    }

    /// <summary>
    /// Adds messages to PostgreSQL database.
    /// </summary>
    public override async Task AddMessagesAsync(
        IEnumerable<ChatMessage> messages,
        CancellationToken cancellationToken = default
    )
    {
        // Generate ThreadDbKey on first message
        ThreadDbKey ??= Guid.NewGuid().ToString("N");

        var messageList = messages.ToList();

        foreach (ChatMessage message in messageList)
        {
            string messageId = message.MessageId ?? Guid.NewGuid().ToString("N");

            // Serialize message contents to JSON
            string contentJson = JsonSerializer.Serialize(
                message.Contents,
                new JsonSerializerOptions { WriteIndented = false }
            );

            var entity = ConversationMessage.Create(
                messageId: messageId,
                threadId: ThreadDbKey,
                role: message.Role.ToString(),
                content: contentJson
            );

            await _context.ConversationMessages.AddAsync(entity, cancellationToken);
        }

        // Save messages first
        await _context.SaveChangesAsync(cancellationToken);

        // Update or create thread metadata
        await UpdateThreadMetadataAsync(cancellationToken);
    }

    /// <summary>
    /// Updates or creates thread metadata with title, preview, and message count.
    /// </summary>
    private async Task UpdateThreadMetadataAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(ThreadDbKey))
        {
            return;
        }

        // Get existing metadata or create new
        ConversationThreadMetadata? metadata = await _context.ConversationThreads
            .FirstOrDefaultAsync(t => t.ThreadId == ThreadDbKey, cancellationToken);

        // Get all messages for this thread (for metadata extraction)
        List<ConversationMessage> allMessages = await _context.ConversationMessages
            .Where(m => m.ThreadId == ThreadDbKey)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken);

        if (allMessages.Count == 0)
        {
            return;
        }

        // Extract title from first user message
        ConversationMessage? firstUserMessage = allMessages.FirstOrDefault(m => m.Role == "user");
        string? title = firstUserMessage != null
            ? ExtractTitle(firstUserMessage.Content)
            : "New chat";

        // Extract preview from last assistant message
        ConversationMessage? lastAssistantMessage = allMessages
            .LastOrDefault(m => m.Role == "assistant");
        string? preview = lastAssistantMessage != null
            ? ExtractPreview(lastAssistantMessage.Content)
            : null;

        int messageCount = allMessages.Count;
        
        // Serialize ThreadDbKey as serializedState (AG-UI protocol format)
        string serializedState = JsonSerializer.Serialize(ThreadDbKey);

        if (metadata == null)
        {
            // Create new metadata
            metadata = ConversationThreadMetadata.Create(ThreadDbKey, serializedState);
            metadata.UpdateMetadata(title, preview, messageCount, serializedState);
            await _context.ConversationThreads.AddAsync(metadata, cancellationToken);
        }
        else
        {
            // Update existing metadata (including serializedState)
            metadata.UpdateMetadata(title, preview, messageCount, serializedState);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Extracts conversation title from first user message content (max 50 chars).
    /// </summary>
    private static string ExtractTitle(string contentJson)
    {
        try
        {
            var content = JsonDocument.Parse(contentJson);
            if (content.RootElement.ValueKind == JsonValueKind.Array)
            {
                // Find first text content element (skip function calls)
                foreach (JsonElement element in content.RootElement.EnumerateArray())
                {
                    // Check $type to ensure it's a text message, not a function call
                    if (element.TryGetProperty("$type", out JsonElement typeProperty))
                    {
                        string? typeValue = typeProperty.GetString();
                        if (typeValue != "text")
                        {
                            continue; // Skip function calls, function results, etc.
                        }
                    }

                    // Try both "Text" (Pascal case from serialization) and "text" (camel case)
                    if (!element.TryGetProperty("Text", out JsonElement textProp) &&
                        !element.TryGetProperty("text", out textProp))
                    {
                        continue;
                    }

                    string text = textProp.GetString() ?? "New chat";
                    return text.Length > 50 ? text[..50] + "..." : text;
                }
            }
        }
        catch
        {
            // Fallback if JSON parsing fails
        }

        return "New chat";
    }

    /// <summary>
    /// Extracts preview from last assistant message content (max 100 chars).
    /// </summary>
    private static string ExtractPreview(string contentJson)
    {
        try
        {
            var content = JsonDocument.Parse(contentJson);
            if (content.RootElement.ValueKind == JsonValueKind.Array)
            {
                // Find first text content element (skip function calls)
                foreach (JsonElement element in content.RootElement.EnumerateArray())
                {
                    // Check $type to ensure it's a text message, not a function call
                    if (element.TryGetProperty("$type", out JsonElement typeProperty))
                    {
                        string? typeValue = typeProperty.GetString();
                        if (typeValue != "text")
                        {
                            continue; // Skip function calls, function results, etc.
                        }
                    }

                    // Try both "Text" (Pascal case from serialization) and "text" (camel case)
                    if (!element.TryGetProperty("Text", out JsonElement textProp) &&
                        !element.TryGetProperty("text", out textProp))
                    {
                        continue;
                    }

                    string text = textProp.GetString() ?? "";
                    return text.Length > 100 ? text[..100] + "..." : text;
                }
            }
        }
        catch
        {
            // Fallback
        }

        return "";
    }

    /// <summary>
    /// Retrieves messages from PostgreSQL in chronological order.
    /// </summary>
    public override async Task<IEnumerable<ChatMessage>> GetMessagesAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrEmpty(ThreadDbKey))
        {
            return [];
        }

        List<ConversationMessage> entities = await _context
            .ConversationMessages.AsNoTracking()
            .Where(m => m.ThreadId == ThreadDbKey)
            .OrderBy(m => m.Timestamp)
            .ToListAsync(cancellationToken);

        List<ChatMessage> messages = new(entities.Count);

        foreach (ConversationMessage entity in entities)
        {
            // Deserialize message contents from JSON
            List<AIContent>? contents = JsonSerializer.Deserialize<List<AIContent>>(
                entity.Content
            );

            if (contents == null)
            {
                continue;
            }

            // ChatRole is a struct, not an enum. Use the ChatRole constructor to parse from string.
            var role = new ChatRole(entity.Role);

            messages.Add(new ChatMessage(role, contents) { MessageId = entity.MessageId });
        }

        return messages;
    }

    /// <summary>
    /// Serializes thread state (only <c>ThreadDbKey</c>) for AG-UI protocol.
    /// </summary>
    /// <remarks>
    /// Only the <c>ThreadDbKey</c> is serialized; messages are retrieved from database.
    /// Frontend sends this as <c>serializedState</c> in subsequent requests.
    /// </remarks>
    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return JsonSerializer.SerializeToElement(ThreadDbKey, jsonSerializerOptions);
    }
}
