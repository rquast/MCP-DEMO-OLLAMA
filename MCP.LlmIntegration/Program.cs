using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using System.Text.Json;

namespace MCP.LlmIntegration;

class Program
{
    // Configuration to access secrets
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .Build();
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("MCP LLM Integration Demo - Simple Version");
        Console.WriteLine("==========================================");
        
        try
        {
            // Step 1: Set up the MCP client
            var mcpClient = await SetupMcpClient();
            
            // Step 2: List available tools
            await ListToolsAsync(mcpClient);
            
            // Step 3: Run the interactive demo
            await RunInteractiveDemo(mcpClient);
            
            // Clean up
            await mcpClient.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
    
    static async Task<IMcpClient> SetupMcpClient()
    {
        Console.WriteLine("Setting up MCP client...");
        
        // Path to the server executable
        string serverPath = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "..", "..", "..", "..", 
            "MCP.Server", "bin", "Debug", "net8.0", "MCP.Server"));
        
        // For Windows, add .exe extension if needed
        if (OperatingSystem.IsWindows())
        {
            serverPath += ".exe";
        }
        
        // Check if the server exists
        if (!File.Exists(serverPath))
        {
            string serverDir = Path.GetDirectoryName(serverPath) ?? "";
            throw new FileNotFoundException(
                $"Server executable not found at {serverPath}. " +
                $"Directory exists: {Directory.Exists(serverDir)}. " +
                "Please build the server project first.");
        }
        
        Console.WriteLine($"Connecting to server at: {serverPath}");
        
        // Create and return the MCP client
        var client = await McpClientFactory.CreateAsync(new()
        {
            Id = "mcp-llm-client",
            Name = "MCP LLM Integration Client",
            TransportType = "stdio",
            TransportOptions = new()
            {
                ["command"] = serverPath,
            }
        });
        
        Console.WriteLine("MCP client connected successfully.");
        return client;
    }
    
    static async Task ListToolsAsync(IMcpClient mcpClient)
    {
        Console.WriteLine("\nAvailable tools:");
        Console.WriteLine("----------------");
        
        var tools = await mcpClient.ListToolsAsync();
        
        foreach (var tool in tools)
        {
            Console.WriteLine($"- {tool.Name}: {tool.Description}");
            
            // Try to display parameter information by checking the Schema property
            try 
            {
                // Use reflection to safely access schema property if it exists
                var schemaProperty = tool.GetType().GetProperty("Schema");
                if (schemaProperty != null)
                {
                    var schema = schemaProperty.GetValue(tool);
                    if (schema != null)
                    {
                        // Try to extract parameters from schema
                        var json = JsonSerializer.Serialize(schema);
                        Console.WriteLine($"  Schema: {json}");
                    }
                }
            }
            catch
            {
                Console.WriteLine("  (Unable to display parameter information)");
            }
        }
    }
    
    static async Task RunInteractiveDemo(IMcpClient mcpClient)
    {
        Console.WriteLine("\nInteractive MCP Tool Demo");
        Console.WriteLine("=========================");
        Console.WriteLine("Type 'exit' to quit, or use the following commands:");
        Console.WriteLine("  echo <message> - Call the Echo tool");
        Console.WriteLine("  add <num1> <num2> - Call the Add tool");
        Console.WriteLine("  time - Call the GetDateTime tool");
        
        // Get tool list once
        var tools = await mcpClient.ListToolsAsync();
        
        while (true)
        {
            Console.Write("\n> ");
            string input = Console.ReadLine() ?? "";
            
            if (input.ToLower() == "exit")
                break;
            
            if (input.StartsWith("echo ", StringComparison.OrdinalIgnoreCase))
            {
                string message = input.Substring(5).Trim();
                await CallEchoToolAsync(mcpClient, message);
            }
            else if (input.StartsWith("add ", StringComparison.OrdinalIgnoreCase))
            {
                string[] parts = input.Substring(4).Trim().Split(' ');
                if (parts.Length >= 2 && 
                    double.TryParse(parts[0], out double a) && 
                    double.TryParse(parts[1], out double b))
                {
                    await CallAddToolAsync(mcpClient, a, b);
                }
                else
                {
                    Console.WriteLine("Error: Please provide two numbers for addition.");
                }
            }
            else if (input.StartsWith("time", StringComparison.OrdinalIgnoreCase))
            {
                await CallGetDateTimeToolAsync(mcpClient);
            }
            else
            {
                // Simulate an LLM choosing a tool based on the input
                Console.WriteLine("\nSimulating what an LLM would do with this input:");
                
                if (input.Contains("add") || input.Contains("sum") || input.Contains("plus"))
                {
                    Console.WriteLine("LLM detected a request for addition. It would call the Add tool.");
                    
                    // Extract numbers if possible
                    var numbers = ExtractNumbers(input);
                    if (numbers.Count >= 2)
                    {
                        await CallAddToolAsync(mcpClient, numbers[0], numbers[1]);
                    }
                    else
                    {
                        Console.WriteLine("LLM couldn't find two numbers, so it would ask for clarification.");
                    }
                }
                else if (input.Contains("time") || input.Contains("date") || input.Contains("now"))
                {
                    Console.WriteLine("LLM detected a request for date/time information. It would call the GetDateTime tool.");
                    await CallGetDateTimeToolAsync(mcpClient);
                }
                else if (input.Contains("hello") || input.Contains("hi") || input.Contains("hey"))
                {
                    Console.WriteLine("LLM detected a greeting. It would call the Echo tool with a friendly response.");
                    await CallEchoToolAsync(mcpClient, "friendly greeting");
                }
                else
                {
                    Console.WriteLine("LLM couldn't determine which tool to use. It would ask for clarification.");
                }
            }
        }
    }
    
    static async Task CallEchoToolAsync(IMcpClient mcpClient, string message)
    {
        try
        {
            Console.WriteLine($"Calling Echo tool with message: \"{message}\"");
            
            var result = await mcpClient.CallToolAsync(
                "Echo", 
                new Dictionary<string, object?>() { ["message"] = message });
            
            DisplayToolResult(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling Echo tool: {ex.Message}");
        }
    }
    
    static async Task CallAddToolAsync(IMcpClient mcpClient, double a, double b)
    {
        try
        {
            Console.WriteLine($"Calling Add tool with a={a}, b={b}");
            
            var result = await mcpClient.CallToolAsync(
                "Add", 
                new Dictionary<string, object?>() { ["a"] = a, ["b"] = b });
            
            DisplayToolResult(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling Add tool: {ex.Message}");
        }
    }
    
    static async Task CallGetDateTimeToolAsync(IMcpClient mcpClient)
    {
        try
        {
            Console.WriteLine("Calling GetDateTime tool");
            
            var result = await mcpClient.CallToolAsync(
                "GetDateTime", 
                new Dictionary<string, object?>());
            
            DisplayToolResult(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling GetDateTime tool: {ex.Message}");
        }
    }
    
    static void DisplayToolResult(ModelContextProtocol.Protocol.Types.CallToolResponse result)
    {
        Console.WriteLine("Tool Result:");
        
        if (result.Content != null && result.Content.Count > 0)
        {
            foreach (var content in result.Content)
            {
                if (content.Type == "text")
                {
                    Console.WriteLine($"  {content.Text}");
                }
                else
                {
                    Console.WriteLine($"  [Content of type {content.Type}]");
                    if (content.Data != null)
                    {
                        Console.WriteLine($"  Data: {JsonSerializer.Serialize(content.Data)}");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("  No content returned");
        }
    }
    
    static List<double> ExtractNumbers(string input)
    {
        var result = new List<double>();
        var words = input.Split(new[] { ' ', ',', '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var word in words)
        {
            if (double.TryParse(word, out double num))
            {
                result.Add(num);
            }
        }
        
        return result;
    }
}