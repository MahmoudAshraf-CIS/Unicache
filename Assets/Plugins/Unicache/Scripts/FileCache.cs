﻿using System.Collections.Generic;
using System.IO;
using UniRx;

namespace Unicache
{
    public class FileCache : IUnicache
    {
        public ICacheHandler Handler { get; set; }
        public IUrlLocator UrlLocator { get; set; }
        public ICacheLocator CacheLocator { get; set; }

        public FileCache()
        {
        }

        public IObservable<byte[]> Fetch(string key)
        {
            var url = this.UrlLocator.CreateUrl(key);
            var path = this.CacheLocator.CreateCachePath(key);

            if (this.HasCacheByPath(path))
            {
                return Observable.Return(this.GetCacheByPath(path));
            }
            else
            {
                var observable = this.Handler.Fetch(url)
                    .Do(_ => this.RemoveCachesByKey(key))
                    .Do(data => this.SetCacheByPath(path, data))
                    .Select(_ => this.GetCacheByPath(path));
                return this.AsAsync(observable);
            }
        }

        // this is for test
        protected virtual IObservable<byte[]> AsAsync(IObservable<byte[]> observable)
        {
            return observable
                .SubscribeOn(Scheduler.ThreadPool)
                .ObserveOnMainThread();
        }

        private void RemoveCachesByKey(string key)
        {
            var allFiles = Directory.GetFiles(UnicacheConfig.Directory);
            var keyFiles = new List<string>(this.CacheLocator.GetSameKeyCachePathes(key, allFiles));

            foreach (var file in keyFiles)
            {
                File.Delete(file);
            }
        }

        public void ClearAll()
        {
            IO.CleanDirectory(UnicacheConfig.Directory);
        }

        public byte[] GetCache(string key)
        {
            return this.GetCacheByPath(this.CacheLocator.CreateCachePath(key));
        }

        private byte[] GetCacheByPath(string path)
        {
            return IO.Read(UnicacheConfig.Directory + path);
        }

        public void SetCache(string key, byte[] data)
        {
            this.SetCacheByPath(this.CacheLocator.CreateCachePath(key), data);
        }

        private void SetCacheByPath(string path, byte[] data)
        {
            IO.MakeParentDirectory(UnicacheConfig.Directory + path);
            IO.Write(UnicacheConfig.Directory + path, data);
        }

        public bool HasCache(string key)
        {
            return this.HasCacheByPath(this.CacheLocator.CreateCachePath(key));
        }

        private bool HasCacheByPath(string path)
        {
            return File.Exists(UnicacheConfig.Directory + path);
        }
    }
}