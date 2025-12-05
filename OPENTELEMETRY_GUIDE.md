# OpenTelemetry Integration Guide

This guide explains how to run and monitor your Local LLM Agent with OpenTelemetry and the Aspire Dashboard.

## Overview

The application now includes full OpenTelemetry instrumentation following the [Microsoft Agent Framework's official pattern](https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/GettingStarted/AgentOpenTelemetry/Program.cs), providing:

- **Distributed Tracing**: Track requests across your agent, chat client, and LLM
- **Metrics**: Monitor performance, interactions, and response times
- **Structured Logging**: View detailed logs with context and correlation

## Quick Start

### 1. Set Up Aspire Dashboard (Recommended)

The Aspire Dashboard provides a beautiful UI to view your telemetry data.

**Install .NET Aspire:**
```bash
dotnet workload install aspire
```

**Run the Aspire Dashboard:**
```bash
docker run --rm -it -p 18888:18888 -p 4317:18889 -d --name aspire-dashboard mcr.microsoft.com/dotnet/aspire-dashboard:latest
```

Or if you have Aspire installed locally:
```bash
dotnet run --project $(dotnet new list | grep "Aspire" | awk '{print $1}')
```

The dashboard will be available at: **http://localhost:18888**

### 2. Configure Environment Variables

**Required for Local LLM:**
```bash
# Windows PowerShell
$env:LOCAL_LLM_ENDPOINT = "http://localhost:11434/v1/chat/completions"
$env:LOCAL_LLM_MODEL_NAME = "llama3"
$env:LOCAL_LLM_API_KEY = "not-needed"

# Windows Command Prompt
set LOCAL_LLM_ENDPOINT=http://localhost:11434/v1/chat/completions
set LOCAL_LLM_MODEL_NAME=llama3
set LOCAL_LLM_API_KEY=not-needed

# Linux/macOS
export LOCAL_LLM_ENDPOINT="http://localhost:11434/v1/chat/completions"
export LOCAL_LLM_MODEL_NAME="llama3"
export LOCAL_LLM_API_KEY="not-needed"
```

**Optional OpenTelemetry Configuration:**
```bash
# Default is http://localhost:4318
$env:OTEL_EXPORTER_OTLP_ENDPOINT = "http://localhost:4318"
```

### 3. Run Your Application

```bash
dotnet run
```

### 4. View Telemetry

Open the Aspire Dashboard at **http://localhost:18888** and explore:
- **Traces**: See the full execution flow
- **Metrics**: Monitor performance counters
- **Logs**: View structured application logs

## What's Instrumented

### Agent Level Instrumentation

The agent is instrumented using the builder pattern:

```csharp
var agent = new ChatClientAgent(instrumentedChatClient,
    name: "LocalLLMAgent",
    instructions: "You are a helpful AI assistant.")
    .AsBuilder()
    .UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
    .Build();
```

This captures:
- Agent execution spans
- Message processing
- Tool invocations (if any)
- Agent state changes

### Chat Client Instrumentation

The chat client is instrumented to track LLM interactions:

```csharp
var instrumentedChatClient = new LocalLLMChatClient(localLlmEndpoint, modelName, apiKey)
    .AsBuilder()
    .UseOpenTelemetry(sourceName: SourceName, configure: (cfg) => cfg.EnableSensitiveData = true)
    .Build();
```

This captures:
- Request/response to LLM
- Token usage (if available)
- Model parameters
- Latency

### Custom Telemetry

The application also includes custom instrumentation:

**Activities (Spans):**
- `Agent Session`: Parent span for the entire conversation session
- `Agent Interaction`: Individual user-agent exchanges

**Metrics:**
- `agent_interactions_total`: Counter of total interactions (tagged by status: success/error)
- `agent_response_time_seconds`: Histogram of response times

**Structured Logging:**
- All interactions logged with context
- Session ID and Agent Name in scope
- Detailed error logging

## Telemetry Data Structure

### Trace Example

```
Agent Session [TraceId: abc123]
├── Agent Interaction #1
│   ├── Chat Client Request
│   │   └── HTTP POST to LLM
│   └── Agent Response Processing
├── Agent Interaction #2
│   ├── Chat Client Request
│   │   └── HTTP POST to LLM
│   └── Agent Response Processing
└── ...
```

### Tags and Attributes

**Session Tags:**
- `agent.name`: Name of the agent
- `session.id`: Unique session identifier
- `session.start_time`: When the session started
- `session.end_time`: When the session ended
- `session.total_interactions`: Number of interactions
- `llm.endpoint`: LLM endpoint URL
- `llm.model`: Model name

**Interaction Tags:**
- `user.input`: User's message (if EnableSensitiveData = true)
- `interaction.number`: Sequence number
- `response.success`: Whether the interaction succeeded
- `response.time_seconds`: How long it took
- `error.message`: Error message (if failed)
- `error.type`: Exception type (if failed)

## Viewing Telemetry

### Aspire Dashboard

1. **Traces Tab**: 
   - View all traces with their hierarchy
   - Click on a trace to see detailed timing
   - Inspect tags and events
   - See the full call stack

2. **Metrics Tab**:
   - View `agent_interactions_total` counter
   - See `agent_response_time_seconds` histogram
   - Monitor HTTP client metrics
   - Check .NET runtime metrics

3. **Logs Tab**:
   - Structured logs with full context
   - Filter by log level
   - Search by session ID or interaction
   - Correlate with traces

### Example Queries

**Find all failed interactions:**
- Filter traces where `response.success = false`

**Find slow interactions:**
- Filter traces where duration > 5 seconds

**View specific session:**
- Search logs by `SessionId` property

## Alternative: Export to Application Insights

To export to Azure Application Insights:

1. **Set connection string:**
```bash
$env:APPLICATIONINSIGHTS_CONNECTION_STRING = "InstrumentationKey=..."
```

2. **The code automatically detects this** and adds the Azure Monitor exporter

3. **View in Azure Portal:**
   - Go to your Application Insights resource
   - Navigate to "Transaction search" or "Performance"
   - View end-to-end traces

## Alternative: Console Exporter (for Development)

For quick debugging without a dashboard, you can add the console exporter:

```csharp
.AddConsoleExporter() // Add this to your trace and metric builders
```

This will print telemetry directly to the console.

## Performance Considerations

### Sensitive Data

The application enables `EnableSensitiveData = true` for both agent and chat client. This means:

✅ **Pros:**
- Full visibility into prompts and responses
- Easier debugging
- Complete trace context

⚠️ **Cons:**
- User inputs are logged
- LLM responses are logged
- May contain PII or sensitive information

**For production**, consider setting `EnableSensitiveData = false`:

```csharp
.UseOpenTelemetry(SourceName, configure: (cfg) => cfg.EnableSensitiveData = false)
```

### Sampling

For high-volume production environments, consider adding sampling:

```csharp
.SetSampler(new TraceIdRatioBasedSampler(0.1)) // Sample 10% of traces
```

## Troubleshooting

### Aspire Dashboard Not Accessible

**Check Docker:**
```bash
docker ps | grep aspire-dashboard
```

**Check Ports:**
- Dashboard: `http://localhost:18888`
- OTLP Endpoint: `http://localhost:4318`

### No Telemetry Data Appearing

1. **Verify OTLP endpoint:**
   ```bash
   echo $env:OTEL_EXPORTER_OTLP_ENDPOINT
   ```

2. **Check application logs** for connection errors

3. **Verify the dashboard is running:**
   ```bash
   curl http://localhost:18888
   ```

### Build Errors

If you see missing types or methods:

1. **Ensure packages are installed:**
   ```bash
   dotnet restore
   ```

2. **Check package versions** in `agent-framework.csproj`

3. **Clean and rebuild:**
   ```bash
   dotnet clean
   dotnet build
   ```

## Learn More

- [Microsoft Agent Framework OpenTelemetry Sample](https://github.com/microsoft/agent-framework/blob/main/dotnet/samples/GettingStarted/AgentOpenTelemetry/Program.cs)
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [OpenTelemetry .NET Documentation](https://opentelemetry.io/docs/languages/net/)
- [Agent Framework Documentation](https://learn.microsoft.com/en-us/agent-framework/)

## Example Session Output

```
=== Microsoft Agent Framework - Local LLM Chat ===
This demo shows OpenTelemetry integration with a local LLM.
You can view the telemetry data in the Aspire Dashboard.
Type your message and press Enter. Type 'exit' or empty message to quit.

Connecting to: http://localhost:11434/v1/chat/completions
Model: llama3

Trace ID: 4bf92f3577b34da6a3ce929d0e0e4736

You (or 'exit' to quit): What is the capital of France?
Agent: The capital of France is Paris.

You (or 'exit' to quit): Tell me a fun fact about it
Agent: The Eiffel Tower was originally intended to be temporary and was almost torn down in 1909!

You (or 'exit' to quit): exit
Goodbye!
```

Then view the complete trace in the Aspire Dashboard to see:
- Session span covering all interactions
- Individual interaction spans with timing
- HTTP calls to your local LLM
- Custom metrics recorded
- Structured logs with full context

