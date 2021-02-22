﻿using System;
using IPA.Utilities;
using SaberFactory.Configuration;
using SaberFactory.Helpers;
using UnityEngine;

namespace SaberFactory.Instances.Trail
{
    /// <summary>
    /// Class for interfacing with the direct trail rendering implementation
    /// </summary>
    internal class TrailHandler
    {
        public SFTrail TrailInstance { get; protected set; }

        protected InstanceTrailData _instanceTrailData;

        private readonly SaberTrail _backupTrail;

        public TrailHandler(GameObject gameobject)
        {
            TrailInstance = gameobject.AddComponent<SFTrail>();
        }

        public TrailHandler(GameObject gameobject, SaberTrail backupTrail) : this(gameobject)
        {
            _backupTrail = backupTrail;
        }

        public void CreateTrail(TrailConfig trailConfig)
        {
            if (_instanceTrailData is null)
            {

                if (_backupTrail is null) return;

                var trailStart = TrailInstance.gameObject.CreateGameObject("Trail Start");
                var trailEnd = TrailInstance.gameObject.CreateGameObject("TrailEnd");
                trailEnd.transform.localPosition = new Vector3(0, 0, 1);

                var trailRenderer = _backupTrail.GetField<SaberTrailRenderer, SaberTrail>("_trailRendererPrefab");

                var material = trailRenderer.GetField<MeshRenderer, SaberTrailRenderer>("_meshRenderer").material;

                var trailInitDataVanilla = new SFTrail.TrailInitData
                {
                    TrailColor = Color.white,
                    TrailLength = 15,
                    Whitestep = 0.02f,
                    UVMultiplier = trailConfig.UVMultiplier,
                    Granularity = trailConfig.Granularity,
                    SamplingFrequency = trailConfig.SamplingFrequency
                };

                TrailInstance.Setup(
                    trailInitDataVanilla,
                    material,
                    trailStart.transform,
                    trailEnd.transform
                );
                return;
            }

            var trailInitData = new SFTrail.TrailInitData
            {
                TrailColor = Color.white,
                TrailLength = _instanceTrailData.Length,
                Whitestep = _instanceTrailData.WhiteStep,
                UVMultiplier = trailConfig.UVMultiplier,
                Granularity = trailConfig.Granularity,
                SamplingFrequency = trailConfig.SamplingFrequency
            };

            Transform pointStart = _instanceTrailData.IsTrailReversed
                ? _instanceTrailData.PointEnd
                : _instanceTrailData.PointStart;

            Transform pointEnd = _instanceTrailData.IsTrailReversed
                ? _instanceTrailData.PointStart
                : _instanceTrailData.PointEnd;

            TrailInstance.Setup(
                trailInitData,
                _instanceTrailData.Material.Material,
                pointStart,
                pointEnd
            );
        }

        public void DestroyTrail()
        {
            TrailInstance.TryDestoryImmediate();
        }

        public void SetTrailData(InstanceTrailData instanceTrailData)
        {
            _instanceTrailData = instanceTrailData;
        }

        public void SetColor(Color color)
        {
            if (TrailInstance is {})
            {
                TrailInstance.Color = color;
            }
        }
    }
}