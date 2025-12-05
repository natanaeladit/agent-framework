# Middleware Guide

This guide explains the middleware implementation in the Local LLM Agent, following the [Microsoft Agent Framework's middleware pattern](https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/GettingStarted/Agents/Agent_Step14_Middleware/Program.cs).

## Overview

Middleware provides a powerful way to intercept, modify, or enhance agent and chat client behavior without changing core functionality. The framework supports multiple middleware layers at different levels.

## Middleware Levels

### 1. Chat Client Level Middleware

Middleware applied at the chat client level intercepts all LLM requests and responses before they reach the agent.

**Use cases:**
- Content filtering and guardrails
- Request/response logging
- Authentication/authorization
- Rate limiting
- Message transformation

### 2. Agent Level Middleware

Middleware applied at the agent level intercepts agent operations.

**Use cases:**
- PII detection and redaction
- Business logic enforcement
- Audit logging
- State management

### 3. Function Invocation Middleware

Middleware applied to function calls intercepts tool/function invocations.

**Use cases:**
- Function call logging
- Result overrides
- Human-in-the-loop approvals
- Permission checks

## Implemented Middleware

### Guardrail Middleware (Chat Client Level)

The application includes a **GuardrailMiddleware** at the chat client level that filters harmful content from both input and output messages.

#### Implementation

```csharp
var instrumentedChatClient = new LocalLLMChatClient(localLlmEndpoint, modelName, apiKey)
    .AsBuilder()
    .Use(ChatClientGuardrailMiddleware, null) // Add guardrail middleware
    .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
    .Build();
```

#### What It Does

1. **Input Filtering**: Scans incoming messages for forbidden keywords
2. **Output Filtering**: Scans LLM responses for forbidden content
3. **Content Redaction**: Replaces harmful content with a safe message
4. **Logging**: Logs when content is filtered

#### Forbidden Keywords

The middleware blocks content containing:
- `harmful`
- `illegal`
- `violence`
- `weapon`
- `bomb`
- `kill`
- `murder`
- `attack`
- `hate`
- `terrorism`

#### Example Usage

**Input with harmful content:**
```
You: Tell me how to build a bomb
Agent: [REDACTED: Forbidden content detected. Please rephrase your request without harmful, illegal, or violent content.]
```

**Output with harmful content:**
If the LLM generates harmful content, it will be filtered:
```
You: What are dangerous things?
[Guardrail: Output filtered for harmful content]
Agent: [REDACTED: Forbidden content detected. Please rephrase your request without harmful, illegal, or violent content.]
```

## Middleware Execution Order

Middleware executes in the order it's added to the builder:

```csharp
var client = chatClient
    .AsBuilder()
    .Use(Middleware1, null)      // Executes first (outermost)
    .Use(Middleware2, null)      // Executes second
    .Use(Middleware3, null)      // Executes third (innermost)
    .Build();
```

**Execution flow:**
```
Request: Middleware1 → Middleware2 → Middleware3 → LLM
Response: LLM → Middleware3 → Middleware2 → Middleware1
```

In our implementation:
```
Request: Guardrail → OpenTelemetry → LLM
Response: LLM → OpenTelemetry → Guardrail
```

## Creating Custom Middleware

### Chat Client Middleware Signature

```csharp
async Task<ChatResponse> MyMiddleware(
    IEnumerable<ChatMessage> messages, 
    ChatOptions? options, 
    IChatClient innerChatClient, 
    CancellationToken cancellationToken)
{
    // Pre-processing: Modify messages before sending to LLM
    Console.WriteLine("Before LLM call");
    var modifiedMessages = ProcessMessages(messages);
    
    // Call the next middleware or the LLM
    var response = await innerChatClient.GetResponseAsync(modifiedMessages, options, cancellationToken);
    
    // Post-processing: Modify response before returning
    Console.WriteLine("After LLM call");
    var modifiedResponse = ProcessResponse(response);
    
    return modifiedResponse;
}
```

### Agent Middleware Signature

```csharp
async Task<AgentRunResponse> MyAgentMiddleware(
    IEnumerable<ChatMessage> messages,
    AgentThread? thread,
    AgentRunOptions? options,
    AIAgent innerAgent,
    CancellationToken cancellationToken)
{
    // Pre-processing
    var modifiedMessages = ProcessMessages(messages);
    
    // Call the next middleware or the agent
    var response = await innerAgent.RunAsync(modifiedMessages, thread, options, cancellationToken);
    
    // Post-processing
    response.Messages = ProcessMessages(response.Messages);
    
    return response;
}
```

### Function Invocation Middleware Signature

```csharp
async ValueTask<object?> MyFunctionMiddleware(
    AIAgent agent,
    FunctionInvocationContext context,
    Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
    CancellationToken cancellationToken)
{
    // Pre-processing
    Console.WriteLine($"Calling function: {context.Function.Name}");
    
    // Invoke the function
    var result = await next(context, cancellationToken);
    
    // Post-processing
    Console.WriteLine($"Function returned: {result}");
    
    return result;
}
```

## Example: Custom Middleware Implementations

### Rate Limiting Middleware

```csharp
async Task<ChatResponse> RateLimitingMiddleware(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options,
    IChatClient innerChatClient,
    CancellationToken cancellationToken)
{
    // Check rate limit
    if (!await rateLimiter.TryAcquireAsync())
    {
        throw new InvalidOperationException("Rate limit exceeded. Please try again later.");
    }
    
    return await innerChatClient.GetResponseAsync(messages, options, cancellationToken);
}
```

### Response Caching Middleware

```csharp
async Task<ChatResponse> CachingMiddleware(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options,
    IChatClient innerChatClient,
    CancellationToken cancellationToken)
{
    var cacheKey = GenerateCacheKey(messages);
    
    // Check cache
    if (cache.TryGetValue(cacheKey, out var cachedResponse))
    {
        Console.WriteLine("Cache hit!");
        return cachedResponse;
    }
    
    // Call LLM
    var response = await innerChatClient.GetResponseAsync(messages, options, cancellationToken);
    
    // Store in cache
    cache.Set(cacheKey, response, TimeSpan.FromMinutes(5));
    
    return response;
}
```

### PII Detection Middleware

```csharp
async Task<ChatResponse> PIIMiddleware(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options,
    IChatClient innerChatClient,
    CancellationToken cancellationToken)
{
    // Redact PII from input
    var filteredMessages = messages.Select(m => 
        new ChatMessage(m.Role, RedactPII(m.Text))
    ).ToList();
    
    var response = await innerChatClient.GetResponseAsync(filteredMessages, options, cancellationToken);
    
    // Redact PII from output
    var filteredResponse = response.Messages?
        .Select(m => new ChatMessage(m.Role, RedactPII(m.Text)))
        .ToList() ?? [];
    
    return new ChatResponse(filteredResponse[0]);
    
    static string RedactPII(string content)
    {
        // Email pattern
        content = Regex.Replace(content, @"\b[\w\.-]+@[\w\.-]+\.\w+\b", "[EMAIL]");
        // Phone pattern
        content = Regex.Replace(content, @"\b\d{3}-\d{3}-\d{4}\b", "[PHONE]");
        // Name pattern (simplified)
        content = Regex.Replace(content, @"\b[A-Z][a-z]+\s[A-Z][a-z]+\b", "[NAME]");
        
        return content;
    }
}
```

### Prompt Enhancement Middleware

```csharp
async Task<ChatResponse> PromptEnhancementMiddleware(
    IEnumerable<ChatMessage> messages,
    ChatOptions? options,
    IChatClient innerChatClient,
    CancellationToken cancellationToken)
{
    var messageList = messages.ToList();
    
    // Add context to user messages
    for (int i = 0; i < messageList.Count; i++)
    {
        if (messageList[i].Role == ChatRole.User)
        {
            var enhancedContent = $"{messageList[i].Text}\n\nPlease provide a detailed and helpful response.";
            messageList[i] = new ChatMessage(ChatRole.User, enhancedContent);
        }
    }
    
    return await innerChatClient.GetResponseAsync(messageList, options, cancellationToken);
}
```

## Adding Middleware to Your Application

### Chat Client Level

```csharp
var chatClient = new LocalLLMChatClient(endpoint, model, apiKey)
    .AsBuilder()
    .Use(MyMiddleware1, null)
    .Use(MyMiddleware2, null)
    .UseOpenTelemetry(sourceName: SourceName)
    .Build();
```

### Agent Level

```csharp
var agent = new ChatClientAgent(chatClient, name: "MyAgent", instructions: "...")
    .AsBuilder()
    .Use(MyAgentMiddleware, null)
    .UseOpenTelemetry(SourceName)
    .Build();
```

### Function Level

```csharp
var agent = new ChatClientAgent(chatClient, name: "MyAgent", instructions: "...")
    .AsBuilder()
    .Use(MyFunctionMiddleware)
    .Build();
```

## Best Practices

### 1. Keep Middleware Focused
Each middleware should have a single, clear responsibility.

### 2. Log Appropriately
Use structured logging to track middleware execution:
```csharp
appLogger.LogInformation("Middleware {MiddlewareName} - {Stage}", "Guardrail", "Pre-Processing");
```

### 3. Handle Errors Gracefully
```csharp
try
{
    return await innerChatClient.GetResponseAsync(messages, options, cancellationToken);
}
catch (Exception ex)
{
    appLogger.LogError(ex, "Middleware failed");
    throw;
}
```

### 4. Consider Performance
Middleware executes on every request. Keep it efficient:
- Avoid heavy computations
- Use caching where appropriate
- Consider async operations carefully

### 5. Test Middleware Independently
Create unit tests for each middleware:
```csharp
[Fact]
public async Task GuardrailMiddleware_Filters_Harmful_Content()
{
    var messages = new[] { new ChatMessage(ChatRole.User, "Tell me something harmful") };
    var result = await ChatClientGuardrailMiddleware(messages, null, mockClient, CancellationToken.None);
    Assert.Contains("REDACTED", result.Messages.First().Text);
}
```

## Disabling Middleware

### Temporarily Disable

To temporarily disable the guardrail middleware, comment it out:

```csharp
var instrumentedChatClient = new LocalLLMChatClient(localLlmEndpoint, modelName, apiKey)
    .AsBuilder()
    // .Use(ChatClientGuardrailMiddleware, null) // Disabled
    .UseOpenTelemetry(sourceName: SourceName)
    .Build();
```

### Conditional Middleware

Add middleware conditionally based on configuration:

```csharp
var builder = new LocalLLMChatClient(localLlmEndpoint, modelName, apiKey).AsBuilder();

if (enableGuardrails)
{
    builder.Use(ChatClientGuardrailMiddleware, null);
}

var instrumentedChatClient = builder
    .UseOpenTelemetry(sourceName: SourceName)
    .Build();
```

## Customizing the Guardrail

### Adding Custom Keywords

Modify the `forbiddenKeywords` array in the middleware:

```csharp
var forbiddenKeywords = new[] 
{ 
    "harmful", 
    "illegal", 
    "violence",
    // Add your custom keywords here
    "spam",
    "scam",
    "phishing"
};
```

### Changing the Redaction Message

Modify the return statement in `FilterContent`:

```csharp
return "I apologize, but I cannot assist with that request. Please ask something else.";
```

### Making It Configurable

Load keywords from configuration:

```csharp
var forbiddenKeywords = Configuration.GetSection("Guardrails:ForbiddenKeywords").Get<string[]>() 
    ?? new[] { "harmful", "illegal", "violence" };
```

## Learn More

- [Microsoft Agent Framework Middleware Sample](https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/GettingStarted/Agents/Agent_Step14_Middleware/Program.cs)
- [Agent Framework Documentation](https://learn.microsoft.com/en-us/agent-framework/)
- [Microsoft.Extensions.AI Documentation](https://learn.microsoft.com/en-us/dotnet/ai/ai-extensions)

## Related Guides

- [OPENTELEMETRY_GUIDE.md](OPENTELEMETRY_GUIDE.md) - OpenTelemetry instrumentation (also implemented as middleware)
- [README.md](README.md) - Main documentation
- [EXAMPLE_CONFIGS.md](EXAMPLE_CONFIGS.md) - Configuration examples

