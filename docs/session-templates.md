# Session Template Framework

## Overview

The TechWayFit Pulse Session Template Framework allows facilitators to create full workshop sessions from pre-defined or custom templates. Templates include session settings, join form schemas, and multiple activities configured in advance.

## Features

- **Pre-defined System Templates**: 4 built-in templates for common workshop scenarios:
  - **Retro Sprint Review** (🔄): Quick pulse + themes + actions
  - **Ops Pain Points** (⚙️): Impact/Effort + 5-Whys
  - **Product Discovery** (💡): Idea cloud + prioritization
  - **Incident Review** (🚨): Root cause ladder + fixes

- **Custom Templates**: Create and save your own templates
- **Customizable**: Override template settings when creating a session
- **Full Activity Support**: Templates include all activity types (Polls, Word Clouds, Quadrants, 5-Whys, etc.)

## Architecture

### Components

1. **Domain Layer**
   - `SessionTemplate` entity: Stores template metadata and configuration
   - `SessionTemplateConfig` model: Defines template structure (JSON serializable)
   - `TemplateCategory` enum: Categorizes templates

2. **Application Layer**
   - `ISessionTemplateService`: Business logic for template management
   - `ISessionTemplateRepository`: Data access interface

3. **Infrastructure Layer**
   - `SessionTemplateService`: Service implementation
   - `SessionTemplateRepository`: SQLite repository implementation

4. **Web/API Layer**
   - `SessionTemplatesController`: RESTful API endpoints
   - Template DTOs for API contracts

### Database Schema

```sql
CREATE TABLE SessionTemplates (
    Id GUID PRIMARY KEY,
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(500) NOT NULL,
    Category INT NOT NULL,
    IconEmoji NVARCHAR(10) NOT NULL,
    ConfigJson TEXT NOT NULL,
    IsSystemTemplate BIT NOT NULL,
    CreatedByUserId GUID NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,
    UpdatedAt DATETIMEOFFSET NOT NULL
);
```

## API Endpoints

### GET /api/templates
Get all available templates (system + user-created)

**Query Parameters:**
- `category` (optional): Filter by category (Retrospective, ProductDiscovery, IncidentReview, etc.)

**Response:**
```json
{
  "templates": [
    {
      "id": "guid",
      "name": "Retro Sprint Review",
      "description": "Quick pulse + themes + actions",
      "category": "Retrospective",
      "iconEmoji": "🔄",
      "isSystemTemplate": true,
      "createdAt": "2026-01-20T00:00:00Z",
      "updatedAt": "2026-01-20T00:00:00Z"
    }
  ]
}
```

### GET /api/templates/{id}
Get template details with full configuration

**Response:**
```json
{
  "template": {
    "id": "guid",
    "name": "Retro Sprint Review",
    "description": "Quick pulse + themes + actions",
    "category": "Retrospective",
    "iconEmoji": "🔄",
    "isSystemTemplate": true,
    "config": {
      "title": "Sprint Retrospective",
      "goal": "Reflect on the sprint and identify improvements",
      "settings": {
        "maxContributionsPerParticipantPerSession": 100,
        "allowAnonymous": false,
        "ttlMinutes": 60
      },
      "joinFormSchema": {
        "maxFields": 10,
        "fields": [
          {
            "id": "name",
            "label": "Your Name",
            "type": "Text",
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
          "type": "Poll",
          "title": "Sprint Satisfaction",
          "prompt": "How satisfied are you with this sprint?",
          "config": {
            "options": ["😞 Very Unsatisfied", "😐 Unsatisfied", "🙂 Neutral", "😊 Satisfied", "🎉 Very Satisfied"]
          }
        }
      ]
    }
  }
}
```

### POST /api/templates
Create a custom template (Requires Authentication)

**Request:**
```json
{
  "name": "My Custom Workshop",
  "description": "A custom workshop template",
  "category": "Custom",
  "iconEmoji": "🎨",
  "config": {
    "title": "Custom Workshop",
    "goal": "Learn something new",
    "settings": {
      "maxContributionsPerParticipantPerSession": 50,
      "allowAnonymous": true,
      "ttlMinutes": 90
    },
    "joinFormSchema": {
      "maxFields": 10,
      "fields": []
    },
    "activities": [
      {
        "order": 1,
        "type": "Poll",
        "title": "Welcome Poll",
        "prompt": "How are you feeling today?"
      }
    ]
  }
}
```

**Response:**
```json
{
  "templateId": "guid",
  "message": "Template created successfully"
}
```

### PUT /api/templates/{id}
Update a custom template (Requires Authentication, Own templates only)

**Request:** Same as POST

### DELETE /api/templates/{id}
Delete a custom template (Requires Authentication, Own templates only)

### POST /api/templates/create-session
Create a session from a template

**Request:**
```json
{
  "templateId": "guid",
  "groupId": "guid" (optional),
  "customizations": {
    "title": "Override title" (optional),
    "goal": "Override goal" (optional),
    "context": "Override context" (optional),
    "settings": { ... } (optional),
    "joinFormSchema": { ... } (optional)
  }
}
```

**Response:**
```json
{
  "sessionId": "guid",
  "code": "ABC-DEF-GHI"
}
```

## Template Configuration Structure

### SessionTemplateConfig

```typescript
{
  "title": string,              // Session title
  "goal": string?,              // Session goal/objective
  "context": string?,           // Additional context
  "settings": {
    "durationMinutes": number?, // Expected duration
    "maxParticipants": number?, // Max participants
    "allowAnonymous": boolean,  // Allow anonymous participation
    "allowLateJoin": boolean,   // Allow joining after start
    "showResultsDuringActivity": boolean
  },
  "joinFormSchema": {
    "fields": [
      {
        "name": string,         // Field identifier
        "label": string,        // Display label
        "type": string,         // "text", "number", "select", "multiselect", "boolean"
        "required": boolean,
        "options": string[]?    // For select/multiselect types
      }
    ]
  },
  "activities": [
    {
      "order": number,          // Display order (1-based)
      "type": "Poll" | "Quiz" | "WordCloud" | "QnA" | "Rating" | "Quadrant" | "FiveWhys" | "GeneralFeedback",
      "title": string,          // Activity title
      "prompt": string?,        // Instructions/prompt
      "config": {               // Activity-specific configuration
        // Poll/Quiz
        "options": string[]?,
        "multipleChoice": boolean?,
        "correctOptionIndex": number?,
        
        // Rating
        "maxRating": number?,
        "ratingLabel": string?,
        
        // Quadrant
        "xAxisLabel": string?,
        "yAxisLabel": string?,
        "topLeftLabel": string?,
        "topRightLabel": string?,
        "bottomLeftLabel": string?,
        "bottomRightLabel": string?,
        
        // Five Whys
        "maxDepth": number?,
        
        // Word Cloud
        "maxWords": number?,
        "minWordLength": number?,
        
        // General Feedback
        "categories": string[]?
      }
    }
  ]
}
```

## Usage Examples

### 1. List All Templates

```javascript
fetch('/api/templates')
  .then(res => res.json())
  .then(data => console.log(data.templates));
```

### 2. Get Template Details

```javascript
fetch('/api/templates/template-id-here')
  .then(res => res.json())
  .then(data => console.log(data.template.config));
```

### 3. Create Session from Template

```javascript
fetch('/api/templates/create-session', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer YOUR_TOKEN'
  },
  body: JSON.stringify({
    templateId: 'template-id-here',
    groupId: 'optional-group-id',
    customizations: {
      title: 'My Custom Sprint Retro'
    }
  })
})
  .then(res => res.json())
  .then(data => {
    console.log('Session created:', data.code);
    window.location.href = `/facilitator/live?code=${data.code}`;
  });
```

### 4. Create Custom Template

```javascript
fetch('/api/templates', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer YOUR_TOKEN'
  },
  body: JSON.stringify({
    name: 'Team Building Workshop',
    description: 'Fun activities for team bonding',
    category: 'TeamBuilding',
    iconEmoji: '🎉',
    config: {
      title: 'Team Building Session',
      goal: 'Get to know each other better',
      settings: {
        durationMinutes: 60,
        allowAnonymous: false,
        allowLateJoin: true,
        showResultsDuringActivity: true
      },
      joinFormSchema: {
        fields: [
          {
            name: 'name',
            label: 'Your Name',
            type: 'text',
            required: true
          },
          {
            name: 'hobby',
            label: 'Favorite Hobby',
            type: 'text',
            required: false
          }
        ]
      },
      activities: [
        {
          order: 1,
          type: 'WordCloud',
          title: 'One Word About Our Team',
          prompt: 'Describe our team in one word'
        },
        {
          order: 2,
          type: 'GeneralFeedback',
          title: 'Team Strengths',
          prompt: 'What are our team strengths?',
          config: {
            categories: ['Communication', 'Collaboration', 'Innovation', 'Delivery']
          }
        }
      ]
    }
  })
})
  .then(res => res.json())
  .then(data => console.log('Template created:', data.templateId));
```

## System Templates

### 1. Retro Sprint Review (🔄)
**Category:** Retrospective  
**Activities:**
1. Word Cloud: "Sprint in One Word"
2. Poll: "Sprint Satisfaction"
3. Quadrant: "What Went Well / What Didn't"
4. General Feedback: "Action Items"

### 2. Ops Pain Points (⚙️)
**Category:** IncidentReview  
**Activities:**
1. General Feedback: "Pain Point Collection"
2. Quadrant: "Impact vs Effort Matrix"
3. Five Whys: "Root Cause Analysis"
4. General Feedback: "Solutions & Next Steps"

### 3. Product Discovery (💡)
**Category:** ProductDiscovery  
**Activities:**
1. Word Cloud: "Customer Needs"
2. General Feedback: "Feature Ideas"
3. Quadrant: "Value vs Complexity"
4. Poll: "Top Priority Vote"
5. General Feedback: "Next Steps"

### 4. Incident Review (🚨)
**Category:** IncidentReview  
**Activities:**
1. Q&A: "Incident Timeline"
2. Five Whys: "Root Cause Analysis"
3. General Feedback: "Contributing Factors"
4. Quadrant: "Remediation Prioritization"
5. General Feedback: "Action Items"
6. Rating: "Incident Response Rating"

## Implementation Details

### Initialization

System templates are automatically initialized on application startup in `Program.cs`:

```csharp
using (var scope = app.Services.CreateScope())
{
    var templateService = scope.ServiceProvider.GetRequiredService<ISessionTemplateService>();
    await templateService.InitializeSystemTemplatesAsync();
}
```

### Service Registration

Services are registered in the DI container:

```csharp
builder.Services.AddScoped<ISessionTemplateRepository, SessionTemplateRepository>();
builder.Services.AddScoped<ISessionTemplateService, SessionTemplateService>();
```

### Creating a Session from Template

The service handles:
1. Loading template configuration
2. Applying customizations
3. Generating session code
4. Creating session with proper settings
5. Creating all activities in order

```csharp
var session = await templateService.CreateSessionFromTemplateAsync(
    templateId,
    facilitatorUserId,
    groupId,
    customizations,
    cancellationToken);
```

## Future Enhancements

1. **Template Sharing**: Allow facilitators to share templates with each other
2. **Template Marketplace**: Public repository of community templates
3. **Template Versioning**: Track template changes over time
4. **Template Import/Export**: JSON/YAML file import/export
5. **Template Analytics**: Track which templates are most popular
6. **Template Tags**: Better organization and discovery
7. **Template Cloning**: Duplicate and modify existing templates
8. **Activity Library**: Reusable activity configurations

## Testing

Use the API endpoints with tools like:
- **Postman**: Import collection for testing
- **cURL**: Command-line testing
- **Browser DevTools**: Frontend integration testing

Example cURL:
```bash
# List templates
curl http://localhost:5000/api/templates

# Get template details
curl http://localhost:5000/api/templates/{id}

# Create session from template (requires auth)
curl -X POST http://localhost:5000/api/templates/create-session \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"templateId":"template-id","customizations":{"title":"My Workshop"}}'
```

## Troubleshooting

### Templates not showing up
- Check if system templates were initialized on startup
- Check database has SessionTemplates table
- Verify API endpoint is accessible

### Cannot create session from template
- Ensure user is authenticated
- Verify template ID exists
- Check activity configurations are valid

### Custom template validation errors
- Ensure all required fields are provided
- Check activity types are valid
- Verify join form field types are supported

## Related Files

- **Domain**: `src/TechWayFit.Pulse.Domain/Entities/SessionTemplate.cs`
- **Models**: `src/TechWayFit.Pulse.Domain/Models/SessionTemplateConfig.cs`
- **Service**: `src/TechWayFit.Pulse.Infrastructure/Services/SessionTemplateService.cs`
- **Repository**: `src/TechWayFit.Pulse.Infrastructure/Repositories/SessionTemplateRepository.cs`
- **API**: `src/TechWayFit.Pulse.Web/Controllers/Api/SessionTemplatesController.cs`
- **Migration**: `src/TechWayFit.Pulse.Web/Migrations/*_AddSessionTemplates.cs`
