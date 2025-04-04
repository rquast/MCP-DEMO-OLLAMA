# MCP-Demo: Model Context Protocol Integration with OpenAI

A comprehensive demonstration of the Model Context Protocol (MCP) for .NET applications, showcasing how to create MCP clients and servers, and integrate them with OpenAI's LLM capabilities.

## What is MCP?

The Model Context Protocol (MCP) is an open protocol that standardizes how applications provide context to Large Language Models (LLMs). It enables secure integration between LLMs and various data sources and tools, allowing for a standardized way to extend LLM capabilities.

This demo showcases:
- An MCP server that exposes custom tools
- An MCP client that can connect to the server
- Integration with OpenAI to use these tools in conversations

## Solution Structure

```
MCP-Demo/
├── MCP.Server/        # MCP server with custom tools
├── MCP.Client/        # Simple client for testing tools
├── MCP.LlmIntegration/ # OpenAI integration with MCP tools
└── README.md          # This documentation
```

## Features

### MCP Server

Exposes several function-based tools:
- **Echo**: Returns a greeting with the input message
- **Add**: Adds two numerical values together
- **GetDateTime**: Returns the current date and time

### OpenAI Integration

- Connects to the OpenAI API using your API key
- Maintains conversation context
- Extracts tool calls from AI responses
- Executes tools through the MCP server
- Returns tool results to the user

## Getting Started

### Prerequisites

- .NET 9.0 SDK
- OpenAI API key
- A code editor like Visual Studio, VS Code, or JetBrains Rider

### Installation

1. Clone the repository
   ```bash
   git clone https://your-repository-url/MCP-Demo.git
   cd MCP-Demo
   ```

2. Build the solution
   ```bash
   dotnet build
   ```

3. Set up your OpenAI API key
   ```bash
   cd MCP.LlmIntegration
   dotnet user-secrets set "OpenAI:ApiKey" "your-api-key-here"
   ```

### Running the Demo

1. Run the LLM integration application
   ```bash
   cd MCP.LlmIntegration
   dotnet run
   ```

2. The application will:
   - Start the MCP server automatically
   - Connect to the OpenAI API
   - Present an interactive chat interface
   - Allow you to interact with MCP tools through OpenAI

## Usage Examples

Here are some examples of how to interact with the demo:

### Basic Calculation
```
You: What is 42 plus 17?

AI: I'll calculate that for you using the Add tool.
[TOOL_CALL:Add(a=42, b=17)]

Executing tool: Add
Arguments:
  a: 42
  b: 17

Tool Result:
  59
```

### Date and Time Check
```
You: What time is it right now?

AI: Let me check the current time for you.
[TOOL_CALL:GetDateTime()]

Executing tool: GetDateTime
Arguments:

Tool Result:
  Friday, April 4, 2025 3:45:21 PM
```

### Echo Test
```
You: Can you echo back the phrase "MCP is working!"

AI: I'll echo that phrase for you.
[TOOL_CALL:Echo(message="MCP is working!")]

Executing tool: Echo
Arguments:
  message: MCP is working!

Tool Result:
  hello MCP is working!
```

## Architecture

The integration works through these components:

1. **MCP Server**: Hosts tool implementations using the ModelContextProtocol.Server namespace
2. **MCP Client**: Connects to the server using stdio and provides tool execution capabilities
3. **OpenAI Client**: Handles conversations using the OpenAI Chat API
4. **Tool Extraction**: Uses regex to parse tool calls from AI responses
5. **Tool Execution**: Routes tool calls to the MCP server and returns results

## Extending the Project

### Adding New Tools

To add a new tool to the MCP server, add a new method to the DemoTools class:

```csharp
[McpServerTool, Description("Calculates the square of a number")]
public static double Square(double number) => number * number;
```

The tool will be automatically discovered and made available to the LLM.

### Customizing OpenAI Integration

You can modify the OpenAI integration by:
- Changing the model (e.g., to "gpt-3.5-turbo" for lower cost)
- Adding additional system instructions
- Implementing more complex conversation management

## Troubleshooting

Common issues:

- **Server Not Found**: Ensure the server path in the LLM integration is correct
- **API Key Issues**: Verify your OpenAI API key is correctly set in user secrets
- **Tool Execution Errors**: Check the tool parameters match what the AI is trying to send

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgements

- [Model Context Protocol](https://github.com/microsoft/modelcontextprotocol) for creating the protocol specification
- [OpenAI](https://github.com/openai/openai-dotnet) for their .NET client library
- Microsoft for the .NET framework and hosting libraries
