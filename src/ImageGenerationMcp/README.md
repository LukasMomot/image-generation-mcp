# ImageGenerationMcp

A local (stdio) MCP server that generates images from text prompts via [OpenRouter](https://openrouter.ai/) and saves them as PNG files (`output/yyyyMMdd_HHmmss.png`). The tool returns the absolute file path to the LLM.

## Tool

`generate_image(prompt, model?, quality = "medium", aspectRatio = "3:2")`

## Configuration

| Variable | Required | Default |
|---|---|---|
| `OPENROUTER_API_KEY` | yes | – |
| `IMAGE_GEN_DEFAULT_MODEL` | no | `openai/gpt-image-2.5-flare` |
| `IMAGE_GEN_OUTPUT_DIR` | no | `./output` |

## Usage

```json
{
  "servers": {
    "image-gen": {
      "type": "stdio",
      "command": "dnx",
      "args": ["ImageGenerationMcp", "--yes"],
      "env": { "OPENROUTER_API_KEY": "<your key>" }
    }
  }
}
```

See the repository README for full documentation, client setup (Claude Code, Claude Desktop, VS Code, Cursor) and debugging tips.
