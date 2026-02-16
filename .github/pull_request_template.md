## Summary
<!-- 1-3 bullet points describing what this PR does -->

## Architectural Laws Checklist

- [ ] No new `EveMonClient.` direct access from UI code (Law 14)
- [ ] No new static events — all events through EventAggregator (Law 5)
- [ ] No new static mutable state outside AppServices (Law 1)
- [ ] All `async void` methods wrapped in try/catch (Law 10)
- [ ] All EventAggregator subscriptions stored and disposed (Law 11)
- [ ] New services have interfaces in EVEMon.Core (Law 4, 5)
- [ ] Dependencies flow downward only — no circular refs (Law 3)
- [ ] New files in correct assembly (Law 8)
- [ ] Nothing added to EveMonClient (Law 9)
- [ ] Tests added for new behavior (Law 12)
- [ ] All 821+ tests pass

## Test Plan
<!-- How was this tested? Which test classes cover the changes? -->
