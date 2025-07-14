using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SolutionAnalyzer.Mcp.Utils;

namespace SolutionAnalyzer.Mcp;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateEmptyApplicationBuilder(settings: null);

        builder.Configuration
            .AddEnvironmentVariables()
            .AddCommandLine(args);

        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        builder.Logging.AddConsole(options =>
        {
            // Configure all logs to go to stderr
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        var solutionPath = builder.Configuration.GetValue<string>("SolutionPath")
                           ?? throw new InvalidOperationException("SolutionPath is not configured");

        builder.Services.AddSingleton<ISolutionAccessor>(new SolutionAccessor(solutionPath));
        builder.Services.AddHostedService<SolutionInitHostedService>();

        var app = builder.Build();

        await app.RunAsync();
    }
}