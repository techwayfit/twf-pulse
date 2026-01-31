# AI-Powered Session Generation - Design Document

> **Version**: 1.0  
> **Status**: ?? Design Phase (Awaiting Approval)  
> **Last Updated**: January 2025

---

## ?? Executive Summary

This document outlines the design for AI-powered workshop session generation in TechWayFit Pulse. The system will accept natural language descriptions of workshop goals and automatically generate complete session templates with appropriate activities, configurations, and join forms.

### Key Capabilities

- **Natural Language Input**: Facilitators describe their workshop in plain English
- **Intelligent Activity Selection**: AI selects optimal activity types based on goals
- **Complete Template Generation**: Returns fully-configured JSON matching our template schema
- **Seamless Integration**: Uses existing session creation APIs
- **Template Validation**: Ensures generated templates comply with our schema

---

## ?? Use Cases

### Primary Use Cases

1. **Quick Session Creation**
   - Facilitator: "I need a 90-minute sprint retrospective for a team of 12"
   - AI generates: Session with WordCloud ? GeneralFeedback ? Poll ? Rating

2. **Domain-Specific Workshops**
   - Facilitator: "Create an incident post-mortem for a production outage"
   - AI generates: Session with Timeline QnA ? FiveWhys ? Quadrant ? GeneralFeedback

3. **Custom Workshop Design**
   - Facilitator: "I want to gather customer feedback on our new feature with 50+ participants"
   - AI generates: Session with WordCloud ? Poll ? Rating ? GeneralFeedback (categorized)

4. **Learning from Examples**
   - Facilitator: "Create a session similar to our strategic planning template but for product roadmap"
   - AI adapts existing template structure with new content

---

## ??? Architecture Overview

```
???????????????????????????????????????????????????????????????????
?   User Interface           ?
?  ????????????????????????????????????????????????????????? ?
?  ?  Facilitator Input Form   ?      ?
?  ?  - Natural language description           ? ?
?  ?  - Workshop context (optional)   ?   ?
?  ?  - Constraints (duration, participant count, etc.)    ?      ?
?  ?????????????????????????????????????????????????????????      ?
???????????????????????????????????????????????????????????????????
    ?
   ?
???????????????????????????????????????????????????????????????????
?             AI Session Generator API              ?
?  ?????????????????????????????????????????????????????????      ?
?  ?  POST /api/sessions/generate   ?      ?
?  ?  - Validate input     ?      ?
?  ?  - Build AI prompt from template          ? ?
?  ?  - Call AI service (Azure OpenAI)                ?      ?
?  ?  - Parse & validate response    ?      ?
?  ?  - Return session template JSON         ?      ?
?  ?????????????????????????????????????????????????????????      ?
???????????????????????????????????????????????????????????????????
          ?
            ?
???????????????????????????????????????????????????????????????????
?            AI Service Layer             ?
?  ?????????????????????????????????????????????????????????      ?
?  ?  Azure OpenAI / OpenAI API       ?      ?
?  ?  - GPT-4 or GPT-3.5-turbo          ?      ?
?  ?  - System prompt: Session template schema    ?      ?
?  ?  - User prompt: Workshop description       ?      ?
?  ?  - Response: JSON session template   ?      ?
?  ?????????????????????????????????????????????????????????      ?
???????????????????????????????????????????????????????????????????
    ?
   ?
???????????????????????????????????????????????????????????????????
?            Existing Session Creation Flow        ?
?  ?????????????????????????????????????????????????????????      ?
?  ?  POST /api/sessions        ?      ?
?  ?  - Uses generated JSON as input     ?      ?
?  ?  - Standard validation & creation      ?      ?
?  ?  - Returns session code    ?      ?
?  ?????????????????????????????????????????????????????????      ?
???????????????????????????????????????????????????????????????????
```

---

## ?? API Design

### Endpoint 1: Generate Session Template

**Endpoint**: `POST /api/sessions/generate`

**Purpose**: Generate a session template using AI based on natural language input

**Request Schema**:

```json
{
  "description": "string (required, 20-2000 characters)",
  "context": {
    "workshopType": "string (optional)",
    "durationMinutes": "integer (optional, 15-600)",
    "participantCount": "integer (optional, 1-1000)",
    "participantTypes": {
      "primary": "Technical|Managers|Business|Leaders|Mixed (optional)",
 "breakdown": {
     "technical": "integer (optional, count of engineers/developers)",
        "managers": "integer (optional, count of team leads/managers)",
        "business": "integer (optional, count of business analysts/product)",
      "leaders": "integer (optional, count of directors/executives)",
"other": "integer (optional)"
      },
      "experienceLevels": {
    "junior": "integer (optional, 0-2 years)",
        "midLevel": "integer (optional, 3-5 years)",
        "senior": "integer (optional, 6-10 years)",
    "expert": "integer (optional, 10+ years)"
  },
      "customRoles": ["string"] (optional, specific roles like "QA", "DevOps", "Designer")
    },
"goals": ["string"] (optional, array of objectives),
    "constraints": ["string"] (optional, array of constraints),
    "tone": "string (optional: professional|casual|educational)",
    "includeActivityTypes": ["Poll", "WordCloud", ...] (optional),
    "excludeActivityTypes": ["Quiz", "FiveWhys", ...] (optional),
    "contextDocuments": {
      "sprintBacklog": {
        "provided": "boolean (optional)",
        "summary": "string (optional, 500 chars max)",
        "keyItems": ["string"] (optional, top 5-10 backlog items)
   },
      "productDocumentation": {
        "provided": "boolean (optional)",
        "summary": "string (optional, 500 chars max)",
        "features": ["string"] (optional, relevant features)
      },
      "incidentReport": {
        "provided": "boolean (optional)",
        "summary": "string (optional, 500 chars max)",
        "severity": "P0|P1|P2|P3|P4 (optional)",
        "impactedSystems": ["string"] (optional)
      },
      "customDocument": {
        "type": "string (optional, e.g., 'Strategy Document', 'Customer Feedback')",
  "summary": "string (optional, 500 chars max)",
        "keyPoints": ["string"] (optional)
      }
    }
  },
  "options": {
    "aiProvider": "AzureOpenAI|OpenAI" (optional, default: AzureOpenAI),
    "model": "gpt-4|gpt-3.5-turbo" (optional, default: gpt-4),
    "temperature": "number (optional, 0.0-1.0, default: 0.7)",
    "returnMultipleOptions": "boolean (optional, default: false)",
    "optionsCount": "integer (optional, 1-3, default: 1)"
  }
}
```

**Request Example**:

```json
{
  "description": "I need a retrospective for our engineering team after a challenging sprint. We had some production issues and want to discuss what went well, what didn't, and identify action items. The team is remote and some members prefer anonymous feedback.",
  "context": {
  "workshopType": "Retrospective",
    "durationMinutes": 60,
    "participantCount": 12,
    "participantTypes": {
      "primary": "Technical",
      "breakdown": {
        "technical": 8,
        "managers": 2,
 "business": 1,
        "leaders": 1
    },
    "experienceLevels": {
      "junior": 2,
        "midLevel": 6,
      "senior": 3,
        "expert": 1
      },
  "customRoles": ["Backend Engineer", "Frontend Engineer", "QA", "DevOps", "Engineering Manager", "Product Manager"]
    },
    "goals": [
      "Identify what went well during the sprint",
      "Discuss production issues without blame",
      "Create actionable improvement items"
    ],
    "constraints": [
   "Allow anonymous feedback",
      "Keep it under 60 minutes",
      "Focus on last 2 weeks only"
    ],
    "tone": "professional",
    "contextDocuments": {
      "sprintBacklog": {
  "provided": true,
        "summary": "Sprint 42 focused on new payment gateway integration. 15 stories planned, 12 completed. 3 stories rolled over due to production hotfixes.",
      "keyItems": [
    "Payment Gateway Integration (rolled over)",
          "User Dashboard Redesign (completed)",
          "API Rate Limiting (completed)",
          "Mobile Push Notifications (rolled over)",
          "Database Migration (completed with issues)"
  ]
      },
  "incidentReport": {
    "provided": true,
        "summary": "Production outage on Jan 10th, 2:30 PM - 4:15 PM. Database connection pool exhausted during peak traffic. Affected 15% of active users.",
   "severity": "P1",
     "impactedSystems": ["Payment Service", "User Dashboard", "API Gateway"]
      }
    }
  },
"options": {
  "aiProvider": "AzureOpenAI",
    "model": "gpt-4",
    "temperature": 0.7,
    "returnMultipleOptions": false
  }
}
```

**Response Schema**:

```json
{
  "success": "boolean",
  "data": {
    "generatedTemplate": {
      "name": "string",
      "description": "string",
      "category": "string",
      "iconEmoji": "string",
      "config": {
      "title": "string",
      "goal": "string",
        "context": "string",
        "settings": {
          "durationMinutes": "integer",
          "allowAnonymous": "boolean",
     "allowLateJoin": "boolean",
          "showResultsDuringActivity": "boolean"
        },
        "joinFormSchema": {
      "fields": []
        },
"activities": []
      }
    },
  "alternativeOptions": [] (optional, if returnMultipleOptions: true),
    "aiMetadata": {
      "model": "string",
      "tokensUsed": "integer",
      "processingTimeMs": "integer",
   "confidence": "number (0.0-1.0)"
    },
    "validationResult": {
      "isValid": "boolean",
      "warnings": ["string"],
      "suggestions": ["string"]
    }
  },
  "errors": [] (if success: false)
}
```

**Response Example (Success)**:

```json
{
  "success": true,
  "data": {
    "generatedTemplate": {
      "name": "Sprint Retrospective - Production Issues Review",
      "description": "Reflect on sprint challenges, discuss production issues, and create action items",
      "category": "Retrospective",
      "iconEmoji": "??",
      "config": {
        "title": "Sprint 42 Retrospective",
        "goal": "Reflect on what went well, discuss production issues without blame, and identify actionable improvements",
        "context": "Remote team retrospective focusing on the last 2 weeks with anonymous feedback options",
        "settings": {
      "durationMinutes": 60,
       "allowAnonymous": true,
   "allowLateJoin": true,
    "showResultsDuringActivity": true
        },
        "joinFormSchema": {
          "fields": [
          {
              "name": "name",
         "label": "Your Name (optional for anonymous mode)",
              "type": "text",
          "required": false
            },
      {
            "name": "role",
              "label": "Role",
              "type": "select",
              "required": true,
   "options": ["Engineer", "QA", "DevOps", "Product Manager"]
        }
          ]
   },
      "activities": [
        {
            "order": 1,
  "type": "WordCloud",
            "title": "Sprint in One Word",
      "prompt": "Describe this sprint in one word",
"durationMinutes": 5,
     "config": {}
          },
          {
"order": 2,
            "type": "GeneralFeedback",
            "title": "Sprint Reflection",
   "prompt": "Share your thoughts on the sprint",
          "durationMinutes": 15,
          "config": {
     "categoriesEnabled": true,
        "categories": [
         {
         "id": "went-well",
         "label": "What Went Well",
       "icon": "?"
      },
     {
    "id": "didnt-go-well",
   "label": "What Didn't Go Well",
     "icon": "??"
         },
     {
"id": "action-items",
        "label": "Action Items",
            "icon": "??"
         }
     ],
  "requireCategory": true,
          "allowAnonymous": true,
        "maxLength": 500,
              "maxResponsesPerParticipant": 5
  }
          },
  {
        "order": 3,
     "type": "Poll",
         "title": "Priority Action Items",
   "prompt": "Which improvement should we prioritize?",
            "durationMinutes": 10,
        "config": {
   "options": [
   {
       "id": "monitoring",
   "label": "Improve monitoring and alerts",
           "description": "Better visibility into production issues"
        },
  {
       "id": "testing",
         "label": "Enhance testing coverage",
        "description": "Catch issues before production"
     },
      {
        "id": "documentation",
       "label": "Better documentation",
               "description": "Improve runbooks and troubleshooting guides"
             },
  {
           "id": "communication",
      "label": "Improve team communication",
               "description": "Better sync on changes and deployments"
      }
        ],
   "allowMultiple": false,
  "maxResponsesPerParticipant": 1
            }
          },
          {
            "order": 4,
      "type": "Rating",
            "title": "Sprint Satisfaction",
        "prompt": "How satisfied are you with this sprint overall?",
 "durationMinutes": 5,
      "config": {
        "scale": 5,
     "minLabel": "1 - Very Dissatisfied",
      "maxLabel": "5 - Very Satisfied",
              "midpointLabel": "3 - Neutral",
              "allowComments": true,
       "commentRequired": false,
    "displayType": "Stars",
"maxResponsesPerParticipant": 1
            }
    }
        ]
      }
    },
    "aiMetadata": {
      "model": "gpt-4",
   "tokensUsed": 1847,
      "processingTimeMs": 3421,
      "confidence": 0.92
  },
    "validationResult": {
      "isValid": true,
      "warnings": [],
      "suggestions": [
   "Consider adding a Quadrant activity for prioritizing action items by impact vs effort",
      "You could extend the GeneralFeedback activity to 20 minutes for deeper discussion"
      ]
    }
  },
  "errors": []
}
```

**Response Example (Error)**:

```json
{
  "success": false,
  "data": null,
  "errors": [
  {
      "code": "AI_GENERATION_FAILED",
      "message": "Failed to generate session template: AI service timeout",
      "details": "The AI service did not respond within 30 seconds. Please try again."
    }
  ]
}
```

---

### Endpoint 2: Create Session from Generated Template

**Endpoint**: `POST /api/sessions/from-generated`

**Purpose**: Create a session directly from AI-generated template with validation

**Request Schema**:

```json
{
  "generatedTemplate": {
    // Full template JSON from generate endpoint
  },
  "facilitatorToken": "string (required)",
  "customizations": {
    "title": "string (optional, override AI-generated title)",
    "durationMinutes": "integer (optional)",
    "additionalActivities": [] (optional, add more activities),
    "removeActivities": [] (optional, array of order numbers to remove)
  }
}
```

**Response Schema**:

```json
{
  "success": "boolean",
  "data": {
    "sessionCode": "string",
    "sessionId": "GUID",
    "facilitatorUrl": "string",
    "participantUrl": "string",
    "qrCodeUrl": "string",
    "expiresAt": "ISO 8601 timestamp"
  },
  "errors": []
}
```

---

### Error Codes

| Code | Description | HTTP Status |
|------|-------------|-------------|
| `INVALID_INPUT` | Request validation failed | 400 |
| `DESCRIPTION_TOO_SHORT` | Description must be at least 20 characters | 400 |
| `DESCRIPTION_TOO_LONG` | Description exceeds 2000 characters | 400 |
| `AI_SERVICE_UNAVAILABLE` | AI service is not available | 503 |
| `AI_GENERATION_FAILED` | Failed to generate template | 500 |
| `INVALID_JSON_RESPONSE` | AI returned invalid JSON | 500 |
| `TEMPLATE_VALIDATION_FAILED` | Generated template doesn't match schema | 422 |
| `RATE_LIMIT_EXCEEDED` | Too many AI generation requests | 429 |
| `INSUFFICIENT_CREDITS` | AI service quota exceeded | 402 |

---

## ?? AI Prompt Engineering

### System Prompt Template

This is the instruction set sent to the AI model to ensure consistent, valid output.

**File**: `docs/ai-prompts/session-generation-system-prompt.md`

---

## ?? Integration Architecture

### Component Diagram

```
???????????????????????????????????????????????????????????????????
?       Web Layer (Blazor)     ?
???????????????????????????????????????????????????????????????????
?        ?
?  ??????????????????????????????????????????????????????  ?
?  ?  Pages/Facilitator/CreateSessionWithAI.razor       ?  ?
?  ?  - Natural language input form      ?         ?
?  ?  - Context configuration (optional)      ?         ?
?  ?  - Preview generated template   ?         ?
?  ?  - Edit & customize before creating       ?         ?
?  ??????????????????????????????????????????????????????         ?
?       ?
?  ??????????????????????????????????????????????????????         ?
?  ?  Components/AI/TemplatePreview.razor               ? ?
?  ?  - Display generated template   ?         ?
?  ?  - Activity list with configs     ?     ?
?  ?  - Edit individual activities          ?     ?
?  ??????????????????????????????????????????????????????    ?
?     ?
???????????????????????????????????????????????????????????????????
          ?
   ?
???????????????????????????????????????????????????????????????????
?    Controllers (API) ?
???????????????????????????????????????????????????????????????????
?       ?
?  ??????????????????????????????????????????????????????   ?
?  ?  AISessionGeneratorController.cs    ?         ?
?  ?  - POST /api/sessions/generate         ?         ?
?  ?  - POST /api/sessions/from-generated    ?         ?
?  ?  - Validation & error handling         ?         ?
?  ??????????????????????????????????????????????????????         ?
?             ?
???????????????????????????????????????????????????????????????????
?
               ?
???????????????????????????????????????????????????????????????????
?Application Layer      ?
???????????????????????????????????????????????????????????????????
?   ?
?  ??????????????????????????????????????????????????????         ?
?  ?  Services/IAISessionGeneratorService.cs     ?         ?
?  ?  - GenerateSessionTemplateAsync()     ?         ?
?  ?  - ValidateGeneratedTemplate()         ? ?
?  ?  - CreateSessionFromTemplate()      ?         ?
?  ??????????????????????????????????????????????????????   ?
?        ?
?  ??????????????????????????????????????????????????????         ?
?  ?  Services/IAIPromptBuilder.cs              ?         ?
?  ?  - BuildSystemPrompt()         ?       ?
?  ?  - BuildUserPrompt(description, context)           ?         ?
?  ?  - LoadPromptTemplate()                ?         ?
?  ??????????????????????????????????????????????????????     ?
?      ?
?  ??????????????????????????????????????????????????????         ?
?  ?  Services/ITemplateValidator.cs           ?   ?
?  ?  - ValidateTemplateSchema()          ?      ?
?  ?  - ValidateActivityConfigs()               ?         ?
?  ?  - GenerateWarningsAndSuggestions()      ?     ?
?  ??????????????????????????????????????????????????????    ?
?         ?
???????????????????????????????????????????????????????????????????
             ?
       ?
???????????????????????????????????????????????????????????????????
?             Infrastructure Layer    ?
???????????????????????????????????????????????????????????????????
?          ?
?  ??????????????????????????????????????????????????????   ?
?  ?  AI/AzureOpenAIService.cs           ?         ?
?  ?  - CompleteAsync(systemPrompt, userPrompt)         ?         ?
?  ?  - Uses Azure.AI.OpenAI SDK          ?    ?
?  ?  - Handles retries & rate limiting    ?         ?
?  ??????????????????????????????????????????????????????         ?
?       ?
?  ??????????????????????????????????????????????????????     ?
?  ?  AI/OpenAIService.cs            ?         ?
?  ?  - Alternative implementation for OpenAI API       ?   ?
?  ?  - Uses OpenAI SDK           ?         ?
?  ??????????????????????????????????????????????????????      ?
?      ?
???????????????????????????????????????????????????????????????????
```

---

### File Structure

```
src/
??? TechWayFit.Pulse.Application/
?   ??? Abstractions/
?   ?   ??? Services/
?   ?       ??? IAISessionGeneratorService.cs
?   ?       ??? IAIPromptBuilder.cs
?   ?       ??? ITemplateValidator.cs
?   ??? Services/
?       ??? AISessionGeneratorService.cs
?       ??? AIPromptBuilder.cs
?    ??? TemplateValidator.cs
?
??? TechWayFit.Pulse.Infrastructure/
?   ??? AI/
?       ??? IAICompletionService.cs (abstraction)
?       ??? AzureOpenAIService.cs (implementation)
?       ??? OpenAIService.cs (alternative implementation)
?
??? TechWayFit.Pulse.Contracts/
?   ??? Requests/
?   ?   ??? GenerateSessionRequest.cs
?   ?   ??? CreateSessionFromGeneratedRequest.cs
?   ??? Responses/
?       ??? GeneratedSessionResponse.cs
?       ??? AIMetadataResponse.cs
?
??? TechWayFit.Pulse.Web/
    ??? Controllers/
    ?   ??? AISessionGeneratorController.cs
    ??? Pages/
    ?   ??? Facilitator/
    ?     ??? CreateSessionWithAI.razor
    ??? Components/
  ??? AI/
   ??? TemplatePreview.razor
 ??? ActivityConfigurator.razor

docs/
??? ai-prompts/
    ??? session-generation-system-prompt.md
    ??? activity-selection-guidelines.md
    ??? template-schema-reference.json
```

---

## ?? Security & Privacy

### API Key Management

```json
// appsettings.json (DO NOT commit)
{
  "AI": {
    "Provider": "AzureOpenAI",
    "AzureOpenAI": {
  "Endpoint": "https://YOUR-RESOURCE.openai.azure.com/",
      "ApiKey": "***STORED IN AZURE KEY VAULT***",
   "DeploymentName": "gpt-4",
      "ApiVersion": "2024-02-15-preview"
    },
    "OpenAI": {
      "ApiKey": "***STORED IN AZURE KEY VAULT***",
      "OrganizationId": "org-***"
    },
    "RateLimiting": {
 "MaxRequestsPerMinute": 10,
   "MaxRequestsPerDay": 100
    }
  }
}
```

### Data Handling

1. **User Input**:
   - Sanitize all user input before sending to AI
   - Remove PII (personally identifiable information)
   - Log requests for audit (without sensitive data)

2. **AI Responses**:
   - Validate all generated JSON
   - Strip any potentially harmful content
   - Cache responses for 24 hours (with user consent)

3. **Rate Limiting**:
   - Per-user limits: 10 generations per hour
   - Per-organization: 100 generations per day
   - Implement exponential backoff

---

## ?? Cost Estimation

### Azure OpenAI Pricing (GPT-4)

| Component | Tokens | Cost per 1K Tokens | Cost per Request |
|-----------|--------|-------------------|------------------|
| System Prompt | ~2,000 | $0.03 (input) | $0.06 |
| User Input | ~500 | $0.03 (input) | $0.015 |
| AI Response | ~3,000 | $0.06 (output) | $0.18 |
| **Total per request** | ~5,500 | - | **~$0.255** |

**Monthly Estimates**:
- 10 sessions/day × 30 days = 300 sessions/month
- 300 × $0.255 = **$76.50/month**

**Cost Optimization**:
- Use GPT-3.5-turbo for simpler requests (~90% cheaper)
- Cache common templates
- Implement smart retry logic (avoid duplicate requests)

---

## ?? Success Metrics

### Key Performance Indicators (KPIs)

1. **Generation Success Rate**: Target >95%
2. **Template Validation Pass Rate**: Target >90%
3. **User Satisfaction**: Post-generation survey rating >4.5/5
4. **Time Savings**: Average 5-10 minutes vs. manual creation
5. **API Response Time**: Target <5 seconds for generation

### Monitoring

```json
{
  "metrics": {
    "totalGenerations": "integer",
    "successfulGenerations": "integer",
    "failedGenerations": "integer",
    "averageProcessingTimeMs": "integer",
    "averageTokensUsed": "integer",
    "userSatisfactionRating": "number",
    "templateValidationErrors": ["string"],
    "mostRequestedActivityTypes": ["Poll", "WordCloud", ...]
  }
}
```

---

## ?? Testing Strategy

### Test Cases

1. **Basic Generation**:
   - Input: Simple retrospective description
   - Expected: Valid template with 3-5 activities

2. **Complex Scenarios**:
   - Input: Multi-day workshop with 100+ participants
   - Expected: Multiple sessions with appropriate activities

3. **Edge Cases**:
   - Empty description
   - Extremely long description (>2000 chars)
   - Invalid activity type requests
   - Contradictory constraints

4. **AI Response Validation**:
   - Invalid JSON
   - Missing required fields
   - Incorrect activity configurations
   - Out-of-range values

5. **Error Handling**:
 - AI service timeout
   - Rate limit exceeded
   - Invalid API key
   - Network failures

### Manual Testing Checklist

- [ ] Generate session from natural language
- [ ] Verify template structure matches schema
- [ ] Validate all activity configurations
- [ ] Test with different workshop types
- [ ] Verify join form fields are appropriate
- [ ] Check duration calculations
- [ ] Test with multiple AI providers
- [ ] Verify error messages are user-friendly
- [ ] Test template preview UI
- [ ] Verify session creation from generated template

---

## ?? Rollout Plan

### Phase 1: Alpha (Internal Testing)
**Duration**: 2 weeks

- [ ] Implement core AI generation service
- [ ] Create basic UI for input/preview
- [ ] Test with 5 internal users
- [ ] Collect feedback on generated templates
- [ ] Refine prompts based on feedback

### Phase 2: Beta (Limited Release)
**Duration**: 4 weeks

- [ ] Add template customization UI
- [ ] Implement rate limiting
- [ ] Deploy to production (feature flag)
- [ ] Invite 20 beta testers
- [ ] Monitor costs and performance
- [ ] A/B test: AI-generated vs. manual templates

### Phase 3: General Availability
**Duration**: Ongoing

- [ ] Remove feature flag
- [ ] Add to main navigation
- [ ] Create tutorial/documentation
- [ ] Monitor usage and costs
- [ ] Iterate on prompts and features

---

## ?? Configuration Examples

### Example 1: Sprint Retrospective

**Input**:
```json
{
  "description": "We need a 60-minute sprint retrospective for our engineering team of 10. Focus on what went well, what didn't, and action items. Some team members prefer anonymous feedback.",
  "context": {
    "workshopType": "Retrospective",
    "durationMinutes": 60,
    "participantCount": 10
  }
}
```

**Expected Activities**:
1. WordCloud (5 min) - Sprint sentiment
2. GeneralFeedback (20 min) - Categorized: Went Well / Didn't Go Well / Actions
3. Poll (10 min) - Vote on top action items
4. Rating (5 min) - Sprint satisfaction

---

### Example 2: Customer Feedback Session

**Input**:
```json
{
  "description": "Gather feedback from 50 customers about our new mobile app. We want to understand pain points, feature requests, and overall satisfaction.",
  "context": {
    "workshopType": "ProductDiscovery",
    "durationMinutes": 45,
    "participantCount": 50
  }
}
```

**Expected Activities**:
1. WordCloud (5 min) - App in one word
2. Rating (10 min) - App satisfaction (1-10)
3. GeneralFeedback (15 min) - Categorized: Bugs / Features / UX
4. Poll (10 min) - Top feature request

---

### Example 3: Incident Post-Mortem

**Input**:
```json
{
  "description": "Post-mortem for production outage that affected customers. Need to identify root cause, contributing factors, and preventive measures. Blameless culture is critical.",
  "context": {
    "workshopType": "IncidentReview",
    "durationMinutes": 90,
    "participantCount": 8,
    "constraints": ["Blameless", "Focus on systems not people"]
  }
}
```

**Expected Activities**:
1. QnA (15 min) - Timeline of events (chronological)
2. FiveWhys (20 min) - Root cause analysis
3. GeneralFeedback (20 min) - Contributing factors
4. Quadrant (15 min) - Remediation priority (Impact vs Effort)
5. GeneralFeedback (10 min) - Action items with owners

---

## ?? User Experience Flow

### Facilitator Journey

```
1. Navigate to "Create Session with AI"
   ?
2. Enter workshop description (natural language)
   ?
3. [Optional] Provide additional context
   - Workshop type
   - Duration
   - Participant count
   - Goals & constraints
   ?
4. Click "Generate Session"
   ?
5. [AI Processing - 3-10 seconds]
   - Show loading state
   - "Creating your workshop template..."
   ?
6. Preview Generated Template
   - Session metadata (title, category, icon)
   - Activity list with configs
   - Join form fields
   - Duration breakdown
?
7. [Optional] Customize Template
   - Edit activity titles/prompts
   - Reorder activities
   - Add/remove activities
   - Adjust configurations
   ?
8. Click "Create Session"
   ?
9. Redirected to Live Console
   - Session code displayed
   - QR code generated
   - Ready to launch
```

---

## ?? Configuration Management

### Environment Variables

```bash
# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://YOUR-RESOURCE.openai.azure.com/
AZURE_OPENAI_API_KEY=***
AZURE_OPENAI_DEPLOYMENT=gpt-4
AZURE_OPENAI_API_VERSION=2024-02-15-preview

# OpenAI (alternative)
OPENAI_API_KEY=***
OPENAI_ORGANIZATION_ID=org-***

# Feature Flags
FEATURE_AI_SESSION_GENERATION=true
FEATURE_AI_MULTIPLE_OPTIONS=false

# Rate Limiting
AI_MAX_REQUESTS_PER_MINUTE=10
AI_MAX_REQUESTS_PER_DAY=100

# Caching
AI_CACHE_ENABLED=true
AI_CACHE_TTL_HOURS=24
```

---

## ?? Related Documentation

### Prerequisites (Read First)
- `docs/activity-type-status.md` - Activity type implementation status
- `src/TechWayFit.Pulse.Web/App_Data/Templates/README.md` - Template schema
- `docs/template-schema.md` - Detailed schema documentation

### To Be Created
- `docs/ai-prompts/session-generation-system-prompt.md` - AI system prompt
- `docs/ai-session-generation-api.md` - API implementation guide
- `docs/ai-session-generation-testing.md` - Testing procedures

---

## ? Open Questions (For Review)

1. **AI Provider Choice**:
   - Should we support both Azure OpenAI and OpenAI, or pick one?
   - Recommendation: Start with Azure OpenAI (enterprise-ready, better compliance)

2. **Rate Limiting**:
   - What are acceptable limits per user/organization?
   - Recommendation: 10/hour per user, 100/day per organization

3. **Cost Management**:
   - Should we charge users for AI-generated sessions?
   - Recommendation: Free for first 10 generations, then tiered pricing

4. **Template Caching**:
   - Should we cache similar requests to reduce costs?
   - Recommendation: Yes, cache for 24 hours with user consent

5. **Multiple Options**:
   - Should we generate 2-3 template options and let users pick?
   - Recommendation: Phase 2 feature (increases cost 3x)

6. **Activity Type Availability**:
   - Should we only allow AI to use completed activity types (Poll, Rating, WordCloud, GeneralFeedback)?
   - Recommendation: Yes for Phase 1, add Quadrant/QnA in Phase 2

7. **Template Validation Strictness**:
   - Should we reject invalid templates or auto-fix them?
   - Recommendation: Auto-fix minor issues, reject major structural problems

8. **User Feedback Loop**:
   - How should we collect feedback on generated templates?
   - Recommendation: Post-generation survey + "Report Issue" button

---

## ? Approval Checklist

Before implementation, confirm:

- [ ] API design reviewed and approved
- [ ] AI prompt strategy validated
- [ ] Security and privacy measures approved
- [ ] Cost estimates acceptable
- [ ] Testing strategy agreed upon
- [ ] Rollout plan confirmed
- [ ] Open questions resolved
- [ ] AI provider selected (Azure OpenAI or OpenAI)
- [ ] Rate limiting strategy approved
- [ ] Error handling approach validated

---

**Status**: ?? **Awaiting Review and Approval**

**Next Steps**:
1. Review this design document
2. Answer open questions
3. Approve/modify API design
4. Approve/modify AI prompt strategy
5. Greenlight implementation

Once approved, proceed to:
- `docs/ai-prompts/session-generation-system-prompt.md`
- `docs/ai-session-generation-implementation.md`

---

**Document Version**: 1.0  
**Last Updated**: January 2025  
**Author**: TechWayFit Engineering Team
