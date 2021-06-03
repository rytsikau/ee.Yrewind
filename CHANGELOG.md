# Changelog

## [21.061] - 2021-06-03

### Added

- Parameter `-cookie` to save live stream using cookie file.

### Changed

- The `-start` parameter is now calculated not relative to the time on the computer, but relative to the server time.
- The relative path in the `-ffmpeg` parameter is now determined relative to the location of the batch file (command run) directory, rather than relative to the location of *yrewind.exe*.

### Fixed

- Several minor fixes.

### Removed

- The ability to view a specified time interval using *VLC Media Player*.

## [21.051] - 2021-05-20

### Added

- Support for arguments and nested quotes when using the `-executeonexit` parameter.
- The ability to autorestart to get the next part of the stream (command `-executeonexit=*getnext*`).

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

- Parameter `-filename` to save live stream with custom file name (rename masks supported).
- Parameter `-start=yyyyMMdd:hhmmss` to specify starting point with seconds.
- Parameter `-url=[channelUrl]` to monitor the specified channel for new live streams.

### Fixed

- The time in the file name now more closely matches the time of the streamer.
- Several minor fixes.

## [21.012] - 2021-01-08

### Added

- Option `-start=+[minutes]` for the delayed start of recording.
- Streamer name to video file metadata.
- Time in filename is now with seconds.

### Fixed

- The bug due to which the program could not find information about the stream.
- Several minor fixes.

## [21.011] - 2021-01-04

### Added

- Option `-start=beginning` to rewind the live stream to the first available moment.
- Option `-start=wait` to wait for the scheduled live stream to start and then automatically record it from the first second.

### Changed

- The built-in help of the program has become a little more convenient.

### Fixed

- The bug with FFmpeg freezing if stream terminated during real time recording.
- Several minor fixes.

### Removed

- The `-pathchrome` and `-nocache` parameters have been removed.

## [20.124] - 2020-12-31

### Changed

- The speed of receiving information about live stream has been increased.

### Fixed

- Several minor fixes.

### Removed

- Dependency on Google Chrome. Now the browser is not required for the program to work.

## [20.123] - 2020-12-28

### Added

- Parameter `-vformat=[formatExtension]`.
- Support for resolutions higher than 1080p.

### Fixed

- Several minor fixes.

## [20.122] - 2020-12-25

### Changed

- Improved and accelerated work with cache.
- Modes *rewind* and *real time* are combined: now it's possible to save intervals like `-start=-30 -duration=60` (the first part of the file is downloaded at high speed and the rest is recorded in real time).

### Fixed

- The bug due to which all incomplete videos were without sound (for example, when the program was manually closed during recording).
- Several minor fixes.

### Removed

- Sync warning in file name if duration does not match specified. Now program just leaves temp file name.

## [20.121] - 2020-12-10

### Fixed

- An issue where some streams could not be downloaded due to an error 9411 (*Cannot process live stream with FFmpeg library*).
- Several minor fixes.

## [20.113] - 2020-11-28

### Added

- The *real time* mode: now program can record live stream in real time.

### Changed

- If the `-start` parameter is missing, the program now runs in real time recording mode, saving the *following* 1 hour of the stream, not the previous ones.
- Improved speed of caching information about the required live stream.
- Increased the maximum allowed video duration (up to 90 minutes).

### Fixed

- The bug that caused an exception to be thrown when specifying a non-absolute path to the `-pathsave` parameter.
- Several minor fixes.

## [20.112] - 2020-11-16

### Added

- Preliminary internet connection check to prevent FFmpeg freezing.

### Changed

- Increased the maximum allowed video duration (up to 75 minutes).

### Fixed

- The bug with incorrect URL recognition if it was specified without quotes and contained a hyphen.
- Several minor fixes.

## [20.111] - 2020-11-03

### Added

- Parameter `-start=-[minutes]`.

### Fixed

- Several minor fixes.

## [20.105] - 2020-10-31

### Added

- Duration checking for downloaded videos.

### Fixed

- An error 9124 (*FFmpeg not found*) if *bat* file was located in a different directory than program.
- Several minor fixes.

## [20.104] - 2020-10-13

### Added

- Saving metadata to the output video.

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

- Checking if other instances of the program is running.
- Determining of the earliest available live stream time point.
- The ability to cache live stream information to improve save speed.

### Changed

- Now the program determines the nearest lower resolution if nonexistent is specified (instead of higher).
- The program interface has been redesigned.

### Fixed

- The bug when empty directories created by the current instance of the program (for example, if the video did not downloaded) were not deleted.
- The bug causing the duration of some videos to be several seconds longer than the specified one.
- Several minor fixes.

## [20.075] - 2020-07-30

### Added

- Checking for interval availability before downloading it.

### Fixed

- The bug with playing live stream sound when receiving information about it.
- Several minor fixes.

## [20.074] - 2020-07-21

### Changed

- To reduce the file size, the assembly of the program has been moved from .NET Core to .NET Framework.

### Fixed

- Several minor fixes.

## [20.073] - 2020-07-20

### Added

- Recognition of different URL spellings.

### Fixed

- Several minor fixes.

## [20.072] - 2020-07-19

### Added

- The function showing download progress.
- Built-in help (with contents of *readme* file).
- Live stream title parsing.
- Video type recognition (live stream or regular video).
- Video is now first saved under a temporary file name to prevent overwriting in case of an error.

### Fixed

- Several minor fixes.

### Removed

- The ability to download video files without limiting the duration.

## [20.071] - 2020-07-06

### Added

- Basic functionality developed.

[21.061]: https://github.com/rytsikau/ee.Yrewind/releases/download/20210603/ee.yrewind_21.061.zip
[21.051]: https://github.com/rytsikau/ee.Yrewind/releases/download/20210520/ee.yrewind_21.051.zip
[21.041]: https://github.com/rytsikau/ee.Yrewind/releases/download/20210406/ee.yrewind_21.041.zip
