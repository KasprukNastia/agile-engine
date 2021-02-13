namespace PhotoStorageAPI.Models
{
    public class PhotosApiClientSettings
    {
        public string BaseAddresUrl { get; set; }
        public string ApiKey { get; set; }
        public int ApiRetriesCount { get; set; }
        public int TimeoutSeconds { get; set; }
    }
}
