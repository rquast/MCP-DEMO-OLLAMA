using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace MCP.Server;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Create the application builder
        var builder = Host.CreateApplicationBuilder(args);
        
        // Configure logging to send all logs to stderr
        builder.Logging.AddConsole(consoleLogOptions =>
        {
            consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
        });
        
        // Configure MCP server services
        builder.Services
            .AddMcpServer()  // Add core MCP server services
            .WithStdioServerTransport()  // Use stdio for transport
            .WithToolsFromAssembly();  // Auto-discover tools from assembly
        
        // Build and run the host
        var host = builder.Build();
        await host.RunAsync();
    }
}

/// <summary>
/// Contains various tools that can be called through MCP
/// </summary>
[McpServerToolType]
public static class DemoTools
{
    /// <summary>
    /// Simple echo tool that returns a greeting with the input message
    /// </summary>
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
    
    /// <summary>
    /// Adds two numbers together
    /// </summary>
    [McpServerTool, Description("Adds two numbers together and returns the result.")]
    public static double Add(
        [Description("First number to add")] double a, 
        [Description("Second number to add")] double b) => a + b;
    
    /// <summary>
    /// Gets the current date and time
    /// </summary>
    [McpServerTool, Description("Returns the current date and time.")]
    public static string GetDateTime() => DateTime.Now.ToString("F");
}