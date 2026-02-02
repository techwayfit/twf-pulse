# AI Provider Quick Reference

## Switch AI Providers

Edit `appsettings.json` or `appsettings.local.json`:

```json
{
  "AI": {
    "Enabled": true,
    "Provider": "MLNet"
  }
}
```

## Available Providers

| Provider | Use Case | Setup | Cost | ML |
|----------|----------|-------|------|-----|
| **Mock** | Quick testing | None | Free | No |
| **Intelligent** | Dev (no ML) | None | Free | NLP-inspired |
| **MLNet** | Dev/Staging/Prod | None | Free | Yes (ML.NET) |
| **OpenAI** | Production | API Key | $$ | Yes (GPT) |

## Quick Configs

### Development (Recommended)
```json
{
  "AI": {
    "Provider": "MLNet"
  }
}
```

### Testing (No ML)
```json
{
  "AI": {
    "Provider": "Mock"
  }
}
```

### Production (No API costs)
```json
{
  "AI": {
    "Enabled": true,
    "Provider": "MLNet"
  }
}
```

### Production (Best quality)
```json
{
  "AI": {
    "Enabled": true,
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-..."
    }
  }
}
```

## Service Implementations

| Provider | Service Class | File |
|----------|--------------|------|
| Mock | `MockSessionAIService` | `MockSessionAIService.cs` |
| Intelligent | `IntelligentSessionAIService` | `IntelligentSessionAIService.cs` |
| **MLNet** | `MLNetSessionAIService` | `MLNetSessionAIService.cs` |
| OpenAI | `SessionAIService` | `SessionAIService.cs` |

## Features Comparison

### Mock Provider
- 8 activities
- Basic keyword extraction
- Stop word filtering

### Intelligent Provider
- 10 contextual activities
- TF-IDF keyword extraction
- Bigram detection
- Domain identification
- **Pure C# - no dependencies**

### MLNet Provider ⭐ Recommended
- **11 contextual activities**
- **ML.NET machine learning**
- Text featurization
- N-gram extraction
- Domain classification (ML-scored)
- **Sentiment analysis**
- Domain-specific templates
- Offline ML capabilities

### OpenAI Provider
- GPT-powered generation
- Best quality
- Fully personalized
- Requires API key & internet

## Verify Active Provider

Check application logs on startup:

```
AI Provider: MLNet
Registering ML.NET AI services (Microsoft ML.NET machine learning)
```

## Environment Variables

```bash
export AI__Enabled=true
export AI__Provider=MLNet
```

## Which Provider Should I Use?

| Scenario | Recommended Provider |
|----------|---------------------|
| Development/Testing | **MLNet** or Intelligent |
| Staging | **MLNet** |
| Production (offline) | **MLNet** |
| Production (best quality, online) | OpenAI |
| Quick test (no ML) | Mock |

## See Full Documentation

[AI Provider Configuration Guide](./ai-provider-configuration.md)
