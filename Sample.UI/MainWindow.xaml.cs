using ModernWpf.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
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
        private static MediaManager mediaManager = new MediaManager();
        private static MediaSession? currentSession = null;

        public MainWindow()
        {
            InitializeComponent();

            mediaManager.OnAnyNewSource += MediaManager_OnAnyNewSource;
            mediaManager.OnAnyRemovedSource += MediaManager_OnAnyRemovedSource;

            mediaManager.Start();
        }

        private void MediaManager_OnAnyNewSource(MediaSession mediaSession)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var menuItem = new NavigationViewItem();
                menuItem.Content = mediaSession.ControlSession.SourceAppUserModelId;
                menuItem.Icon = new SymbolIcon() { Symbol = Symbol.Audio };
                menuItem.Tag = mediaSession;
                SongList.MenuItems.Add(menuItem);
            });
        }

        private void MediaManager_OnAnyRemovedSource(MediaSession session)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                List<NavigationViewItem?> itemsToRemove = new List<NavigationViewItem?>();

                foreach (NavigationViewItem? item in SongList.MenuItems)
                    if (item?.Content.ToString() == session.ControlSession.SourceAppUserModelId)
                        itemsToRemove.Add(item);

                itemsToRemove.ForEach(x => SongList.MenuItems.Remove(x));
            });
        }

        private void SongList_SelectionChanged(NavigationView navView, NavigationViewSelectionChangedEventArgs args)
        {
            if(currentSession != null)
            {
                currentSession.OnSongChanged -= CurrentSession_OnSongChanged;
                currentSession.OnPlaybackStateChanged -= CurrentSession_OnPlaybackStateChanged;
                currentSession = null;
            }

            if(navView.SelectedItem != null)
            {
                currentSession = (MediaSession)((NavigationViewItem)navView.SelectedItem).Tag;
                UpdateUI(currentSession);
                currentSession.OnSongChanged += CurrentSession_OnSongChanged;
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

        private void CurrentSession_OnSongChanged(MediaSession mediaSession, GlobalSystemMediaTransportControlsSessionMediaProperties mediaProperties)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateUI(mediaSession);
            });
        }

        private void UpdateUI(MediaSession mediaSession)
        {
            if (mediaSession.ControlSession.GetPlaybackInfo().Controls.IsPauseEnabled)
                ControlPlayPause.Content = "II";
            else
                ControlPlayPause.Content = "▶️";

            var songInfo = mediaSession.ControlSession.TryGetMediaPropertiesAsync().GetAwaiter().GetResult();
            if(songInfo != null)
            {
                SongTitle.Content = songInfo.Title.ToUpper();
                SongAuthor.Content = songInfo.Artist;
                SongImage.Source = Helper.GetThumbnail(songInfo.Thumbnail);
            }

            var mediaProp = mediaSession.ControlSession.GetPlaybackInfo();
            ControlBack.IsEnabled = ControlForward.IsEnabled = mediaProp.Controls.IsNextEnabled;
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
