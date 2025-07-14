using Microsoft.Extensions.Hosting;
using SolutionAnalyzer.Mcp.Utils;

namespace SolutionAnalyzer.Mcp
{
    internal class SolutionInitHostedService(ISolutionAccessor solutionAccessor) : IHostedService
    {
        private readonly ISolutionAccessor _solutionAccessor = solutionAccessor ?? throw new ArgumentNullException(nameof(solutionAccessor));

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _solutionAccessor.GetSolutionAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
