using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsMediaController;
using Windows.Media.Control;

namespace Tester
{
    class Program
    {
        static MediaManager mediaManager;

        public static void Main()
            => Start().GetAwaiter().GetResult();

        static async Task Start()
        {
            mediaManager = new MediaManager();

            mediaManager.OnNewSource += MediaManager_OnNewSource;
            mediaManager.OnRemovedSource += MediaManager_OnRemovedSource;
            mediaManager.OnPlaybackStateChanged += MediaManager_OnPlaybackStateChanged;
            mediaManager.OnSongChanged += MediaManager_OnSongChanged;
            await mediaManager.Start();

            InfTask().GetAwaiter().GetResult();
            mediaManager.Dispose();
        }
        private static async Task InfTask() => await Task.Delay(-1);


        private static void MediaManager_OnNewSource(MediaManager.MediaSession session)
        {
            WriteLineColor("-- New Source: " + session.ControlSession.SourceAppUserModelId, ConsoleColor.Green);
        }
        private static void MediaManager_OnRemovedSource(MediaManager.MediaSession session)
        {
            WriteLineColor("-- Removed Source: " + session.ControlSession.SourceAppUserModelId, ConsoleColor.Red);
        }

        private static void MediaManager_OnSongChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args)
        {
            WriteLineColor($"{sender.ControlSession.SourceAppUserModelId} is now playing {args.Title} {(String.IsNullOrEmpty(args.Artist) ? "" : $"by {args.Artist}")}", ConsoleColor.Cyan);
        }

        private static void MediaManager_OnPlaybackStateChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args)
        {
            WriteLineColor($"{sender.ControlSession.SourceAppUserModelId} is now {args.PlaybackStatus}", ConsoleColor.Yellow);
        }


        public static void WriteLineColor(object toprint, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + toprint);
        }
    }
}
