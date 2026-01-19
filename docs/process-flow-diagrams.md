# TechWayFit Pulse - Process Flow Diagrams
**Date:** January 17, 2026  
**Platform:** .NET 8 Blazor Server with SignalR  

---

## Table of Contents
1. [Create Activity Flow](#1-create-activity-flow)
2. [Facilitator Start Session Flow](#2-facilitator-start-session-flow)
3. [Participant Join Session Flow](#3-participant-join-session-flow)
4. [Facilitator Control Session Flow](#4-facilitator-control-session-flow)
5. [SignalR Connection - Facilitator Live Page](#5-signalr-connection---facilitator-live-page)
6. [SignalR Connection - Activity Type Dashboards](#6-signalr-connection---activity-type-dashboards)
7. [SignalR Connection - Participant Submission](#7-signalr-connection---participant-submission)
8. [Login and Session Management Flow](#8-login-and-session-management-flow)

---

## 1. Create Activity Flow

### Overview
This flow describes how a facilitator creates an activity within a session, including validation, persistence, and real-time notifications.

```mermaid
sequenceDiagram
    participant F as Facilitator UI<br/>(Blazor/MVC)
    participant AC as SessionsController<br/>/api/sessions
    participant AS as ActivityService<br/>(Application)
    participant AR as ActivityRepository<br/>(Infrastructure)
    participant DB as SQLite Database
    participant Hub as SignalR Hub<br/>(WorkshopHub)
    participant P as Participants<br/>(SignalR Clients)

    Note over F,P: Activity Creation Flow

    F->>F: Fill activity form<br/>(Title, Type, Prompt, Config)
    F->>AC: POST /api/sessions/{code}/activities<br/>CreateActivityRequest

    activate AC
    AC->>AC: Validate Request<br/>(Title, Type, Config)
    
    alt Validation Fails
        AC-->>F: 400 Bad Request<br/>{error: "validation_error"}
    end

    AC->>AS: GetSessionByCodeAsync(code)
    activate AS
    AS->>AR: Repository.GetByCodeAsync(code)
    AR->>DB: SELECT * FROM Sessions<br/>WHERE Code = @code
    DB-->>AR: SessionRecord
    AR-->>AS: Session Entity
    AS-->>AC: Session
    deactivate AS

    alt Session Not Found
        AC-->>F: 404 Not Found<br/>{error: "Session not found"}
    end

    alt Session Not Owned by Facilitator
        AC-->>F: 403 Forbidden<br/>{error: "Unauthorized"}
    end

    AC->>AS: CreateActivityAsync(sessionId, type, title, prompt, config)
    activate AS
    
    AS->>AS: Generate Activity ID<br/>Guid.NewGuid()
    AS->>AS: Determine Order<br/>(Max Order + 1)
    AS->>AS: Create Domain Entity<br/>new Activity(...)
    
    AS->>AR: Repository.AddAsync(activity)
    activate AR
    AR->>AR: Map to Record<br/>activity.ToRecord()
    AR->>DB: INSERT INTO Activities<br/>(Id, SessionId, Order, Type,<br/>Title, Prompt, Config, Status)
    DB-->>AR: Success
    AR-->>AS: void
    deactivate AR
    
    AS-->>AC: Activity Entity
    deactivate AS

    AC->>AC: Map to Response<br/>ActivityResponse
    AC->>Hub: Clients.Group(sessionCode)<br/>.ActivityStateChanged(event)
    
    activate Hub
    Hub->>P: Broadcast: ActivityStateChanged<br/>{activityId, status: "Draft"}
    deactivate Hub
    
    AC-->>F: 200 OK<br/>ActivityResponse
    deactivate AC

    Note over F: UI updates with new activity
    Note over P: Participants see new activity (if Live)
```

### Key Components:

1. **Request Validation**
   - Title required and ≤ 200 chars
   - ActivityType enum validation
   - Config JSON validation (activity-type specific)

2. **Business Rules**
   - Activity order auto-assigned (max + 1)
   - New activities default to "Draft" status
   - Only facilitator who owns session can create activities

3. **Real-Time Notification**
   - SignalR broadcasts `ActivityStateChanged` event
   - All connected clients in session group receive update
   - Facilitator dashboard and participant views auto-update

---

## 2. Facilitator Start Session Flow

### Overview
This flow shows how a facilitator transitions a session from "Draft" to "Live" status, enabling participant access.

```mermaid
sequenceDiagram
    participant F as Facilitator UI<br/>(Live.razor)
    participant AC as SessionsController<br/>/api/sessions
    participant SS as SessionService<br/>(Application)
    participant SR as SessionRepository<br/>(Infrastructure)
    participant DB as SQLite Database
    participant Hub as SignalR Hub<br/>(WorkshopHub)
    participant P as Participant Clients<br/>(Waiting to Join)

    Note over F,P: Start Session Flow

    F->>F: Click "Start Session" button
    F->>F: Disable button<br/>(isPerformingAction = true)
    
    F->>AC: PUT /api/sessions/{code}/start
    activate AC
    
    AC->>AC: Get Facilitator User ID<br/>from HttpContext
    
    AC->>SS: GetByCodeAsync(code)
    activate SS
    SS->>SR: Repository.GetByCodeAsync(code)
    SR->>DB: SELECT * FROM Sessions<br/>WHERE Code = @code
    DB-->>SR: SessionRecord
    SR-->>SS: Session Entity
    SS-->>AC: Session
    deactivate SS

    alt Session Not Found
        AC-->>F: 404 Not Found<br/>{error: "Session not found"}
        F->>F: Show error message
        F->>F: Re-enable button
    end

    alt Session Not Owned by Facilitator
        AC-->>F: 403 Forbidden<br/>{error: "Unauthorized"}
        F->>F: Show error message
        F->>F: Re-enable button
    end

    alt Session Already Live or Ended
        AC-->>F: 400 Bad Request<br/>{error: "Invalid status transition"}
        F->>F: Show error message
        F->>F: Re-enable button
    end

    AC->>SS: SetStatusAsync(sessionId,<br/>SessionStatus.Live, DateTimeOffset.UtcNow)
    activate SS
    
    SS->>SR: Repository.GetByIdAsync(sessionId)
    SR->>DB: SELECT * FROM Sessions<br/>WHERE Id = @id
    DB-->>SR: SessionRecord
    SR-->>SS: Session Entity
    
    SS->>SS: session.SetStatus(<br/>SessionStatus.Live, now)
    SS->>SS: Update UpdatedAt timestamp
    
    SS->>SR: Repository.UpdateAsync(session)
    activate SR
    SR->>SR: Map to Record<br/>session.ToRecord()
    SR->>DB: UPDATE Sessions SET<br/>Status = 'Live',<br/>UpdatedAt = @now<br/>WHERE Id = @id
    DB-->>SR: Success
    SR-->>SS: void
    deactivate SR
    
    SS-->>AC: void
    deactivate SS

    AC->>Hub: Clients.Group(sessionCode)<br/>.SessionStateChanged(event)
    activate Hub
    Hub->>F: SessionStateChanged<br/>{sessionId, status: "Live"}
    Hub->>P: SessionStateChanged<br/>{sessionId, status: "Live"}
    deactivate Hub

    AC-->>F: 200 OK<br/>{success: true}
    deactivate AC

    F->>F: Update UI state<br/>(session.Status = "Live")
    F->>F: Re-enable controls
    F->>F: Change button to<br/>"End Session"

    P->>P: Enable join button<br/>(Session now accepting participants)

    Note over F: Facilitator can now open activities
    Note over P: Participants can now join
```

### Key State Transitions:

```
Draft → Live → Ended
  ↓      ↓
Valid  Valid
```

### Business Rules:

1. Only session owner (facilitator) can start session
2. Session must be in "Draft" status to start
3. Once started, session accepts participant joins
4. Session TTL countdown begins (typically 6 hours)
5. All connected clients notified in real-time

---

## 3. Participant Join Session Flow

### Overview
This flow details how a participant joins a live session, submits join form data, and receives a participant token.

```mermaid
sequenceDiagram
    participant P as Participant Browser
    participant JC as ParticipantController<br/>/participant/join
    participant AC as SessionsController<br/>/api/sessions
    participant SS as SessionService
    participant PS as ParticipantService
    participant PR as ParticipantRepository
    participant DB as SQLite Database
    participant Hub as SignalR Hub
    participant F as Facilitator<br/>(Live Dashboard)

    Note over P,F: Participant Join Flow

    P->>P: Navigate to<br/>/participant/join
    P->>JC: GET /participant/join?code={code}
    activate JC
    
    alt No code provided
        JC->>JC: Render code entry form
        JC-->>P: Join.cshtml (Enter Code)
        P->>P: Enter session code
        P->>JC: POST /participant/join<br/>{code}
    end

    JC->>AC: GET /api/sessions/{code}
    activate AC
    AC->>SS: GetByCodeAsync(code)
    SS->>DB: SELECT * FROM Sessions WHERE Code = @code
    DB-->>SS: SessionRecord
    SS-->>AC: Session
    AC-->>JC: SessionSummaryResponse
    deactivate AC

    alt Session Not Found
        JC-->>P: 404 Page<br/>"Session not found"
    end

    alt Session Not Live
        JC-->>P: Error Page<br/>"Session not accepting participants"
    end

    JC->>JC: Render join form<br/>(based on JoinFormSchema)
    JC-->>P: Join.cshtml (Join Form)
    deactivate JC

    P->>P: Fill join form<br/>(Name, Email, Custom Fields)
    P->>AC: POST /api/sessions/{code}/participants<br/>JoinParticipantRequest
    
    activate AC
    AC->>AC: Validate Request<br/>(Required fields, email format)
    
    alt Validation Fails
        AC-->>P: 400 Bad Request<br/>{error: "validation_error"}
        P->>P: Show validation errors
    end

    AC->>SS: GetByCodeAsync(code)
    SS->>DB: SELECT * FROM Sessions
    DB-->>SS: Session
    SS-->>AC: Session

    alt Session Not Live
        AC-->>P: 400 Bad Request<br/>{error: "Session not accepting joins"}
    end

    AC->>PS: JoinSessionAsync(sessionId, name, email, formData)
    activate PS
    
    PS->>PS: Generate Participant ID<br/>Guid.NewGuid()
    PS->>PS: Create Participant Entity<br/>new Participant(id, sessionId, name, email, formData)
    
    PS->>PR: Repository.AddAsync(participant)
    activate PR
    PR->>DB: INSERT INTO Participants<br/>(Id, SessionId, Name,<br/>Email, FormData, JoinedAt)
    DB-->>PR: Success
    PR-->>PS: void
    deactivate PR
    
    PS-->>AC: ParticipantResponse
    deactivate PS

    AC->>AC: Generate participant token<br/>(JWT or session-based)
    
    AC->>Hub: Clients.Group(sessionCode)<br/>.ParticipantJoined(event)
    activate Hub
    Hub->>F: ParticipantJoined<br/>{participantId, name, email, joinedAt}
    deactivate Hub

    AC-->>P: 200 OK<br/>{participantId, token, sessionCode}
    deactivate AC

    P->>P: Store token in localStorage<br/>Set session cookie
    P->>P: Redirect to<br/>/participant/activity?code={code}&participantId={id}

    F->>F: Update participant count<br/>Add participant to list

    Note over P: Participant is now in session
    Note over F: Facilitator sees new participant in real-time
```

### Join Form Validation:

```csharp
// JoinFormSchema structure
{
  "fields": [
    {
      "name": "name",
      "label": "Your Name",
      "type": "text",
      "required": true
    },
    {
      "name": "email",
      "label": "Email Address",
      "type": "email",
      "required": true
    },
    {
      "name": "organization",
      "label": "Organization",
      "type": "text",
      "required": false
    }
  ]
}
```

### Security Considerations:

1. Session code validated before showing join form
2. Email format validation
3. Rate limiting on join endpoint (prevent spam)
4. Participant token generated for subsequent requests
5. Session must be "Live" to accept joins

---

## 4. Facilitator Control Session Flow

### Overview
This flow shows how facilitators control activities (open, close, switch) during a live session.

```mermaid
sequenceDiagram
    participant F as Facilitator UI<br/>(Live.razor)
    participant AC as SessionsController<br/>/api/sessions
    participant AS as ActivityService
    participant AR as ActivityRepository
    participant SR as SessionRepository
    participant DB as SQLite Database
    participant Hub as SignalR Hub
    participant P as Participants<br/>(Activity Pages)

    Note over F,P: Activity Control Flow

    rect rgb(200, 220, 255)
        Note over F,P: 1. Open Activity
        
        F->>F: Click "Open" on Activity Card
        F->>AC: POST /api/sessions/{code}/activities/{activityId}/open
        
        activate AC
        AC->>AS: GetActivityAsync(activityId)
        AS->>AR: Repository.GetByIdAsync(activityId)
        AR->>DB: SELECT * FROM Activities WHERE Id = @id
        DB-->>AR: ActivityRecord
        AR-->>AS: Activity
        AS-->>AC: Activity

        alt Activity Not Found
            AC-->>F: 404 Not Found
        end

        AC->>AS: OpenActivityAsync(activityId, DateTimeOffset.UtcNow)
        activate AS
        
        AS->>AS: activity.Open(now)
        AS->>AS: Set Status = Open<br/>Set OpenedAt = now<br/>Clear ClosedAt
        
        AS->>AR: Repository.UpdateAsync(activity)
        AR->>DB: UPDATE Activities SET<br/>Status = 'Open',<br/>OpenedAt = @now,<br/>ClosedAt = NULL
        DB-->>AR: Success
        AR-->>AS: void
        
        AS->>SR: Update Session.CurrentActivityId
        SR->>DB: UPDATE Sessions SET<br/>CurrentActivityId = @activityId
        DB-->>SR: Success
        
        AS-->>AC: void
        deactivate AS

        AC->>Hub: Clients.Group(sessionCode)<br/>.ActivityStateChanged(event)
        activate Hub
        Hub->>F: ActivityStateChanged<br/>{activityId, status: "Open"}
        Hub->>P: ActivityStateChanged<br/>{activityId, status: "Open"}
        deactivate Hub

        AC-->>F: 200 OK
        deactivate AC

        F->>F: Update UI<br/>(Activity badge → "Live")
        P->>P: Enable activity UI<br/>(Show activity interface)
    end

    rect rgb(255, 220, 200)
        Note over F,P: 2. Close Activity
        
        F->>F: Click "Close" on Activity Card
        F->>AC: POST /api/sessions/{code}/activities/{activityId}/close
        
        activate AC
        AC->>AS: CloseActivityAsync(activityId, DateTimeOffset.UtcNow)
        activate AS
        
        AS->>AS: activity.Close(now)
        AS->>AS: Set Status = Closed<br/>Set ClosedAt = now
        
        AS->>AR: Repository.UpdateAsync(activity)
        AR->>DB: UPDATE Activities SET<br/>Status = 'Closed',<br/>ClosedAt = @now
        DB-->>AR: Success
        AR-->>AS: void
        
        AS->>SR: Clear Session.CurrentActivityId
        SR->>DB: UPDATE Sessions SET<br/>CurrentActivityId = NULL
        DB-->>SR: Success
        
        AS-->>AC: void
        deactivate AS

        AC->>Hub: Clients.Group(sessionCode)<br/>.ActivityStateChanged(event)
        activate Hub
        Hub->>F: ActivityStateChanged<br/>{activityId, status: "Closed"}
        Hub->>P: ActivityStateChanged<br/>{activityId, status: "Closed"}
        deactivate Hub

        AC-->>F: 200 OK
        deactivate AC

        F->>F: Update UI<br/>(Activity badge → "Closed")
        P->>P: Disable activity UI<br/>(Show "Activity Closed" message)
    end

    rect rgb(220, 255, 200)
        Note over F,P: 3. Switch to Different Activity
        
        F->>F: Open Activity B<br/>(while Activity A is open)
        
        F->>AC: POST /api/sessions/{code}/activities/{activityBId}/open
        activate AC
        
        AC->>AS: OpenActivityAsync(activityBId)
        activate AS
        
        AS->>AS: Check if another activity<br/>is currently open
        
        alt Another Activity Open
            AS->>AS: Close previous activity<br/>activity.Close(now)
            AS->>AR: UpdateAsync(previousActivity)
            AR->>DB: UPDATE Activities SET Status = 'Closed'
            
            AC->>Hub: Clients.Group(sessionCode)<br/>.ActivityStateChanged(closedEvent)
            Hub->>P: ActivityStateChanged<br/>{previousActivityId, status: "Closed"}
        end
        
        AS->>AS: Open new activity<br/>activity.Open(now)
        AS->>AR: UpdateAsync(newActivity)
        AR->>DB: UPDATE Activities SET Status = 'Open'
        
        AS->>SR: Update CurrentActivityId
        SR->>DB: UPDATE Sessions SET<br/>CurrentActivityId = @activityBId
        
        AS-->>AC: void
        deactivate AS

        AC->>Hub: Clients.Group(sessionCode)<br/>.ActivityStateChanged(openEvent)
        Hub->>F: ActivityStateChanged<br/>{activityBId, status: "Open"}
        Hub->>P: ActivityStateChanged<br/>{activityBId, status: "Open"}

        AC-->>F: 200 OK
        deactivate AC

        F->>F: Update both activities in UI
        P->>P: Switch to new activity view
    end

    Note over F: Facilitator maintains full control
    Note over P: Participants see real-time transitions
```

### Activity State Diagram:

```mermaid
stateDiagram-v2
    [*] --> Draft: Activity Created
    Draft --> Open: Facilitator Opens
    Open --> Closed: Facilitator Closes
    Closed --> Open: Facilitator Re-opens
    Open --> Open: Switch to Different Activity<br/>(Auto-close previous)
    Closed --> [*]
```

### Business Rules:

1. **Single Active Activity**: Only one activity can be "Open" at a time per session
2. **Auto-Close Previous**: Opening a new activity automatically closes the currently open one
3. **Re-open Allowed**: Closed activities can be re-opened
4. **Real-Time Sync**: All state changes broadcast via SignalR
5. **Authorization**: Only session owner can control activities

---

## 5. SignalR Connection - Facilitator Live Page

### Overview
Detailed flow showing how the facilitator's live dashboard establishes and maintains SignalR connection.

```mermaid
sequenceDiagram
    participant Browser as Facilitator Browser
    participant Blazor as Live.razor<br/>(Blazor Component)
    participant JS as JavaScript Runtime
    participant SRClient as SignalR Client<br/>(HubConnection)
    participant Server as ASP.NET Core Server
    participant Hub as WorkshopHub
    participant Groups as SignalR Groups

    Note over Browser,Groups: Facilitator Live Page - SignalR Connection

    Browser->>Blazor: Navigate to /facilitator/live?code={code}
    activate Blazor
    
    Blazor->>Blazor: OnInitializedAsync()
    Blazor->>Blazor: Load session data<br/>sessionCode = QueryString["code"]
    
    Blazor->>Blazor: Call InitializeSignalR()
    
    Blazor->>SRClient: new HubConnectionBuilder()<br/>.WithUrl("/hubs/workshop")<br/>.Build()
    activate SRClient
    
    Note over SRClient: Configure reconnection
    SRClient->>SRClient: .WithAutomaticReconnect()<br/>(0s, 2s, 10s, 30s delays)
    
    Note over SRClient: Register event handlers
    
    rect rgb(200, 220, 255)
        Note over Blazor,SRClient: Register Event: SessionStateChanged
        Blazor->>SRClient: hubConnection.On<SessionStateChangedEvent><br/>("SessionStateChanged", handler)
        SRClient->>SRClient: Store handler:<br/>async (sessionEvent) => {<br/>  session.Status = sessionEvent.Status<br/>  await InvokeAsync(StateHasChanged)<br/>}
    end

    rect rgb(220, 255, 220)
        Note over Blazor,SRClient: Register Event: ParticipantJoined
        Blazor->>SRClient: hubConnection.On<ParticipantJoinedEvent><br/>("ParticipantJoined", handler)
        SRClient->>SRClient: Store handler:<br/>async (participantEvent) => {<br/>  participantCount++<br/>  await InvokeAsync(StateHasChanged)<br/>}
    end

    rect rgb(255, 220, 220)
        Note over Blazor,SRClient: Register Event: ActivityStateChanged
        Blazor->>SRClient: hubConnection.On<ActivityStateChangedEvent><br/>("ActivityStateChanged", handler)
        SRClient->>SRClient: Store handler:<br/>async (activityEvent) => {<br/>  UpdateActivityInList(activityEvent)<br/>  await InvokeAsync(StateHasChanged)<br/>}
    end

    rect rgb(255, 255, 200)
        Note over Blazor,SRClient: Register Event: ResponseReceived
        Blazor->>SRClient: hubConnection.On<ResponseReceivedEvent><br/>("ResponseReceived", handler)
        SRClient->>SRClient: Store handler:<br/>async (responseEvent) => {<br/>  await RefreshDashboardData()<br/>}
    end

    Note over SRClient: Start connection
    SRClient->>Server: WebSocket Handshake<br/>GET /hubs/workshop
    activate Server
    Server->>Server: Negotiate transport<br/>(WebSockets preferred)
    Server-->>SRClient: 101 Switching Protocols
    SRClient-->>Blazor: Connection State: Connected
    deactivate Server

    Blazor->>SRClient: hubConnection.InvokeAsync(<br/>"Subscribe", sessionCode)
    activate SRClient
    SRClient->>Server: RPC: Subscribe(sessionCode)
    activate Server
    Server->>Hub: Subscribe(sessionCode)
    activate Hub
    
    Hub->>Hub: Validate sessionCode
    Hub->>Groups: AddToGroupAsync(<br/>Context.ConnectionId,<br/>sessionCode)
    activate Groups
    Groups->>Groups: Add connection to group<br/>"SESSION_{code}"
    Groups-->>Hub: void
    deactivate Groups
    
    Hub-->>Server: void
    deactivate Hub
    Server-->>SRClient: Success
    deactivate Server
    SRClient-->>Blazor: Subscribed successfully
    deactivate SRClient

    Blazor->>Blazor: isLoading = false<br/>StateHasChanged()
    Blazor-->>Browser: Render Live Dashboard
    deactivate Blazor

    Note over Browser,Groups: Connection established, listening for events

    rect rgb(240, 240, 240)
        Note over Browser,Groups: Event Reception Example
        
        Server->>Hub: Activity opened by facilitator
        activate Hub
        Hub->>Groups: Clients.Group(sessionCode)<br/>.ActivityStateChanged(event)
        activate Groups
        Groups->>SRClient: Send: ActivityStateChanged<br/>{activityId, status: "Open"}
        deactivate Groups
        deactivate Hub
        
        SRClient->>Blazor: Invoke handler:<br/>ActivityStateChanged(event)
        activate Blazor
        Blazor->>Blazor: Update activity in local state<br/>activities[i].Status = event.Status
        Blazor->>Blazor: await InvokeAsync(StateHasChanged)
        Blazor-->>Browser: Re-render UI with updated activity
        deactivate Blazor
    end

    rect rgb(255, 240, 240)
        Note over Browser,Groups: Reconnection Scenario
        
        SRClient-xServer: Connection lost<br/>(network issue)
        SRClient->>SRClient: OnReconnecting event
        SRClient->>Blazor: Connection state: Reconnecting
        Blazor->>Browser: Show "Reconnecting..." indicator

        SRClient->>Server: Attempt reconnect<br/>(with exponential backoff)
        Server-->>SRClient: Reconnected
        
        SRClient->>SRClient: OnReconnected event
        SRClient->>Blazor: Connection state: Connected
        
        Blazor->>SRClient: Re-subscribe to group<br/>InvokeAsync("Subscribe", sessionCode)
        SRClient->>Hub: Subscribe(sessionCode)
        Hub->>Groups: AddToGroupAsync(newConnectionId, sessionCode)
        
        Blazor->>Blazor: Refresh all data<br/>(session, activities, participants)
        Blazor-->>Browser: Hide reconnection indicator<br/>Update UI with fresh data
    end

    Note over Browser,Groups: Component disposal
    
    Browser->>Blazor: Navigate away / close tab
    activate Blazor
    Blazor->>Blazor: DisposeAsync()
    
    Blazor->>SRClient: hubConnection.InvokeAsync(<br/>"Unsubscribe", sessionCode)
    SRClient->>Hub: Unsubscribe(sessionCode)
    Hub->>Groups: RemoveFromGroupAsync(<br/>connectionId, sessionCode)
    
    Blazor->>SRClient: hubConnection.DisposeAsync()
    SRClient->>Server: Close WebSocket connection
    Server-->>SRClient: Connection closed
    deactivate Blazor
```

### Connection Lifecycle:

```mermaid
stateDiagram-v2
    [*] --> Disconnected: Page Load
    Disconnected --> Connecting: InitializeSignalR()
    Connecting --> Connected: Handshake Success
    Connecting --> Failed: Connection Error
    Connected --> Reconnecting: Network Loss
    Reconnecting --> Connected: Reconnect Success
    Reconnecting --> Disconnected: Max Retries Exceeded
    Connected --> Disconnected: Manual Disconnect
    Failed --> [*]
    Disconnected --> [*]: Component Disposed
```

### Configuration:

```csharp
// Program.cs - SignalR configuration
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);  // Ping clients every 15s
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);  // Disconnect if no response in 30s
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);  // Handshake timeout
    options.MaximumReceiveMessageSize = 32 * 1024;  // 32KB message limit
});
```

### Event Types:

| Event Name | Payload | Triggered By | Purpose |
|------------|---------|--------------|---------|
| `SessionStateChanged` | `{sessionId, status}` | Session start/end | Update session status badge |
| `ParticipantJoined` | `{participantId, name, email, joinedAt}` | Participant joins | Increment participant count |
| `ActivityStateChanged` | `{activityId, status, openedAt?, closedAt?}` | Activity open/close | Update activity cards |
| `ResponseReceived` | `{activityId, participantId, responseData}` | Participant submits | Refresh dashboard data |

---

## 6. SignalR Connection - Activity Type Dashboards

### Overview
This flow shows SignalR connections for real-time activity dashboards (Poll, Word Cloud, Quadrant Matrix).

```mermaid
sequenceDiagram
    participant F as Facilitator Browser
    participant Dashboard as Activity Dashboard<br/>(Blazor Component)
    participant SRClient as SignalR Client
    participant Hub as WorkshopHub
    participant API as SessionsController<br/>/api/sessions
    participant DashSvc as Dashboard Service<br/>(Poll/WordCloud/etc)
    participant DB as Database

    Note over F,DB: Activity Dashboard Real-Time Updates

    rect rgb(200, 220, 255)
        Note over F,DB: Initial Load
        
        F->>Dashboard: Navigate to<br/>/facilitator/dashboards/{activityType}?code={code}&activityId={id}
        activate Dashboard
        
        Dashboard->>API: GET /api/sessions/{code}/activities/{activityId}/dashboard
        activate API
        API->>DashSvc: GetDashboardDataAsync(activityId)
        activate DashSvc
        
        alt Poll Dashboard
            DashSvc->>DB: Get poll responses grouped by option
            DB-->>DashSvc: {optionA: 10, optionB: 15, optionC: 5}
        else Word Cloud Dashboard
            DashSvc->>DB: Get word frequency counts
            DB-->>DashSvc: [{word: "innovation", count: 8}, ...]
        else Quadrant Matrix Dashboard
            DashSvc->>DB: Get quadrant placements
            DB-->>DashSvc: [{quadrant: "urgent-important", count: 12}, ...]
        end
        
        DashSvc-->>API: DashboardResponse
        deactivate DashSvc
        API-->>Dashboard: 200 OK + Dashboard Data
        deactivate API
        
        Dashboard->>Dashboard: Render initial data<br/>(Chart.js, D3, etc)
    end

    rect rgb(220, 255, 220)
        Note over F,DB: SignalR Connection Setup
        
        Dashboard->>SRClient: new HubConnectionBuilder()<br/>.WithUrl("/hubs/workshop")<br/>.WithAutomaticReconnect()<br/>.Build()
        
        Dashboard->>SRClient: hubConnection.On<ResponseReceivedEvent><br/>("ResponseReceived", handler)
        
        Note over Dashboard,SRClient: Handler function
        SRClient->>SRClient: async (responseEvent) => {<br/>  if (responseEvent.ActivityId == activityId) {<br/>    await RefreshDashboard()<br/>  }<br/>}
        
        SRClient->>Hub: Connect + Subscribe(sessionCode)
        Hub-->>SRClient: Connected to group
    end

    rect rgb(255, 220, 220)
        Note over F,DB: Real-Time Update Flow
        
        Note over DB: Participant submits response
        
        DB->>Hub: Trigger ResponseReceived event
        activate Hub
        Hub->>Hub: Build ResponseReceivedEvent<br/>{sessionCode, activityId,<br/>participantId, responseData}
        
        Hub->>SRClient: Clients.Group(sessionCode)<br/>.ResponseReceived(event)
        deactivate Hub
        
        SRClient->>Dashboard: Invoke event handler<br/>ResponseReceived(event)
        activate Dashboard
        
        Dashboard->>Dashboard: Check if event.ActivityId<br/>matches current activityId
        
        alt Event matches current activity
            Dashboard->>API: GET /api/sessions/{code}/activities/{activityId}/dashboard
            activate API
            API->>DashSvc: GetDashboardDataAsync(activityId)
            DashSvc->>DB: Query updated data
            DB-->>DashSvc: Fresh aggregated data
            DashSvc-->>API: Updated DashboardResponse
            API-->>Dashboard: 200 OK + Updated Data
            deactivate API
            
            Dashboard->>Dashboard: Update chart/visualization<br/>(animate transition)
            Dashboard->>Dashboard: StateHasChanged()
            Dashboard-->>F: Re-render dashboard<br/>(smooth animation)
        else Event is for different activity
            Dashboard->>Dashboard: Ignore event
        end
        
        deactivate Dashboard
    end

    Note over F: Facilitator sees real-time updates as participants respond

    rect rgb(255, 255, 200)
        Note over F,DB: Activity Type Specific Updates
        
        alt Poll Dashboard
            Note over Dashboard: Update bar chart heights
            Dashboard->>Dashboard: chart.data.datasets[0].data = updatedCounts<br/>chart.update('show')
        else Word Cloud Dashboard
            Note over Dashboard: Resize words based on frequency
            Dashboard->>Dashboard: wordCloud.update(newWordFrequencies)
        else Quadrant Matrix Dashboard
            Note over Dashboard: Update quadrant counts
            Dashboard->>Dashboard: quadrants.forEach(q => q.count = newCount)
        end
    end

    rect rgb(240, 240, 255)
        Note over F,DB: Cleanup on Navigation
        
        F->>Dashboard: Navigate away
        activate Dashboard
        Dashboard->>Dashboard: DisposeAsync()
        Dashboard->>SRClient: hubConnection.InvokeAsync(<br/>"Unsubscribe", sessionCode)
        Dashboard->>SRClient: hubConnection.DisposeAsync()
        SRClient->>Hub: Unsubscribe + Disconnect
        deactivate Dashboard
    end
```

### Dashboard-Specific Logic:

#### Poll Dashboard
```csharp
// Real-time update handler
hubConnection.On<ResponseReceivedEvent>("ResponseReceived", async (evt) =>
{
    if (evt.ActivityId == activityId)
    {
        var updatedData = await ApiService.GetPollDashboardAsync(sessionCode, activityId);
        
        // Update Chart.js data
        pollChart.data.datasets[0].data = updatedData.OptionCounts;
        pollChart.update('show');  // Animate transition
        
        await InvokeAsync(StateHasChanged);
    }
});
```

#### Word Cloud Dashboard
```csharp
// Real-time update handler
hubConnection.On<ResponseReceivedEvent>("ResponseReceived", async (evt) =>
{
    if (evt.ActivityId == activityId)
    {
        var updatedData = await ApiService.GetWordCloudDashboardAsync(sessionCode, activityId);
        
        // Update word cloud visualization
        await JS.InvokeVoidAsync("updateWordCloud", updatedData.WordFrequencies);
        
        await InvokeAsync(StateHasChanged);
    }
});
```

### Performance Optimization:

1. **Debouncing**: If multiple responses arrive rapidly, debounce dashboard refreshes
```csharp
private CancellationTokenSource _debounceTokenSource;

private async Task RefreshDashboardDebounced()
{
    _debounceTokenSource?.Cancel();
    _debounceTokenSource = new CancellationTokenSource();
    
    try
    {
        await Task.Delay(500, _debounceTokenSource.Token);  // 500ms debounce
        await RefreshDashboard();
    }
    catch (TaskCanceledException) { }
}
```

2. **Incremental Updates**: Instead of fetching all data, fetch only changed data
```csharp
// Instead of full refresh
var allData = await GetAllDashboardData();

// Incremental update
var deltaData = await GetDashboardDataSince(lastUpdateTimestamp);
MergeIntoExistingData(deltaData);
```

---

## 7. SignalR Connection - Participant Submission

### Overview
This flow shows how participant responses are submitted and broadcast via SignalR.

```mermaid
sequenceDiagram
    participant P as Participant Browser
    participant Activity as Activity Component<br/>(Blazor/MVC)
    participant API as SessionsController<br/>/api/sessions
    participant RS as ResponseService
    participant RR as ResponseRepository
    participant CC as ContributionCounter<br/>Repository
    participant DB as Database
    participant Hub as WorkshopHub
    participant F as Facilitator Dashboard

    Note over P,F: Participant Response Submission Flow

    rect rgb(200, 220, 255)
        Note over P,Activity: Initial Activity Load
        
        P->>Activity: Navigate to activity page<br/>?code={code}&participantId={id}
        activate Activity
        
        Activity->>API: GET /api/sessions/{code}/activities/current
        activate API
        API->>DB: Get current open activity
        DB-->>API: Activity (Poll/WordCloud/etc)
        API-->>Activity: ActivityResponse
        deactivate API
        
        alt No Activity Open
            Activity-->>P: "No activity is currently active"
        end
        
        Activity->>Activity: Render activity UI<br/>(based on activity type)
        Activity-->>P: Show activity interface
        deactivate Activity
    end

    rect rgb(220, 255, 220)
        Note over P,F: Response Submission
        
        P->>P: Interact with activity<br/>(Select poll option,<br/>enter words, place quadrant)
        P->>P: Click "Submit" button
        P->>P: Disable submit button<br/>(prevent double submit)
        
        P->>API: POST /api/sessions/{code}/responses<br/>SubmitResponseRequest {<br/>  activityId,<br/>  participantId,<br/>  responseData<br/>}
        
        activate API
        API->>API: Validate request<br/>(activityId, participantId, data)
        
        alt Validation Fails
            API-->>P: 400 Bad Request<br/>{error: "Invalid response data"}
            P->>P: Re-enable submit button<br/>Show error message
        end
        
        API->>RS: ValidateAndSubmitResponseAsync(<br/>activityId, participantId, responseData)
        activate RS
        
        RS->>DB: Check if activity is open
        DB-->>RS: Activity.Status
        
        alt Activity Not Open
            RS-->>API: throw InvalidOperationException<br/>("Activity is not open")
            API-->>P: 400 Bad Request<br/>{error: "Activity closed"}
            P->>P: Show "Activity has closed" message
        end
        
        RS->>DB: Check if participant already responded
        DB-->>RS: Existing response count
        
        alt Already Responded (if activity doesn't allow multiple)
            RS-->>API: throw InvalidOperationException<br/>("Already submitted")
            API-->>P: 400 Bad Request<br/>{error: "Response already submitted"}
            P->>P: Show "Already submitted" message
        end
        
        RS->>RS: Generate Response ID<br/>Guid.NewGuid()
        RS->>RS: Create Response entity<br/>new Response(id, activityId,<br/>participantId, responseData, submittedAt)
        
        RS->>RR: Repository.AddAsync(response)
        activate RR
        RR->>DB: BEGIN TRANSACTION
        RR->>DB: INSERT INTO Responses<br/>(Id, ActivityId, ParticipantId,<br/>ResponseData, SubmittedAt)
        DB-->>RR: Success
        deactivate RR
        
        Note over RS,DB: Update contribution counter
        RS->>CC: IncrementAsync(participantId, activityId)
        activate CC
        CC->>DB: INSERT OR UPDATE<br/>ContributionCounters<br/>SET Count = Count + 1
        DB-->>CC: Success
        deactivate CC
        
        RS->>DB: COMMIT TRANSACTION
        
        RS-->>API: Response entity
        deactivate RS
        
        API->>API: Map to ResponseResponse
    end

    rect rgb(255, 220, 220)
        Note over API,F: SignalR Broadcast
        
        API->>Hub: Build ResponseReceivedEvent<br/>{sessionCode, activityId,<br/>participantId, responseData}
        activate Hub
        
        Hub->>Hub: Clients.Group(sessionCode)<br/>.ResponseReceived(event)
        
        Hub->>F: Send: ResponseReceived<br/>(to facilitator dashboard)
        activate F
        F->>F: Update dashboard in real-time<br/>(refresh chart/data)
        deactivate F
        
        Note over Hub: Event also sent to other participants<br/>(if activity shows peer responses)
        
        deactivate Hub
        
        API-->>P: 200 OK<br/>{responseId, submittedAt}
        deactivate API
    end

    rect rgb(255, 255, 200)
        Note over P: Post-Submission UI Update
        
        P->>P: Show success message<br/>"Response submitted!"
        P->>P: Disable activity controls<br/>(prevent resubmission)
        
        alt Activity allows viewing results
            P->>P: Show "View Results" button
            P->>API: GET /api/sessions/{code}/activities/{id}/results
            API->>DB: Get aggregated results
            DB-->>API: Result summary
            API-->>P: Display results to participant
        else Activity hides results
            P->>P: Show "Thank you" message<br/>Wait for next activity
        end
    end

    Note over P,F: Response successfully recorded and broadcast
```

### Response Data Formats:

#### Poll Response
```json
{
  "activityId": "uuid",
  "participantId": "uuid",
  "responseData": {
    "selectedOptionId": "option-a"
  }
}
```

#### Word Cloud Response
```json
{
  "activityId": "uuid",
  "participantId": "uuid",
  "responseData": {
    "words": ["innovation", "teamwork", "agility"]
  }
}
```

#### Quadrant Matrix Response
```json
{
  "activityId": "uuid",
  "participantId": "uuid",
  "responseData": {
    "quadrant": "urgent-important",
    "item": "Improve CI/CD pipeline"
  }
}
```

### Duplicate Submission Prevention:

```csharp
// Client-side (JavaScript/Blazor)
let isSubmitting = false;

async function submitResponse() {
    if (isSubmitting) return;
    isSubmitting = true;
    
    try {
        await fetch('/api/sessions/{code}/responses', {
            method: 'POST',
            body: JSON.stringify(data)
        });
    } finally {
        isSubmitting = false;
    }
}

// Server-side (C#)
public async Task<ActionResult> SubmitResponse(SubmitResponseRequest request)
{
    var existingResponse = await _responses.GetByParticipantAndActivityAsync(
        request.ParticipantId, request.ActivityId);
    
    if (existingResponse != null && !activity.AllowMultipleResponses)
    {
        return BadRequest(new { error = "Response already submitted" });
    }
    
    // Process submission...
}
```

---

## 8. Login and Session Management Flow

### Overview
Comprehensive flow showing OTP-based authentication and session management for facilitators.

```mermaid
sequenceDiagram
    participant F as Facilitator Browser
    participant AC as AccountController<br/>/account
    participant Auth as AuthenticationService
    participant FUR as FacilitatorUser<br/>Repository
    participant OTP as LoginOtp<br/>Repository
    participant Email as EmailService<br/>(SMTP/Console)
    participant DB as Database
    participant Cookie as Cookie Auth<br/>Middleware
    participant Token as FacilitatorToken<br/>Service (Memory Cache)

    Note over F,Token: Facilitator Login Flow (OTP-based)

    rect rgb(200, 220, 255)
        Note over F,Email: Step 1: Request OTP
        
        F->>AC: GET /account/login
        AC-->>F: Login.cshtml (Email form)
        
        F->>F: Enter email address<br/>& optional display name
        F->>AC: POST /account/login<br/>{email, displayName}
        activate AC
        
        AC->>AC: Validate email format
        
        alt Invalid Email
            AC-->>F: Show validation error
        end
        
        AC->>Auth: SendLoginOtpAsync(email, displayName)
        activate Auth
        
        Auth->>Auth: Normalize email<br/>email.ToLowerInvariant()
        
        Note over Auth,DB: Rate Limiting Check
        Auth->>OTP: GetRecentOtpsForEmailAsync(email, limit: 5)
        OTP->>DB: SELECT * FROM LoginOtps<br/>WHERE Email = @email<br/>ORDER BY CreatedAt DESC<br/>LIMIT 5
        DB-->>OTP: Recent OTPs
        OTP-->>Auth: List<LoginOtp>
        
        Auth->>Auth: Count OTPs in last hour
        
        alt Rate Limit Exceeded (>= 5 in 1 hour)
            Auth-->>AC: SendOtpResult<br/>{success: false,<br/>message: "Too many requests"}
            AC-->>F: Error: "Too many OTP requests.<br/>Try again later."
        end
        
        Note over Auth,DB: Generate OTP
        Auth->>Auth: GenerateOtpCode()<br/>(6-digit random number)
        Auth->>Auth: Create LoginOtp entity<br/>new LoginOtp(id, email, code,<br/>createdAt, expiresAt: now + 10min)
        
        Auth->>OTP: Repository.AddAsync(otp)
        OTP->>DB: INSERT INTO LoginOtps<br/>(Id, Email, OtpCode,<br/>CreatedAt, ExpiresAt)
        DB-->>OTP: Success
        
        Note over Auth,Email: Send OTP Email
        Auth->>FUR: GetByEmailAsync(email)
        FUR->>DB: SELECT * FROM FacilitatorUsers<br/>WHERE Email = @email
        DB-->>FUR: User (if exists)
        FUR-->>Auth: FacilitatorUser?
        
        Auth->>Auth: Determine display name<br/>userName = existingUser?.DisplayName<br/>         ?? displayName<br/>         ?? "there"
        
        Auth->>Email: SendLoginOtpAsync(<br/>email, otpCode, userName)
        activate Email
        
        alt SMTP Provider
            Email->>Email: Build HTML email from template
            Email->>Email: Connect to SMTP server
            Email->>Email: Send via SMTP
            Email-->>Auth: Success
        else Console Provider (Dev)
            Email->>Email: Console.WriteLine(<br/>"OTP for {email}: {otpCode}")
            Email-->>Auth: Success
        end
        deactivate Email
        
        Auth-->>AC: SendOtpResult<br/>{success: true,<br/>message: "OTP sent"}
        deactivate Auth
        
        AC->>AC: Store email in TempData
        AC-->>F: Redirect to /account/verify-otp
        deactivate AC
        
        F-->>F: Show OTP verification form
    end

    rect rgb(220, 255, 220)
        Note over F,DB: Step 2: Verify OTP
        
        F->>AC: GET /account/verify-otp
        AC-->>F: VerifyOtp.cshtml (OTP input)
        
        F->>F: Enter 6-digit OTP code<br/>(received via email)
        F->>AC: POST /account/verify-otp<br/>{email, otpCode}
        activate AC
        
        AC->>Auth: VerifyOtpAsync(email, otpCode)
        activate Auth
        
        Auth->>Auth: Normalize inputs<br/>email.ToLowerInvariant()<br/>otpCode.Trim()
        
        Auth->>OTP: GetValidOtpAsync(email, otpCode)
        activate OTP
        OTP->>DB: SELECT * FROM LoginOtps<br/>WHERE Email = @email<br/>  AND OtpCode = @code<br/>  AND ExpiresAt > @now<br/>LIMIT 1
        DB-->>OTP: LoginOtp (if valid)
        OTP-->>Auth: LoginOtp?
        deactivate OTP
        
        alt OTP Not Found or Expired
            Auth-->>AC: VerifyOtpResult<br/>{success: false,<br/>errorMessage: "Invalid or expired OTP"}
            AC-->>F: Error: "Invalid OTP code"
        end
        
        Note over Auth,DB: Get or Create User
        Auth->>FUR: GetByEmailAsync(email)
        FUR->>DB: SELECT * FROM FacilitatorUsers<br/>WHERE Email = @email
        DB-->>FUR: User (if exists)
        FUR-->>Auth: FacilitatorUser?
        
        alt User Doesn't Exist
            Auth->>Auth: Create new user<br/>new FacilitatorUser(<br/>  id: Guid.NewGuid(),<br/>  email: email,<br/>  displayName: displayName,<br/>  createdAt: now)
            
            Auth->>FUR: Repository.AddAsync(user)
            FUR->>DB: INSERT INTO FacilitatorUsers<br/>(Id, Email, DisplayName,<br/>CreatedAt)
            DB-->>FUR: Success
        end
        
        Auth->>Auth: Mark OTP as used<br/>(delete or flag)
        Auth->>OTP: DeleteAsync(otp)
        OTP->>DB: DELETE FROM LoginOtps<br/>WHERE Id = @id
        
        Auth-->>AC: VerifyOtpResult<br/>{success: true,<br/>user: FacilitatorUser}
        deactivate Auth
    end

    rect rgb(255, 220, 220)
        Note over AC,Cookie: Step 3: Create Authentication Session
        
        AC->>AC: Build Claims<br/>claims = [<br/>  {ClaimTypes.NameIdentifier, userId},<br/>  {ClaimTypes.Email, email},<br/>  {ClaimTypes.Name, displayName},<br/>  {"FacilitatorRole", "Facilitator"}<br/>]
        
        AC->>AC: Create ClaimsIdentity<br/>identity = new ClaimsIdentity(<br/>  claims,<br/>  CookieAuthenticationDefaults.AuthenticationScheme)
        
        AC->>AC: Create ClaimsPrincipal<br/>principal = new ClaimsPrincipal(identity)
        
        AC->>Cookie: SignInAsync(<br/>  principal,<br/>  authProperties: {<br/>    IsPersistent: true,<br/>    ExpiresUtc: now + 30 days<br/>  })
        
        activate Cookie
        Cookie->>Cookie: Encrypt authentication ticket
        Cookie->>Cookie: Create auth cookie<br/>"TechWayFit.Pulse.Auth"
        Cookie-->>F: Set-Cookie: TechWayFit.Pulse.Auth=<encrypted>;<br/>HttpOnly; Secure; SameSite=Lax;<br/>Expires=...
        deactivate Cookie
        
        AC-->>F: Redirect to /facilitator/dashboard
        deactivate AC
    end

    rect rgb(255, 255, 200)
        Note over F,Token: Step 4: Generate Facilitator Token
        
        F->>F: Navigate to /facilitator/dashboard
        F->>Cookie: Send request with auth cookie
        
        Cookie->>Cookie: Validate cookie
        Cookie->>Cookie: Decrypt authentication ticket
        Cookie->>Cookie: Populate HttpContext.User<br/>with claims
        
        Note over Cookie,Token: FacilitatorTokenMiddleware executes
        
        Cookie->>Token: Middleware: GetOrCreateTokenAsync(userId)
        activate Token
        
        Token->>Token: Check memory cache<br/>cacheKey = "facilitator_token_{userId}"
        
        alt Token Exists in Cache
            Token-->>Cookie: Return existing token
        else Token Not in Cache
            Token->>Token: GenerateSecureToken()<br/>(32 random bytes, Base64)
            Token->>Token: Store in cache<br/>_cache.Set(cacheKey, token,<br/>  expiration: now + 6 hours)
            Token->>Token: Store reverse lookup<br/>_cache.Set("token_user_{token}", userId)
            Token-->>Cookie: Return new token
        end
        deactivate Token
        
        Cookie->>Cookie: Store token in HttpContext.Items<br/>context.Items["FacilitatorToken"] = token
        
        Cookie-->>F: Render dashboard page<br/>(user authenticated)
    end

    rect rgb(240, 240, 255)
        Note over F,Token: Step 5: Subsequent Requests
        
        F->>AC: Any facilitator request<br/>(e.g., create session)
        
        AC->>Cookie: Validate auth cookie
        Cookie->>Cookie: Check cookie validity
        
        alt Cookie Valid
            Cookie->>Cookie: Populate User claims
            Cookie->>Token: Get/Create token (via middleware)
            Token-->>Cookie: Token
            Cookie-->>AC: Authenticated request<br/>+ HttpContext.User populated
            AC->>AC: Process request with<br/>authenticated user context
        else Cookie Invalid/Expired
            Cookie-->>F: Redirect to /account/login<br/>(LoginPath configured)
        end
    end

    rect rgb(255, 240, 240)
        Note over F,Cookie: Step 6: Logout
        
        F->>AC: POST /account/logout
        activate AC
        
        AC->>Cookie: SignOutAsync(<br/>  CookieAuthenticationDefaults.AuthenticationScheme)
        activate Cookie
        Cookie->>Cookie: Delete auth cookie
        Cookie-->>F: Set-Cookie: TechWayFit.Pulse.Auth=;<br/>Expires=(past date)
        deactivate Cookie
        
        AC->>Token: RevokeTokenAsync(userId)
        activate Token
        Token->>Token: Remove from cache<br/>_cache.Remove("facilitator_token_{userId}")<br/>_cache.Remove("token_user_{token}")
        deactivate Token
        
        AC-->>F: Redirect to /
        deactivate AC
        
        F->>F: Show logged out state
    end

    Note over F,Token: Authentication complete
```

### Authentication State Diagram:

```mermaid
stateDiagram-v2
    [*] --> Anonymous: Initial State
    Anonymous --> OtpRequested: Enter Email
    OtpRequested --> OtpSent: OTP Generated & Sent
    OtpSent --> Authenticated: Valid OTP Verified
    OtpSent --> OtpRequested: Invalid OTP (Retry)
    OtpSent --> RateLimited: Too Many Attempts
    Authenticated --> TokenGenerated: Middleware Creates Token
    TokenGenerated --> ActiveSession: User Accesses Pages
    ActiveSession --> ActiveSession: Subsequent Requests
    ActiveSession --> Anonymous: Logout / Cookie Expired
    RateLimited --> [*]: Wait 1 Hour
    Authenticated --> [*]: Session Timeout (30 days)
```

### Security Measures:

1. **OTP Security**
   - 6-digit random code
   - 10-minute expiration
   - Single-use (deleted after verification)
   - Rate limiting (5 attempts per hour)

2. **Cookie Security**
   ```csharp
   options.Cookie.HttpOnly = true;        // Prevent XSS access
   options.Cookie.SecurePolicy = CookieSecurePolicy.Always;  // HTTPS only
   options.Cookie.SameSite = SameSiteMode.Lax;  // CSRF protection
   options.ExpireTimeSpan = TimeSpan.FromDays(30);  // Long-lived
   options.SlidingExpiration = true;  // Renew on activity
   ```

3. **Token Security**
   - 32-byte random tokens (256-bit)
   - 6-hour expiration
   - Stored in memory (not persisted to disk)
   - Revoked on logout

4. **Data Protection**
   - File-based key storage for cookie encryption
   - Keys protected with Data Protection API

### Session Flow Summary:

| Step | Action | Duration | Storage |
|------|--------|----------|---------|
| 1 | Request OTP | - | Temporary (TempData) |
| 2 | Verify OTP | 10 min expiry | Database (LoginOtps) |
| 3 | Create Auth Session | 30 days | Encrypted Cookie |
| 4 | Generate Facilitator Token | 6 hours | Memory Cache |
| 5 | Authenticated Requests | Session lifetime | Cookie + Cache |
| 6 | Logout | - | Remove Cookie + Cache |

---

## Summary

These process flow diagrams provide a comprehensive view of the TechWayFit Pulse application's key workflows:

1. **Create Activity**: Shows the complete flow from UI interaction to database persistence and real-time broadcasting
2. **Start Session**: Details the state transition and participant enablement process
3. **Participant Join**: Demonstrates the join form submission and token generation
4. **Control Session**: Illustrates how facilitators manage activity states in real-time
5. **Facilitator SignalR**: Details the connection lifecycle for the live dashboard
6. **Dashboard SignalR**: Shows real-time updates for activity-specific visualizations
7. **Participant Submission**: Complete flow from response submission to broadcast
8. **Login & Session**: Comprehensive OTP-based authentication and session management

Each diagram includes:
- Detailed sequence flows
- Error handling paths
- State transitions
- Security considerations
- Performance optimizations

These diagrams serve as both documentation and architectural reference for development, testing, and scaling the platform.

---

**Document Created:** January 17, 2026  
**Last Updated:** January 17, 2026  
**Maintained By:** Solution Architecture Team
