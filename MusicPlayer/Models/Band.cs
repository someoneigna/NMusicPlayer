using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Models
{
    public class Band : IComparable<Band>
    {
        public ISet<Album> Albums { get; set; }
        public string Name { get; set; }

        [JsonIgnore]
        public virtual Collection Collection { get; set; } // To retrieve path for each song

        public Band()
        {
            Albums = new SortedSet<Album>();
        }

        public int CompareTo(Band other)
        {
            return string.Compare(this.Name, other.Name, true, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
