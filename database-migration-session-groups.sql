-- Manual migration script for Session Groups
-- Run this script against your SQLite database to add session groups support

-- Create SessionGroups table
CREATE TABLE IF NOT EXISTS "SessionGroups" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_SessionGroups" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Description" TEXT NULL,
    "Level" INTEGER NOT NULL,
    "ParentGroupId" TEXT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL,
    "FacilitatorUserId" TEXT NULL
);

-- Create indexes for SessionGroups
CREATE INDEX IF NOT EXISTS "IX_SessionGroups_FacilitatorUserId" ON "SessionGroups" ("FacilitatorUserId");
CREATE INDEX IF NOT EXISTS "IX_SessionGroups_ParentGroupId" ON "SessionGroups" ("ParentGroupId");
CREATE INDEX IF NOT EXISTS "IX_SessionGroups_FacilitatorUserId_Level_ParentGroupId" ON "SessionGroups" ("FacilitatorUserId", "Level", "ParentGroupId");

-- Add GroupId column to Sessions table
ALTER TABLE "Sessions" ADD COLUMN "GroupId" TEXT NULL;

-- Create index for Sessions.GroupId
CREATE INDEX IF NOT EXISTS "IX_Sessions_GroupId" ON "Sessions" ("GroupId");