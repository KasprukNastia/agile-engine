using PhotoStorageAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoStorageAPI.ExternalApi
{
    public interface IPhotosExternalClient
    {
        Task<List<PhotosPage>> GetAllPhotosPages();

        Task<List<DetailedPhoto>> GetAllPhotosDetails(List<string> photosIds);
    }
}
