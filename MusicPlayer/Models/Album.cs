using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Models
{
    public class Album : IComparable<Album>
    {
        public ISet<Song> Songs { get; set; }
        public string Name { get; set; }
        public uint Year { get; set; }

        [JsonIgnore]
        public virtual Band Band { get; set; }

        public Album()
        {
            Songs = new SortedSet<Song>();
        }

        public int CompareTo(Album other)
        {
            return string.Compare(this.Name, other.Name, true, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
