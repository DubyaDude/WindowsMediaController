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
        static void Main(string[] args)
        {
            MediaManager.OnNewSource += MediaManager_OnNewSource;
            MediaManager.OnRemovedSource += MediaManager_OnRemovedSource;
            MediaManager.OnPlaybackStateChanged += MediaManager_OnPlaybackStateChanged;
            MediaManager.OnSongChanged += MediaManager_OnSongChanged;
            MediaManager.Start();
            while (true)
                Console.ReadLine();
        }
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
            WriteLineColor($"{sender.ControlSession.SourceAppUserModelId} is now {args.PlaybackStatus}", ConsoleColor.Magenta);
        }


        public static void WriteLineColor(object toprint, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + toprint);
        }
    }
}
