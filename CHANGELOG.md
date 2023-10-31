# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.5.0] - 2023-10-31

###Added

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