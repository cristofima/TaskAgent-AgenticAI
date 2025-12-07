using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TaskAgent.Application.DTOs.Responses;
using TaskAgent.Domain.Entities;
using TaskAgent.Infrastructure.Data;
using TaskAgent.Infrastructure.IntegrationTests.Fixtures;
using TaskAgent.Infrastructure.Services;

namespace TaskAgent.Infrastructure.IntegrationTests.Services;

/// <summary>
/// Integration tests for ConversationService using PostgreSQL Testcontainers.
/// These tests verify actual database operations against a real PostgreSQL instance.
/// </summary>
[Collection("PostgreSql")]
public class ConversationServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainerFixture _fixture;
    private ConversationDbContext _context = null!;
    private ConversationService _service = null!;

    public ConversationServiceTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        // Create fresh context for each test to ensure isolation
        _context = _fixture.CreateDbContext();
        _service = new ConversationService(_context, NullLogger<ConversationService>.Instance);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        // Clean up test data after each test
        await _context.ConversationMessages.ExecuteDeleteAsync();
        await _context.ConversationThreads.ExecuteDeleteAsync();
        await _context.DisposeAsync();
    }

    #region Helper Methods

    private async Task<ConversationThreadMetadata> CreateTestThreadAsync(
        string? title = null,
        string? preview = null,
        bool isActive = true)
    {
        string threadId = Guid.NewGuid().ToString();
        var thread = ConversationThreadMetadata.Create(threadId);

        if (title != null || preview != null)
        {
            thread.UpdateMetadata(title, preview, 0, null);
        }

        if (!isActive)
        {
            thread.Deactivate();
        }

        _context.ConversationThreads.Add(thread);
        await _context.SaveChangesAsync();
        return thread;
    }

    private async Task CreateTestMessageAsync(string threadId,
        string role,
        string content)
    {
        string messageId = Guid.NewGuid().ToString();
        string jsonContent = $"[{{\"Text\": \"{content}\"}}]";
        var message = ConversationMessage.Create(messageId, threadId, role, jsonContent);

        _context.ConversationMessages.Add(message);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region ListThreadsAsync Tests

    [Fact]
    public async Task ListThreadsAsync_WithMultipleThreads_ReturnsPaginatedResults()
    {
        // Arrange
        await CreateTestThreadAsync(title: "Thread 1", preview: "Preview 1");
        await Task.Delay(10); // Ensure different timestamps
        await CreateTestThreadAsync(title: "Thread 2", preview: "Preview 2");
        await Task.Delay(10);
        await CreateTestThreadAsync(title: "Thread 3", preview: "Preview 3");

        // Act
        ListThreadsResponseDto result = await _service.ListThreadsAsync(
            page: 1,
            pageSize: 10,
            sortBy: "updatedAt",
            sortOrder: "desc");

        // Assert
        result.Should().NotBeNull();
        result.Threads.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task ListThreadsAsync_WithPagination_RespectsPageSize()
    {
        // Arrange - Create 5 threads
        for (int i = 1; i <= 5; i++)
        {
            await CreateTestThreadAsync(title: $"Thread {i}");
            await Task.Delay(10);
        }

        // Act - Get page 1 with size 2
        ListThreadsResponseDto page1 = await _service.ListThreadsAsync(
            page: 1,
            pageSize: 2,
            sortBy: "updatedAt",
            sortOrder: "desc");

        // Act - Get page 2
        ListThreadsResponseDto page2 = await _service.ListThreadsAsync(
            page: 2,
            pageSize: 2,
            sortBy: "updatedAt",
            sortOrder: "desc");

        // Assert
        page1.Threads.Should().HaveCount(2);
        page1.TotalCount.Should().Be(5);
        page1.TotalPages.Should().Be(3);

        page2.Threads.Should().HaveCount(2);

        // Ensure different threads on different pages
        IEnumerable<string> page1Ids = page1.Threads.Select(t => t.Id);
        IEnumerable<string> page2Ids = page2.Threads.Select(t => t.Id);
        page1Ids.Should().NotIntersectWith(page2Ids);
    }

    [Fact]
    public async Task ListThreadsAsync_WithSortByUpdatedAt_SortsDescByDefault()
    {
        // Arrange
        ConversationThreadMetadata oldThread = await CreateTestThreadAsync(title: "Old Thread");
        await Task.Delay(50);
        ConversationThreadMetadata newThread = await CreateTestThreadAsync(title: "New Thread");

        // Act
        ListThreadsResponseDto result = await _service.ListThreadsAsync(
            page: 1,
            pageSize: 10,
            sortBy: "updatedAt",
            sortOrder: "desc");

        // Assert
        result.Threads.Should().HaveCount(2);
        result.Threads[0].Id.Should().Be(newThread.ThreadId); // Most recent first
        result.Threads[1].Id.Should().Be(oldThread.ThreadId);
    }

    [Fact]
    public async Task ListThreadsAsync_ExcludesInactiveThreads()
    {
        // Arrange
        await CreateTestThreadAsync(title: "Active Thread", isActive: true);
        await CreateTestThreadAsync(title: "Inactive Thread", isActive: false);

        // Act
        ListThreadsResponseDto result = await _service.ListThreadsAsync(
            page: 1,
            pageSize: 10,
            sortBy: "updatedAt",
            sortOrder: "desc");

        // Assert
        result.Threads.Should().HaveCount(1);
        result.Threads[0].Title.Should().Be("Active Thread");
    }

    [Fact]
    public async Task ListThreadsAsync_WhenNoThreads_ReturnsEmptyResult()
    {
        // Arrange - database is empty

        // Act
        ListThreadsResponseDto result = await _service.ListThreadsAsync(
            page: 1,
            pageSize: 10,
            sortBy: "updatedAt",
            sortOrder: "desc");

        // Assert
        result.Should().NotBeNull();
        result.Threads.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }

    #endregion

    #region GetConversationHistoryAsync Tests

    [Fact]
    public async Task GetConversationHistoryAsync_WithValidThreadId_ReturnsMessages()
    {
        // Arrange
        ConversationThreadMetadata thread = await CreateTestThreadAsync(title: "Test Thread");
        await CreateTestMessageAsync(thread.ThreadId, "user", "Hello");
        await Task.Delay(10);
        await CreateTestMessageAsync(thread.ThreadId, "assistant", "Hi there!");

        // Act
        GetConversationResponseDto? result = await _service.GetConversationHistoryAsync(
            thread.ThreadId,
            page: 1,
            pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result!.ThreadId.Should().Be(thread.ThreadId);
        result.Messages.Should().HaveCount(2);
        result.Messages[0].Role.Should().Be("user");
        result.Messages[0].Content.Should().Be("Hello");
        result.Messages[1].Role.Should().Be("assistant");
        result.Messages[1].Content.Should().Be("Hi there!");
    }

    [Fact]
    public async Task GetConversationHistoryAsync_WithInvalidThreadId_ReturnsNull()
    {
        // Arrange
        string nonExistentThreadId = Guid.NewGuid().ToString();

        // Act
        GetConversationResponseDto? result = await _service.GetConversationHistoryAsync(
            nonExistentThreadId,
            page: 1,
            pageSize: 10);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConversationHistoryAsync_FiltersSystemMessages()
    {
        // Arrange
        ConversationThreadMetadata thread = await CreateTestThreadAsync(title: "Test Thread");
        await CreateTestMessageAsync(thread.ThreadId, "user", "Hello");
        await CreateTestMessageAsync(thread.ThreadId, "system", "System message");
        await CreateTestMessageAsync(thread.ThreadId, "assistant", "Response");
        await CreateTestMessageAsync(thread.ThreadId, "tool", "Tool result");

        // Act
        GetConversationResponseDto? result = await _service.GetConversationHistoryAsync(
            thread.ThreadId,
            page: 1,
            pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result!.Messages.Should().HaveCount(2); // Only user and assistant
        result.Messages.Should().OnlyContain(m => m.Role == "user" || m.Role == "assistant");
    }

    [Fact]
    public async Task GetConversationHistoryAsync_WithEmptyThread_ReturnsEmptyMessages()
    {
        // Arrange
        ConversationThreadMetadata thread = await CreateTestThreadAsync(title: "Empty Thread");
        // No messages added

        // Act
        GetConversationResponseDto? result = await _service.GetConversationHistoryAsync(
            thread.ThreadId,
            page: 1,
            pageSize: 10);

        // Assert
        result.Should().NotBeNull();
        result!.ThreadId.Should().Be(thread.ThreadId);
        result.Messages.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetConversationHistoryAsync_WithInactiveThread_ReturnsNull()
    {
        // Arrange
        ConversationThreadMetadata thread = await CreateTestThreadAsync(title: "Inactive Thread", isActive: false);
        await CreateTestMessageAsync(thread.ThreadId, "user", "Hello");

        // Act
        GetConversationResponseDto? result = await _service.GetConversationHistoryAsync(
            thread.ThreadId,
            page: 1,
            pageSize: 10);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region DeleteThreadAsync Tests

    [Fact]
    public async Task DeleteThreadAsync_WithValidId_SetsIsActiveToFalse()
    {
        // Arrange
        ConversationThreadMetadata thread = await CreateTestThreadAsync(title: "Thread to Delete");
        await CreateTestMessageAsync(thread.ThreadId, "user", "Message 1");
        await CreateTestMessageAsync(thread.ThreadId, "assistant", "Message 2");

        // Act
        bool result = await _service.DeleteThreadAsync(thread.ThreadId);

        // Assert
        result.Should().BeTrue();

        // Verify in database with new context
        await using ConversationDbContext verificationContext = _fixture.CreateDbContext();
        ConversationThreadMetadata? deletedThread = await verificationContext.ConversationThreads
            .FirstOrDefaultAsync(t => t.ThreadId == thread.ThreadId);

        deletedThread.Should().NotBeNull();
        deletedThread!.IsActive.Should().BeFalse();

        // Messages should also be deleted
        List<ConversationMessage> messages = await verificationContext.ConversationMessages
            .Where(m => m.ThreadId == thread.ThreadId)
            .ToListAsync();
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteThreadAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        string nonExistentThreadId = Guid.NewGuid().ToString();

        // Act
        bool result = await _service.DeleteThreadAsync(nonExistentThreadId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteThreadAsync_WithAlreadyInactiveThread_ReturnsFalse()
    {
        // Arrange
        ConversationThreadMetadata thread = await CreateTestThreadAsync(title: "Already Inactive", isActive: false);

        // Act
        bool result = await _service.DeleteThreadAsync(thread.ThreadId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Sorting Tests

    [Fact]
    public async Task ListThreadsAsync_WithSortByCreatedAt_SortsCorrectly()
    {
        // Arrange
        ConversationThreadMetadata thread1 = await CreateTestThreadAsync(title: "First Created");
        await Task.Delay(50);
        ConversationThreadMetadata thread2 = await CreateTestThreadAsync(title: "Second Created");

        // Act - Sort ascending
        ListThreadsResponseDto ascResult = await _service.ListThreadsAsync(
            page: 1,
            pageSize: 10,
            sortBy: "createdAt",
            sortOrder: "asc");

        // Act - Sort descending
        ListThreadsResponseDto descResult = await _service.ListThreadsAsync(
            page: 1,
            pageSize: 10,
            sortBy: "createdAt",
            sortOrder: "desc");

        // Assert
        ascResult.Threads[0].Id.Should().Be(thread1.ThreadId); // Oldest first
        ascResult.Threads[1].Id.Should().Be(thread2.ThreadId);

        descResult.Threads[0].Id.Should().Be(thread2.ThreadId); // Newest first
        descResult.Threads[1].Id.Should().Be(thread1.ThreadId);
    }

    #endregion
}
