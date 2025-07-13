using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace SolutionAnalyzer.Mcp.Tools
{
    [McpServerToolType]
    public static class SolutionTools
    {
        [McpServerTool, Description("Returns projects defined in solution")]
        public static string ListProjectsInSolution(Solution solution)
        {
            var response = SerializeToJson(solution.Projects.Select(x => new
            {
                ProjectName = x.Name,
                Kind = x.CompilationOptions?.OutputKind.ToString(),
            }));

            return response;
        }

        [McpServerTool, Description("Get symbol references in solution")]
        public static async Task<string> FindSymbolReferencesInSolutionAsync(Solution solution,

            [Description(".NET Type name of symbol")]
            string symbolTypeName,

            [Description("Optional namespace of the symbol")]
            string? symbolNamespace,

            CancellationToken cancellationToken)
        {
            var symbolsInSolution = await GetSymbolsInSolutionsAsyncValue(solution, symbolTypeName, symbolNamespace, cancellationToken);

            var symbolReferences = new List<object>();

            foreach (var symbolInProject in symbolsInSolution)
            {
                var references = await SymbolFinder.FindReferencesAsync(symbolInProject, solution, cancellationToken);

                foreach (var referencedSymbol in references.Where(x => x.Locations.Any()))
                {
                    symbolReferences.Add(new
                    {
                        ReferencedSymbol = referencedSymbol.Definition.ToDisplayString(),
                        Locations = referencedSymbol.Locations.Select(r => new
                        {
                            Location = r.Location.IsInSource ? r.Location.SourceTree?.FilePath : r.Location.ToString(),
                            Line = r.Location.GetMappedLineSpan().StartLinePosition.Line
                        })
                    });
                }
            }

            return SerializeToJson(symbolReferences);
        }

        [McpServerTool, Description("Gets the references to property accessors (get or set) defined in solution")]
        public static async Task<string> FindPropertyReferencesInSolutionAsync(Solution solution,

            [Description(".NET Type name of symbol")]
            string symbolTypeName,

            [Description("Name of the property")] string propertyName,

            [Description("Optional namespace of the property symbol if there are multiple properties with that name")]
            string? propertyNamespace,

            [Description("Optional accessor type to find references for: 'get', 'set', or 'both'. If not specified 'both' will be used.")]
            string? accessorType,

            CancellationToken cancellationToken)
        {
            accessorType = string.IsNullOrWhiteSpace(accessorType) ? "both" : accessorType.ToLowerInvariant();

            var matchingPropertyAccessors = new List<IMethodSymbol>();

            // First find all symbols matching the type name
            var symbols = await GetSymbolsInSolutionsAsyncValue(solution, symbolTypeName, propertyNamespace, cancellationToken);

            foreach (var symbol in symbols.OfType<INamedTypeSymbol>())
            {
                var matchingProperties = symbol.GetMembers().OfType<IPropertySymbol>()
                    .Where(p => p.Name == propertyName);

                foreach (var property in matchingProperties)
                {
                    if (accessorType is "get" or "both" && property.GetMethod != null)
                    {
                        matchingPropertyAccessors.Add(property.GetMethod);
                    }

                    if (accessorType is "set" or "both" && property.SetMethod != null)
                    {
                        matchingPropertyAccessors.Add(property.SetMethod);
                    }
                }
            }

            // HashSet to eliminate duplicates
            var referenceResults = new HashSet<(string Accessor, string Location, int Line)>();

            foreach (var accessor in matchingPropertyAccessors.Distinct(SymbolEqualityComparer.Default))
            {
                var references = await SymbolFinder.FindReferencesAsync(accessor, solution, cancellationToken);

                foreach (var reference in references)
                {
                    foreach (var location in reference.Locations.Where(loc => loc.Location.IsInSource))
                    {
                        var filePath = location.Location.SourceTree?.FilePath ?? "";
                        var line = location.Location.GetMappedLineSpan().StartLinePosition.Line;

                        referenceResults.Add((accessor.ToDisplayString(), filePath, line));
                    }
                }
            }

            var resultObjects = referenceResults.Select(r => new
            {
                r.Accessor,
                r.Location,
                r.Line
            });

            return SerializeToJson(resultObjects);
        }

        [McpServerTool, Description("Gets the references to a method defined in solution")]
        public static async Task<string> FindMethodReferencesInSolutionAsync(Solution solution,

            [Description(".NET Type name of the symbol containing the method")]
            string symbolTypeName,

            [Description("Name of the method to find references for")]
            string methodName,

            [Description("Optional namespace of the symbol")]
            string? symbolNamespace,

            CancellationToken cancellationToken)
        {
            var matchingMethods = new List<IMethodSymbol>();

            // Get matching type symbols
            var symbols = await GetSymbolsInSolutionsAsyncValue(solution, symbolTypeName, symbolNamespace, cancellationToken);

            foreach (var symbol in symbols.OfType<INamedTypeSymbol>())
            {
                // Get methods by name
                var methods = symbol.GetMembers()
                    .OfType<IMethodSymbol>()
                    .Where(m => m.Name == methodName);

                matchingMethods.AddRange(methods);
            }

            var referenceResults = new HashSet<(string Method, string Location, int Line)>();

            foreach (var method in matchingMethods.Distinct(SymbolEqualityComparer.Default))
            {
                var references = await SymbolFinder.FindReferencesAsync(method, solution, cancellationToken);

                foreach (var reference in references)
                {
                    foreach (var location in reference.Locations.Where(loc => loc.Location.IsInSource))
                    {
                        var filePath = location.Location.SourceTree?.FilePath ?? "";
                        var line = location.Location.GetMappedLineSpan().StartLinePosition.Line;

                        referenceResults.Add((method.ToDisplayString(), filePath, line));
                    }
                }
            }

            var resultObjects = referenceResults.Select(r => new
            {
                r.Method,
                r.Location,
                r.Line
            });

            return SerializeToJson(resultObjects);
        }

        [McpServerTool, Description("Gets the references to a field defined in solution")]
        public static async Task<string> FindFieldReferencesInSolutionAsync(Solution solution,

            [Description(".NET Type name of the symbol containing the field")]
            string symbolTypeName,

            [Description("Name of the field to find references for")]
            string fieldName,

            [Description("Optional namespace of the symbol")]
            string? symbolNamespace,

            CancellationToken cancellationToken)
        {
            var matchingFields = new List<IFieldSymbol>();

            // Get matching type symbols
            var symbols = await GetSymbolsInSolutionsAsyncValue(solution, symbolTypeName, symbolNamespace, cancellationToken);

            foreach (var symbol in symbols.OfType<INamedTypeSymbol>())
            {
                // Get fields by name
                var fields = symbol.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(f => f.Name == fieldName);

                matchingFields.AddRange(fields);
            }

            var referenceResults = new HashSet<(string Field, string Location, int Line)>();

            foreach (var field in matchingFields.Distinct(SymbolEqualityComparer.Default))
            {
                var references = await SymbolFinder.FindReferencesAsync(field, solution, cancellationToken);

                foreach (var reference in references)
                {
                    foreach (var location in reference.Locations.Where(loc => loc.Location.IsInSource))
                    {
                        var filePath = location.Location.SourceTree?.FilePath ?? "";
                        var line = location.Location.GetMappedLineSpan().StartLinePosition.Line;

                        referenceResults.Add((field.ToDisplayString(), filePath, line));
                    }
                }
            }

            var resultObjects = referenceResults.Select(r => new
            {
                r.Field,
                r.Location,
                r.Line
            });

            return SerializeToJson(resultObjects);
        }

        private static async Task<IEnumerable<ISymbol>> GetSymbolsInSolutionsAsyncValue(Solution solution, string symbolTypeName,
            string? symbolNamespace, CancellationToken cancellationToken)
        {
            var symbolTasks = solution.Projects.Select(async p =>
            {
                var symbols = await SymbolFinder.FindDeclarationsAsync(p,
                    symbolTypeName, ignoreCase: true,
                    cancellationToken);

                return string.IsNullOrEmpty(symbolNamespace)
                    ? symbols
                    : symbols.Where(s => s.ContainingNamespace.ToDisplayString() == symbolNamespace);
            });

            var symbolsInProject = await Task.WhenAll(symbolTasks);
            return symbolsInProject.SelectMany(x => x);
        }

        private static string SerializeToJson(object response)
        {
            var projects = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            return projects;
        }
    }
}
