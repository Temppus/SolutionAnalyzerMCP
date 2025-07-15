# SolutionAnalyzerMCP
MCP server that uses roslyn to query relevant context from larger net solution codebase

```
  "solution-analyer": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "--mount",
        "type=bind,src=/c/git-repos/your-repo-dir,dst=/app/your-repo-dir",
        "ghcr.io/temppus/solution-analyzer",
        "--SolutionPath",
        "/app/your-repo-dir/your-solution.sln"
      ]
    },
```
