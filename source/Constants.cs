using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace yrewind
{
    // Storing messages, readme text and various constants
    static class Constants
    {
        #region Msg - Messages
        public static readonly Dictionary<int, string> Msg = new Dictionary<int, string>
        {
            [9000] = "[class Program]",
            [9050] = "----------------------------------------------------------------",
            [9051] = "{0} (version {1})",
            [9052] = "--> {0} (started at {1})",
            [9053] = "--> OK ({0})",
            [9054] = "--> ERROR {0}: {1} ({2})",
            [9055] = "",
            [9056] = "",
            [9057] = "Check '-output' argument",
            [9058] = "Checking",
            [9059] = "Command line input is too long",
            [9060] = "Current version is actual",
            [9061] = "Delayed start is limited to 24 hours",
            [9062] = "Downloading, please wait a minute",
            [9063] = "Error",
            [9064] = "File already exists",
            [9065] = "Getting live stream info",
            [9066] = "New version available",
            [9067] = "Output file",
            [9068] = "Please see the actual text on the program page",
            [9069] = "Press <D> to download",
            [9070] = "<Esc> exit",
            [9071] = "<H> show help",
            [9072] = "<U> check for updates",
            [9073] = "Ready",
            [9074] = "Waiting",
            [9075] = "YouTube not responding",
            [9076] = "Resolutions",
            [9077] = "Saving",
            [9078] = "See file '{0}' on the desktop",
            [9079] = "Skipped! File already exists",
            [9080] = "Stream title",
            [9081] = "Unable to check for updates",
            [9082] = "Unable to verify the saved file is correct",
            [9083] = "Use <Up>, <Down>, <PageUp>, <PageDown> to scroll",

            [9100] = "[class Validator]",
            [9110] = "Check for duplicate arguments on command line input",
            [9111] = "Cannot read cookie file",
            [9112] = "Check command line input",
            [9113] = "Required argument '-url' not found",
            [9114] = "Check '-url' argument",
            [9115] = "Check '-start' argument",
            [9116] = "Check '-duration' argument",
            [9117] = "Check '-resolution' argument",
            [9118] = "Check '-ffmpeg' argument",
            [9119] = "Check '-output' argument",
            [9120] = "Check '-browser' argument",
            [9121] = "Check '-cookie' argument",

            [9200] = "[class Waiter]",
            [9210] = "Cannot get live stream information",
            [9211] = "This live stream ended too long ago, now it's a regular video",
            [9212] = "Cannot get channel information",
            [9213] = "Cannot get channel information. If URL contains '%', is it escaped?",
            [9214] = "Video unavailable",
            [9215] = "It's not a live stream",
            [9216] = "Saving copyrighted live streams is blocked",
            [9217] = "Seems to be a restricted live stream, try '-b' or '-c' option",
            [9218] = "For an upcoming stream, start point cannot be in the past",
            [9219] = "Cannot get live stream direct URL",
            [9220] = "Cannot get live stream information with browser",

            [9300] = "[class Preparer]",
            [9310] = "Cannot get live stream information",
            [9311] = "Cannot get live stream information with browser",
            [9312] = "Further parts of the stream are unavailable",
            [9313] = "Requested time interval isn't available",

            [9400] = "[class Saver]",
            [9410] = "FFmpeg not responding",
            [9411] = "Output file(s) creating error",
            [9412] = "Output folder creating error",
            [9413] = "Output file isn't completed, there will be a retry",

            [9500] = "[class Cache]",

            [9900] = "",
            [9999] = "Unknown error"
        };
        #endregion

        #region Itag - Description of formats ('itag' codes)
        // [itag] = "container;content;resolution/bitrate;other(3d,hdr,vr,etc.)"
        public static readonly Dictionary<int, string> Itag = new Dictionary<int, string>
        {
            [5] = "flv;audio+video;240p30;-",
            [6] = "flv;audio+video;270p30;-",
            [17] = "3gp;audio+video;144p30;-",
            [18] = "mp4;audio+video;360p30;-",
            [22] = "mp4;audio+video;720p30;-",
            [34] = "flv;audio+video;360p30;-",
            [35] = "flv;audio+video;480p30;-",
            [36] = "3gp;audio+video;180p30;-",
            [37] = "mp4;audio+video;1080p30;-",
            [38] = "mp4;audio+video;3072p30;-",
            [43] = "webm;audio+video;360p30;-",
            [44] = "webm;audio+video;480p30;-",
            [45] = "webm;audio+video;720p30;-",
            [46] = "webm;audio+video;1080p30;-",
            [82] = "mp4;audio+video;360p30;3d",
            [83] = "mp4;audio+video;480p30;3d",
            [84] = "mp4;audio+video;720p30;3d",
            [85] = "mp4;audio+video;1080p30;3d",
            [92] = "hls;audio+video;240p30;3d",
            [93] = "hls;audio+video;360p30;3d",
            [94] = "hls;audio+video;480p30;3d",
            [95] = "hls;audio+video;720p30;3d",
            [96] = "hls;audio+video;1080p30;-",
            [100] = "webm;audio+video;360p30;3d",
            [101] = "webm;audio+video;480p30;3d",
            [102] = "webm;audio+video;720p30;3d",
            [132] = "hls;audio+video;240p30;-",
            [133] = "mp4;video;240p30;-",
            [134] = "mp4;video;360p30;-",
            [135] = "mp4;video;480p30;-",
            [136] = "mp4;video;720p30;-",
            [137] = "mp4;video;1080p30;-",
            [138] = "mp4;video;2160p60;-",
            [139] = "m4a;audio;48k;-",
            [140] = "m4a;audio;128k;-",
            [141] = "m4a;audio;256k;-",
            [151] = "hls;audio+video;72p30;-",
            [160] = "mp4;video;144p30;-",
            [167] = "webm;video;360p30;-",
            [168] = "webm;video;480p30;-",
            [169] = "webm;video;1080p30;-",
            [171] = "webm;audio;128k;-",
            [218] = "webm;video;480p30;-",
            [219] = "webm;video;144p30;-",
            [242] = "webm;video;240p30;-",
            [243] = "webm;video;360p30;-",
            [244] = "webm;video;480p30;-",
            [245] = "webm;video;480p30;-",
            [246] = "webm;video;480p30;-",
            [247] = "webm;video;720p30;-",
            [248] = "webm;video;1080p30;-",
            [249] = "webm;audio;50k;-",
            [250] = "webm;audio;70k;-",
            [251] = "webm;audio;160k;-",
            [264] = "mp4;video;1440p30;-",
            [266] = "mp4;video;2160p60;-",
            [271] = "webm;video;1440p30;-",
            [272] = "webm;video;2880p30;-",
            [278] = "webm;video;144p30;-",
            [298] = "mp4;video;720p60;-",
            [299] = "mp4;video;1080p60;-",
            [302] = "webm;video;720p60;-",
            [303] = "webm;video;1080p60;-",
            [308] = "webm;video;1440p60;-",
            [313] = "webm;video;2160p30;-",
            [315] = "webm;video;2160p60;-",
            [330] = "webm;video;144p60;hdr",
            [331] = "webm;video;240p60;hdr",
            [332] = "webm;video;360p60;hdr",
            [333] = "webm;video;480p60;hdr",
            [334] = "webm;video;720p60;hdr",
            [335] = "webm;video;1080p60;hdr",
            [336] = "webm;video;1440p60;hdr",
            [337] = "webm;video;2160p60;hdr",
            [394] = "mp4;video;144p30;-",
            [395] = "mp4;video;240p30;-",
            [396] = "mp4;video;360p30;-",
            [397] = "mp4;video;480p30;-",
            [398] = "mp4;video;720p30;-",
            [399] = "mp4;video;1080p30;-",
            [400] = "mp4;video;1440p30;-",
            [401] = "mp4;video;2160p30;-",
            [402] = "mp4;video;2880p30;-",
        };
        #endregion

        #region Help - Program readme text
        public static readonly string Help = @"
# ee.Yrewind

Yrewind is a command line utility for saving YouTube live streams in original quality.

The program has the following features:

* Delayed start recording
* Recording in real time
* Downloading recently finished live streams
* Rewinding to the specified time point in the past, and downloading from that point
* Waiting for the scheduled live stream to start and then automatically recording from the first second
* Monitoring the specified channel for new live streams and then automatically recording from the first second



# Supported parameters

-url (-u)
-start (-s)
-duration (-d)
-resolution (-r)
-ffmpeg (-f)
-output (-o)
-browser (-b)
-cookie (-c)
-keepstreaminfo (-k)
-log (-l)
-executeonexit (-e)



# Links

Download: https://github.com/rytsikau/ee.Yrewind/releases/download/20230804/ee.yrewind_23.081.zip
Changelog: https://github.com/rytsikau/ee.Yrewind/blob/main/CHANGELOG.md
Report an issue: https://github.com/rytsikau/ee.Yrewind/issues
Previous versions: https://github.com/rytsikau/ee.Yrewind/releases
Program screenshot: https://github.com/rytsikau/ee.yrewind/raw/main/screenshot.png



# Quick start

1. Unpack the downloaded zip
2. Open *run.bat* in a text editor and paste the URLs of required streams instead of existing samples
3. Save *run.bat* and run it
* Some [examples](#examples)



# Usage

The only required command line argument is the `-url`:

-url=[url]

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



To rewind the livestream or delay the start of recording, use the `-start` parameter. It has several spellings:

-start=[YYYYMMDD:hhmm], -start=[YYYYMMDD:hhmmss]
-start=[Y:hhmm], -start=[Y:hhmmss]
-start=[T:hhmm], -start=[T:hhmmss]
-start=beginning, -start=b
-start=-[minutes]
-start=+[minutes]

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



The program also has several other parameters:

-duration=[minutes]
-duration=[minutes].[seconds]

Specifies the required duration. The minimum value is 0.01 (1 second), the maximum is limited to 300 (5 hours). If the parameter is missing, program uses the default 1 hour. Depending on technical parameters of the livestream, result duration may differ from the requested one by several seconds (in a larger direction). The examples below saves 300 minutes of the livestream:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -duration=300
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -duration=300.00
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -duration=max



-resolution=[heightPixels]

Specifies the required resolution in pixels (height). If this parameter is missing, program uses the default 1080. If the requested resolution is not available, the nearest lower will be selected. In the examples below, the livestream will be saved at 144p:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=144
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=200
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=1
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=min



-ffmpeg='c:\path\to\ffmpeg\'

Specifies the path to FFmpeg library. If relative path is specified, the base folder is the folder from which the command line was run. If this parameter is missing, Yrewind tries to find FFmpeg in its own folder and using environment variables.
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -ffmpeg='c:\Program Files\FFmpeg\'



-output='c:\dir1\dir2\filename.extension'

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

The next example saves the livestream as *d:\Streams\Sky News\2021-01-12_12-34.mkv*:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -output='d:\Streams\*author*\*start[yyyy-MM-dd_HH-mm]*'



-browser='c:\path\to\browser\file.exe'

Allows to use an alternative browser to get technical information about the livestream. For the portable version of browser, specify the full path to the executable file; for the installed version, it's usually enough to specify the name. Only Chromium-based browsers are supported - Chrome, Edge, Brave, Opera, Vivaldi, etc. If this parameter is missing, program uses pre-installed MS Edge. If this parameter is set to `false`, Yrewind will not use browser, but this may slow down the download speed.
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -browser='d:\Portable programs\Vivaldi\Application\vivaldi.exe'
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -browser=chrome



-cookie='c:\path\to\cookie\file.ext'

Specifies the path to the cookie file. If relative path is specified, the base folder is the folder from which the command line was run. The parameter can be useful if YouTube requires a captcha or authorization to access a livestream with age or membership restrictions. The cookie file must be in Netscape format and can be obtained using any suitable browser add-on. Please note that cookie created after solving captcha is usable for only a few hours. Instead of solving captcha, it's better to log in to YouTube and create cookie after that.
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -cookie='cookies.txt'



-keepstreaminfo=false

If this parameter is set to `false`, Yrewind will not keep technical information about the livestream in a temporary cache file, and will delete this file if it exists.
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -keepstreaminfo=false



-log=true

If this parameter is set to `true`, Yrewind will generate log files (in the folder from which the command line was run).
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -log=true



-executeonexit='c:\path\to\some\file.ext'

Specifies the command to run after Yrewind exits. If it's an executable file, you can also specify the arguments it supports (don't forget the quotes - nested supported). The non-executable file will be launched by the associated program. The parameter supports two rename masks - `*output*`, which contains the full path of the saved video, and `*getnext*`, which contains the command to start Yrewind again to get the next interval of the stream. When using `-executeonexit=*getnext*` command inside a batch file, keep in mind that this file is first executed to the end, and only then the `*getnext*` command is executed. Also use rename masks `*start*` and `*start[customDateTime]*` to avoid duplicate names of saved stream parts (or just use default auto-naming). In the first example below, the saved video will be opened by the associated program, in the second - using the specified program:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -executeonexit=*output*
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -executeonexit=''c:\Program Files\VLC mediaplayer\vlc.exe' *output*'



# Examples

Save 1 hour of the stream from 04:55AM to 05:55AM on May 5, 2020, at 720p, to specified folder:
>     yrewind -u=9Auq9mYxFEE -s=20200505:0455 -r=720 -o='d:\Streams\'

Save 89 minutes 30 seconds of the stream from today 10:45AM to 12:15PM, at 1080p:
>     yrewind -u=9Auq9mYxFEE -s=T:1045 -d=89.30

Record livestream until it ends, starting from the beginning, in `.ts` format, save result video to desktop:
>     yrewind -u=9Auq9mYxFEE -s=b -o=%UserProfile%\Desktop\.ts -e=*getnext*

Immediately play (without downloading) with assotiated mediaplayer, from yesterday 03:00AM, at the maximum available resolution:
>     yrewind -u=9Auq9mYxFEE -s=Y:0300 -r=max -o=.m3u -e=*output*

Wait for a new livestream on the specified channel, then start recording from the 30th minute:
>     yrewind -u=https://www.youtube.com/c/SkyNews -s=+30

Batch file example (runs 3 copies of the program at the same time):

@echo off
set O='d:\Streams\'
set B='c:\Program Files\Chrome\chrome.exe'
set F='c:\Program Files\FFmpeg\'
set Y='c:\Program Files\ee.Yrewind\'
cd /d %Y%

set U=9Auq9mYxFEE
start yrewind -f=%F% -b=%B% -o=%O% -u=%U% -s=Y:1500
start yrewind -f=%F% -b=%B% -o=%O% -u=%U% -s=Y:1600
start yrewind -f=%F% -b=%B% -o=%O% -u=%U% -s=Y:1700



# Notes

* Loss of packets on the streamer side causes the estimated time to shift. The offset is usually seconds, but if its internet connection is unstable and/or the stream has been running for a long time, it can be minutes or even hours. For example, if the stream was interrupted for a total of 1 hour, then 24-hour frames will be presented as 23-hour. Thus, start point time accuracy can only be guaranteed for the current moment. The further the livestream is rewound, the less accuracy. Also, if there are interruptions in the livestream at the specified time interval, the duration of the saved file will be shorter by the total duration of those interruptions; a warning for this incompleted file will be displayed
* Occasionally, the message `unable to verify the saved file is correct` appears. The reasons may be as follows: if the duration of the saved file cannot be verified (there is a possibility that the file is damaged); if the duration of the saved file does not match the requested one (also in this case, the output file name contains the word *INCOMPLETE*); if the starting point of the requested time interval cannot be accurately determined (for example due to server side error)
* To reduce the chance of output file corruption, it's better not to use Moov Atom formats (`.3gp`, `.mov`, `.mp4`, `.m4a`) for long recordings. Also, these formats don't allow to play a file that is in the process of downloading (other formats do)
* Recently finished livestream can be downloaded within approximately 6 hours of its completion. After this time, such stream 'turns' into a regular video and can be downloaded, for example, using Youtube-dl
* When using proxy, VPN or special firewall settings, keep in mind that not only Yrewind should have appropriate access, but also FFmpeg



# Terms of use

* This software provides access to information on the Internet that is publicly available and does not require authorization or authentication
* This software is free for non-commercial use and is provided 'as is' without warranty of any kind, either express or implied
* The author will not be liable for data loss, damages or any other kind of loss while using or misusing this software
* The author will not be liable for the misuse of content obtained using this software, including copyrighted, age-restricted, or any other protected content



# Developer info

* C#
* .NET Framework 4.8
* Visual Studio Community 2022



# Requirements

* FFmpeg static build (included in the archive)
* Windows 7 and on / Windows Server 2008 R2 and on
* Chromium-based browser



# Tested configuration

* FFmpeg 4.3 x86 (by Zeranoe)
* Windows 10 Pro x32
* Windows 10 Pro x64



# About

Console utility for saving YouTube live streams with rewind function up to 167 hours



# Tags

download downloader dvr live livestream record rewind save stream youtube

---
[program page](https://rytsikau.github.io/ee.Yrewind) [start page](https://rytsikau.github.io) [author e-mail](y.rytsikau@gmail.com)
";
        #endregion

        #region [misc] - Paths, URLs, internal presets, etc.

        // Program info
        public const string Name = "ee.Yrewind";
        public const int BuildDate = 230804;
        public static readonly string Version =
            Assembly.GetExecutingAssembly().GetName().Version.ToString();

        // Random substring for temp files naming
        public static readonly string RandomString =
            Guid.NewGuid().ToString().Replace("-", "").Replace("+", "").Substring(0, 7);

        // Cache file path
        public static readonly string PathCache =
            Path.GetTempPath() + Constants.Name.ToLower() + "_" + Constants.Version + ".cache";

        // Browser netlog path
        public static readonly string PathNetlog =
            Path.GetTempPath() + Constants.Name.ToLower() + "_" + Constants.RandomString + ".netlog";

        // URL of one-line file with info about latest release, content like:
        // 210520;https://github.com/rytsikau/ee.Yrewind/releases/download/20210520/ee.yrewind_21.051.zip
        public const string UrlUpdate =
            "https://raw.githubusercontent.com/rytsikau/ee.Yrewind/main/_updated.log";

        // Other URLs
        public const string UrlProxy = "http://localhost:7799/";
        public const string UrlMain = "https://www.youtube.com/";
        public const string UrlStream = "https://www.youtube.com/watch?v=[stream_id]";
        public const string UrlStreamCover = "https://img.youtube.com/vi/[stream_id]/0.jpg";
        public const string UrlStreamOembed =
            "https://www.youtube.com/oembed?url=http://youtube.com/watch?v=[stream_id]";
        public const string UrlChannel = "https://www.youtube.com/channel/[channel_id]";
        public const string UrlChannelCheck =
            "https://www.youtube.com/feeds/videos.xml?channel_id=[channel_id]";

        // Other constants
        public const int DurationMin = 1;
        public const int DurationMax = 18000;
        public const int DurationDefault = 3600;
        public const int ResolutionMin = 1;
        public const int ResolutionMax = 9999;
        public const int ResolutionDefault = 1080;
        public const int RealTimeBuffer = 60;
        public const int FfmpegConsoleWidthMin = 105;
        public const int FfmpegTimeout = 120;
        public const int CacheShelflifeMinutes = 100;
        public const int RewindMaxHours = 167;
        public const string BrowserDefault = "msedge";
        public const string OutputDirDefault = "saved_streams";
        public const string OutputExtDefault = ".mkv";
        public const string StartBeginning = "20010101:000000";

        #endregion
    }
}