using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using ModelContextProtocol.Server;
using SolutionAnalyzer.Mcp.Utils;
using System.ComponentModel;
using System.Text.Json;

namespace SolutionAnalyzer.Mcp.Tools
{
    [McpServerToolType]
    public static class SolutionTools
    {
        [McpServerTool, Description("Returns projects defined in solution")]
        public static async Task<string> ListProjectsInSolutionAsync(ISolutionAccessor solutionAccessor, CancellationToken cancellationToken)
        {
            var solution = await solutionAccessor.GetSolutionAsync(cancellationToken);

            var response = SerializeToJson(solution.Projects.Select(x => new
            {
                ProjectName = x.Name,
                Kind = x.CompilationOptions?.OutputKind.ToString(),
            }));

            return response;
        }

        [McpServerTool, Description("Refresh state of the solution. Should be called when any files were changed in the solution")]
        public static async Task<string> RefreshSolutionAsync(ISolutionAccessor solutionAccessor, CancellationToken cancellationToken)
        {
            await solutionAccessor.ReloadAsync(cancellationToken);

            var response = SerializeToJson(new { Ok = true });
            return response;
        }

        [McpServerTool, Description("Get symbol references in solution")]
        public static async Task<string> FindSymbolReferencesInSolutionAsync(
            ISolutionAccessor solutionAccessor,
            [Description(".NET Type name of symbol")] string symbolTypeName,
            [Description("Optional namespace of the symbol")] string? symbolNamespace,
            CancellationToken cancellationToken)
        {
            var solution = await solutionAccessor.GetSolutionAsync(cancellationToken);
            var symbols = await GetSymbolsInSolutionsAsyncValue(solution, symbolTypeName, symbolNamespace, cancellationToken);
            var results = await FindReferencesAndFormatAsync(solution, symbols, cancellationToken);

            var formattedResults = results.Select(r => new
            {
                ReferencedSymbol = r.SymbolDisplay,
                r.Location,
                r.Line
            });

            return SerializeToJson(formattedResults);
        }

        [McpServerTool, Description("Gets the references to property accessors (get or set) defined in solution")]
        public static async Task<string> FindPropertyReferencesInSolutionAsync(
            ISolutionAccessor solutionAccessor,
            [Description(".NET Type name of symbol")] string symbolTypeName,
            [Description("Name of the property")] string propertyName,
            [Description("Optional namespace of the property symbol if there are multiple properties with that name")] string? propertyNamespace,
            [Description("Optional accessor type to find references for: 'get', 'set', or 'both'. If not specified 'both' will be used.")] string? accessorType,
            CancellationToken cancellationToken)
        {
            // Normalize accessorType
            accessorType = string.IsNullOrWhiteSpace(accessorType) ? "both" : accessorType.ToLowerInvariant();

            var solution = await solutionAccessor.GetSolutionAsync(cancellationToken);
            var namedTypes = await FindNamedTypeSymbolsAsync(solution, symbolTypeName, propertyNamespace, cancellationToken);

            // Retrieve matching property accessors
            var accessors = new List<IMethodSymbol>();
            foreach (var type in namedTypes)
            {
                var properties = type.GetMembers().OfType<IPropertySymbol>().Where(p => p.Name == propertyName);
                foreach (var property in properties)
                {
                    if ((accessorType is "get" or "both") && property.GetMethod != null)
                        accessors.Add(property.GetMethod);
                    if ((accessorType is "set" or "both") && property.SetMethod != null)
                        accessors.Add(property.SetMethod);
                }
            }

            var results = await FindReferencesAndFormatAsync(solution, accessors, cancellationToken);
            var formattedResults = results.Select(r => new
            {
                Accessor = r.SymbolDisplay,
                r.Location,
                r.Line
            });

            return SerializeToJson(formattedResults);
        }

        [McpServerTool, Description("Gets the references to a method defined in solution")]
        public static async Task<string> FindMethodReferencesInSolutionAsync(
            ISolutionAccessor solutionAccessor,
            [Description(".NET Type name of the symbol containing the method")] string symbolTypeName,
            [Description("Name of the method to find references for")] string methodName,
            [Description("Optional namespace of the symbol")] string? symbolNamespace,
            CancellationToken cancellationToken)
        {
            var solution = await solutionAccessor.GetSolutionAsync(cancellationToken);
            var namedTypes = await FindNamedTypeSymbolsAsync(solution, symbolTypeName, symbolNamespace, cancellationToken);

            // Get all methods matching the provided name
            var methodSymbols = namedTypes.SelectMany(t => t.GetMembers()
                                                               .OfType<IMethodSymbol>()
                                                               .Where(m => m.Name == methodName));

            var results = await FindReferencesAndFormatAsync(solution, methodSymbols, cancellationToken);
            var formattedResults = results.Select(r => new
            {
                Method = r.SymbolDisplay,
                r.Location,
                r.Line
            });

            return SerializeToJson(formattedResults);
        }

        [McpServerTool, Description("Gets the references to a field defined in solution")]
        public static async Task<string> FindFieldReferencesInSolutionAsync(
            ISolutionAccessor solutionAccessor,
            [Description(".NET Type name of the symbol containing the field")] string symbolTypeName,
            [Description("Name of the field to find references for")] string fieldName,
            [Description("Optional namespace of the symbol")] string? symbolNamespace,
            CancellationToken cancellationToken)
        {
            var solution = await solutionAccessor.GetSolutionAsync(cancellationToken);
            var namedTypes = await FindNamedTypeSymbolsAsync(solution, symbolTypeName, symbolNamespace, cancellationToken);

            // Get matching field symbols by name
            var fieldSymbols = namedTypes.SelectMany(t => t.GetMembers()
                                                           .OfType<IFieldSymbol>()
                                                           .Where(f => f.Name == fieldName));

            var results = await FindReferencesAndFormatAsync(solution, fieldSymbols, cancellationToken);
            var formattedResults = results.Select(r => new
            {
                Field = r.SymbolDisplay,
                r.Location,
                r.Line
            });

            return SerializeToJson(formattedResults);
        }

        /// <summary>
        /// Extracts and formats reference locations for a collection of symbols.
        /// </summary>
        private static async Task<IEnumerable<(string SymbolDisplay, string Location, int Line)>> FindReferencesAndFormatAsync(
            Solution solution,
            IEnumerable<ISymbol> symbols,
            CancellationToken cancellationToken)
        {
            var results = new HashSet<(string, string, int)>();

            foreach (var symbol in symbols.Distinct(SymbolEqualityComparer.Default))
            {
                var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken);
                foreach (var reference in references)
                {
                    foreach (var location in reference.Locations.Where(loc => loc.Location.IsInSource))
                    {
                        var filePath = location.Location.SourceTree?.FilePath ?? "";
                        var line = location.Location.GetMappedLineSpan().StartLinePosition.Line;
                        results.Add((symbol.ToDisplayString(), filePath, line));
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Searches for symbols across the solution and filters by the optional namespace.
        /// </summary>
        private static async Task<IEnumerable<INamedTypeSymbol>> FindNamedTypeSymbolsAsync(
            Solution solution,
            string typeName,
            string? namespaceName,
            CancellationToken cancellationToken)
        {
            var symbols = await GetSymbolsInSolutionsAsyncValue(solution, typeName, namespaceName, cancellationToken);
            return symbols.OfType<INamedTypeSymbol>();
        }

        /// <summary>
        /// Uses the Roslyn API to find declarations matching a type name, optionally filtering by namespace.
        /// </summary>
        private static async Task<IEnumerable<ISymbol>> GetSymbolsInSolutionsAsyncValue(
            Solution solution,
            string symbolTypeName,
            string? symbolNamespace,
            CancellationToken cancellationToken)
        {
            var symbolTasks = solution.Projects.Select(async p =>
            {
                var symbols = await SymbolFinder.FindDeclarationsAsync(
                    p,
                    symbolTypeName,
                    ignoreCase: true,
                    cancellationToken);

                return string.IsNullOrEmpty(symbolNamespace)
                    ? symbols
                    : symbols.Where(s => s.ContainingNamespace?.ToDisplayString() == symbolNamespace);
            });

            var symbolsInProjects = await Task.WhenAll(symbolTasks);
            return symbolsInProjects.SelectMany(x => x);
        }

        /// <summary>
        /// Serializes the provided object to JSON with indented formatting.
        /// </summary>
        private static string SerializeToJson(object response)
        {
            return JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }
}
