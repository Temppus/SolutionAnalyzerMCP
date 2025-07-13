using Microsoft.CodeAnalysis;
using SolutionAnalyzer.Mcp.Utils;

namespace SolutionAnalyzer.Tests
{
    public class SolutionFixture : IAsyncLifetime
    {
        public Solution Solution { get; private set; }

        public async Task InitializeAsync()
        {
            string assemblyDir = Directory.GetCurrentDirectory();
            string repoRootDir = Directory.GetParent(assemblyDir)?.Parent?.Parent?.Parent?.FullName;
            var solutionPath = Path.Combine(repoRootDir, "SolutionAnalyzer.Mcp.sln");
            Solution = await SolutionLoader.LoadSolutionAsync(solutionPath);
            Assert.NotNull(Solution);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
