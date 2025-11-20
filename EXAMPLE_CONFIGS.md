# Example Configurations

Quick copy-paste configurations for popular local LLM setups. Just replace the configuration section in `Program.cs` with one of these.

## Ollama Configurations

### Ollama - Llama 3
```csharp
var localLlmEndpoint = "http://localhost:11434/v1/chat/completions";
var modelName = "llama3";
var apiKey = "not-needed";
```

### Ollama - Mistral
```csharp
var localLlmEndpoint = "http://localhost:11434/v1/chat/completions";
var modelName = "mistral";
var apiKey = "not-needed";
```

### Ollama - Phi-3
```csharp
var localLlmEndpoint = "http://localhost:11434/v1/chat/completions";
var modelName = "phi3";
var apiKey = "not-needed";
```

### Ollama - CodeLlama (for coding)
```csharp
var localLlmEndpoint = "http://localhost:11434/v1/chat/completions";
var modelName = "codellama";
var apiKey = "not-needed";
```

### Ollama - Custom Port
```csharp
var localLlmEndpoint = "http://localhost:11434/v1/chat/completions";
var modelName = "llama3";
var apiKey = "not-needed";
```

---

## LM Studio Configurations

### LM Studio - Default Setup
```csharp
var localLlmEndpoint = "http://localhost:1234/v1/chat/completions";
var modelName = "local-model"; // Check LM Studio for exact name
var apiKey = "lm-studio";
```

### LM Studio - With Specific Model
```csharp
var localLlmEndpoint = "http://localhost:1234/v1/chat/completions";
var modelName = "TheBloke/Mistral-7B-Instruct-v0.2-GGUF";
var apiKey = "lm-studio";
```

---

## LocalAI Configurations

### LocalAI - Default
```csharp
var localLlmEndpoint = "http://localhost:8080/v1/chat/completions";
var modelName = "mistral-7b-instruct";
var apiKey = "not-needed";
```

### LocalAI - Custom Model
```csharp
var localLlmEndpoint = "http://localhost:8080/v1/chat/completions";
var modelName = "your-model-name";
var apiKey = "not-needed";
```

---

## Text Generation WebUI (oobabooga)

### Default Configuration
```csharp
var localLlmEndpoint = "http://localhost:5000/v1/chat/completions";
var modelName = "your-model-name";
var apiKey = "not-needed";
```

---

## Remote LLM Servers

### LM Studio on Another Computer
```csharp
var localLlmEndpoint = "http://192.168.1.100:1234/v1/chat/completions"; // Replace with actual IP
var modelName = "local-model";
var apiKey = "lm-studio";
```

### Ollama on Another Computer
```csharp
var localLlmEndpoint = "http://192.168.1.100:11434/v1/chat/completions"; // Replace with actual IP
var modelName = "llama3";
var apiKey = "not-needed";
```

---

## Cloud/Hosted LLMs (OpenAI Compatible)

### OpenRouter
```csharp
var localLlmEndpoint = "https://openrouter.ai/api/v1/chat/completions";
var modelName = "meta-llama/llama-3-8b-instruct";
var apiKey = "your-openrouter-api-key"; // Get from https://openrouter.ai
```

### Together AI
```csharp
var localLlmEndpoint = "https://api.together.xyz/v1/chat/completions";
var modelName = "meta-llama/Llama-3-8b-chat-hf";
var apiKey = "your-together-api-key"; // Get from https://together.ai
```

### Groq (Very Fast Inference)
```csharp
var localLlmEndpoint = "https://api.groq.com/openai/v1/chat/completions";
var modelName = "llama3-8b-8192";
var apiKey = "your-groq-api-key"; // Get from https://console.groq.com
```

---

## Custom Agent Instructions

You can also customize the agent's behavior by modifying the instructions:

### Helpful Assistant (Default)
```csharp
var agent = new ChatClientAgent(
    chatClient: chatClient,
    instructions: "You are a helpful AI assistant. Be concise and friendly in your responses.",
    name: "LocalAssistant"
);
```

### Coding Assistant
```csharp
var agent = new ChatClientAgent(
    chatClient: chatClient,
    instructions: "You are an expert programming assistant. Provide clear, well-commented code examples. Explain your reasoning and suggest best practices.",
    name: "CodingAssistant"
);
```

### Technical Writer
```csharp
var agent = new ChatClientAgent(
    chatClient: chatClient,
    instructions: "You are a technical documentation expert. Write clear, structured documentation with examples. Use proper formatting and explain technical concepts simply.",
    name: "TechWriter"
);
```

### Debugging Helper
```csharp
var agent = new ChatClientAgent(
    chatClient: chatClient,
    instructions: "You are a debugging expert. Analyze code carefully, identify potential issues, suggest fixes, and explain why bugs occur.",
    name: "DebugHelper"
);
```

### Teacher/Tutor
```csharp
var agent = new ChatClientAgent(
    chatClient: chatClient,
    instructions: "You are a patient teacher. Explain concepts step-by-step, use analogies, provide examples, and check for understanding.",
    name: "Tutor"
);
```

---

## Advanced Options

### With Custom Temperature (More Creative)
```csharp
// Note: Temperature is passed via ChatOptions in the CompleteAsync method
// To customize per request, modify the LocalLLMChatClient implementation
```

### With Custom System Message
The `instructions` parameter in `ChatClientAgent` acts as the system message.

### Multiple Agents with Different Models
```csharp
// Fast model for simple tasks
var fastClient = new LocalLLMChatClient("http://localhost:11434/v1/chat/completions", "phi3", "not-needed");
var fastAgent = new ChatClientAgent(fastClient, "Quick responder", "FastBot");

// Powerful model for complex tasks
var powerfulClient = new LocalLLMChatClient("http://localhost:11434/v1/chat/completions", "llama3:70b", "not-needed");
var powerfulAgent = new ChatClientAgent(powerfulClient, "Deep thinker", "SmartBot");
```

---

## Verifying Your Configuration

Before running the full app, you can test your endpoint with curl:

### Windows PowerShell
```powershell
$body = @{
    model = "llama3"
    messages = @(
        @{
            role = "user"
            content = "Say hello!"
        }
    )
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:11434/v1/chat/completions" `
    -Method Post `
    -ContentType "application/json" `
    -Body $body
```

### Windows Command Prompt
```cmd
curl http://localhost:11434/v1/chat/completions ^
  -H "Content-Type: application/json" ^
  -d "{\"model\": \"llama3\", \"messages\": [{\"role\": \"user\", \"content\": \"Hello!\"}]}"
```

If you get a JSON response with a message, your configuration is correct!

---

## Tips

1. **Start with Ollama and Phi-3** for the easiest setup and lowest resource requirements
2. **Use LM Studio** if you prefer a GUI and want to experiment with different models
3. **Monitor resource usage** - smaller models (7B and below) work better on limited hardware
4. **Experiment with instructions** - different system prompts can dramatically change behavior
5. **Check model names** - they must match exactly what your LLM server expects
6. **Use localhost vs 127.0.0.1** - try both if one doesn't work

---

For more detailed setup instructions, see [LOCAL_LLM_SETUP.md](LOCAL_LLM_SETUP.md).

