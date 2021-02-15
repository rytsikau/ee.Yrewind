# ee.Yrewind

[ ` -url ` ](#-urlurl)
[ ` -start ` ](#-startyyyymmddhhmm--startyyyymmddhhmmss)
[ ` -duration ` ](#-durationminutes)
[ ` -resolution ` ](#-resolutionheightpixels)
[ ` -vformat ` ](#-vformatmediacontainerextension)
[ ` -pathffmpeg ` ](#-pathffmpegcpathtoffmpeg)
[ ` -pathsave ` ](#-pathsavedpathtosavestreams)
[ ` -filename ` ](#-filenamefilenamewithoutextension)

Yrewind is a command line utility to save YouTube live stream in its original quality. The program has the following features:

* Delayed start recording
* Recording the live stream in real time
* Rewinding the live stream to a specified point in the past
* Rewinding the live stream to the first available point in the past
* Waiting for the scheduled live stream to start and then automatically recording from the first second
* Monitoring the specified channel for new live streams and then automatically recording from the first second

Yrewind also allows to set the required duration, resolution and media format. Please note that the program can only save when the live stream is active. For a list of changes in new version, see the [changelog](https://github.com/rytsikau/ee.Yrewind/blob/main/CHANGELOG.md).

### [>> download version 21.021](https://github.com/rytsikau/ee.Yrewind/releases/download/20210215/ee.yrewind_21.021.zip)

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

The only required command line argument is the `-url`:

##### [**` -url=[url] `**](#)

With this command, the program records a livestream in real time for 1 hour at 1080p resolution (or at a lower if 1080p is not available). URL can be specified in various formats:
>     yrewind -url='youtube.com/watch?v=9Auq9mYxFEE'
>     yrewind -url=https://www.youtu.be/9Auq9mYxFEE
>     yrewind -url=9Auq9mYxFEE
>     (etc.)

You can also specify a channel URL (along with parameter `-start=wait`) to automatically record if livestream URL and start time are unknown. Please note that when specifying a channel URL, active livestreams on the channel are ignored, the program will wait for a new one to start.
>     yrewind -url='https://www.youtube.com/c/SkyNews' -start=wait
>     yrewind -url=www.youtube.com/user/SkyNews/ -start=wait
>     yrewind -url='youtube.com/channel/UCoMdktPbSTixAyNGwb-UYkQ' -start=wait
>     yrewind -url=UCoMdktPbSTixAyNGwb-UYkQ -start=wait

<br>

To rewind the livestream or delay the start of recording, use the `-start` parameter. It has several spellings:

##### [**` -start=[YYYYMMDD:hhmm], -start=[YYYYMMDD:hhmmss] `**](#)
##### [**` -start=[Y:hhmm], -start=[T:hhmm] `**](#)
##### [**` -start=-[minutes], -start=+[minutes] `**](#)
##### [**` -start=beginning, -start=wait `**](#)

The parameter specifies the point in time from which the stream will be saved. If this parameter is missing, the program records a livestream in real time, starting from the moment the program starts.

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

To record the scheduled (not currently active) livestream from the first second when it starts:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=wait

<br>

The program also has several other parameters:

##### [**` -duration=[minutes] `**](#)

Specifies the required duration in minutes. The minimum value is 1, the maximum is limited to 300. If this parameter is missing, the program uses the default 60. The example below saves 15 minutes of the livestream:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -duration=15

<br>

##### [**` -resolution=[heightPixels] `**](#)


Specifies the required resolution in pixels (height). If this parameter is missing, the program uses the default 1080. If the resolution is not available, the program uses the nearest lower. In the examples below, the livestream will be saved at 480p:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=500
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -resolution=480

<br>

##### [**` -vformat=[mediaContainerExtension] `**](#)

Specifies the required media container for the saved video. If this parameter is missing, the program uses the default MP4.
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -vformat=ts

<br>

##### [**` -pathffmpeg='C:\path\to\ffmpeg\' `**](#)

Specifies the path to the FFmpeg library. If this parameter is missing, the program uses FFmpeg located in the program folder.

<br>

##### [**` -pathsave='D:\path\to\save\streams\' `**](#)

Specifies a custom path for saving livestream. If this parameter is missing, the program saves the video to the directory where the batch file is located (or from which the command line is launched).

<br>

##### [**` -filename=[filenameWithoutExtension] `**](#)

Specifies a custom filename to save the livestream. If this parameter is missing, the program saves the video with name like *9Auq9mYxFEE_20210111-185830_060m_1080p*. Alternate filename supports the following renaming masks: `*id*`, `*start*`, `*start[customDateTime]*`, `*duration*`, `*resolution*`, `*author*`, `*title*`, `*channel_id*`. String `customDateTime` recognizes letters yyyyMMddHHmmss. The example below saves the livestream with name *Sky News - Watch Sky News live (2021-01-12)*:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -filename='*author* - *title* (*start[yyyy-MM-dd]*)'

<br>

More examples:

To save 15 minutes of the stream from yesterday 10:45AM to 11:00AM, at 1080p:
>     yrewind -url='youtube.com/v/9Auq9mYxFEE?fs=1&rel=0' -start=Y:1045 -duration=15

To save 1 hour of the stream from 04:55AM to 05:55AM on May 5, 2020, at 720p, to specified directory:
>     yrewind -url=9Auq9mYxFEE -start=20200505:0455 -resolution=720 -pathsave='D:\Saved_streams\'

To save 90 minutes of the stream, starting from half an hour ago, at the highest available resolution:
>     yrewind -url='https://www.youtube.com/watch?v=9Auq9mYxFEE' -start=-30 -duration=90 -resolution=9999

<br>

## Other info

* Loss of packets on the streamer side causes the estimated time to shift. The offset is usually seconds, but if its internet connection is unstable and/or the stream has been running for a long time, it can be minutes or even hours. For example, if the broadcast was interrupted for a total of 1 hour, then 24-hour frames will be downloaded as 23-hour. Thus, time accuracy can only be guaranteed for the current moment. The farther the video is rewound, the greater the probability of an error
* The calculated point for the `-start` parameter is the time of the local computer when the program starts (displayed in the first line)
* To determine the earliest available point, try download the knowingly unavailable time interval (for example, `-start=20000101:0000`). After a while, the program will show a warning indicating the earliest available point at the moment
* To prevent video file corruption due to network errors or software crashes, use TS container to save video (`-vformat=ts`). Please note that it does not currently support resolutions higher than 1080p and embedded metadata. It is also more reliable to download live stream as multiple video files with a relatively short duration (for example, 60 minutes) than as a single file with the maximum possible duration
* With the option `-start=beginning` sometimes an incomplete file is saved. In this case, set the `-start` value manually for a few minutes later (for example, `-start=20200715:0710`)

<br>

## Requirements

* FFmpeg static build (included in the archive)
* Windows 7 and on / Windows Server 2008 and on

<br>

## Tested Configuration

* FFmpeg 4.3 x86 (by Zeranoe)
* Windows 10 Pro x32 version 1909
* Windows 10 Pro x64 version 1909

<br>

## Developer info

* C#
* .NET Framework 4.5
* Visual Studio Community 2019

<br>

## Tags

download dvr live livestream record rewind save stream youtube

---
[[program page]](https://rytsikau.github.io/ee.Yrewind) [[start page]](https://rytsikau.github.io) [[author e-mail]](mailto:y.rytsikau@gmail.com)
