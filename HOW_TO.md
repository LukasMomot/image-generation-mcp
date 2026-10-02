# How-to: install, update and maintain the MCP server in Claude Code

This guide covers how to use the server day to day in **Claude Code** (CLI, VS Code extension and the Claude desktop app's **Code** tab):

1. [Build a release version](#1-build-a-release-version)
2. [Register the server](#2-register-the-server)
3. [Check that it works](#3-check-that-it-works)
4. [Change settings (API key, model, output folder)](#4-change-settings-api-key-model-output-folder)
5. [After code changes: publish again](#5-after-code-changes-publish-again)
6. [Remove the server](#6-remove-the-server)
7. [Troubleshooting](#7-troubleshooting)

For the full feature and configuration reference, see [README.md](README.md).

---

## 1. Build a release version

For everyday use, run a **published build** instead of `dotnet run`. It starts faster and doesn't rebuild every time a session starts.

```bash
cd /path/to/image-generation-mcp
dotnet publish src/ImageGenerationMcp -c Release -o publish
```

This creates the executable `publish/ImageGenerationMcp` (`publish\ImageGenerationMcp.exe` on Windows). The `publish/` folder is git-ignored.

## 2. Register the server

Register it once in **user scope** so it's available in every project:

```bash
claude mcp add --scope user image-gen \
  -e OPENROUTER_API_KEY=sk-or-v1-YOUR_KEY \
  -e IMAGE_GEN_DEFAULT_MODEL=openai/gpt-image-2.5-flare \
  -e IMAGE_GEN_OUTPUT_DIR="/absolute/path/where/images/should/go" \
  -- "/path/to/image-generation-mcp/publish/ImageGenerationMcp"
```

Notes:
- Put the server name (`image-gen`) **before** the `-e` options, and the executable path after `--`.
- Quote paths that contain spaces.
- Use an **absolute** `IMAGE_GEN_OUTPUT_DIR`. Otherwise images land in `./output` of whichever project Claude Code happens to be running in.
- The command writes to `~/.claude.json` under the top-level `mcpServers` key. The API key is stored there **in plain text**; it never goes into this repository.
- For other scopes:
  - `--scope local` (the default): only for the current project.
  - `--scope project`: writes a shareable `.mcp.json` to the project. Don't put keys in it; use `${OPENROUTER_API_KEY}` instead.

> **Tip:** to keep the key out of your Claude conversation, run the command in your own terminal, or prefix it with `!` in the Claude Code prompt.

## 3. Check that it works

```bash
claude mcp list           # image-gen should show "✔ Connected"
claude mcp get image-gen  # shows command and env (prints the API key in plain text!)
```

Claude Code loads MCP servers **only when a session starts**. After registering:

| Where | What to do |
|---|---|
| Terminal (`claude`) | Start a new `claude` session |
| VS Code extension | Start a new conversation, or reload the window (`Cmd+Shift+P` → *Developer: Reload Window*) |
| Claude desktop app, **Code** tab | Quit the app completely (**Cmd+Q**), reopen it and start a new session |

Then run `/mcp` in the session. `image-gen` should be listed with the `generate_image` tool. Try it with:

> Generate an image of a lighthouse at dawn.

### Claude desktop app: Chat tab

The desktop app's **Chat** tab does **not** read `~/.claude.json`; it only reads `claude_desktop_config.json`:
- macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`
- Windows: `%APPDATA%\Claude\claude_desktop_config.json`

Add the server there too if you want it in Chat. Servers defined in this file also show up in the Code tab.

```json
{
  "mcpServers": {
    "image-gen": {
      "command": "/path/to/image-generation-mcp/publish/ImageGenerationMcp",
      "args": [],
      "env": {
        "OPENROUTER_API_KEY": "sk-or-v1-YOUR_KEY",
        "IMAGE_GEN_DEFAULT_MODEL": "openai/gpt-image-2.5-flare",
        "IMAGE_GEN_OUTPUT_DIR": "/absolute/path/where/images/should/go"
      }
    }
  }
}
```

If you change settings (section 4), remember to update **both** files.

## 4. Change settings (API key, model, output folder)

There is no `claude mcp update` command. Use one of these two options.

### Option A: remove and add again (simplest)

```bash
claude mcp remove image-gen -s user

claude mcp add --scope user image-gen \
  -e OPENROUTER_API_KEY=sk-or-v1-NEW_KEY \
  -e IMAGE_GEN_DEFAULT_MODEL=openai/gpt-image-2.5-flare \
  -e IMAGE_GEN_OUTPUT_DIR="/absolute/path/where/images/should/go" \
  -- "/path/to/image-generation-mcp/publish/ImageGenerationMcp"
```

Pass **all** variables again, not just the one that changed.

### Option B: edit `~/.claude.json` directly

Find the top-level `"mcpServers"` → `"image-gen"` → `"env"` entry and change the value:

```json
"image-gen": {
  "type": "stdio",
  "command": "/path/to/image-generation-mcp/publish/ImageGenerationMcp",
  "args": [],
  "env": {
    "OPENROUTER_API_KEY": "sk-or-v1-NEW_KEY",
    "IMAGE_GEN_DEFAULT_MODEL": "openai/gpt-image-2.5-flare",
    "IMAGE_GEN_OUTPUT_DIR": "/absolute/path/where/images/should/go"
  }
}
```

`~/.claude.json` is a large file that Claude Code also writes to. Edit it while Claude Code is closed, and make sure the JSON stays valid.

### Afterwards

Restart Claude Code (see [section 3](#3-check-that-it-works)) and check the result with `claude mcp get image-gen`.

> **Just want a different model once?** You don't need to change any settings. Name the model in your prompt, e.g. *"…use the model google/gemini-3-pro-image"*. `IMAGE_GEN_DEFAULT_MODEL` only applies when no model is given.

## 5. After code changes: publish again

The registered server runs the executable in `publish/`, so code changes only take effect after you publish again:

```bash
cd /path/to/image-generation-mcp
git pull                                   # if the changes come from GitHub
dotnet test                                # optional, but recommended
dotnet publish src/ImageGenerationMcp -c Release -o publish
```

Then restart Claude Code (see [section 3](#3-check-that-it-works)). You don't need to register again, because the path and settings stay the same.

> On Windows, publishing can fail with *"file is in use"* while a Claude session is running the server. Close Claude Code and publish again. On macOS/Linux you can publish while it runs; the restart picks up the new version.

## 6. Remove the server

```bash
claude mcp remove image-gen -s user
```

If you added it to `claude_desktop_config.json`, delete the `image-gen` entry there as well.

## 7. Troubleshooting

| Problem | Solution |
|---|---|
| `image-gen` not visible in `/mcp` or the desktop app | Claude Code was already running when the server was registered. Restart it (desktop app: **Cmd+Q**) and start a new session. |
| Visible in the Code tab but not in the Chat tab | Expected: Chat only reads `claude_desktop_config.json` (see [section 3](#claude-desktop-app-chat-tab)). |
| `claude mcp list` shows ✘ *Failed to connect* | Check that `publish/ImageGenerationMcp` exists (run `dotnet publish` again) and that the path in the config is correct. |
| Tool returns *"API key is not configured"* | `OPENROUTER_API_KEY` is missing or empty in the server's `env`. See [section 4](#4-change-settings-api-key-model-output-folder). |
| Tool returns HTTP 401 / 402 | Invalid key / no OpenRouter credits. |
| New settings don't take effect | The server reads its environment only on startup, so restart Claude Code. |
| Code changes don't take effect | Publish again and restart (see [section 5](#5-after-code-changes-publish-again)). |

For logs and deeper debugging (MCP Inspector, attaching a debugger), see [README.md → Debugging](README.md#debugging).
