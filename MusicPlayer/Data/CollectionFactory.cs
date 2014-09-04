using MusicPlayer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicPlayer.Data
{
    public static class CollectionFactory
    {
        private static string UnknownBandName = "Unknown";

        public class TagLibFile : TagLib.File.IFileAbstraction
        {
            private FileInfo file;
            
            public TagLibFile(FileInfo file)
            {
                this.file = file;
            }

            public void CloseStream(Stream stream)
            {
                stream.Close();
            }

            public string Name
            {
                get
                {
                    return file.Name;
                }
            }

            public Stream ReadStream
            {
                get
                {
                    return file.OpenRead();
                }
            }

            public Stream WriteStream
            {
                get
                {
                    return file.OpenWrite();
                }
            }
        }

        public struct TagInformation
        {
            public string Band, Album, Title;
            public uint Track;

            public TagInformation(TagLib.Tag tag)
            {
                Band = tag.FirstAlbumArtist ?? tag.FirstPerformer ?? tag.FirstComposer ?? UnknownBandName;
                Title = tag.Title;
                Track = tag.Track;
                Album = tag.Album ?? tag.Title;
            }
        }

        /// <summary>
        /// Scans the specified path for music files.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>A <see cref="Collection"/> with tag information.</returns>
        public static Collection ScanDirectory(string path)
        {
            Collection collection = new Collection(path);
            DirectoryInfo directory = new DirectoryInfo(path);

            foreach (var file in directory.EnumerateFiles("*.mp3", SearchOption.AllDirectories))
            {
                var libFile = new TagLibFile(file);
                TagLib.File tagLibFile = null;

                try
                {
                    tagLibFile = TagLib.File.Create(libFile);
                }
                catch(TagLib.CorruptFileException)
                {
                    File.Move(file.FullName, file.FullName + "_corrrupt_");
                    continue;
                }
                catch(TagLib.UnsupportedFormatException)
                {
                    continue;
                }
                catch(IOException)
                {
                    continue;
                }

                using (tagLibFile)
                {
                    Band band = null;
                    Album album = null;
                    Song song = null;

                    TagLib.Tag tag = tagLibFile.Tag;

                    TagInformation information = new TagInformation(tag);

                    if (information.Title == null)
                    {
                        information.Title = file.Name;
                    }

                    if (information.Album == null) 
                    {
                        information.Album = information.Title;
                    }
                    
                    band = collection.Bands.FirstOrDefault(b => b.Name == information.Band);
                    // If no band with that name found, make a new one
                    if (band == null)
                    {
                        band = new Band();
                        band.Name = information.Band;
                        collection.Bands.Add(band);
                    }

                    album = band.Albums.FirstOrDefault(a => a.Name == information.Album);
                    // If no album with that name found, or its a new band
                    if (album == null)
                    {
                        album = new Album();
                        album.Name = information.Album;
                        album.Year = tag.Year;
                        band.Albums.Add(album);
                    }

                    song = album.Songs.FirstOrDefault(s => s.Name == information.Title);
                    // If that song exists, add the song suffixing with '_'
                    if (song != null)
                    {
                        information.Title += "_";
                    }

                    song = new Song();
                    album.Songs.Add(song);

                    song.Name = information.Title;
                    song.FilePath = file.FullName.Replace(path, "");
                    song.Duration = null;
                    song.Track = information.Track;

                    band.Albums.Add(album);

                }
           }

           return collection;
        }

    }
}
