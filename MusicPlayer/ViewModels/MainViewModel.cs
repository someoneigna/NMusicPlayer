using MusicPlayer.Data;
using MusicPlayer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MusicPlayer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public readonly string CollectionSavePath = "collectionData.json";

        public MediaPlayer Player { get; set; }

        private List<Song> _playList;
        public List<Song> PlayList
        { 
            get
            {
                return _playList;
            }
            set
            {
                _playList = value;
                OnPropertyChanged("PlayList");
            }
        }

        private ISet<Band> SourceBands;

        private ISet<Band> _bands;
        public ISet<Band> Bands
        {
            get
            {
                return _bands;
            }
            set
            {
                _bands = value;
                OnPropertyChanged("Bands");
            }
        }

        private List<Collection> _collections;
        public List<Collection> Collections
        {
            get
            {
                return _collections;
            }
            set
            {
                _collections = value;
                RefreshBands();
            }
        }

        private BitmapSource _albumPhoto;
        public BitmapSource AlbumPhoto
        {
            get
            {
                return _albumPhoto;
            }

            set
            {
                _albumPhoto = value;
                OnPropertyChanged("AlbumPhoto");
            }
        }

        private Song _currentSong;
        public Song CurrentSong
        { 
            get
            {
                return _currentSong;
            }
            set
            {
                _currentSong = value;
                OnPropertyChanged("CurrentSong");
            }
        }

        private MusicSearchField _searchField;
        public MusicSearchField SearchField
        {
            get
            {
                return _searchField;
            }
            set
            {
                _searchField = value;
                OnPropertyChanged("SearchField");
            }
        }

        private SongStatus _status;
        public SongStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
                PlayButtonEnabled = (_status == SongStatus.Stopped) ? false : true;

                OnPropertyChanged("PlayButtonImage");
                OnPropertyChanged("PlayButtonMessage");
                OnPropertyChanged("Status");
            }
        }

        public Queue<Song> PlayQueue { get; set;}

        public SortedSet<Song> Songs { get; set; }

        public SortedSet<Album> Albums { get; set; }

        private CroppedBitmap PlayStateBitmap { get; set; }
        private CroppedBitmap PauseStateBitmap { get; set; }
        private CroppedBitmap StopStateBitmap { get; set; }
        private static BitmapImage PlayerControlsSourceImage { get; set; }

        public ImageSource PlayButtonImage
        {
            get
            {
                switch (Status)
                {
                    case SongStatus.Paused:
                        return PauseStateBitmap;
                    case SongStatus.Stopped:
                        return StopStateBitmap;
                    default:
                        return null;
                }
            }
        }

        public string PlayButtonMessage
        {
            get
            {
                switch(Status)
                {
                    case SongStatus.Paused:
                        return "Play";
                    case SongStatus.Playing:
                        return "Pause";
                    case SongStatus.Stopped:
                        return "Stopped";
                    default:
                        return null;
                }
            }
        }

        private bool _playButtonEnabled;
        public bool PlayButtonEnabled
        {
            get
            {
                return _playButtonEnabled;
            }
            set
            {
                _playButtonEnabled = value;
                OnPropertyChanged("PlayButtonEnabled");
            }
        }

        private SortedSet<Song> GetSongsFromCollection()
        {
            return  new SortedSet<Song>(from c in Collections
                                        from song in c.Songs
                                        select song);
        }

        private SortedSet<Band> GetBandsFromCollection()
        {
           return new SortedSet<Band>(from c in Collections
                                        from band in c.Bands
                                        select band);
        }

        private SortedSet<Album> GetAlbumsFromCollection()
        {
            return new SortedSet<Album>(from c in Collections
                                        from album in c.Albums
                                        select album);
        }

        public MainViewModel()
        {
            Player = new MediaPlayer();
            SourceBands = new SortedSet<Band>();
            Collections = new List<Collection>();
           /* Bands = GetBandsFromCollection();
            Songs = GetSongsFromCollection();
            Albums = GetAlbumsFromCollection();*/

            CurrentSong = Song.EmptySong;
            Status = SongStatus.Stopped;
            PlayList = new List<Song>();
            PlayQueue = new Queue<Song>();

            SearchField = new MusicSearchField(SourceBands, Bands);

            // Fill play button images
            /*PlayerControlsSourceImage = new BitmapImage(new Uri("pack://application:,,,/Resources/playerControls-.png"));

            var pauseStateImage = new CroppedBitmap(PlayerControlsSourceImage, new Int32Rect(0, 0, 48, 48));
            PauseStateBitmap = pauseStateImage;

            var stopStateImage = new CroppedBitmap(PlayerControlsSourceImage, new Int32Rect(58, 0, 48, 48));
            StopStateBitmap = stopStateImage;*/
        }

        /// <summary>
        /// Generates a new collection scanning recursively the specified dir
        /// for music files.
        /// </summary>
        /// <param name="musicScanDir">A dir containing music files.</param>
        public void ScanDirectory(string musicScanDir)
        {
            if (musicScanDir == null)
            {
                throw new ArgumentNullException("musicScanDir", "The collection data load path cant be null");
            }
            Collection data = CollectionFactory.ScanDirectory(musicScanDir);
            data.Reconnect();

            Collections.Add(data);
            RefreshBands();
        }

        /// <summary>
        /// Serializes music collections data, and
        /// saves in <see cref="CollectionSavePath"/>
        /// </summary>
        public void SaveCollections()
        {
            try
            {
                new JSONCollectionSource().Save(Collections);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (IOException)
            {
                throw;
            }
        }

        /// <summary>
        ///  Loads music data collections from the default file path.
        /// </summary>
        /// <returns>
        /// True if loaded correctly, false if file doesn't exists.
        /// </returns>
        public bool LoadCollections()
        {
            IList<Collection> collections = null;

            try
            {
                collections = new JSONCollectionSource().Load();
            }
            catch (IOException)
            {
                return false;
            }

            Collections.AddRange(collections);
            Collections.ForEach(action => action.Reconnect());
            RefreshBands();
            return true;
        }

        /// <summary>
        /// Handles MediaPlayer state, and changes playButton
        /// current image.
        /// </summary>
        public void Play()
        {
            if (Status == SongStatus.Stopped)
            {
                Player.Close();
                if (CurrentSong != Song.EmptySong)
                {
                    Player.Open(new Uri(CurrentSong.Album.Band.Collection.Directory + "\\" + CurrentSong.FilePath, UriKind.RelativeOrAbsolute));
                    Player.Play();

                    Status = SongStatus.Playing;
                }
            }
            else if (Status == SongStatus.Playing)
            {
                Player.Pause();

                Status = SongStatus.Paused;
            }
            else if (Status == SongStatus.Paused)
            {
                Player.Play();

                Status = SongStatus.Playing;
            }
        }

        /// <summary>
        /// Fills Bands set with each Collection data
        /// </summary>
        public void RefreshBands()
        {
            SourceBands.Clear();
            foreach (var collection in _collections)
            {
                SourceBands.UnionWith(collection.Bands);
            }
            Bands = new SortedSet<Band>(SourceBands);
            OnPropertyChanged("Bands");
        }

        public void FilterSearch()
        {
            if (SearchField.DoSearch())
            {
                // Show filtered set
                Bands = SearchField.ResultCollection;
                OnPropertyChanged("Bands");
            }
            else
            {
                // Reestablish full data
                RefreshBands();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public static string InitialSearchBoxText = "Search...";

        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                if (property != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(property));
                }
            }
        }

    }
}
