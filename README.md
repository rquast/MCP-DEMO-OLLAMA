# MCP-Demo: Model Context Protocol Demo

A simple demonstration of the Model Context Protocol (MCP) for .NET applications. This project showcases how to create MCP clients and servers that can be used to extend the capabilities of Large Language Models.

## What is MCP?

The Model Context Protocol (MCP) is an open protocol that standardizes how applications provide context to Large Language Models (LLMs). It enables secure integration between LLMs and various data sources and tools.

This demo implements:
- An MCP server that exposes tools
- An MCP client that can connect to the server
- Example tools for demonstration purposes

## Project Structure

```
MCP-Demo/
├── MCP.Server/        # MCP server implementation
├── MCP.Client/        # MCP client for testing
└── README.md          # This file
```

## Features

- **Example Tools**:
  - `Echo`: Echoes a message back to the client
  - `Add`: Adds two numbers together
  - `GetDateTime`: Returns the current date and time

- **Interactive Client**:
  - Lists available tools and their descriptions
  - Allows testing tools through a command-line menu
  - Displays tool results in a formatted manner

## Getting Started

### Prerequisites

- .NET 8.0 SDK or newer
- A code editor like Visual Studio, VS Code, or JetBrains Rider

### Building the Solution

1. Clone the repository
   ```bash
   git clone https://your-repository-url/MCP-Demo.git
   cd MCP-Demo
   ```

2. Build both projects
   ```bash
   dotnet build
   ```

### Running the Demo

1. The client is designed to start the server automatically
   ```bash
   cd MCP.Client
   dotnet run
   ```

2. Use the interactive menu to test the tools:
   - Type `1` for Echo
   - Type `2` for Add
   - Type `3` for GetDateTime
   - Type `4` to Exit

## How It Works

### Server Side

The server uses the .NET hosting framework to set up an MCP server that:
- Listens for tool invocation requests via standard input/output
- Registers tools from the current assembly
- Defines tools using attributes

```csharp
[McpServerToolType]
public static class DemoTools
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
    
    // More tools...
}
```

### Client Side

The client:
- Locates and launches the server executable
- Connects to it via the stdio transport
- Lists available tools and their parameters
- Allows calling tools with user-provided parameters

## LLM Integration

An example of integrating with an LLM is provided in the comments. This shows how to:
- Connect to an MCP server
- Convert MCP tools to LLM functions
- Use these functions in an LLM interaction

## Next Steps

Some suggestions for extending this demo:

1. **Add Authentication**: Implement authentication between client and server
2. **Add More Complex Tools**: Create tools that perform more advanced operations
3. **Implement Error Handling**: Add more robust error handling and reporting
4. **Create a GUI**: Build a graphical interface for interacting with the tools
5. **Add Configuration**: Move hardcoded values to configuration files

## Troubleshooting

If you encounter issues:

- **Server Not Found**: Ensure the server path in the client is correct
- **Build Issues**: Make sure both projects are built correctly
- **Path Problems**: Verify file paths are correct for your operating system

## License

[MIT License](LICENSE)

## Acknowledgements

- [Model Context Protocol](https://github.com/modelcontextprotocol/mcp) for creating the protocol
- Microsoft for the .NET framework and hosting libraries
