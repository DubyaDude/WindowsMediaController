using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace WindowsMediaController
{
    public class MediaManager : IDisposable
    {
        public delegate void SourceChangeDelegate(MediaSession session);
        public delegate void PlaybackChangeDelegate(MediaSession session, GlobalSystemMediaTransportControlsSessionPlaybackInfo playbackInfo);
        public delegate void SongChangeDelegate(MediaSession session, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties);

        /// <summary>
        /// Triggered when a new media source gets added to the <c>CurrentMediaSessions</c> dictionary
        /// </summary>
        public event SourceChangeDelegate OnAnyNewSource;

        /// <summary>
        /// Triggered when a media source gets removed from the <c>CurrentMediaSessions</c> dictionary
        /// </summary>
        public event SourceChangeDelegate OnAnyRemovedSource;

        /// <summary>
        /// Triggered when a playback state changes of any <c>MediaSession</c>
        /// </summary>
        public event PlaybackChangeDelegate OnAnyPlaybackStateChanged;

        /// <summary>
        /// Triggered when a song changes of any <c>MediaSession</c>
        /// </summary>
        public event SongChangeDelegate OnAnySongChanged;

        /// <summary>
        /// A dictionary of the current <c>(string MediaSessionIds, MediaSession MediaSessionInstance)</c>
        /// </summary>
        public ReadOnlyDictionary<string, MediaSession> CurrentMediaSessions => new ReadOnlyDictionary<string, MediaSession>(_CurrentMediaSessions);
        private Dictionary<string, MediaSession> _CurrentMediaSessions = new Dictionary<string, MediaSession>();

        /// <summary>
        /// Tells if <c>MediaManager</c> has started
        /// </summary>
        public bool IsStarted { get => _IsStarted; }
        private bool _IsStarted;

        /// <summary>
        /// The <c>GlobalSystemMediaTransportControlsSessionManager</c> component from the Windows library
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager"/>
        public GlobalSystemMediaTransportControlsSessionManager WindowsSessionManager { get => _WindowsSessionManager; }
        private GlobalSystemMediaTransportControlsSessionManager _WindowsSessionManager;

        /// <summary>
        /// This starts the <c>MediaManager</c>
        /// </summary>
        public async Task Start()
        {
            if (!_IsStarted)
            {
                //Populate CurrentMediaSessions with already open Sessions
                _WindowsSessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                SessionsChanged(_WindowsSessionManager);
                _WindowsSessionManager.SessionsChanged += SessionsChanged;
                _IsStarted = true;
            }
            else
            {
                throw new InvalidOperationException("MediaManager already started");
            }
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
                    try { OnAnyNewSource?.Invoke(mediaSession); } catch { }
                    mediaSession.OnSongChange(controlSession);
                }
            }

            //Checking if a source fell off the session list without doing a proper Closed event (*cough* spotify *cough*)
            IEnumerable<string> controlSessionIds = controlSessionList.Select(x=> x.SourceAppUserModelId);
            List<MediaSession> sessionsToRemove = new List<MediaSession>();

            foreach(var session in CurrentMediaSessions)
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
                try { OnAnyRemovedSource?.Invoke(mediaSession); } catch { }
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            OnAnyNewSource = null;
            OnAnyRemovedSource = null;
            OnAnySongChanged = null;
            OnAnyPlaybackStateChanged = null;

            List<string> keys = CurrentMediaSessions.Keys.ToList();
            foreach(var key in keys)
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
            /// Triggered when a playback state changes of the <c>MediaSession</c>
            /// </summary>
            public event PlaybackChangeDelegate OnPlaybackStateChanged;

            /// <summary>
            /// Triggered when a song changes of the <c>MediaSession</c>
            /// </summary>
            public event SongChangeDelegate OnSongChanged;

            /// <summary>
            /// Triggered when this media source gets removed from the <c>CurrentMediaSessions</c> dictionary
            /// </summary>
            public event SourceChangeDelegate OnRemovedSource;

            /// <summary>
            /// The GlobalSystemMediaTransportControlsSession component from the Windows library
            /// </summary>
            /// <seealso href="https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession"/>
            public GlobalSystemMediaTransportControlsSession ControlSession { get => _ControlSession; }
            private GlobalSystemMediaTransportControlsSession _ControlSession;

            /// <summary>
            /// The Unique Id of the <c>MediaSession</c>, grabbed from <c>GlobalSystemMediaTransportControlsSession.SourceAppUserModelId</c> from the Windows library
            /// </summary>
            /// <seealso href="https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession.sourceappusermodelid"/>
            public string Id { get => _Id; }
            private string _Id;

            internal MediaManager MediaManagerInstance;

            internal MediaSession(GlobalSystemMediaTransportControlsSession controlSession, MediaManager mediaMangerInstance)
            {
                MediaManagerInstance = mediaMangerInstance;
                _ControlSession = controlSession;
                _Id = _ControlSession.SourceAppUserModelId;
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

                try { OnSongChanged?.Invoke(this, mediaProperties); } catch { }
                try { MediaManagerInstance.OnAnySongChanged?.Invoke(this, mediaProperties); } catch { }
            }

            internal void Dispose()
            {
                if (MediaManagerInstance.RemoveSource(this))
                {
                    OnPlaybackStateChanged = null;
                    OnSongChanged = null;
                    OnRemovedSource = null;
                    _ControlSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                    _ControlSession.MediaPropertiesChanged -= OnSongChange;
                    _ControlSession = null;
                    try { OnRemovedSource?.Invoke(this); } catch { }
                }
            }
        }
    }
}
