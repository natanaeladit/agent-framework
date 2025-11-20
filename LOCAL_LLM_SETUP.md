# Setting Up Local LLM Servers

This guide will help you set up a local LLM server to use with the Microsoft Agent Framework chat application.

## Option 1: Ollama (Recommended for Beginners)

Ollama is the easiest way to run LLMs locally with a simple command-line interface.

### Installation

1. **Download Ollama**
   - Visit: https://ollama.ai
   - Download the installer for Windows
   - Run the installer

2. **Install a Model**
   ```bash
   ollama pull llama3
   ```
   
   Other popular models:
   ```bash
   ollama pull mistral      # Fast, good for general use
   ollama pull phi          # Small, efficient
   ollama pull codellama    # Specialized for coding
   ollama pull gemma        # Google's model
   ```

3. **Run Ollama Server**
   ```bash
   ollama serve
   ```
   The server will start on `http://localhost:11434`

### Configuration for This App

```csharp
var localLlmEndpoint = "http://localhost:11434/v1/chat/completions";
var modelName = "llama3";  // or "mistral", "phi", etc.
var apiKey = "not-needed";
```

---

## Option 2: LM Studio (Recommended for GUI Users)

LM Studio provides a user-friendly graphical interface to download and run LLMs.

### Installation

1. **Download LM Studio**
   - Visit: https://lmstudio.ai
   - Download for Windows
   - Install and run

2. **Download a Model**
   - Open LM Studio
   - Click on the "Discover" tab
   - Search for models (e.g., "Llama-3", "Mistral", "Phi-3")
   - Click download on your preferred model

3. **Start the Server**
   - Click on the "Local Server" tab
   - Select your downloaded model
   - Click "Start Server"
   - Server will run on `http://localhost:1234`

### Configuration for This App

```csharp
var localLlmEndpoint = "http://localhost:1234/v1/chat/completions";
var modelName = "your-model-name";  // Check LM Studio for the exact name
var apiKey = "not-needed";
```

---

## Option 3: LocalAI (For Advanced Users)

LocalAI is a drop-in replacement for OpenAI API that runs locally.

### Installation with Docker

1. **Install Docker Desktop**
   - Visit: https://www.docker.com/products/docker-desktop
   - Install for Windows

2. **Run LocalAI**
   ```bash
   docker run -p 8080:8080 --name local-ai -ti localai/localai:latest
   ```

3. **Download a Model**
   ```bash
   curl http://localhost:8080/models/apply -H "Content-Type: application/json" -d '{
     "id": "TheBloke/Mistral-7B-Instruct-v0.2-GGUF"
   }'
   ```

### Configuration for This App

```csharp
var localLlmEndpoint = "http://localhost:8080/v1/chat/completions";
var modelName = "mistral-7b-instruct";
var apiKey = "not-needed";
```

---

## Option 4: Text Generation WebUI (oobabooga)

A feature-rich web interface for running LLMs with many customization options.

### Installation

1. **Install Prerequisites**
   - Python 3.10 or 3.11
   - Git

2. **Clone and Install**
   ```bash
   git clone https://github.com/oobabooga/text-generation-webui
   cd text-generation-webui
   start_windows.bat
   ```

3. **Download a Model**
   - Open the web interface (usually `http://localhost:7860`)
   - Go to the "Model" tab
   - Download a model from HuggingFace

4. **Enable OpenAI API Extension**
   - Go to "Session" tab
   - Enable "openai" extension
   - The API will be available at `http://localhost:5000`

### Configuration for This App

```csharp
var localLlmEndpoint = "http://localhost:5000/v1/chat/completions";
var modelName = "your-model-name";
var apiKey = "not-needed";
```

---

## Recommended Models by Use Case

### For General Chat
- **Llama 3** (8B or 70B): Excellent all-around performance
- **Mistral** (7B): Fast and efficient
- **Phi-3** (3.8B): Very small, surprisingly capable

### For Coding
- **CodeLlama** (7B, 13B, or 34B): Specialized for code
- **DeepSeek Coder**: Great for code generation
- **StarCoder**: Another excellent coding model

### For Resource-Constrained Systems
- **Phi-3 Mini** (3.8B): Very small, runs on most hardware
- **TinyLlama** (1.1B): Extremely lightweight
- **Gemma** (2B): Small but capable

### For Best Quality (Requires Good GPU)
- **Llama 3 70B**: Top-tier performance
- **Mixtral 8x7B**: Mixture of experts model
- **Yi 34B**: Excellent reasoning

---

## System Requirements

### Minimum
- **RAM**: 8GB (for small models like Phi-3 or TinyLlama)
- **Storage**: 10GB free space
- **GPU**: Optional, but recommended

### Recommended
- **RAM**: 16GB or more
- **Storage**: 50GB+ free space
- **GPU**: NVIDIA GPU with 6GB+ VRAM (for faster inference)

### For Large Models (70B+)
- **RAM**: 64GB+
- **GPU**: NVIDIA GPU with 24GB+ VRAM or Apple Silicon with 64GB+ unified memory
- **Storage**: 100GB+ free space

---

## Testing Your Setup

Once your LLM server is running, you can test it with curl:

```bash
curl http://localhost:11434/v1/chat/completions ^
  -H "Content-Type: application/json" ^
  -d "{\"model\": \"llama3\", \"messages\": [{\"role\": \"user\", \"content\": \"Hello!\"}]}"
```

(Adjust the URL and model name based on your setup)

If you get a JSON response with a message, your server is ready!

---

## Troubleshooting

### Server Won't Start
- Check if another application is using the port
- Try running with administrator privileges
- Check firewall settings

### Model Loading Errors
- Ensure you have enough RAM
- Try a smaller model
- Check the model is fully downloaded

### Slow Responses
- Use a smaller model
- Enable GPU acceleration if available
- Reduce context window size
- Close other applications to free up resources

### Connection Refused
- Make sure the server is running
- Check the port number is correct
- Try using `127.0.0.1` instead of `localhost`

---

## Next Steps

Once your local LLM is set up and running:

1. Update the configuration in `Program.cs`
2. Run the application: `dotnet run`
3. Start chatting!

For more information about the Agent Framework, see the main [README.md](README.md).

