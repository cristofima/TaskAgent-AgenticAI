using System.Diagnostics;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using TaskAgent.Application.Functions;
using TaskAgent.Application.Interfaces;
using TaskAgent.Application.Telemetry;

namespace TaskAgent.WebApp.Services;

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
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (string.IsNullOrWhiteSpace(modelDeployment))
        {
            throw new ArgumentException(
                "Model deployment cannot be empty",
                nameof(modelDeployment)
            );
        }

        if (taskRepository == null)
        {
            throw new ArgumentNullException(nameof(taskRepository));
        }

        if (metrics == null)
        {
            throw new ArgumentNullException(nameof(metrics));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

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

                CONTEXTUAL SUGGESTIONS:

                After each response, provide 1-2 helpful suggestions based on the context:

                After creating a task:
                - Suggest viewing all tasks or the specific task details
                - Suggest creating related tasks if the title implies a project
                - Example: '💡 Suggestions: • View all pending tasks • Create a follow-up task'

                After listing tasks:
                - Suggest filtering by status or priority if showing all tasks
                - Suggest updating the oldest pending task
                - Suggest getting a summary
                - Example: '💡 Suggestions: • Filter by high priority • Update task #X status'

                After updating a task:
                - Suggest viewing the updated task details
                - If completed, suggest viewing remaining pending tasks
                - Example: '💡 Suggestions: • View updated task details • List remaining pending tasks'

                After deleting a task:
                - Suggest viewing remaining tasks
                - Suggest getting a fresh summary
                - Example: '� Suggestions: • View remaining tasks • Get task summary'

                After showing a summary:
                - Suggest viewing the oldest pending tasks
                - Suggest updating high priority tasks
                - Example: '� Suggestions: • View all high priority tasks • Update oldest pending task'

                When there are no tasks:
                - Suggest creating their first task
                - Provide examples of good task titles
                - Example: '� Suggestions: • Create your first task • Start with a high priority task'

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

    public async Task<(string response, string threadId)> SendMessageAsync(
        string message,
        string? threadId = null
    )
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message cannot be empty", nameof(message));
        }

        var stopwatch = Stopwatch.StartNew();
        string activeThreadId = threadId ?? Guid.NewGuid().ToString();

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

            // Serialize and persist the updated thread for next request
            JsonElement updatedThreadJson = thread.Serialize();
            string updatedThreadSerialized = JsonSerializer.Serialize(
                updatedThreadJson,
                JsonSerializerOptions.Web
            );
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

            return (responseText ?? "I'm sorry, I couldn't process that request.", activeThreadId);
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

            return (
                $"An error occurred while processing your request: {ex.Message}",
                activeThreadId
            );
        }
    }

    public string GetNewThreadId()
    {
        // Simply return a new GUID - client manages thread persistence
        return Guid.NewGuid().ToString();
    }
}
