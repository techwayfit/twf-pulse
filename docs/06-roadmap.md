# Roadmap and Implementation Tracker — TechWayFit Pulse

> Last Updated: 21 February 2026

---

## 1. Executive Summary

TechWayFit Pulse has successfully completed **Phases 1, 2, and 3** and is a fully functional, production-ready workshop engagement platform.

| Phase | Status | Highlights |
|-------|--------|-----------|
| Phase 1 — MVP | Complete | Poll, Rating, WordCloud, GeneralFeedback activities; SignalR real-time events; participant join flow |
| Phase 2 — Interactive Features | Complete | Quadrant activity; full API integration; production infrastructure (DB migrations) |
| Phase 3 — Advanced Platform | Complete | Cookie auth (OTP email), session groups, session templates, AI integration (4 providers), Serilog logging, session scheduling |
| Phase 4 — Remaining Backlog | In Progress | QnA, Quiz, FiveWhys activities; API key encryption; advanced exports |

---

## 2. What Has Been Delivered

### Phase 1 — MVP (Complete)
- **4 Activity Types**: Poll (bar chart), Rating (histogram), WordCloud (cloud/chart/list), GeneralFeedback (categorized text)
- **Session wizard**: 4-step create flow (context → join form → activities → review)
- **Real-time event system**: 5 typed SignalR events (`SessionStateChanged`, `ActivityStateChanged`, `ParticipantJoined`, `ResponseReceived`, `DashboardUpdated`)
- **Participant join flow**: Code entry → join form → lobby → activity → done
- **InMemory and SQLite database support**
- **16 unit tests passing**

### Phase 2 — Interactive Features (Complete)
- **Quadrant activity**: Full participant input (dual-slider X/Y + label) and facilitator scatter plot dashboard
- **Full API integration**: All endpoints wired — create, start, end session; open/close activity; join; submit response; dashboard
- **Production infrastructure**: EF Core migrations, SQL Server manual scripts, environment config
- **Professional UI/UX**: Responsive Bootstrap 5.3 design system with consistent branding

### Phase 3 — Advanced Platform (Complete, Feb 2026)
- **Cookie-based facilitator authentication**: OTP email login, 8-hour sliding cookie, `FacilitatorTokenMiddleware` + `FacilitatorContextMiddleware`
- **Session Groups**: Hierarchical session organisation with group management UI
- **Session Templates**: 4 system templates + custom templates; seeded from JSON at startup via `TemplateInitializationHostedService`
- **AI integration**: 4 providers (OpenAI, Intelligent, MLNet, Mock); background queue (`IAIWorkQueue` + `AIProcessingHostedService`); PII sanitization; quota system (5 free/month); BYOK
- **AI-generated sessions**: Full UI flow for generating a 3–7 activity workshop agenda from title + goal + context docs
- **Serilog structured logging**: Console + rolling file sinks, 30-day retention
- **Session scheduling**: `SessionStart` / `SessionEnd` timestamps on sessions

---

## 3. Current Tracker

### A. Feature Completion

| ID | Priority | Item | Status | Target |
|----|----------|------|--------|--------|
| FEAT-01 | P1 | Quadrant facilitator dashboard | Done | 18 Feb 2026 |
| FEAT-02 | P1 | FiveWhys — hidden from creation UI until ready | Done (Hidden) | 18 Feb 2026 |
| FEAT-03 | P2 | QnA activity end-to-end (voting, moderation) | Not Started | — |
| FEAT-04 | P2 | Quiz activity end-to-end (scoring, answer validation) | Not Started | — |

### B. Security and Reliability

| ID | Priority | Item | Status | Target |
|----|----------|------|--------|--------|
| ENG-01 | P0 | Encrypt stored OpenAI API key | Not Started | — |
| ENG-02 | P0 | Enforce facilitator auth on live controls | Done | 18 Feb 2026 |
| ENG-03 | P1 | Replace placeholder response count with real API count | Done | 18 Feb 2026 |
| ENG-04 | P1 | Validate existing participant session with server before auto-rejoin | Done | 18 Feb 2026 |
| ENG-05 | P1 | Replace delete confirmation stub with real confirmation flow | Done | 18 Feb 2026 |

### C. Technical Debt

| ID | Priority | Item | Status | Target |
|----|----------|------|--------|--------|
| TECH-01 | P1 | Resolve nullability warnings in token stores / view models / views | Done | 18 Feb 2026 |
| TECH-02 | P1 | Add focused tests for activity workflows and dashboard services | Done | 18 Feb 2026 |
| TECH-03 | P2 | Sync outdated docs with actual implementation | Done | 21 Feb 2026 |
| TECH-04 | P2 | Remove/replace inline styles in activity components | Not Started | — |

---

## 4. Immediate Next Actions (P0 First)

1. **ENG-01 — Encrypt stored OpenAI API key** (P0, security)
   - Sensitive credential currently stored without encryption in the database
   - Options: use `IDataProtectionProvider`, ASP.NET Core Data Protection, or environment variable
   - Blocks: none — straightforward addition to `FacilitatorUserService`

2. **FEAT-03 / FEAT-04 — QnA and Quiz activities** (P2, feature)
   - QnA requires voting architecture (separate response records for upvotes), moderation UI
   - Quiz requires answer validation, scoring, per-question timing
   - Use Poll as reference implementation — estimated 1–2 days each

3. **TECH-04 — Remove inline styles from activity components** (P2, debt)
   - Audit `Participant/Activities/` and `Dashboards/` for inline `style=` attributes
   - Replace with Bootstrap utilities or custom CSS classes in `pulse.css`

---

## 5. Future Roadmap

### Near-Term (Phase 4)

| Feature | Value | Effort | Notes |
|---------|-------|--------|-------|
| QnA activity | High | Medium–High | Voting system, moderation UI |
| Quiz activity | Medium | Medium | Scoring, randomization |
| FiveWhys activity | Very High | Very High | Multi-step wizard, optional AI |
| Encrypt API key (ENG-01) | P0 | Low | Data Protection API |
| Remove inline styles (TECH-04) | Medium | Low | Audit + replace |

### Medium-Term

| Feature | Value | Effort | Notes |
|---------|-------|--------|-------|
| Export (PDF/Excel/JSON) | High | Medium | Session complete data package |
| Advanced dashboard filtering | High | Medium | Multi-field dimension filtering, time range |
| TTL cleanup background service | Medium | Low | Auto-expire and delete old sessions |
| Rate limiting on join / submit endpoints | Medium | Low | Prevent abuse |
| Redis backplane for SignalR | Medium | Medium | Required for multi-instance deployments |

### Long-Term Considerations

| Feature | Notes |
|---------|-------|
| Advanced analytics | Session comparisons, participant engagement trends |
| Azure / cloud deployment | ARM templates, GitHub Actions CI/CD |
| Mobile apps | Native iOS/Android for participant experience |
| SSO / enterprise auth | Integration with Azure AD, Okta |
| Multi-tenancy | Org-level isolation, billing, admin portal |
| Elasticsearch | Full-text search across sessions and responses |

---

## 6. Production Readiness Checklist

| Item | Status |
|------|--------|
| SQLite EF Core migrations | Ready |
| SQL Server manual scripts | Ready (V1.0/, V1.1/) |
| InMemory for dev/test | Ready |
| Cookie-based authentication | Implemented |
| Facilitator auth enforced on all live endpoints | Done |
| Serilog structured logging | Implemented |
| Session TTL and expiry fields | Implemented |
| Docker + docker-compose | Available in `publish/` |
| Deployment guide | `publish/DEPLOYMENT-GUIDE.md` |
| Environment-specific config separation | Done |
| AI provider fallback (Mock) | Done — no hard dependency on API key |
| OpenAI API key encryption | Not Started (ENG-01) |

---

## 7. Activity Type Completion Progress

```
Phase 1 (MVP) — 100%
  [x] Poll
  [x] Rating
  [x] WordCloud
  [x] GeneralFeedback

Phase 2 (Interactive) — 100%
  [x] Quadrant

Phase 3 (Advanced — Backlog)
  [ ] QnA          (FEAT-03)
  [ ] Quiz         (FEAT-04)
  [~] FiveWhys     (FEAT-02 — hidden, deferred)

Overall: 5 / 8 complete (62.5%)
```
