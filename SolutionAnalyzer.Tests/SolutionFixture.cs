using SolutionAnalyzer.Mcp.Utils;

namespace SolutionAnalyzer.Tests
{
    public class SolutionFixture : IAsyncLifetime
    {
        public ISolutionAccessor SolutionAccessor { get; private set; }

        public async Task InitializeAsync()
        {
            string assemblyDir = Directory.GetCurrentDirectory();
            string repoRootDir = Directory.GetParent(assemblyDir)?.Parent?.Parent?.Parent?.FullName;
            var solutionPath = Path.Combine(repoRootDir, "SolutionAnalyzer.Mcp.sln");
            SolutionAccessor = new SolutionAccessor(solutionPath);

            Assert.NotNull(SolutionAccessor);
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
