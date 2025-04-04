using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol.Types;
using System.Text.Json;

namespace MCP.Client;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting MCP Client...");
        Console.WriteLine("Connecting to MCP server...");
        
        try
        {
            // Path to the server executable - adjust this based on your setup
            string serverPath = Path.Combine("..", "MCP.Server", "bin", "Debug", "net9.0", "MCP.Server");
            
            // For Windows, add .exe extension if needed
            if (OperatingSystem.IsWindows())
            {
                serverPath += ".exe";
            }
            
            // Make sure path is valid
            if (!File.Exists(serverPath))
            {
                Console.WriteLine($"Error: Server executable not found at {Path.GetFullPath(serverPath)}");
                Console.WriteLine("Please build the server project first or adjust the path.");
                return;
            }
            
            // Create the MCP client
            var client = await McpClientFactory.CreateAsync(new()
            {
                Id = "mcp-demo-client",
                Name = "MCP Demo Client",
                TransportType = "stdio",
                TransportOptions = new()
                {
                    ["command"] = serverPath,
                }
            });
            
            Console.WriteLine("Connected to server successfully!");
            
            // List available tools
            Console.WriteLine("\nListing available tools:");
            var tools = await client.ListToolsAsync();
            foreach (var tool in tools)
            {
                Console.WriteLine($"- {tool.Name}: {tool.Description}");
                
                // Try to display parameter information if available
                // Use reflection to safely check what properties are available
                var schema = tool.GetType().GetProperty("Schema")?.GetValue(tool);
                if (schema != null)
                {
                    try
                    {
                        Console.WriteLine("  Parameters (from schema):");
                        var schemaJson = JsonSerializer.Serialize(schema);
                        Console.WriteLine($"    {schemaJson}");
                    }
                    catch
                    {
                        Console.WriteLine("  (Unable to display parameter info)");
                    }
                }
            }
            
            // Display menu
            while (true)
            {
                Console.WriteLine("\nChoose a tool to call:");
                Console.WriteLine("1. Echo");
                Console.WriteLine("2. Add");
                Console.WriteLine("3. GetDateTime");
                Console.WriteLine("4. Exit");
                
                Console.Write("\nEnter your choice (1-4): ");
                var choice = Console.ReadLine();
                
                if (choice == "4") break;
                
                switch (choice)
                {
                    case "1":
                        await CallEchoTool(client);
                        break;
                    case "2":
                        await CallAddTool(client);
                        break;
                    case "3":
                        await CallGetDateTimeTool(client);
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
            
            // Dispose the client
            await client.DisposeAsync();
            Console.WriteLine("Client disposed. Exiting...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
    
    static async Task CallEchoTool(IMcpClient client)
    {
        Console.Write("Enter a message to echo: ");
        var message = Console.ReadLine() ?? "";
        
        var result = await client.CallToolAsync(
            "Echo",
            new Dictionary<string, object?>() { ["message"] = message });
        
        DisplayResults(result);
    }
    
    static async Task CallAddTool(IMcpClient client)
    {
        Console.Write("Enter first number: ");
        if (!double.TryParse(Console.ReadLine(), out double a))
        {
            Console.WriteLine("Invalid number. Using 0.");
            a = 0;
        }
        
        Console.Write("Enter second number: ");
        if (!double.TryParse(Console.ReadLine(), out double b))
        {
            Console.WriteLine("Invalid number. Using 0.");
            b = 0;
        }
        
        var result = await client.CallToolAsync(
            "Add",
            new Dictionary<string, object?>() { ["a"] = a, ["b"] = b });
        
        DisplayResults(result);
    }
    
    static async Task CallGetDateTimeTool(IMcpClient client)
    {
        var result = await client.CallToolAsync(
            "GetDateTime",
            new Dictionary<string, object?>());
        
        DisplayResults(result);
    }
    
    static void DisplayResults(ModelContextProtocol.Protocol.Types.CallToolResponse result)
    {
        Console.WriteLine("\nResult:");
        if (result.Content != null)
        {
            foreach (var content in result.Content)
            {
                if (content.Type == "text")
                {
                    Console.WriteLine($"  Text: {content.Text}");
                }
                else
                {
                    Console.WriteLine($"  Content type: {content.Type}");
                    if (content.Data != null)
                    {
                        Console.WriteLine($"  Data: {JsonSerializer.Serialize(content.Data)}");
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("  No content returned.");
        }
    }
}