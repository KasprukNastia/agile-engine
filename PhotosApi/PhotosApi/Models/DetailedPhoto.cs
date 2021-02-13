using Newtonsoft.Json;

namespace PhotoStorageAPI.Models
{
    public class DetailedPhoto : Photo
    {
        public string Author { get; set; }
        public string Camera { get; set; }
        public string Tags { get; set; }

        [JsonProperty("full_picture")]
        public string FullPicture { get; set; }
    }
}
