﻿using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using SaberFactory.Helpers;
using SaberFactory.UI;
using SaberFactory.UI.Lib;
using SiraUtil;
using SiraUtil.Web;
using UnityEngine;

namespace SaberFactory.Models
{
    internal class RemoteLocationPart : ICustomListItem
    {
        public readonly string RemoteLocation;
        private readonly DirectoryInfo _customSaberDir;
        private readonly string _filename;

        private readonly IHttpService _webClient;

        private RemoteLocationPart(InitData initData, IHttpService webClient, PluginDirectories pluginDirs)
        {
            _webClient = webClient;
            _customSaberDir = pluginDirs.CustomSaberDir;

            RemoteLocation = initData.RemoteLocation;
            ListName = initData.Name;
            ListAuthor = initData.Author + " : <color=green>Download</color>";
            _filename = initData.Filename;

            if (!string.IsNullOrEmpty(initData.CoverPath))
            {
                var data = Utilities.GetResource(Assembly.GetExecutingAssembly(), initData.CoverPath);
                ListCover = BeatSaberMarkupLanguage.Utilities.LoadSpriteRaw(data);
            }
        }

        public string ListName { get; }

        public string ListAuthor { get; }

        public Sprite ListCover { get; }

        public bool IsFavorite { get; }

        public async Task<Tuple<bool, string>> Download(CancellationToken token)
        {
            try
            {
                var response = await _webClient.GetAsync(RemoteLocation, null, token);
                if (!response.Successful)
                {
                    return default;
                }

                var filename = GetFilename();
                File.WriteAllBytes(_customSaberDir.GetFile(filename).FullName, await response.ReadAsByteArrayAsync());
                return new Tuple<bool, string>(true, "CustomSabers\\" + filename);
            }
            catch (Exception)
            {
                return default;
            }
        }

        private string GetFilename()
        {
            return _filename;
            //var split = RemoteLocation.Split('/');
            //return split.Last();
        }

        public struct InitData
        {
            public string RemoteLocation;
            public string Name;
            public string Author;
            public string Filename;
            public string CoverPath;
        }
    }
}