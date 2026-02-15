using System;
using System.Threading.Tasks;
using EVEMon.Common.Service;

namespace EVEMon.Common.Services
{
    public sealed class ImageServiceAdapter : Core.Interfaces.IImageService
    {
        public async Task<object?> GetImageAsync(Uri url, bool useCache = true)
        {
            return await ImageService.GetImageAsync(url, useCache);
        }
    }
}
