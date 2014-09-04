using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Models
{
    public class Song : IComparable<Song>
    {
        public int Id { get; set; }
        public uint? Track { get; set; }
        public string Name { get; set; }
        public TimeSpan? Duration { get; set; }

        [JsonIgnore]
        public virtual Album  Album { get; set; } // Used for playlist generation

        public string FilePath { get; set; }

        public Song()
        {
        }

        public static Song EmptySong = new Song { Id = -1, Name = "", Duration = TimeSpan.FromSeconds(0), FilePath = ""};
        public static Song TestSong = new Song { Id = 0, Name = "Doom E1M1", Duration = null, FilePath = @"DoomMetal\D_E1M1.mp3" };

        public int CompareTo(Song other)
        {
            if (other.Track == null || this.Track == null)
            {
                return string.Compare(this.Name, other.Name, true, System.Globalization.CultureInfo.InvariantCulture);
            }
            return (int)other.Track.Value - (int)Track.Value;
        }

    }
}
