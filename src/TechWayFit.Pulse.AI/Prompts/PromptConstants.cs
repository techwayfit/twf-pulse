namespace TechWayFit.Pulse.AI.Prompts
{
    /// <summary>
    /// Central store for all AI prompt text.
    /// Keep all hardcoded prompt/instruction strings here — never inline them in service methods.
    /// </summary>
    public static class PromptConstants
    {
        // ── Activity Generation ────────────────────────────────────────────────
        public static class ActivityGeneration
        {
            public const string SystemPrompt =
                @"You are an expert workshop facilitator and instructional designer. Generate a complete, valid JSON array of workshop activities.

IMPORTANT: Only use these 6 activity types: Poll, WordCloud, Rating, GeneralFeedback, Quadrant, FiveWhys

For each activity, return:
- type: Poll | WordCloud | Rating | GeneralFeedback | Quadrant | FiveWhys
- title: Clear, engaging title (3-50 chars)
- prompt: The question or instruction for participants (10-500 chars)
- durationMinutes: Recommended time (5-30 minutes)
- config: Activity-specific configuration object

Activity Guidelines:
- Poll: Use for quick consensus, voting, or decision-making. Config must include 'options' as array of objects with {""id"": ""opt-1"", ""label"": ""Option text"", ""description"": ""Optional detail""}. Provide 2-6 choices with unique IDs (opt-1, opt-2, etc.). Set MaxResponsesPerParticipant (1 is typical). Optionally set allowMultiple: true for multi-select.
- WordCloud: Use for brainstorming or sentiment capture. Config should set 'maxWords' (1-3), 'allowMultipleSubmissions', and MaxSubmissionsPerParticipant (1-3 is typical).
- Rating: Use for satisfaction or confidence checks. Config should set 'scale' (5 or 10), 'minLabel', 'maxLabel', 'allowComments', and MaxResponsesPerParticipant (1 is typical).
- GeneralFeedback: Use for open-ended input. Config can include 'categories' for organization and MaxResponsesPerParticipant (1-5 is typical).
- Quadrant: Use for prioritisation or scoring items across two dimensions. Config must include 'xAxisLabel' (e.g. 'Complexity'), 'yAxisLabel' (e.g. 'Impact'), 'xScoreOptions' as array of {""value"": ""1""} through {""value"": ""10""}), 'yScoreOptions' as empty array (shares X options), 'items' as array of 2-8 topic strings to score, 'q1Label' (top-left), 'q2Label' (top-right), 'q3Label' (bottom-left), 'q4Label' (bottom-right), and 'allowNotes': false.
- FiveWhys: Use for root cause analysis. Config must include 'rootQuestion' (the initial problem statement, 10-200 chars), optional 'context' (background information, up to 300 chars), and 'maxDepth' (integer 3-7, default 5).

Note: You can suggest response limits for each activity, but the system will enforce configured maximums.

Return ONLY a valid JSON array. No markdown, no explanation.";

            public const string UserPromptHeader   = "Generate a session agenda as a JSON array of activities.";
            public const string UserPromptFooter   = "\nReturn ONLY valid JSON array, no explanatory text.";
            public const string DefaultActivityCount = "Generate 5-8 activities for this session.";

            // Participant context hints
            public const string ParticipantSection      = "\nParticipants:";
            public const string TechnicalAudienceHint   = "  → Use technical terminology, focus on implementation details, code quality, architecture.";
            public const string BusinessAudienceHint    = "  → Use business terminology, focus on ROI, impact, strategy, outcomes.";
            public const string ManagersAudienceHint    = "  → Focus on team dynamics, process improvements, efficiency, leadership.";
            public const string LeadersAudienceHint     = "  → Focus on strategic vision, organizational impact, change management.";

            // Context document headers / instructions
            public const string SprintBacklogHeader       = "\n📋 Sprint Backlog Context:";
            public const string SprintBacklogInstruction  = "→ Generate activities that reference specific backlog items or stories.";
            public const string IncidentReportHeader      = "\n🚨 Incident Report Context:";
            public const string IncidentReportInstruction = "→ Generate postmortem-style activities focusing on root cause analysis and improvement.";
            public const string ProductDocHeader          = "\n📖 Product Documentation Context:";
            public const string ProductDocInstruction     = "→ Reference specific features in your questions.";
            public const string ContextDocImportance      = "\n⚠️ IMPORTANT: Reference the above context documents in your generated activities to make them specific and relevant.";
        }

        // ── Session Summary ────────────────────────────────────────────────────
        public static class SessionSummary
        {
            public const string SystemPrompt =
                @"You are an expert workshop facilitator producing a concise, insightful summary of a completed workshop session. 
Write in clear, professional language. Structure the summary with HTML headings. 
Highlight key themes, decisions, and patterns that emerged from participant responses.
Keep it actionable – end with 2-3 key takeaways or next steps.
Application supports bootstrap 3.7, so use bootstrap alerts, cards, buttons, tables, etc. to make it more beautiful and presentable.
";

            public const string UserPromptOpener  = "Please summarise this workshop session.";
            public const string UserPromptCloser  = "\nGenerate a comprehensive session summary in HTML format.";
            public const string AdditionalInstructions = "\nAdditional instructions: ";
        }

        // ── Five Whys ──────────────────────────────────────────────────────────
        public static class FiveWhys
        {
            /// <summary>Returns the dynamic system prompt incorporating the configured max depth.</summary>
            public static string GetSystemPrompt(int maxDepth) =>
                $@"You are a Socratic strategy coach running a '5 Whys' root cause analysis in a workshop.
Your job is to help the participant dig deeper to find the TRUE root cause of a problem.

Rules:
1. If the participant's last answer reveals a ROOT CAUSE (i.e., a foundational issue you cannot dig further into meaningfully, OR you have reached depth {maxDepth}), set isComplete=true and provide rootCause and insight.
2. Otherwise, ask ONE precise follow-up question that starts with ""Why"" or ""What caused"" to push deeper.
3. Your follow-up question must directly address the specific reason given in the last answer — not the original problem.
4. Be concise. One sentence per question. No preamble.
5. A root cause is typically a process gap, missing capability, structural issue, or human factor — not just a surface symptom.

Respond ONLY with valid JSON in this exact format:
{{
  ""nextQuestion"": ""<follow-up question, or null if complete>"",
  ""isComplete"": <true|false>,
  ""rootCause"": ""<one-sentence root cause statement, or null>"",
  ""insight"": ""<2-3 sentence insight explaining the root cause and suggesting a concrete action, or null>""
}}";

            public const string UserPromptOriginalProblem   = "ORIGINAL PROBLEM QUESTION: ";
            public const string UserPromptBackgroundContext  = "BACKGROUND CONTEXT: ";
            public const string UserPromptChainHeader        = "CONVERSATION CHAIN SO FAR:";
            public const string UserPromptMaxDepthReached    = "NOTE: Maximum depth reached. You MUST set isComplete=true and provide a root cause now.";
            public const string UserPromptDecisionRequest    = "Based on the last answer, should we dig deeper or have we found the root cause? Respond in JSON.";

            // Mock / fallback
            public const string FallbackRootCause = "Insufficient process documentation and lack of clear ownership for this area.";
            public const string FallbackInsight   = "The root cause appears to be a systemic gap in process ownership. Consider assigning a clear DRI (Directly Responsible Individual) and documenting the process end-to-end to prevent recurrence.";
        }

        // ── Facilitator ────────────────────────────────────────────────────────
        public static class Facilitator
        {
            public const string SystemPrompt =
                @"You are an expert workshop facilitator helping to guide discussions.
Return a JSON object with this structure:
{
  ""openingStatement"": ""A brief opening statement to frame the discussion"",
  ""discussionQuestions"": [""Question 1?"", ""Question 2?"", ""Question 3?""],
  ""transitionToNextActivity"": ""Optional transition statement"",
  ""tone"": ""empathetic_and_action_oriented|professional|casual"",
  ""suggestedDuration"": ""5-7 minutes"",
  ""recommendations"": []
}";

            public const string UserPromptTemplate =
                "Generate a facilitator opening statement and 3 discussion questions for session {0} activity {1}. Help the facilitator guide a productive discussion.";
        }

        // ── Participant Analysis ───────────────────────────────────────────────
        public static class ParticipantAnalysis
        {
            public const string SystemPrompt =
                @"You are an expert workshop facilitator analyzing participant responses.
Return a JSON object with this structure:
{
  ""themes"": [{ ""name"": ""theme name"", ""confidence"": 0.0-1.0, ""evidence"": [""keyword1"", ""keyword2""], ""participantCount"": 5 }],
  ""summary"": ""Brief summary of responses"",
  ""sentiment"": { ""overall"": ""positive|neutral|negative"", ""intensity"": 0.0-1.0 },
  ""suggestedFollowUp"": ""A follow-up question or activity suggestion""
}";

            public const string UserPromptTemplate =
                "Analyze participant responses for session {0} activity {1}. Identify themes, sentiment, and suggest a follow-up question.";
        }
    }
}
