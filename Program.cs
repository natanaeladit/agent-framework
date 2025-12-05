using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

#region Setup Telemetry

const string SourceName = "LocalLLM.ConsoleApp";
const string ServiceName = "LocalLLMAgent";

// Configure OpenTelemetry for Aspire dashboard
var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4318";

// Create a resource to identify this service
var resource = ResourceBuilder.CreateDefault()
    .AddService(ServiceName, serviceVersion: "1.0.0")
    .AddAttributes(new Dictionary<string, object>
    {
        ["service.instance.id"] = Environment.MachineName,
        ["deployment.environment"] = "development"
    })
    .Build();

// Setup tracing with resource
var tracerProviderBuilder = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName, serviceVersion: "1.0.0"))
    .AddSource(SourceName) // Our custom activity source
    .AddSource("*Microsoft.Agents.AI") // Agent Framework telemetry
    .AddHttpClientInstrumentation() // Capture HTTP calls to LLM
    .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));

using var tracerProvider = tracerProviderBuilder.Build();

// Setup metrics with resource and instrument name filtering
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName, serviceVersion: "1.0.0"))
    .AddMeter(SourceName) // Our custom meter
    .AddMeter("*Microsoft.Agents.AI") // Agent Framework metrics
    .AddHttpClientInstrumentation() // HTTP client metrics
    .AddRuntimeInstrumentation() // .NET runtime metrics
    .AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint))
    .Build();

// Setup structured logging with OpenTelemetry
var serviceCollection = new ServiceCollection();
serviceCollection.AddLogging(loggingBuilder => loggingBuilder
    .SetMinimumLevel(LogLevel.Debug)
    .AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(ServiceName, serviceVersion: "1.0.0"));
        options.AddOtlpExporter(otlpOptions => otlpOptions.Endpoint = new Uri(otlpEndpoint));
        options.IncludeScopes = true;
        options.IncludeFormattedMessage = true;
    }));

using var activitySource = new ActivitySource(SourceName);
using var meter = new Meter(SourceName);

// Create custom metrics
var interactionCounter = meter.CreateCounter<int>("agent_interactions_total", description: "Total number of agent interactions");
var responseTimeHistogram = meter.CreateHistogram<double>("agent_response_time_seconds", description: "Agent response time in seconds");

#endregion

var serviceProvider = serviceCollection.BuildServiceProvider();
var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
var appLogger = loggerFactory.CreateLogger<Program>();

Console.WriteLine("""
    === Microsoft Agent Framework - Local LLM Chat ===
    This demo shows OpenTelemetry integration with a local LLM.
    You can view the telemetry data in the Aspire Dashboard.
    Type your message and press Enter. Type 'exit' or empty message to quit.
    """);

// Configuration for your local LLM
// Update these values to match your local LLM setup
var localLlmEndpoint = Environment.GetEnvironmentVariable("LOCAL_LLM_ENDPOINT") ?? "http://localhost:1234/v1/chat/completions";
var modelName = Environment.GetEnvironmentVariable("LOCAL_LLM_MODEL_NAME") ?? "local-model";
var apiKey = Environment.GetEnvironmentVariable("LOCAL_LLM_API_KEY") ?? "not-needed";

Console.WriteLine($"Connecting to: {localLlmEndpoint}");
Console.WriteLine($"Model: {modelName}");
Console.WriteLine();

// Log application startup
appLogger.LogInformation("Local LLM Agent application started");

// Create the instrumented chat client using the builder pattern
var instrumentedChatClient = new LocalLLMChatClient(localLlmEndpoint, modelName, apiKey)
    .AsBuilder()
    .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the chat client level
    .Build();

appLogger.LogInformation("Creating Agent with OpenTelemetry instrumentation");

// Create the agent with the instrumented chat client using the builder pattern
var agent = new ChatClientAgent(instrumentedChatClient,
    name: "LocalLLMAgent",
    instructions: "You are a helpful AI assistant. Be concise and friendly in your responses.")
    .AsBuilder()
    .UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true) // enable telemetry at the agent level
    .Build();

// Get a new thread for conversation management
var thread = agent.GetNewThread();

appLogger.LogInformation("Agent created successfully with ID: {AgentId}", agent.Id);

// Create a parent span for the entire agent session
using var sessionActivity = activitySource.StartActivity("Agent Session");
Console.WriteLine($"Trace ID: {sessionActivity?.TraceId}");
Console.WriteLine();

var sessionId = Guid.NewGuid().ToString("N");
sessionActivity?
    .SetTag("agent.name", "LocalLLMAgent")
    .SetTag("session.id", sessionId)
    .SetTag("session.start_time", DateTimeOffset.UtcNow.ToString("O"))
    .SetTag("llm.endpoint", localLlmEndpoint)
    .SetTag("llm.model", modelName);

appLogger.LogInformation("Starting agent session with ID: {SessionId}", sessionId);

using (appLogger.BeginScope(new Dictionary<string, object> { ["SessionId"] = sessionId, ["AgentName"] = "LocalLLMAgent" }))
{
    var interactionCount = 0;

    while (true)
    {
        Console.Write("You (or 'exit' to quit): ");
        var userInput = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            appLogger.LogInformation("User requested to exit the session");
            break;
        }

        interactionCount++;
        appLogger.LogInformation("Processing user interaction #{InteractionNumber}: {UserInput}", interactionCount, userInput);

        // Create a child span for each individual interaction
        using var activity = activitySource.StartActivity("Agent Interaction");
        activity?
            .SetTag("user.input", userInput)
            .SetTag("agent.name", "LocalLLMAgent")
            .SetTag("interaction.number", interactionCount);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            appLogger.LogDebug("Starting agent execution for interaction #{InteractionNumber}", interactionCount);
            Console.Write("Agent: ");

            // Run the agent with streaming (this will create its own internal telemetry spans)
            await foreach (var update in agent.RunStreamingAsync(userInput, thread))
            {
                Console.Write(update.Text);
            }

            Console.WriteLine();
            Console.WriteLine();

            stopwatch.Stop();
            var responseTime = stopwatch.Elapsed.TotalSeconds;

            // Record metrics
            interactionCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));
            responseTimeHistogram.Record(responseTime,
                new KeyValuePair<string, object?>("status", "success"));

            activity?.SetTag("response.success", true);
            activity?.SetTag("response.time_seconds", responseTime);

            appLogger.LogInformation("Agent interaction #{InteractionNumber} completed successfully in {ResponseTime:F2} seconds",
                interactionCount, responseTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Details: {ex.InnerException?.Message}");
            Console.WriteLine();

            stopwatch.Stop();
            var responseTime = stopwatch.Elapsed.TotalSeconds;

            // Record error metrics
            interactionCounter.Add(1, new KeyValuePair<string, object?>("status", "error"));
            responseTimeHistogram.Record(responseTime,
                new KeyValuePair<string, object?>("status", "error"));

            activity?
                .SetTag("response.success", false)
                .SetTag("error.message", ex.Message)
                .SetTag("error.type", ex.GetType().Name)
                .SetStatus(ActivityStatusCode.Error, ex.Message);

            appLogger.LogError(ex, "Agent interaction #{InteractionNumber} failed after {ResponseTime:F2} seconds: {ErrorMessage}",
                interactionCount, responseTime, ex.Message);
        }
    }

    // Add session summary to the parent span
    sessionActivity?
        .SetTag("session.total_interactions", interactionCount)
        .SetTag("session.end_time", DateTimeOffset.UtcNow.ToString("O"));

    appLogger.LogInformation("Agent session completed. Total interactions: {TotalInteractions}", interactionCount);
} // End of logging scope

Console.WriteLine("Goodbye!");
appLogger.LogInformation("Local LLM Agent application shutting down");

// Custom IChatClient implementation for local LLM
public class LocalLLMChatClient : IChatClient
{
    private readonly string _endpoint;
    private readonly string _modelName;
    private readonly string _apiKey;
    private readonly HttpClient _httpClient;

    public LocalLLMChatClient(string endpoint, string modelName, string apiKey)
    {
        _endpoint = endpoint;
        _modelName = modelName;
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    public ChatClientMetadata Metadata => new("LocalLLM", new Uri(_endpoint), _modelName);

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        // Convert messages to OpenAI format
        var messages = chatMessages.Select(m => new
        {
            role = m.Role.Value.ToLowerInvariant(),
            content = m.Text
        }).ToList();

        var requestBody = new
        {
            model = _modelName,
            messages = messages,
            temperature = options?.Temperature ?? 0.7,
            max_tokens = options?.MaxOutputTokens ?? 1000
        };

        var jsonContent = System.Text.Json.JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(_endpoint, httpContent, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"LLM request failed: {response.StatusCode} - {responseContent}");
        }

        // Parse response
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;

        var content = root.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        var chatResponse = new ChatResponse(
            new ChatMessage(ChatRole.Assistant, content)
        );

        return chatResponse;
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // For simplicity, use non-streaming completion
        var response = await GetResponseAsync(chatMessages, options, cancellationToken);

        // Extract text from the response
        var text = response.Messages?.FirstOrDefault()?.Text ?? string.Empty;

        var update = new ChatResponseUpdate();
        update.Contents.Add(new TextContent(text));
        yield return update;
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        return null;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
