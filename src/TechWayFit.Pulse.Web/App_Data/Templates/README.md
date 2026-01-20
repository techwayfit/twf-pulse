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
        "config": {
          // Activity-specific configuration
        }
      }
    ]
  }
}
```

## Supported Activity Types

- **WordCloud**: Word cloud visualization
- **Poll**: Single or multiple choice poll
- **Quiz**: Quiz with correct answers
- **QnA**: Question and answer
- **Rating**: Rating scale (e.g., 1-5 stars)
- **Quadrant**: 2x2 matrix/quadrant
- **FiveWhys**: Root cause analysis (5 Whys technique)
- **GeneralFeedback**: Open feedback with categories

## Activity-Specific Configurations

### Poll
```json
{
  "config": {
    "options": ["Option 1", "Option 2", "Option 3"]
  }
}
```

### Quadrant
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

### FiveWhys
```json
{
  "config": {
    "maxDepth": 5
  }
}
```

### GeneralFeedback
```json
{
  "config": {
    "categories": ["Category 1", "Category 2", "Category 3"]
  }
}
```

### Rating
```json
{
  "config": {
    "maxRating": 5,
    "ratingLabel": "Quality"
  }
}
```

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
