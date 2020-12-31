# Changelog

## [20.124] - 2020-12-31

### Changed

- The speed of receiving information about live stream has been increased.

### Fixed

- Several minor fixes.

### Removed

- Removed dependency on Google Chrome. Now the browser is not required for the program to work.

## [20.123] - 2020-12-28

### Added

- Added parameter `-vformat=[formatExtension]`.
- Added support for resolutions higher than 1080p.

### Fixed

- Several minor fixes.

## [20.122] - 2020-12-25

### Changed

- Improved and accelerated work with cache.
- Modes *rewind* and *real time* are combined: now it's possible to save intervals like `-start=-30 -duration=60` (the first part of the file is downloaded at high speed and the rest is recorded in real time).

### Fixed

- Fixed a bug due to which all incomplete videos were without sound (for example, when the program was manually closed during recording).
- Several minor fixes.

### Removed

- Removed *sync warning* in file name if duration does not match specified. Now program just leaves temp file name.

## [20.121] - 2020-12-10

### Fixed

- Fixed an issue where some streams could not be downloaded due to an error 9411 (*Cannot process live stream with FFmpeg library*).
- Several minor fixes.

## [20.113] - 2020-11-28

### Added

- Added *real time* mode: now program can record live stream in real time.

### Changed

- If the `-start` parameter is missing, the program now runs in *real time* recording mode, saving the *following* 1 hour of the stream, not the previous ones.
- Improved speed of caching information about the required live stream.
- Increased the maximum allowed video duration (up to 90 minites).


### Fixed

- Fixed a bug that caused an exception to be thrown when specifying a non-absolute path to the `-pathsave` parameter.
- Several minor fixes.

## [20.112] - 2020-11-16

### Added

- Added preliminary internet connection check to prevent FFmpeg freezing.

### Changed

- Increased the maximum allowed video duration (up to 75 minites).

### Fixed

- Fixed a bug with incorrect URL recognition if it was specified without quotes and contained a hyphen.
- Several minor fixes.

## [20.111] - 2020-11-03

### Added

- Added parameter `-start=-[minutes]`.

### Fixed

- Several minor fixes.

## [20.105] - 2020-10-31

### Added

- Added duration checking for downloaded videos.

### Fixed

- Fixed an error 9124 (*FFmpeg not found*) if *bat* file was located in a different directory than program.
- Several minor fixes.

## [20.104] - 2020-10-13

### Added

- Added saving metadata to the output video.

## [20.103] - 2020-10-12

### Changed

- Command line arguments are now case insensitive.

### Fixed

- Several minor fixes.

## [20.102] - 2020-10-07

### Fixed

- Several minor fixes.

## [20.101] - 2020-10-06

### Added

- Added check for other instances of the program is running.
- Added determining of the earliest available live stream time point.
- Added the ability to cache live stream information to improve save speed.

### Changed

- Now the program determines the nearest lower resolution if nonexistent is specified (instead of higher).
- The program interface has been redesigned.

### Fixed

- Fixed a bug when empty directories created by the current instance of the program (for example, if the video did not downloaded) were not deleted.
- Fixed a bug causing the duration of some videos to be several seconds longer than the specified one.
- Several minor fixes.

## [20.075] - 2020-07-30

### Added

- Added check for interval availability before downloading it.

### Fixed

- Fixed a funny bug with playing live stream sound when receiving information about it.
- Several minor fixes.

## [20.074] - 2020-07-21

### Changed

- To reduce the file size, the assembly of the program has been moved from .NET Core to .NET Framework.

### Fixed

- Several minor fixes.

## [20.073] - 2020-07-20

### Added

- Added recognition of different URL spellings.

### Fixed

- Several minor fixes.

## [20.072] - 2020-07-19

### Added

- Added a function showing download progress.
- Added built-in help system (with contents of *readme* file).
- Added live stream title parsing.
- Added video type recognition (live stream or regular video).
- Video is now first saved under a temporary file name to prevent overwriting in case of an error.

### Fixed

- Several minor fixes.

### Removed

- The ability to download video files without limiting the duration.

## [20.071] - 2020-07-06

- Basic functionality developed.

[20.124]: https://github.com/rytsikau/ee.yrewind/raw/main/ee.yrewind_20.124.zip
[20.123]: https://github.com/rytsikau/ee.yrewind/raw/main/ee.yrewind_20.123.zip
[20.122]: https://github.com/rytsikau/ee.yrewind/raw/main/ee.yrewind_20.122.zip
