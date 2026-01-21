-- Add DurationMinutes column to Activities table
-- This allows optional timeboxing for each activity

ALTER TABLE Activities ADD COLUMN DurationMinutes INTEGER NULL;

-- Optional: Set default duration for existing activities (e.g., 10 minutes for polls, 5 for word clouds)
-- UPDATE Activities SET DurationMinutes = 10 WHERE Type = 0 AND DurationMinutes IS NULL; -- Poll
-- UPDATE Activities SET DurationMinutes = 5 WHERE Type = 1 AND DurationMinutes IS NULL;  -- WordCloud
-- UPDATE Activities SET DurationMinutes = 5 WHERE Type = 2 AND DurationMinutes IS NULL;  -- Rating
-- UPDATE Activities SET DurationMinutes = 10 WHERE Type = 3 AND DurationMinutes IS NULL; -- GeneralFeedback
