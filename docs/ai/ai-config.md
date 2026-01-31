# AI Configuration

This document describes configuration keys for enabling and using AI features in TechWayFit Pulse.

Configuration keys (add to `appsettings.json` or `appsettings.local.json`):

- `AI:Enabled` (bool) - Enable AI features. Default: `false`.
- `AI:OpenAI:ApiKey` (string) - OpenAI / Azure OpenAI API key. Required when `AI:Enabled` is `true`.
- `AI:OpenAI:Endpoint` (string) - Base endpoint for the OpenAI service. For OpenAI public API use `https://api.openai.com/v1/` (the code uses `/v1/chat/completions` by default). For Azure OpenAI use your resource endpoint (e.g. `https://your-resource.openai.azure.com/`).
- `AI:OpenAI:Model` (string) - Optional model name to use (default `gpt-4`).
- `AI:OpenAI:TimeoutSeconds` (int) - Optional timeout for AI calls (default 60).

Security and privacy:
- Do NOT commit API keys to source control. Use `appsettings.local.json`, environment variables, or a secret store.
- Ensure context documents do not contain PII before sending to AI.

Example usage:
- Enable AI generation of facilitator prompts and participant response analysis.
- AI calls are only registered when `AI:Enabled:true` and `AI:OpenAI:ApiKey` is present.

See `publish/appsettings.ai.sample.json` for a sample configuration file.
