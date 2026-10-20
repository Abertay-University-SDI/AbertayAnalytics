# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.9.1] - 2026-10-20

### Added

- New session ID for every manager initialisation, recorded on each event alongside the active scene.
- `AnalyticsManager.GetCurrentSessionID()` for retrieving the current session ID.
- Explicit handling and validation for common Unity value types, scalars, lists and dictionaries.
- CSV columns for session, scene and individual properties of structured parameters.

### Changed

- New event timestamps use ISO 8601 UTC; existing stored timestamps remain untouched.
- Removed obsolete `GA_Value` from the event model; legacy recordings still load.
- Reset manager state when destroyed, including when domain reload is disabled.


## [0.8.3] - 2026-10-20

### Changed

- Use append-only `.jsonl` event logs instead of continually rewriting a JSON array.
- Serialize each event on the calling thread before enqueuing; the background worker only writes immutable JSON lines.
- Coalesce queued events into bounded batches, with ordered retries on I/O failure, a final flush on shutdown, and an explicit flush for live CSV exports.
- Export menus discover either `.jsonl` or legacy `.json` recordings; `ExportCsvFromJson` supports both formats.

### Added

- One-time, non-destructive migration of older `*_JSON.json` arrays when the corresponding `.jsonl` is first created.
- Crash-tail recovery: preserve an invalid final fragment in `.partial_*.txt` before truncating it; complete unterminated final JSON objects get a newline.
- `Vector3` JSON converter to avoid recursively serializing its `normalized` property.


## [0.8.2] - 2026-10-20

### Changed

- JSON remains the automatic event log; CSV generation is now explicit instead of performed on every event.
- Each CSV export uses a new timestamped file under `Assets/Analytics/Exports/` (or an `Exports` folder beside external log directories), so Excel can keep older exports open.

### Added

- `Export CSV` Scene view toolbar overlay (Unity 6.0+) and main-toolbar action (Unity 6.3+).
- `Tools > Abertay Analytics > Export Latest CSV` and `Export CSV From JSON...` menu actions.
- Play-mode export flushes pending JSON writes before converting the active session.
- `AbertayAnalytics.ExportCsvFromJson` public file converter for use outside the Editor.


## [0.8.1] - 2026-10-20

### Fixed

- Coalesced background CSV/JSON writes with snapshotting and a synchronous shutdown flush.
- Removed unguarded UnityEditor imports from runtime source files.
- Load saved events before invoking analytics initialisation callbacks.
- Proper CSV escaping for all fields, including quotes and newlines.
- Generate unique event IDs with GUIDs.
- Restore saved user IDs or generate and persist a default on first launch.

## [0.8.0] - 2025-10-28

### Changed

- Updated for Unity 6000.0

### Removed
- Temporarily removed support for Unity Analytics. Since the last update the Analytics API has changed significantly and is no longer compatible with the current design of Abertay Analytics. This will require a larger overhaul at a later date.

## [0.6.0] - 2023-11-28

### Changed

- Saving is now handled Asyncronously. No more stalling when you log events!

## [0.5.0] - 2023-10-31

### Added

- Abertay Analytics now exports to CSV! Look for it in the Analytics/Events folder

## [0.4.3] - 2023-10-31

### Changed

- Heatmaps cubes no longer cast or receive shadows.
- Heatmap cubes are static geometry, a little bit nicer for performance!

### Fixed

- Rendered Heatmap cubes no longer have colliders attached. No more bumping!
- Temporary Heatmap materials should be correctly destroyed when regenerating heatmaps.

## [0.4.2] - 2023-10-31

### Fixed

- Removed unused package that cause builds to fail

### Removed

- Further references to the Game Analytics service

## [0.4.1] - 2023-10-31

### Added

- Abertay Analytics now has Heatmap functionality thanks to Hadi Mehrpouya!

### Changed

- Analytics output now saves in a more sensible location
- Unity analytics version upgraded to 5.0.0
- Updated deprecated Unity Analytics functionality

### Fixed

- Environment name is properly set at all times


[unreleased]: https://github.com/Abertay-University-SDI/AbertayAnalytics
[0.4.1]: https://github.com/Abertay-University-SDI/AbertayAnalytics/commit/1c85dc01634cae1f85c985d35868a715e5306262