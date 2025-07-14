using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace SolutionAnalyzer.Mcp.Utils
{
    public class SolutionAccessor : ISolutionAccessor
    {
        private readonly string _solutionPath;

        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private Lazy<Task<Solution>> _solutionTask;

        public SolutionAccessor(string solutionPath)
        {
            _solutionPath = solutionPath ?? throw new ArgumentNullException(nameof(solutionPath));
            _solutionTask = new Lazy<Task<Solution>>(() => LoadSolutionAsync(_solutionPath));
        }

        public async Task<Solution> GetSolutionAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                var solution = await _solutionTask.Value;
                return solution;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<Solution> ReloadAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                _solutionTask = new Lazy<Task<Solution>>(() => LoadSolutionAsync(_solutionPath));
                var solution = await _solutionTask.Value;
                return solution;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static async Task<Solution> LoadSolutionAsync(string solutionPath)
        {
            string workspaceLoadError = string.Empty;
            var workspace = MSBuildWorkspace.Create();
            workspace.WorkspaceFailed += (_, e) =>
            {
                if (e.Diagnostic.Kind == WorkspaceDiagnosticKind.Failure)
                {
                    workspaceLoadError = e.Diagnostic.Message;
                }
            };

            var solution = await workspace.OpenSolutionAsync(solutionPath);

            if (solution == null)
            {
                throw new InvalidOperationException(workspaceLoadError);
            }

            return solution;
        }
    }
}
