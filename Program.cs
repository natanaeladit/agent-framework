using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

// Configuration for your local LLM
// Update these values to match your local LLM setup
var localLlmEndpoint = Environment.GetEnvironmentVariable("LOCAL_LLM_ENDPOINT"); // Change this to your local LLM endpoint
var modelName = Environment.GetEnvironmentVariable("LOCAL_LLM_MODEL_NAME"); // Change this to your model name
var apiKey = Environment.GetEnvironmentVariable("LOCAL_LLM_API_KEY"); // Most local LLMs don't require an API key

ArgumentNullException.ThrowIfNull(localLlmEndpoint);
ArgumentNullException.ThrowIfNull(modelName);
ArgumentNullException.ThrowIfNull(apiKey);

Console.WriteLine("=== Microsoft Agent Framework - Local LLM Chat ===");
Console.WriteLine($"Connecting to: {localLlmEndpoint}");
Console.WriteLine($"Model: {modelName}");
Console.WriteLine("Type 'exit' or 'quit' to end the conversation.\n");

// Create a custom chat client for local LLM
var chatClient = new LocalLLMChatClient(localLlmEndpoint, modelName, apiKey);

// Create a ChatClientAgent
var agent = new ChatClientAgent(
    chatClient: chatClient,
    instructions: "You are a helpful AI assistant. Be concise and friendly in your responses.",
    name: "LocalAssistant"
);

// Main chat loop
while (true)
{
    // Get user input
    Console.Write("You: ");
    var userInput = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(userInput))
    {
        continue;
    }

    // Check for exit commands
    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        userInput.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Goodbye!");
        break;
    }

    try
    {
        // Get response from the agent
        Console.Write("Assistant: ");

        var response = await agent.RunAsync(userInput);

        Console.WriteLine(response);
        Console.WriteLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\nError: {ex.Message}");
        Console.WriteLine($"Details: {ex.InnerException?.Message}");
        Console.WriteLine("Please check your local LLM is running and the configuration is correct.\n");
    }
}

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
