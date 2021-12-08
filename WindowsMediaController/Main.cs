using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Media.Control;

namespace WindowsMediaController
{
    public class MediaManager
    {
        public delegate void SourceChangeDelegate(MediaSession session);
        public delegate void PlaybackChangeDelegate(MediaSession session, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo);
        public delegate void SongChangeDelegate(MediaSession session, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties);

        /// <summary>
        /// Triggered when a new media source gets added to the MediaSessions
        /// </summary>
        public event SourceChangeDelegate OnNewSource;

        /// <summary>
        /// Triggered when a media source gets removed from the MediaSessions
        /// </summary>
        public event SourceChangeDelegate OnRemovedSource;

        /// <summary>
        /// Triggered when a playback state changes of a MediaSession
        /// </summary>
        public event PlaybackChangeDelegate OnPlaybackStateChanged;

        /// <summary>
        /// Triggered when a song changes of a MediaSession
        /// </summary>
        public event SongChangeDelegate OnSongChanged;
        
        /// <summary>
        /// A dictionary of the current MediaSessions
        /// </summary>
        public Dictionary<string, MediaSession> CurrentMediaSessions = new Dictionary<string, MediaSession>();


        private bool IsStarted;

        /// <summary>
        /// This starts the MediaManager
        /// This can be changed to a constructor if you don't care for the first few 'new sources' events
        /// </summary>
        public void Start()
        {
            if (!IsStarted)
            {
                var sessionManager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult();
                SessionsChanged(sessionManager);
                sessionManager.SessionsChanged += SessionsChanged;
                IsStarted = true;
            }
            else
            {
                throw new InvalidOperationException("MediaController Already Started");
            }
        }

        private void SessionsChanged(GlobalSystemMediaTransportControlsSessionManager sender, SessionsChangedEventArgs args = null)
        {
            var sessionList = sender.GetSessions();

            foreach (var session in sessionList)
            {
                if (!CurrentMediaSessions.ContainsKey(session.SourceAppUserModelId))
                {
                    MediaSession mediaSession = new MediaSession(session, this);
                    CurrentMediaSessions[session.SourceAppUserModelId] = mediaSession;
                    OnNewSource?.Invoke(mediaSession);
                    mediaSession.OnSongChange(session);
                }
            }

            IEnumerable<string> currentSessionIds = sessionList.Select(x=> x.SourceAppUserModelId);
            List<MediaSession> sessionsToRemove = new List<MediaSession>();

            foreach(var session in CurrentMediaSessions)
            {
                if (!currentSessionIds.Contains(session.Key))
                {
                    sessionsToRemove.Add(session.Value);
                }
            }

            sessionsToRemove.ForEach(x => x.RemoveSource());
        }


        private void RemoveSource(MediaSession mediaSession)
        {
            CurrentMediaSessions.Remove(mediaSession.ControlSession.SourceAppUserModelId); 
            OnRemovedSource?.Invoke(mediaSession);
        }

        public class MediaSession
        {
            public GlobalSystemMediaTransportControlsSession ControlSession;
            internal MediaManager MediaManagerInstance;

            internal MediaSession(GlobalSystemMediaTransportControlsSession ctrlSession, MediaManager mediaMangerInstance)
            {
                MediaManagerInstance = mediaMangerInstance;
                ControlSession = ctrlSession;
                ControlSession.MediaPropertiesChanged += OnSongChange;
                ControlSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            }


            private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession session, PlaybackInfoChangedEventArgs args = null)
            {
                var props = session.GetPlaybackInfo();
                if (props.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed)
                {
                    RemoveSource();
                }
                else
                {
                    MediaManagerInstance.OnPlaybackStateChanged?.Invoke(this, props);
                }
            }

            internal void RemoveSource()
            {
                ControlSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                ControlSession.MediaPropertiesChanged -= OnSongChange;
                MediaManagerInstance.RemoveSource(this);
            }

            internal async void OnSongChange(GlobalSystemMediaTransportControlsSession session, MediaPropertiesChangedEventArgs args = null)
            {
                MediaManagerInstance.OnSongChanged?.Invoke(this, await session.TryGetMediaPropertiesAsync());
            }
        }
    }
}
