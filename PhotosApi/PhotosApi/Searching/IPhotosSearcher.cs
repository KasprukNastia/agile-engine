using PhotoStorageAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoStorageAPI.Searching
{
    public interface IPhotosSearcher
    {
        Task<List<DetailedPhoto>> GetAllPhotosOnTerm(string searchTerm);
    }
}
