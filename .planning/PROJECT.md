# Datadog MAUI SDK

## What This Is

An official Datadog SDK for .NET MAUI applications, distributed as a NuGet package (`Datadog.Maui`). It wraps the native iOS and Android Datadog SDKs through .NET bindings, providing a unified cross-platform API for Real User Monitoring (RUM), Logs, and crash reporting. Built for teams migrating from App Center or adopting Datadog for mobile observability.

## Core Value

Enable .NET MAUI developers to monitor their applications with Datadog using a single, cross-platform API that "just works" — automatic crash and network instrumentation out of the box, with manual APIs for custom tracking.

## Requirements

### Validated

(None yet — ship to validate)

### Active

- [ ] Official NuGet package (`Datadog.Maui`) owned by Datadog
- [ ] Support for .NET MAUI 10 (v9 EOL: 2026-05-12)
- [ ] iOS platform support (iOS 17.0+)
- [ ] Android platform support (API 26+)
- [ ] Automatic collection of .NET exceptions with stack traces
- [ ] Automatic collection of native iOS crashes with symbolication
- [ ] Automatic collection of native Android crashes with deobfuscation
- [ ] Automatic App Hangs detection (iOS)
- [ ] Automatic ANR detection (Android)
- [ ] Automatic network request tracking for HttpClient
- [ ] Manual view tracking API
- [ ] Manual action tracking API
- [ ] RUM product support
- [ ] Logs product support

### Out of Scope

- Automatic navigation-aware view tracking (Shell, NavigationPage) — P1, defer to v2
- Automated actions tracking — P1, defer to v2
- MAUI-specific App Launch tracking — P2
- Operations — P2
- Session Replay — P2
- .NET MAUI 9 and earlier — P2, EOL soon
- Desktop targets (Windows, Mac Catalyst) — P2
- Blazor Hybrid support — P2, separate instrumentation model

## Context

**Source of truth repositories:**
- `dd-sdk-ios` — Native iOS SDK implementation
- `dd-sdk-android` — Native Android SDK implementation
- `browser-sdk` — Reference for API design patterns
- `rum-events-format` — Event schema definitions

**Reference implementation:**
- Kyle Taylor's fork: `kyletaylored/datadog-dotnet-mobile-sdk-bindings`
- Provides working .NET bindings for iOS/Android SDKs
- Can be used as base, adapted as needed, or referenced for patterns

**Architecture approach:**
```
┌─────────────────────────────────────┐
│  Datadog.Maui (Unified .NET API)   │  ← Cross-platform, MAUI-idiomatic
├─────────────────────────────────────┤
│  Datadog.Maui.iOS │ Datadog.Maui.Android │  ← Platform bindings
├─────────────────────────────────────┤
│  dd-sdk-ios       │ dd-sdk-android       │  ← Native SDKs
└─────────────────────────────────────┘
```

**Design principles:**
- Unified + Raw: Clean cross-platform API for common cases, with access to native APIs when needed
- Opt-out instrumentation: Automatic features enabled by default, configurable to disable
- Mirror native SDK behavior: Leverage battle-tested native implementations

**Business context:**
- $3.6M ARR opportunity tied to MAUI support
- 20+ escalation emails, 40+ feature requests from customers
- Microsoft App Center retirement accelerating demand
- Competitors (Sentry, New Relic, AppDynamics, Dynatrace) all have MAUI support

## Constraints

- **Tech stack**: .NET MAUI 10, C#, NuGet distribution
- **Platforms**: iOS 17.0+ (matches native SDK), Android API 26+
- **Architecture**: Must wrap native SDKs via bindings (not reimplement)
- **Versioning**: Should align with native SDK versions where possible

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Wrap native SDKs via bindings | Leverage existing battle-tested implementations, reduce maintenance burden | — Pending |
| Unified + Raw API approach | Clean DX for common cases, power user access to native features | — Pending |
| Opt-out auto-instrumentation | Match competitor behavior, maximize out-of-box value | — Pending |
| Target MAUI 10 only | v9 EOL soon, avoid maintaining legacy compatibility | — Pending |

---
*Last updated: 2025-01-22 after initialization*
