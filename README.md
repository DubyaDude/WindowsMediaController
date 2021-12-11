# Windows Media Controller
This repository provides a wrapper for developers to more easily get information from and interact with the Windows 10/11 OS media interface. 

![Windows 10 Media Interface](https://raw.githubusercontent.com/DubyaDude/WindowsMediaController/master/docs/images/Win10.png)

This allows for a better understanding and control of the Media Sessions and can have many different applications. Some features include:
- Control playback on individual Media Sessions (Spotify, Chrome, etc)
- Get media information of currently playing (Song, Author, Thumbnail, etc)

## Requirements
- Windows 10 (Build 17763+) or Windows 11
- The ability to talk to Windows Runtime. (In a majority of cases, this will not be an issue)
- .NET Framework 4.6+ or .NET Core 3.0+

## How To Use
### Initialization:
```csharp
mediaManager = new MediaManager();

mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
mediaManager.OnAnyPlaybackStateChanged += MediaManager_OnAnyPlaybackStateChanged;
mediaManager.OnAnySongChanged += MediaManager_OnAnySongChanged;

await mediaManager.Start();
```

### Class Structure:
MediaManager:
```csharp
ReadOnlyDictionary<string, MediaSession> CurrentMediaSessions;
bool IsStarted { get; }
GlobalSystemMediaTransportControlsSessionManager WindowsSessionManager { get; }

delegate void OnAnySessionOpened(MediaManager.MediaSession session);
delegate void OnAnySessionClosed(MediaManager.MediaSession session);
delegate void OnAnyMediaPropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args);
delegate void OnAnyPlaybackStateChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args);
```
MediaManager.MediaSession:
```csharp
readonly string Id;
GlobalSystemMediaTransportControlsSession ControlSession { get; }

delegate void OnSessionClosed(MediaManager.MediaSession session);
delegate void OnMediaPropertyChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionMediaProperties args);
delegate void OnPlaybackStateChanged(MediaManager.MediaSession sender, GlobalSystemMediaTransportControlsSessionPlaybackInfo args);
```

### Getting Some Info:

- Getting PlaybackInfo (Seeing what actions are available/Is paused or playing, etc)
  - Returns: GlobalSystemMediaTransportControlsSessionPlaybackInfo
  - ``mediaSession.ControlSession.GetPlaybackInfo()``

- Getting current MediaProperties (Currently playing title, author, thumbnail, etc)
  - Returns: GlobalSystemMediaTransportControlsSessionMediaProperties
  - ``await mediaSession.ControlSession.TryGetMediaPropertiesAsync()``

### Useful Microsoft Documentations:
- [GlobalSystemMediaTransportControlsSessionManager](https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmanager) - Located in `MediaManager.WindowsSessionManager`. This class allows for events whenever a source's state changes.
- [GlobalSystemMediaTransportControlsSession](https://docs.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession) - Located in `MediaManager.MediaSession.ControlSession`. The Media Session that allows for events whenever the playback state or the media property changes as well as to grab such info whenever desired.

  
## Samples
- Sample.CMD - A very barebone console application for developers to get a feel of how their use-case might act.

![Sample.CMD](https://raw.githubusercontent.com/DubyaDude/WindowsMediaController/master/docs/images/Sample.CMD.png)

- Sample.UI - A WPF media controller

![Sample.UI](https://raw.githubusercontent.com/DubyaDude/WindowsMediaController/master/docs/images/Sample.UI.png)


## Credit
- Luca Marini ([gitlab](https://gitlab.com/naatiivee)) - Helped me understand the Windows API
- Google ([materialui](https://github.com/google/material-design-icons)) - Utilizing their play icon to create our icon
- Kinnara ([ModernWpf](https://github.com/Kinnara/ModernWpf)) - Utilized the ModernWpf library to create the UI sample
