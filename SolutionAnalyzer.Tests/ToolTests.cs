using SolutionAnalyzer.Mcp.Tools;
using SolutionAnalyzer.Tests.Shared;

namespace SolutionAnalyzer.Tests
{
    public class ToolTests(SolutionFixture solutionFixture) : IClassFixture<SolutionFixture>
    {
        private readonly SolutionFixture _solutionFixture = solutionFixture ?? throw new ArgumentNullException(nameof(solutionFixture));

        [Fact]
        public void Test_List_Projects_In_Solution()
        {
            var toolResponse = SolutionTools.ListProjectsInSolution(_solutionFixture.Solution);

            const string expectedResponse = """
                                            [
                                              {
                                                "ProjectName": "SolutionAnalyzer.Mcp",
                                                "Kind": "ConsoleApplication"
                                              },
                                              {
                                                "ProjectName": "SolutionAnalyzer.Tests",
                                                "Kind": "ConsoleApplication"
                                              },
                                              {
                                                "ProjectName": "SolutionAnalyzer.Tests.Shared",
                                                "Kind": "DynamicallyLinkedLibrary"
                                              }
                                            ]
                                            """;

            Assert.Equal(expectedResponse, toolResponse);
        }

        [Fact]
        public async Task Test_Find_Symbol_References()
        {
            var toolResponse = await SolutionTools.FindSymbolReferencesInSolutionAsync(_solutionFixture.Solution,
                nameof(LookupExample), symbolNamespace: null, CancellationToken.None);
            Assert.NotEqual("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Symbol_References_Within_Namespace()
        {
            var toolResponse = await SolutionTools.FindSymbolReferencesInSolutionAsync(_solutionFixture.Solution,
                nameof(LookupExample),
                symbolNamespace: "SolutionAnalyzer.Tests.Shared", CancellationToken.None);
            Assert.NotEqual("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Symbol_References_Within_NonExisting_Namespace()
        {
            var toolResponse = await SolutionTools.FindSymbolReferencesInSolutionAsync(_solutionFixture.Solution,
                nameof(LookupExample),
                symbolNamespace: "Not.Exists.Shared", CancellationToken.None);

            Assert.Equal("[]", toolResponse);
        }
    }
}