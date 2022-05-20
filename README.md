# ee.Yrewind

Yrewind is a command line utility for saving YouTube live streams in original quality.

The program has the following features:

* Delayed start recording
* Recording in real time
* Downloading recently finished live streams
* Rewinding to the specified time point in the past, and downloading from that point
* Waiting for the scheduled live stream to start and then automatically recording from the first second
* Monitoring the specified channel for new live streams and then automatically recording from the first second

Supported parameters:

[ ` -url (-u) ` ](#-urlurl)
[ ` -start (-s) ` ](#-startyyyymmddhhmm--startyyyymmddhhmmss)
[ ` -duration (-d) ` ](#-durationminutes)
[ ` -resolution (-r) ` ](#-resolutionheightpixels)<br>
[ ` -ffmpeg (-f) ` ](#-ffmpegcpathtoffmpeg)
[ ` -output (-o) ` ](#-outputcdir1dir2filenameextension)
[ ` -browser (-b) ` ](#-browsercpathtobrowserfileexe)
[ ` -cookie (-c) ` ](#-cookiecpathtocookiefileext)
[ ` -executeonexit (-e) ` ](#-executeonexitcpathtosomefileext)

<br>

# [>> download <<](https://github.com/rytsikau/ee.Yrewind/releases/download/20220519/ee.yrewind_22.051.zip)
Changelog is [here](https://github.com/rytsikau/ee.Yrewind/blob/main/CHANGELOG.md). If something doesn't work, please [report](https://github.com/rytsikau/ee.Yrewind/issues) and try [previous versions](https://github.com/rytsikau/ee.Yrewind/releases).

<br>

## Screenshot

<img src='https://github.com/rytsikau/ee.yrewind/raw/main/screenshot.png'>

<br>

## Quick start

1. Unpack the downloaded zip.
2. Open *run.bat* in a text editor and paste the URLs of required streams instead of existing samples.
3. Save *run.bat* and run it!

<br>

## Usage

The only required command line argument is the `-url`:

##### [**` -url=[url] `**](#)

With this command, the program records a livestream in real time for 1 hour at 1080p resolution, or at a lower if 1080p is not available. URL can be specified in various formats:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE'
>     yrewind -url=https://www.youtu.be/9Auq9mYxFEE
>     yrewind -url=9Auq9mYxFEE
>     (etc.)

Channel URL can also be specified, this allows to wait for a new livestream on the channel and automatically start recording from the first second when it starts. Please note that when specifying a channel URL, active livestreams are ignored, the program will wait for a new one.
>     yrewind -url='https://www.youtube.com/c/SkyNews'
>     yrewind -url=www.youtube.com/user/SkyNews/
>     yrewind -url='youtube.com/channel/UCoMdktPbSTixAyNGwb-UYkQ'
>     yrewind -url=UCoMdktPbSTixAyNGwb-UYkQ

<br>

To rewind the livestream or delay the start of recording, use the `-start` parameter. It has several spellings:

##### [**` -start=[YYYYMMDD:hhmm], -start=[YYYYMMDD:hhmmss] `**](#)
##### [**` -start=[Y:hhmm], -start=[Y:hhmmss] `**](#)
##### [**` -start=[T:hhmm], -start=[T:hhmmss] `**](#)
##### [**` -start=beginning, -start=b `**](#)
##### [**` -start=-[minutes] `**](#)
##### [**` -start=+[minutes] `**](#)

This parameter specifies the point in time from which to save the stream. It's calculated relative to the moment the program was started (displayed in the first line). If the parameter is missing, program saves ongoing livestream in real time, scheduled and finished - from the beginning. Depending on technical parameters of the livestream, start point may be shifted from the requested one by several seconds (in a larger direction).

To download the time interval from 7:10AM to 8:10AM on July 15, 2020:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=20200715:0710

To download the time interval from yesterday 10:15PM to 11:15PM:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=Y:2215

To download the time interval from today 02:00AM to 03:00AM:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=T:020000

To download from the first currently available moment:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=beginning

To download the time interval from 3 hours ago to 2 hours ago:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=-180

To wait 2 hours, then record for 1 hour:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=+120

<br>

The program also has several other parameters:

##### [**` -duration=[minutes] `**](#)
##### [**` -duration=[minutes].[seconds] `**](#)

Specifies the required duration. The minimum value is 0.01 (1 second), the maximum is limited to 300 (5 hours). If the parameter is missing, program uses the default 1 hour. Depending on technical parameters of the livestream, result duration may differ from the requested one by several seconds (in a larger direction). The examples below saves 300 minutes of the livestream:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -duration=300
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -duration=300.00
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -duration=max

<br>

##### [**` -resolution=[heightPixels] `**](#)

Specifies the required resolution in pixels (height). If this parameter is missing, program uses the default 1080. If the requested resolution is not available, the nearest lower will be selected. In the examples below, the livestream will be saved at 144p:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=144
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=200
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=1
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=min

<br>

##### [**` -ffmpeg='c:\path\to\ffmpeg\' `**](#)

Specifies the path to FFmpeg library. If relative path is specified, the base folder is the folder from which the command line was run. If this parameter is missing, Yrewind tries to find FFmpeg in its own folder and using environment variables.
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -ffmpeg='c:\Program Files\FFmpeg\'

<br>

##### [**` -output='c:\dir1\dir2\filename.extension' `**](#)

Specifies custom folder, filename and extension (media container format) for the saved livestream. If relative path is specified, the base folder is the folder from which the command line was run. If this parameter is missing, program uses the next default values:
* `[batch file folder]\saved_streams\` - for folder
* `[id]_[date]-[time]_[duration]_[resolution]` - for filename
* `.mkv` - for extension

The `-output` parameter can be specified partially, then the missing parts are replaced with default values. In this case, the part of the string to the right of the last slash is interpreted as filename and/or extension. If the string does not contain slashes, it's fully interpreted as filename and/or extension:
* `c:\dir1\dir2\` - custom folder, default filename, default extension
* `dir1\filename` - custom subfolder, custom filename, default extension
* `dir1\.extension` - custom subfolder, default filename, custom extension
* `filename` - default folder, custom filename, default extension
* `.extension` - default folder, default filename, custom extension

Folder and filename supports renaming masks: `*id*`, `*start*`, `*start[customDateTime]*` (recognizes letters yyyyMMddHHmmss), `*duration*`, `*resolution*`, `*channel_id*`, `*author*` and `*title*`.

The extension defines the format of the media container in which the livestream will be saved. Formats description:
* `.avi`, `.mp4` - use AVC and MP4a data; if AVC is unavailable, use VP9
* `.asf`, `.mkv`, `.wmv` - use VP9 and MP4a data; if VP9 is unavailable, use AVC
* `.3gp`, ` .flv`, ` .mov`, `.ts` - use AVC and MP4a data; doesn't support high resolutions - saves at 1080p even if requested higher resolution is available
* `.aac`, `.m4a`, `.wma` - use MP4a data; saves only audio (to save only audio, you can also specify zero resolution `-r=0` - this works with all audio and video formats except `.mp4`)
* `.m3u`, `.m3u8` - playlist files pointing to livestream on the Internet, allows watching in media player without downloading. Tested with VLC. Shelf life of playlist files is only 6 hours

The example below saves the livestream as *\saved_streams\9Auq9mYxFEE_20210123-185830_060.00m_1080p.ts*:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -output=.ts

The next example saves the livestream as *d:\My saved streams\Sky News\2021-01-12_12-34.mkv*:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -output='d:\My saved streams\*author*\*start[yyyy-MM-dd_HH-mm]*'

<br>

##### [**` -browser='c:\path\to\browser\file.exe' `**](#)

Allows to use browser in headless mode to get technical information about the livestream. It can be useful if Yrewind can't get info about the stream on its own. For the portable version of browser, specify the full path to the executable file; for the installed version, it's usually enough to specify the name. Only Chromium-based browsers are supported - Chrome, Edge, Brave, Opera, Vivaldi, etc.
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -browser='d:\My Portable Programs\Vivaldi\Application\vivaldi.exe'
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -browser=msedge

<br>

##### [**` -cookie='c:\path\to\cookie\file.ext' `**](#)

Specifies the path to the cookie file. If relative path is specified, the base folder is the folder from which the command line was run. The parameter can be useful if YouTube requires a captcha or authorization to access a livestream with age or membership restrictions. The cookie file must be in Netscape format and can be obtained using any suitable browser add-on. Please note that cookie created after solving captcha is usable for only a few hours. Instead of solving captcha, it's better to log in to YouTube and create cookie after that.
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -cookie='cookies.txt'

<br>

##### [**` -executeonexit='c:\path\to\some\file.ext' `**](#)

Specifies the command to run after Yrewind exits. If it's an executable file, you can also specify the arguments it supports (don't forget the quotes - nested supported). The non-executable file will be launched by the associated program. The parameter supports two rename masks - `*output*`, which contains the full path of the saved video, and `*getnext*`, which contains the command to start Yrewind again to get the next interval of the stream. When using `-executeonexit=*getnext*` command inside a batch file, keep in mind that this file is first executed to the end, and only then the `*getnext*` command is executed. Also use rename masks `*start*` and `*start[customDateTime]*` to avoid duplicate names of saved stream parts (or just use default auto-naming). In the first example below, the saved video will be opened by the associated program, in the second - using the specified program:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -executeonexit=*output*
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -executeonexit=''c:\Program Files\VLC mediaplayer\vlc.exe' *output*'

<br>

**More examples:**

To save 1 hour of the stream from 04:55AM to 05:55AM on May 5, 2020, at 720p, to specified folder:
>     yrewind -u=9Auq9mYxFEE -s=20200505:0455 -r=720 -o='d:\My saved streams\'

To save 89 minutes 30 seconds of the stream from today 10:45AM to 12:15PM, at 1080p:
>     yrewind -u=9Auq9mYxFEE -s=T:1045 -d=89.30

To record livestream until it ends, starting from the beginning, in `.ts` format, save result video to desktop:
>     yrewind -u=9Auq9mYxFEE -s=b -o=%UserProfile%\Desktop\.ts -e=*getnext*

To immediately play (without downloading) with assotiated mediaplayer, from yesterday 03:00AM, at the maximum available resolution:
>     yrewind -u=9Auq9mYxFEE -s=Y:0300 -r=max -o=.m3u -e=*output*

<br>

## Notes

* Loss of packets on the streamer side causes the estimated time to shift. The offset is usually seconds, but if its internet connection is unstable and/or the stream has been running for a long time, it can be minutes or even hours. For example, if the stream was interrupted for a total of 1 hour, then 24-hour frames will be presented as 23-hour. Thus, start point time accuracy can only be guaranteed for the current moment. The further the livestream is rewound, the less accuracy. Also, if there are interruptions in the livestream at the specified time interval, the duration of the saved file will be shorter by the total duration of those interruptions; a warning for this incompleted file will be displayed
* Occasionally, the message `unable to verify the saved file is correct` appears. The reasons may be as follows: if the duration of the saved file cannot be verified (there is a possibility that the file is damaged); if the duration of the saved file does not match the requested one (also in this case, the output file name contains the word *INCOMPLETE*); if the starting point of the requested time interval cannot be accurately determined (for example due to server side error)
* To reduce the chance of output file corruption, it's better not to use Moov Atom formats (`.3gp`, `.mov`, `.mp4`, `.m4a`) for long recordings. Also, these formats don't allow to play a file that is in the process of downloading (other formats do)
* Recently finished livestreams can be downloaded within approximately 6 hours of completion. After this time, such a stream "turns" into a regular video and can be downloaded, for example, using youtube-dl
* When using proxy, VPN or special firewall settings, keep in mind that not only Yrewind should have appropriate access, but also FFmpeg

<br>

## Terms of use

* This software provides access to information on the Internet that is publicly available and does not require authorization or authentication
* This software is free for non-commercial use and is provided 'as is' without warranty of any kind, either express or implied
* The author will not be liable for data loss, damages or any other kind of loss while using or misusing this software
* The author will not be liable for the misuse of content obtained using this software, including copyrighted, age-restricted, or any other protected content

<br>

## Developer info

* C#
* .NET Framework 4.5
* Visual Studio Community 2019

<br>

## Requirements

* FFmpeg static build (included in the archive)
* Windows 7 and on / Windows Server 2008 and on
* Chromium-based browser (for some livestreams)

<br>

## Tested configuration

* FFmpeg 4.3 x86 (by Zeranoe)
* Windows 10 Pro x32
* Windows 10 Pro x64

<br>

## About

Console utility for saving YouTube live streams with rewind function up to 167 hours

<br>

## Tags

download downloader dvr live livestream record rewind save stream youtube

---
[[program page]](https://rytsikau.github.io/ee.Yrewind) [[start page]](https://rytsikau.github.io) [[author e-mail]](mailto:y.rytsikau@gmail.com)
