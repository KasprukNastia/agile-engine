using Microsoft.Extensions.Caching.Memory;
using PhotoStorageAPI.ExternalApi;
using PhotoStorageAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PhotoStorageAPI.Searching
{
    public class PhotosSearcher : IPhotosSearcher
    {
        private static readonly object locker = new object();

        private bool _cacheLoaded;

        private readonly IMemoryCache _memoryCache;
        private readonly IPhotosExternalClient _photosExternalClient;

        public PhotosSearcher(
            IMemoryCache memoryCache,
            IPhotosExternalClient photosExternalClient)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _photosExternalClient = photosExternalClient ?? throw new ArgumentNullException(nameof(photosExternalClient));
        }

        public async Task<List<DetailedPhoto>> GetAllPhotosOnTerm(string searchTerm)
        {
            if (!_cacheLoaded)
                await LoadCache();

            if (!_memoryCache.TryGetValue("photosIds", out List<string> photosIds))
                return new List<DetailedPhoto>();

            List<DetailedPhoto> foundPhotos = new List<DetailedPhoto>();
            foreach(string photoId in photosIds)
            {
                if (_memoryCache.TryGetValue(photoId, out DetailedPhoto detailedPhoto) &&
                    (detailedPhoto.Author.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    detailedPhoto.Camera.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    detailedPhoto.Tags.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                    foundPhotos.Add(detailedPhoto);
            }

            return foundPhotos;
        }

        private async Task LoadCache()
        {
            if (!Monitor.TryEnter(locker, TimeSpan.FromSeconds(5)))
                return;

            if (_cacheLoaded)
                return;

            List<PhotosPage> photosPages = await _photosExternalClient.GetAllPhotosPages();

            List<string> photosIds = photosPages.SelectMany(page => page.Pictures.Select(p => p.Id)).ToList();
            _memoryCache.Set("photosIds", photosIds);

            List<DetailedPhoto> detailedPhotos = await _photosExternalClient.GetAllPhotosDetails(photosIds);
            detailedPhotos.ForEach(photo => _memoryCache.Set(photo.Id, photo));

            _cacheLoaded = true;

            Monitor.Exit(locker);
        }
    }
}
