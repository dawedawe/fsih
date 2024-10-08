# Changelog

## [1.0.4] - 2024-08-22

### Fixed

* Fixed support for non-FSharp.Core namespaces when using a preview SDK.

## [1.0.3] - 2024-08-20

### Fixed

* Fixed support for non-FSharp.Core namespaces, #14.

## [1.0.2] - 2024-05-18

### Fixed

* Fixed support for expressions with quotes, for example `h shouldn't`.

## [1.0.1] - 2024-01-25

### Fixed

* Fixed support for operators, for example `h (+)` works now.

## [1.0.0] - 2024-01-20

### Changed

* Made `h expr` just work without the need for quotation wrapping.

## [0.2.0] - 2024-01-16

### Fixed

* Fixed a caching issue which caused the loss of xml fragments.

### Added

* Added `H.H` to apply directly to expressions without the need for quotation wrapping.

## [0.1.1] - 2023-12-25

### Fixed

* Improve handling of nested xml.

## [0.1.0] - 2023-12-24

### Added

* Initial release
