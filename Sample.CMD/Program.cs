using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WindowsMediaController;
using Windows.Media.Control;

namespace Sample.CMD
{
    class Program
    {
        static MediaManager mediaManager;

        public static void Main()
        {
            mediaManager = new MediaManager();

            mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
            mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
            mediaManager.OnAnyMediaPropertyChanged += MediaManager_OnAnyMediaPropertyChanged;

            mediaManager.Start();

            Console.ReadLine();
            mediaManager.Dispose();
        }

        private static void MediaManager_OnAnySessionOpened(MediaManager.MediaSession session)
        {
            WriteLineColor("-- New Source: " + session.Id, ConsoleColor.Green);
        }
        private static void MediaManager_OnAnySessionClosed(MediaManager.MediaSession session)
        {
            WriteLineColor("-- Removed Source: " + session.Id, ConsoleColor.Red);
        }

        private static void MediaManager_OnAnyPlaybackStateChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args)
        {
            WriteLineColor($"{sender.Id} is now {args.PlaybackStatus}", ConsoleColor.Yellow);
        }

        private static void MediaManager_OnAnyMediaPropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args)
        {
            WriteLineColor($"{sender.Id} is now playing {args.Title} {(String.IsNullOrEmpty(args.Artist) ? "" : $"by {args.Artist}")}", ConsoleColor.Cyan);
        }


        public static void WriteLineColor(object toprint, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + toprint);
        }
    }
}
