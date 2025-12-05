# Microsoft Agent Framework - Local LLM Chat

A simple chat-based application using the Microsoft Agent Framework that connects to your locally running Large Language Model (LLM).

## Overview

This application demonstrates how to:
- Create a custom `IChatClient` implementation for local LLMs
- Use the `ChatClientAgent` from Microsoft Agent Framework
- Build an interactive chat interface
- Instrument your agent with OpenTelemetry for observability

## Prerequisites

- .NET 10.0 SDK
- A locally running LLM with an OpenAI-compatible API endpoint

## Supported Local LLM Servers

This application works with any LLM server that provides an OpenAI-compatible API. Common options include:

### Ollama
- Endpoint: `http://localhost:11434/v1/chat/completions`
- Models: llama3, mistral, phi, gemma, etc.
- Install: https://ollama.ai

### LM Studio
- Endpoint: `http://localhost:1234/v1/chat/completions`
- Supports many models with a GUI interface
- Download: https://lmstudio.ai

### LocalAI
- Endpoint: `http://localhost:8080/v1/chat/completions`
- Self-hosted OpenAI alternative
- Repo: https://github.com/mudler/LocalAI

### Text Generation WebUI (oobabooga)
- Endpoint: `http://localhost:5000/v1/chat/completions`
- Web interface for running LLMs
- Repo: https://github.com/oobabooga/text-generation-webui

## Features

✨ **Full OpenTelemetry Integration** - Following the [official Microsoft Agent Framework pattern](https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/GettingStarted/AgentOpenTelemetry/Program.cs):
- Distributed tracing with spans for sessions and interactions
- Custom metrics (interaction counts, response times)
- Structured logging with correlation
- Compatible with Aspire Dashboard and Azure Application Insights

## Configuration

Before running, set these environment variables:

```bash
# Windows PowerShell
$env:LOCAL_LLM_ENDPOINT = "http://localhost:11434/v1/chat/completions"
$env:LOCAL_LLM_MODEL_NAME = "llama3"
$env:LOCAL_LLM_API_KEY = "not-needed"

# Optional: OpenTelemetry endpoint (default: http://localhost:4318)
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4318"
```

### Example Configurations

**For Ollama:**
```bash
$env:LOCAL_LLM_ENDPOINT = "http://localhost:11434/v1/chat/completions"
$env:LOCAL_LLM_MODEL_NAME = "llama3"
$env:LOCAL_LLM_API_KEY = "not-needed"
```

**For LM Studio:**
```bash
$env:LOCAL_LLM_ENDPOINT = "http://localhost:1234/v1/chat/completions"
$env:LOCAL_LLM_MODEL_NAME = "your-model-name"
$env:LOCAL_LLM_API_KEY = "not-needed"
```

## Running the Application

### Basic Run (Without Observability)

1. Make sure your local LLM server is running
2. Set the environment variables (see Configuration above)
3. Run the application:

```bash
dotnet run
```

### With OpenTelemetry & Aspire Dashboard (Recommended)

1. **Start Aspire Dashboard:**
   ```bash
   docker run --rm -it -p 18888:18888 -p 4317:18889 -d --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:latest
   ```

2. **Set environment variables** (see Configuration above)

3. **Run the application:**
   ```bash
   dotnet run
   ```

4. **View telemetry** at http://localhost:18888

For detailed instructions, see [OPENTELEMETRY_GUIDE.md](OPENTELEMETRY_GUIDE.md)

## Usage

Once running, you can:
- Type your messages and press Enter to chat with the AI
- Type `exit` or `quit` to end the conversation

Example session:
```
=== Microsoft Agent Framework - Local LLM Chat ===
This demo shows OpenTelemetry integration with a local LLM.
You can view the telemetry data in the Aspire Dashboard.
Type your message and press Enter. Type 'exit' or empty message to quit.

Connecting to: http://localhost:11434/v1/chat/completions
Model: llama3

Trace ID: 4bf92f3577b34da6a3ce929d0e0e4736

You (or 'exit' to quit): Hello! Can you help me with coding?
Agent: Of course! I'd be happy to help you with coding. What language or project are you working on?

You (or 'exit' to quit): exit
Goodbye!
```

## How It Works

### Custom IChatClient Implementation

The `LocalLLMChatClient` class implements the `Microsoft.Extensions.AI.IChatClient` interface to communicate with your local LLM:

1. **GetResponseAsync**: Sends messages to your LLM and returns the response
2. **GetStreamingResponseAsync**: Provides streaming responses (currently uses non-streaming internally)
3. Formats requests in OpenAI's API format
4. Parses responses from the LLM

### ChatClientAgent with OpenTelemetry

The application uses the builder pattern to add instrumentation:

```csharp
var agent = new ChatClientAgent(instrumentedChatClient,
    name: "LocalLLMAgent",
    instructions: "You are a helpful AI assistant.")
    .AsBuilder()
    .UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
    .Build();
```

This provides:
- Automatic span creation for agent operations
- Thread-based conversation management
- Distributed tracing correlation
- Custom metrics and structured logging

## Dependencies

### Core Framework
- `Microsoft.Agents.AI` (v1.0.0-preview.251114.1) - Core agent framework
  - Includes `Microsoft.Extensions.AI` - AI abstractions and IChatClient interface
  - Includes transitive OpenTelemetry dependencies

### OpenTelemetry
- `OpenTelemetry` - Core OpenTelemetry SDK
- `OpenTelemetry.Exporter.OpenTelemetryProtocol` - OTLP exporter for Aspire/Jaeger
- `OpenTelemetry.Instrumentation.Http` - HTTP client instrumentation
- `OpenTelemetry.Instrumentation.Runtime` - .NET runtime metrics

All dependencies are automatically installed when you build the project.

## Troubleshooting

### Connection Errors

If you see connection errors:
1. Verify your LLM server is running
2. Check the endpoint URL is correct
3. Ensure the port is accessible (check firewall settings)

### Model Not Found

If you get model-related errors:
1. Verify the model name matches exactly what your LLM server expects
2. For Ollama, use `ollama list` to see available models
3. Check your LLM server's documentation for model naming conventions

### API Format Issues

Some LLM servers might have slight variations in their API:
- Check your server's documentation for the exact endpoint path
- Some servers use `/v1/chat/completions`, others might use different paths
- Verify the expected request/response format

## Additional Resources

- 📖 [OpenTelemetry Integration Guide](OPENTELEMETRY_GUIDE.md) - Complete guide for observability
- 📖 [Local LLM Setup Guide](LOCAL_LLM_SETUP.md) - Detailed setup for various LLM servers
- 📖 [Example Configurations](EXAMPLE_CONFIGS.md) - Quick copy-paste configs

## Learn More

- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview)
- [Agent Framework OpenTelemetry Sample](https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/GettingStarted/AgentOpenTelemetry/Program.cs)
- [Agent Framework on GitHub](https://github.com/microsoft/agent-framework)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)

## License

This project follows the same license as the Microsoft Agent Framework.

