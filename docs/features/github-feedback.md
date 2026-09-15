# Feature: github-feedback

> Compose GitHub issue-form URLs, clipboard fallback, fail-soft duplicate search.

## Acceptance criteria

- ✅ User-visible behavior: small fields prefill `issues/new?template=...`; large bodies use clipboard + short URL
- ✅ Offline/error behavior: `OWNER/REPO` never hits the network; 403/timeout/placeholder return `[]`
- ✅ Accessibility: N/A (logic); Open GitHub is `https` only
- ✅ i18n: N/A in this container (copy lives in `feedback.*`)

## Smoke scenario

1. Given `release_repo` `acme/app` and fingerprint `a1b2c3d4e5f6`
2. When the composer builds a crash title
3. Then the title is `[crash] a1b2c3d4e5f6 TypeError` and a second search inside 60s does not fetch again

## Container map

| Layer | Path |
|-------|------|
| Logic | `QuickMediaIngest/Core/GitHubFeedback/` |
| Tests | `QuickMediaIngest.Tests/GitHubIssueComposerTests.cs`, `GitHubDuplicateSearchTests.cs` |
| Wiring | none (Feedback overlay calls this) |
## Tests

- Automated: yes — `QuickMediaIngest.Tests/GitHubIssueComposerTests.cs`, `GitHubDuplicateSearchTests.cs`

## Fallback validation

- Why tests are not feasible: N/A (automated tests exist)
- Command: `python3 scripts/agent-run.py feature-gate --stack dotnet-wpf`

## Definition of Done

See `docs/FEATURE_MODULES.md`. Fallback: `python3 scripts/agent-run.py feature-gate --stack dotnet-wpf`

## Notes

- Own `isPlaceholderRepo` in this container (do not import About) so About add/remove still passes. Default feedback repo is `release_repo`
- Share the 60s cooldown with Open GitHub (Search API 10 req/min)
