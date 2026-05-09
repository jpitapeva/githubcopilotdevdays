using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using DotNetEnv;
using Microsoft.EntityFrameworkCore;

Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

var minimumLogLevel = ParseLogLevel(Environment.GetEnvironmentVariable("LOG_LEVEL"), LogLevel.Information);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(minimumLogLevel);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:5173";
var dbPath = Environment.GetEnvironmentVariable("DB_PATH") ?? "todo.db";
var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "admin";

var sqliteConnectionString = BuildSqliteConnectionString(dbPath);

builder.Services.AddDbContext<TodoDbContext>(options => options.UseSqlite(sqliteConnectionString));

builder.Services.AddCors(options =>
{
	options.AddPolicy("frontend", policy =>
	{
		policy.WithOrigins(frontendUrl)
			.AllowAnyHeader()
			.AllowAnyMethod();
	});
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var db = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
	db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("frontend");

app.Use(async (context, next) =>
{
	var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("RequestLogger");
	var sw = Stopwatch.StartNew();

	await next();

	sw.Stop();
	logger.LogInformation(
		"HTTP {Method} {Path} -> {StatusCode} ({ElapsedMs}ms)",
		context.Request.Method,
		context.Request.Path,
		context.Response.StatusCode,
		sw.ElapsedMilliseconds);
});

app.Use(async (context, next) =>
{
	if (!context.Request.Path.StartsWithSegments("/api"))
	{
		await next();
		return;
	}

	var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Auth");
	if (!TryReadBasicCredentials(context.Request.Headers.Authorization, out var username, out var password) ||
		!SecureEquals(username, adminUsername) ||
		!SecureEquals(password, adminPassword))
	{
		logger.LogWarning("Unauthorized request to {Path}", context.Request.Path);
		context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"TodoApi\"");
		context.Response.StatusCode = StatusCodes.Status401Unauthorized;
		await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
		return;
	}

	await next();
});

app.MapGet("/", () => Results.Ok(new { message = "Todo API running" }));

app.MapGet("/api/todos", async (TodoDbContext db) =>
{
	var all = await db.Todos.AsNoTracking().OrderBy(todo => todo.Id).ToListAsync();
	return Results.Ok(all);
});

app.MapPost("/api/todos", async (CreateTodoRequest request, TodoDbContext db, ILoggerFactory loggerFactory) =>
{
	if (string.IsNullOrWhiteSpace(request.Title))
	{
		return Results.BadRequest(new { error = "Title is required" });
	}

	var todo = new TodoItem
	{
		Title = request.Title.Trim(),
		IsDone = false,
		CreatedAtUtc = DateTime.UtcNow
	};

	db.Todos.Add(todo);
	await db.SaveChangesAsync();
	loggerFactory.CreateLogger("TodoActions").LogInformation("Todo created {TodoId}", todo.Id);

	return Results.Created($"/api/todos/{todo.Id}", todo);
});

app.MapPut("/api/todos/{id:int}", async (int id, UpdateTodoRequest request, TodoDbContext db, ILoggerFactory loggerFactory) =>
{
	var current = await db.Todos.FirstOrDefaultAsync(todo => todo.Id == id);
	if (current is null)
	{
		return Results.NotFound();
	}

	if (!string.IsNullOrWhiteSpace(request.Title))
	{
		current.Title = request.Title.Trim();
	}

	if (request.IsDone is not null)
	{
		current.IsDone = request.IsDone.Value;
	}

	await db.SaveChangesAsync();
	loggerFactory.CreateLogger("TodoActions").LogInformation("Todo updated {TodoId}", current.Id);
	return Results.Ok(current);
});

app.MapDelete("/api/todos/{id:int}", async (int id, TodoDbContext db, ILoggerFactory loggerFactory) =>
{
	var current = await db.Todos.FirstOrDefaultAsync(todo => todo.Id == id);
	if (current is null)
	{
		return Results.NotFound();
	}

	db.Todos.Remove(current);
	await db.SaveChangesAsync();
	loggerFactory.CreateLogger("TodoActions").LogInformation("Todo deleted {TodoId}", id);
	return Results.NoContent();
});

app.Run();

static LogLevel ParseLogLevel(string? value, LogLevel fallback)
{
	return Enum.TryParse<LogLevel>(value, ignoreCase: true, out var parsed)
		? parsed
		: fallback;
}

static bool TryReadBasicCredentials(string? authorizationHeader, out string username, out string password)
{
	username = string.Empty;
	password = string.Empty;

	if (string.IsNullOrWhiteSpace(authorizationHeader) || !authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
	{
		return false;
	}

	var encoded = authorizationHeader["Basic ".Length..].Trim();
	try
	{
		var bytes = Convert.FromBase64String(encoded);
		var decoded = Encoding.UTF8.GetString(bytes);
		var separator = decoded.IndexOf(':');
		if (separator <= 0)
		{
			return false;
		}

		username = decoded[..separator];
		password = decoded[(separator + 1)..];
		return true;
	}
	catch (FormatException)
	{
		return false;
	}
}

static bool SecureEquals(string left, string right)
{
	var leftBytes = Encoding.UTF8.GetBytes(left);
	var rightBytes = Encoding.UTF8.GetBytes(right);
	return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
}

static string BuildSqliteConnectionString(string configuredPath)
{
	if (Path.IsPathRooted(configuredPath))
	{
		var directory = Path.GetDirectoryName(configuredPath);
		if (!string.IsNullOrWhiteSpace(directory))
		{
			Directory.CreateDirectory(directory);
		}

		return $"Data Source={configuredPath}";
	}

	var fullPath = Path.GetFullPath(configuredPath);
	var fullDirectory = Path.GetDirectoryName(fullPath);
	if (!string.IsNullOrWhiteSpace(fullDirectory))
	{
		Directory.CreateDirectory(fullDirectory);
	}

	return $"Data Source={fullPath}";
}

public class TodoItem
{
	public int Id { get; set; }
	public string Title { get; set; } = string.Empty;
	public bool IsDone { get; set; }
	public DateTime CreatedAtUtc { get; set; }
}

public record CreateTodoRequest(string Title);
public record UpdateTodoRequest(string? Title, bool? IsDone);

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
	public DbSet<TodoItem> Todos => Set<TodoItem>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<TodoItem>(entity =>
		{
			entity.HasKey(todo => todo.Id);
			entity.Property(todo => todo.Title).HasMaxLength(200).IsRequired();
		});
	}
}

public partial class Program;
