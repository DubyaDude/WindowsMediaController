using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Media.Control;

namespace WindowsMediaController
{
    public class MediaManager : IDisposable
    {
        public delegate void SessionChangeDelegate(MediaSession mediaSession);
        public delegate void PlaybackChangeDelegate(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo);
        public delegate void SongChangeDelegate(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties);

        /// <summary>
        /// Triggered when a new media source gets added to the <see cref="CurrentMediaSessions"/> dictionary.
        /// </summary>
        public event SessionChangeDelegate OnAnySessionOpened;

        /// <summary>
        /// Triggered when a media source gets removed from the <see cref="CurrentMediaSessions"/> dictionary.
        /// </summary>
        public event SessionChangeDelegate OnAnySessionClosed;

        /// <summary>
        /// Triggered when the focused <see cref="MediaSession"/> changes.
        /// </summary>
        public event SessionChangeDelegate OnFocusedSessionChanged;

        /// <summary>
        /// Triggered when a playback state changes of any <see cref="MediaSession"/>.
        /// </summary>
        public event PlaybackChangeDelegate OnAnyPlaybackStateChanged;

        /// <summary>
        /// Triggered when a song changes of any <see cref="MediaSession"/>.
        /// </summary>
        public event SongChangeDelegate OnAnyMediaPropertyChanged;

        /// <summary>
        /// A dictionary of the current <c>(<see cref="string"/> MediaSessionIds, <see cref="MediaSession"/> MediaSessionInstance)</c>
        /// </summary>
        public ReadOnlyDictionary<string, MediaSession> CurrentMediaSessions => new ReadOnlyDictionary<string, MediaSession>(_CurrentMediaSessions);
        private readonly Dictionary<string, MediaSession> _CurrentMediaSessions = new Dictionary<string, MediaSession>();

        /// <summary>
        /// Whether the <see cref="MediaSession"/> has started.
        /// </summary>
        public bool IsStarted { get => _IsStarted; }
        private bool _IsStarted;

        /// <summary>
        /// The <see cref="GlobalSystemMediaTransportControlsSessionManager"/> component from the Windows library.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager"/>
        public GlobalSystemMediaTransportControlsSessionManager WindowsSessionManager { get => _WindowsSessionManager; }
        private GlobalSystemMediaTransportControlsSessionManager _WindowsSessionManager;

        /// <summary>
        /// Starts the <see cref="MediaSession"/>.
        /// </summary>
        public void Start()
        {
            if (!_IsStarted)
            {
                //Populate CurrentMediaSessions with already open Sessions
                _WindowsSessionManager = GlobalSystemMediaTransportControlsSessionManager.RequestAsync().GetAwaiter().GetResult();
                SessionsChanged(_WindowsSessionManager);
                CurrentSessionChanged(_WindowsSessionManager);
                _WindowsSessionManager.SessionsChanged += SessionsChanged;
                _WindowsSessionManager.CurrentSessionChanged += CurrentSessionChanged;
                _IsStarted = true;
            }
            else
            {
                throw new InvalidOperationException("MediaManager already started");
            }
        }

        /// <summary>
        /// Gets the currently focused <see cref="MediaSession"/>.
        /// </summary>
        public MediaSession GetFocusedSession()
        {
            return GetFocusedSession(_WindowsSessionManager);
        }

        private void CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args = null)
        {
            MediaSession currentMediaSession = GetFocusedSession(sender);

            try { OnFocusedSessionChanged?.Invoke(currentMediaSession); } catch { }
        }

        private MediaSession GetFocusedSession(GlobalSystemMediaTransportControlsSessionManager sender)
        {
            var currentSession = sender.GetCurrentSession();

            MediaSession currentMediaSession = null;
            if (currentSession != null)
            {
                currentMediaSession = _CurrentMediaSessions[currentSession.SourceAppUserModelId];
            }

            return currentMediaSession;
        }

        private void SessionsChanged(GlobalSystemMediaTransportControlsSessionManager winSessionManager, SessionsChangedEventArgs args = null)
        {
            var controlSessionList = winSessionManager.GetSessions();

            //Checking for any new sessions, if found a new one add it to our dictiony and fire OnNewSource
            foreach (var controlSession in controlSessionList)
            {
                if (!CurrentMediaSessions.ContainsKey(controlSession.SourceAppUserModelId))
                {
                    MediaSession mediaSession = new MediaSession(controlSession, this);
                    _CurrentMediaSessions[controlSession.SourceAppUserModelId] = mediaSession;
                    try { OnAnySessionOpened?.Invoke(mediaSession); } catch { }
                    mediaSession.OnSongChange(controlSession);
                }
            }

            //Checking if a source fell off the session list without doing a proper Closed event (*cough* spotify *cough*)
            IEnumerable<string> controlSessionIds = controlSessionList.Select(x => x.SourceAppUserModelId);
            List<MediaSession> sessionsToRemove = new List<MediaSession>();

            foreach (var session in CurrentMediaSessions)
            {
                if (!controlSessionIds.Contains(session.Key))
                {
                    sessionsToRemove.Add(session.Value);
                }
            }

            sessionsToRemove.ForEach(x => x.Dispose());
        }

        private bool RemoveSource(MediaSession mediaSession)
        {
            if (_CurrentMediaSessions.ContainsKey(mediaSession.Id))
            {
                _CurrentMediaSessions.Remove(mediaSession.Id);
                try { OnAnySessionClosed?.Invoke(mediaSession); } catch { }
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            OnAnySessionOpened = null;
            OnAnySessionClosed = null;
            OnAnyMediaPropertyChanged = null;
            OnAnyPlaybackStateChanged = null;

            List<string> keys = CurrentMediaSessions.Keys.ToList();
            foreach (var key in keys)
            {
                CurrentMediaSessions[key].Dispose();
            }
            _CurrentMediaSessions?.Clear();

            _IsStarted = false;
            _WindowsSessionManager.SessionsChanged -= SessionsChanged;
            _WindowsSessionManager = null;
        }

        public class MediaSession
        {
            /// <summary>
            /// Triggered when this media source gets removed from the <see cref="CurrentMediaSessions"/> dictionary.
            /// </summary>
            public event SessionChangeDelegate OnSessionClosed;

            /// <summary>
            /// Triggered when a playback state changes of the <see cref="MediaSession"/>.
            /// </summary>
            public event PlaybackChangeDelegate OnPlaybackStateChanged;

            /// <summary>
            /// Triggered when a song changes of the <see cref="MediaSession"/>.
            /// </summary>
            public event SongChangeDelegate OnMediaPropertyChanged;

            /// <summary>
            /// The <see cref="GlobalSystemMediaTransportControlsSession"/> component from the Windows library.
            /// </summary>
            /// <seealso href="https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession"/>
            public GlobalSystemMediaTransportControlsSession ControlSession { get => _ControlSession; }
            private GlobalSystemMediaTransportControlsSession _ControlSession;

            /// <summary>
            /// The Unique Id of the <see cref="MediaSession"/>, grabbed from <see cref="GlobalSystemMediaTransportControlsSession.SourceAppUserModelId"/> from the Windows library.
            /// </summary>
            /// <seealso href="https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession.sourceappusermodelid"/>
            public readonly string Id;

            internal MediaManager MediaManagerInstance;

            internal MediaSession(GlobalSystemMediaTransportControlsSession controlSession, MediaManager mediaMangerInstance)
            {
                MediaManagerInstance = mediaMangerInstance;
                _ControlSession = controlSession;
                Id = _ControlSession.SourceAppUserModelId;
                _ControlSession.MediaPropertiesChanged += OnSongChange;
                _ControlSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            }

            private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession controlSession, PlaybackInfoChangedEventArgs args = null)
            {
                var playbackInfo = controlSession.GetPlaybackInfo();

                if (playbackInfo.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Closed)
                {
                    Dispose();
                }
                else
                {
                    try { OnPlaybackStateChanged?.Invoke(this, playbackInfo); } catch { }
                    try { MediaManagerInstance.OnAnyPlaybackStateChanged?.Invoke(this, playbackInfo); } catch { }
                }
            }

            internal async void OnSongChange(GlobalSystemMediaTransportControlsSession controlSession, MediaPropertiesChangedEventArgs args = null)
            {
                var mediaProperties = await controlSession.TryGetMediaPropertiesAsync();

                try { OnMediaPropertyChanged?.Invoke(this, mediaProperties); } catch { }
                try { MediaManagerInstance.OnAnyMediaPropertyChanged?.Invoke(this, mediaProperties); } catch { }
            }

            internal void Dispose()
            {
                if (MediaManagerInstance.RemoveSource(this))
                {
                    OnPlaybackStateChanged = null;
                    OnMediaPropertyChanged = null;
                    OnSessionClosed = null;
                    _ControlSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                    _ControlSession.MediaPropertiesChanged -= OnSongChange;
                    _ControlSession = null;
                    try { OnSessionClosed?.Invoke(this); } catch { }
                }
            }
        }
    }
}
