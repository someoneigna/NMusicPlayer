using MusicPlayer.Data;
using MusicPlayer.Models;
using MusicPlayer.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace MusicPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; set; }
        private DispatcherTimer SliderTimer { get; set; }
        private DispatcherTimer SearchTimer { get; set; }
        private bool SliderDragging { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new MainViewModel();
            DataContext = ViewModel;

            ViewModel.Player.MediaFailed += Player_MediaFailed;
            ViewModel.Player.MediaOpened += Player_MediaOpened;
            ViewModel.Player.MediaEnded += Player_MediaEnded;

            // To position thumb with mouse click
            sliderControl.PreviewMouseLeftButtonDown += sliderControl_PreviewMouseLeftButtonDown;

            volumeSlider.Value = 50f;
            ViewModel.Player.Volume = 1.0f;

            SliderTimer = new DispatcherTimer();
            SliderTimer.Interval = TimeSpan.FromMilliseconds(200);
            SliderTimer.Tick += timer_Tick;

            SearchTimer = new DispatcherTimer();
            SearchTimer.Interval = TimeSpan.FromMilliseconds(300);
            SearchTimer.Tick += SearchTimer_Tick;

            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;

            worker.ProgressChanged += worker_ProgressChanged;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.RunWorkerAsync();
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            ViewModel.LoadCollections();
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("Completed:" + e.ProgressPercentage);
        }

        /// <summary>
        /// Checks searchBox for valid input,
        /// in that case filters treeView content.
        /// </summary>
        private void SearchTimer_Tick(object sender, EventArgs e)
        {
            if (ViewModel.SearchField["Input"] == null) // If validation passed
            {
                if (ViewModel.SearchField.Input != MusicSearchField.InitialInputValue)
                {
                    ViewModel.FilterSearch();
                }
            }
            else
            {
                ViewModel.RefreshBands();
            }
            SearchTimer.Stop();
        }

        private static Track GetSliderTrack(Slider slider)
        {
            Track track = (Track)slider.Template.FindName("PART_Track", slider);
            return track;
        }

        void sliderControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Track track = GetSliderTrack(sliderControl);
            double value = track.ValueFromPoint(e.GetPosition(track));
            sliderControl.Value = value;

            ViewModel.Player.Position = TimeSpan.FromSeconds(sliderControl.Value);
        }

        /// <summary>
        /// Updates position of the song position slider,
        /// and sets the time label.
        /// </summary>
        void timer_Tick(object sender, EventArgs e)
        {
            if (!SliderDragging)
            {
                sliderControl.Value = ViewModel.Player.Position.TotalSeconds;
                songTime.Content = ViewModel.Player.Position.ToString(@"mm\:ss");
            }
        }

        /// <summary>
        /// Occurs when MediaPlayer started playback,
        /// sets position slider to start and
        /// sets volume to last position.
        /// </summary>
        void Player_MediaOpened(object sender, EventArgs e)
        {
            var player = sender as MediaPlayer;
            player.Position = new TimeSpan(0, 0, 0);
            sliderControl.Maximum = player.NaturalDuration.TimeSpan.TotalSeconds;
            sliderControl.Minimum = 0;
            player.Volume = volumeSlider.Value;
        }

        /// <summary>
        /// Occurs when current playing song ended,
        /// looks play queue and plays next one.
        /// </summary>
        void Player_MediaEnded(object sender, EventArgs e)
        {
            sliderControl.Value = 0;
            songTime.Content = "";
            SliderTimer.Stop();
            ViewModel.Status = SongStatus.Stopped;

            if (ViewModel.PlayQueue.Count > 0)
            {
                ViewModel.CurrentSong = ViewModel.PlayQueue.Dequeue();
                Play();
            }
        }

        /// <summary>
        /// Occurs when MediaPlayer failed to reproduce the fail,
        /// show a error message and stop playback.
        /// </summary>
        void Player_MediaFailed(object sender, ExceptionEventArgs e)
        {
            MessageBox.Show(e.ErrorException.Message);
            Stop();
        }

        public void Play()
        {
            ViewModel.Play();
            switch(ViewModel.Status)
            {
                case SongStatus.Playing:
                    SliderTimer.Start();
                    break;
                case SongStatus.Paused:
                    // Fall through
                case SongStatus.Stopped:
                    SliderTimer.Stop();
                    break;
            }
        }

        public void Stop()
        {
            SearchTimer.Stop();
            SliderTimer.Stop();

            ViewModel.Status = SongStatus.Stopped;

            sliderControl.Value = 0;

            ViewModel.Player.Stop();
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void sliderControl_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            SliderDragging = false;
            ViewModel.Player.Position = TimeSpan.FromSeconds(sliderControl.Value);
            if (ViewModel.Status == SongStatus.Paused)
            {
                Play();
            }
        }

        private void sliderControl_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            SliderDragging = true;
            if (ViewModel.Status == SongStatus.Playing)
            {
                Play(); // Pause
            }
        }

        /// <summary>
        ///  When a music file from the tree is chosen,
        ///  fill the play queue with the album songs,
        ///  set current song, and start playing.
        /// </summary>
        private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var treeView = sender as TreeView;
            var item = treeView.SelectedItem;

            if (item is Song)
            {
                Song song = item as Song;
                ViewModel.PlayList = new List<Song>(song.Album.Songs);
                int songIndex = ViewModel.PlayList.IndexOf(song);

                ViewModel.PlayQueue = new Queue<Song>(ViewModel.PlayList.GetRange(songIndex + 1, ViewModel.PlayList.Count - songIndex - 1));

                ViewModel.CurrentSong = song;

                Stop();
                Play();
            }
        }

        /// <summary>
        /// On exit stop the media player and timers,
        /// and save the music collection.
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Stop();
            ViewModel.SaveCollections();
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var logged = Math.Log(volumeSlider.Value + 1) / Math.Log(101);
            ViewModel.Player.Volume = (logged >= 0 && logged <= 100) ? logged : Math.Max(logged - 100, logged + double.Epsilon);
        }

        /// <summary>
        /// If a element on the playlist is chosen,
        /// sets the play queue state and starts chosen
        /// song.
        /// </summary>
        private void playList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var playListBox = sender as ListBox;
            var song = playListBox.SelectedItem as Song;
            int playListIndex = playListBox.SelectedIndex;

            playListBox.UpdateLayout();
            playListBox.ScrollIntoView(playListBox.SelectedItem);

            ViewModel.PlayQueue.Clear();
            Stop();

            ViewModel.PlayQueue = new Queue<Song>(ViewModel.PlayList.GetRange(playListIndex + 1, ViewModel.PlayList.Count - playListIndex - 1));

            ViewModel.CurrentSong = playListBox.SelectedItem as Song;

            Play();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            Stop();
            ViewModel.SaveCollections();
        }

        private void Menu_Archivos_Salir_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Spawns a FolderBrowserDialog to choose a directory
        /// to scan music files.
        /// </summary>
        /// <returns>A string containing a directory path</returns>
        private string SpawnSelectScanDir()
        {
            Ionic.Utils.FolderBrowserDialogEx dialog = new Ionic.Utils.FolderBrowserDialogEx();
            //System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Select music directory";
            dialog.ShowEditBox = true;
            dialog.ShowNewFolderButton = false;
            dialog.ShowBothFilesAndFolders = true;
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK && string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                MessageBox.Show("Cant scan a invalid directory", "Directory selection failed");
                return null;
            }
            else if (result == System.Windows.Forms.DialogResult.Cancel)
            {
                return null;
            }

            return dialog.SelectedPath;
        }

        private void Menu_Files_Scan_Click(object sender, RoutedEventArgs e)
        {
            string musicScanDir = SpawnSelectScanDir();
            if (musicScanDir != null)
            {
                ViewModel.ScanDirectory(musicScanDir);
            }
        }

        /// <summary>
        /// Clears search box on first click.
        /// </summary>
        private void searchBox_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var searchBox = sender as TextBox;
            if (searchBox.Text == MusicSearchField.InitialInputValue)
            {
                searchBox.Clear();
            }
        }

        /// <summary>
        /// Input on the search box changed,
        /// start searchTimer to validate and search.
        /// </summary>
        private void searchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // No need to search if there is no data
            if (ViewModel.Collections.Count > 0)
            {
                SearchTimer.Stop();
                SearchTimer.Start();
            }
        }

    }
}
