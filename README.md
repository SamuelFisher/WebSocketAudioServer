WebSocket Audio Server
===============

Play (live) audio using HTML5 by streaming from a WebSocket connection.

This is a demo server for the [WebSocket Audio](https://github.com/SamuelFisher/WebSocketAudio) project.
It streams the output of a local audio device over a WebSocket connection.

### Usage

Works on Microsoft Windows only.

1. FFmpeg is required for encoding to Vorbis WebM:
    - Download the Windows build of [FFmpeg](https://ffmpeg.org/download.html)
    - Place `ffmpeg.exe` in the directory the project will be run from. E.g. `bin/Debug/ffmpeg.exe`
1. Open WebSocketAudioServer.sln in Visual Studio.
2. Compile and run.
3. Choose the audio device to stream.
