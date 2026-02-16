# EVEMon.Infrastructure

Service implementations that don't depend on EVEMon.Common. Cross-cutting concerns.

## What goes here
- `EventAggregator` -- the sole pub/sub event delivery mechanism (replaces 74 static events)
- `SmartQueryScheduler` -- rate-limited ESI query scheduling
- `ScheduledQueryableAdapter` -- bridges query monitors to the scheduler
- Network infrastructure: `ApiRequestQueue`, `HttpWebClientServiceException`, `ResponseParams`
- Notification types: `NotificationCategory`, `NotificationPriority`, `NotificationBehaviour`
- Email providers: Gmail, Yahoo, Hotmail, GMX (via MailKit)
- `CredentialProtection` -- DPAPI wrapper for token storage

## What does NOT go here
- Adapter services that wrap EveMonClient statics (those go in Common/Services)
- UI-specific code
- Types that need to reference EVEMon.Common

## Key folders
- `Services/` -- `EventAggregator`, `SmartQueryScheduler`, `ScheduledQueryableAdapter`
- `Net/` -- HTTP infrastructure, exception types, request queue
- `Notifications/` -- Notification enums and behavior definitions
- `EmailProvider/` -- Email service provider implementations
- `Helpers/` -- `CredentialProtection`

## Dependencies
- EVEMon.Core, EVEMon.Data, EVEMon.Serialization, EVEMon.Models
- MailKit, System.IdentityModel.Tokens.Jwt
- `InternalsVisibleTo`: EVEMon.Common, EVEMon, EVEMon.Tests
