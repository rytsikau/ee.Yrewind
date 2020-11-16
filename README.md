# ee.Yrewind

Yrewind is a command line program to download specified past portion of any YouTube live stream. It can rewind a live stream the desired number of hours back and save the result as a video file. Please note that the program can only save videos when streaming is active, and is not intended to download recorded live streams as well as regular YouTube videos.

### [>> download version 20.111](https://github.com/rytsikau/ee.yrewind/raw/main/ee.yrewind_20.111.zip)<br><br><br>



## Program screenshot

<img src='https://github.com/rytsikau/ee.yrewind/raw/main/screenshot.png'>



## Quick Start

1. Unpack the downloaded archive
2. Open *start.bat* in a text editor and paste the URLs of required streams instead of existing samples
3. Save *start.bat* and run it



## Usage info

The only required command line argument is the '-url':

<br>**` -url=[url] `**

With this command, the program saves the last 1 hour of the required livestream at the highest possible resolution. URL can be specified in various formats:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE'
>     yrewind -url=https://www.youtu.be/9Auq9mYxFEE
>     yrewind -url=9Auq9mYxFEE
>     (etc.)



Also the program has the following optional parameters:

<br>**` -resolution=[pixels] `**

Specifies the required resolution in pixels (height). If the specified resolution is not available, the program uses the closest lower. In the following examples, the livestream will be saved at 480p:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE' -resolution=480
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE' -resolution=500

* If this parameter is missing, the program uses the default 1080



<br>**` -start=[YYYYMMDD:hhmm], -start=[Y:hhmm], -start=[T:hhmm], -start=-[minutes] `**

Specifies the point in time in the past from which the stream will be saved. The value can be written in various formats. The following example saves the time interval from 7:10AM to 8:10AM on July 15, 2020:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE' -start=20200715:0710

To save the time interval from yesterday 10:15PM to 11:15PM:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE' -start=Y:2215

To save the time interval from today 02:00AM to 03:00AM:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE' -start=T:0200

To save the time interval from 3 hours ago to 2 hours ago:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE' -start=-180

* If this parameter is missing, the program saves the specified number of the last minutes of the livestream (see '-duration' parameter)



<br>**` -duration=[minutes] `**

Specifies the required duration in minutes. The minimum value is 1, the maximum is limited to 60. The following example saves the last 15 minutes of the livestream:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE' -duration=15

* If this parameter is missing, the program uses the default value of 60



<br>**` -pathchrome='C:\path\to\chrome\' `**

Specifies the path to the Google Chrome browser. This parameter is required when using the portable version of Chrome.



<br>**` -pathffmpeg='C:\path\to\ffmpeg\' `**

Specifies the path to the FFmpeg library. This parameter is required if *ffmpeg.exe* is not present in the program folder.



<br>**` -pathsave='D:\path\to\save\streams\' `**

Specifies an alternate path to save streams. If this parameter is missing, the program saves the video to the directory where the batch file is located (or from which the command line is launched).



<br>**` -nocache=true `**

This parameter disables the use of the program cache.



<br>**More examples:**

To save the last 30 minutes of the stream at the highest available resolution:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE' -duration=30 -resolution=9999

To save 15 minutes of the stream from yesterday 10:45AM to 11:00AM, at the highest available resolution:
>     yrewind -url='youtube.com/v/9Auq9mYxFEE?fs=1&rel=0' -start=Y:1045 -duration=15

To save 1 hour of the stream from 04:55AM to 05:55AM on May 5, 2020, at 720p, to specified directory:
>     yrewind -url='9Auq9mYxFEE' -start=20200505:0455 -resolution=720 -pathsave='D:\Saved_streams\'



## Other info

* If the file name of downloaded video contains a sync warning (for example *9Auq9mYxFEE_20201101-0730_45m_0480p (sync warning).mp4*), the file may be corrupted. In this case, try downloading later or shift/change the time interval
* Loss of packets on the streamer side causes the estimated time to shift. The offset is usually seconds, but if the internet connection is unstable and/or the stream has been running for a long time, it can be minutes or even hours. For example, if the broadcast was interrupted for a total of 1 hour, then 24-hour frames will be downloaded as 23-hour
* The endpoint of the requested time interval cannot be in the future relative to the time when the program was started
* The maximum resolution supported by the program is 1080p
* To determine the earliest available point, try download the knowingly unavailable time interval (for example, '-start=19990101:0000'). The program will show a warning indicating the earliest available point at the moment



## Known issues

* Some livestreams (usually 60 fps) may cause the error *Can't process livestream with FFmpeg library (code 9411)*. In this case, run the program without arguments and select option to clear the cache, or add '-nocache=true' parameter to the command line



## Requirements

* FFmpeg static build (included in the archive)
* Chrome 60 and on (installed or portable)
* Windows 7 and on / Windows Server 2008 and on



## Tested Configuration

* FFmpeg 4.3 x86 (by Zeranoe)
* Chrome 62 x86 (portable)
* Chrome 83 x64 (installed)
* Windows 10 Pro x32 version 1909
* Windows 10 Pro x64 version 1903



## Developer info

* C#
* .NET Framework 4.0
* Visual Studio Community 2019



## Tags

download dvr live livestream record rewind save stream youtube



## Author

[(c) Yauheni Rytsikau, 2020](mailto:y.rytsikau@gmail.com)

---
[[program page]](https://rytsikau.github.io/ee.Yrewind) [[start page]](https://rytsikau.github.io)
