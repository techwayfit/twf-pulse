# Session Templates

This directory contains system template definitions in JSON format. On application startup, these templates are automatically loaded into the database and then moved to the `installed/` subdirectory.

## How It Works

1. **Add Template**: Place a new `.json` file in this directory
2. **Start Application**: Templates are automatically processed on startup
3. **Auto-Install**: Successfully processed templates are moved to `installed/` folder
4. **Database Storage**: Template data is stored in the `SessionTemplates` table

## Template File Format

Each JSON file should follow this structure:

```json
{
  "name": "Template Name",
  "description": "Short description of the template",
  "category": "Retrospective|ProductDiscovery|IncidentReview|TeamBuilding|Training",
  "iconEmoji": "🔄",
  "config": {
    "title": "Default session title",
    "goal": "Session goal/objective",
    "context": "Additional context for the session",
    "settings": {
      "durationMinutes": 60,
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
        }
      ]
    },
    "activities": [
      {
        "order": 1,
        "type": "WordCloud|Poll|Quiz|QnA|Rating|Quadrant|FiveWhys|GeneralFeedback",
        "title": "Activity Title",
        "prompt": "Activity prompt/question",
        "durationMinutes": 5,
        "config": {
          // Activity-specific configuration
        }
      }
    ]
  }
}
```

## Supported Activity Types

All 8 activity types are fully supported:

- **Poll**: Single or multiple choice voting with full configuration
- **Quiz**: Knowledge assessment with correct answers
- **WordCloud**: Word cloud visualization with text constraints
- **QnA**: Question and answer forum with upvoting
- **Rating**: Rating scale (e.g., 1-5 stars) with comments
- **Quadrant**: 2x2 matrix/quadrant for prioritization
- **FiveWhys**: Root cause analysis (5 Whys technique)
- **GeneralFeedback**: Open feedback with categories

---

## Activity-Specific Configurations

### Poll
**Purpose**: Vote on predefined options (single or multiple choice)

**Full Configuration**:
```json
{
  "config": {
    "options": [
      {
        "id": "option-1",
        "label": "Option 1",
        "description": "Optional description for Option 1"
      },
      {
        "id": "option-2",
        "label": "Option 2",
        "description": null
      }
    ],
    "allowMultiple": false,
    "minSelections": 1,
    "maxSelections": 1,
    "allowCustomOption": false,
    "customOptionPlaceholder": "Other (please specify)",
    "randomizeOrder": false,
    "showResultsAfterSubmit": false,
    "maxResponsesPerParticipant": 1
  }
}
```

**Properties**:
- `options` (array, required): List of poll options with id, label, and optional description
- `allowMultiple` (boolean, optional): Allow multiple selections (default: false)
- `minSelections` (integer, optional): Minimum selections required (default: 1)
- `maxSelections` (integer, optional): Maximum selections allowed (default: 1)
- `allowCustomOption` (boolean, optional): Allow "Other" write-in option (default: false)
- `customOptionPlaceholder` (string, optional): Placeholder text for custom option
- `randomizeOrder` (boolean, optional): Randomize option order (default: false)
- `showResultsAfterSubmit` (boolean, optional): Show results after voting (default: false)
- `maxResponsesPerParticipant` (integer, optional): Limit responses per participant (default: 1)

---

### Quiz
**Purpose**: Knowledge assessment with correct answers

**Full Configuration**:
```json
{
  "config": {
    "questions": [
      {
        "id": "q1",
        "text": "What is the correct answer?",
        "options": [
          {"id": "a1", "label": "Answer 1", "description": null},
          {"id": "a2", "label": "Answer 2", "description": null},
          {"id": "a3", "label": "Answer 3", "description": null}
        ],
        "correctAnswerId": "a2",
        "explanation": "This is why answer 2 is correct."
      }
    ],
    "showCorrectAnswers": true,
    "showExplanations": true,
    "allowRetry": false,
    "randomizeQuestions": false,
    "randomizeOptions": false,
    "passingScore": 70,
    "maxResponsesPerParticipant": 1
  }
}
```

**Properties**:
- `questions` (array, required): List of quiz questions
  - `id` (string, required): Unique question identifier
  - `text` (string, required): Question text
  - `options` (array, required): Answer choices with id, label, description
  - `correctAnswerId` (string, required): ID of the correct answer
  - `explanation` (string, optional): Explanation shown after answering
- `showCorrectAnswers` (boolean, optional): Show correct/incorrect feedback (default: true)
- `showExplanations` (boolean, optional): Display explanations (default: true)
- `allowRetry` (boolean, optional): Allow retrying incorrect answers (default: false)
- `randomizeQuestions` (boolean, optional): Randomize question order (default: false)
- `randomizeOptions` (boolean, optional): Randomize answer order (default: false)
- `passingScore` (integer, optional): Percentage required to pass (default: 70)
- `maxResponsesPerParticipant` (integer, optional): Limit quiz attempts (default: 1)

---

### WordCloud
**Purpose**: Collect single words or short phrases to visualize common themes

**Full Configuration**:
```json
{
  "config": {
    "maxWords": 3,
    "minWordLength": 3,
    "maxWordLength": 50,
    "placeholder": "Enter a word or short phrase",
    "allowMultipleSubmissions": false,
    "maxSubmissionsPerParticipant": 1,
    "stopWords": ["the", "and", "is", "a", "an"],
    "caseSensitive": false
  }
}
```

**Properties**:
- `maxWords` (integer, optional): Maximum words per submission (default: 3)
- `minWordLength` (integer, optional): Minimum characters per word (default: 3)
- `maxWordLength` (integer, optional): Maximum characters per word (default: 50)
- `placeholder` (string, optional): Input placeholder text
- `allowMultipleSubmissions` (boolean, optional): Allow multiple submissions (default: false)
- `maxSubmissionsPerParticipant` (integer, optional): Max submissions if allowed (default: 1)
- `stopWords` (array, optional): Words to exclude from cloud
- `caseSensitive` (boolean, optional): Treat "Word" and "word" as different (default: false)

---

### QnA (Questions & Answers)
**Purpose**: Collect questions from participants with voting/upvoting

**Full Configuration**:
```json
{
  "config": {
    "allowUpvoting": true,
    "allowDownvoting": false,
    "allowAnonymous": true,
    "moderationEnabled": false,
    "maxQuestionLength": 500,
    "maxQuestionsPerParticipant": 3,
    "showQuestionCount": true,
    "sortBy": "upvotes",
    "placeholder": "Ask your question here..."
  }
}
```

**Properties**:
- `allowUpvoting` (boolean, optional): Participants can upvote questions (default: true)
- `allowDownvoting` (boolean, optional): Participants can downvote (default: false)
- `allowAnonymous` (boolean, optional): Hide participant identity (default: true)
- `moderationEnabled` (boolean, optional): Require facilitator approval (default: false)
- `maxQuestionLength` (integer, optional): Character limit (default: 500)
- `maxQuestionsPerParticipant` (integer, optional): Limit submissions (default: 3)
- `showQuestionCount` (boolean, optional): Display total count (default: true)
- `sortBy` (string, optional): Default sorting: "upvotes", "chronological", "random" (default: "upvotes")
- `placeholder` (string, optional): Input placeholder text

---

### Rating
**Purpose**: Numerical rating or scoring with optional comments

**Full Configuration**:
```json
{
  "config": {
    "scale": 5,
    "minLabel": "1 - Poor",
    "maxLabel": "5 - Excellent",
    "midpointLabel": "3 - Average",
    "allowComments": true,
    "commentRequired": false,
    "commentPlaceholder": "Tell us more (optional)",
    "displayType": "Stars",
    "showAverageAfterSubmit": false,
    "maxResponsesPerParticipant": 1
  }
}
```

**Properties**:
- `scale` (integer, required): Maximum rating value (typically 5 or 10)
- `minLabel` (string, optional): Label for lowest value (e.g., "1 - Poor")
- `maxLabel` (string, required): Label for highest value (e.g., "5 - Excellent")
- `midpointLabel` (string, optional): Label for middle value (e.g., "3 - Average")
- `allowComments` (boolean, optional): Show comment field (default: true)
- `commentRequired` (boolean, optional): Force participants to add comment (default: false)
- `commentPlaceholder` (string, optional): Comment field placeholder
- `displayType` (string, optional): UI type: "Stars", "Slider", "Buttons" (default: "Stars")
- `showAverageAfterSubmit` (boolean, optional): Show current average (default: false)
- `maxResponsesPerParticipant` (integer, optional): Limit ratings per participant (default: 1)

---

### Quadrant
**Purpose**: 2D mapping of items across two dimensions (e.g., Impact vs. Effort)

**Full Configuration**:
```json
{
  "config": {
    "xAxisLabel": "Horizontal Axis",
    "yAxisLabel": "Vertical Axis",
    "topLeftLabel": "Top Left Quadrant",
    "topRightLabel": "Top Right Quadrant",
    "bottomLeftLabel": "Bottom Left Quadrant",
    "bottomRightLabel": "Bottom Right Quadrant"
  }
}
```

**Properties** (all required):
- `xAxisLabel` (string): Label for horizontal axis (e.g., "Effort", "Complexity")
- `yAxisLabel` (string): Label for vertical axis (e.g., "Impact", "Value")
- `topLeftLabel` (string): Description for top-left quadrant
- `topRightLabel` (string): Description for top-right quadrant
- `bottomLeftLabel` (string): Description for bottom-left quadrant
- `bottomRightLabel` (string): Description for bottom-right quadrant

**Common Patterns**:
- Impact/Effort Matrix
- Urgency/Importance (Eisenhower Matrix)
- Risk Matrix (Likelihood/Impact)
- Skills Matrix (Confidence/Experience)

---

### FiveWhys
**Purpose**: Root cause analysis using the "5 Whys" technique

**Full Configuration**:
```json
{
  "config": {
    "maxDepth": 5,
    "minDepth": 3,
    "initialProblem": "Why did this incident occur?",
    "context": "Background information for the analysis",
    "allowEarlySubmit": true,
    "showProgressIndicator": true,
    "minAnswerLength": 10,
    "maxAnswerLength": 200
  }
}
```

**Properties**:
- `maxDepth` (integer, optional): Maximum "why" iterations (default: 5, range: 3-10)
- `minDepth` (integer, optional): Minimum iterations before allowing submit (default: 3)
- `initialProblem` (string, optional): Starting problem statement
- `context` (string, optional): Background information
- `allowEarlySubmit` (boolean, optional): Allow stopping before maxDepth (default: true)
- `showProgressIndicator` (boolean, optional): Display progress bar (default: true)
- `minAnswerLength` (integer, optional): Minimum characters per answer (default: 10)
- `maxAnswerLength` (integer, optional): Maximum characters per answer (default: 200)

---

### GeneralFeedback
**Purpose**: Categorized open-ended feedback collection

**Full Configuration**:
```json
{
  "config": {
    "categoriesEnabled": true,
    "categories": [
      {
        "id": "category-1",
        "label": "Category 1",
        "icon": "📝"
      },
      {
        "id": "category-2",
        "label": "Category 2",
        "icon": "💡"
      }
    ],
    "requireCategory": false,
    "showCharacterCount": true,
    "maxLength": 1000,
    "minLength": 10,
    "placeholder": "Share your feedback...",
    "allowAnonymous": false,
    "maxResponsesPerParticipant": 5
  }
}
```

**Properties**:
- `categoriesEnabled` (boolean, optional): Enable category selection (default: false)
- `categories` (array, optional): Predefined categories with id, label, and icon
- `requireCategory` (boolean, optional): Force category selection (default: false)
- `showCharacterCount` (boolean, optional): Display character counter (default: true)
- `maxLength` (integer, optional): Maximum characters (default: 1000)
- `minLength` (integer, optional): Minimum characters (default: 10)
- `placeholder` (string, optional): Input placeholder text
- `allowAnonymous` (boolean, optional): Allow anonymous feedback (default: false)
- `maxResponsesPerParticipant` (integer, optional): Limit submissions (default: 5)

**Common Category Patterns**:
- Retrospective: "What Went Well", "What Didn't", "Action Items"
- Feedback: "Features", "Bugs", "Documentation", "Support"
- Ideas: "Process", "Product", "People", "Technology"

---

## Updating Existing Templates

To update an existing system template:
1. Modify the JSON file in the `installed/` folder
2. Copy it back to this directory
3. Restart the application

The system will detect the template by name and update it in the database.

## CI/CD Integration

For automated deployments:
1. Store template JSON files in source control
2. Include them in the build/publish process
3. Ensure they're copied to `App_Data/Templates` in the deployment package
4. Application will auto-process them on first startup after deployment

## Airtight Design

- Templates are part of the application package
- No external configuration files needed
- All templates are self-contained within the deployment
- Version control friendly (JSON files in source control)
- Automatic installation and versioning

## Example Files

See the existing template files for examples:
- `retro-sprint-review.json` - Sprint retrospective
- `ops-pain-points.json` - Operations workshop
- `product-discovery.json` - Product ideation
- `incident-review.json` - Incident post-mortem
- `team-building.json` - Team building activities
- `strategic-planning.json` - Strategic planning workshop
- `customer-feedback.json` - Customer discovery session
- `university-lecture.json` - Educational workshop

---

## Validation & Best Practices

### Template Validation Checklist
- [ ] All required fields present
- [ ] Category matches use case
- [ ] Duration realistic for activity count
- [ ] Activities have sequential `order` values
- [ ] Poll has 2+ options with proper id/label structure
- [ ] Quadrant has all 6 required labels
- [ ] FiveWhys maxDepth between 3-10
- [ ] Rating scale specified (typically 5 or 10)
- [ ] Join form has at least name field
- [ ] Select fields have options array
- [ ] No typos in emoji or descriptions
- [ ] JSON is valid (no syntax errors)

### Activity Design Best Practices
1. **Sequencing**: Start with icebreakers, build to core content, end with actions
2. **Timing**: Allocate 5-10 minutes per activity
3. **Anonymity**: Use for sensitive feedback, disable for accountability
4. **Categories**: Keep to 3-6 categories, use clear labels
5. **Limits**: Set reasonable maxResponsesPerParticipant values
