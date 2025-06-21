# RhinoAI Plugin - API Reference

## Overview

This document provides comprehensive API reference documentation for the RhinoAI plugin, covering all public classes, methods, properties, and interfaces available for developers.

## Namespace: RhinoAI.AI

### EnhancedNLPProcessor

Main processing engine for natural language commands with advanced AI capabilities.

```csharp
public class EnhancedNLPProcessor
```

#### Methods

##### ProcessAsync
Processes natural language input with enhanced AI capabilities.

```csharp
public async Task<ProcessingResult> ProcessAsync(string input, ConversationContext context)
```

**Parameters:**
- `input` (string): Natural language input text
- `context` (ConversationContext): Current conversation context

**Returns:** ProcessingResult containing execution results and status

**Example:**
```csharp
var processor = new EnhancedNLPProcessor();
var context = new ConversationContext();
var result = await processor.ProcessAsync("create a red sphere", context);
```

##### ClassifyIntentAsync
Classifies user intent using hierarchical classification.

```csharp
public async Task<IntentResult> ClassifyIntentAsync(string input)
```

**Parameters:**
- `input` (string): Natural language input text

**Returns:** IntentResult with classified intent and confidence score

##### ExtractParametersAsync
Extracts parameters from natural language input using NER.

```csharp
public async Task<Dictionary<string, object>> ExtractParametersAsync(string input, CommandTemplate template, ConversationContext context)
```

**Parameters:**
- `input` (string): Natural language input text
- `template` (CommandTemplate): Command template for parameter mapping
- `context` (ConversationContext): Current conversation context

**Returns:** Dictionary of extracted parameters

#### Supported Intents

| Intent | Description | Keywords | Parameters |
|--------|-------------|----------|------------|
| CREATE_SPHERE | Create sphere geometry | sphere, ball | center, radius, color, name |
| CREATE_BOX | Create box geometry | box, cube | center, size, color, name |
| CREATE_CYLINDER | Create cylinder geometry | cylinder, pipe, tube | center, radius, height, color, name |
| MOVE_OBJECTS | Move selected objects | move, translate, shift | translation |
| SCALE_OBJECTS | Scale selected objects | scale, resize, size | scale |
| BOOLEAN_UNION | Boolean union operation | union, add, combine, merge | - |
| BOOLEAN_DIFFERENCE | Boolean difference operation | difference, subtract, cut | - |
| BOOLEAN_INTERSECTION | Boolean intersection operation | intersection, intersect | - |
| SELECT_ALL | Select all objects | select all, select everything | - |
| SELECT_OBJECTS | Select objects by name | select, choose | name |
| DESELECT_ALL | Deselect all objects | deselect all, clear selection | - |
| EXPLODE_OBJECTS | Explode selected objects | explode, break, separate | - |
| JOIN_OBJECTS | Join selected objects | join, connect, weld | - |

### IntentClassifier

Hierarchical intent recognition system with confidence scoring.

```csharp
public class IntentClassifier
```

#### Methods

##### ClassifyAsync
Classifies user intent with confidence scoring.

```csharp
public async Task<IntentResult> ClassifyAsync(string input)
```

**Parameters:**
- `input` (string): Natural language input text

**Returns:** IntentResult with intent classification and confidence

##### CalculateConfidence
Calculates confidence score for a specific intent.

```csharp
public double CalculateConfidence(string input, string intent)
```

**Parameters:**
- `input` (string): Natural language input text
- `intent` (string): Intent to calculate confidence for

**Returns:** Confidence score (0.0 to 1.0)

#### Intent Hierarchy

```
GEOMETRY_OPERATIONS
├── CREATE_GEOMETRY
│   ├── CREATE_SPHERE
│   ├── CREATE_BOX
│   └── CREATE_CYLINDER
├── TRANSFORM_GEOMETRY
│   ├── MOVE_OBJECTS
│   ├── SCALE_OBJECTS
│   └── ROTATE_OBJECTS
└── BOOLEAN_OPERATIONS
    ├── BOOLEAN_UNION
    ├── BOOLEAN_DIFFERENCE
    └── BOOLEAN_INTERSECTION

OBJECT_MANAGEMENT
├── SELECTION
│   ├── SELECT_ALL
│   ├── SELECT_OBJECTS
│   └── DESELECT_ALL
└── ORGANIZATION
    ├── EXPLODE_OBJECTS
    ├── JOIN_OBJECTS
    └── GROUP_OBJECTS
```

### ParameterExtractor

Advanced parameter parsing with Named Entity Recognition capabilities.

```csharp
public class ParameterExtractor
```

#### Methods

##### ExtractParametersAsync
Extracts parameters from natural language input.

```csharp
public async Task<Dictionary<string, object>> ExtractParametersAsync(string input, CommandTemplate template, ConversationContext context)
```

**Parameters:**
- `input` (string): Natural language input text
- `template` (CommandTemplate): Command template for parameter mapping
- `context` (ConversationContext): Current conversation context

**Returns:** Dictionary of extracted parameters

#### Supported Parameter Types

| Type | Description | Examples |
|------|-------------|----------|
| Point3d | 3D coordinates | "at origin", "at point 5,5,0", "center at 10,0,5" |
| Vector3d | 3D vectors | "5 units in X", "move by 10,0,0" |
| double | Numeric values | "radius 5", "scale by 2", "height 10" |
| Color | Color values | "red", "blue", "RGB(255,0,0)" |
| string | Text values | "name it sphere1", "call it wall" |
| bool | Boolean values | "visible", "hidden", "enabled" |

#### Parameter Extraction Patterns

```csharp
// Geometric parameters
"radius 5" → radius: 5.0
"size 10x10x10" → size: Vector3d(10,10,10)
"at origin" → center: Point3d(0,0,0)
"at point 5,5,0" → center: Point3d(5,5,0)

// Color parameters
"red" → color: Color.Red
"blue sphere" → color: Color.Blue
"RGB(255,0,0)" → color: Color.FromArgb(255,0,0)

// Name parameters
"name it sphere1" → name: "sphere1"
"call it wall" → name: "wall"

// Scale parameters
"scale by 2" → scale: 2.0
"make it bigger" → scale: 1.5 (context-dependent)
"resize to 50%" → scale: 0.5
```

### SemanticValidator

CAD-aware validation for geometric constraints and operations.

```csharp
public class SemanticValidator
```

#### Methods

##### ValidateParametersAsync
Validates extracted parameters for semantic correctness.

```csharp
public async Task<ValidationResult> ValidateParametersAsync(Dictionary<string, object> parameters, string intent, ConversationContext context)
```

**Parameters:**
- `parameters` (Dictionary<string, object>): Extracted parameters
- `intent` (string): Classified intent
- `context` (ConversationContext): Current conversation context

**Returns:** ValidationResult with validation status and messages

#### Validation Rules

| Rule Type | Description | Examples |
|-----------|-------------|----------|
| Geometric | Validate geometric constraints | Positive radius, valid coordinates |
| Logical | Validate operation logic | Objects exist for selection |
| Performance | Validate performance impact | Reasonable object counts |
| Safety | Validate safe operations | Prevent infinite loops |

### ConversationContext

Manages conversation history and scene context.

```csharp
public class ConversationContext
```

#### Properties

```csharp
public List<ConversationTurn> History { get; set; }
public SceneAnalysis CurrentScene { get; set; }
public Dictionary<string, object> SessionData { get; set; }
public DateTime LastActivity { get; set; }
public string UserId { get; set; }
```

#### Methods

##### AddTurn
Adds a conversation turn to history.

```csharp
public void AddTurn(string userInput, string response, ProcessingResult result)
```

##### GetRecentContext
Gets recent conversation context for processing.

```csharp
public string GetRecentContext(int turnCount = 5)
```

##### AnalyzeScene
Analyzes current Rhino scene for context.

```csharp
public SceneAnalysis AnalyzeScene()
```

## Namespace: RhinoAI.Core

### AIManager

Central coordination of AI services and provider management.

```csharp
public class AIManager
```

#### Methods

##### ProcessRequestAsync
Processes AI request with provider selection and fallback.

```csharp
public async Task<string> ProcessRequestAsync(string prompt, AIProvider preferredProvider = AIProvider.Auto)
```

**Parameters:**
- `prompt` (string): AI prompt text
- `preferredProvider` (AIProvider): Preferred AI provider

**Returns:** AI response text

##### GetAvailableProviders
Gets list of available and configured AI providers.

```csharp
public List<AIProvider> GetAvailableProviders()
```

#### AI Providers

```csharp
public enum AIProvider
{
    Auto,       // Automatic provider selection
    OpenAI,     // OpenAI GPT-4
    Claude,     // Anthropic Claude
    Local,      // Local models via Ollama
    Custom      // Custom provider
}
```

### ConfigurationManager

Manages plugin settings and configuration.

```csharp
public class ConfigurationManager
```

#### Properties

```csharp
public string OpenAIApiKey { get; set; }
public string ClaudeApiKey { get; set; }
public string LocalModelEndpoint { get; set; }
public bool EnableCaching { get; set; }
public int MaxCacheSize { get; set; }
public TimeSpan CacheExpiry { get; set; }
public AIProvider DefaultProvider { get; set; }
public double ConfidenceThreshold { get; set; }
public bool EnableDebugLogging { get; set; }
```

#### Methods

##### LoadSettings
Loads settings from Rhino preferences.

```csharp
public void LoadSettings()
```

##### SaveSettings
Saves settings to Rhino preferences.

```csharp
public void SaveSettings()
```

##### ValidateConfiguration
Validates current configuration.

```csharp
public ValidationResult ValidateConfiguration()
```

### SimpleLogger

Logging system for debugging and monitoring.

```csharp
public class SimpleLogger
```

#### Methods

##### LogInfo
Logs informational message.

```csharp
public void LogInfo(string message)
```

##### LogWarning
Logs warning message.

```csharp
public void LogWarning(string message)
```

##### LogError
Logs error message.

```csharp
public void LogError(string message, Exception exception = null)
```

##### LogDebug
Logs debug message (only in debug mode).

```csharp
public void LogDebug(string message)
```

## Namespace: RhinoAI.Integration

### OpenAIClient

OpenAI API integration client.

```csharp
public class OpenAIClient
```

#### Methods

##### SendRequestAsync
Sends request to OpenAI API.

```csharp
public async Task<string> SendRequestAsync(string prompt, string model = "gpt-4")
```

**Parameters:**
- `prompt` (string): Request prompt
- `model` (string): OpenAI model name

**Returns:** AI response text

##### GenerateImageAsync
Generates image using DALL-E.

```csharp
public async Task<string> GenerateImageAsync(string prompt, string size = "1024x1024")
```

**Parameters:**
- `prompt` (string): Image generation prompt
- `size` (string): Image size specification

**Returns:** Image URL or base64 data

### ClaudeClient

Anthropic Claude API integration client.

```csharp
public class ClaudeClient
```

#### Methods

##### SendRequestAsync
Sends request to Claude API.

```csharp
public async Task<string> SendRequestAsync(string prompt, string model = "claude-3-sonnet-20240229")
```

**Parameters:**
- `prompt` (string): Request prompt
- `model` (string): Claude model name

**Returns:** AI response text

### MCPClient

Model Context Protocol client implementation.

```csharp
public class MCPClient
```

#### Methods

##### ConnectAsync
Connects to MCP server.

```csharp
public async Task<bool> ConnectAsync(string serverEndpoint)
```

##### SendMessageAsync
Sends message via MCP protocol.

```csharp
public async Task<MCPResponse> SendMessageAsync(MCPRequest request)
```

### MCPServer

Model Context Protocol server implementation.

```csharp
public class MCPServer
```

#### Methods

##### StartAsync
Starts MCP server.

```csharp
public async Task StartAsync(int port = 8080)
```

##### StopAsync
Stops MCP server.

```csharp
public async Task StopAsync()
```

## Namespace: RhinoAI.UI

### AIControlPanel

Main UI panel for AI interaction.

```csharp
public class AIControlPanel : UserControl
```

#### Properties

```csharp
public string CurrentInput { get; set; }
public List<string> History { get; set; }
public bool IsProcessing { get; set; }
```

#### Events

```csharp
public event EventHandler<string> CommandSubmitted;
public event EventHandler<ProcessingResult> ProcessingCompleted;
```

#### Methods

##### SubmitCommand
Submits command for processing.

```csharp
public async Task SubmitCommand(string command)
```

##### ClearHistory
Clears command history.

```csharp
public void ClearHistory()
```

### AIAssistantCommand

Rhino command for AI interaction.

```csharp
public class AIAssistantCommand : Command
```

#### Properties

```csharp
public override string EnglishName => "AIAssistant";
```

#### Methods

##### RunCommand
Executes the AI assistant command.

```csharp
protected override Result RunCommand(RhinoDoc doc, RunMode mode)
```

## Data Models

### ProcessingResult

Result of natural language processing operation.

```csharp
public class ProcessingResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string ErrorMessage { get; set; }
    public Dictionary<string, object> Data { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    
    public static ProcessingResult Success(string message, Dictionary<string, object> data = null)
    public static ProcessingResult Warning(string message)
    public static ProcessingResult Error(string message, Exception exception = null)
}
```

### IntentResult

Result of intent classification.

```csharp
public class IntentResult
{
    public string Intent { get; set; }
    public double Confidence { get; set; }
    public Dictionary<string, double> AlternativeIntents { get; set; }
    public bool IsValid => Confidence > 0.7;
}
```

### ValidationResult

Result of semantic validation.

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
    public Dictionary<string, object> ValidatedParameters { get; set; }
}
```

### CommandTemplate

Template for command matching and parameter extraction.

```csharp
public class CommandTemplate
{
    public string CommandName { get; set; }
    public string[] Keywords { get; set; }
    public string Description { get; set; }
    public string[] Parameters { get; set; }
    public Dictionary<string, Type> ParameterTypes { get; set; }
}
```

### ConversationTurn

Single turn in conversation history.

```csharp
public class ConversationTurn
{
    public string UserInput { get; set; }
    public string Response { get; set; }
    public ProcessingResult Result { get; set; }
    public DateTime Timestamp { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
```

### SceneAnalysis

Analysis of current Rhino scene.

```csharp
public class SceneAnalysis
{
    public int ObjectCount { get; set; }
    public List<string> ObjectTypes { get; set; }
    public List<string> LayerNames { get; set; }
    public BoundingBox SceneBounds { get; set; }
    public List<RhinoObject> SelectedObjects { get; set; }
    public DateTime AnalysisTime { get; set; }
}
```

## Error Handling

### Exception Types

#### RhinoAIException
Base exception for RhinoAI operations.

```csharp
public class RhinoAIException : Exception
{
    public RhinoAIException(string message) : base(message) { }
    public RhinoAIException(string message, Exception innerException) : base(message, innerException) { }
}
```

#### AIProviderException
Exception for AI provider communication errors.

```csharp
public class AIProviderException : RhinoAIException
{
    public AIProvider Provider { get; set; }
    public int ErrorCode { get; set; }
}
```

#### ValidationException
Exception for validation errors.

```csharp
public class ValidationException : RhinoAIException
{
    public ValidationResult ValidationResult { get; set; }
}
```

## Usage Examples

### Basic Usage

```csharp
// Initialize processor
var processor = new EnhancedNLPProcessor();
var context = new ConversationContext();

// Process natural language command
var result = await processor.ProcessAsync("create a red sphere with radius 5", context);

if (result.Success)
{
    Console.WriteLine($"Command executed successfully: {result.Message}");
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

### Advanced Usage with Custom Configuration

```csharp
// Configure AI manager
var config = new ConfigurationManager();
config.DefaultProvider = AIProvider.Claude;
config.EnableCaching = true;
config.ConfidenceThreshold = 0.8;

var aiManager = new AIManager(config);
var processor = new EnhancedNLPProcessor(aiManager);

// Process with custom context
var context = new ConversationContext
{
    UserId = "user123",
    SessionData = new Dictionary<string, object>
    {
        ["preferred_units"] = "millimeters",
        ["default_color"] = Color.Blue
    }
};

var result = await processor.ProcessAsync("make a cylinder 50 high", context);
```

### Error Handling Example

```csharp
try
{
    var processor = new EnhancedNLPProcessor();
    var result = await processor.ProcessAsync(userInput, context);
    
    if (!result.Success)
    {
        // Handle processing failure
        logger.LogWarning($"Processing failed: {result.ErrorMessage}");
        ShowUserMessage(result.ErrorMessage);
    }
}
catch (AIProviderException ex)
{
    // Handle AI provider errors
    logger.LogError($"AI Provider error: {ex.Message}", ex);
    ShowUserMessage("AI service temporarily unavailable. Please try again.");
}
catch (ValidationException ex)
{
    // Handle validation errors
    logger.LogWarning($"Validation error: {ex.Message}");
    ShowUserMessage($"Invalid input: {ex.ValidationResult.Errors.First()}");
}
catch (Exception ex)
{
    // Handle unexpected errors
    logger.LogError($"Unexpected error: {ex.Message}", ex);
    ShowUserMessage("An unexpected error occurred. Please check the logs.");
}
```

## Performance Considerations

### Caching
- Enable response caching for improved performance
- Configure appropriate cache size limits
- Use cache expiry to balance performance and freshness

### Parallel Processing
- Use async/await patterns for non-blocking operations
- Configure thread pool size for optimal performance
- Implement cancellation tokens for long-running operations

### Memory Management
- Dispose of large objects promptly
- Monitor memory usage in long-running sessions
- Implement periodic cleanup of conversation history

---

*API Reference last updated: January 2025*
*Plugin Version: 2.0.0*
*Rhino Version: 8.0+* 