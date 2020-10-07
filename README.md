# ee.Yrewind
Yrewind is a command-line program to download the required time interval of any YouTube active livestream - even if this interval was many hours ago. So this is not a 'current time' recorder - Yrewind allows you to 'rewind' the livestream as many hours back as necessary. Please note that Yrewind can only download a livestream while it's streaming; and is not intended for downloading recorded livestreams as well as 'regular' YouTube videos

### [>> download version 20.102](https://github.com/rytsikau/ee.yrewind/raw/main/ee.yrewind_20.102.zip)


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


<br>**Also, the program has several optional parameters:**

    yrewind -url=[url] -resolution=[pixels]

Specifies the required resolution in pixels. The minimum value is 0, the maximum limited to 9999. If the specified resolution is not available, the program will use the closest lower. If parameter is absent, the program will use the default value of 9999 (i.e. livestream will be saved at the highest possible resolution). In the following examples, the livestream will be saved at 144p:
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -resolution=0
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -resolution=144
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -resolution=150


    yrewind -url=[url] -start=[YYYYMMDD:hhmm]

Saves the stream from the specified starting point. Date digits can be replaced with letter T (means 'today') or Y (means 'yesterday'). If parameter is absent, the program will save the specified number of minutes (see '-duration' parameter) up to the moment the program was started. In the following first example, the program will save the time interval from 7:10 am on July 15, 2020 to 8:10 am on July 15, 2020. In the second example, the program will save the time interval from yesterday 11:15 pm to today at 12:15 am:
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -start=20200715:0710
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -start=Y:2315


    yrewind -url=[url] -duration=[minutes]

Specifies the required duration in minutes. The minimum value is 1, the maximum is limited to 60. If parameter is absent, the program will use the default value of 60. The following example will save the last 15 minutes of the live stream up to the moment the program was started:
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -duration=15


    yrewind -url=[url] -pathchrome='C:\path\to\chrome\'

Specifies the path to the Google Chrome browser. This parameter is required when using the portable version of Chrome


    yrewind -url=[url] -pathffmpeg='C:\path\to\ffmpeg\'

Specifies the path to the FFmpeg library. This parameter is required if 'ffmpeg.exe' is not present in the program folder


    yrewind -url=[url] -pathsave='D:\path\to\saved\streams\'

Specifies an alternate path for saving streams. If this parameter is absent, the program will save the streams in its own directory


<br>**More examples:**

To save the last 30 minutes of the stream at the highest available resolution:
>     yrewind -url=youtube.com/watch?v=9Auq9mYxFEE -duration=30 -resolution=9999

To save 10 minutes of the stream from yesterday 11:55 pm to today 12:05 am, at the highest available resolution:
>     yrewind -url='youtube.com/v/9Auq9mYxFEE?fs=1&rel=0' -start=Y:2355 -duration=10

To save 1 hour of the stream from 04:55 am to 05:55 am on May 5, 2020, at 720p, to specified directory:
>     yrewind -url=9Auq9mYxFEE -start=20200505:0455 -resolution=720 -pathsave='D:\Saved_streams\'


## Other info
* Loss of packets on the streamer side leads to a shift in the estimated time. Usually the offset is in seconds, but if the internet connection is unstable and/or the stream started a long time ago, it will be minutes or even hours
* The last 7 days (~168 hours) of any livestream are usually available for download
* The maximum possible resolution is 1080p, the program does not support higher resolutions


## Known issues
* Some livestreams (usually 60+ FPS) may cause the error 'Cannot process livestream with FFmpeg library (code 9411)'. In this case, you must run the program with no arguments and select the <C> option to clear the cache. Then try again


## Requirements
* FFmpeg executable library (included in the archive)
* Google Chrome 60+ (installed or portable)
* Microsoft Windows XP or later / Microsoft Windows Server 2003 or later


## Tested Configuration
* FFmpeg 4.3 x86 ([Zeranoe static build](https://ffmpeg.zeranoe.com/builds))
* Google Chrome 62 x86 (portable)
* Google Chrome 83 x64 (installed)
* Microsoft Windows 10 Pro x32 version 1909
* Microsoft Windows 10 Pro x64 version 1903


## Developer info
* C#
* .NET Framework 4.0
* Visual Studio Community 2019


## Tags
youtube-dvr youtube-live youtube-live-stream youtube-live-stream-download youtube-live-stream-dvr youtube-live-stream-record youtube-live-stream-rewind youtube-live-stream-save youtube-livestream youtube-livestream-download youtube-livestream-dvr youtube-livestream-record youtube-livestream-rewind youtube-livestream-save youtube-record youtube-rewind youtube-stream


## Author
[(c) Yauheni Rytsikau, 2020](mailto:y.rytsikau@gmail.com)

---
[[program page]](https://rytsikau.github.io/ee.yrewind) [[start page]](https://rytsikau.github.io)
