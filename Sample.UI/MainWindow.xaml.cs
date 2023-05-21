using ModernWpf.Controls;
using System;
using System.Windows;
using System.Windows.Media.Imaging;
using Windows.Media.Control;
using Windows.Storage.Streams;
using WindowsMediaController;
using static WindowsMediaController.MediaManager;

namespace Sample.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly MediaManager mediaManager = new MediaManager();
        private static MediaSession? currentSession = null;

        public MainWindow()
        {
            InitializeComponent();

            mediaManager.OnAnySessionOpened += MediaManager_OnAnySessionOpened;
            mediaManager.OnAnySessionClosed += MediaManager_OnAnySessionClosed;
            mediaManager.OnFocusedSessionChanged += MediaManager_OnFocusedSessionChanged;

            mediaManager.Start();
        }

        private void MediaManager_OnAnySessionOpened(MediaSession mediaSession)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var menuItem = new NavigationViewItem
                {
                    Content = mediaSession.Id,
                    Icon = new SymbolIcon() { Symbol = Symbol.Audio },
                    Tag = mediaSession
                };
                SongList.MenuItems.Add(menuItem);
            });
        }

        private void MediaManager_OnAnySessionClosed(MediaSession session)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                NavigationViewItem? itemToRemove = null;

                foreach (NavigationViewItem? item in SongList.MenuItems)
                    if (((MediaSession?)item?.Tag)?.Id == session.Id)
                        itemToRemove = item;

                if (itemToRemove != null)
                    SongList.MenuItems.Remove(itemToRemove);
            });
        }

        private void MediaManager_OnFocusedSessionChanged(MediaSession session)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (NavigationViewItem? item in SongList.MenuItems)
                {
                    if (item != null)
                    {
                        item.Content = (((MediaSession?)item?.Tag)?.Id == session?.Id ? "# " : "") + ((MediaSession?)item?.Tag)?.Id;
                    }
                }
            });
        }

        private void SongList_SelectionChanged(NavigationView navView, NavigationViewSelectionChangedEventArgs args)
        {
            if (currentSession != null)
            {
                currentSession.OnMediaPropertyChanged -= CurrentSession_OnMediaPropertyChanged;
                currentSession.OnPlaybackStateChanged -= CurrentSession_OnPlaybackStateChanged;
                currentSession = null;
            }

            if (navView.SelectedItem != null)
            {
                currentSession = (MediaSession)((NavigationViewItem)navView.SelectedItem).Tag;
                currentSession.OnMediaPropertyChanged += CurrentSession_OnMediaPropertyChanged;
                currentSession.OnPlaybackStateChanged += CurrentSession_OnPlaybackStateChanged;
                CurrentSession_OnPlaybackStateChanged(currentSession);
            }
            else
            {
                SongImage.Source = null;
                SongTitle.Content = "TITLE";
                SongAuthor.Content = "Author";
                ControlPlayPause.Content = "▶️";
            }
        }

        private void CurrentSession_OnPlaybackStateChanged(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionPlaybackInfo? playbackInfo = null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateUI(mediaSession);
            });
        }

        private void CurrentSession_OnMediaPropertyChanged(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateUI(mediaSession);
            });
        }

        private void UpdateUI(MediaSession mediaSession)
        {
            var mediaProp = mediaSession.ControlSession.GetPlaybackInfo();
            if (mediaProp != null)
            {
                if (mediaSession.ControlSession.GetPlaybackInfo().Controls.IsPauseEnabled)
                    ControlPlayPause.Content = "II";
                else
                    ControlPlayPause.Content = "▶️";
                ControlBack.IsEnabled = ControlForward.IsEnabled = mediaProp.Controls.IsNextEnabled;
            }

            var songInfo = mediaSession.ControlSession.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
            if (songInfo != null)
            {
                SongTitle.Content = songInfo.Title.ToUpper();
                SongAuthor.Content = songInfo.Artist;
                SongImage.Source = Helper.GetThumbnail(songInfo.Thumbnail);
            }

        }

        private async void Back_Click(object sender, RoutedEventArgs e)
        {
            await currentSession?.ControlSession.TrySkipPreviousAsync();
        }

        private async void PlayPause_Click(object sender, RoutedEventArgs e)
        {
            var controlsInfo = currentSession?.ControlSession.GetPlaybackInfo().Controls;

            if (controlsInfo?.IsPauseEnabled == true)
                await currentSession?.ControlSession.TryPauseAsync();
            else if (controlsInfo?.IsPlayEnabled == true)
                await currentSession?.ControlSession.TryPlayAsync();
        }

        private async void Forward_Click(object sender, RoutedEventArgs e)
        {
            await currentSession?.ControlSession.TrySkipNextAsync();
        }
    }

    internal static class Helper
    {
        internal static BitmapImage? GetThumbnail(IRandomAccessStreamReference Thumbnail)
        {
            if (Thumbnail == null)
                return null;

            var imageStream = Thumbnail.OpenReadAsync().GetAwaiter().GetResult();
            byte[] fileBytes = new byte[imageStream.Size];
            using (DataReader reader = new DataReader(imageStream))
            {
                reader.LoadAsync((uint)imageStream.Size).GetAwaiter().GetResult();
                reader.ReadBytes(fileBytes);
            }

            var image = new BitmapImage();
            using (var ms = new System.IO.MemoryStream(fileBytes))
            {
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
            }
            return image;
        }
    }
}
