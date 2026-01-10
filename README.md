# TechWayFit - Pulse

> **Make Workshops Fun, Measurable, and Focused.**

**TechWayFit - Pulse** is an open-source, single-purpose workshop engagement platform designed to capture high-quality, filterable insights through a facilitator-controlled flow. Unlike broad whiteboarding tools, Pulse focuses on structured activities—such as 4-quadrant mapping and 5-Whys analysis—to produce actionable, measurable data in real-time.

---

## 🚀 Key Features

* **Facilitator-Led Pacing**: Control the workshop momentum by unlocking activities one-by-one, ensuring participants stay synchronized.
* **Structured Insights**: Generate real-time visualizations including Word Clouds, 4-Quadrant Scatters, 5-Whys Ladders, and Rating distributions.
* **Filterable Dashboards**: Use up to 5 custom participant dimensions (e.g., Team, Role) to slice and dice workshop results live.
* **Contribution Guardrails**: Enforce a maximum number (N) of responses per participant to ensure concise, high-impact feedback.
* **Real-Time Engagement**: Powered by SignalR to provide instant dashboard updates and state changes.

---

## 🛠️ Tech Stack

* **Framework**: ASP.NET Core.
* **UI Layer**: Blazor Server for both Facilitator and Participant experiences.
* **Real-time**: SignalR for live event broadcasting.
* **Persistence**: EF Core with SQLite (Production-lite) or InMemory (Development).
* **API**: Minimal APIs for core service contracts and session management.

---

## 📋 How It Works

1.  **Create**: A facilitator sets up a session, defines the join form schema (max 5 fields), and builds an activity sequence.
2.  **Join**: Participants enter via a short code or QR link and fill out their lightweight profile based on the session schema.
3.  **Engage**: The facilitator opens activities one at a time. Participants submit responses until they hit the session-wide contribution limit.
4.  **Analyze**: View live dashboards that can be filtered by participant dimensions to identify trends across different segments.

---

## 🏗️ Architecture & Logic

* **State Machine**: Manages the progression of activities from `Pending` to `Open` to `Closed`, ensuring only one activity is active at a time.
* **Data Model**: Utilizes JSON columns in SQLite for flexibility in storing varied activity configurations and response payloads.
* **Security & Privacy**: Sessions are accessed via unique codes, and participant data can be set to anonymous or aggregated modes.
* **Operational TTL**: Automatic background cleanup service deletes expired sessions and related data to maintain performance.

---

## 🛑 Constraints & Rules

* **Join Form Limit**: Maximum of 5 fields to keep the onboarding process frictionless.
* **Strict Mode**: Submissions are only accepted for the activity currently marked as "Open" by the facilitator.
* **Contribution Limits**: Total participant points are tracked via `ParticipantCounters` to prevent individual participants from dominating the session.

---

**Would you like me to generate the C# implementation for the `Session` and `Activity` entities based on this data model?**
