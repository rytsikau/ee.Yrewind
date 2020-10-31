# ee.Yrewind

Yrewind is a command line program to download specified past portion of any YouTube live stream. It can rewind a live stream the desired number of hours back and save the result as a video file. Note that Yrewind can only work when live streaming is active; the program is not intended for downloading recorded live streams, as well as regular YouTube videos

### [>> download version 20.105](https://github.com/rytsikau/ee.yrewind/raw/main/ee.yrewind_20.105.zip)



## Program screenshot

<img src='https://github.com/rytsikau/ee.yrewind/raw/main/screenshot.png'>



## Quick Start

1. Unpack the downloaded archive
2. Open 'start.bat' in a text editor, and insert URLs of required streams instead of existing samples
3. Save 'start.bat', and run it



## Usage info

<br>**The only required command line argument is the '-url':**

    yrewind -url=[url]

With this command, the program saves the last 1 hour of the required livestream at the highest possible resolution. URL can be specified in various formats:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE'
>     yrewind -url=https://www.youtu.be/9Auq9mYxFEE
>     yrewind -url=9Auq9mYxFEE
>     etc.



<br>**Also the program has several optional parameters:**

    -resolution=[pixels]

Specifies the required resolution in pixels (height). If parameter is absent, the program will use the default value of 1080. If the specified (or default) resolution is not available, the program will use the closest lower. In the following examples, the livestream will be saved at 480p:
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -resolution=480
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -resolution=500



    -start=[YYYYMMDD:hhmm]

Saves the stream from the specified starting point. Date digits can be replaced with letter T (means 'today') or Y (means 'yesterday'). If parameter is absent, the program will save the specified number of minutes (see '-duration' parameter) up to the moment the program was started. In the following first example, the program will save the time interval from 7:10AM to 8:10AM on July 15, 2020. In the second example, the program will save the time interval from yesterday 11:15PM to today 12:15AM:
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -start=20200715:0710
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -start=Y:2315



    -duration=[minutes]

Specifies the required duration in minutes. The minimum value is 1, the maximum is limited to 60. If parameter is absent, the program will use the default value of 60. The following example will save the last 15 minutes of the livestream up to the moment the program was started:
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -duration=15



    -pathchrome='C:\path\to\chrome\'

Specifies the path to the Google Chrome browser. This parameter is required when using the portable version of Chrome



    -pathffmpeg='C:\path\to\ffmpeg\'

Specifies the path to the FFmpeg library. This parameter is required if 'ffmpeg.exe' is not present in the program folder



    -pathsave='D:\path\to\saved\streams\'

Specifies an alternate path for saving streams. If this parameter is absent, the program will save the streams to the directory where the batch file is located (or from which the command line is launched)



    -nocache=true

This parameter disables the use of the program cache



<br>**More examples:**

To save the last 30 minutes of the stream at the highest available resolution:
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -duration=30 -resolution=9999

To save 10 minutes of the stream from yesterday 11:55PM to today 12:05AM, at the highest available resolution:
>     yrewind -url='youtube.com/v/9Auq9mYxFEE?fs=1&rel=0' -start=Y:2355 -duration=10

To save 1 hour of the stream from 04:55AM to 05:55AM on May 5, 2020, at 720p, to specified directory:
>     yrewind -url=9Auq9mYxFEE -start=20200505:0455 -resolution=720 -pathsave='D:\Saved_streams\'



## Other info

* If the filename of downloaded video contains a sync warning, the video file may have errors. In this case, try downloading later or shift/change the time interval
* Loss of packets on the streamer side leads to a shift in the estimated time. Usually the offset is in seconds, but if the internet connection is unstable and/or the stream started a long time ago, it will be minutes or even hours. For example, if the stream was interrupted for 1 hour, then 24-hour frames will be downloaded as 23-hour
* The endpoint of the requested time interval cannot be in the future relative to the time when the program was started
* The maximum possible resolution supported by the program is 1080p
* To determine the earliest available starting point, try download the knowingly unavailable time interval (for example, '-start=19990101:0000'). The program will show a warning indicating the earliest available point at the moment. For long livestreams, the last 7 days are usually available (~168 hours)



## Known issues

* Some livestreams (usually 60+ FPS) may cause the error 'Cannot process livestream with FFmpeg library (code 9411)'. In this case, run the program without arguments and select option to clear the cache, or add '-nocache=true' parameter to the command line



## Requirements

* FFmpeg static build (included in the archive)
* Google Chrome 60+ (installed or portable)
* Microsoft Windows 7 and on / Microsoft Windows Server 2008 and on



## Tested Configuration

* FFmpeg 4.3 x86 (by Zeranoe)
* Google Chrome 62 x86 (portable)
* Google Chrome 83 x64 (installed)
* Microsoft Windows 10 Pro x32 version 1909
* Microsoft Windows 10 Pro x64 version 1903



## Developer info

* C#
* .NET Framework 4.0
* Visual Studio Community 2019



## Tags

youtube-live-stream-download youtube-live-stream-record youtube-live-stream-save youtube-livestream-download youtube-livestream-record youtube-livestream-save



## Author

[(c) Yauheni Rytsikau, 2020](mailto:y.rytsikau@gmail.com)

---
[[program page]](https://rytsikau.github.io/ee.Yrewind) [[start page]](https://rytsikau.github.io)
