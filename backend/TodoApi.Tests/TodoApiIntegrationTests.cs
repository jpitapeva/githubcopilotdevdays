using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TodoApi.Tests;

public class TodoApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly string _dbPath;
    private readonly HttpClient _authorizedClient;
    private readonly WebApplicationFactory<Program> _factory;

    public TodoApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"todo-api-tests-{Guid.NewGuid():N}.db");

        Environment.SetEnvironmentVariable("DB_PATH", _dbPath);
        Environment.SetEnvironmentVariable("ADMIN_USERNAME", "admin");
        Environment.SetEnvironmentVariable("ADMIN_PASSWORD", "admin");
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:0");

        _factory = factory;
        _authorizedClient = _factory.CreateClient();
        _authorizedClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:admin")));
    }

    [Fact]
    public async Task Should_Return401_When_AuthHeader_IsMissing()
    {
        using var anonymousClient = _factory.CreateClient();

        var response = await anonymousClient.GetAsync("/api/todos");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Should_Create_Update_And_Delete_Todo()
    {
        var createResponse = await _authorizedClient.PostAsJsonAsync("/api/todos", new { title = "Estudar .NET" });
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<TodoItemDto>();
        Assert.NotNull(created);
        Assert.Equal("Estudar .NET", created!.Title);

        var updateResponse = await _authorizedClient.PutAsJsonAsync($"/api/todos/{created.Id}", new { title = "Estudar .NET 10", isDone = true });
        updateResponse.EnsureSuccessStatusCode();

        var updated = await updateResponse.Content.ReadFromJsonAsync<TodoItemDto>();
        Assert.NotNull(updated);
        Assert.Equal("Estudar .NET 10", updated!.Title);
        Assert.True(updated.IsDone);

        var deleteResponse = await _authorizedClient.DeleteAsync($"/api/todos/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await _authorizedClient.GetAsync("/api/todos");
        listResponse.EnsureSuccessStatusCode();

        var todos = await listResponse.Content.ReadFromJsonAsync<List<TodoItemDto>>();
        Assert.NotNull(todos);
        Assert.Empty(todos!);
    }

    public void Dispose()
    {
        _authorizedClient.Dispose();
    }

    private sealed record TodoItemDto(int Id, string Title, bool IsDone, DateTime CreatedAtUtc);
}
