# RhinoAI Plugin - User Guide

## Table of Contents

1. [Getting Started](#getting-started)
2. [Installation](#installation)
3. [Configuration](#configuration)
4. [Basic Usage](#basic-usage)
5. [Advanced Features](#advanced-features)
6. [Command Reference](#command-reference)
7. [Tips and Best Practices](#tips-and-best-practices)
8. [Troubleshooting](#troubleshooting)
9. [FAQ](#faq)

## Getting Started

RhinoAI is an intelligent assistant plugin for Rhino 8 that allows you to interact with your 3D models using natural language. Instead of remembering complex commands and parameters, you can simply describe what you want to do in plain English.

### What You Can Do

- **Create Geometry**: "Create a red sphere with radius 10"
- **Modify Objects**: "Move the selected objects 5 units up"
- **Boolean Operations**: "Subtract the cylinder from the box"
- **Object Management**: "Select all spheres" or "Hide the walls"
- **Complex Operations**: "Create 5 variations of this design"

### Key Benefits

- **Natural Language Interface**: No need to memorize commands
- **Context Awareness**: The AI remembers your conversation and understands references
- **Multiple AI Providers**: Choose from OpenAI, Claude, or local models
- **Intelligent Suggestions**: Get smart recommendations as you work
- **Error Prevention**: Built-in validation prevents common mistakes

## Installation

### Prerequisites

Before installing RhinoAI, ensure you have:

- **Rhino 8** (latest version recommended)
- **Windows 10/11** or **macOS 12+**
- **16 GB RAM** minimum (32 GB recommended for complex operations)
- **Internet connection** for cloud AI services
- **AI Service Account** (OpenAI or Anthropic) for cloud features

### Installation Steps

#### Method 1: YAK Package Manager (Recommended)

1. Open Rhino 8
2. Type `_PackageManager` in the command line
3. Search for "RhinoAI"
4. Click "Install"
5. Restart Rhino when prompted

#### Method 2: Manual Installation

1. Download the latest `RhinoAI.rhp` file from the releases page
2. Close Rhino if it's running
3. Copy the file to your Rhino plugins folder:
   - **Windows**: `%APPDATA%\McNeel\Rhinoceros\8.0\Plug-ins\`
   - **macOS**: `~/Library/Application Support/McNeel/Rhinoceros/8.0/Plug-ins/`
4. Start Rhino
5. The plugin should load automatically

#### Verification

To verify the installation:
1. Type `AIAssistant` in the Rhino command line
2. The AI Control Panel should appear
3. You should see the RhinoAI menu in the Tools menu

## Configuration

### Initial Setup

When you first run RhinoAI, you'll need to configure your AI providers:

1. **Open AI Control Panel**: Type `AIAssistant` or go to Tools > RhinoAI > AI Assistant
2. **Click Settings** (gear icon in the panel)
3. **Configure AI Providers**:

#### OpenAI Configuration
- **API Key**: Enter your OpenAI API key
- **Model**: Select GPT-4 (recommended) or GPT-3.5-turbo
- **Max Tokens**: Default 2048 (adjust for longer responses)

#### Anthropic Claude Configuration
- **API Key**: Enter your Anthropic API key
- **Model**: Select Claude-3-Sonnet (recommended)
- **Max Tokens**: Default 2048

#### Local Models (Optional)
- **Endpoint**: Enter your Ollama server URL (e.g., http://localhost:11434)
- **Model**: Select your preferred local model

### Getting API Keys

#### OpenAI API Key
1. Visit [OpenAI Platform](https://platform.openai.com)
2. Sign up or log in to your account
3. Navigate to API Keys section
4. Create a new API key
5. Copy and paste into RhinoAI settings

#### Anthropic API Key
1. Visit [Anthropic Console](https://console.anthropic.com)
2. Sign up or log in to your account
3. Navigate to API Keys section
4. Create a new API key
5. Copy and paste into RhinoAI settings

### Performance Settings

#### Caching
- **Enable Response Caching**: Improves performance for repeated operations
- **Cache Size**: Default 100MB (adjust based on available memory)
- **Cache Expiry**: Default 1 hour for API responses

#### Processing
- **Confidence Threshold**: Minimum confidence for intent recognition (default 0.7)
- **Fallback Mode**: What to do when enhanced processing fails
- **Parallel Processing**: Number of concurrent operations (default: CPU cores)

#### Memory Management
- **Conversation History**: Number of previous exchanges to remember (default 10)
- **Auto-cleanup**: Automatically clear old data (recommended)

## Basic Usage

### Starting a Conversation

1. **Open AI Assistant**: Type `AIAssistant` in the command line
2. **Type Your Request**: Enter natural language in the input field
3. **Press Enter**: The AI will process your request and execute the command

### Simple Commands

#### Creating Objects

```
"Create a sphere"
"Make a red box with size 10"
"Create a cylinder with radius 5 and height 20"
"Make a sphere at point 10,10,0 with radius 8"
```

#### Modifying Objects

```
"Move the sphere 5 units up"
"Scale the selected objects by 2"
"Rotate the box 45 degrees around Z axis"
"Change the color to blue"
```

#### Selection

```
"Select all spheres"
"Select the red objects"
"Select objects named 'wall'"
"Deselect everything"
```

#### Boolean Operations

```
"Union the selected objects"
"Subtract the cylinder from the box"
"Find the intersection of these shapes"
```

### Using the AI Control Panel

The AI Control Panel is your main interface for interacting with RhinoAI:

#### Input Area
- **Text Input**: Type your natural language commands here
- **Send Button**: Click to submit your command (or press Enter)
- **Voice Input**: Click microphone icon for voice commands (if available)

#### History Area
- **Command History**: See your previous commands and results
- **Clear History**: Remove old commands to save memory
- **Export History**: Save your session for reference

#### Status Area
- **Processing Indicator**: Shows when AI is working
- **Provider Status**: Which AI service is currently active
- **Error Messages**: Displays any issues or warnings

#### Settings Panel
- **AI Provider**: Switch between OpenAI, Claude, or local models
- **Performance**: Adjust caching and processing settings
- **Preferences**: Customize interface and behavior

## Advanced Features

### Context-Aware Commands

RhinoAI remembers your conversation and can understand references:

```
User: "Create a red sphere"
AI: "Created a red sphere at origin with radius 1"

User: "Make it bigger"
AI: "Scaled the sphere by factor 1.5"

User: "Move it up 10 units"
AI: "Moved the sphere 10 units in Z direction"

User: "Create another one like this but blue"
AI: "Created a blue sphere with the same size at a new location"
```

### Complex Operations

#### Multi-step Operations
```
"Create a box, then create a cylinder inside it, then subtract the cylinder"
```

#### Parametric Variations
```
"Create 5 spheres with different sizes from 1 to 10"
"Make variations of this design with different proportions"
```

#### Scene Analysis
```
"Analyze the current scene and suggest improvements"
"What objects are currently selected?"
"Show me the properties of the red objects"
```

### Batch Operations

#### Processing Multiple Objects
```
"Scale all spheres by 2"
"Change all red objects to blue"
"Move all objects on layer 'furniture' up 5 units"
```

#### Layer Management
```
"Create a new layer called 'walls'"
"Move selected objects to layer 'structure'"
"Hide all layers except 'main'"
```

### Advanced AI Features

#### Design Assistance
```
"Suggest ways to optimize this model for 3D printing"
"What's the best way to create a spiral staircase?"
"How can I make this design more structurally sound?"
```

#### Code Generation
```
"Generate a Grasshopper script to create this pattern"
"Create a Python script to automate this operation"
"Show me the RhinoScript equivalent of this command"
```

## Command Reference

### Geometry Creation Commands

| Command Pattern | Description | Example |
|----------------|-------------|---------|
| Create [shape] | Creates basic geometry | "Create a sphere" |
| Make [color] [shape] | Creates colored geometry | "Make a red box" |
| [Shape] with [parameters] | Creates with specific parameters | "Sphere with radius 10" |
| [Shape] at [location] | Creates at specific location | "Box at point 5,5,0" |

### Transformation Commands

| Command Pattern | Description | Example |
|----------------|-------------|---------|
| Move [objects] [direction] | Moves objects | "Move sphere up 5 units" |
| Scale [objects] by [factor] | Scales objects | "Scale by 2" |
| Rotate [objects] [angle] | Rotates objects | "Rotate 45 degrees" |
| Mirror [objects] [plane] | Mirrors objects | "Mirror across XY plane" |

### Selection Commands

| Command Pattern | Description | Example |
|----------------|-------------|---------|
| Select [criteria] | Selects objects | "Select all spheres" |
| Select [color] objects | Selects by color | "Select red objects" |
| Deselect [criteria] | Deselects objects | "Deselect everything" |
| Hide/Show [objects] | Controls visibility | "Hide selected objects" |

### Boolean Operation Commands

| Command Pattern | Description | Example |
|----------------|-------------|---------|
| Union [objects] | Boolean union | "Union selected objects" |
| Subtract [A] from [B] | Boolean difference | "Subtract cylinder from box" |
| Intersect [objects] | Boolean intersection | "Intersect the shapes" |
| Split [object] with [tool] | Split operation | "Split surface with curve" |

### Advanced Commands

| Command Pattern | Description | Example |
|----------------|-------------|---------|
| Analyze [object/scene] | Analysis operations | "Analyze the current scene" |
| Optimize [object] for [purpose] | Optimization suggestions | "Optimize for 3D printing" |
| Generate [variations] | Create variations | "Generate 5 variations" |
| Export [objects] as [format] | Export operations | "Export as STL" |

## Tips and Best Practices

### Writing Effective Commands

#### Be Specific
- ❌ "Make something"
- ✅ "Create a red sphere with radius 5"

#### Use Clear References
- ❌ "Move it there"
- ✅ "Move the sphere 10 units in X direction"

#### Provide Context
- ❌ "Change the color"
- ✅ "Change the selected objects to blue"

#### Use Standard Terms
- ✅ "sphere", "box", "cylinder"
- ✅ "red", "blue", "green" or "RGB(255,0,0)"
- ✅ "up", "down", "left", "right"

### Optimizing Performance

#### Use Caching
- Enable response caching for frequently used commands
- Clear cache periodically to free memory

#### Batch Operations
- Group similar operations together
- Use "select all [type]" before batch modifications

#### Context Management
- Clear conversation history for complex sessions
- Use specific names for objects you'll reference later

### Working with Large Models

#### Performance Considerations
- Process objects in smaller batches
- Use local models for better performance
- Enable parallel processing for complex operations

#### Memory Management
- Clear unnecessary objects from memory
- Use layers to organize and manage complexity
- Monitor system resources during heavy operations

### Collaboration Tips

#### Naming Conventions
- Use descriptive names for objects: "Create a sphere named 'head'"
- Follow consistent naming patterns
- Use layer names that make sense: "structure", "details", "temporary"

#### Documentation
- Export command history for project documentation
- Use comments in complex operations
- Save important configurations as presets

## Troubleshooting

### Common Issues

#### Plugin Not Loading

**Symptoms**: RhinoAI commands not available, no AI menu

**Solutions**:
1. Check Rhino version (requires Rhino 8)
2. Verify .NET 7.0 runtime is installed
3. Ensure plugin file is in correct directory
4. Check Windows/macOS permissions
5. Restart Rhino with administrator privileges

#### AI Service Connection Errors

**Symptoms**: "AI service not responding", timeout errors

**Solutions**:
1. Check internet connection
2. Verify API keys are correct and active
3. Check API service status (OpenAI/Anthropic)
4. Verify API quotas and billing
5. Try switching to alternative AI provider

#### Commands Not Recognized

**Symptoms**: "I don't understand", low confidence warnings

**Solutions**:
1. Rephrase using simpler language
2. Use more specific geometric terms
3. Break complex commands into steps
4. Check command reference for supported patterns
5. Lower confidence threshold in settings

#### Performance Issues

**Symptoms**: Slow response times, high memory usage

**Solutions**:
1. Enable response caching
2. Clear conversation history
3. Reduce parallel processing threads
4. Close unnecessary Rhino viewports
5. Use local AI models for better performance

#### Parameter Extraction Errors

**Symptoms**: Wrong values, missing parameters

**Solutions**:
1. Use explicit numeric values: "radius 5" not "small radius"
2. Specify units when ambiguous: "5 millimeters"
3. Use standard color names or RGB values
4. Provide complete coordinate information

### Debug Mode

Enable debug mode for detailed troubleshooting:

1. Open AI Control Panel settings
2. Enable "Debug Logging"
3. Reproduce the issue
4. Check log files in plugin directory
5. Report issues with log information

### Getting Help

#### Documentation
- Check this user guide for detailed information
- Review API reference for technical details
- See implementation map for architecture understanding

#### Community Support
- GitHub Issues for bug reports
- GitHub Discussions for questions
- Rhino forum for general CAD discussions

#### Professional Support
- Email support for licensed users
- Priority support for enterprise customers
- Custom training and consultation available

## FAQ

### General Questions

**Q: Do I need an internet connection to use RhinoAI?**
A: For cloud AI providers (OpenAI, Claude), yes. However, you can use local models via Ollama for offline operation.

**Q: How much do AI services cost?**
A: Costs depend on usage. OpenAI charges per token (~$0.01-0.03 per 1000 tokens). Claude has similar pricing. Local models are free but require setup.

**Q: Can I use RhinoAI with older versions of Rhino?**
A: RhinoAI requires Rhino 8 with .NET 7.0 support. It's not compatible with earlier versions.

**Q: Is my data sent to AI providers secure?**
A: Data is sent via encrypted HTTPS connections. You can use local models for complete privacy. See privacy policy for details.

### Technical Questions

**Q: Can I customize the AI responses?**
A: Yes, you can adjust confidence thresholds, enable/disable caching, and choose different AI providers with varying response styles.

**Q: How does context awareness work?**
A: The AI maintains conversation history and analyzes your current Rhino scene to understand references like "it", "the sphere", "selected objects".

**Q: Can I extend RhinoAI with custom commands?**
A: Yes, the plugin architecture supports extensions. See the developer documentation for details.

**Q: What languages are supported?**
A: Currently, English is the primary language. Other languages may work but aren't officially supported.

### Troubleshooting Questions

**Q: Why are my commands being misunderstood?**
A: Try using more specific language, standard geometric terms, and explicit parameters. Check the command reference for supported patterns.

**Q: The AI is too slow. How can I improve performance?**
A: Enable caching, use local models, reduce conversation history, and process objects in smaller batches.

**Q: Can I undo AI-generated operations?**
A: Yes, all operations use standard Rhino commands and can be undone with Ctrl+Z or the Undo command.

**Q: How do I report bugs or request features?**
A: Use GitHub Issues for bug reports and feature requests. Include detailed steps to reproduce and log files when possible.

---

*User Guide last updated: January 2025*
*Plugin Version: 2.0.0*
*For technical support, visit our GitHub repository or contact support@rhinoai.com* 