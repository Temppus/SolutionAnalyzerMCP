using SolutionAnalyzer.Mcp.Tools;

namespace SolutionAnalyzer.Tests
{
    public class ToolTests(SolutionFixture solutionFixture) : IClassFixture<SolutionFixture>
    {
        private readonly SolutionFixture _solutionFixture = solutionFixture ?? throw new ArgumentNullException(nameof(solutionFixture));

        [Fact]
        public void Test_List_Projects_In_Solution()
        {
            var projects = SolutionTools.ListProjectsInSolution(_solutionFixture.Solution);
            Assert.NotEmpty(projects);
        }
    }
}