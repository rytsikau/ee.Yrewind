# Changelog

## [21.051] - 2021-05-20

### Added

- Added support for arguments and nested quotes when using the `-executeonexit` parameter.
- Added the ability to autorestart to get the next part of the stream (command `-executeonexit=*getnext*`).

### Fixed

- Added an additional way to determine UTC time if the page of the required stream contains incorrect information (the reason for the hang of some streams).
- Several minor fixes.
- Updated algorithm for obtaining information about the stream.

## [21.041] - 2021-04-06

### Changed

- The format of the metadata in the saved file (it is now formatted as: *title || author || live stream URL || channel URL || UTC start point*).

### Fixed

- Error while saving file if its path contains invalid filesystem characters (when using rename masks `*author*` and `*title*`).
- Several minor fixes.

## [21.031] - 2021-03-18

### Added

- Support for saving audio files.
- The `-executeonexit` parameter to run document or executable file after the program finishes.

### Fixed

- Clarified the saved formats.
- Several minor fixes.

## [21.023] - 2021-02-28

### Fixed

- Several minor fixes.

## [21.022] - 2021-02-24

### Added

- Single-character aliases for all parameters (`-u` for `-url`, `-s` for `-start`, etc.).

### Changed

- The `-pathffmpeg` parameter has been renamed to `-ffmpeg`. The renamed parameter now supports relative paths, now you can also specify the path to *VLC Media Player* (to view the required time interval instead of saving it).
- The `-pathsave` parameter has been renamed to `-output`. The renamed parameter now supports relative paths and rename masks for the output directory (the directory name must now end with a slash). In addition, the functionality of this parameter has been extended with the functionality of the `-filename` and `-vformat` deleted parameters.

### Fixed

- Several minor fixes.

### Removed

- The `-filename` and `-vformat` parameters (their functionality has been moved to the `-output` parameter).

## [21.021] - 2021-02-15

### Fixed

- Several minor fixes.

## [21.015] - 2021-01-27

### Changed

- Increased the maximum allowed video duration (up to 300 minutes).

## [21.014] - 2021-01-17

### Fixed

- Several minor fixes.

## [21.013] - 2021-01-12

### Added

- Added option `-filename` to save live stream with custom file name (rename masks supported).
- Added option `-start=yyyyMMdd:hhmmss` to specify starting point with seconds.
- Added option `-url=[channelUrl]` to monitor the specified channel for new live streams.

### Fixed

- The time in the file name now more closely matches the time of the streamer.
- Several minor fixes.

## [21.012] - 2021-01-08

### Added

- Added `-start=+[minutes]` option for the delayed start of recording.
- Added streamer name to video file metadata.
- Time in filename is now with seconds.

### Fixed

- Fixed a bug due to which the program could not find information about the stream.
- Several minor fixes.

## [21.011] - 2021-01-04

### Added

- Added `-start=beginning` option to rewind the live stream to the first available moment.
- Added `-start=wait` option to wait for the scheduled live stream to start and then automatically record it from the first second.

### Changed

- The built-in help of the program has become a little more convenient.

### Fixed

- Fixed bug with FFmpeg freezing if stream terminated during real time recording.
- Several minor fixes.

### Removed

- The `-pathchrome` and `-nocache` options have been removed.

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

- Removed sync warning in file name if duration does not match specified. Now program just leaves temp file name.

## [20.121] - 2020-12-10

### Fixed

- Fixed an issue where some streams could not be downloaded due to an error 9411 (*Cannot process live stream with FFmpeg library*).
- Several minor fixes.

## [20.113] - 2020-11-28

### Added

- Added *real time* mode: now program can record live stream in real time.

### Changed

- If the `-start` parameter is missing, the program now runs in real time recording mode, saving the *following* 1 hour of the stream, not the previous ones.
- Improved speed of caching information about the required live stream.
- Increased the maximum allowed video duration (up to 90 minutes).

### Fixed

- Fixed a bug that caused an exception to be thrown when specifying a non-absolute path to the `-pathsave` parameter.
- Several minor fixes.

## [20.112] - 2020-11-16

### Added

- Added preliminary internet connection check to prevent FFmpeg freezing.

### Changed

- Increased the maximum allowed video duration (up to 75 minutes).

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

[21.051]: https://github.com/rytsikau/ee.Yrewind/releases/download/20210520/ee.yrewind_21.051.zip
[21.041]: https://github.com/rytsikau/ee.Yrewind/releases/download/20210406/ee.yrewind_21.041.zip
[21.031]: https://github.com/rytsikau/ee.Yrewind/releases/download/20210318/ee.yrewind_21.031.zip
