using Microsoft.Identity.Client;
using System;
using System.IO;

namespace ADLib.Controllers
{
    internal class TokenCacheHelper
    {
        private readonly string _cacheFilePath;
        private readonly object _fileLock = new object();

        public TokenCacheHelper(string cacheFilePath)
        {
            _cacheFilePath = cacheFilePath;
            if (!File.Exists(_cacheFilePath))
            {
                File.Create(_cacheFilePath).Dispose();
            }
        }

        public void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            lock (_fileLock)
            {
                args.TokenCache.DeserializeMsalV3(File.Exists(_cacheFilePath)
                    ? File.ReadAllBytes(_cacheFilePath)
                    : null);
            }
        }

        public void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (args.HasStateChanged)
            {
                lock (_fileLock)
                {
                    File.WriteAllBytes(_cacheFilePath, args.TokenCache.SerializeMsalV3());
                }
            }
        }

        public void BeforeWriteNotification(TokenCacheNotificationArgs args)
        {
            lock (_fileLock)
            {
                Console.WriteLine("Token cache is being written to the file.");
            }
        }
    }
}
