# RhinoAI Plugin - Development Guide

## Table of Contents

1. [Getting Started](#getting-started)
2. [Development Environment Setup](#development-environment-setup)
3. [Project Structure](#project-structure)
4. [Core Architecture](#core-architecture)
5. [Extending the Plugin](#extending-the-plugin)
6. [Contributing Guidelines](#contributing-guidelines)
7. [Testing](#testing)
8. [Debugging](#debugging)
9. [Performance Optimization](#performance-optimization)
10. [Deployment](#deployment)

## Getting Started

This guide provides comprehensive information for developers who want to extend, modify, or contribute to the RhinoAI plugin. Whether you're adding new features, fixing bugs, or integrating with external services, this guide will help you understand the codebase and development practices.

### Prerequisites

Before starting development, ensure you have:

- **Development Environment**:
  - Visual Studio 2022 (recommended) or VS Code
  - .NET 7.0 SDK or later
  - Rhino 8 SDK and development tools
  - Git for version control

- **Knowledge Requirements**:
  - C# programming (intermediate to advanced)
  - Rhino 3D and RhinoCommon API
  - Basic understanding of AI/ML concepts
  - Async/await programming patterns
  - Unit testing with NUnit

- **Optional but Helpful**:
  - Experience with OpenAI or Anthropic APIs
  - Natural Language Processing concepts
  - WPF/Windows Forms for UI development

## Development Environment Setup

### 1. Clone the Repository

```bash
git clone https://github.com/your-org/RhinoAI.git
cd RhinoAI
```

### 2. Install Dependencies

```bash
# Restore NuGet packages
dotnet restore

# Verify Rhino SDK installation
# Ensure RhinoCommon references are resolved
```

### 3. Configure Development Settings

Create a `appsettings.Development.json` file:

```json
{
  "AIProviders": {
    "OpenAI": {
      "ApiKey": "your-development-api-key",
      "Model": "gpt-4",
      "MaxTokens": 2048
    },
    "Claude": {
      "ApiKey": "your-claude-api-key",
      "Model": "claude-3-sonnet-20240229"
    }
  },
  "Logging": {
    "LogLevel": "Debug",
    "EnableFileLogging": true,
    "LogPath": "./logs/"
  },
  "Development": {
    "EnableDebugMode": true,
    "MockAIResponses": false,
    "EnablePerformanceProfiling": true
  }
}
```

### 4. Build and Test

```bash
# Build the solution
dotnet build --configuration Debug

# Run unit tests
dotnet test

# Build for Rhino deployment
dotnet build --configuration Release
```

### 5. Rhino Development Setup

Configure Rhino for plugin development:

1. **Set Startup Project**: Configure Visual Studio to launch Rhino
2. **Debug Settings**: Set Rhino.exe as the startup program
3. **Plugin Location**: Ensure plugin builds to Rhino plugins directory
4. **Debugging**: Enable managed debugging in Visual Studio

## Project Structure

### Directory Organization

```
RhinoAI/
├── AI/                          # Core AI Processing
│   ├── EnhancedNLPProcessor.cs     # Main NLP engine
│   ├── EnhancedNLPSupportingClasses.cs # Supporting classes
│   ├── NLPProcessor.cs             # Original processor
│   ├── GenerativeDesigner.cs      # Generative design (future)
│   ├── VisionProcessor.cs         # Computer vision (future)
│   └── RealTimeAssistant.cs       # Real-time assistance (future)
├── Core/                        # Core Services
│   ├── AIManager.cs               # AI coordination
│   ├── ConfigurationManager.cs   # Configuration management
│   ├── ContextManager.cs          # Context management
│   ├── SimpleLogger.cs            # Logging system
│   └── SuggestionEngine.cs        # Suggestion generation
├── Integration/                 # External Integrations
│   ├── OpenAIClient.cs            # OpenAI integration
│   ├── ClaudeClient.cs            # Anthropic Claude
│   ├── MCPClient.cs               # MCP client
│   └── MCPServer.cs               # MCP server
├── UI/                          # User Interface
│   ├── Commands/                  # Rhino commands
│   │   └── AIAssistantCommand.cs
│   ├── Panels/                    # UI panels
│   │   └── AIControlPanel.cs
│   └── Dialogs/                   # Modal dialogs
├── Models/                      # Data Models
│   ├── DesignPrompt.cs            # Design prompts
│   └── OptimizationGoals.cs       # Optimization goals
├── Tests/                       # Test Projects
│   ├── Unit/                      # Unit tests
│   └── Integration/               # Integration tests
├── Resources/                   # Resources
│   ├── Icons/                     # UI icons
│   └── Templates/                 # Templates
└── Documentation/               # Documentation
    ├── README.md
    ├── API-Reference.md
    ├── User-Guide.md
    └── Development-Guide.md
```

### Key Namespaces

```csharp
// Core namespaces
using RhinoAI.AI;              // AI processing components
using RhinoAI.Core;            // Core services
using RhinoAI.Integration;     // External integrations
using RhinoAI.UI;              // User interface
using RhinoAI.Models;          // Data models

// External dependencies
using Rhino;                   // Rhino core
using Rhino.Commands;          // Rhino commands
using Rhino.Geometry;          // Rhino geometry
using Rhino.UI;                // Rhino UI
```

## Core Architecture

### Processing Pipeline

Understanding the processing pipeline is crucial for development:

```csharp
public class ProcessingPipeline
{
    // Stage 1: Input Preprocessing
    private async Task<string> PreprocessInput(string input)
    {
        // Text normalization, spell checking, language detection
    }
    
    // Stage 2: Intent Classification
    private async Task<IntentResult> ClassifyIntent(string input)
    {
        // Hierarchical intent recognition with confidence scoring
    }
    
    // Stage 3: Context Analysis
    private async Task<ConversationContext> AnalyzeContext(string input, ConversationContext context)
    {
        // Scene analysis, conversation history, reference resolution
    }
    
    // Stage 4: Parameter Extraction
    private async Task<Dictionary<string, object>> ExtractParameters(string input, IntentResult intent, ConversationContext context)
    {
        // NER-based parameter extraction, type conversion
    }
    
    // Stage 5: Semantic Validation
    private async Task<ValidationResult> ValidateParameters(Dictionary<string, object> parameters, string intent)
    {
        // CAD-aware validation, constraint checking
    }
    
    // Stage 6: Command Execution
    private async Task<ProcessingResult> ExecuteCommand(string intent, Dictionary<string, object> parameters)
    {
        // Rhino API calls, transaction management, error handling
    }
}
```

### Key Design Patterns

#### 1. Strategy Pattern for AI Providers

```csharp
public interface IAIProvider
{
    Task<string> ProcessRequestAsync(string prompt, AIRequestOptions options);
    bool IsAvailable { get; }
    string ProviderName { get; }
}

public class OpenAIProvider : IAIProvider
{
    public async Task<string> ProcessRequestAsync(string prompt, AIRequestOptions options)
    {
        // OpenAI-specific implementation
    }
}

public class ClaudeProvider : IAIProvider
{
    public async Task<string> ProcessRequestAsync(string prompt, AIRequestOptions options)
    {
        // Claude-specific implementation
    }
}
```

#### 2. Command Pattern for Operations

```csharp
public interface IRhinoCommand
{
    Task<ProcessingResult> ExecuteAsync(Dictionary<string, object> parameters, ConversationContext context);
    string CommandName { get; }
    string[] Keywords { get; }
}

public class CreateSphereCommand : IRhinoCommand
{
    public string CommandName => "CREATE_SPHERE";
    public string[] Keywords => new[] { "sphere", "ball", "create sphere" };
    
    public async Task<ProcessingResult> ExecuteAsync(Dictionary<string, object> parameters, ConversationContext context)
    {
        // Sphere creation implementation
    }
}
```

#### 3. Factory Pattern for Component Creation

```csharp
public class ComponentFactory
{
    public static IRhinoCommand CreateCommand(string commandType)
    {
        return commandType switch
        {
            "CREATE_SPHERE" => new CreateSphereCommand(),
            "CREATE_BOX" => new CreateBoxCommand(),
            "MOVE_OBJECTS" => new MoveObjectsCommand(),
            _ => throw new NotSupportedException($"Command type {commandType} not supported")
        };
    }
}
```

## Extending the Plugin

### Adding New Commands

#### 1. Create Command Class

```csharp
using RhinoAI.Core;
using RhinoAI.Models;

namespace RhinoAI.Commands
{
    public class CreateConeCommand : IRhinoCommand
    {
        public string CommandName => "CREATE_CONE";
        public string[] Keywords => new[] { "cone", "create cone", "make cone" };
        
        public async Task<ProcessingResult> ExecuteAsync(Dictionary<string, object> parameters, ConversationContext context)
        {
            try
            {
                // Extract parameters
                var center = ExtractPoint3d(parameters, "center", Point3d.Origin);
                var radius = ExtractDouble(parameters, "radius", 1.0);
                var height = ExtractDouble(parameters, "height", 2.0);
                var color = ExtractColor(parameters, "color", Color.Gray);
                var name = ExtractString(parameters, "name", "Cone");
                
                // Validate parameters
                if (radius <= 0 || height <= 0)
                {
                    return ProcessingResult.Error("Radius and height must be positive values");
                }
                
                // Create cone geometry
                var cone = new Cone(new Plane(center, Vector3d.ZAxis), height, radius);
                var brep = cone.ToBrep(true);
                
                if (brep == null || !brep.IsValid)
                {
                    return ProcessingResult.Error("Failed to create valid cone geometry");
                }
                
                // Add to Rhino document
                var doc = RhinoDoc.ActiveDoc;
                var attributes = new ObjectAttributes
                {
                    Name = name,
                    ObjectColor = color,
                    ColorSource = ObjectColorSource.ColorFromObject
                };
                
                var id = doc.Objects.AddBrep(brep, attributes);
                if (id == Guid.Empty)
                {
                    return ProcessingResult.Error("Failed to add cone to document");
                }
                
                // Update context
                context.LastCreatedObject = id;
                context.LastOperation = "CREATE_CONE";
                
                // Update views
                doc.Views.Redraw();
                
                return ProcessingResult.Success($"Created cone '{name}' with radius {radius} and height {height}");
            }
            catch (Exception ex)
            {
                return ProcessingResult.Error($"Error creating cone: {ex.Message}");
            }
        }
        
        private Point3d ExtractPoint3d(Dictionary<string, object> parameters, string key, Point3d defaultValue)
        {
            if (parameters.TryGetValue(key, out var value) && value is Point3d point)
                return point;
            return defaultValue;
        }
        
        private double ExtractDouble(Dictionary<string, object> parameters, string key, double defaultValue)
        {
            if (parameters.TryGetValue(key, out var value) && value is double d)
                return d;
            return defaultValue;
        }
        
        private Color ExtractColor(Dictionary<string, object> parameters, string key, Color defaultValue)
        {
            if (parameters.TryGetValue(key, out var value) && value is Color color)
                return color;
            return defaultValue;
        }
        
        private string ExtractString(Dictionary<string, object> parameters, string key, string defaultValue)
        {
            if (parameters.TryGetValue(key, out var value) && value is string str)
                return str;
            return defaultValue;
        }
    }
}
```

#### 2. Register Command

```csharp
public class CommandRegistry
{
    private static readonly Dictionary<string, Type> RegisteredCommands = new()
    {
        { "CREATE_SPHERE", typeof(CreateSphereCommand) },
        { "CREATE_BOX", typeof(CreateBoxCommand) },
        { "CREATE_CYLINDER", typeof(CreateCylinderCommand) },
        { "CREATE_CONE", typeof(CreateConeCommand) }, // Add new command
        // ... other commands
    };
    
    public static IRhinoCommand CreateCommand(string commandType)
    {
        if (RegisteredCommands.TryGetValue(commandType, out var commandClass))
        {
            return (IRhinoCommand)Activator.CreateInstance(commandClass);
        }
        throw new NotSupportedException($"Command type {commandType} not supported");
    }
}
```

#### 3. Update Intent Classification

Add the new command to the intent classification system:

```csharp
public class IntentClassifier
{
    private readonly Dictionary<string, string[]> _intentKeywords = new()
    {
        // Existing intents...
        { "CREATE_CONE", new[] { "cone", "create cone", "make cone", "add cone", "cone shape" } }
    };
    
    private readonly Dictionary<string, string> _intentHierarchy = new()
    {
        // Existing hierarchy...
        { "CREATE_CONE", "GEOMETRY_OPERATIONS.CREATE_GEOMETRY" }
    };
}
```

### Adding New AI Providers

#### 1. Implement Provider Interface

```csharp
using RhinoAI.Integration;

namespace RhinoAI.Providers
{
    public class CustomAIProvider : IAIProvider
    {
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly HttpClient _httpClient;
        
        public string ProviderName => "CustomAI";
        public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);
        
        public CustomAIProvider(string apiKey, string endpoint)
        {
            _apiKey = apiKey;
            _endpoint = endpoint;
            _httpClient = new HttpClient();
        }
        
        public async Task<string> ProcessRequestAsync(string prompt, AIRequestOptions options)
        {
            try
            {
                var request = new CustomAIRequest
                {
                    Prompt = prompt,
                    MaxTokens = options.MaxTokens,
                    Temperature = options.Temperature
                };
                
                var response = await SendRequestAsync(request);
                return response.Text;
            }
            catch (Exception ex)
            {
                throw new AIProviderException($"CustomAI request failed: {ex.Message}", ex);
            }
        }
        
        private async Task<CustomAIResponse> SendRequestAsync(CustomAIRequest request)
        {
            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", _apiKey);
            
            var response = await _httpClient.PostAsync(_endpoint, content);
            response.EnsureSuccessStatusCode();
            
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<CustomAIResponse>(responseJson);
        }
    }
    
    public class CustomAIRequest
    {
        public string Prompt { get; set; }
        public int MaxTokens { get; set; }
        public double Temperature { get; set; }
    }
    
    public class CustomAIResponse
    {
        public string Text { get; set; }
        public int TokensUsed { get; set; }
    }
}
```

#### 2. Register Provider

```csharp
public class AIManager
{
    private readonly Dictionary<AIProvider, IAIProvider> _providers;
    
    public void RegisterProvider(AIProvider providerType, IAIProvider provider)
    {
        _providers[providerType] = provider;
    }
    
    public void InitializeProviders(ConfigurationManager config)
    {
        // Existing providers...
        
        // Register custom provider
        if (!string.IsNullOrEmpty(config.CustomAIApiKey))
        {
            var customProvider = new CustomAIProvider(config.CustomAIApiKey, config.CustomAIEndpoint);
            RegisterProvider(AIProvider.Custom, customProvider);
        }
    }
}
```

### Adding Custom Parameter Extractors

#### 1. Create Parameter Extractor

```csharp
using RhinoAI.AI;

namespace RhinoAI.Extractors
{
    public class MaterialParameterExtractor : IParameterExtractor
    {
        public string ParameterType => "Material";
        
        public async Task<object> ExtractAsync(string input, ConversationContext context)
        {
            // Extract material information from input
            var materialPatterns = new[]
            {
                @"(?:material|mat)\s+([a-zA-Z]+)",
                @"(?:made\s+of|using)\s+([a-zA-Z]+)",
                @"([a-zA-Z]+)\s+material"
            };
            
            foreach (var pattern in materialPatterns)
            {
                var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var materialName = match.Groups[1].Value.ToLower();
                    return GetMaterialByName(materialName);
                }
            }
            
            return null;
        }
        
        private Material GetMaterialByName(string name)
        {
            return name switch
            {
                "wood" => CreateWoodMaterial(),
                "metal" => CreateMetalMaterial(),
                "glass" => CreateGlassMaterial(),
                "plastic" => CreatePlasticMaterial(),
                _ => null
            };
        }
        
        private Material CreateWoodMaterial()
        {
            var material = new Material();
            material.DiffuseColor = Color.SaddleBrown;
            material.Name = "Wood";
            // Set other material properties...
            return material;
        }
        
        // Implement other material creation methods...
    }
}
```

#### 2. Register Extractor

```csharp
public class ParameterExtractorRegistry
{
    private readonly Dictionary<string, IParameterExtractor> _extractors = new();
    
    public void RegisterExtractor(IParameterExtractor extractor)
    {
        _extractors[extractor.ParameterType] = extractor;
    }
    
    public void InitializeExtractors()
    {
        // Register built-in extractors
        RegisterExtractor(new Point3dParameterExtractor());
        RegisterExtractor(new ColorParameterExtractor());
        RegisterExtractor(new DoubleParameterExtractor());
        
        // Register custom extractors
        RegisterExtractor(new MaterialParameterExtractor());
    }
}
```

### Adding Custom UI Components

#### 1. Create Custom Panel

```csharp
using RhinoAI.UI;

namespace RhinoAI.CustomUI
{
    public partial class MaterialManagerPanel : UserControl
    {
        private readonly MaterialService _materialService;
        
        public MaterialManagerPanel()
        {
            InitializeComponent();
            _materialService = new MaterialService();
            LoadMaterials();
        }
        
        private void LoadMaterials()
        {
            var materials = _materialService.GetAvailableMaterials();
            materialListBox.DataSource = materials;
        }
        
        private void ApplyMaterialButton_Click(object sender, EventArgs e)
        {
            if (materialListBox.SelectedItem is Material selectedMaterial)
            {
                ApplyMaterialToSelection(selectedMaterial);
            }
        }
        
        private void ApplyMaterialToSelection(Material material)
        {
            var doc = RhinoDoc.ActiveDoc;
            var selectedObjects = doc.Objects.GetSelectedObjects(false, false);
            
            foreach (var obj in selectedObjects)
            {
                var attributes = obj.Attributes.Duplicate();
                attributes.MaterialIndex = doc.Materials.Add(material);
                attributes.MaterialSource = ObjectMaterialSource.MaterialFromObject;
                
                doc.Objects.ModifyAttributes(obj, attributes, true);
            }
            
            doc.Views.Redraw();
        }
    }
}
```

#### 2. Register Panel

```csharp
public class RhinoAIPlugin : PlugIn
{
    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        // Register main AI panel
        Panels.RegisterPanel(this, typeof(AIControlPanel), "AI Assistant", null);
        
        // Register custom panel
        Panels.RegisterPanel(this, typeof(MaterialManagerPanel), "Material Manager", null);
        
        return LoadReturnCode.Success;
    }
}
```

## Contributing Guidelines

### Code Standards

#### Naming Conventions
```csharp
// Classes: PascalCase
public class EnhancedNLPProcessor { }

// Methods: PascalCase
public async Task<ProcessingResult> ProcessAsync() { }

// Properties: PascalCase
public string CommandName { get; set; }

// Fields: camelCase with underscore prefix
private readonly IAIManager _aiManager;

// Constants: PascalCase
public const int MaxRetryAttempts = 3;

// Local variables: camelCase
var processingResult = await ProcessAsync();
```

#### Documentation Standards
```csharp
/// <summary>
/// Processes natural language input and executes corresponding Rhino operations.
/// </summary>
/// <param name="input">Natural language input from the user</param>
/// <param name="context">Current conversation context including history and scene state</param>
/// <returns>Processing result containing execution status and any generated data</returns>
/// <exception cref="ArgumentNullException">Thrown when input is null or empty</exception>
/// <exception cref="AIProviderException">Thrown when AI provider communication fails</exception>
/// <example>
/// <code>
/// var processor = new EnhancedNLPProcessor();
/// var context = new ConversationContext();
/// var result = await processor.ProcessAsync("create a red sphere", context);
/// </code>
/// </example>
public async Task<ProcessingResult> ProcessAsync(string input, ConversationContext context)
{
    // Implementation
}
```

#### Error Handling Patterns
```csharp
public async Task<ProcessingResult> ProcessAsync(string input, ConversationContext context)
{
    try
    {
        // Validate input
        if (string.IsNullOrWhiteSpace(input))
        {
            return ProcessingResult.Error("Input cannot be null or empty");
        }
        
        // Process with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await ProcessWithTimeoutAsync(input, context, cts.Token);
        
        return result;
    }
    catch (OperationCanceledException)
    {
        return ProcessingResult.Error("Processing timeout exceeded");
    }
    catch (AIProviderException ex)
    {
        _logger.LogError(ex, "AI provider error during processing");
        return ProcessingResult.Error($"AI service error: {ex.Message}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error during processing");
        return ProcessingResult.Error("An unexpected error occurred");
    }
}
```

### Pull Request Process

#### 1. Preparation
- Fork the repository
- Create a feature branch: `git checkout -b feature/your-feature-name`
- Ensure your development environment is set up correctly

#### 2. Development
- Write code following the established patterns and standards
- Add comprehensive unit tests for new functionality
- Update documentation for any API changes
- Test your changes thoroughly

#### 3. Testing
```bash
# Run all tests
dotnet test

# Run specific test category
dotnet test --filter Category=Unit

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Performance tests
dotnet test --filter Category=Performance
```

#### 4. Documentation
- Update API documentation for new public methods
- Add code examples for complex functionality
- Update user guide if UI changes are involved
- Include performance impact notes for significant changes

#### 5. Submission
- Commit changes with descriptive messages
- Push to your feature branch
- Create a pull request with detailed description
- Include screenshots for UI changes
- Link related issues

#### 6. Review Process
- Code review by maintainers
- Automated testing and quality checks
- Performance impact assessment
- Documentation review
- Final approval and merge

### Commit Message Format

```
type(scope): brief description

Detailed explanation of the change, including:
- What was changed and why
- Any breaking changes
- Performance implications
- Related issues

Closes #123
```

Types:
- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes
- `refactor`: Code refactoring
- `perf`: Performance improvements
- `test`: Test additions or changes
- `chore`: Build process or auxiliary tool changes

## Testing

### Unit Testing Framework

#### Test Structure
```csharp
[TestFixture]
public class EnhancedNLPProcessorTests
{
    private EnhancedNLPProcessor _processor;
    private Mock<IAIManager> _mockAIManager;
    private Mock<IContextManager> _mockContextManager;
    private TestConversationContext _testContext;
    
    [SetUp]
    public void SetUp()
    {
        _mockAIManager = new Mock<IAIManager>();
        _mockContextManager = new Mock<IContextManager>();
        _processor = new EnhancedNLPProcessor(_mockAIManager.Object, _mockContextManager.Object);
        _testContext = new TestConversationContext();
    }
    
    [TearDown]
    public void TearDown()
    {
        _processor?.Dispose();
    }
    
    [Test]
    public async Task ProcessAsync_SimpleGeometryCreation_ReturnsSuccess()
    {
        // Arrange
        var input = "create a red sphere with radius 5";
        _mockAIManager.Setup(x => x.ProcessRequestAsync(It.IsAny<string>(), It.IsAny<AIProvider>()))
                     .ReturnsAsync("CREATE_SPHERE");
        
        // Act
        var result = await _processor.ProcessAsync(input, _testContext);
        
        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Contains.Substring("sphere"));
        _mockAIManager.Verify(x => x.ProcessRequestAsync(It.IsAny<string>(), It.IsAny<AIProvider>()), Times.Once);
    }
    
    [Test]
    [TestCase("")]
    [TestCase(null)]
    [TestCase("   ")]
    public async Task ProcessAsync_InvalidInput_ReturnsError(string input)
    {
        // Act
        var result = await _processor.ProcessAsync(input, _testContext);
        
        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.Not.Null);
    }
    
    [Test]
    public async Task ProcessAsync_AIProviderTimeout_ReturnsError()
    {
        // Arrange
        var input = "create a sphere";
        _mockAIManager.Setup(x => x.ProcessRequestAsync(It.IsAny<string>(), It.IsAny<AIProvider>()))
                     .ThrowsAsync(new TimeoutException("AI provider timeout"));
        
        // Act
        var result = await _processor.ProcessAsync(input, _testContext);
        
        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Contains.Substring("timeout"));
    }
}
```

#### Test Categories
```csharp
[Test]
[Category("Unit")]
public void UnitTest_Method() { }

[Test]
[Category("Integration")]
public void IntegrationTest_Method() { }

[Test]
[Category("Performance")]
public void PerformanceTest_Method() { }

[Test]
[Category("UI")]
public void UITest_Method() { }
```

### Integration Testing

#### Rhino Integration Tests
```csharp
[TestFixture]
public class RhinoIntegrationTests
{
    private RhinoDoc _testDoc;
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        // Initialize Rhino for testing
        if (!RhinoInside.Core.IsInitialized)
        {
            RhinoInside.Core.Initialize();
        }
    }
    
    [SetUp]
    public void SetUp()
    {
        _testDoc = RhinoDoc.CreateHeadless("Test Document");
    }
    
    [TearDown]
    public void TearDown()
    {
        _testDoc?.Dispose();
    }
    
    [Test]
    public async Task CreateSphere_ValidParameters_CreatesGeometry()
    {
        // Arrange
        var command = new CreateSphereCommand();
        var parameters = new Dictionary<string, object>
        {
            { "center", Point3d.Origin },
            { "radius", 5.0 },
            { "color", Color.Red }
        };
        var context = new ConversationContext();
        
        // Act
        var result = await command.ExecuteAsync(parameters, context);
        
        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testDoc.Objects.Count, Is.EqualTo(1));
        
        var createdObject = _testDoc.Objects.First();
        Assert.That(createdObject.Geometry, Is.InstanceOf<Brep>());
    }
}
```

### Performance Testing

#### Performance Test Framework
```csharp
[TestFixture]
[Category("Performance")]
public class PerformanceTests
{
    private EnhancedNLPProcessor _processor;
    private ConversationContext _context;
    
    [SetUp]
    public void SetUp()
    {
        _processor = new EnhancedNLPProcessor();
        _context = new ConversationContext();
    }
    
    [Test]
    public async Task ProcessAsync_SimpleCommand_CompletesWithinTimeLimit()
    {
        // Arrange
        var input = "create a sphere";
        var stopwatch = Stopwatch.StartNew();
        
        // Act
        var result = await _processor.ProcessAsync(input, _context);
        stopwatch.Stop();
        
        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(500), 
                   $"Processing took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }
    
    [Test]
    public async Task ProcessAsync_BatchOperations_ScalesLinearly()
    {
        // Arrange
        var commands = Enumerable.Range(1, 10)
                                .Select(i => $"create a sphere named sphere{i}")
                                .ToArray();
        
        var times = new List<long>();
        
        // Act
        foreach (var command in commands)
        {
            var stopwatch = Stopwatch.StartNew();
            await _processor.ProcessAsync(command, _context);
            stopwatch.Stop();
            times.Add(stopwatch.ElapsedMilliseconds);
        }
        
        // Assert
        var averageTime = times.Average();
        var maxTime = times.Max();
        
        Assert.That(maxTime, Is.LessThan(averageTime * 2), 
                   "Processing time should scale linearly");
    }
}
```

## Debugging

### Debug Configuration

#### Visual Studio Debugging
```xml
<!-- In .csproj file -->
<PropertyGroup Condition="'$(Configuration)'=='Debug'">
  <StartAction>Program</StartAction>
  <StartProgram>C:\Program Files\Rhino 8\System\Rhino.exe</StartProgram>
  <StartArguments>/nosplash</StartArguments>
  <StartWorkingDirectory>$(OutputPath)</StartWorkingDirectory>
</PropertyGroup>
```

#### Debug Logging
```csharp
public class DebugLogger
{
    [Conditional("DEBUG")]
    public static void LogProcessingStep(string step, object data = null)
    {
        var message = $"[{DateTime.Now:HH:mm:ss.fff}] {step}";
        if (data != null)
        {
            message += $": {JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true })}";
        }
        
        Debug.WriteLine(message);
        Console.WriteLine(message);
    }
}

// Usage
DebugLogger.LogProcessingStep("Intent Classification", new { Intent = "CREATE_SPHERE", Confidence = 0.95 });
```

#### Breakpoint Strategies
```csharp
public async Task<ProcessingResult> ProcessAsync(string input, ConversationContext context)
{
    #if DEBUG
    // Conditional breakpoint for specific inputs
    if (input.Contains("debug"))
    {
        Debugger.Break();
    }
    #endif
    
    try
    {
        // Set breakpoint here for normal flow debugging
        var preprocessed = await PreprocessInput(input);
        
        // Breakpoint for intent classification issues
        var intent = await ClassifyIntent(preprocessed);
        
        // Breakpoint for parameter extraction problems
        var parameters = await ExtractParameters(preprocessed, intent, context);
        
        return await ExecuteCommand(intent.Intent, parameters);
    }
    catch (Exception ex)
    {
        // Breakpoint for error analysis
        LogError(ex);
        throw;
    }
}
```

### Common Debugging Scenarios

#### AI Provider Communication Issues
```csharp
public class AIProviderDebugger
{
    public static void LogRequest(string provider, string prompt, AIRequestOptions options)
    {
        Debug.WriteLine($"[AI Request] Provider: {provider}");
        Debug.WriteLine($"[AI Request] Prompt: {prompt}");
        Debug.WriteLine($"[AI Request] Options: {JsonSerializer.Serialize(options)}");
    }
    
    public static void LogResponse(string provider, string response, TimeSpan duration)
    {
        Debug.WriteLine($"[AI Response] Provider: {provider}");
        Debug.WriteLine($"[AI Response] Duration: {duration.TotalMilliseconds}ms");
        Debug.WriteLine($"[AI Response] Response: {response}");
    }
    
    public static void LogError(string provider, Exception error)
    {
        Debug.WriteLine($"[AI Error] Provider: {provider}");
        Debug.WriteLine($"[AI Error] Error: {error.Message}");
        Debug.WriteLine($"[AI Error] Stack: {error.StackTrace}");
    }
}
```

#### Parameter Extraction Debugging
```csharp
public class ParameterExtractionDebugger
{
    public static void LogExtractionAttempt(string input, string parameterType)
    {
        Debug.WriteLine($"[Param Extract] Attempting to extract {parameterType} from: '{input}'");
    }
    
    public static void LogExtractionResult(string parameterType, object result, bool success)
    {
        var status = success ? "SUCCESS" : "FAILED";
        Debug.WriteLine($"[Param Extract] {status}: {parameterType} = {result}");
    }
    
    public static void LogPatternMatch(string pattern, string input, bool matched)
    {
        var status = matched ? "MATCH" : "NO MATCH";
        Debug.WriteLine($"[Pattern] {status}: '{pattern}' against '{input}'");
    }
}
```

## Performance Optimization

### Profiling and Monitoring

#### Performance Profiler
```csharp
public class PerformanceProfiler
{
    private readonly Dictionary<string, List<TimeSpan>> _measurements = new();
    
    public IDisposable StartMeasurement(string operationName)
    {
        return new MeasurementScope(this, operationName);
    }
    
    public void RecordMeasurement(string operationName, TimeSpan duration)
    {
        if (!_measurements.ContainsKey(operationName))
        {
            _measurements[operationName] = new List<TimeSpan>();
        }
        
        _measurements[operationName].Add(duration);
    }
    
    public PerformanceReport GenerateReport()
    {
        var report = new PerformanceReport();
        
        foreach (var (operation, measurements) in _measurements)
        {
            var stats = new OperationStats
            {
                OperationName = operation,
                Count = measurements.Count,
                TotalTime = TimeSpan.FromTicks(measurements.Sum(m => m.Ticks)),
                AverageTime = TimeSpan.FromTicks((long)measurements.Average(m => m.Ticks)),
                MinTime = measurements.Min(),
                MaxTime = measurements.Max()
            };
            
            report.Operations.Add(stats);
        }
        
        return report;
    }
    
    private class MeasurementScope : IDisposable
    {
        private readonly PerformanceProfiler _profiler;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        
        public MeasurementScope(PerformanceProfiler profiler, string operationName)
        {
            _profiler = profiler;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
        }
        
        public void Dispose()
        {
            _stopwatch.Stop();
            _profiler.RecordMeasurement(_operationName, _stopwatch.Elapsed);
        }
    }
}

// Usage
using (profiler.StartMeasurement("Intent Classification"))
{
    var intent = await ClassifyIntent(input);
}
```

### Optimization Techniques

#### Caching Strategies
```csharp
public class IntelligentCache<TKey, TValue>
{
    private readonly ConcurrentDictionary<TKey, CacheEntry<TValue>> _cache = new();
    private readonly TimeSpan _defaultExpiry;
    private readonly int _maxSize;
    
    public IntelligentCache(TimeSpan defaultExpiry, int maxSize)
    {
        _defaultExpiry = defaultExpiry;
        _maxSize = maxSize;
    }
    
    public async Task<TValue> GetOrAddAsync<TArg>(TKey key, Func<TArg, Task<TValue>> factory, TArg arg, TimeSpan? expiry = null)
    {
        if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
        {
            entry.AccessCount++;
            entry.LastAccessed = DateTime.UtcNow;
            return entry.Value;
        }
        
        var value = await factory(arg);
        var newEntry = new CacheEntry<TValue>
        {
            Value = value,
            ExpiresAt = DateTime.UtcNow.Add(expiry ?? _defaultExpiry),
            AccessCount = 1,
            LastAccessed = DateTime.UtcNow
        };
        
        _cache.AddOrUpdate(key, newEntry, (k, existing) => newEntry);
        
        // Cleanup if cache is too large
        if (_cache.Count > _maxSize)
        {
            CleanupCache();
        }
        
        return value;
    }
    
    private void CleanupCache()
    {
        var expiredKeys = _cache
            .Where(kvp => kvp.Value.IsExpired)
            .Select(kvp => kvp.Key)
            .ToList();
        
        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
        
        // If still too large, remove least recently used items
        if (_cache.Count > _maxSize)
        {
            var lruKeys = _cache
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(_cache.Count - _maxSize)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in lruKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }
}
```

#### Parallel Processing
```csharp
public class ParallelProcessor
{
    private readonly SemaphoreSlim _semaphore;
    private readonly ParallelOptions _parallelOptions;
    
    public ParallelProcessor(int maxConcurrency)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency);
        _parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxConcurrency
        };
    }
    
    public async Task<TResult[]> ProcessInParallel<TInput, TResult>(
        IEnumerable<TInput> inputs,
        Func<TInput, Task<TResult>> processor)
    {
        var tasks = inputs.Select(async input =>
        {
            await _semaphore.WaitAsync();
            try
            {
                return await processor(input);
            }
            finally
            {
                _semaphore.Release();
            }
        });
        
        return await Task.WhenAll(tasks);
    }
}

// Usage
var processor = new ParallelProcessor(Environment.ProcessorCount);
var results = await processor.ProcessInParallel(
    commands,
    async command => await ProcessSingleCommand(command)
);
```

## Deployment

### Build Configuration

#### Release Build Script
```powershell
# build-release.ps1
param(
    [string]$Version = "1.0.0",
    [string]$Configuration = "Release"
)

Write-Host "Building RhinoAI Plugin v$Version" -ForegroundColor Green

# Clean previous builds
dotnet clean --configuration $Configuration

# Restore packages
dotnet restore

# Build solution
dotnet build --configuration $Configuration --no-restore

# Run tests
dotnet test --configuration $Configuration --no-build

# Create YAK package
if (Test-Path "manifest.yml") {
    yak build --platform rhino3d-8 --version $Version
    Write-Host "YAK package created successfully" -ForegroundColor Green
} else {
    Write-Warning "manifest.yml not found. Skipping YAK package creation."
}

Write-Host "Build completed successfully!" -ForegroundColor Green
```

#### Deployment Checklist
- [ ] Update version numbers in AssemblyInfo.cs
- [ ] Update manifest.yml with new version
- [ ] Run full test suite
- [ ] Verify plugin loads in Rhino
- [ ] Test core functionality
- [ ] Update documentation
- [ ] Create release notes
- [ ] Tag release in Git
- [ ] Upload to distribution channels

### Continuous Integration

#### GitHub Actions Workflow
```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: 7.0.x
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --no-restore --configuration Release
      
    - name: Test
      run: dotnet test --no-build --configuration Release --logger trx
      
    - name: Upload test results
      uses: actions/upload-artifact@v3
      if: always()
      with:
        name: test-results
        path: "**/*.trx"
        
    - name: Create YAK package
      run: yak build --platform rhino3d-8
      
    - name: Upload artifacts
      uses: actions/upload-artifact@v3
      with:
        name: plugin-package
        path: "*.yak"
```

---

*Development Guide last updated: January 2025*
*For additional support, visit our GitHub repository or contact the development team* 