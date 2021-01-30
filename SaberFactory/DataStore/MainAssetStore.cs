﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SaberFactory.Configuration;
using SaberFactory.Helpers;
using SaberFactory.Loaders;
using SaberFactory.Models;
using SaberFactory.Models.CustomSaber;
using SiraUtil.Tools;

namespace SaberFactory.DataStore
{
    internal class MainAssetStore : IDisposable
    {
        private static readonly int MAX_LOADING_THREADS = 15;

        public bool IsLoading { get; private set; }
        public Task CurrentTask;

        private readonly CustomSaberAssetLoader _customSaberAssetLoader;
        private readonly CustomSaberModelLoader _customSaberModelLoader;

        private readonly PluginConfig _config;
        private readonly SiraLog _logger;

        private readonly Dictionary<string, ModelComposition> _modelCompositions;

        private MainAssetStore(
            PluginConfig config,
            SiraLog logger,
            CustomSaberModelLoader customSaberModelLoader)
        {
            _config = config;
            _logger = logger;

            _customSaberAssetLoader = new CustomSaberAssetLoader();
            _customSaberModelLoader = customSaberModelLoader;

            _modelCompositions = new Dictionary<string, ModelComposition>();
        }

        public Task<ModelComposition> this[string path] => GetCompositionByPath(path);

        public async Task<ModelComposition> GetCompositionByPath(string path)
        {
            if (_modelCompositions.TryGetValue(path, out var result)) return result;

            return await LoadComposition(path);
        }

        public void Dispose()
        {
            UnloadAll();
        }

        public async Task LoadAllAsync(EAssetTypeConfiguration assetType)
        {
            await LoadAllCustomSabersAsync();
            //if (assetType == EAssetType.SaberFactory)
            //{
            //    await LoadAllCustomSabersAsync();
            //}else if (assetType == EAssetType.CustomSaber)
            //{
            //    // Part Loading
            //}
        }

        public async Task LoadAllCustomSabersAsync()
        {
            if (!IsLoading)
            {
                IsLoading = true;
                CurrentTask = LoadAllCustomSabersAsyncInternal(_config.LoadingThreads);
            }

            await CurrentTask;
            IsLoading = false;
        }

        public List<ModelComposition> GetAllModelCompositions()
        {
            return _modelCompositions.Values.ToList();
        }

        public void UnloadAll()
        {
            foreach (var modelCompositions in _modelCompositions.Values)
            {
                modelCompositions.Dispose();
            }
            _modelCompositions.Clear();
        }

        public void Unload(string path)
        {
            if (!_modelCompositions.TryGetValue(path, out var comp)) return;
            comp.Dispose();
            _modelCompositions.Remove(path);
        }

        public async Task Reload(string path)
        {
            Unload(path);
            await LoadComposition(path);
        }

        public async Task ReloadAll()
        {
            UnloadAll();
            await LoadAllCustomSabersAsync();
        }

        public void Delete(string path)
        {
            Unload(path);
            var filePath = PathTools.ToFullPath(path);
            File.Delete(filePath);
        }

        private async Task LoadAllCustomSabersAsyncInternal(int threads)
        {
            threads = Math.Min(threads, MAX_LOADING_THREADS);

            var tasks = new List<Task>();
            var files = new ConcurrentQueue<string>(_customSaberAssetLoader.CollectFiles());

            _logger.Info($"{files.Count} custom sabers found");
            if (files.Count == 0) return;

            var sw = Stopwatch.StartNew();

            async Task LoadingThread()
            {
                while (files.TryDequeue(out string file))
                {
                    var relativePath = PathTools.ToRelativePath(file);
                    if (_modelCompositions.ContainsKey(relativePath)) continue;

                    await LoadComposition(relativePath);
                }
            }

            for (int i = 0; i < threads; i++)
            {
                tasks.Add(LoadingThread());
            }

            await Task.WhenAll(tasks);

            sw.Stop();
            _logger.Info($"Loaded in {sw.Elapsed.Seconds}.{sw.Elapsed.Milliseconds}s");
        }

        private void AddModelComposition(string key, ModelComposition modelComposition)
        {
            if(!_modelCompositions.ContainsKey(key)) _modelCompositions.Add(key, modelComposition);
        }

        private async Task<ModelComposition> LoadModelCompositionAsync(string bundlePath)
        {
            // TODO: Switch between customsaber and part implementation

            AssetBundleLoader loader = _customSaberAssetLoader;
            IStoreAssetParser modelCreator = _customSaberModelLoader;

            var storeAsset = await loader.LoadStoreAssetAsync(bundlePath);
            if (storeAsset == null) return null;
            var model = modelCreator.GetComposition(storeAsset);

            return model;
        }

        private async Task<ModelComposition> LoadComposition(string path)
        {
            var composition = await LoadModelCompositionAsync(path);
            if(composition!=null) _modelCompositions.Add(path, composition);
            return composition;
        }
    }
}