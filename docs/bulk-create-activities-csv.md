# Bulk Create Activities via CSV

## Overview

The bulk create feature allows facilitators to upload multiple activities at once using a CSV file. This is particularly useful when:
- Creating sessions with many activities
- Importing activities from existing workshop templates
- Quickly duplicating session structures

## Location

The bulk upload feature is available on the **Add Activities** page under the **Manual** tab, right above the activity list.

## How to Use

### 1. Download the CSV Template

Click the **"Download CSV Template"** button to get a pre-formatted CSV file with:
- Correct column headers
- Example activities showing proper formatting
- Sample JSON configurations for different activity types

### 2. Edit the CSV File

Open the downloaded template in Excel, Google Sheets, or any text editor and fill in your activities:

#### Required Columns:

| Column | Description | Example |
|--------|-------------|---------|
| **Order** | Numeric order (1, 2, 3...) | `1` |
| **Type** | Activity type name | `Poll`, `WordCloud`, `Rating`, `Quadrant`, `FiveWhys`, `GeneralFeedback`, `QnA`, `AiSummary`, `Break` |
| **Title** | Activity title (max 200 chars) | `How satisfied are you?` |
| **Prompt** | Activity description/question (max 1000 chars) | `Rate your satisfaction with today's session` |
| **ConfigJson** | JSON configuration specific to activity type | `{"options":["Very Satisfied","Satisfied","Neutral","Dissatisfied"],"allowMultiple":false}` |
| **DurationInMin** | Expected duration in minutes | `5` |

### 3. Activity Type Reference

| Type | Enum Value | Common Config Fields |
|------|------------|---------------------|
| `Poll` | 0 | `{"options":[{"id":"option_0","label":"Option 1"}],"allowMultiple":false}` |
| `Quiz` | 1 | `{"questions":[...]}` |
| `WordCloud` | 2 | `{"maxWords":3}` |
| `QnA` | 3 | `{}` |
| `Rating` | 4 | `{"scale":5}` |
| `Quadrant` | 5 | `{"xAxisLabel":"Effort","yAxisLabel":"Impact","topLeft":"Label","topRight":"Label","bottomLeft":"Label","bottomRight":"Label"}` |
| `FiveWhys` | 6 | `{"maxDepth":5}` |
| `GeneralFeedback` | 7 | `{}` |
| `AiSummary` | 8 | `{}` |
| `Break` | 9 | `{}` |

### 4. Configuration JSON Examples

#### Poll Activity
```json
{
  "options": ["Strongly Agree", "Agree", "Neutral", "Disagree", "Strongly Disagree"],
  "allowMultiple": false
}
```

#### Word Cloud Activity
```json
{
  "maxWords": 3
}
```

#### Rating Activity
```json
{
  "scale": 5
}
```

#### Quadrant Activity
```json
{
  "xAxisLabel": "Effort",
  "yAxisLabel": "Impact",
  "topLeft": "High Impact, Low Effort",
  "topRight": "High Impact, High Effort",
  "bottomLeft": "Low Impact, Low Effort",
  "bottomRight": "Low Impact, High Effort"
}
```

#### 5 Whys Activity
```json
{
  "maxDepth": 5
}
```

#### Simple Activities (No Config)
For activities like Break, QnA, GeneralFeedback, and AiSummary, you can use an empty JSON object:
```json
{}
```

### 5. Upload the CSV

1. Click **"Choose File"** and select your edited CSV
2. Click **"Upload"** button
3. Wait for the upload to complete
4. Review the success message (shows count of created activities)
5. If there are errors, they will be listed with row numbers

### 6. Verify Created Activities

After successful upload:
- Activities appear in the activity list below
- They are automatically ordered based on the Order column
- You can edit, reorder, or delete them individually if needed

## Validation Rules

The system validates each row and will report errors:
- **Order**: Must be a positive number
- **Type**: Must match one of the valid activity type names (case-sensitive)
- **Title**: Required, max 200 characters
- **Prompt**: Optional, max 1000 characters
- **ConfigJson**: Must be valid JSON
- **DurationInMin**: Optional, positive number (defaults to 5)

## Limits

- Maximum **100 activities** per CSV upload
- Maximum **100 activities total** per session (system recommendation)

## Tips

1. **Start Small**: Test with 2-3 activities first to ensure formatting is correct
2. **Copy Examples**: Use the downloaded template examples as a starting point
3. **JSON Validation**: Validate your JSON config using online tools like jsonlint.com
4. **Excel Escaping**: When using Excel, ensure cells with commas or quotes are properly formatted as text
5. **Backup**: Download your existing activities before bulk uploading if needed

## Troubleshooting

### "Invalid activity type" Error
- Ensure type names match exactly (case-sensitive): `Poll`, `WordCloud`, `Rating`, etc.
- Don't use enum numbers, use the type name

### "ConfigJson must be valid JSON" Error
- Check for missing commas, quotes, or brackets
- Use a JSON validator to check syntax
- Ensure strings are in double quotes, not single quotes

### "Row X: Title is required" Error
- Every activity must have a title
- Check for empty cells in the Title column

### Upload Button Disabled
- Make sure you've selected a file
- Verify the file has a `.csv` extension

## API Endpoint

For programmatic access, the bulk create API is available at:

```
POST /api/sessions/{sessionCode}/activities/bulk
```

**Request Body:**
```json
{
  "activities": [
    {
      "order": 1,
      "type": 1,
      "title": "Activity Title",
      "prompt": "Activity prompt",
    "config": "{}",
      "durationMinutes": 5
    }
  ]
}
```

**Response:**
```json
{
  "data": {
 "successCount": 5,
    "createdActivityIds": ["guid1", "guid2", ...],
    "errors": null
  }
}
```

## Related Documentation

- [How to Add a New Activity Type](./how-to-add-new-activity-type.md)
- [Getting Started Guide](../Views/Home/GettingStarted.cshtml)
