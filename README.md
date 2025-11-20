# Microsoft Agent Framework - Local LLM Chat

A simple chat-based application using the Microsoft Agent Framework that connects to your locally running Large Language Model (LLM).

## Overview

This application demonstrates how to:
- Create a custom `IChatClient` implementation for local LLMs
- Use the `ChatClientAgent` from Microsoft Agent Framework
- Build an interactive chat interface

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

## Configuration

Before running, update these variables in `Program.cs`:

```csharp
var localLlmEndpoint = "http://localhost:1234/v1/chat/completions"; // Your LLM endpoint
var modelName = "local-model"; // Your model name
var apiKey = "not-needed"; // Most local LLMs don't require a real API key
```

### Example Configurations

**For Ollama:**
```csharp
var localLlmEndpoint = "http://localhost:11434/v1/chat/completions";
var modelName = "llama3";
var apiKey = "not-needed";
```

**For LM Studio:**
```csharp
var localLlmEndpoint = "http://localhost:1234/v1/chat/completions";
var modelName = "your-model-name"; // Check LM Studio for the exact model name
var apiKey = "not-needed";
```

## Running the Application

1. Make sure your local LLM server is running
2. Update the configuration in `Program.cs`
3. Run the application:

```bash
dotnet run
```

## Usage

Once running, you can:
- Type your messages and press Enter to chat with the AI
- Type `exit` or `quit` to end the conversation

Example session:
```
=== Microsoft Agent Framework - Local LLM Chat ===
Connecting to: http://localhost:11434/v1/chat/completions
Model: llama3
Type 'exit' or 'quit' to end the conversation.

You: Hello! Can you help me with coding?
Assistant: Of course! I'd be happy to help you with coding. What language or project are you working on?

You: exit
Goodbye!
```

## How It Works

### Custom IChatClient Implementation

The `LocalLLMChatClient` class implements the `Microsoft.Extensions.AI.IChatClient` interface to communicate with your local LLM:

1. **GetResponseAsync**: Sends messages to your LLM and returns the response
2. **GetStreamingResponseAsync**: Provides streaming responses (currently uses non-streaming internally)
3. Formats requests in OpenAI's API format
4. Parses responses from the LLM

### ChatClientAgent

The Microsoft Agent Framework's `ChatClientAgent` handles:
- Conversation flow
- System instructions
- Message management
- Agent orchestration

## Dependencies

- `Microsoft.Agents.AI` (v1.0.0-preview.251114.1) - Core agent framework

This single package includes all necessary dependencies:
- `Microsoft.Extensions.AI` - AI abstractions and IChatClient interface
- `Microsoft.Extensions.AI.OpenAI` - OpenAI extensions
- Other required transitive dependencies

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

## Learn More

- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/en-us/agent-framework/overview/agent-framework-overview)
- [Agent Framework on GitHub](https://github.com/microsoft/agent-framework)
- [Agent Framework Samples](https://github.com/microsoft/Agent-Framework-Samples)

## License

This project follows the same license as the Microsoft Agent Framework.

