# TechWayFit Pulse - Template Schema Documentation

This document provides a comprehensive guide to the JSON template structure used in TechWayFit Pulse for creating pre-configured workshop sessions.

## Table of Contents
- [Overview](#overview)
- [Template File Location](#template-file-location)
- [Root Schema Structure](#root-schema-structure)
- [Detailed Field Specifications](#detailed-field-specifications)
- [Activity Types & Configurations](#activity-types--configurations)
- [Complete Examples](#complete-examples)
- [Best Practices](#best-practices)

---

## Overview

Templates are JSON files that define complete workshop session configurations, including:
- Session metadata (name, description, category)
- Default settings (duration, anonymity, late join policies)
- Join form schema for participant information
- Pre-configured activities in a specific sequence

Templates are automatically loaded on application startup from `App_Data/Templates/` directory.

---

## Template File Location

**Directory**: `src/TechWayFit.Pulse.Web/App_Data/Templates/`

**File naming convention**: `kebab-case-name.json` (e.g., `strategic-planning.json`)

**Auto-installation**: On startup, templates are:
1. Validated and loaded into the `SessionTemplates` database table
2. Moved to `Templates/installed/` subdirectory
3. Made available for session creation via the UI

---

## Root Schema Structure

```json
{
  "name": "String (Required)",
  "description": "String (Required)",
  "category": "Enum (Required)",
  "iconEmoji": "String (Required)",
  "config": {
    "title": "String (Required)",
    "goal": "String (Required)",
    "context": "String (Optional)",
    "settings": { /* Settings Object */ },
    "joinFormSchema": { /* Join Form Object */ },
    "activities": [ /* Array of Activity Objects */ ]
  }
}
```

---

## Detailed Field Specifications

### 1. Template Metadata

#### `name` (string, required)
- **Purpose**: Display name of the template
- **Constraints**: 3-100 characters
- **Examples**: 
  - ✅ "Strategic Planning"
  - ✅ "Employee Onboarding"
  - ✅ "Agile Retrospective - Sprint Review"
  - ❌ "SP" (too short)

#### `description` (string, required)
- **Purpose**: Brief summary shown in template selection UI
- **Constraints**: 10-200 characters
- **Best Practice**: Highlight key activities or unique features
- **Examples**:
  - ✅ "Executive strategy session with prioritization"
  - ✅ "Interactive orientation for new hires"
  - ✅ "Root cause analysis with action planning"

#### `category` (enum, required)
- **Purpose**: Organizes templates by use case type
- **Accepted Values**:
  - `"Retrospective"` - Team retrospectives, sprint reviews, lessons learned
  - `"ProductDiscovery"` - Product planning, roadmap sessions, customer research
  - `"IncidentReview"` - Post-mortems, incident analysis, blameless reviews
  - `"TeamBuilding"` - Onboarding, team culture, social activities
  - `"Training"` - Educational sessions, workshops, skill development
- **Example**: `"category": "Retrospective"`

#### `iconEmoji` (string, required)
- **Purpose**: Visual identifier for the template
- **Format**: Single emoji character
- **Examples**:
  - `"🔄"` - Retrospectives
  - `"🎯"` - Strategy/Planning
  - `"🚨"` - Incidents
  - `"👋"` - Onboarding
  - `"💻"` - Technical/Hackathons
  - `"🚀"` - Innovation
  - `"📊"` - Analytics/Feedback

---

### 2. Config Object

#### `config.title` (string, required)
- **Purpose**: Default session title (can be changed by facilitator)
- **Constraints**: 5-200 characters
- **Example**: `"Strategic Planning Workshop"`

#### `config.goal` (string, required)
- **Purpose**: Primary objective of the session
- **Constraints**: 10-500 characters
- **Best Practice**: Start with an action verb
- **Examples**:
  - "Align on strategic priorities and action plans"
  - "Help new employees learn about company culture"
  - "Identify root causes and prevent future incidents"

#### `config.context` (string, optional)
- **Purpose**: Additional background or scenario description
- **Constraints**: Up to 1000 characters
- **Example**: `"Executive team strategy session for Q2 planning"`

---

### 3. Settings Object

#### `settings.durationMinutes` (integer, required)
- **Purpose**: Expected workshop duration
- **Range**: 15-600 minutes (15 min to 10 hours)
- **Recommendations**:
  - Quick check-ins: 15-30 min
  - Team meetings: 60-90 min
  - Workshops: 120-180 min
  - Full-day sessions: 360-480 min
- **Example**: `"durationMinutes": 90`

#### `settings.allowAnonymous` (boolean, required)
- **Purpose**: Allow participants to submit responses without identification
- **Values**:
  - `true` - Anonymous participation allowed (encourages honest feedback)
  - `false` - All responses attributed to participants
- **Use Cases**:
  - `true` for: Sensitive feedback, 360 reviews, honest retrospectives
  - `false` for: Accountability sessions, team building, onboarding
- **Example**: `"allowAnonymous": true`

#### `settings.allowLateJoin` (boolean, required)
- **Purpose**: Allow participants to join after session has started
- **Values**:
  - `true` - Late joiners allowed
  - `false` - Session locked after start
- **Use Cases**:
  - `true` for: Large events, flexible workshops, casual sessions
  - `false` for: Small teams, critical planning, time-sensitive activities
- **Example**: `"allowLateJoin": true`

#### `settings.showResultsDuringActivity` (boolean, required)
- **Purpose**: Display real-time results while activity is active
- **Values**:
  - `true` - Results visible during activity (builds energy and engagement)
  - `false` - Results shown only after activity closes (prevents groupthink)
- **Use Cases**:
  - `true` for: Word clouds, polls, brainstorming
  - `false` for: Voting on sensitive topics, independent assessments
- **Example**: `"showResultsDuringActivity": true`

---

### 4. Join Form Schema

The join form collects participant information when they join the session.

#### Structure
```json
"joinFormSchema": {
  "fields": [
    {
      "name": "string (required)",
      "label": "string (required)",
      "type": "text|select (required)",
      "required": true|false,
      "options": ["array", "of", "strings"] // Only for type="select"
    }
  ]
}
```

#### Field Properties

**`name`** (string, required)
- Unique identifier for the field
- Format: camelCase
- Examples: `"name"`, `"department"`, `"role"`, `"teamName"`

**`label`** (string, required)
- Display text shown to participants
- Examples: `"Your Name"`, `"Department"`, `"What's your role?"`

**`type`** (enum, required)
- Field input type
- Values:
  - `"text"` - Single-line text input
  - `"select"` - Dropdown with predefined options

**`required`** (boolean, required)
- Whether field must be filled
- `true` - Participant cannot join without completing
- `false` - Field is optional

**`options`** (array of strings, conditional)
- **Required when**: `type: "select"`
- **Not used when**: `type: "text"`
- List of dropdown choices
- Example: `["Manager", "Individual Contributor", "Executive"]`

#### Common Join Form Patterns

**Minimal (Name Only)**
```json
"joinFormSchema": {
  "fields": [
    {
      "name": "name",
      "label": "Your Name",
      "type": "text",
      "required": true
    }
  ]
}
```

**Role-Based**
```json
"joinFormSchema": {
  "fields": [
    {
      "name": "name",
      "label": "Your Name",
      "type": "text",
      "required": true
    },
    {
      "name": "role",
      "label": "Role",
      "type": "select",
      "required": true,
      "options": ["Engineer", "Designer", "Product Manager", "QA"]
    }
  ]
}
```

**Detailed**
```json
"joinFormSchema": {
  "fields": [
    {
      "name": "name",
      "label": "Your Name",
      "type": "text",
      "required": true
    },
    {
      "name": "department",
      "label": "Department",
      "type": "text",
      "required": true
    },
    {
      "name": "experienceLevel",
      "label": "Experience Level",
      "type": "select",
      "required": false,
      "options": ["0-2 years", "3-5 years", "6-10 years", "10+ years"]
    }
  ]
}
```

---

### 5. Activities Array

Activities define the interactive components of the session in sequential order.

#### Base Activity Structure
```json
{
  "order": 1,
  "type": "ActivityType",
  "title": "Activity Title",
  "prompt": "Question or instruction for participants",
  "config": { /* Activity-specific configuration */ }
}
```

#### Common Properties (All Activity Types)

**`order`** (integer, required)
- Sequence number for activity execution
- Must be unique within the session
- Typically: 1, 2, 3, 4...
- Determines the order activities appear in the facilitator view

**`type`** (enum, required)
- Activity type identifier
- See [Activity Types](#activity-types--configurations) section

**`title`** (string, required)
- Activity name displayed to participants and facilitators
- Constraints: 3-200 characters
- Examples: "Root Cause Analysis", "Vision Keywords", "Team Priorities"

**`prompt`** (string, required)
- Question or instruction shown to participants
- Constraints: 10-1000 characters
- Best Practice: Make it clear and actionable
- Examples:
  - "What went well during this sprint?"
  - "Share your innovative ideas"
  - "Map initiatives by impact and effort"

**`config`** (object, optional/required based on activity type)
- Activity-specific configuration
- See individual activity type sections below

---

## Activity Types & Configurations

### 1. WordCloud

**Purpose**: Collect single words or short phrases to visualize common themes

**Type**: `"WordCloud"`

**Config**: No configuration required (config object can be omitted or empty)

**Example**:
```json
{
  "order": 1,
  "type": "WordCloud",
  "title": "First Impressions",
  "prompt": "Describe your first impression of the company in one word"
}
```

**Participant Experience**:
- Submits single word or short phrase
- Sees real-time word cloud with size based on frequency

**Best Use Cases**:
- Brainstorming keywords
- Capturing emotions or themes
- Icebreaker activities
- Vision/mission exercises

---

### 2. Poll

**Purpose**: Vote on predefined options (single or multiple choice)

**Type**: `"Poll"`

**Config** (required):
```json
"config": {
  "options": ["Option 1", "Option 2", "Option 3"],
  "allowMultiple": false  // Optional, defaults to false
}
```

**Config Properties**:
- `options` (array of strings, required): List of choices (2-10 options recommended)
- `allowMultiple` (boolean, optional): 
  - `false` (default) - Single selection
  - `true` - Multiple selections allowed

**Example**:
```json
{
  "order": 2,
  "type": "Poll",
  "title": "Learning Preference",
  "prompt": "How do you prefer to learn new things?",
  "config": {
    "options": [
      "Hands-on practice",
      "Reading documentation",
      "Watching demos",
      "Pair programming",
      "Asking questions"
    ]
  }
}
```

**Participant Experience**:
- Selects one or more options
- Sees bar chart or pie chart of results

**Best Use Cases**:
- Decision making
- Preference surveys
- Priority voting
- Quick assessments

---

### 3. Quiz

**Purpose**: Knowledge assessment with correct answers

**Type**: `"Quiz"`

**Config** (required):
```json
"config": {
  "questions": [
    {
      "question": "Question text",
      "options": ["Option 1", "Option 2", "Option 3"],
      "correctAnswer": "Option 1"
    }
  ]
}
```

**Config Properties**:
- `questions` (array, required): List of quiz questions
  - `question` (string): Question text
  - `options` (array of strings): Answer choices
  - `correctAnswer` (string): Must match one of the options exactly

**Example**:
```json
{
  "order": 1,
  "type": "Quiz",
  "title": "Security Awareness Check",
  "prompt": "Test your cybersecurity knowledge",
  "config": {
    "questions": [
      {
        "question": "What is the most secure password practice?",
        "options": [
          "Use the same password everywhere",
          "Use a password manager with unique passwords",
          "Write passwords on sticky notes",
          "Share passwords with teammates"
        ],
        "correctAnswer": "Use a password manager with unique passwords"
      }
    ]
  }
}
```

**Participant Experience**:
- Answers questions
- Receives immediate feedback on correctness
- Sees aggregate results

**Best Use Cases**:
- Training assessments
- Compliance verification
- Knowledge checks
- Educational workshops

---

### 4. QnA (Questions & Answers)

**Purpose**: Open discussion forum for questions and answers

**Type**: `"QnA"`

**Config**: No configuration required (config can be omitted or empty)

**Example**:
```json
{
  "order": 1,
  "type": "QnA",
  "title": "Ask Me Anything",
  "prompt": "Share your questions about the new product release"
}
```

**Participant Experience**:
- Submits questions
- Can upvote/like questions from others
- See questions sorted by popularity

**Best Use Cases**:
- Town halls
- AMA (Ask Me Anything) sessions
- Onboarding Q&A
- Open forums

---

### 5. Rating

**Purpose**: Numerical rating or scoring

**Type**: `"Rating"`

**Config** (required):
```json
"config": {
  "maxRating": 5,              // Required
  "ratingLabel": "Label text"  // Optional
}
```

**Config Properties**:
- `maxRating` (integer, required): Maximum rating value (typically 5 or 10)
- `ratingLabel` (string, optional): Descriptor for what's being rated

**Example**:
```json
{
  "order": 5,
  "type": "Rating",
  "title": "Session Effectiveness",
  "prompt": "How would you rate this workshop?",
  "config": {
    "maxRating": 5,
    "ratingLabel": "Overall Quality"
  }
}
```

**Participant Experience**:
- Selects rating (e.g., 1-5 stars)
- Sees average rating and distribution

**Best Use Cases**:
- Satisfaction surveys
- Performance feedback
- Quality assessments
- NPS (Net Promoter Score) style questions

---

### 6. Quadrant

**Purpose**: 2D mapping of items across two dimensions

**Type**: `"Quadrant"`

**Config** (required):
```json
"config": {
  "xAxisLabel": "Horizontal axis label",
  "yAxisLabel": "Vertical axis label",
  "topLeftLabel": "Top-left quadrant label",
  "topRightLabel": "Top-right quadrant label",
  "bottomLeftLabel": "Bottom-left quadrant label",
  "bottomRightLabel": "Bottom-right quadrant label"
}
```

**Config Properties** (all required):
- `xAxisLabel` (string): Label for horizontal axis (e.g., "Effort", "Complexity")
- `yAxisLabel` (string): Label for vertical axis (e.g., "Impact", "Value")
- `topLeftLabel` (string): Description for top-left quadrant (low X, high Y)
- `topRightLabel` (string): Description for top-right quadrant (high X, high Y)
- `bottomLeftLabel` (string): Description for bottom-left quadrant (low X, low Y)
- `bottomRightLabel` (string): Description for bottom-right quadrant (high X, high Y)

**Example**:
```json
{
  "order": 3,
  "type": "Quadrant",
  "title": "Initiative Prioritization",
  "prompt": "Map initiatives by impact and effort",
  "config": {
    "xAxisLabel": "Effort Required",
    "yAxisLabel": "Strategic Impact",
    "topLeftLabel": "High Impact, Low Effort (Quick Wins)",
    "topRightLabel": "High Impact, High Effort (Long-term Bets)",
    "bottomLeftLabel": "Low Impact, Low Effort (Nice to Have)",
    "bottomRightLabel": "Low Impact, High Effort (Avoid)"
  }
}
```

**Common Quadrant Patterns**:

**Eisenhower Matrix (Urgency vs Importance)**
```json
{
  "xAxisLabel": "Urgency",
  "yAxisLabel": "Importance",
  "topLeftLabel": "Important, Not Urgent (Plan)",
  "topRightLabel": "Important & Urgent (Do First)",
  "bottomLeftLabel": "Not Important, Not Urgent (Eliminate)",
  "bottomRightLabel": "Urgent, Not Important (Delegate)"
}
```

**Risk Matrix (Likelihood vs Impact)**
```json
{
  "xAxisLabel": "Likelihood",
  "yAxisLabel": "Impact",
  "topLeftLabel": "High Impact, Low Likelihood (Monitor)",
  "topRightLabel": "High Impact, High Likelihood (Critical Risk)",
  "bottomLeftLabel": "Low Impact, Low Likelihood (Accept)",
  "bottomRightLabel": "Low Impact, High Likelihood (Mitigate)"
}
```

**Skills Matrix (Confidence vs Experience)**
```json
{
  "xAxisLabel": "Experience Level",
  "yAxisLabel": "Confidence",
  "topLeftLabel": "High Confidence, Low Experience",
  "topRightLabel": "Expert (High Confidence & Experience)",
  "bottomLeftLabel": "Beginner (Learning)",
  "bottomRightLabel": "Experienced but Unsure"
}
```

**Participant Experience**:
- Drags items onto 2D grid
- Places items in quadrants based on two criteria
- Sees collective mapping

**Best Use Cases**:
- Prioritization exercises
- Risk assessment
- Strategic planning
- Skills mapping
- SWOT analysis variations

---

### 7. FiveWhys

**Purpose**: Root cause analysis using the "5 Whys" technique

**Type**: `"FiveWhys"`

**Config** (optional):
```json
"config": {
  "maxDepth": 5  // Optional, defaults to 5
}
```

**Config Properties**:
- `maxDepth` (integer, optional): Maximum depth of "why" questions (default: 5, range: 3-10)

**Example**:
```json
{
  "order": 2,
  "type": "FiveWhys",
  "title": "Root Cause Analysis",
  "prompt": "Why did this incident occur?",
  "config": {
    "maxDepth": 5
  }
}
```

**Participant Experience**:
- Starts with problem statement
- Answers "why" repeatedly
- Drills down to root cause
- Creates hierarchical cause chain

**Best Use Cases**:
- Incident post-mortems
- Problem-solving workshops
- Quality improvement
- Process failure analysis

---

### 8. GeneralFeedback

**Purpose**: Categorized open-ended feedback collection

**Type**: `"GeneralFeedback"`

**Config** (optional):
```json
"config": {
  "categories": ["Category 1", "Category 2", "Category 3"]
}
```

**Config Properties**:
- `categories` (array of strings, optional): Predefined categories for organizing feedback
- If omitted, feedback is uncategorized

**Example**:
```json
{
  "order": 4,
  "type": "GeneralFeedback",
  "title": "Questions & Expectations",
  "prompt": "What questions or expectations do you have?",
  "config": {
    "categories": [
      "Career Growth",
      "Team Culture",
      "Work-Life Balance",
      "Tools & Technology",
      "General Questions"
    ]
  }
}
```

**Without Categories**:
```json
{
  "order": 5,
  "type": "GeneralFeedback",
  "title": "Open Feedback",
  "prompt": "Any other thoughts or suggestions?"
}
```

**Participant Experience**:
- Submits text feedback
- Optionally selects category
- Can see categorized feedback from others

**Best Use Cases**:
- Open-ended questions
- Idea collection
- Suggestions and improvements
- Retrospective categories (What went well, What didn't, Action items)

---

## Complete Examples

### Example 1: Team Retrospective (Minimal)
```json
{
  "name": "Quick Retro",
  "description": "15-minute sprint retrospective",
  "category": "Retrospective",
  "iconEmoji": "🔄",
  "config": {
    "title": "Sprint Retrospective",
    "goal": "Reflect on the sprint and identify improvements",
    "settings": {
      "durationMinutes": 15,
      "allowAnonymous": true,
      "allowLateJoin": false,
      "showResultsDuringActivity": true
    },
    "joinFormSchema": {
      "fields": [
        {
          "name": "name",
          "label": "Your Name",
          "type": "text",
          "required": true
        }
      ]
    },
    "activities": [
      {
        "order": 1,
        "type": "WordCloud",
        "title": "Sprint in One Word",
        "prompt": "Describe this sprint in one word"
      },
      {
        "order": 2,
        "type": "GeneralFeedback",
        "title": "Retrospective Discussion",
        "prompt": "Share your thoughts",
        "config": {
          "categories": ["What Went Well", "What Didn't", "Action Items"]
        }
      }
    ]
  }
}
```

### Example 2: Customer Research (Comprehensive)
```json
{
  "name": "Customer Discovery",
  "description": "User research session with feature prioritization",
  "category": "ProductDiscovery",
  "iconEmoji": "👥",
  "config": {
    "title": "Customer Discovery Workshop",
    "goal": "Understand customer needs and prioritize features",
    "context": "Monthly customer advisory board meeting",
    "settings": {
      "durationMinutes": 120,
      "allowAnonymous": false,
      "allowLateJoin": true,
      "showResultsDuringActivity": true
    },
    "joinFormSchema": {
      "fields": [
        {
          "name": "name",
          "label": "Your Name",
          "type": "text",
          "required": true
        },
        {
          "name": "company",
          "label": "Company",
          "type": "text",
          "required": true
        },
        {
          "name": "userType",
          "label": "User Type",
          "type": "select",
          "required": true,
          "options": ["End User", "Administrator", "Developer", "Manager"]
        }
      ]
    },
    "activities": [
      {
        "order": 1,
        "type": "WordCloud",
        "title": "Product Perception",
        "prompt": "What word comes to mind when you think of our product?"
      },
      {
        "order": 2,
        "type": "GeneralFeedback",
        "title": "Pain Points",
        "prompt": "What challenges do you face?",
        "config": {
          "categories": [
            "Usability",
            "Performance",
            "Missing Features",
            "Integrations",
            "Support"
          ]
        }
      },
      {
        "order": 3,
        "type": "Poll",
        "title": "Top Feature Request",
        "prompt": "Which feature would add the most value?",
        "config": {
          "options": [
            "Advanced reporting",
            "Mobile app",
            "API enhancements",
            "Collaboration tools",
            "Automation workflows"
          ]
        }
      },
      {
        "order": 4,
        "type": "Quadrant",
        "title": "Feature Prioritization",
        "prompt": "Map features by value and urgency",
        "config": {
          "xAxisLabel": "Urgency",
          "yAxisLabel": "Business Value",
          "topLeftLabel": "High Value, Low Urgency (Roadmap)",
          "topRightLabel": "High Value, High Urgency (Do Now)",
          "bottomLeftLabel": "Low Value, Low Urgency (Backlog)",
          "bottomRightLabel": "Low Value, High Urgency (Quick Wins)"
        }
      },
      {
        "order": 5,
        "type": "Rating",
        "title": "Overall Satisfaction",
        "prompt": "How satisfied are you with our product?",
        "config": {
          "maxRating": 10,
          "ratingLabel": "Satisfaction Score"
        }
      }
    ]
  }
}
```

### Example 3: Training Assessment
```json
{
  "name": "Security Training",
  "description": "Cybersecurity awareness with knowledge check",
  "category": "Training",
  "iconEmoji": "🔒",
  "config": {
    "title": "Cybersecurity Awareness Training",
    "goal": "Educate team on security best practices",
    "settings": {
      "durationMinutes": 45,
      "allowAnonymous": false,
      "allowLateJoin": false,
      "showResultsDuringActivity": false
    },
    "joinFormSchema": {
      "fields": [
        {
          "name": "name",
          "label": "Your Name",
          "type": "text",
          "required": true
        },
        {
          "name": "department",
          "label": "Department",
          "type": "select",
          "required": true,
          "options": ["Engineering", "Sales", "Marketing", "Support", "HR"]
        }
      ]
    },
    "activities": [
      {
        "order": 1,
        "type": "WordCloud",
        "title": "Security Awareness",
        "prompt": "What security threats are you aware of?"
      },
      {
        "order": 2,
        "type": "Quiz",
        "title": "Knowledge Check",
        "prompt": "Test your security knowledge",
        "config": {
          "questions": [
            {
              "question": "What is phishing?",
              "options": [
                "A type of malware",
                "Fraudulent emails trying to steal information",
                "A network attack",
                "A password cracking technique"
              ],
              "correctAnswer": "Fraudulent emails trying to steal information"
            },
            {
              "question": "How often should you change passwords?",
              "options": [
                "Never",
                "Every week",
                "Every 90 days or when compromised",
                "Only when forced"
              ],
              "correctAnswer": "Every 90 days or when compromised"
            }
          ]
        }
      },
      {
        "order": 3,
        "type": "QnA",
        "title": "Questions",
        "prompt": "Ask any security-related questions"
      },
      {
        "order": 4,
        "type": "Rating",
        "title": "Training Effectiveness",
        "prompt": "How useful was this training?",
        "config": {
          "maxRating": 5,
          "ratingLabel": "Usefulness"
        }
      }
    ]
  }
}
```

---

## Best Practices

### Template Design

1. **Activity Sequence**
   - Start with icebreakers (WordCloud, simple Poll)
   - Build to core content (Feedback, Quadrants)
   - End with action items or ratings
   - Typical flow: 3-6 activities per session

2. **Timing**
   - Allocate 5-10 minutes per activity for planning
   - Quick activities: WordCloud, Poll (5 min)
   - Medium activities: Feedback, Rating (10 min)
   - Complex activities: Quadrant, FiveWhys (15-20 min)

3. **Anonymity**
   - Use `allowAnonymous: true` for sensitive feedback
   - Use `allowAnonymous: false` when accountability matters
   - Consider anonymous polls for voting, named feedback for discussions

4. **Categories in GeneralFeedback**
   - 3-6 categories optimal
   - Keep labels short and clear
   - Examples:
     - Retro: "What Went Well", "What Didn't", "Action Items"
     - Feedback: "Features", "Bugs", "Documentation", "Support"
     - Ideas: "Process", "Product", "People", "Technology"

5. **Quadrant Labels**
   - Make axis labels concise (1-3 words)
   - Make quadrant descriptions actionable
   - Include guidance in parentheses
   - Example: "High Impact, Low Effort (Quick Wins)"

6. **Join Form Fields**
   - Minimum: Name field only
   - Typical: Name + Role/Department
   - Advanced: Name + 2-3 categorization fields
   - Avoid: More than 5 fields (creates friction)

### Validation Checklist

Before deploying a template:

- [ ] All required fields present
- [ ] Category matches use case
- [ ] Duration realistic for activity count
- [ ] Activities have sequential `order` values
- [ ] Poll has 2+ options
- [ ] Quadrant has all 6 labels
- [ ] FiveWhys maxDepth between 3-10
- [ ] Rating maxRating specified
- [ ] Join form has at least name field
- [ ] Select fields have options array
- [ ] No typos in emoji or descriptions
- [ ] JSON is valid (no syntax errors)

### Common Patterns

**Classic Retrospective**
1. WordCloud: Sprint sentiment
2. GeneralFeedback: What went well / What didn't / Actions
3. Poll: Vote on action items
4. Rating: Sprint satisfaction

**Strategic Workshop**
1. WordCloud: Vision themes
2. GeneralFeedback: Idea collection
3. Poll: Initial voting
4. Quadrant: Prioritization matrix
5. GeneralFeedback: Action planning

**Incident Review**
1. QnA: Timeline of events
2. FiveWhys: Root cause
3. Feedback: Contributing factors
4. Quadrant: Remediation priority
5. Feedback: Action items

**Customer Research**
1. WordCloud: Product perception
2. Feedback: Pain points
3. Poll: Feature voting
4. Quadrant: Priority mapping
5. Rating: Satisfaction

---

## Additional Notes

### File Naming
- Use lowercase kebab-case: `security-training.json`
- Be descriptive: `customer-discovery-workshop.json` not `cust-disc.json`
- Avoid spaces and special characters

### Version Control
- Store templates in source control
- Templates are automatically moved to `installed/` after loading
- To update: Modify file in Templates folder, restart application
- Old version in `installed/` will be replaced

### Testing
- Create session from template via UI
- Verify all activities load correctly
- Test participant join flow
- Check activity prompts and configurations
- Validate real-time updates

### Troubleshooting

**Template not loading?**
- Check JSON syntax (use validator)
- Verify all required fields present
- Check logs in `App_Data/logs/`

**Activities in wrong order?**
- Ensure `order` values are unique and sequential
- Order determines display sequence

**Categories not showing?**
- Verify `categories` array in `config`
- Check for typos in category names

---

## Support & Resources

- **Template Examples**: `/App_Data/Templates/installed/`
- **Use Cases**: `/docs/use-cases.md`
- **Application Logs**: `/App_Data/logs/`
- **Database Schema**: See SessionTemplates table

For questions or issues, refer to the main documentation or create a GitHub issue.
