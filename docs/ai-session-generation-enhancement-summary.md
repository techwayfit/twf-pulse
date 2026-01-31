# AI Session Generation - Enhancement Summary

> **Purpose**: Quick summary of participant types and context documents enhancements  
> **Status**: Ready for Review  
> **Last Updated**: January 2025

---

## ?? What Changed

### 1. **Participant Types** (NEW)

Added ability to specify WHO will attend the workshop:

**Simple Selection**:
- Technical (engineers, QA, DevOps)
- Managers (team leads, scrum masters)
- Business (product managers, analysts)
- Leaders (directors, executives)
- Mixed (cross-functional)

**Advanced Options** (optional):
- Exact breakdown (e.g., 8 engineers, 2 managers, 1 PM)
- Experience levels (junior, mid, senior, expert)
- Custom roles (Backend Engineer, Frontend Engineer, etc.)

**Why it matters**: AI adjusts language, question complexity, and activity types based on audience.

---

### 2. **Context Documents** (NEW)

Added ability to provide relevant artifacts to AI:

**Document Types**:
1. **Sprint Backlog** - For retrospectives, planning sessions
2. **Incident Report** - For post-mortems, incident reviews
3. **Product Documentation** - For feature feedback, customer research
4. **Custom Document** - Strategy docs, training materials, etc.

**What to provide**:
- Brief summary (500 chars max)
- Key items/features/systems (5-10 bullet points)
- Metadata (severity, dates, versions)

**Why it matters**: AI generates specific questions referencing actual backlog items, incidents, or features instead of generic questions.

---

## ?? Example Improvements

### Without Enhancements (Generic)

**Input**: "Sprint retrospective for 10 people"

**AI Output**:
```
Activities:
1. WordCloud: "Describe the sprint in one word"
2. Poll: "What should we improve?"
   - Planning
   - Development
   - Testing
   - Deployment
3. Rating: "How satisfied are you with the sprint?"
```

### With Enhancements (Specific & Relevant)

**Input**: 
```json
{
  "description": "Sprint retrospective for backend team",
  "participantTypes": {
    "primary": "Technical",
    "breakdown": { "technical": 8, "managers": 2 }
  },
  "contextDocuments": {
    "sprintBacklog": {
      "summary": "Payment gateway integration. 12/15 stories done, 3 rolled over.",
      "keyItems": [
        "Stripe Integration (completed)",
"Rate Limiting (rolled over - issues)",
        "Webhook Handling (completed)"
      ]
    }
  }
}
```

**AI Output** (Enhanced):
```
Activities:
1. Poll: "Which Stripe integration component was most challenging?"
   - Stripe API Integration (completed)
   - Webhook Handling (completed)
   - Rate Limiting (rolled over - issues)
   - Error Handling
   - Payment Reconciliation

2. GeneralFeedback: "Rate Limiting rolled over due to issues. What specific challenges did you encounter?"
   Categories: Technical Challenges | Solutions Tried | Lessons Learned | Next Sprint Actions

3. Rating: "How confident are you in the Webhook Handling implementation for production?"

4. WordCloud: "Describe the Stripe integration sprint in one word"
```

**Difference**: AI now asks about **actual backlog items** and uses **technical terminology** appropriate for engineers.

---

## ?? Cost Impact

| Scenario | Token Usage | Cost/Request | Monthly (300 sessions) |
|----------|-------------|--------------|------------------------|
| **Base** (no enhancements) | 5,500 | $0.255 | $76.50 |
| **With Participant Types** | 5,850 | $0.272 | $81.60 |
| **With Context Docs** | 6,400 | $0.297 | $89.10 |
| **With Both** | 7,000 | $0.325 | $97.50 |

**Realistic Estimate** (assuming 50% use participant types, 30% use context):
- **~$83/month** (+8.5% from base)

---

## ?? User Experience

### Participant Type Selector (Optional)

**Simple Mode** (Default):
```
Who will participate?
? Technical Team (Engineers, QA, DevOps)
? Managers (Team Leads, Scrum Masters)
? Business (Product, Analysts)
? Leaders (Directors, Executives)
? Mixed (Cross-functional)

[Advanced Options ?]
```

**Advanced Mode** (Expandable):
```
Participant Breakdown (12 total)
Technical:  [8] engineers
Managers:   [2] team leads
Business:   [1] product manager
Leaders:    [1] director

Experience Levels:
Junior (0-2 yrs):   [2]
Mid (3-5 yrs):  [6]
Senior (6-10 yrs):  [3]
Expert (10+ yrs):   [1]
```

### Context Document Input (Optional)

```
Context Documents (Optional - helps AI generate better sessions)

? Sprint Backlog       [Expand ?]
? Incident Report[Expand ?]
? Product Documentation   [Expand ?]
? Custom Document         [Expand ?]

?? Even brief summaries help AI create relevant activities!
```

**Expanded - Sprint Backlog**:
```
? Sprint Backlog     [Collapse ?]

Summary (500 chars max):
??????????????????????????????????????
? Sprint 42 focused on payment      ?
? gateway integration. 12/15 done.  ?
? 3 rolled over due to hotfixes.    ?
?          142/500    ?
??????????????????????????????????????

Key Items (top 5-10):
• Payment Gateway Integration (rolled over)
• User Dashboard Redesign (completed)
• API Rate Limiting (completed)
• Database Migration (completed with issues)
[+ Add item]

?? Important: No customer PII or sensitive data
? I confirm no sensitive data included

[? Save] [? Cancel]
```

---

## ?? Security Considerations

### Data Sanitization

Before sending to AI, we will:
1. Remove email addresses
2. Remove phone numbers
3. Remove customer names
4. Redact sensitive financial figures
5. Strip PII from summaries

### User Warnings

```
?? Data Privacy Notice

Context documents will be sent to Azure OpenAI.
Please ensure:
? No customer PII (names, emails)
? No sensitive financial data
? No unreleased product secrets

? I confirm no sensitive data is included

[Learn more about data handling]
```

### Data Retention

- Context stored for 30 days (audit/debugging)
- User can delete after session creation
- Encrypted at rest and in transit
- GDPR compliant (right to deletion)

---

## ? Approval Questions

### 1. Participant Types

**Q**: Should participant types be required or optional?  
**Recommendation**: **Optional** - Default to "Mixed" if not provided

**Q**: Should we show advanced breakdown by default or hide it?  
**Recommendation**: **Hide** - Show "Advanced Options" expandable section

**Q**: How many custom roles should we allow?  
**Recommendation**: **10 max** - Prevents UI clutter

---

### 2. Context Documents

**Q**: Should we allow file uploads (PDF, DOCX) or just text summaries?  
**Recommendation**: **Text summaries** for Phase 1, file upload in Phase 2

**Q**: Should context documents be required for certain workshop types?  
**Recommendation**: **Always optional** - AI can still generate good sessions without context

**Q**: Should we store context documents long-term?  
**Recommendation**: **30 days only** - For audit/debugging, then auto-delete

**Q**: Should we show a warning about sensitive data?  
**Recommendation**: **Yes** - Prominent warning + confirmation checkbox

---

### 3. Cost Management

**Q**: Should we limit context document usage to control costs?  
**Recommendation**: **No hard limits** - Monitor usage, optimize later if needed

**Q**: Should we charge extra for AI generation with context?  
**Recommendation**: **Not initially** - Include in base AI generation quota

**Q**: Should we summarize long contexts automatically?  
**Recommendation**: **Yes** - Enforce 500 char limit, summarize if exceeded

---

## ?? Documentation Created

1. **`docs/ai-session-generation-design.md`** - Main design document (updated with request schema)
2. **`docs/ai-session-generation-enhancements.md`** - Detailed enhancement guide (NEW)
3. **`docs/ai-prompts/session-generation-system-prompt.md`** - AI prompt (updated with rules)
4. **`docs/ai-session-generation-enhancement-summary.md`** - This summary (NEW)

---

## ?? Next Steps (When Approved)

### Implementation Order

**Phase 1** (2-3 days):
1. Update `GenerateSessionRequest` contract
2. Create participant type selector UI component
3. Update AI prompt builder service
4. Test with different participant types

**Phase 2** (3-4 days):
1. Add context document input UI components
2. Implement document sanitization
3. Update AI prompt builder with context integration
4. Add user consent/warning flows

**Phase 3** (1-2 days):
1. End-to-end testing with real scenarios
2. Security review
3. Performance/cost monitoring
4. Documentation for facilitators

---

## ?? Key Benefits

### For Facilitators
- ? Less time spent crafting relevant questions
- ? AI understands their specific situation
- ? Generated sessions feel personalized, not generic
- ? No need to manually reference sprint items or incidents

### For Participants
- ? More relevant, engaging activities
- ? Questions reference their actual work
- ? Appropriate complexity for their experience level
- ? Higher quality workshops overall

### For the Platform
- ? Differentiation from generic AI tools
- ? Higher user satisfaction
- ? Better workshop outcomes
- ? Increased feature adoption

---

## ? Open Questions for You

1. Should participant types be required or optional? **(Recommend: Optional)**
2. Should we allow file uploads or just text summaries? **(Recommend: Text for Phase 1)**
3. Should we show a data privacy warning? **(Recommend: Yes, prominent)**
4. Should we limit context document length? **(Recommend: Yes, 500 chars)**
5. Should we store context documents? If yes, for how long? **(Recommend: 30 days)**
6. Should we charge extra for AI with context? **(Recommend: No, include in base)**

---

**Status**: ?? **Ready for Your Review**

Please review and let me know:
- ? Approve as designed
- ?? Request specific changes
- ? Need clarification on anything

Once approved, I'll proceed with implementation planning and create detailed technical specs.

---

**Document Version**: 1.0  
**Last Updated**: January 2025
