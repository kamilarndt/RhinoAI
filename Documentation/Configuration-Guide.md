# RhinoAI Configuration Guide

## 🚀 Quick Setup

Your RhinoAI plugin is successfully running! Follow these steps to complete the configuration:

## 📋 Current Status
✅ **MCP Server**: Running on http://localhost:5005/  
✅ **Plugin Loading**: Successful  
⚠️ **API Keys**: Need configuration  

## 🔑 Setting Up API Keys

### Method 1: Using Rhino Commands (Recommended)

1. **Set OpenAI API Key**:
   ```
   RhinoAISetOpenAIKey
   ```
   Enter your OpenAI API key when prompted.

2. **Set Claude API Key**:
   ```
   RhinoAISetClaudeKey
   ```
   Enter your Anthropic Claude API key when prompted.

### Method 2: Using Configuration Files

The plugin stores configuration in:
```
%APPDATA%\RhinoAI\config.json
%APPDATA%\RhinoAI\config.encrypted
```

**Encrypted storage** protects your API keys using Windows Data Protection API.

## 🔧 Available Settings

### OpenAI Configuration
- **Model**: `gpt-4` (default), `gpt-3.5-turbo`, `gpt-4-turbo`
- **Max Tokens**: `4000` (default)
- **Temperature**: `0.7` (default, range: 0.0-2.0)

### Claude Configuration  
- **Model**: `claude-3-sonnet-20240229` (default), `claude-3-opus-20240229`
- **Max Tokens**: `4000` (default)

### MCP Configuration
- **Server URL**: `http://localhost:5005/` (default)
- **Port**: `5005` (default)
- **Enabled**: `true` (default)

## 🎛️ Available Commands

| Command | Description |
|---------|-------------|
| `RhinoAI` | Open AI Control Panel |
| `RhinoAIAssistant` | Start AI Assistant |
| `RhinoAISetOpenAIKey` | Configure OpenAI API key |
| `RhinoAISetClaudeKey` | Configure Claude API key |
| `RhinoAITest` | Test AI functionality |

## 🧪 Testing Your Setup

1. **Test Basic Functionality**:
   ```
   RhinoAITest
   ```

2. **Test Natural Language Commands**:
   ```
   RhinoAI
   ```
   Then try: "Create a sphere with radius 5"

3. **Test MCP Connection**:
   The MCP server should be visible in Cursor's MCP panel if properly configured.

## 🔍 Troubleshooting

### Common Issues

1. **"API key not configured"**
   - Solution: Set API keys using the commands above

2. **MCP Server not responding**
   - Check if port 5005 is available
   - Restart Rhino if needed

3. **Commands not recognized**
   - Verify plugin is loaded: Check Rhino's Plugin Manager

### Debug Information

Enable detailed logging by setting:
```json
{
  "Logging": {
    "Level": "Debug",
    "EnableConsole": true
  }
}
```

## 🔐 Security Notes

- API keys are encrypted using Windows DPAPI
- Keys are stored per-user and cannot be accessed by other users
- Sensitive data logging is disabled by default
- Configuration files are stored in user's AppData folder

## 🚀 Getting Started

1. **Set your API keys** (see methods above)
2. **Open the AI Panel**: Run `RhinoAI` command
3. **Try natural language**: "Create a box 10x10x10"
4. **Explore features**: Check the AI Control Panel for more options

## 🆘 Need Help?

- Check the logs in `%APPDATA%\RhinoAI\logs\`
- Use `RhinoAITest` to verify setup
- Restart Rhino after configuration changes 