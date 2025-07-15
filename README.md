# SolutionAnalyzerMCP
MCP server that uses roslyn to query relevant context from larger net solution codebase

```
  "solution-analyer": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "-v",        
        "/c/git-repos/your-project/your-project.sln:/app/solution.sln",
        "ghcr.io/temppus/solution-analyzer"
      ]
    },
```
