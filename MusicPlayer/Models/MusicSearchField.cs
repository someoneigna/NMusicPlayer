using GalaSoft.MvvmLight.Command;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace MusicPlayer.Models
{
    public class MusicSearchField : INotifyPropertyChanged, IDataErrorInfo
    {
        public MusicSearchField(ISet<Band> sourceData, ISet<Band> resultData)
        {
            SearchCommand = new RelayCommand(() => DoSearch(), () => CanExecuteSearchCommand());
            if (sourceData == null)
            {
                throw new ArgumentNullException("sourceData", "The source collection cant be null.");
            }

            if (resultData == null)
            {
                throw new ArgumentNullException("resultData", "The result collection cant be null.");
            }

            ResultCollection = resultData;
            SourceCollection = sourceData;
            Input = "";
        }

        public ICommand SearchCommand
        {
            get;
            set;
        }

        private bool CanExecuteSearchCommand()
        {
            var args = GetSearchArguments(Input);
            return (args != null && args.Count > 0);
        }

        private void SearchExecute()
        {
            DoSearch();
        }

        public bool DoSearch()
        {
            var arguments = GetSearchArguments(Input);
            if (arguments == null) { throw new ArgumentNullException("args", "Search arguments cant be null."); }

            IEnumerable<Band> query = null;

            foreach(var arg in arguments)
            {
                if (arg.Value == "")
                {
                    continue;
                }

                string key = (arg.Key != null) ? arg.Key.ToLower() : "";
                switch (key)
                {

                    case "song":
                        // Fall through
                    case "":
                        var querySetSong = new SortedSet<Band>();
                        foreach(var band in SourceCollection)
                        {
                            bool appendBand = false;
                            var newBand = new Band(){Name = band.Name, Collection = band.Collection};
                            foreach(var album in band.Albums)
                            {
                                var newAlbum = new Album(){Band=band, Name=album.Name, Year=album.Year};
                                foreach(var song in album.Songs)
                                {
                                    if (song.Name.StartsWith(arg.Value, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        newAlbum.Songs.Add(song);
                                    }
                                }

                                if (newAlbum.Songs.Count > 0)
                                {
                                    newBand.Albums.Add(newAlbum);
                                    appendBand = true;
                                }
                            }
                            if (appendBand)
                            {
                                querySetSong.Add(newBand);
                                appendBand = false;
                            }
                        }
                        query = (querySetSong.Count > 0) ? querySetSong : null;
                        break;
                    case "band":
                        query = SourceCollection
                            .Where<Band>(band => 
                                band.Name
                                    .StartsWith(arg.Value, StringComparison.InvariantCultureIgnoreCase));
                        break;
                    case "album":

                        var querySet = new SortedSet<Band>();
                        foreach(var band in SourceCollection)
                        {
                            var newBand = new Band(){Name = band.Name, Collection = band.Collection};
                            foreach(var album in band.Albums)
                            {
                                if (album.Name.StartsWith(arg.Value, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    newBand.Albums.Add(album);
                                }
                            }
                            if (newBand.Albums.Count > 0) { querySet.Add(newBand); }
                        }
                        query = (querySet.Count > 0) ? querySet : null;
                        break;
                    default:
                        return false;
                }
                if (query == null)
                {
                    ResultCollection.Clear();
                    break;
                }
                else
                {
                    ResultCollection = new SortedSet<Band>(query);
                }
               
            }
            return true;
        }


        private string fieldInput;
        public static readonly string InitialInputValue = "Search...";


        public string Input
        {
            get { return fieldInput; }
            set
            {
                fieldInput = value;
                OnPropertyChanged("Input");
            }
        }

        private string LastSearchInput { get; set; }

        public bool SearchInputChanged { get { return LastSearchInput != Input; } }

        public string Error
        {
            get { throw new NotImplementedException(); }
        }

        public ISet<Band> SourceCollection { get; set; }
        
        private ISet<Band> _resultCollection;
        public ISet<Band> ResultCollection
        { 
            get
            {
                return _resultCollection;
            }
            set
            {
                _resultCollection = value;
                OnPropertyChanged("ResultCollection");
            }
        }


        /// <summary>
        /// Gets search parameters for query from input.
        /// </summary>
        /// <param name="input">Text input in "parameter:value" format.</param>
        /// <returns>A dictionary containing paired argument:value.</returns>
        public Dictionary<string, string> GetSearchArguments(string input)
        {

            LastSearchInput = input;
            MatchCollection results = null;
            Dictionary<string, string> arguments = new Dictionary<string, string>();

            if (input == null) { throw new ArgumentNullException("input", "Input string cant be null."); }

            if (!string.IsNullOrEmpty(input))
            {
                results = Regex.Matches(input, "\\w+:+\\w+");
                if (results.Count > 0)
                {
                    for (int i = 0; i < results.Count; i++)
                    {
                        if (results[i].Value.Contains(":"))
                        {
                            string value, key;
                            string[] keypair = results[i].Value.Split(':');
                            key = keypair[0];
                            value = keypair[1];
                            arguments.Add(key, value);
                        }
                    }
                    return arguments;
                }
                else
                {
                    arguments.Add("", input);
                }
                return arguments;

            }
            return null;
        }

        public string this[string columnName]
        {
            get
            {
                if (columnName == "Input")
                {
                    // If validation passes 
                    if (    
                            !string.IsNullOrEmpty(Input) &&
                            Input != InitialInputValue
                        )
                    {
                        Dictionary<string, string> arguments = GetSearchArguments(Input);

                        if (arguments == null || arguments.Count == 0)
                        {
                            return "Input don't complies with the correct format.";
                        }
                    }
                    else
                    {
                        return "Input cant be empty.";
                    }

                }
                LastSearchInput = Input;
                return null;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            var propertyChanged = PropertyChanged;
            if (propertyChanged != null)
            {
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
