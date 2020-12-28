# ee.Yrewind

Yrewind is a command line program to save any YouTube live stream as a video file. The program can both record a stream in real time and download it, starting from a specified moment in the past. Also, Yrewind allows to set the required duration and resolution. Please note that the program can only save videos when streaming is active, and is not intended to download old recorded streams as well as regular YouTube videos.

### [>> download version 20.123](https://github.com/rytsikau/ee.yrewind/raw/main/ee.yrewind_20.123.zip)
For a list of changes in new version, see the [changelog](https://github.com/rytsikau/ee.Yrewind/blob/main/CHANGELOG.md)<br><br>


<br>


## Program screenshot

<img src='https://github.com/rytsikau/ee.yrewind/raw/main/screenshot.png'>


<br>


## Quick Start

1. Unpack the downloaded archive
2. Open *run.bat* in a text editor and paste the URLs of required streams instead of existing samples
3. Save *run.bat* and run it


<br>


## Usage info

**The only required command line argument is the `-url`:**

**` -url=[url] `**

With this command, the program runs in real time mode, recording the livestream for 1 hour at the highest possible resolution. URL can be specified in various formats:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE'
>     yrewind -url=https://www.youtu.be/9Auq9mYxFEE
>     yrewind -url=9Auq9mYxFEE
>     (etc.)


<br>**To enable rewind, use `-start` parameter:**

**` -start=[YYYYMMDD:hhmm], -start=[Y:hhmm], -start=[T:hhmm], -start=-[minutes] `**

The parameter specifies the point in time in the past from which the stream will be saved. It can be written in various formats. For example, to save the time interval from 7:10AM to 8:10AM on July 15, 2020:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=20200715:0710

To save the time interval from yesterday 10:15PM to 11:15PM:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=Y:2215

To save the time interval from today 02:00AM to 03:00AM:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=T:0200

To save the time interval from 3 hours ago to 2 hours ago:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=-180


<br>**The program also has several other parameters:**

**` -duration=[minutes] `**

Specifies the required duration in minutes. The minimum value is 1, the maximum is limited to 90. If this parameter is missing, the program uses the default value of 60. The example below saves 15 minutes of the livestream:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -duration=15


<br>**` -resolution=[heightPixels] `**

Specifies the required resolution in pixels (height). If the specified resolution is not available, the program uses the closest lower. If this parameter is missing, the program uses the default 1080. In the examples below, the livestream will be saved at 480p:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=480
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=500


<br>**` -vformat=[formatExtension] `**

Sets the required format (container) for the saved video. By default, program uses `mp4`:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -vformat=ts


<br>**` -pathchrome='C:\path\to\chrome\' `**

Specifies the path to the Google Chrome browser. This parameter is required when using the portable version of Chrome.


<br>**` -pathffmpeg='C:\path\to\ffmpeg\' `**

Specifies the path to the FFmpeg library. This parameter is required if *ffmpeg.exe* is not present in the program folder.


<br>**` -pathsave='D:\path\to\save\streams\' `**

Specifies an alternate path to save streams. If this parameter is missing, the program saves the video to the directory where the batch file is located (or from which the command line is launched).


<br>**More examples:**

To save 15 minutes of the stream from yesterday 10:45AM to 11:00AM, at the highest available resolution:
>     yrewind -url='youtube.com/v/9Auq9mYxFEE?fs=1&rel=0' -start=Y:1045 -duration=15

To save 1 hour of the stream from 04:55AM to 05:55AM on May 5, 2020, at 720p, to specified directory:
>     yrewind -url=9Auq9mYxFEE -start=20200505:0455 -resolution=720 -pathsave='D:\Saved_streams\'

To save 90 minutes of the stream, starting from half an hour ago, at 480p resolution:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=-30 -duration=90 -resolution=500


<br>


## Other info

* Loss of packets on the streamer side causes the estimated time to shift. The offset is usually seconds, but if the internet connection is unstable and/or the stream has been running for a long time, it can be minutes or even hours. For example, if the broadcast was interrupted for a total of 1 hour, then 24-hour frames will be downloaded as 23-hour
* The reference point for the `-start` parameter is the local time when the program started: *--> ee.Yrewind (started at ...)*
* To determine the earliest available point, try download the knowingly unavailable time interval (for example, `-start=19990101:0000`). The program will show a warning indicating the earliest available point at the moment
* To prevent video file corruption due to network errors or FFmpeg crashes, use `ts` container to save video: `-vformat=ts` (unfortunately it does not currently support resolutions higher than 1080p and embedded metadata)


<br>


## Requirements

* FFmpeg static build (included in the archive)
* Chrome 60 and on (installed or portable)
* Windows 7 and on / Windows Server 2008 and on


<br>


## Tested Configuration

* FFmpeg 4.3 x86 (by Zeranoe)
* Chrome 62 x86 (portable)
* Chrome 83 x64 (installed)
* Windows 10 Pro x32 version 1909
* Windows 10 Pro x64 version 1903


<br>


## Developer info

* C#
* .NET Framework 4.5
* Visual Studio Community 2019


<br>


## Tags

download dvr live livestream record rewind save stream youtube


<br>


## Author

[(c) Yauheni Rytsikau, 2020](mailto:y.rytsikau@gmail.com)

---
[[program page]](https://rytsikau.github.io/ee.Yrewind) [[start page]](https://rytsikau.github.io)
