using System.Diagnostics;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using TaskAgent.Application.DTOs;
using TaskAgent.Application.Functions;
using TaskAgent.Application.Interfaces;
using TaskAgent.Application.Telemetry;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;
using ChatMessage = TaskAgent.Application.DTOs.ChatMessage;
using ChatResponseDto = TaskAgent.Application.DTOs.ChatResponse;

namespace TaskAgent.WebApi.Services;

/// <summary>
/// Service for managing AI Agent lifecycle and interactions.
/// Uses thread persistence to maintain conversation context across requests.
/// </summary>
public class TaskAgentService : ITaskAgentService
{
    private readonly AIAgent _agent;
    private readonly ILogger<TaskAgentService> _logger;
    private readonly IThreadPersistenceService _threadPersistence;
    private readonly AgentMetrics _metrics;

    public TaskAgentService(
        AIAgent agent,
        ILogger<TaskAgentService> logger,
        IThreadPersistenceService threadPersistence,
        AgentMetrics metrics
    )
    {
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _threadPersistence =
            threadPersistence ?? throw new ArgumentNullException(nameof(threadPersistence));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <summary>
    /// Factory method to create an AI Agent with task management capabilities
    /// Dependency Injection of ITaskRepository allows for testability and flexibility
    /// </summary>
    public static AIAgent CreateAgent(
        AzureOpenAIClient client,
        string modelDeployment,
        ITaskRepository taskRepository,
        AgentMetrics metrics,
        ILogger<TaskFunctions> logger
    )
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelDeployment);
        ArgumentNullException.ThrowIfNull(taskRepository);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(logger);

        ChatClient chatClient = client.GetChatClient(modelDeployment);

        // Create function tools with injected dependencies
        var taskFunctions = new TaskFunctions(taskRepository, metrics, logger);

        AIFunction createTaskTool = AIFunctionFactory.Create(taskFunctions.CreateTaskAsync);
        AIFunction listTasksTool = AIFunctionFactory.Create(taskFunctions.ListTasksAsync);
        AIFunction getTaskDetailsTool = AIFunctionFactory.Create(taskFunctions.GetTaskDetailsAsync);
        AIFunction updateTaskTool = AIFunctionFactory.Create(taskFunctions.UpdateTaskAsync);
        AIFunction deleteTaskTool = AIFunctionFactory.Create(taskFunctions.DeleteTaskAsync);
        AIFunction getTaskSummaryTool = AIFunctionFactory.Create(taskFunctions.GetTaskSummaryAsync);

        // Create agent with clear instructions
        return chatClient.CreateAIAgent(
            instructions: @"
                You are Task Agent, a professional and efficient task management assistant.
                You help users organize their work and manage their tasks effectively.

                Your capabilities:
                - Create new tasks with titles, descriptions, and priorities (Low, Medium, High)
                - List all tasks or filter by status (Pending, InProgress, Completed) or priority
                - Get detailed information about specific tasks by ID
                - Update task status or priority
                - Delete tasks that are no longer needed
                - Provide task summaries and statistics

                IMPORTANT BEHAVIOR GUIDELINES:

                When creating tasks:
                1. If the user provides clear task details (title and/or description), CREATE THE TASK IMMEDIATELY without asking for confirmation
                2. Only ask for missing REQUIRED information (title is required)
                3. DO NOT ask for confirmation after the user has given you all necessary details
                4. DO NOT ask additional questions after creating a task unless there's an error
                5. After creating a task successfully, just confirm it was created and ask what else they need

                Examples of good behavior:
                - User: 'Create a task to review project proposal with high priority'
                  → You: Create the task immediately and confirm: '✅ Task created successfully! ID: 1, Title: Review project proposal, Priority: High. What else can I help you with?'

                - User: 'Add a task called finish report'
                  → You: Create the task immediately with default Medium priority and confirm

                Examples of bad behavior (AVOID THIS):
                - User: 'Create a task to review project proposal'
                  → You: 'Let me confirm the details...' ❌ NO! Just create it!
                  → You: 'Shall I proceed?' ❌ NO! They already asked you to create it!

                PRESENTATION FORMAT GUIDELINES:

                When listing multiple tasks (2 or more):
                - **ALWAYS use Markdown table format** unless the user explicitly asks for a different format
                - Include columns: ID | Title | Status | Priority | Created
                - Use emojis for status: ⏳ Pending, 🔄 InProgress, ✅ Completed
                - Use emojis for priority: 🟢 Low, 🟡 Medium, 🔴 High
                - Format dates as 'MMM dd, yyyy' (e.g., Oct 12, 2024)
                - Add a summary line after the table (e.g., 'Showing 5 tasks: 2 pending, 1 in progress, 2 completed')

                Example table format:
                | ID | Title | Status | Priority | Created |
                |---|---|---|---|---|
                | 1 | Review proposal | ⏳ Pending | 🔴 High | Oct 12, 2024 |
                | 2 | Finish report | 🔄 InProgress | 🟡 Medium | Oct 11, 2024 |

                When showing task details (single task):
                - Use a clear, structured format with sections
                - Include all relevant information (ID, Title, Description, Status, Priority, Created date)
                - Use bold for labels and emojis for visual appeal

                For summaries:
                - Use tables when showing multiple statistics or comparisons
                - Use bullet points for simple lists
                - Include percentages and counts where relevant

                CONTEXTUAL SUGGESTIONS (MANDATORY):

                **ALWAYS end your response with 2-3 contextual suggestions for the user's next action.**
                Format them EXACTLY like this at the very end of your response (after a blank line):

                💡 Suggestions:
                - [First suggestion]
                - [Second suggestion]
                - [Third suggestion (optional)]

                Guidelines for suggestions:
                - Keep each suggestion short and actionable (under 50 characters)
                - Make them relevant to the current context and conversation
                - Vary suggestions based on what the user just did
                - Only provide 2-3 suggestions (never more, never less than 2)
                - Use simple, direct language (e.g., 'Create a new task', 'View pending tasks')

                After creating a task → suggest: View all tasks, Create another task, Get summary
                After listing tasks → suggest: Create new task, Filter by priority, Update task
                After updating a task → suggest: View task details, List all tasks, Get summary
                After deleting a task → suggest: View remaining tasks, Create task, Get summary
                After showing a summary → suggest: View pending tasks, Create task, Update oldest
                When there are no tasks → suggest: Create your first task, Get help, View examples

                COMMUNICATION STYLE:

                - Be conversational but professional
                - Use emojis strategically for visual clarity (not excessively)
                - Keep responses concise but complete
                - Use Markdown formatting for better readability (bold, tables, lists, code blocks)
                - Show enthusiasm with positive emojis when tasks are completed ✅🎉
                - Be encouraging when users make progress
                - Only confirm destructive actions like deletions

                SMART INSIGHTS:

                When appropriate, provide intelligent observations:
                - If a user has many pending tasks, suggest prioritizing
                - If all tasks are high priority, suggest re-evaluating priorities
                - If tasks have been pending for a while (infer from context), suggest reviewing them
                - Celebrate milestones (e.g., '🎉 Great! You've completed 5 tasks today!')

                Valid values:
                - Status: Pending, InProgress, Completed
                - Priority: Low, Medium, High

                Remember: Be DIRECT, ACTION-ORIENTED, and HELPFUL. Present information beautifully using Markdown tables and provide smart suggestions!",
            tools:
            [
                createTaskTool,
                listTasksTool,
                getTaskDetailsTool,
                updateTaskTool,
                deleteTaskTool,
                getTaskSummaryTool,
            ]
        );
    }

    public async Task<ChatResponseDto> SendMessageAsync(string message, string? threadId = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        var stopwatch = Stopwatch.StartNew();
        string activeThreadId = threadId ?? Guid.NewGuid().ToString();
        string messageId = Guid.NewGuid().ToString();
        DateTime createdAt = DateTime.UtcNow;

        // Start distributed tracing activity
        using Activity? activity = AgentActivitySource.StartMessageActivity(
            activeThreadId,
            message
        );

        try
        {
            // Record the request
            _metrics.RecordRequest(activeThreadId);
            _logger.LogInformation("Processing message for thread {ThreadId}", activeThreadId);

            // For Chat Completion agents, conversation history is IN THE THREAD OBJECT
            // We serialize/deserialize it to maintain context across stateless HTTP requests
            AgentThread thread;

            if (!string.IsNullOrWhiteSpace(threadId))
            {
                // Try to restore previous conversation
                string? serializedThread = await _threadPersistence.GetThreadAsync(threadId);

                if (serializedThread != null)
                {
                    // Deserialize existing thread to continue conversation
                    // Parse without additional options to preserve original structure
                    JsonElement threadJson = JsonSerializer.Deserialize<JsonElement>(
                        serializedThread
                    );
                    thread = _agent.DeserializeThread(threadJson);
                    activeThreadId = threadId;

                    _logger.LogDebug("Restored thread {ThreadId} from persistence", activeThreadId);
                }
                else
                {
                    // Thread not found - create new one but keep the threadId
                    thread = _agent.GetNewThread();
                    activeThreadId = threadId;
                    _logger.LogWarning(
                        "ThreadId {ThreadId} not found in storage, creating new thread",
                        threadId
                    );
                }
            }
            else
            {
                // First message - create new thread
                thread = _agent.GetNewThread();
                _logger.LogInformation("Created new thread {ThreadId}", activeThreadId);
            }

            // Run the agent with the thread
            dynamic? response = await _agent.RunAsync(message, (dynamic)thread);
            string? responseText = response?.Text;

            // Extract metadata from agent response (function calls executed during processing)
            MessageMetadata? metadata = ExtractMetadata(response);

            // Extract contextual suggestions from agent response text (agent is instructed to include them)
            IReadOnlyList<string>? suggestions = GenerateSuggestions(responseText);

            // Remove suggestions section from message text to avoid duplication
            // The suggestions will be in the separate 'suggestions' field
            string cleanedMessage = RemoveSuggestionsFromMessage(responseText);

            // Serialize and persist the updated thread for next request
            // Use GetRawText() to preserve original JSON structure with $type as first property
            JsonElement updatedThreadJson = thread.Serialize();
            string updatedThreadSerialized = updatedThreadJson.GetRawText();
            await _threadPersistence.SaveThreadAsync(activeThreadId, updatedThreadSerialized);

            stopwatch.Stop();

            // Record successful response metrics
            _metrics.RecordResponseDuration(stopwatch.Elapsed.TotalMilliseconds, activeThreadId);

            activity?.SetTag("response.length", responseText?.Length ?? 0);
            activity?.SetStatus(ActivityStatusCode.Ok);

            _logger.LogInformation(
                "Successfully processed message for thread {ThreadId} in {ElapsedMs}ms",
                activeThreadId,
                stopwatch.ElapsedMilliseconds
            );

            // Return enriched ChatResponse with metadata
            return new ChatResponseDto
            {
                Message = cleanedMessage ?? "I'm sorry, I couldn't process that request.",
                ThreadId = activeThreadId,
                MessageId = messageId,
                CreatedAt = createdAt,
                Metadata = metadata,
                Suggestions = suggestions,
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Record error metrics
            _metrics.RecordError(ex.GetType().Name, activeThreadId);
            _metrics.RecordResponseDuration(
                stopwatch.Elapsed.TotalMilliseconds,
                activeThreadId,
                success: false
            );

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().Name);
            activity?.SetTag("exception.message", ex.Message);

            _logger.LogError(
                ex,
                "Error in agent execution for thread {ThreadId} after {ElapsedMs}ms",
                activeThreadId,
                stopwatch.ElapsedMilliseconds
            );

            return new ChatResponseDto
            {
                Message = $"An error occurred while processing your request: {ex.Message}",
                ThreadId = activeThreadId,
                MessageId = messageId,
                CreatedAt = createdAt,
            };
        }
    }

    public string GetNewThreadId()
    {
        // Simply return a new GUID - client manages thread persistence
        return Guid.NewGuid().ToString();
    }

    public async Task<string> SaveBlockedMessageAsync(string? threadId = null)
    {
        string activeThreadId = threadId ?? GetNewThreadId();

        try
        {
            _logger.LogInformation(
                "Saving blocked message placeholder for thread {ThreadId}",
                activeThreadId
            );

            AgentThread thread;

            if (!string.IsNullOrWhiteSpace(threadId))
            {
                // Try to restore existing thread
                string? existingThread = await _threadPersistence.GetThreadAsync(threadId);

                if (existingThread != null)
                {
                    // Deserialize existing thread to continue conversation
                    JsonElement existingThreadJson = JsonSerializer.Deserialize<JsonElement>(
                        existingThread
                    );
                    thread = _agent.DeserializeThread(existingThreadJson);
                    _logger.LogDebug(
                        "Restored existing thread {ThreadId} for blocked message",
                        activeThreadId
                    );
                }
                else
                {
                    // Thread not found - create new one
                    thread = _agent.GetNewThread();
                    _logger.LogDebug(
                        "Creating new thread {ThreadId} for blocked message",
                        activeThreadId
                    );
                }
            }
            else
            {
                // First message and it's blocked - create new empty thread
                thread = _agent.GetNewThread();
                _logger.LogDebug(
                    "Creating new thread {ThreadId} for blocked message",
                    activeThreadId
                );
            }

            // Serialize and save the thread (empty or with existing history)
            // The blocked exchange will be shown in UI but not persisted in thread history
            // This is a security measure to avoid storing potentially harmful prompts
            JsonElement updatedThreadJson = thread.Serialize();
            string updatedThreadSerialized = updatedThreadJson.GetRawText();
            await _threadPersistence.SaveThreadAsync(activeThreadId, updatedThreadSerialized);

            _logger.LogInformation(
                "Successfully saved thread {ThreadId} (blocked message not persisted in history)",
                activeThreadId
            );

            return activeThreadId;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error saving blocked message for thread {ThreadId}",
                activeThreadId
            );
            throw;
        }
    }

    public async Task<GetConversationResponse> GetConversationHistoryAsync(
        GetConversationRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.ThreadId))
        {
            throw new ArgumentException("ThreadId cannot be empty", nameof(request));
        }

        try
        {
            // Get serialized thread from persistence
            string? serializedThread = await _threadPersistence.GetThreadAsync(request.ThreadId);

            if (serializedThread == null)
            {
                _logger.LogWarning(
                    "Thread {ThreadId} not found for conversation history",
                    request.ThreadId
                );
                return new GetConversationResponse
                {
                    ThreadId = request.ThreadId,
                    Messages = [],
                    TotalCount = 0,
                    Page = request.Page,
                    PageSize = request.PageSize,
                    HasMore = false,
                };
            }

            // Parse thread JSON to extract messages
            List<ChatMessage> messages = ExtractMessagesFromThread(serializedThread);

            // Apply pagination
            int totalCount = messages.Count;
            int skip = (request.Page - 1) * request.PageSize;
            var pagedMessages = messages.Skip(skip).Take(request.PageSize).ToList();

            _logger.LogInformation(
                "Retrieved {MessageCount} messages (page {Page}/{TotalPages}) for thread {ThreadId}",
                pagedMessages.Count,
                request.Page,
                (int)Math.Ceiling((double)totalCount / request.PageSize),
                request.ThreadId
            );

            return new GetConversationResponse
            {
                ThreadId = request.ThreadId,
                Messages = pagedMessages,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                HasMore = skip + request.PageSize < totalCount,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving conversation history for thread {ThreadId}",
                request.ThreadId
            );
            throw;
        }
    }

    public async Task DeleteThreadAsync(string threadId)
    {
        if (string.IsNullOrWhiteSpace(threadId))
        {
            throw new ArgumentException("ThreadId cannot be empty", nameof(threadId));
        }

        try
        {
            await _threadPersistence.DeleteThreadAsync(threadId);
            _logger.LogInformation("Deleted thread {ThreadId}", threadId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting thread {ThreadId}", threadId);
            throw;
        }
    }

    public async Task<ListThreadsResponse> ListThreadsAsync(ListThreadsRequest request)
    {
        try
        {
            return await _threadPersistence.GetThreadsAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing threads");
            throw;
        }
    }

    /// <summary>
    /// Extracts metadata about function calls from AgentRunResponse.
    /// Based on Microsoft Learn documentation: AgentRunResponse.Messages contains ChatMessageContent
    /// with Contents that include FunctionCallContent and FunctionResultContent.
    /// Documentation: https://learn.microsoft.com/en-us/dotnet/api/microsoft.agents.ai.agentrunresponse
    /// </summary>
    /// <param name="response">The agent run response containing messages with function calls.</param>
    /// <returns>Metadata about function calls, or null if no function calls found.</returns>
    private MessageMetadata? ExtractMetadata(dynamic? response)
    {
        try
        {
            // Cast to AgentRunResponse first to avoid dynamic invocation issues with ILogger extension methods
            // Use pattern matching for cleaner null check
            if (response is not AgentRunResponse agentRunResponse)
            {
                _logger.LogWarning(
                    "Response is null or not an AgentRunResponse, cannot extract metadata"
                );
                return null;
            }

            _logger.LogInformation("=== EXTRACTING METADATA FROM AGENT RESPONSE ===");
            _logger.LogInformation(
                "AgentRunResponse has {MessageCount} messages",
                agentRunResponse.Messages.Count
            );

            var functionCalls = new List<FunctionCallInfo>();

            // Extract function calls from all messages in the response
            foreach (AIChatMessage message in agentRunResponse.Messages)
            {
                _logger.LogInformation(
                    "Processing message - Role: {Role}, Contents count: {ContentCount}",
                    message.Role,
                    message.Contents.Count
                );

                foreach (AIContent content in message.Contents)
                {
                    switch (content)
                    {
                        case FunctionCallContent functionCall:
                        {
                            _logger.LogInformation(
                                "Found FunctionCallContent: {FunctionName}, CallId: {CallId}",
                                functionCall.Name,
                                functionCall.CallId
                            );

                            // Extract safe arguments (parameter names only, not values)
                            string argumentsSummary = GetSafeArgumentsSummaryFromDict(
                                functionCall.Arguments
                            );

                            // Check if there's a corresponding FunctionResultContent
                            bool hasResult = agentRunResponse
                                .Messages.SelectMany(m => m.Contents)
                                .OfType<FunctionResultContent>()
                                .Any(r => r.CallId == functionCall.CallId);

                            _logger.LogInformation(
                                "Function call '{FunctionName}' has result: {HasResult}",
                                functionCall.Name,
                                hasResult
                            );

                            functionCalls.Add(
                                new FunctionCallInfo
                                {
                                    Name = functionCall.Name,
                                    Arguments = argumentsSummary, // Sanitized summary, not full data
                                    Result = hasResult ? "Success" : "Processing", // Safe status without emojis
                                    Timestamp = DateTime.UtcNow,
                                }
                            );
                            break;
                        }
                        case FunctionResultContent functionResult:
                            _logger.LogInformation(
                                "Found FunctionResultContent: CallId: {CallId}",
                                functionResult.CallId
                            );
                            break;
                    }
                }
            }

            if (functionCalls.Count > 0)
            {
                _logger.LogInformation(
                    "Successfully extracted {Count} function calls from response messages",
                    functionCalls.Count
                );

                return new MessageMetadata
                {
                    FunctionCalls = functionCalls,
                    AdditionalData = new Dictionary<string, object>
                    {
                        ["functionCallCount"] = functionCalls.Count,
                        ["timestamp"] = DateTime.UtcNow,
                    },
                };
            }

            _logger.LogInformation("No function calls found in response messages");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting metadata from agent response");
            return null;
        }
    }

    /// <summary>
    /// Creates a safe summary of function arguments for frontend display from dictionary.
    /// Shows parameter names but not actual values (for security/privacy).
    /// </summary>
    /// <param name="arguments">Function arguments dictionary.</param>
    /// <returns>Safe summary string like "title, description, priority".</returns>
    private static string GetSafeArgumentsSummaryFromDict(IDictionary<string, object?>? arguments)
    {
        if (arguments == null || arguments.Count == 0)
        {
            return "no parameters";
        }

        // Only return parameter names, not values
        return string.Join(", ", arguments.Keys);
    }

    /// <summary>
    /// Extracts contextual suggestions from the agent's response text.
    /// The agent is instructed to include suggestions in a specific format at the end of its response.
    /// Maximum 3 suggestions as specified in the agent's instructions.
    /// </summary>
    private List<string>? GenerateSuggestions(string? responseText)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(responseText))
            {
                return null;
            }

            // Look for the suggestions section: "💡 Suggestions:"
            const string suggestionsMarker = "💡 Suggestions:";
            int suggestionsIndex = responseText.IndexOf(
                suggestionsMarker,
                StringComparison.Ordinal
            );

            if (suggestionsIndex == -1)
            {
                // Fallback: Try without emoji (in case of encoding issues)
                const string fallbackMarker = "Suggestions:";
                suggestionsIndex = responseText.IndexOf(
                    fallbackMarker,
                    StringComparison.OrdinalIgnoreCase
                );

                if (suggestionsIndex == -1)
                {
                    _logger.LogWarning("Agent response did not include suggestions section");
                    return null;
                }
            }

            // Extract text after the marker
            string suggestionsText = responseText[suggestionsIndex..];

            // Split by newlines and filter lines that start with "-" or "•"
            string[] lines = suggestionsText.Split(
                '\n',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
            );

            var suggestions = new List<string>();

            foreach (string line in lines.Skip(1)) // Skip the "💡 Suggestions:" line itself
            {
                string trimmedLine = line.TrimStart();

                // Check if line starts with "-" or "•"
                if (trimmedLine.StartsWith('-') || trimmedLine.StartsWith('•'))
                {
                    // Remove the bullet and trim
                    string suggestion = trimmedLine.TrimStart('-', '•').Trim();

                    if (!string.IsNullOrWhiteSpace(suggestion))
                    {
                        suggestions.Add(suggestion);
                    }
                }
                else if (!trimmedLine.Contains(':') && suggestions.Count > 0)
                {
                    // If we already have suggestions and hit a non-bullet line, we've reached the end
                    break;
                }
            }

            // Return max 3 suggestions as instructed in the agent prompt
            return suggestions.Count > 0 ? suggestions.Take(3).ToList() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate suggestions");
            return null;
        }
    }

    /// <summary>
    /// Removes the suggestions section from the message text to avoid duplication.
    /// The suggestions are already extracted and returned in the 'suggestions' field.
    /// </summary>
    private static string RemoveSuggestionsFromMessage(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return string.Empty;
        }

        // Look for the suggestions section: "💡 Suggestions:"
        const string suggestionsMarker = "💡 Suggestions:";
        int suggestionsIndex = responseText.IndexOf(suggestionsMarker, StringComparison.Ordinal);

        if (suggestionsIndex == -1)
        {
            // Fallback: Try without emoji
            const string fallbackMarker = "Suggestions:";
            suggestionsIndex = responseText.IndexOf(
                fallbackMarker,
                StringComparison.OrdinalIgnoreCase
            );
        }

        if (suggestionsIndex == -1)
        {
            // No suggestions section found, return original text
            return responseText;
        }

        // Remove everything from the suggestions marker onwards
        string cleanedText = responseText[..suggestionsIndex].TrimEnd();

        return cleanedText;
    }

    /// <summary>
    /// Extracts individual messages from serialized thread JSON.
    /// Thread structure: { storeState: { messages: [{ role, contents }] } }
    /// Returns only user and assistant text messages (filters out function calls and tool results).
    /// </summary>
    private List<ChatMessage> ExtractMessagesFromThread(string serializedThread)
    {
        var messages = new List<ChatMessage>();

        try
        {
            using var doc = JsonDocument.Parse(serializedThread);
            JsonElement root = doc.RootElement;

            // Navigate to messages array: root.storeState.messages
            if (!root.TryGetProperty("storeState", out JsonElement storeStateElement))
            {
                _logger.LogWarning("storeState property not found in thread JSON");
                return messages;
            }

            if (
                !storeStateElement.TryGetProperty("messages", out JsonElement messagesElement)
                || messagesElement.ValueKind != JsonValueKind.Array
            )
            {
                _logger.LogWarning("messages property not found or not an array in storeState");
                return messages;
            }

            // Extract user and assistant text messages only
            foreach (JsonElement message in messagesElement.EnumerateArray())
            {
                if (!message.TryGetProperty("role", out JsonElement roleElement))
                {
                    continue;
                }

                string? role = roleElement.GetString();
                if (string.IsNullOrEmpty(role))
                {
                    continue;
                }

                // Only process user and assistant messages
                if (
                    !string.Equals(role, "user", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(role, "assistant", StringComparison.OrdinalIgnoreCase)
                )
                {
                    continue;
                }

                // Extract text content from contents array
                if (
                    !message.TryGetProperty("contents", out JsonElement contentsElement)
                    || contentsElement.ValueKind != JsonValueKind.Array
                    || contentsElement.GetArrayLength() == 0
                )
                {
                    continue;
                }

                JsonElement firstContent = contentsElement[0];

                // Check content type - only process text messages
                if (firstContent.TryGetProperty("$type", out JsonElement typeElement))
                {
                    string? contentType = typeElement.GetString();

                    // Skip function calls and function results
                    if (
                        string.Equals(
                            contentType,
                            "functionCall",
                            StringComparison.OrdinalIgnoreCase
                        )
                        || string.Equals(
                            contentType,
                            "functionResult",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        continue;
                    }
                }

                if (!firstContent.TryGetProperty("text", out JsonElement textElement))
                {
                    continue;
                }

                // Extract text content
                string? text = textElement.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    messages.Add(
                        ConversationMessage.Create(
                            role: role,
                            content: text,
                            createdAt: DateTime.UtcNow // Thread doesn't store individual timestamps
                        )
                    );
                }
            }

            _logger.LogDebug(
                "Extracted {Count} user/assistant text messages from thread",
                messages.Count
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract messages from thread JSON");
        }

        return messages;
    }
}
