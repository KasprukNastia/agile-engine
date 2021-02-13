using System.Collections.Generic;

namespace PhotoStorageAPI.Models
{
    public class PhotosPage
    {
        public List<Photo> Pictures { get; set; }

        public int Page { get; set; }

        public int PageCount { get; set; }

        public bool HasMore { get; set; }
    }
}
