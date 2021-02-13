using Newtonsoft.Json;

namespace PhotoStorageAPI.Models
{
    public class Photo
    {
        public string Id { get; set; }

        [JsonProperty("cropped_picture")]
        public string CroppedPicture { get; set; }
    }
}
