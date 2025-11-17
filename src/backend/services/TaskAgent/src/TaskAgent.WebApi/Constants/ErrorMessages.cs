namespace TaskAgent.WebApi.Constants;

/// <summary>
/// User-facing error messages for the web application
/// </summary>
public static class ErrorMessages
{
    public const string MESSAGE_EMPTY = "Message cannot be empty";
    public const string PROCESSING_ERROR =
        "An error occurred while processing your message. Please try again.";
    public const string THREAD_CREATION_ERROR =
        "An error occurred while creating a new conversation thread.";
}
