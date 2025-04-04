using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenAI.Chat;

namespace MCP.LlmIntegration;

class Program
{
    // Configuration to access secrets
    private static readonly IConfiguration Configuration = new ConfigurationBuilder()
        .AddUserSecrets<Program>()
        .Build();
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("MCP-OpenAI Integration Demo");
        Console.WriteLine("===========================");
        
        try
        {
            // Step 1: Set up the MCP client
            var mcpClient = await SetupMcpClient();
            
            // Step 2: Set up the OpenAI ChatClient
            var chatClient = SetupChatClient();
            
            // Step 3: List available tools
            var tools = await ListToolsAsync(mcpClient);
            
            // Step 4: Run the chat loop with OpenAI
            await RunChatLoopWithOpenAI(mcpClient, chatClient, tools);
            
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
    
    static ChatClient SetupChatClient()
    {
        Console.WriteLine("Setting up OpenAI ChatClient...");
        
        // Get the API key from user secrets
        string? apiKey = Configuration["OpenAI:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key not found in user secrets. " +
                "Please run: dotnet user-secrets set \"OpenAI:ApiKey\" \"your-api-key\"");
        }
        
        // Create the ChatClient with model and API key
        var chatClient = new ChatClient(
            model: "gpt-4o",
            apiKey: apiKey);
        
        Console.WriteLine("OpenAI ChatClient set up successfully.");
        return chatClient;
    }
    
    static async Task<List<McpClientTool>> ListToolsAsync(IMcpClient mcpClient)
    {
        Console.WriteLine("\nAvailable tools:");
        Console.WriteLine("----------------");
        
        var tools = await mcpClient.ListToolsAsync();
        var toolList = tools.ToList();
        
        foreach (var tool in toolList)
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
        
        return toolList;
    }
    
    static async Task RunChatLoopWithOpenAI(IMcpClient mcpClient, ChatClient chatClient, List<McpClientTool> tools)
    {
        Console.WriteLine("\nChat with OpenAI (type 'exit' to quit):");
        Console.WriteLine("----------------------------------------");
        
        // Create a list to store chat messages
        var messages = new List<ChatMessage>();
        
        // Add system message explaining the available tools
        messages.Add(new SystemChatMessage(BuildSystemMessage(tools)));
        
        while (true)
        {
            // Get user input
            Console.Write("\nYou: ");
            string userInput = Console.ReadLine() ?? "";
            
            if (userInput.ToLower() == "exit")
                break;
            
            try
            {
                // Add the user's message
                messages.Add(new UserChatMessage(userInput));
                
                // Get completion from OpenAI
                var completionResult = await chatClient.CompleteChatAsync(messages);
                string responseText = completionResult.Value.Content[0].Text;
                
                // Display the response
                Console.WriteLine($"\nAI: {responseText}");
                
                // Add the assistant's response to chat history
                messages.Add(new AssistantChatMessage(responseText));
                
                // Check if the response contains a tool call
                var toolCall = ExtractToolCall(responseText);
                if (toolCall != null)
                {
                    // Execute the tool
                    await ExecuteToolCall(mcpClient, toolCall.Value.tool, toolCall.Value.args);
                }
                
                // Keep message history manageable
                if (messages.Count > 10)
                {
                    // Remove older messages but keep the system message
                    messages.RemoveRange(1, 2);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
    
    static string BuildSystemMessage(List<McpClientTool> tools)
    {
        var message = "You are a helpful AI assistant with access to the following tools:\n\n";
        
        foreach (var tool in tools)
        {
            message += $"- {tool.Name}: {tool.Description}\n";
            
            // Try to add parameter information if available
            try
            {
                var schemaProperty = tool.GetType().GetProperty("Schema");
                if (schemaProperty != null)
                {
                    var schema = schemaProperty.GetValue(tool);
                    if (schema != null)
                    {
                        message += $"  Parameters: {JsonSerializer.Serialize(schema)}\n";
                    }
                }
            }
            catch
            {
                // Skip parameter info if not available
            }
        }
        
        message += "\nWhen a user asks you something that requires one of these tools, tell them " +
                  "you will use the appropriate tool, and then include a tool call using this format:\n" +
                  "[TOOL_CALL:ToolName(param1=value1, param2=value2)]\n\n" +
                  "For example, if the user asks you to add two numbers, you might say:\n" +
                  "I'll calculate that for you using the Add tool.\n" +
                  "[TOOL_CALL:Add(a=5, b=3)]\n\n" +
                  "After a tool call, I will execute the tool and show you the result.";
        
        return message;
    }
    
    static (string tool, Dictionary<string, object?> args)? ExtractToolCall(string text)
    {
        // Look for the tool call pattern
        var toolCallMatch = Regex.Match(
            text, 
            @"\[TOOL_CALL:([a-zA-Z0-9_]+)\(([^)]*)\)\]");
        
        if (!toolCallMatch.Success)
            return null;
        
        // Extract the tool name and arguments
        string toolName = toolCallMatch.Groups[1].Value;
        string argString = toolCallMatch.Groups[2].Value;
        
        // Parse the arguments
        var args = new Dictionary<string, object?>();
        var argMatches = Regex.Matches(
            argString, 
            @"([a-zA-Z0-9_]+)=([^,]*)(?:,|$)");
        
        foreach (Match match in argMatches)
        {
            string paramName = match.Groups[1].Value;
            string paramValue = match.Groups[2].Value.Trim();
            
            // Try to parse the value
            if (double.TryParse(paramValue, out double numValue))
            {
                args[paramName] = numValue;
            }
            else if (bool.TryParse(paramValue, out bool boolValue))
            {
                args[paramName] = boolValue;
            }
            else
            {
                // Remove quotes if present
                if (paramValue.StartsWith("\"") && paramValue.EndsWith("\""))
                {
                    paramValue = paramValue.Substring(1, paramValue.Length - 2);
                }
                else if (paramValue.StartsWith("'") && paramValue.EndsWith("'"))
                {
                    paramValue = paramValue.Substring(1, paramValue.Length - 2);
                }
                
                args[paramName] = paramValue;
            }
        }
        
        return (toolName, args);
    }
    
    static async Task ExecuteToolCall(IMcpClient mcpClient, string toolName, Dictionary<string, object?> args)
    {
        Console.WriteLine($"\nExecuting tool: {toolName}");
        Console.WriteLine("Arguments:");
        foreach (var arg in args)
        {
            Console.WriteLine($"  {arg.Key}: {arg.Value}");
        }
        
        try
        {
            // Call the tool
            var result = await mcpClient.CallToolAsync(toolName, args);
            
            // Display the result
            Console.WriteLine("\nTool Result:");
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing tool: {ex.Message}");
        }
    }
}