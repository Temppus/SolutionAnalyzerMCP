using System.ComponentModel;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using ModelContextProtocol.Server;

namespace SolutionAnalyzer.Mcp.Tools
{
    [McpServerToolType]
    public static class SolutionTools
    {
        [McpServerTool, Description("Gets the projects defined in solution")]
        public static string ListProjectsInSolution(Solution solution)
        {
            var response = SerializeToJson(solution.Projects.Select(x => new
            {
                ProjectName = x.Name,
                Kind = x.CompilationOptions?.OutputKind.ToString(),
            }));

            return response;
        }

        [McpServerTool, Description("Gets the projects defined in solution")]
        public static async Task<string> GetSymbolReferencesInSolution(Solution solution,

            [Description("ProjectName defined in solution")]
            string projectName,

            [Description(".NET Type name of symbol (without namespace)")]
            string symbolTypeName,

            [Description("Optional namespace of the symbol if there are multiple symbols with that name")]
            string? symbolNamespace,

            CancellationToken cancellationToken)
        {
            var project = solution.Projects.SingleOrDefault(p => p.Name == projectName);

            if (project == null)
            {
                throw new ArgumentException($"No project with name {projectName} found");
            }

            var symbols = (await SymbolFinder.FindDeclarationsAsync(project,
                symbolTypeName, ignoreCase: true,
                cancellationToken)).ToArray();

            if (!string.IsNullOrEmpty(symbolNamespace))
            {
                symbols = symbols.Where(s => s.ContainingNamespace.Name == symbolNamespace).ToArray();
            }

            if (symbols.Length == 0)
            {
                throw new ArgumentException($"No symbol with name {symbolTypeName} found in project {projectName}");
            }

            if (symbols.Length > 1)
            {
                throw new ArgumentException($"Multiple symbols found {string.Join(",", symbols.Select(x => x.Name))} in project {projectName}");
            }

            var symbol = symbols[0];

            var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken);

            var symbolReferences = new List<object>();

            foreach (var referencedSymbol in references.Where(x => x.Locations.Any()))
            {
                symbolReferences.Add(new
                {
                    ReferencedSymbol = referencedSymbol.Definition.ToDisplayString(),
                    Locations = referencedSymbol.Locations.Select(r => new
                    {
                        Location = r.Location.ToString()
                    })
                });
            }

            return SerializeToJson(symbolReferences);
        }

        [McpServerTool, Description("Gets the references to property accessors (get or set) defined in solution")]
        public static async Task<string> GetPropertyAccessorReferencesInSolution(Solution solution,

            [Description("ProjectName defined in solution")]
            string projectName,

            [Description(".NET Type name of the property (without namespace)")]
            string propertyName,

            [Description("Optional namespace of the property if there are multiple properties with that name")]
            string? propertyNamespace,

            [Description("Accessor type to find references for: 'get', 'set', or 'both'")]
            string accessorType,

            CancellationToken cancellationToken)
        {
            // Validate accessor type
            if (accessorType != "get" && accessorType != "set" && accessorType != "both")
            {
                throw new ArgumentException("Accessor type must be 'get', 'set', or 'both'");
            }

            // Find the project
            var project = solution.Projects.SingleOrDefault(p => p.Name == projectName);
            if (project == null)
            {
                throw new ArgumentException($"No project with name {projectName} found");
            }

            // Find property declarations
            var symbols = (await SymbolFinder.FindDeclarationsAsync(project, propertyName, ignoreCase: true, cancellationToken))
                .OfType<IPropertySymbol>()
                .ToArray();

            // Filter by namespace if provided
            if (!string.IsNullOrEmpty(propertyNamespace))
            {
                symbols = symbols.Where(s => s.ContainingNamespace.Name == propertyNamespace).ToArray();
            }

            if (symbols.Length == 0)
            {
                throw new ArgumentException($"No property with name {propertyName} found in project {projectName}");
            }

            if (symbols.Length > 1)
            {
                throw new ArgumentException($"Multiple properties found {string.Join(",", symbols.Select(x => x.Name))} in project {projectName}");
            }

            var propertySymbol = symbols[0];
            var accessorSymbols = new List<IMethodSymbol>();

            // Collect the requested accessor(s)
            if (accessorType == "get" || accessorType == "both")
            {
                if (propertySymbol.GetMethod != null)
                {
                    accessorSymbols.Add(propertySymbol.GetMethod);
                }
            }
            if (accessorType == "set" || accessorType == "both")
            {
                if (propertySymbol.SetMethod != null)
                {
                    accessorSymbols.Add(propertySymbol.SetMethod);
                }
            }

            if (!accessorSymbols.Any())
            {
                throw new ArgumentException($"No {accessorType} accessor found for property {propertyName} in project {projectName}");
            }

            // Find references to each accessor
            var symbolReferences = new List<object>();
            foreach (var accessorSymbol in accessorSymbols)
            {
                var references = await SymbolFinder.FindReferencesAsync(accessorSymbol, solution, cancellationToken);
                foreach (var referencedSymbol in references.Where(x => x.Locations.Any()))
                {
                    symbolReferences.Add(new
                    {
                        ReferencedSymbol = referencedSymbol.Definition.ToDisplayString(),
                        AccessorType = accessorSymbol == propertySymbol.GetMethod ? "get" : "set",
                        Locations = referencedSymbol.Locations.Select(r => new
                        {
                            Location = r.Location.ToString()
                        })
                    });
                }
            }

            return SerializeToJson(symbolReferences);
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
