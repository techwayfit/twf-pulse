# AI Session Generation - Participant Types & Context Documents Enhancement

> **Version**: 1.1  
> **Status**: ?? Design Enhancement  
> **Last Updated**: January 2025  
> **Parent Document**: `docs/ai-session-generation-design.md`

---

## ?? Overview

This document extends the AI Session Generation design with two critical enhancements:

1. **Participant Types** - Understanding who will attend (technical, managers, business, leaders)
2. **Context Documents** - Providing relevant context (sprint backlog, incident reports, product docs)

These enhancements enable the AI to generate more targeted, relevant, and effective workshop sessions.

---

## ?? Why These Enhancements Matter

### Problem with Generic Sessions

Without participant and context information, AI generates **one-size-fits-all** sessions:
- Generic questions that don't resonate with the audience
- Activities that may be too technical or too simplistic
- Missed opportunities to leverage existing artifacts (sprint data, incident reports)
- Lower engagement due to irrelevant content

### Solution: Context-Aware Generation

With participant types and context documents, AI can:
- **Adjust complexity** based on experience levels
- **Tailor language** to audience type (technical vs. business)
- **Reference actual data** from sprint backlogs or incident reports
- **Generate specific questions** about documented issues/features
- **Increase relevance** and participant engagement

---

## ?? Participant Types

### Categories

| Type | Description | Typical Activities | Example Roles |
|------|-------------|-------------------|---------------|
| **Technical** | Engineers, developers, QA, DevOps | Deep-dive technical polls, code quality ratings, incident analysis | Software Engineer, QA Engineer, DevOps Engineer, Solutions Architect |
| **Managers** | Team leads, engineering managers, scrum masters | Team health ratings, process improvement, resource allocation | Engineering Manager, Team Lead, Scrum Master, Tech Lead |
| **Business** | Product managers, analysts, stakeholders | Feature prioritization, customer impact, ROI analysis | Product Manager, Business Analyst, Product Owner, Customer Success |
| **Leaders** | Directors, VPs, executives | Strategic alignment, budget allocation, organizational goals | Director of Engineering, VP Product, CTO, CEO |
| **Mixed** | Cross-functional teams | Balanced activities covering technical, business, and strategic angles | Any combination of above |

### Enhanced Request Schema

```json
{
  "participantTypes": {
    "primary": "Technical|Managers|Business|Leaders|Mixed",
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
    "customRoles": ["Backend Engineer", "Frontend Engineer", "QA", "DevOps"]
  }
}
```

### AI Generation Impact

#### Technical Teams
```
Generic Question: "What went well this sprint?"
Enhanced Question: "Which microservice deployment went smoothly and why?"

Generic Poll: "What should we improve?"
Enhanced Poll: 
- "Improve CI/CD pipeline reliability"
- "Better test coverage for API endpoints"
- "Reduce Docker image build times"
- "Improve observability and logging"
```

#### Business Teams
```
Generic Question: "What features do you want?"
Enhanced Question: "Which customer segment would benefit most from enhanced reporting?"

Generic Poll: "Priority features?"
Enhanced Poll:
- "Customer dashboard with ROI metrics"
- "Automated reporting for stakeholders"
- "Integration with CRM for sales insights"
- "Mobile app for on-the-go access"
```

#### Mixed Teams
```
Generic Question: "What challenges did we face?"
Enhanced Question: "What prevented us from delivering value to customers this sprint?"

Generic Poll: "Biggest blocker?"
Enhanced Poll:
- "Technical debt slowing feature development"
- "Unclear requirements from stakeholders"
- "Integration issues with external systems"
- "Resource constraints and competing priorities"
```

---

## ?? Context Documents

### Types of Context Documents

#### 1. Sprint Backlog

**When to provide**: Sprint retrospectives, planning sessions, velocity tracking

**Schema**:
```json
{
  "sprintBacklog": {
    "provided": true,
    "summary": "Sprint 42 focused on payment gateway integration. 12/15 stories completed. 3 rolled over due to production issues.",
    "keyItems": [
      "Payment Gateway Integration (rolled over)",
      "User Dashboard Redesign (completed)",
"API Rate Limiting (completed)",
      "Mobile Push Notifications (rolled over)",
      "Database Migration (completed with issues)"
    ]
  }
}
```

**AI Enhancement Example**:
```
Without context: "What went well this sprint?"

With sprint backlog:
- Poll: "Which completed story had the smoothest delivery?"
  - User Dashboard Redesign
  - API Rate Limiting
- Database Migration
  
- GeneralFeedback: "The Database Migration completed but had issues. What specific challenges did you encounter?"

- Rating: "How satisfied are you with the API Rate Limiting implementation?" (1-5)
```

#### 2. Incident Report

**When to provide**: Post-mortems, incident retrospectives, process improvement

**Schema**:
```json
{
  "incidentReport": {
    "provided": true,
    "summary": "Production outage on Jan 10th, 2:30 PM - 4:15 PM. Database connection pool exhausted during peak traffic. Affected 15% of active users.",
    "severity": "P0|P1|P2|P3|P4",
    "impactedSystems": ["Payment Service", "User Dashboard", "API Gateway"],
    "durationMinutes": 105,
    "customersImpacted": "15%",
 "revenueImpact": "$50,000 (estimated)"
  }
}
```

**AI Enhancement Example**:
```
Without context: "What caused the incident?"

With incident report:
- Poll: "What was the primary contributing factor to the database connection pool exhaustion?"
  - Unexpected traffic spike (Black Friday sale)
  - Insufficient connection pool configuration
  - Lack of auto-scaling
  - Missing monitoring alerts
  - Delayed incident detection

- GeneralFeedback: "The Payment Service, User Dashboard, and API Gateway were all affected. What dependencies or architectural patterns contributed to this cascading failure?"

- Rating: "How confident are you that we can detect this type of issue before it impacts customers?" (1-10)

- GeneralFeedback Categories:
  - "Monitoring Gaps"
  - "Architecture Issues"
  - "Configuration Problems"
  - "Preventive Measures"
```

#### 3. Product Documentation

**When to provide**: Product discovery, feature feedback, customer research

**Schema**:
```json
{
  "productDocumentation": {
    "provided": true,
  "summary": "New analytics dashboard with real-time metrics, custom reports, and data exports. Launched 2 weeks ago to 500 beta users.",
    "features": [
      "Real-time Metrics Dashboard",
      "Custom Report Builder",
      "CSV/Excel Export",
    "Scheduled Email Reports",
      "Team Sharing & Collaboration"
    ],
    "version": "2.0",
    "launchDate": "2025-01-01"
  }
}
```

**AI Enhancement Example**:
```
Without context: "What features do users want?"

With product docs:
- Rating: "How useful is each feature?" (1-5 per feature)
  - Real-time Metrics Dashboard
  - Custom Report Builder
  - CSV/Excel Export
  - Scheduled Email Reports
  - Team Sharing & Collaboration

- Poll: "Which missing feature would add the most value?"
  - PDF Export
  - More chart types (heatmaps, scatter plots)
  - Mobile app
  - API for programmatic access
  - Advanced filtering and drill-down

- WordCloud: "Describe the Custom Report Builder in one word"

- GeneralFeedback: "The Team Sharing feature was added based on user requests. How are you using it?"
```

#### 4. Custom Document

**When to provide**: Strategy sessions, compliance training, any other relevant context

**Schema**:
```json
{
  "customDocument": {
    "type": "Strategy Document|Customer Feedback|Policy Document|Training Material|Other",
    "summary": "Q1 2025 Engineering Strategy: Focus on platform scalability, developer experience, and security. Key initiatives: Migrate to Kubernetes, implement OAuth 2.0, reduce build times by 50%.",
    "keyPoints": [
      "Migrate monolith to microservices on Kubernetes",
   "Implement OAuth 2.0 for better security",
      "Reduce CI/CD build times from 30min to 15min",
    "Improve developer onboarding from 2 weeks to 3 days"
  ]
  }
}
```

---

## ?? User Interface Design

### Participant Type Selector

**Simple Mode** (Default - 90% of users):
```
??????????????????????????????????????????????
? Who will participate in this workshop?    ?
??????????????????????????????????????????????
?      ?
?  ? Technical Team    ?
?    Engineers, QA, DevOps  ?
?   ?
?  ? Managers & Team Leads            ?
?    Engineering managers, scrum masters     ?
?          ?
?  ? Business & Product        ?
?    Product managers, analysts, stakeholders?
?     ?
?  ? Leadership & Executives               ?
?    Directors, VPs, C-level         ?
?          ?
?  ? Mixed / Cross-functional           ?
?    Combination of multiple roles   ?
?           ?
?  [? Advanced Options...]  ?
??????????????????????????????????????????????
```

**Advanced Mode** (Expandable for power users):
```
??????????????????????????????????????????????
? Participant Breakdown (12 total)  ?
??????????????????????????????????????????????
?     ?
?  Technical:  [8___] ?? Engineers/QA        ?
?  Managers: [2___] ?? Team Leads        ?
?  Business:   [1___] ?? Product/Analysts    ?
?  Leaders:    [1___] ?? Directors/Execs     ?
?                     ?
??????????????????????????????????????????????
? Experience Levels     ?
??????????????????????????????????????????????
?            ?
?  Junior (0-2 years):     [2___]            ?
?  Mid-level (3-5 years):  [6___]    ?
?  Senior (6-10 years):    [3___]    ?
?  Expert (10+ years):  [1___] ?
?          ?
??????????????????????????????????????????????
? Specific Roles (Optional)    ?
??????????????????????????????????????????????
?     ?
?  • Backend Engineer      ?
?  • Frontend Engineer        ?
?  • QA Engineer         ?
?  • DevOps       ?
?  • Engineering Manager   ?
?  • Product Manager          ?
?           ?
?  [+ Add custom role]    ?
?             ?
??????????????????????????????????????????????
```

### Context Document Input

**Collapsed View** (Default):
```
??????????????????????????????????????????????
? ?? Context Documents (Optional)            ?
??????????????????????????????????????????????
?                ?
?  Provide context to help AI generate more  ?
?  relevant activities and questions.        ?
?       ?
?  ? Sprint Backlog            [Expand ?] ?
?  ? Incident Report           [Expand ?]    ?
?  ? Product Documentation     [Expand ?] ?
?  ? Custom Document           [Expand ?]    ?
?      ?
?  ?? Tip: Even brief summaries help AI      ?
?     create better workshops!     ?
?       ?
??????????????????????????????????????????????
```

**Expanded View - Sprint Backlog**:
```
??????????????????????????????????????????????
? ? Sprint Backlog     [Collapse ?]?
??????????????????????????????????????????????
?     ?
?  Sprint Summary (500 chars max)       ?
?  ????????????????????????????????????????  ?
?  ? Sprint 42 focused on payment gateway ?  ?
?  ? integration with Stripe. 12 out of   ?  ?
?  ? 15 stories completed. 3 rolled over  ?  ?
?  ? due to production hotfixes needed.   ?  ?
?  ?        142/500  ?  ?
?  ????????????????????????????????????????  ?
?    ?
?  Key Backlog Items (top 5-10)       ?
?  ?????????????????????????????????????????
?  ? • Payment Gateway Integration ?  ?
?  ?   Status: Rolled over          ?  ?
?  ?          ?  ?
?  ? • User Dashboard Redesign         ?  ?
?  ?   Status: Completed           ?  ?
?  ?    ?  ?
?  ? • API Rate Limiting       ?  ?
?  ?   Status: Completed           ?  ?
?  ?               ?  ?
?  ? • Mobile Push Notifications          ?  ?
?  ?   Status: Rolled over                ?  ?
?  ? ?  ?
?  ? • Database Migration     ?  ?
?  ?   Status: Completed (with issues)    ?  ?
?  ?    ?  ?
?  ? [+ Add item]         ?  ?
?  ????????????????????????????????????????  ?
?        ?
?  [? Save] [? Cancel]          ?
?             ?
??????????????????????????????????????????????
```

**Expanded View - Incident Report**:
```
??????????????????????????????????????????????
? ? Incident Report    [Collapse ?]?
??????????????????????????????????????????????
?         ?
?  Incident Summary (500 chars max)    ?
?  ????????????????????????????????????????  ?
?  ? Production outage on Jan 10th,       ?  ?
?  ? 2:30 PM - 4:15 PM. Database        ??
?  ? connection pool exhausted during     ?  ?
?  ? peak traffic (Black Friday sale).    ?  ?
?  ? Affected 15% of active users. ?  ?
?  ? Estimated revenue impact: $50K.      ?  ?
?  ?      236/500  ?  ?
?  ????????????????????????????????????????  ?
??
?  Severity: ? P0  ? P1  ? P2  ? P3  ? P4   ?
?          ?
?  Impacted Systems        ?
?  ????????????????????????????????????????  ?
?  ? • Payment Service          ??
?  ? • User Dashboard               ?  ?
?  ? • API Gateway     ?  ?
?  ? [+ Add system]      ?  ?
?  ????????????????????????????????????????  ?
?           ?
?  Additional Details (Optional)             ?
?Duration: [105] minutes             ?
?  Customers Impacted: [15]%   ?
?  Revenue Impact: [$50,000]         ?
?         ?
?  [? Save] [? Cancel]  ?
?      ?
??????????????????????????????????????????????
```

---

## ?? AI Prompt Engineering Enhancements

### Updated System Prompt Section

```markdown
## Participant Type Awareness

You have been provided with information about the workshop participants. Use this to tailor your session design:

### If Primary Type is "Technical":
- Use technical terminology confidently (APIs, CI/CD, microservices, containers, databases)
- Create polls about architecture decisions, code quality, testing strategies
- Ask about specific tools, frameworks, and technical debt
- Example poll options:
  - "Improve CI/CD pipeline reliability"
  - "Reduce database query optimization issues"
  - "Better test coverage for critical paths"
  - "Improve observability and monitoring"

### If Primary Type is "Managers":
- Focus on team dynamics, velocity, process efficiency, resource allocation
- Create polls about blockers, dependencies, team morale
- Ask about process improvements, communication, collaboration
- Example poll options:
  - "Improve cross-team communication"
  - "Better sprint planning and estimation"
  - "Reduce context switching for team members"
  - "Improve onboarding for new team members"

### If Primary Type is "Business":
- Use business terminology (ROI, customer value, revenue, market fit)
- Create polls about feature prioritization, customer impact, competitive analysis
- Ask about business outcomes, user needs, market opportunities
- Example poll options:
  - "Feature that drives most user adoption"
  - "Improvement with highest customer satisfaction impact"
  - "Integration that unlocks new market segments"
  - "Capability that reduces churn"

### If Primary Type is "Leaders":
- Focus on strategic alignment, organizational goals, resource allocation
- Create polls about budget priorities, organizational challenges, vision alignment
- Ask about long-term strategy, competitive positioning, talent development
- Example poll options:
  - "Strategic initiative with highest business impact"
  - "Organizational change to improve delivery"
  - "Investment area for competitive advantage"
  - "Key metric to measure success"

### If Primary Type is "Mixed":
- Balance technical and business perspectives
- Use clear, accessible language that bridges domains
- Create activities that highlight cross-functional collaboration challenges
- Example poll options:
  - "Technical debt vs. new feature development"
  - "Build custom solution vs. buy third-party tool"
  - "Improve quality vs. increase velocity"
  - "Focus on existing customers vs. new markets"

### Experience Level Adjustments:

**Junior-heavy teams (50%+ junior):**
- Provide more context in prompts
- Use clearer, more structured questions
- Offer more guided activities with examples
- Avoid overly abstract or strategic questions

**Senior/Expert-heavy teams (50%+ senior):**
- Allow more open-ended exploration
- Ask more strategic, architectural questions
- Use less prescriptive prompts
- Encourage deeper analysis and root cause thinking

## Context Document Integration

When context documents are provided, you MUST use them to create specific, relevant activities.

### Sprint Backlog Context:

If sprint backlog is provided:
1. Reference actual story names in poll options
2. Ask about specific completed/incomplete items
3. Create questions about rollover stories
4. Tailor prompts to sprint outcomes

Example transformations:
- Generic: "What went well?"
- Specific: "The User Dashboard Redesign was completed successfully. What practices made this story go smoothly?"

- Generic: "What should we improve?"
- Specific: "Payment Gateway Integration rolled over. What blocked completion?"

### Incident Report Context:

If incident report is provided:
1. Reference actual systems/services by name
2. Tailor questions to severity level (P0 = more depth)
3. Ask about specific contributing factors
4. Create activities around prevention

Example transformations:
- Generic: "What caused the incident?"
- Specific: "What monitoring gaps contributed to the 105-minute detection delay?"

- Generic: "How can we prevent this?"
- Specific: "The Payment Service, User Dashboard, and API Gateway all failed. What architectural patterns contributed to this cascade?"

### Product Documentation Context:

If product documentation is provided:
1. Reference actual features by name
2. Create feature-specific rating activities
3. Ask about documented capabilities
4. Suggest improvements aligned with roadmap

Example transformations:
- Generic: "What features do you want?"
- Specific: "We now support 5 currencies. Which additional currencies would add the most value?"

- Generic: "How's the product?"
- Specific: "Rate each feature: Real-time Metrics, Custom Reports, CSV Export, Scheduled Emails, Team Sharing"

### Custom Document Context:

If custom document is provided:
1. Extract key themes from keyPoints
2. Align activities with document objectives
3. Reference specific initiatives or goals
4. Create questions that move document themes forward

Example:
If strategy doc mentions "Migrate to Kubernetes":
- Poll: "What's the biggest Kubernetes migration challenge?"
  - Learning curve for team
  - Application refactoring needed
  - Infrastructure setup
  - Migration timing and risk

## Validation Rules:

1. If participant type is "Technical" and incident report is provided, create at least one technical deep-dive activity
2. If participant type is "Business" and product docs are provided, create at least one feature prioritization activity
3. If sprint backlog is provided, reference at least 2 backlog items in activities
4. If incident report severity is P0/P1, create more in-depth root cause activities
5. Always use specific names/terms from context documents rather than generic placeholders
```

---

## ?? Real-World Examples

### Example 1: Technical Team with Sprint Backlog

**Input**:
```json
{
  "description": "Sprint retrospective for backend team",
  "context": {
    "participantTypes": {
      "primary": "Technical",
      "breakdown": { "technical": 6, "managers": 1 },
      "experienceLevels": { "midLevel": 4, "senior": 3 }
    },
"contextDocuments": {
      "sprintBacklog": {
        "summary": "Payment gateway integration with Stripe API. 8 stories completed, 2 rolled over due to API rate limiting issues.",
        "keyItems": [
          "Stripe Integration API (completed)",
          "Webhook Handling (completed)",
  "Rate Limiting (rolled over - issues)",
          "Error Handling (completed)",
     "Payment Reconciliation (rolled over)"
    ]
      }
    }
  }
}
```

**AI-Generated Session** (Enhanced):

```json
{
  "name": "Sprint Retrospective - Stripe Integration Review",
  "activities": [
    {
      "order": 1,
      "type": "Poll",
      "title": "Most Challenging Integration Component",
      "prompt": "Which part of the Stripe integration presented the biggest technical challenge?",
      "config": {
        "options": [
   {
  "id": "api-integration",
            "label": "Stripe API Integration",
         "description": "Initial setup and authentication"
  },
          {
            "id": "webhook-handling",
          "label": "Webhook Handling",
     "description": "Processing async payment events"
          },
{
            "id": "rate-limiting",
     "label": "Rate Limiting Implementation",
            "description": "Managing API rate limits (caused rollover)"
          },
      {
      "id": "error-handling",
        "label": "Error Handling & Retries",
   "description": "Handling failed payments gracefully"
          },
  {
      "id": "reconciliation",
      "label": "Payment Reconciliation",
     "description": "Matching payments to orders (rolled over)"
          }
        ]
      }
    },
    {
  "order": 2,
      "type": "GeneralFeedback",
      "title": "Rate Limiting Deep Dive",
      "prompt": "Rate Limiting rolled over due to technical issues. What specific challenges did you encounter? What solutions did you try?",
      "config": {
        "categoriesEnabled": true,
   "categories": [
          { "id": "challenges", "label": "Technical Challenges", "icon": "??" },
{ "id": "solutions-tried", "label": "Solutions Attempted", "icon": "??" },
       { "id": "lessons-learned", "label": "Lessons Learned", "icon": "??" },
          { "id": "next-steps", "label": "Next Sprint Actions", "icon": "??" }
        ]
      }
    },
    {
      "order": 3,
      "type": "Rating",
      "title": "Implementation Confidence",
      "prompt": "How confident are you in the current Webhook Handling implementation for production use?",
"config": {
        "scale": 5,
        "minLabel": "1 - Needs significant work",
        "maxLabel": "5 - Production ready",
        "allowComments": true
   }
    },
    {
      "order": 4,
      "type": "WordCloud",
      "title": "Integration Experience",
      "prompt": "Describe the Stripe integration sprint in one word"
    }
  ]
}
```

---

### Example 2: Mixed Team with Incident Report

**Input**:
```json
{
  "description": "Post-mortem for Black Friday database outage",
  "context": {
    "participantTypes": {
      "primary": "Mixed",
  "breakdown": {
        "technical": 4,
        "managers": 2,
  "business": 1,
        "leaders": 1
      }
    },
    "contextDocuments": {
      "incidentReport": {
        "summary": "Database connection pool exhausted during Black Friday sale peak. 105-minute outage affecting Payment Service, User Dashboard, and API Gateway. 15% of users impacted, $50K revenue loss.",
     "severity": "P0",
  "impactedSystems": ["Payment Service", "User Dashboard", "API Gateway"]
      }
    }
  }
}
```

**AI-Generated Session** (Enhanced):

```json
{
  "name": "Post-Mortem: Black Friday Database Outage",
  "activities": [
    {
      "order": 1,
      "type": "GeneralFeedback",
    "title": "Incident Timeline & Response",
      "prompt": "Walk through what happened during the 105-minute outage. What went well in our incident response?",
      "config": {
        "categoriesEnabled": true,
        "categories": [
          { "id": "detection", "label": "Detection & Alerting", "icon": "??" },
          { "id": "communication", "label": "Communication", "icon": "??" },
   { "id": "resolution", "label": "Resolution Actions", "icon": "??" },
          { "id": "customer-impact", "label": "Customer Impact Mitigation", "icon": "??" }
        ]
      }
  },
    {
      "order": 2,
      "type": "Poll",
      "title": "Primary Root Cause",
      "prompt": "What was the primary factor that led to database connection pool exhaustion?",
   "config": {
        "options": [
          {
        "id": "traffic-spike",
      "label": "Unexpected traffic spike (Black Friday sale)",
    "description": "Traffic exceeded capacity planning estimates"
          },
          {
      "id": "config",
   "label": "Insufficient connection pool configuration",
   "description": "Pool size not tuned for peak load"
   },
       {
 "id": "monitoring",
            "label": "Lack of proactive monitoring",
 "description": "No alerts before pool exhaustion"
       },
        {
       "id": "autoscaling",
      "label": "Missing auto-scaling configuration",
            "description": "No automatic scale-up during demand spike"
          },
          {
     "id": "cascade",
            "label": "Cascading failure across services",
            "description": "Payment, Dashboard, API Gateway all impacted"
          }
        ]
      }
    },
    {
  "order": 3,
      "type": "Rating",
      "title": "Incident Preparedness Assessment",
      "prompt": "How prepared were we to detect and respond to this type of incident?",
      "config": {
      "scale": 10,
   "minLabel": "1 - Not prepared at all",
        "maxLabel": "10 - Fully prepared",
        "midpointLabel": "5 - Somewhat prepared",
        "allowComments": true,
        "commentRequired": true,
 "commentPlaceholder": "What gaps in preparedness did you notice?"
      }
    },
    {
      "order": 4,
      "type": "GeneralFeedback",
      "title": "Preventive Measures & Action Items",
  "prompt": "Given the Payment Service, User Dashboard, and API Gateway all failed, what architectural, monitoring, or process changes should we implement?",
      "config": {
        "categoriesEnabled": true,
        "categories": [
          { "id": "monitoring", "label": "Monitoring & Alerting", "icon": "??" },
    { "id": "architecture", "label": "Architecture Changes", "icon": "???" },
          { "id": "capacity", "label": "Capacity Planning", "icon": "??" },
          { "id": "process", "label": "Process Improvements", "icon": "??" }
        ],
        "maxResponsesPerParticipant": 5
      }
    }
  ]
}
```

---

## ?? Cost Impact Analysis

### Token Usage Breakdown

| Component | Base Tokens | With Participant Types | With Context Docs | Combined |
|-----------|-------------|------------------------|-------------------|----------|
| System Prompt | 2,000 | 2,100 (+5%) | 2,000 | 2,200 (+10%) |
| User Prompt | 500 | 550 (+10%) | 900 (+80%) | 1,000 (+100%) |
| AI Response | 3,000 | 3,200 (+7%) | 3,500 (+17%) | 3,800 (+27%) |
| **Total** | **5,500** | **5,850** | **6,400** | **7,000** |
| **Cost/Request** | **$0.255** | **$0.272** | **$0.297** | **$0.325** |

### Monthly Cost Estimates

**Assumptions**:
- 300 sessions generated per month
- 50% use participant types
- 30% use context documents
- 20% use both

**Calculation**:
```
Base (50%):        150 × $0.255 = $38.25
With Types (20%): 60 × $0.272 = $16.32
With Context (10%): 30 × $0.297 = $8.91
With Both (20%):    60 × $0.325 = $19.50

Total: $83/month (vs. $76.50 base, +8.5%)
```

### Cost Mitigation Strategies

1. **Make context optional** (already planned)
2. **Limit context document length** (500 char max summaries)
3. **Cache responses for similar requests**
4. **Use GPT-3.5-turbo for simple sessions** (90% cheaper)
5. **Summarize long context before sending to AI**

---

## ?? Security & Privacy Considerations

### Sensitive Data in Context Documents

**Risks**:
- Sprint backlog may contain customer names, project codenames
- Incident reports may include sensitive system details, revenue figures
- Product docs may contain unreleased feature information

**Mitigations**:

1. **Data Sanitization**:
```typescript
function sanitizeContextDocument(doc: ContextDocument): ContextDocument {
  return {
    ...doc,
    summary: redactPII(doc.summary),
    keyItems: doc.keyItems.map(item => redactPII(item)),
    // Remove email addresses
    // Remove phone numbers
    // Remove customer names
    // Redact revenue figures above threshold
  };
}
```

2. **User Warnings**:
```
??????????????????????????????????????????????
? ?? Important: Data Privacy        ?
??????????????????????????????????????????????
??
? Context documents will be sent to Azure   ?
? OpenAI for session generation.      ?
?    ?
? Please ensure:   ?
? ? No customer PII (names, emails)         ?
? ? No sensitive financial data             ?
? ? No unreleased product information ?
?       ?
? ? I confirm no sensitive data is included ?
?     ?
? [Learn more about data handling]          ?
?   ?
??????????????????????????????????????????????
```

3. **Data Retention Policy**:
- Context documents stored for 30 days (audit/debugging)
- Allow users to delete after session creation
- Encrypt at rest and in transit
- Log what was sent to AI (compliance)

4. **GDPR Compliance**:
- Right to deletion (delete stored context on request)
- Right to access (show what was sent to AI)
- Consent tracking (log user confirmation checkbox)
- Data minimization (summarize, don't send full docs)

---

## ? Implementation Checklist

### Phase 1: Participant Types

- [ ] Update `GenerateSessionRequest` contract with `participantTypes` field
- [ ] Create UI component for participant type selector
- [ ] Update AI prompt builder to include participant type context
- [ ] Add participant type to system prompt
- [ ] Test with different participant configurations
- [ ] Document participant type guidelines

### Phase 2: Context Documents

- [ ] Update `GenerateSessionRequest` with `contextDocuments` field
- [ ] Create UI components for each document type (Sprint, Incident, Product, Custom)
- [ ] Implement document summarization logic (500 char limit)
- [ ] Add PII sanitization service
- [ ] Update AI prompt builder to include context documents
- [ ] Add context integration rules to system prompt
- [ ] Implement user consent checkbox
- [ ] Add data retention and deletion logic

### Phase 3: Testing & Validation

- [ ] Test with technical teams + sprint backlog
- [ ] Test with mixed teams + incident report
- [ ] Test with business users + product docs
- [ ] Validate AI uses context appropriately
- [ ] Check token usage and costs
- [ ] Security review of PII handling
- [ ] Performance testing with large contexts

---

## ?? Related Documentation

- **Parent Document**: `docs/ai-session-generation-design.md`
- **AI Prompt**: `docs/ai-prompts/session-generation-system-prompt.md`
- **Template Schema**: `docs/template-schema.md`
- **Activity Type Status**: `docs/activity-type-status.md`

---

**Document Version**: 1.1  
**Last Updated**: January 2025  
**Status**: Ready for Review
