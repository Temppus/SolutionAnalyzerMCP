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
                nameof(LookupExample),
                symbolNamespace: null,
                CancellationToken.None);

            Assert.NotEqual("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Symbol_References_Within_Namespace()
        {
            var toolResponse = await SolutionTools.FindSymbolReferencesInSolutionAsync(_solutionFixture.Solution,
                nameof(LookupExample),
                symbolNamespace: "SolutionAnalyzer.Tests.Shared",
                CancellationToken.None);

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

        [Theory]
        [InlineData("get")]
        [InlineData("set")]
        [InlineData("both")]
        [InlineData("")]
        [InlineData(null)]
        public async Task Test_Find_Property_References_With_AccessorType(string accessorType)
        {
            var toolResponse = await SolutionTools.FindPropertyReferencesInSolutionAsync(_solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample),
                propertyName: nameof(LookupExample.X),
                accessorType: accessorType,
                propertyNamespace: null,
                cancellationToken: CancellationToken.None);

            Assert.NotEqual("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Property_References_Not_Present()
        {
            var toolResponse = await SolutionTools.FindPropertyReferencesInSolutionAsync(_solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample),
                propertyName: "ABC",
                accessorType: null,
                propertyNamespace: null,
                cancellationToken: CancellationToken.None);

            Assert.Equal("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Property_References_Namespace_Not_Present()
        {
            var toolResponse = await SolutionTools.FindPropertyReferencesInSolutionAsync(_solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample),
                propertyName: nameof(LookupExample.X),
                accessorType: null,
                propertyNamespace: "a.b.c",
                cancellationToken: CancellationToken.None);

            Assert.Equal("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Property_References_SymbolTypeName_Not_Present()
        {
            var toolResponse = await SolutionTools.FindPropertyReferencesInSolutionAsync(_solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample) + "abc",
                propertyName: nameof(LookupExample.X),
                accessorType: null,
                propertyNamespace: null,
                cancellationToken: CancellationToken.None);

            Assert.Equal("[]", toolResponse);
        }

        [Theory]
        [InlineData("MyPrivateMethod")]
        [InlineData("MyStaticMethod")]
        [InlineData(nameof(LookupExample.MyMethod))]
        public async Task Test_Find_Method_References(string methodName)
        {
            var toolResponse = await SolutionTools.FindMethodReferencesInSolutionAsync(
                solution: _solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample),
                methodName: methodName,
                symbolNamespace: "SolutionAnalyzer.Tests.Shared",
                cancellationToken: CancellationToken.None);

            Assert.NotEqual("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Method_References_Not_Present()
        {
            var toolResponse = await SolutionTools.FindMethodReferencesInSolutionAsync(
                solution: _solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample),
                methodName: "NonExistentMethod",
                symbolNamespace: null,
                cancellationToken: CancellationToken.None);

            Assert.Equal("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Method_References_Namespace_Not_Present()
        {
            var toolResponse = await SolutionTools.FindMethodReferencesInSolutionAsync(
                solution: _solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample),
                methodName: nameof(LookupExample.MyMethod),
                symbolNamespace: "Invalid.Namespace",
                cancellationToken: CancellationToken.None);

            Assert.Equal("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Method_References_SymbolType_Not_Present()
        {
            var toolResponse = await SolutionTools.FindMethodReferencesInSolutionAsync(
                solution: _solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample) + "XYZ",
                methodName: nameof(LookupExample.MyMethod),
                symbolNamespace: null,
                cancellationToken: CancellationToken.None);

            Assert.Equal("[]", toolResponse);
        }


        [Theory]
        [InlineData(nameof(LookupExample.ConstName))]
        [InlineData(nameof(LookupExample.StaticBool))]
        public async Task Test_Find_Field_References(string fieldName)
        {
            var toolResponse = await SolutionTools.FindFieldReferencesInSolutionAsync(
                solution: _solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample),
                fieldName: fieldName,
                symbolNamespace: "SolutionAnalyzer.Tests.Shared",
                cancellationToken: CancellationToken.None);

            Assert.NotEqual("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Field_References_Not_Present()
        {
            var toolResponse = await SolutionTools.FindFieldReferencesInSolutionAsync(
                solution: _solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample),
                fieldName: "NonExistentField",
                symbolNamespace: null,
                cancellationToken: CancellationToken.None);

            Assert.Equal("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Field_References_Namespace_Not_Present()
        {
            var toolResponse = await SolutionTools.FindFieldReferencesInSolutionAsync(
                solution: _solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample),
                fieldName: nameof(LookupExample.ConstName),
                symbolNamespace: "Invalid.Namespace",
                cancellationToken: CancellationToken.None);

            Assert.Equal("[]", toolResponse);
        }

        [Fact]
        public async Task Test_Find_Field_References_SymbolType_Not_Present()
        {
            var toolResponse = await SolutionTools.FindFieldReferencesInSolutionAsync(
                solution: _solutionFixture.Solution,
                symbolTypeName: nameof(LookupExample) + "XYZ",
                fieldName: nameof(LookupExample.ConstName),
                symbolNamespace: null,
                cancellationToken: CancellationToken.None);

            Assert.Equal("[]", toolResponse);
        }
    }
}