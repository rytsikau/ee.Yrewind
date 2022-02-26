using System;
using System.Collections.Generic;
using System.Reflection;

namespace yrewind
{
    // Class for storing messages, readme text and various constants
    static class Constants
    {
        #region Msg - Messages
        public static readonly Dictionary<int, string> Msg = new Dictionary<int, string>
        {
            [9000] = "[class Program]",
            [9001] = "----------------------------------------------------------------",
            [9002] = "Press <H> to show help",
            [9003] = "Press <U> to get the latest version",
            [9004] = "Press <Esc> to exit",
            [9005] = "Press <D> to confirm download",
            [9006] = "Use <Up>, <Down>, <PageUp>, <PageDown> to scroll",
            [9007] = "Unable to check for updates",
            [9008] = "getting live stream info",
            [9009] = "Ready",
            [9010] = "Check '-output' argument",
            [9011] = "Command line input is too long",
            [9012] = "Please update the program",
            [9013] = "Checking",
            [9014] = "Requested time interval is not available",
            [9015] = "please see the actual text on the program page",
            [9016] = "New version available",
            [9017] = "Current version is actual",
            [9018] = "Delayed start is limited to 24 hours",
            [9019] = "unable to verify the saved file is correct",
            [9020] = "stream title",
            [9021] = "resolutions",
            [9022] = "output file",
            [9023] = "requested start",
            [9024] = "actually available",
            [9025] = "waiting",
            [9026] = "{0} (version {1})",
            [9027] = "--> {0} (started at {1})",
            [9028] = "--> OK (finished at {0})",
            [9029] = "--> ERROR {0}: {1} ({2})",
            [9030] = "see file '{0}' on the desktop",
            [9031] = "Downloading, please wait a minute",
            [9032] = "Error",
            [9033] = "Unable to check for updates",
            [9034] = "Skipped! File already exists",
            [9035] = "caching data, wait a minute",
            [9036] = "File already exists",
            [9037] = "saving",
            [9038] = "Unable to check for updates",

            [9100] = "[class CLInput]",
            [9110] = "Check '-url' argument",
            [9111] = "Check '-resolution' argument",
            [9112] = "Value of '-resolution' argument is out of range",
            [9113] = "Check for duplicate arguments on command line input",
            [9114] = "Check '-duration' argument",
            [9115] = "Value of '-duration' argument is out of range",
            [9116] = "Check '-start' argument",
            [9117] = "Check '-ffmpeg' argument",
            [9118] = "Cannot read cookie file",
            [9119] = "Unknown command line input format",
            [9120] = "Command line input contains unknown argument(s)",
            [9121] = "Required argument '-url' not found",
            [9122] = "FFmpeg not found",
            [9123] = "Check '-output' argument",
            [9124] = "Check '-output' argument",
            [9125] = "Check '-output' argument",
            [9126] = "Check '-cookie' argument",

            [9200] = "[class IDInfo]",
            [9210] = "Cannot get live stream information",
            [9211] = "Cannot get live stream information",
            [9212] = "Cannot get live stream information. Is it a live stream?",
            [9213] = "Cannot get channel information",
            [9214] = "Channel URL can only be specified with '-start=wait' option",
            [9215] = "Cannot get live stream information",
            [9216] = "Cannot get live stream information",
            [9217] = "Cannot get live stream information",
            [9218] = "Cannot get live stream information",
            [9219] = "Cannot get live stream information",
            [9220] = "Cannot get live stream information",
            [9221] = "URL not found. If it contains '%' characters, are they escaped?",
            [9222] = "Cannot get channel information",
            [9223] = "URL not found",
            [9224] = "Server displays captcha page. Use '-cookie' parameter",
            [9225] = "Cannot get channel information",
            [9226] = "Saving copyrighted live streams is blocked",
            [9227] = "Cannot get live stream information. Is it a live stream?",
            [9228] = "Cannot get live stream information",
            [9229] = "Cannot get live stream information",
            [9230] = "Cannot get live stream information",
            [9231] = "Cannot get live stream information",
            [9232] = "Cannot launch browser to get live stream information",
            [9233] = "Cannot get live stream information",
            [9234] = "Cannot get live stream information",
            [9235] = "Unable to access temporary data",
            [9236] = "Cannot get live stream information",
            [9237] = "Saving copyrighted live streams is blocked",
            [9238] = "Cannot get live stream information",

            [9300] = "[class Preparer]",
            [9310] = "Cannot get live stream information",
            [9311] = "Cannot get live stream information",
            [9312] = "Cannot get live stream information",
            [9313] = "Cannot get live stream information",

            [9400] = "[class GetSave]",
            [9410] = "FFmpeg not responding",
            [9411] = "Output file not created",
            [9412] = "Unable to access the saved file",
            [9413] = "Cannot create output folder",

            [9999] = "Unknown error"
        };
        #endregion

        #region Itag - Description of formats ('itag' codes)
        // [itag] = "container;content;resolution/bitrate;other(3d,hdr,vr,etc.)"
        public static readonly Dictionary<int, string> Itag = new Dictionary<int, string>
        {
            [5]   = "flv;audio+video;240p30;-",
            [6]   = "flv;audio+video;270p30;-",
            [17]  = "3gp;audio+video;144p30;-",
            [18]  = "mp4;audio+video;360p30;-",
            [22]  = "mp4;audio+video;720p30;-",
            [34]  = "flv;audio+video;360p30;-",
            [35]  = "flv;audio+video;480p30;-",
            [36]  = "3gp;audio+video;180p30;-",
            [37]  = "mp4;audio+video;1080p30;-",
            [38]  = "mp4;audio+video;3072p30;-",
            [43]  = "webm;audio+video;360p30;-",
            [44]  = "webm;audio+video;480p30;-",
            [45]  = "webm;audio+video;720p30;-",
            [46]  = "webm;audio+video;1080p30;-",
            [82]  = "mp4;audio+video;360p30;3d",
            [83]  = "mp4;audio+video;480p30;3d",
            [84]  = "mp4;audio+video;720p30;3d",
            [85]  = "mp4;audio+video;1080p30;3d",
            [92]  = "hls;audio+video;240p30;3d",
            [93]  = "hls;audio+video;360p30;3d",
            [94]  = "hls;audio+video;480p30;3d",
            [95]  = "hls;audio+video;720p30;3d",
            [96]  = "hls;audio+video;1080p30;-",
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

[ ` -url ` ]
[ ` -start ` ]
[ ` -duration ` ]
[ ` -resolution ` ]
[ ` -ffmpeg ` ]
[ ` -output ` ]
[ ` -cookie ` ]
[ ` -executeonexit ` ]

Yrewind is a command line utility to save YouTube live stream in its original quality. The program has the following features:

* Delayed start recording
* Recording in real time
* Rewinding to the specified time point in the past
* Rewinding to the first available time point in the past
* Waiting for the scheduled live stream to start and then automatically recording from the first second
* Monitoring the specified channel for new live streams and then automatically recording from the first second

The program also allows to set the required duration, resolution and media format. For a list of changes in new version, see the [changelog](https://github.com/rytsikau/ee.Yrewind/blob/main/CHANGELOG.md).

### [>> download version 22.011](https://github.com/rytsikau/ee.Yrewind/releases/download/20220121/ee.yrewind_22.011.zip)



## Screenshot

https://github.com/rytsikau/ee.yrewind/raw/main/screenshot.png



## Requirements

To get technical information about live stream, Yrewind uses a browser (Google Chrome or Microsoft Edge if Chrome is not installed)



## Quick start

1. Unpack the downloaded archive
2. Open *run.bat* in a text editor and paste the URLs of required streams instead of existing samples
3. Save *run.bat* and run it



## Usage

The only required command line argument is the `-url`:

-url=[url]

With this command, the program records a livestream in real time for 1 hour at 1080p resolution, or at a lower if 1080p is not available. URL can be specified in various formats:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE'
>     yrewind -url=https://www.youtu.be/9Auq9mYxFEE
>     yrewind -url=9Auq9mYxFEE
>     (etc.)

You can also specify a channel URL (along with parameter `-start=wait`) to automatically record if livestream URL and start time are unknown. Please note that when specifying a channel URL, active livestreams on the channel are ignored, the program will wait for a new one to start. The channel URL can also be specified in various formats:
>     yrewind -url='https://www.youtube.com/c/SkyNews' -start=wait
>     yrewind -url=www.youtube.com/user/SkyNews/ -start=wait
>     yrewind -url='youtube.com/channel/UCoMdktPbSTixAyNGwb-UYkQ' -start=wait
>     yrewind -url=UCoMdktPbSTixAyNGwb-UYkQ -start=wait



To rewind the livestream or delay the start of recording, use the `-start` parameter. It has several spellings:

-start=[YYYYMMDD:hhmm], -start=[YYYYMMDD:hhmmss]
-start=[Y:hhmm], -start=[T:hhmm]
-start=-[minutes], -start=+[minutes]
-start=beginning, -start=wait

This parameter specifies the point in time from which to save the stream. It is calculated relative to the moment the program was started (displayed in the first line). If the parameter is absent, the program records the livestream in real time.

To download the time interval from 7:10AM to 8:10AM on July 15, 2020:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=20200715:0710

To download the time interval from yesterday 10:15PM to 11:15PM:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=Y:2215

To download the time interval from today 02:00AM to 03:00AM:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=T:0200

To download the time interval from 3 hours ago to 2 hours ago:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=-180

To record 1 hour of livestream with a 2 hour delay:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=+120

To download the time interval from the first currently available moment:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=beginning

To record the scheduled livestream from the first second when it starts:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=wait



The program also has several other parameters:

-duration=[minutes]

Specifies the required duration in minutes. The minimum value is 1, the maximum is limited to 300. If the parameter is missing, the program uses the default 60. The example below saves 15 minutes of the livestream:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -duration=15



-resolution=[heightPixels]

Specifies the required resolution in pixels (height). If this parameter is missing, the program uses the default 1080. If the requested resolution is not available, the program uses the nearest lower. In the examples below, the livestream will be saved at 480p:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=500
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=480



-ffmpeg='c:\path\to\ffmpeg\'

Specifies the path to FFmpeg library. If a relative path is specified, the base folder is the folder from which the command line was run. If this parameter is missing, Yrewind uses FFmpeg located in its folder.
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -ffmpeg='c:\Program Files\FFmpeg\'



-output='c:\dir1\dir2\filename.extension'

Specifies custom folder, filename and extension (media container format) for the saved livestream. If a relative path is specified, the base folder is the folder from which the command line was run. If this parameter is missing, the program uses the default values:
* `[batch file folder]\saved_streams\` - for folder
* `[id]_[date]-[time]_[duration]_[resolution]` - for filename
* `.mp4` - for extension

The `-output` parameter can be specified partially, then the missing parts are replaced with the default values. In this case, the part of the string to the right of the last slash is interpreted as filename and/or extension. If the string does not contain slashes, it's fully interpreted as filename and/or extension:
* `c:\dir1\dir2\` - custom folder, default filename, default extension
* `dir1\filename` - custom subfolder, custom filename, default extension
* `dir1\.extension` - custom subfolder, default filename, custom extension
* `filename` - default folder, custom filename, default extension
* `.extension` - default folder, default filename, custom extension

Folder and filename supports renaming masks: `*id*`, `*start*`, `*start[customDateTime]*` (recognizes letters yyyyMMddHHmmss), `*duration*`, `*resolution*`, `*channel_id*`, `*author*` and `*title*`.

The extension defines the format of the media container in which the livestream will be saved. Formats description:
* `.avi`, `.mp4` - use AVC and MP4a data (if AVC is unavailable, use VP9)
* `.asf`, `.mkv`, `.wmv` - use VP9 and MP4a data (if VP9 is unavailable, use AVC)
* `.3gp`, ` .flv`, ` .mov`, `.ts` - use AVC and MP4a data (does not support 1080+ resolutions - saves at 1080p even if requested higher resolution is available)
* `.aac`, `.m4a`, `.wma` - use MP4a data (saves audio only)

The example below saves the livestream as *\saved_streams\9Auq9mYxFEE_20210123-185830_060m_1080p.ts*:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -output=.ts

The next example saves the livestream as *d:\My saved streams\Sky News\2021-01-12_12-34.mp4*:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -output='d:\My saved streams\*author*\*start[yyyy-MM-dd_HH-mm]*'



-cookie='c:\path\to\cookie\file.ext'

Specifies the path to the cookie file. If a relative path is specified, the base folder is the folder from which the command line was run. The parameter is required if YouTube displays captcha page. Cookie file can be obtained using a browser add-on, or, alternatively, cookie string can be copied directly from the browser developer console and saved as a text file. Please note, that cookie obtained as a result of solving captcha is usable for only a few hours. Possibly, instead of solving captcha, it's better to log in to YouTube and create cookie file containing the login data.
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -cookie='cookies.txt'



-executeonexit='c:\path\to\some\file.ext'

Specifies the command to run after Yrewind exits. If it's an executable file, you can also specify the arguments it supports (don't forget the quotes - nested supported). The non-executable file will be launched by the associated program. The parameter supports two rename masks - `*output*`, which contains the full path of the saved video, and `*getnext*`, which contains the command to start Yrewind again to get the next interval of the stream. When using `-executeonexit=*getnext*` command inside a batch file, keep in mind that this file is first executed to the end, and only then the `*getnext*` command is executed. Also use rename masks `*start*` and `*start[customDateTime]*` to avoid duplicate names of saved stream parts (or just use default auto-naming). In the first example below, the saved video will be opened by the associated program, in the second - using the specified program:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -executeonexit=*output*
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -executeonexit=''c:\Program Files\VLC mediaplayer\vlc.exe' *output*'



**More examples:**

To save 15 minutes of the stream from yesterday 10:45AM to 11:00AM, at 1080p:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=Y:1045 -duration=15

To save 1 hour of the stream from 04:55AM to 05:55AM on May 5, 2020, at 720p, to specified folder:
>     yrewind -url=9Auq9mYxFEE -start=20200505:0455 -resolution=720 -output='d:\My saved streams\'

To save 90 minutes of the stream, starting from half an hour ago, at the highest available resolution:
>     yrewind -u=9Auq9mYxFEE -s=-30 -d=90 -r=9999



## Notes

* All arguments and keywords can be replaced with single-character aliases: `-url` with `-u`, `-start` with `-s`, `beginning` with `b`, etc. This does not apply to rename `*masks*`
* Loss of packets on the streamer side causes the estimated time to shift. The offset is usually seconds, but if its internet connection is unstable and/or the stream has been running for a long time, it can be minutes or even hours. For example, if the stream was interrupted for a total of 1 hour, then 24-hour frames will be downloaded as 23-hour. Thus, start point time accuracy can only be guaranteed for the current moment. The farther the livestream is rewound, the greater the probability of an error. Also, if there are interruptions in the livestream at the specified time interval (this often happens at the beginning of the stream), the duration of the saved file will be shorter by the total duration of those interruptions; a warning for this incompleted file will be displayed
* Most media formats allows to watch livestream while it is downloading
* Occasionally, the message `unable to verify the saved file is correct` appears. The reasons may be as follows: if the duration of the saved file cannot be verified (there is a possibility that the file is damaged); if the duration of the saved file does not match the requested one (also in this case, the output file name contains the word *INCOMPLETE*); if the starting point of the requested time interval cannot be accurately determined (for example due to server side error)
* To reduce the likelihood of output file corruption during live recording (without rewind function), it is better not to use Moov Atom formats - `.3gp`, `.m4a`, `.mov`, `.mp4`
* When using proxy, VPN or special firewall settings, keep in mind that not only Yrewind should have appropriate access, but also FFmpeg



## Terms of use

* This software provides access to information on the Internet that is publicly available and does not require authorization or authentication
* This software is free for non-commercial use and is provided 'as is' without warranty of any kind, either express or implied
* The author will not be liable for data loss, damages or any other kind of loss while using or misusing this software
* The author will not be liable for the misuse of content obtained using this software, including copyrighted, age-restricted, or any other protected content



## Developer info

* C#
* .NET Framework 4.5
* Visual Studio Community 2019



## Requirements

* FFmpeg static build (included in the archive)
* Windows 7 and on / Windows Server 2008 and on



## Tested configuration

* FFmpeg 4.3 x86 (by Zeranoe)
* Windows 10 Pro x32
* Windows 10 Pro x64



## Tags

download downloader dvr live livestream record rewind save stream youtube



## About

Records YouTube live stream that's in progress, has the ability to rewind up to 168 hours

---
[program page](https://rytsikau.github.io/ee.Yrewind) [start page](https://rytsikau.github.io) [author e-mail](y.rytsikau@gmail.com)
        ";
        #endregion

        #region [misc] - Paths, URLs, internal presets, etc.
        // Build info
        public const string Name = "ee.Yrewind";
        public static readonly string Version =
            Assembly.GetExecutingAssembly().GetName().Version.ToString();
        public const int BuildDate = 220121; // YYMMDD
        public const int ExpiryDate = 221201; // YYMMDD

        // Random substring for temp files naming
        public static readonly string RandomString = Guid.NewGuid().ToString()
            .Replace("-", "").Replace("+", "").Substring(0, 7);

        // Online one-line file with info about latest release
        public const string UpdateInfoUrl =
            "https://raw.githubusercontent.com/rytsikau/ee.Yrewind/main/_updated.log";
        // Format:
        // [_]YYMMDD;directUri (optional '_' blocks outdated versions)
        // Example:
        // _201128;https://github.com/rytsikau/ee.Yrewind/releases/download/20210520/ee.yrewind_21.051.zip

        // Other values
        public const int DurationMin = 1;
        public const int DurationMax = 300;
        public const int DurationDefault = 60;
        public const int ResolutionMin = 1;
        public const int ResolutionMax = 9999;
        public const int ResolutionDefault = 1080;
        public const string UrlProxy = "http://localhost:7799/";
        public const string UrlChannel =
            "https://www.youtube.com/channel/[channel_id]";
        public const string UrlChannelCheck =
            "https://www.youtube.com/feeds/videos.xml?channel_id=[channel_id]";
        public const string UrlStream =
            "https://www.youtube.com/watch?v=[stream_id]";
        public const string UrlStreamCover =
            "https://img.youtube.com/vi/[stream_id]/0.jpg";
        public const string UrlStreamInfo =
            "https://www.youtube.com/get_video_info?video_id=[stream_id]&el=";
        public const string UrlStreamInfoOembed =
            "https://www.youtube.com/oembed?url=http://youtube.com/watch?v=[stream_id]";
        public const int NetworkAttemptsNumber = 6;
        public const int NetworkAttemptDelaySeconds = 6;
        public const int RealTimeBufferSeconds = 55;
        public const int FfmpegConsoleWidthMin = 105;
        public const int CommandLineStringLengthLimit = 8191;
        public const int CookieFileLengthMax = 100000;
        public const string OutputDirDefault = "saved_streams";
        public const string OutputExtDefault = ".mp4";
        #endregion
    }
}