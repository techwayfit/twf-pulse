/**
 * CSV Bulk Upload Handler for Activities
 */

(function() {
    'use strict';

    // Activity type mapping (enum values) - MUST match backend ActivityType enum
    const ACTIVITY_TYPES = {
        'Poll': 0,           // ActivityType.Poll
        'Quiz': 1,           // ActivityType.Quiz
  'WordCloud': 2,      // ActivityType.WordCloud
        'QnA': 3,            // ActivityType.QnA
        'Rating': 4,         // ActivityType.Rating
        'Quadrant': 5,    // ActivityType.Quadrant
      'FiveWhys': 6,       // ActivityType.FiveWhys
        'GeneralFeedback': 7, // ActivityType.GeneralFeedback
        'AiSummary': 8,      // ActivityType.AiSummary
        'Break': 9    // ActivityType.Break
    };

    // Initialize on DOM load
 if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    function init() {
      const fileInput = document.getElementById('csvFileInput');
        const uploadBtn = document.getElementById('uploadCsvBtn');
  const downloadBtn = document.getElementById('downloadCsvTemplateBtn');

        if (!fileInput || !uploadBtn || !downloadBtn) {
            console.warn('CSV bulk upload elements not found');
            return;
        }

        // Enable upload button when file is selected
      fileInput.addEventListener('change', (e) => {
      uploadBtn.disabled = !e.target.files || e.target.files.length === 0;
        });

   // Handle CSV upload
        uploadBtn.addEventListener('click', handleCsvUpload);

      // Handle template download
        downloadBtn.addEventListener('click', handleTemplateDownload);
    }

    async function handleCsvUpload() {
        const fileInput = document.getElementById('csvFileInput');
        const statusDiv = document.getElementById('csvUploadStatus');
      const uploadBtn = document.getElementById('uploadCsvBtn');

        if (!fileInput.files || fileInput.files.length === 0) {
            showStatus('error', 'Please select a CSV file');
  return;
        }

   const file = fileInput.files[0];
        if (!file.name.toLowerCase().endsWith('.csv')) {
            showStatus('error', 'Please select a valid CSV file');
            return;
        }

        try {
   // Show loading state
  uploadBtn.disabled = true;
            uploadBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Uploading...';
      showStatus('info', 'Reading CSV file...');

            // Read and parse CSV
      const csvText = await readFileAsText(file);
            const activities = parseCsvToActivities(csvText);

            if (activities.length === 0) {
        throw new Error('No valid activities found in CSV');
            }

    if (activities.length > 100) {
  throw new Error('Cannot upload more than 100 activities at once');
            }

        showStatus('info', `Uploading ${activities.length} activities...`);

      // Get session code from page
  const sessionCode = document.getElementById('hiddenSessionCode')?.value;
    if (!sessionCode) {
      throw new Error('Session code not found');
            }

         // Call bulk create API
       const response = await fetch(`/api/sessions/${sessionCode}/activities/bulk`, {
    method: 'POST',
      headers: {
         'Content-Type': 'application/json'
     },
   body: JSON.stringify({
   activities: activities
    })
         });

            if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
     throw new Error(errorData.errors?.[0]?.message || errorData.message || 'Failed to upload activities');
    }

            const result = await response.json();
      const data = result.data || result;

            // Show results
      if (data.errors && data.errors.length > 0) {
                const errorMsg = `Created ${data.successCount} activities with ${data.errors.length} errors:\n${data.errors.join('\n')}`;
 showStatus('warning', errorMsg);
            } else {
 showStatus('success', `Successfully created ${data.successCount} activities!`);
         }

 // Clear file input
            fileInput.value = '';
            uploadBtn.disabled = true;

            // Reload activities list after a short delay
      setTimeout(() => {
                if (window.activityManager && typeof window.activityManager.loadActivities === 'function') {
  window.activityManager.loadActivities();
            } else {
  window.location.reload();
}
  }, 2000);

        } catch (error) {
      console.error('CSV upload error:', error);
  showStatus('error', error.message);
        } finally {
         uploadBtn.disabled = false;
     uploadBtn.innerHTML = '<i class="ics ics-upload ic-sm ic-mr"></i>Upload';
        }
    }

    function handleTemplateDownload(e) {
        e.preventDefault();

     // Generate CSV template
     const csvContent = generateCsvTemplate();

        // Create blob and download
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
 const link = document.createElement('a');
   link.href = url;
        link.download = 'activity-template.csv';
      link.style.display = 'none';
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(url);
    }

  function generateCsvTemplate() {
        const headers = ['Order', 'Type', 'Title', 'Prompt', 'ConfigJson', 'DurationInMin'];
 
        const examples = [
    ['1', 'Poll', 'How satisfied are you?', 'Rate your satisfaction', '{"options":[{"id":"option_0","label":"Very Satisfied"},{"id":"option_1","label":"Satisfied"},{"id":"option_2","label":"Neutral"},{"id":"option_3","label":"Dissatisfied"}],"allowMultiple":false}', '5'],
            ['2', 'WordCloud', 'What comes to mind?', 'Share your thoughts', '{"maxWords":3}', '5'],
         ['3', 'Rating', 'Rate this session', 'How would you rate this?', '{"scale":5}', '3'],
  ['4', 'Quadrant', 'Prioritize features', 'Place features on the matrix', '{"xAxisLabel":"Effort","yAxisLabel":"Impact","topLeft":"High Impact Low Effort","topRight":"High Impact High Effort","bottomLeft":"Low Impact Low Effort","bottomRight":"Low Impact High Effort"}', '10'],
         ['5', 'FiveWhys', 'Root cause analysis', 'Why did this happen?', '{"maxDepth":5}', '15'],
          ['6', 'GeneralFeedback', 'Share feedback', 'What feedback do you have?', '{}', '5'],
   ['7', 'QnA', 'Questions & Answers', 'Ask your questions', '{}', '10'],
     ['8', 'AiSummary', 'AI Summary', 'Generate session summary', '{}', '0'],
         ['9', 'Break', 'Coffee Break', 'Take a 10 minute break', '{}', '10']
        ];

        // Create CSV with header and examples
        let csv = headers.join(',') + '\n';
   examples.forEach(row => {
            // Properly escape fields containing commas or quotes
            const escapedRow = row.map(field => {
       if (field.includes(',') || field.includes('"') || field.includes('\n')) {
    return '"' + field.replace(/"/g, '""') + '"';
    }
     return field;
       });
          csv += escapedRow.join(',') + '\n';
        });

      return csv;
    }

    function parseCsvToActivities(csvText) {
     // Remove BOM if present
 if (csvText.charCodeAt(0) === 0xFEFF) {
      csvText = csvText.slice(1);
        }
        
        const lines = csvText.split('\n').filter(line => line.trim());
        if (lines.length < 2) {
     throw new Error('CSV file is empty or has no data rows');
        }

      // Parse header and trim whitespace from each header
        const header = parseCsvLine(lines[0]).map(h => h.trim());
        const expectedHeaders = ['Order', 'Type', 'Title', 'Prompt', 'ConfigJson', 'DurationInMin'];
        
        // Log for debugging
        console.log('Parsed headers:', header);
     console.log('Expected headers:', expectedHeaders);
  
        // Validate headers (case-insensitive)
  const headerLower = header.map(h => h.toLowerCase());
        const expectedLower = expectedHeaders.map(h => h.toLowerCase());
        
        const hasAllHeaders = expectedLower.every(expected => 
            headerLower.includes(expected)
        );
        
        if (!hasAllHeaders) {
    const missing = expectedLower.filter(expected => !headerLower.includes(expected));
   console.error('Header validation failed. Found headers:', header);
        console.error('Expected headers:', expectedHeaders);
            console.error('Missing headers:', missing);
    throw new Error(`CSV must have headers: ${expectedHeaders.join(', ')}. Missing: ${missing.join(', ')}. Found: ${header.join(', ')}`);
        }

        // Find column indices
        const indices = {};
   expectedHeaders.forEach(h => {
   indices[h] = headerLower.indexOf(h.toLowerCase());
        });
 
        console.log('Column indices:', indices);

        // Parse data rows
    const activities = [];
        for (let i = 1; i < lines.length; i++) {
        try {
      const row = parseCsvLine(lines[i]);
     if (row.length === 0 || row.every(cell => !cell.trim())) {
        continue; // Skip empty rows
   }

         const order = parseInt(row[indices.Order]?.trim());
   const type = row[indices.Type]?.trim();
  const title = row[indices.Title]?.trim();
       const prompt = row[indices.Prompt]?.trim() || null;
         const configJson = row[indices.ConfigJson]?.trim() || '{}';
 const durationStr = row[indices.DurationInMin]?.trim();
 const durationMinutes = durationStr ? parseInt(durationStr) : 5;

 // Log parsed data for debugging
    console.log(`Row ${i + 1} - Type: "${type}", Title: "${title}"`);

    // Validate
         if (!order || order <= 0) {
       throw new Error(`Invalid order value: ${row[indices.Order]}`);
   }
        if (!type) {
 throw new Error('Type is required');
        }
 if (!title) {
        throw new Error('Title is required');
     }

      // Validate JSON config first
            try {
       JSON.parse(configJson);
 } catch {
                throw new Error('ConfigJson must be valid JSON');
            }

            // Map type name to enum value (case-insensitive match)
       let mappedType = ACTIVITY_TYPES[type];
     
  if (!mappedType) {
 // Try case-insensitive match
   const typeKey = Object.keys(ACTIVITY_TYPES).find(
 key => key.toLowerCase() === type.toLowerCase()
          );
    
    if (!typeKey) {
       throw new Error(`Invalid activity type: "${type}". Valid types: ${Object.keys(ACTIVITY_TYPES).join(', ')}`);
      }
    
  mappedType = ACTIVITY_TYPES[typeKey];
      console.log(`Matched type "${type}" to "${typeKey}" (value: ${mappedType})`);
        }

            activities.push({
                order,
         type: mappedType,
   title,
    prompt,
      config: configJson,
             durationMinutes
     });
        } catch (error) {
 throw new Error(`Row ${i + 1}: ${error.message}`);
}
        }

        return activities;
    }

    function parseCsvLine(line) {
        const result = [];
        let current = '';
        let inQuotes = false;

        // Trim the line first to remove any trailing whitespace or carriage returns
        line = line.trim();

        for (let i = 0; i < line.length; i++) {
         const char = line[i];
       const nextChar = line[i + 1];

       if (char === '"') {
         if (inQuotes && nextChar === '"') {
        // Escaped quote
      current += '"';
      i++; // Skip next quote
      } else {
  // Toggle quote mode
    inQuotes = !inQuotes;
    }
            } else if (char === ',' && !inQuotes) {
      // End of field
         result.push(current);
             current = '';
            } else {
     current += char;
            }
      }

        // Add last field
     result.push(current);

        return result;
    }

    function readFileAsText(file) {
 return new Promise((resolve, reject) => {
  const reader = new FileReader();
            reader.onload = (e) => resolve(e.target.result);
      reader.onerror = (e) => reject(new Error('Failed to read file'));
   reader.readAsText(file);
        });
    }

    function showStatus(type, message) {
        const statusDiv = document.getElementById('csvUploadStatus');
        if (!statusDiv) return;

        const iconMap = {
      success: 'ics-check-mark',
      error: 'ics-cross-mark',
      warning: 'ics-warning',
            info: 'ics-info'
        };

        const colorMap = {
            success: 'alert-success',
      error: 'alert-danger',
   warning: 'alert-warning',
            info: 'alert-info'
        };

    statusDiv.className = `alert ${colorMap[type] || 'alert-info'} d-flex align-items-start gap-2 mt-2`;
    statusDiv.innerHTML = `
   <i class="ics ${iconMap[type] || 'ics-info'} ic-sm"></i>
            <div class="flex-grow-1" style="white-space: pre-wrap;">${escapeHtml(message)}</div>
        `;
      statusDiv.classList.remove('d-none');
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

})();
