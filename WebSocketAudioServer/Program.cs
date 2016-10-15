//
// Copyright (c) 2016 Samuel Fisher
// Licensed under the MIT License. See LICENSE.txt file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using NAudio.Wave;
using WebSocketSharp.Server;

namespace WebSocketAudioServer
{
    class Program
    {
        private static int SegmentDurationSeconds = 1;

        static void Main(string[] args)
        {
            // 1. Select input device

            int waveInDevices = WaveIn.DeviceCount;
            int waveInDevice;
            for (waveInDevice = 0; waveInDevice < waveInDevices; waveInDevice++)
            {
                WaveInCapabilities deviceInfo = WaveIn.GetCapabilities(waveInDevice);
                Console.WriteLine("Device {0}: {1}, {2} channels", waveInDevice, deviceInfo.ProductName, deviceInfo.Channels);
            }

            Console.WriteLine($"Device {waveInDevice}: WASAPI Loopback");

            int chosenDevice = args.Length > 0 ? int.Parse(args[0]) : int.Parse(Console.ReadLine());

            // 2. Start streaming

            var socket = new WebSocketSharp.Server.WebSocketServer(5001);
            socket.AddWebSocketService<WebSocketStreamService>("/stream");
            socket.Start();

            IWaveIn waveIn;
            if (chosenDevice == waveInDevice)
            {
                waveIn = new WasapiLoopbackCapture();
            }
            else
            {
                waveIn = new WaveInEvent
                {
                    DeviceNumber = chosenDevice,
                    WaveFormat = new WaveFormat(44100, 2)
                };
            }

            var wav = new BufferedWaveProvider(waveIn.WaveFormat);
            wav.BufferDuration = TimeSpan.FromMinutes(1);

            waveIn.DataAvailable += (sender, e) =>
            {
                wav.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };
            waveIn.StartRecording();
            
            while (true)
            {
                Console.WriteLine("Waiting for audio...");

                while (wav.BufferedDuration < TimeSpan.FromSeconds(SegmentDurationSeconds))
                {
                    // Wait for more audio to become available
                }

                // Write audio to wave file
                
                var fn = "sample.wav";
                using (var fs = File.OpenWrite(fn))
                using (var wtr = new WaveFileWriter(fs, wav.WaveFormat))
                {
                    int total = 0;
                    while (total < wav.WaveFormat.AverageBytesPerSecond * SegmentDurationSeconds)
                    {
                        byte[] buffer = new byte[wav.WaveFormat.AverageBytesPerSecond];
                        int read = wav.Read(buffer, 0, buffer.Length);

                        wtr.Write(buffer, 0, read);

                        total += read;
                    }
                }

                // Transcode wave file to vorbis webm file

                string output = "sample.webm";

                if (File.Exists(output))
                    File.Delete(output);

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo("ffmpeg.exe",
                        $"-i \"{fn}\" -c:a libvorbis -qscale:a 7 \"{output}\"")
                    {
                        UseShellExecute = false
                    }
                };

                process.Start();
                process.WaitForExit();

                socket.WebSocketServices.Broadcast(File.ReadAllBytes(output));

                File.Delete(output);
            }
        }
    }

    class WebSocketStreamService : WebSocketBehavior
    {
    }
}
