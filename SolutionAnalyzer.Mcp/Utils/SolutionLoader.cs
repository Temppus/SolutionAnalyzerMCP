using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace SolutionAnalyzer.Mcp.Utils
{
    public static class SolutionLoader
    {
        public static async Task<Solution> LoadSolutionAsync(string solutionPath)
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
