using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Models
{
    public class Collection
    {
        public ISet<Band> Bands { get; set; }
        [JsonIgnore]
        public ISet<Song> Songs { get; set; }
        [JsonIgnore]
        public ISet<Album> Albums { get; set; }

        public string Directory { get; set; }

        public Collection()
        {
            Bands = new SortedSet<Band>();
            Songs = new SortedSet<Song>();
            Albums = new SortedSet<Album>();
        }

        public Collection(string directory)
        {
            Bands = new SortedSet<Band>();
            Directory = directory;
        }


        public static Collection TestCollection = new Collection("");


        // Connects topdown songs -> album -> band for fast playlist generation
        public void Reconnect()
        {
            // Reconnect songs to album for fast playlist generation
            foreach (var band in Bands)
            {
                foreach (var album in band.Albums)
                {
                    foreach (var song in album.Songs)
                    {
                        song.Album = album;
                    }
                    album.Band = band;
                }
                band.Collection = this;
            }
        }
    }
}
