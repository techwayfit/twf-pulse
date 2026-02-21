# Technical Process Flows — TechWayFit Pulse

> Platform: .NET 8 Blazor Server with SignalR | Date: January 2026

All diagrams use Mermaid syntax and render in VS Code (Markdown Preview) or GitHub.

---

## Table of Contents

1. [Create Activity Flow](#1-create-activity-flow)
2. [Facilitator Start Session Flow](#2-facilitator-start-session-flow)
3. [Participant Join Session Flow](#3-participant-join-session-flow)
4. [Facilitator Control Session Flow](#4-facilitator-control-session-flow)
5. [Activity State Diagram](#5-activity-state-diagram)
6. [SignalR Connection — Facilitator Live Page](#6-signalr-connection--facilitator-live-page)
7. [SignalR Connection — Activity Dashboards](#7-signalr-connection--activity-dashboards)
8. [Participant Response Submission Flow](#8-participant-response-submission-flow)
9. [Login and OTP Authentication Flow](#9-login-and-otp-authentication-flow)
10. [AI Session Generation Flow](#10-ai-session-generation-flow)
11. [SignalR Connection State Machine](#11-signalr-connection-state-machine)

---

## 1. Create Activity Flow

```mermaid
sequenceDiagram
    participant F as Facilitator UI (Blazor/MVC)
    participant AC as SessionsController /api/sessions
    participant AS as ActivityService (Application)
    participant AR as ActivityRepository (Infrastructure)
    participant DB as SQLite Database
    participant Hub as SignalR Hub (WorkshopHub)
    participant P as Participants (SignalR Clients)

    F->>F: Fill activity form (Title, Type, Prompt, Config)
    F->>AC: POST /api/sessions/{code}/activities

    activate AC
    AC->>AC: Validate Request (Title, Type, Config)

    alt Validation Fails
        AC-->>F: 400 Bad Request {error: validation_error}
    end

    AC->>AS: GetSessionByCodeAsync(code)
    AS->>DB: SELECT * FROM Sessions WHERE Code = @code
    DB-->>AS: SessionRecord
    AS-->>AC: Session

    alt Session Not Found
        AC-->>F: 404 Not Found
    end

    AC->>AS: CreateActivityAsync(sessionId, type, title, prompt, config)
    activate AS
    AS->>AS: Generate Activity ID, determine Order (Max + 1)
    AS->>AR: Repository.AddAsync(activity)
    AR->>DB: INSERT INTO Activities (Id, SessionId, Order, Type, Title, Prompt, Config, Status)
    DB-->>AR: Success
    AR-->>AS: void
    AS-->>AC: Activity Entity
    deactivate AS

    AC->>Hub: Clients.Group(sessionCode).ActivityStateChanged(event)
    Hub->>P: Broadcast: ActivityStateChanged {activityId, status: "Draft"}

    AC-->>F: 200 OK ActivityResponse
    deactivate AC
```

**Business Rules**:
- Activity order auto-assigned (max existing order + 1)
- New activities default to `Draft` status
- Only the session owner can create activities
- Validates title (required, max 200 chars) and type-specific config

---

## 2. Facilitator Start Session Flow

```mermaid
sequenceDiagram
    participant F as Facilitator UI (Live.razor)
    participant AC as SessionsController
    participant SS as SessionService
    participant SR as SessionRepository
    participant DB as SQLite Database
    participant Hub as SignalR Hub
    participant P as Participant Clients

    F->>F: Click "Start Session" — disable button
    F->>AC: PUT /api/sessions/{code}/start
    activate AC

    AC->>SS: GetByCodeAsync(code)
    SS->>DB: SELECT * FROM Sessions WHERE Code = @code
    DB-->>SS: Session
    SS-->>AC: Session

    alt Session Not Found
        AC-->>F: 404 Not Found
    end
    alt Not Owned by Facilitator
        AC-->>F: 403 Forbidden
    end
    alt Already Live or Ended
        AC-->>F: 400 Bad Request {error: Invalid status transition}
    end

    AC->>SS: SetStatusAsync(sessionId, Live, now)
    SS->>SR: UPDATE Sessions SET Status='Live', UpdatedAt=@now WHERE Id=@id
    DB-->>SR: Success

    AC->>Hub: Clients.Group(sessionCode).SessionStateChanged(event)
    Hub->>F: SessionStateChanged {status: "Live"}
    Hub->>P: SessionStateChanged {status: "Live"}

    AC-->>F: 200 OK {success: true}
    deactivate AC
    F->>F: Update UI, change button to "End Session"
    P->>P: Enable join button
```

**State transition**: `Draft → Live`

---

## 3. Participant Join Session Flow

```mermaid
sequenceDiagram
    participant P as Participant Browser
    participant JC as ParticipantController /participant/join
    participant AC as SessionsController
    participant PS as ParticipantService
    participant DB as SQLite Database
    participant Hub as SignalR Hub
    participant F as Facilitator Dashboard

    P->>JC: GET /participant/join?code={code}
    JC->>AC: GET /api/sessions/{code}
    AC->>DB: SELECT * FROM Sessions WHERE Code = @code
    DB-->>AC: Session
    AC-->>JC: SessionSummaryResponse

    alt Session Not Live
        JC-->>P: Error: Session not accepting participants
    end

    JC-->>P: Join.cshtml (with dynamic join form)
    P->>P: Fill Name, Email, and custom fields
    P->>AC: POST /api/sessions/{code}/participants

    activate AC
    AC->>AC: Validate required fields + email format

    alt Validation Fails
        AC-->>P: 400 Bad Request {validation errors}
    end

    AC->>PS: JoinSessionAsync(sessionId, name, email, formData)
    activate PS
    PS->>PS: Generate Participant ID (Guid.NewGuid())
    PS->>DB: INSERT INTO Participants (Id, SessionId, Name, Email, FormData, JoinedAt)
    DB-->>PS: Success
    PS-->>AC: ParticipantResponse
    deactivate PS

    AC->>Hub: Clients.Group(sessionCode).ParticipantJoined(event)
    Hub->>F: ParticipantJoined {participantId, name, joinedAt}

    AC-->>P: 200 OK {participantId, token, sessionCode}
    deactivate AC

    P->>P: Store token in localStorage
    P->>P: Redirect to /participant/activity?code={code}
    F->>F: Update participant count in real-time
```

---

## 4. Facilitator Control Session Flow

### Open / Close / Switch Activity

```mermaid
sequenceDiagram
    participant F as Facilitator UI (Live.razor)
    participant AC as SessionsController
    participant AS as ActivityService
    participant DB as SQLite Database
    participant Hub as SignalR Hub
    participant P as Participants

    rect rgb(200,220,255)
        Note over F,P: 1. Open Activity
        F->>AC: POST /api/sessions/{code}/activities/{activityId}/open
        AC->>AS: OpenActivityAsync(activityId, now)
        AS->>DB: UPDATE Activities SET Status='Open', OpenedAt=@now
        AS->>DB: UPDATE Sessions SET CurrentActivityId=@activityId
        AC->>Hub: ActivityStateChanged {status: "Open"}
        Hub->>F: ActivityStateChanged
        Hub->>P: ActivityStateChanged
        P->>P: Enable activity UI
    end

    rect rgb(255,220,200)
        Note over F,P: 2. Close Activity
        F->>AC: POST /api/sessions/{code}/activities/{activityId}/close
        AC->>AS: CloseActivityAsync(activityId, now)
        AS->>DB: UPDATE Activities SET Status='Closed', ClosedAt=@now
        AS->>DB: UPDATE Sessions SET CurrentActivityId=NULL
        AC->>Hub: ActivityStateChanged {status: "Closed"}
        Hub->>F: ActivityStateChanged
        Hub->>P: ActivityStateChanged — show "Activity Closed"
    end

    rect rgb(220,255,200)
        Note over F,P: 3. Switch Activity (auto-close previous)
        F->>AC: POST /api/sessions/{code}/activities/{activityBId}/open
        AC->>AS: OpenActivityAsync(activityBId)
        AS->>DB: UPDATE Activities SET Status='Closed' WHERE Id=@previousId
        Hub->>P: ActivityStateChanged {previousId, "Closed"}
        AS->>DB: UPDATE Activities SET Status='Open' WHERE Id=@activityBId
        Hub->>P: ActivityStateChanged {activityBId, "Open"}
    end
```

---

## 5. Activity State Diagram

```mermaid
stateDiagram-v2
    [*] --> Draft: Activity Created
    Draft --> Open: Facilitator Opens
    Open --> Closed: Facilitator Closes
    Closed --> Open: Facilitator Re-opens
    Open --> Open: Switch to Different Activity (auto-close previous)
    Closed --> [*]
```

**Business Rules**:
- Only one activity can be `Open` at a time per session
- Opening a new activity automatically closes the currently open one
- Closed activities can be re-opened
- Only the session owner controls activity state

---

## 6. SignalR Connection — Facilitator Live Page

```mermaid
sequenceDiagram
    participant Browser as Facilitator Browser
    participant Blazor as Live.razor
    participant SRClient as SignalR Client
    participant Hub as WorkshopHub
    participant Groups as SignalR Groups

    Browser->>Blazor: Navigate to /facilitator/live?code={code}
    Blazor->>Blazor: OnInitializedAsync() — load session data

    Blazor->>SRClient: new HubConnectionBuilder().WithUrl("/hubs/workshop").Build()
    SRClient->>SRClient: .WithAutomaticReconnect() (0s, 2s, 10s, 30s)

    Note over Blazor,SRClient: Register event handlers
    Blazor->>SRClient: On("SessionStateChanged", handler)
    Blazor->>SRClient: On("ParticipantJoined", handler)
    Blazor->>SRClient: On("ActivityStateChanged", handler)
    Blazor->>SRClient: On("ResponseReceived", handler)

    SRClient->>Hub: WebSocket Handshake
    Hub-->>SRClient: 101 Switching Protocols

    Blazor->>SRClient: InvokeAsync("Subscribe", sessionCode)
    Hub->>Groups: AddToGroupAsync(connectionId, "SESSION_{code}")

    Blazor-->>Browser: Render Live Dashboard

    rect rgb(240,240,240)
        Note over Browser,Groups: Event Reception
        Hub->>Groups: ActivityStateChanged event
        Groups->>SRClient: Send event
        SRClient->>Blazor: Invoke handler
        Blazor->>Blazor: Update state, StateHasChanged()
        Blazor-->>Browser: Re-render UI
    end

    rect rgb(255,240,240)
        Note over Browser,Groups: Reconnection
        SRClient-xHub: Connection lost
        SRClient->>Blazor: Connection: Reconnecting
        Browser->>Browser: Show "Reconnecting..." indicator
        SRClient->>Hub: Reconnect with exponential backoff
        Hub-->>SRClient: Reconnected
        Blazor->>SRClient: Re-subscribe to group
        Blazor->>Blazor: Refresh all data
    end

    Note over Browser: On dispose / navigate away
    Blazor->>SRClient: Unsubscribe(sessionCode)
    Blazor->>SRClient: DisposeAsync()
```

### SignalR Event Reference

| Event | Payload | Triggers |
|-------|---------|---------|
| `SessionStateChanged` | `{ sessionId, status }` | Session start / end |
| `ParticipantJoined` | `{ participantId, name, joinedAt }` | New participant joins |
| `ActivityStateChanged` | `{ activityId, status, openedAt?, closedAt? }` | Activity open / close |
| `ResponseReceived` | `{ activityId, participantId, responseData }` | Participant submits |
| `DashboardUpdated` | `{ activityId, aggregateType, payload }` | Aggregate recalculated or AI insight ready |

---

## 7. SignalR Connection — Activity Dashboards

```mermaid
sequenceDiagram
    participant F as Facilitator Browser
    participant Dashboard as Activity Dashboard (Blazor)
    participant SRClient as SignalR Client
    participant Hub as WorkshopHub
    participant API as SessionsController
    participant DashSvc as Dashboard Service
    participant DB as Database

    rect rgb(200,220,255)
        Note over F,DB: Initial Load
        F->>Dashboard: Navigate to dashboard?code={code}&activityId={id}
        Dashboard->>API: GET /api/sessions/{code}/activities/{id}/dashboard
        API->>DashSvc: GetDashboardDataAsync(activityId)
        DashSvc->>DB: Query aggregated data
        DB-->>DashSvc: Grouped results
        DashSvc-->>API: DashboardResponse
        API-->>Dashboard: 200 OK
        Dashboard->>Dashboard: Render initial chart/visualization
    end

    rect rgb(220,255,220)
        Note over Dashboard,Hub: Subscribe for live updates
        Dashboard->>SRClient: On("ResponseReceived", handler)
        SRClient->>Hub: Connect + Subscribe(sessionCode)
    end

    rect rgb(255,220,220)
        Note over F,DB: Real-Time Update on Response
        Hub->>SRClient: ResponseReceived {activityId}
        SRClient->>Dashboard: Invoke handler
        Dashboard->>Dashboard: Check activityId matches current
        Dashboard->>API: GET /api/sessions/{code}/activities/{id}/dashboard
        API->>DB: Query fresh aggregated data
        DB-->>API: Updated results
        API-->>Dashboard: Updated DashboardResponse
        Dashboard->>Dashboard: Animate chart update, StateHasChanged()
        Dashboard-->>F: Re-render with live data
    end
```

**Performance tip**: Debounce rapid updates (use 500ms delay) to avoid unnecessary API calls when many participants submit simultaneously.

---

## 8. Participant Response Submission Flow

```mermaid
sequenceDiagram
    participant P as Participant Browser
    participant API as SessionsController
    participant RS as ResponseService
    participant RR as ResponseRepository
    participant CC as ContributionCounter
    participant DB as Database
    participant Hub as WorkshopHub
    participant F as Facilitator Dashboard

    P->>API: GET /api/sessions/{code}/activities/current
    API->>DB: Get current open activity
    DB-->>API: Activity
    API-->>P: Show activity UI

    P->>P: Interact with activity (select option, enter text, place quadrant point)
    P->>P: Click Submit — disable button

    P->>API: POST /api/sessions/{code}/responses
    activate API
    API->>RS: ValidateAndSubmitResponseAsync(activityId, participantId, data)
    activate RS

    RS->>DB: Check activity is Open
    alt Activity Closed
        RS-->>API: throw InvalidOperationException
        API-->>P: 400 Activity closed
    end

    RS->>DB: Check contribution count against maxResponsesPerParticipant
    alt Already Submitted
        RS-->>API: throw InvalidOperationException
        API-->>P: 400 Already submitted
    end

    RS->>RR: INSERT INTO Responses (Id, ActivityId, ParticipantId, ResponseData, SubmittedAt)
    DB-->>RR: Success
    RS->>CC: INCREMENT ContributionCounters (participantId, activityId)
    DB-->>CC: Success
    RS-->>API: Response entity
    deactivate RS

    API->>Hub: Clients.Group(sessionCode).ResponseReceived(event)
    Hub->>F: ResponseReceived — trigger dashboard refresh

    API-->>P: 200 OK {responseId, submittedAt}
    deactivate API

    P->>P: Show "Response submitted!" — disable re-submission
```

---

## 9. Login and OTP Authentication Flow

```mermaid
sequenceDiagram
    participant F as Facilitator Browser
    participant AC as AccountController /account
    participant Auth as AuthenticationService
    participant OTP as LoginOtp Repository
    participant Email as EmailService (SMTP/Console)
    participant DB as Database
    participant Cookie as Cookie Auth Middleware

    rect rgb(200,220,255)
        Note over F,Email: Step 1 — Request OTP
        F->>AC: GET /account/login
        AC-->>F: Login.cshtml (email form)
        F->>AC: POST /account/login {email, displayName}
        AC->>Auth: SendLoginOtpAsync(email, displayName)

        Auth->>OTP: GetRecentOtpsForEmailAsync(email, limit: 5)
        OTP->>DB: SELECT * FROM LoginOtps WHERE Email=@email (last 1h)
        DB-->>OTP: Recent OTPs

        alt Rate Limit >= 5 in 1 hour
            Auth-->>AC: {success: false, rateLimited}
            AC-->>F: Error: Too many requests
        end

        Auth->>Auth: Generate 6-digit OTP + hash
        Auth->>OTP: INSERT INTO LoginOtps (Email, CodeHash, ExpiresAt)
        DB-->>OTP: Success

        Auth->>Email: SendOtpEmailAsync(email, displayName, code)
        Email-->>F: OTP code sent to email (or printed to console in dev)
        AC-->>F: Redirect to /account/verify-otp?email={email}
    end

    rect rgb(220,255,220)
        Note over F,Cookie: Step 2 — Verify OTP
        F->>AC: POST /account/verify-otp {email, code}
        AC->>Auth: VerifyOtpAsync(email, code)

        Auth->>OTP: GetValidOtpAsync(email)
        OTP->>DB: SELECT * FROM LoginOtps WHERE Email=@email AND ExpiresAt > now
        DB-->>OTP: Latest LoginOtp

        alt OTP Expired or Not Found
            Auth-->>AC: {success: false, invalid}
            AC-->>F: Error: Invalid or expired code
        end

        Auth->>Auth: VerifyHash(code, storedHash)

        alt Hash Mismatch
            Auth-->>AC: {success: false, invalid}
            AC-->>F: Error: Invalid code
        end

        Auth->>OTP: DELETE LoginOtp (mark as used)
        Auth->>Auth: Build ClaimsPrincipal (email, userId, displayName)
        Auth->>Cookie: SignInAsync("TechWayFit.Pulse.Auth", principal)

        Cookie-->>F: Set auth cookie (8h sliding expiry)
        AC-->>F: Redirect to /facilitator/dashboard
    end

    rect rgb(255,240,240)
        Note over F,Cookie: Subsequent Requests
        F->>AC: Any /facilitator/* request
        Cookie->>Cookie: Validate "TechWayFit.Pulse.Auth" cookie
        Cookie->>Cookie: Slide expiry (reset 8h window on activity)
        Cookie-->>F: Request authorized + FacilitatorContext injected
    end
```

**Cookie settings**: 8-hour sliding expiry; `HttpOnly`; `SameSite=Strict`.

---

## 10. AI Session Generation Flow

```mermaid
sequenceDiagram
    participant F as Facilitator Browser
    participant AC as SessionsController
    participant Queue as IAIWorkQueue
    participant BG as AIProcessingHostedService
    participant AI as AI Service (OpenAI/Intelligent/MLNet)
    participant PII as PiiSanitizer
    participant Hub as WorkshopHub

    rect rgb(200,220,255)
        Note over F,AI: Session Generation (synchronous path)
        F->>AC: POST /api/sessions/generate-with-ai {title, goal, type, contextDocs}
        AC->>PII: Sanitize context documents
        PII-->>AC: Sanitized docs (emails, phones, PII stripped)
        AC->>AI: GenerateSessionAsync(title, goal, type, sanitizedContext)
        AI-->>AC: GeneratedSession {activities[]}
        AC-->>F: 200 OK — populated wizard with 3-7 activities
    end

    rect rgb(220,255,220)
        Note over F,Hub: AI Analysis After Activity Close (async path)
        F->>AC: POST /api/sessions/{code}/activities/{id}/close
        AC->>AC: Close activity, update state
        AC->>Hub: ActivityStateChanged {status: "Closed"}
        AC->>Queue: Enqueue AI analysis work item (non-blocking)
        AC-->>F: 200 OK (immediate — AI runs in background)

        Queue->>BG: Dequeue work item
        BG->>AI: AnalyzeParticipantResponsesAsync(sessionId, activityId)
        BG->>AI: GenerateFacilitatorPromptAsync(sessionId, activityId)
        AI-->>BG: Analysis results + facilitator prompt
        BG->>Hub: DashboardUpdated {aggregateType: "AIInsight", payload: {analysis, prompt}}
        Hub->>F: AIInsight received — render insight panel
    end
```

**Key behaviors**:
- Session generation is synchronous (facilitator waits for result, typically < 5s)
- Post-activity AI analysis is async — HTTP response returns immediately, insights arrive via SignalR
- If AI is disabled or errors, mock services return instant stubs; no UX degradation

---

## 11. SignalR Connection State Machine

```mermaid
stateDiagram-v2
    [*] --> Disconnected: Page Load
    Disconnected --> Connecting: InitializeSignalR()
    Connecting --> Connected: Handshake Success
    Connecting --> Failed: Connection Error
    Connected --> Reconnecting: Network Loss
    Reconnecting --> Connected: Reconnect Success
    Reconnecting --> Disconnected: Max Retries Exceeded
    Connected --> Disconnected: Manual Disconnect / Component Dispose
    Failed --> [*]
    Disconnected --> [*]: Component Disposed
```

**Reconnection backoff**: 0s, 2s, 10s, 30s (`.WithAutomaticReconnect()`)

**On reconnect**: client re-subscribes to session group and refreshes all data from API.
