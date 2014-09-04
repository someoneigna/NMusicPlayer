using MusicPlayer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MusicPlayer.Data
{
    public class JSONCollectionSource
    {
        private string _savePath;
        private static readonly string CollectionDefaultSavePath = "collectionData.json";
        public JSONCollectionSource(string directory)
        {
            _savePath = directory;
        }

        public JSONCollectionSource() : this(CollectionDefaultSavePath)
        {
        }

        /// <summary>
        /// Save the <see cref="Collection"/>s into <see cref="CollectionSavePath"/>
        /// </summary>
        /// <param name="collections">A list of <see cref=""Collection"/>s</param>
        public void Save(IList<Collection> collections)
        {
            try
            {
                string json = null;
                json = Newtonsoft.Json.JsonConvert.SerializeObject(collections);

                if (File.Exists(_savePath) &&
                   new FileInfo(_savePath).Length == json.Length) // Change for a SHA1 hash check
                {
                    return;
                }

                File.WriteAllText(_savePath, json);
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
        /// Load a JSON file containing <see cref="Collection"/> information:
        /// </summary>
        /// <returns>A see list of <see cref="Collection"/> with the found data.</returns>
        public IList<Collection> Load()
        {
            List<Collection> collections = new List<Collection>();

            if (System.IO.File.Exists(_savePath))
            {
                using (var stream = new StreamReader(File.OpenRead(_savePath)))
                {
                    collections = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Collection>>(stream.ReadToEnd());
                }
            }
            return collections;
        }
    }
}
