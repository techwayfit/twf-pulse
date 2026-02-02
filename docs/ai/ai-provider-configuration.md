# AI Provider Configuration Guide

## Overview

TechWayFit Pulse supports **four AI providers** for session activity generation:

1. **Mock** - Simple keyword-based questions (lightweight)
2. **Intelligent** - NLP-inspired algorithms (no dependencies, pure C#)
3. **MLNet** - Microsoft ML.NET machine learning (local ML, no API key)
4. **OpenAI** - GPT-powered generation (requires API key)

## Configuration

Edit `appsettings.json` or environment-specific config files:

```json
{
  "AI": {
    "Enabled": true,
    "Provider": "MLNet",
    "OpenAI": {
      "ApiKey": "your-api-key-here",
      "Model": "gpt-4o-mini",
      "Endpoint": "",
      "TimeoutSeconds": 60,
      "MaxTokens": 512,
      "UseAzure": false
    }
  }
}
```

## Provider Options

### 1. Mock Provider

**When to use:**
- Quick testing
- No AI features needed
- Fastest startup time

**Configuration:**
```json
{
  "AI": {
    "Enabled": false,
    "Provider": "Mock"
  }
}
```

**Features:**
- ✅ No dependencies
- ✅ Instant response
- ✅ Always available
- ✅ Improved keyword extraction
- ❌ Limited context awareness

**Activities Generated:** 8 activities

---

### 2. Intelligent Provider (Recommended for Development)

**When to use:**
- Development without API costs
- Testing with realistic data
- Offline environments
- Cost-sensitive deployments

**Configuration:**
```json
{
  "AI": {
    "Enabled": true,
    "Provider": "Intelligent"
  }
}
```

**Features:**
- ✅ NLP-inspired keyword extraction (TF-IDF)
- ✅ Context-aware questions
- ✅ No API key required
- ✅ No external dependencies
- ✅ Domain-specific templates
- ✅ Bigram detection
- ✅ 10 varied activities
- ✅ Pure C# implementation

**Algorithm:**
1. Tokenizes title, context, and goal
2. Removes stop words
3. Calculates term frequency (TF)
4. Extracts meaningful bigrams
5. Identifies domain context (agile, devops, team, etc.)
6. Generates contextual questions using templates

**Example Output:**

Input: "Agile Retrospective - Improve team velocity"

Generated activities:
- Experience Poll: "How would you rate your experience with Agile?"
- Priority Poll: "Which area needs most attention?" (Sprint planning, Daily standup, etc.)
- Word Cloud: "What word describes your thinking about velocity?"
- Challenge Feedback: "What's the biggest challenge with retrospective?"
- Impact vs Effort Quadrant
- Action Word Cloud: "What's the ONE thing we should do first?"
- And more...

---
MLNet Provider (Machine Learning) ⭐ Recommended

**When to use:**
- Development and staging
- Production without API costs
- Offline/on-premises deployments
- Want ML capabilities without cloud dependency

**Configuration:**
```json
{
  "AI": {
    "Enabled": true,
    "Provider": "MLNet"
  }
}
```

**Features:**
- ✅ **Actual ML.NET machine learning**
- ✅ Text featurization and analytics
- ✅ Domain classification
- ✅ Sentiment analysis
- ✅ N-gram extraction (unigrams & bigrams)
- ✅ Contextual activity generation
- ✅ 11 varied activities
- ✅ No API key required
- ✅ Runs locally/offline
- ✅ Deterministic and fast

**ML.NET Capabilities Used:**
- Text featurization pipeline
- Word bag extraction
- N-gram analysis (up to bigrams)
- Domain classification scoring
- Sentiment detection

**Example Output:**

Input: "Agile Retrospective - Improve team velocity and collaboration"

ML.NET extracts:
- Keywords: "Agile", "Retrospective", "Team", "Velocity", "Collaboration"
- Domain: "agile" (scored 8.0 based on keyword matches)
- Sentiment: "improvement-focused"

Generated activities:
- Opening Assessment (sentiment-aware)
- Agile Priorities Poll (domain-specific: Sprint planning, Retrospective, etc.)
- Key Themes Word Cloud
- Opportunity Identification (sentiment-adjusted)
- Approach Selection Poll
- Solution Ideation
- Impact vs Effort Quadrant
- Insights Word Cloud
- Next Action Poll
- Commitment Level Poll
- Session Reflection

---

### 4. 
### 3. OpenAI Provider (Production)

**When to use:**
- Production deployments
- Maximum quality
- Custom workshop scenarios
- Advanced personalization

**Configuration:**
```json
{
  "AI": {
    "Enabled": true,
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "sk-...",
      "Model": "gpt-4o-mini",
      "UseAzure": false
    }
  }
}
```

**For Azure OpenAI:**
```json
{
  "AI": {
    "Enabled": true,
    "Provider": "OpenAI",MLNet | OpenAI |
|---------|------|-------------|-------|--------|
| **Setup Complexity** | None | None | None | API Key Required |
| **Dependencies** | None | None | ML.NET NuGet | OpenAI API |
| **Cost** | Free | Free | Free | ~$0.01-0.10/session |
| **Quality** | Good | Very Good | Excellent | Best |
| **ML Capabilities** | No | NLP-inspired | Yes (ML.NET) | Yes (GPT) |
| **Context Awareness** | Basic | Yes | Yes (ML) | Yes (Advanced) |
| **Offline Support** | Yes | Yes | Yes | No |
| **Activities Generated** | 8 | 10 | 11 | Variable (6-12) |
| **Keyword Extraction** | Basic TF | TF-IDF | ML.NET features | GPT NLP |
| **Domain Detection** | No | Yes | Yes (ML scoring) | Yes (GPT) |
| **Sentiment Analysis** | No | No | Yes | Yes |
| **Response Time** | <1ms | <10ms | 10-50ms | 1-3s |
| **Implementation** | C# | C# (NLP) | ML.NET
- ✅ Highest quality questions
- ✅ Fully personalized
- ✅ Creative variations
- ✅ Natural language understanding
- ❌ Requires API key
- ❌ External dependency
- ❌ Cost per request

---

## Switching Providers

### At Runtime
Change `appsettings.json` and restart the application.

### Environment-Specific

**Development (appsettings.Development.json):**
```json
{
  "AI": {
    "Provider": "Intelligent"
  }
}
```

**Production (appsettings.Production.json):**
```json
{
  "AI": {
    "Enabled": true,
    "Provider": "OpenAI",
    "OpenAI": {
      "ApiKey": "${OPENAI_API_KEY}"
    }
  }
}
```

### Via Environment Variables
```bash
export AI__Provider=Intelligent
export AI__Enabled=true
```

---

## Comparison Table

| Feature | Mock | Intelligent | OpenAI |
|---------|------|-------------|--------|
| **Setup Complexity** | None | None | API Key Required |
| **Cost** | Free | Free | ~$0.01-0.10 per session |
| **Quality** | Good | Very Good | Excellent |
| **Context Awareness** | Basic | Yes | Yes |
| **Offline Support** | Yes | Yes | No |
| **Activities Generated** | 8 | 10 | Variable (6-12) |
| **Keyword Extraction** | Yes (Basic) | Yes (TF-IDF) | Yes (NLP) |
| **Domain Detection** | No | Yes | Yes |
| **Response Time** | <1ms | <10ms | 1-3s |
| **Implementation** | C# | C# (NLP-inspired) | GPT API |

---

## Troubleshooting

### Provider Not Loading
Check logs for:
```Intelligent
Registering INTELLIGENT AI services (NLP-inspired keyword-based generation)
```

### Intelligent Service Not Working
1. Verify `Provider` is set to `"Intelligen
1. Verify `Provider` is set to `"MLNet"` (case-sensitive)
2. Check logs for service registration
3. Restart application

### OpenAI Fallback
If OpenAI fails, it automatically falls back to MockSessionAIService. Check:
```
SessionAIService: OpenAI call failed after XXXms, falling back to mock
```

---

## Best Practices

### Development
```json
{
  "AI": {Intelligent"
  }
}
```
- Fast iteration
- No API costs
- Realistic testing

### Staging
```json
{
  "AI": {
    "Provider": "Intelligen
    "Provider": "MLNet"
  }
}
```
- Pre-production testing
- Cost control

### Production
```json
{
  "AI": {
    "Enabled": true,
    "Provider": "OpenAI"
  }
}
```
- Best user experience
- Monitor costs

---

## Implementation Details

### Mock Provider
**File:** `MockSessionAIService.cs`
- 8 activities with improved keyword-based generation
- Basic stop word filtering
- Simple TF analysis
Intelligent Provider
**File:** `IntelligentSessionAIService.cs`
- TF-IDF-inspired keyword extraction
- Bigram detection for multi-word phrases
- Domain context identification (8 domains)
- Template-based question generation
- 10 diverse activities
- **Pure C# implementation** - no external NLP libraries

### OpenAI Provider
**File:** `SessionAIService.cs`
- Full GPT integration
- Structured JSON prompts
- Fallback to Mock on failure
- Configurable models and parameters

---

## Future Enhancements

### Planned for Intelligent Provider:
- [ ] Add optional ML.NET NuGet package for true ML features
- [ ] Implement sentiment analysis
- [ ] Add named entity recognition
- [ ] Create custom ML models for workshop optimization
- [ ] A/B testing framework
- [ ] Word embedding supportls for workshop optimization
- [ ] A/B testing framework

### Planned for All Providers:
- [ ] Activity recommendation engine
- [ ] Workshop effectiveness tracking
- [ ] Participant engagement scoring
- [ ] Real-time adaptation based on responses
