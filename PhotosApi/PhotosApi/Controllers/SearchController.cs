using Microsoft.AspNetCore.Mvc;
using PhotoStorageAPI.Models;
using PhotoStorageAPI.Searching;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhotoStorageAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IPhotosSearcher _photosSearcher;

        public SearchController(IPhotosSearcher photosSearcher)
        {
            _photosSearcher = photosSearcher ?? throw new ArgumentNullException(nameof(photosSearcher));
        }

        [HttpGet]
        public async Task<List<DetailedPhoto>> GetPhotos([FromRoute] string searchTerm)
        {
            return await _photosSearcher.GetAllPhotosOnTerm(searchTerm);
        }
    }
}
