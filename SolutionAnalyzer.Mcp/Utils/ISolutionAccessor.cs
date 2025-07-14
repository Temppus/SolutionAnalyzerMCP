using Microsoft.CodeAnalysis;

namespace SolutionAnalyzer.Mcp.Utils
{
    public interface ISolutionAccessor
    {
        Task<Solution> GetSolutionAsync(CancellationToken cancellationToken);
        Task<Solution> ReloadAsync(CancellationToken cancellationToken);
    }
}
