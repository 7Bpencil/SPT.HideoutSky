//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.WeaponModding;
using Newtonsoft.Json;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using SystemObject = System.Object;

namespace SevenBoldPencil.HideoutSky
{
    public class SkyData
    {
        public Light Sunlight;
        public Transform SunlightTransform;
        public Material SkyboxMaterial;
        public Cubemap SkyboxCubemap;
        public Mesh SkyboxMesh;
    }

    [BepInPlugin("7Bpencil.HideoutSky", "7Bpencil.HideoutSky", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static readonly int _Tint = Shader.PropertyToID("_Tint");
        public static readonly int _Exposure = Shader.PropertyToID("_Exposure");
        public static readonly int _Rotation = Shader.PropertyToID("_Rotation");
        public static readonly int _Tex = Shader.PropertyToID("_Tex");

        public static Plugin Instance;
		public ManualLogSource LoggerInstance;

        public static ConfigEntry<float> SunLightColorH;
        public static ConfigEntry<float> SunLightColorS;
        public static ConfigEntry<float> SunLightColorV;
        public static ConfigEntry<float> SunElevationAngle;
        public static ConfigEntry<float> SunAzimuthAngle;
        public static ConfigEntry<float> SunIntensity;
        public static ConfigEntry<LightShadows> SunShadowType;
        public static ConfigEntry<float> SunShadowStrength;

        public static ConfigEntry<float> CubemapTintH;
        public static ConfigEntry<float> CubemapTintS;
        public static ConfigEntry<float> CubemapTintV;
        public static ConfigEntry<float> CubemapExposure;
        public static ConfigEntry<float> CubemapRotation;

        public Option<SkyData> SkyData;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            // TODO make proper setting groups
            SunLightColorH = Config.Bind<float>("Main", "Sun | Light Color Hue", 0.08169935f, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            SunLightColorS = Config.Bind<float>("Main", "Sun | Light Color Saturation", 0.4f, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            SunLightColorV = Config.Bind<float>("Main", "Sun | Light Color Value", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            SunElevationAngle = Config.Bind<float>("Main", "Sun | Elevation Angle", 27f, new ConfigDescription("", new AcceptableValueRange<float>(0, 90)));
            SunAzimuthAngle = Config.Bind<float>("Main", "Sun | Azimuth Angle", 148f, new ConfigDescription("", new AcceptableValueRange<float>(0, 360)));
            SunIntensity = Config.Bind<float>("Main", "Sun | Intensity", 0.6f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 8f)));
            SunShadowType = Config.Bind<LightShadows>("Main", "Sun | Shadow Type", LightShadows.Soft);
            SunShadowStrength = Config.Bind<float>("Main", "Sun | Shadow Strength", 1f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));

            SunLightColorH.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunLightColor(skyData.Sunlight); } };
            SunLightColorS.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunLightColor(skyData.Sunlight); } };
            SunLightColorV.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunLightColor(skyData.Sunlight); } };
            SunElevationAngle.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunAngle(skyData.SunlightTransform); } };
            SunAzimuthAngle.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunAngle(skyData.SunlightTransform); } };
            SunIntensity.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunIntensity(skyData.Sunlight); } };
            SunShadowType.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunShadowType(skyData.Sunlight); } };
            SunShadowStrength.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunShadowStrength(skyData.Sunlight); } };

            CubemapTintH = Config.Bind<float>("Main", "Cubemap | Tint Hue", 0f, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            CubemapTintS = Config.Bind<float>("Main", "Cubemap | Tint Saturation", 0f, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            CubemapTintV = Config.Bind<float>("Main", "Cubemap | Tint Value", 0.8f, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            CubemapExposure = Config.Bind<float>("Main", "Cubemap | Exposure", 0.3f, new ConfigDescription("", new AcceptableValueRange<float>(0, 8)));
            CubemapRotation = Config.Bind<float>("Main", "Cubemap | Rotation", 190, new ConfigDescription("", new AcceptableValueRange<float>(0, 360)));

            CubemapTintH.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetCubemapTint(skyData.SkyboxMaterial); } };
            CubemapTintS.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetCubemapTint(skyData.SkyboxMaterial); } };
            CubemapTintV.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetCubemapTint(skyData.SkyboxMaterial); } };
            CubemapExposure.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetCubemapExposure(skyData.SkyboxMaterial); } };
            CubemapRotation.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetCubemapRotation(skyData.SkyboxMaterial); } };

            new Patch_HideoutController_HideoutAwake().Enable();
            new Patch_HideoutController_OnDestroy().Enable();
        }

        public void SetSunLightColor(Light sunLight)
        {
            sunLight.color = Color.HSVToRGB(SunLightColorH.Value, SunLightColorS.Value, SunLightColorV.Value);
        }

        public void SetSunAngle(Transform sunTransform)
        {
            sunTransform.eulerAngles = new(SunElevationAngle.Value, SunAzimuthAngle.Value, 0);
        }

        public void SetSunIntensity(Light sunLight)
        {
            sunLight.intensity = SunIntensity.Value;
        }

        public void SetSunShadowType(Light sunLight)
        {
            sunLight.shadows = SunShadowType.Value;
        }

        public void SetSunShadowStrength(Light sunLight)
        {
            sunLight.shadowStrength = SunShadowStrength.Value;
        }

        public void SetCubemapTint(Material skyboxMaterial)
        {
            skyboxMaterial.SetColor(_Tint, Color.HSVToRGB(CubemapTintH.Value, CubemapTintS.Value, CubemapTintV.Value));
        }

        public void SetCubemapExposure(Material skyboxMaterial)
        {
            skyboxMaterial.SetFloat(_Exposure, CubemapExposure.Value);
        }

        public void SetCubemapRotation(Material skyboxMaterial)
        {
            skyboxMaterial.SetFloat(_Rotation, CubemapRotation.Value);
        }

        public void LoadSkybox()
        {
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var bundlePath = Path.Combine(assemblyDir, "assets", "bundles", "hideout-sky");
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            var mesh = bundle.LoadAsset<Mesh>("Assets/HideoutSky/Meshes/atmosphere.mesh");
            bundle.UnloadAsync(false);

            var atmosphere = new GameObject("Skybox");
            {
                atmosphere.transform.localScale = new Vector3(1000, 1000, 1000);
            }

            var meshFilter = atmosphere.AddComponent<MeshFilter>();
            {
                meshFilter.sharedMesh = mesh;
            }

            var meshRenderer = atmosphere.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Skybox/Cubemap"));
            var cubemap = LoadCubemap(Path.Combine(assemblyDir, "assets", "cubemap"));
            {
                SetCubemapTint(material);
                SetCubemapExposure(material);
                SetCubemapRotation(material);
                material.SetTexture(_Tex, cubemap);
                meshRenderer.material = material;
            }

            var sunLight = new GameObject("SunLight");
            var light = sunLight.AddComponent<Light>();
            var lightTransform = light.transform;
            {
                light.type = LightType.Directional;
                SetSunLightColor(light);
                SetSunAngle(lightTransform);
                SetSunIntensity(light);
                SetSunShadowType(light);
                SetSunShadowStrength(light);
            }

            SkyData = new(new()
            {
                Sunlight = light,
                SunlightTransform = lightTransform,
                SkyboxMaterial = material,
                SkyboxCubemap = cubemap,
                SkyboxMesh = mesh,
            });
        }

        public static Cubemap LoadCubemap(string directoryPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false, linear: false, createUninitialized: true);

            LoadCubemapFace(directoryPath, "right", texture);
            var cube = new Cubemap(texture.width, TextureFormat.RGBA32, false, false);
            cube.SetPixels(texture.GetPixels(), CubemapFace.PositiveX);

            LoadCubemapFace(directoryPath, "left", texture);
            cube.SetPixels(texture.GetPixels(), CubemapFace.NegativeX);

            LoadCubemapFace(directoryPath, "top", texture);
            cube.SetPixels(texture.GetPixels(), CubemapFace.PositiveY);

            LoadCubemapFace(directoryPath, "front", texture);
            cube.SetPixels(texture.GetPixels(), CubemapFace.PositiveZ);

            LoadCubemapFace(directoryPath, "back", texture);
            cube.SetPixels(texture.GetPixels(), CubemapFace.NegativeZ);

            cube.Apply();

            Destroy(texture);

            return cube;
        }

        public static void LoadCubemapFace(string directoryPath, string faceName, Texture2D texture)
        {
            var filePath = Path.Combine(directoryPath, $"{faceName}.png");
            var fileBytes = File.ReadAllBytes(filePath);
            if (!ImageConversion.LoadImage(texture, fileBytes, markNonReadable: false))
            {
                throw new Exception($"Failed to load cubemap: {filePath}");
            }
        }

        public void UnloadSkybox()
        {
            if (SkyData.Some(out var skyData))
            {
                SkyData = default;
                Destroy(skyData.SkyboxCubemap);
                Destroy(skyData.SkyboxMaterial);
                Destroy(skyData.SkyboxMesh);
            }
        }

#if DEBUG
        public class MeshData
        {
            public Vector3[] Vertices;
            public int[] Triangles;
            public Vector3[] Normals;
            public Vector4[] Tangents;
            public Vector2[] UV;
        }

        // this is how I dumped factory skybox mesh,
        // cubemap is easy to get via UnityExplorer
        public void DumpAtmosphere()
        {
            var weatherFactory = GameObject.Find("Weather_Factory");
            var weatherFactoryTransform = weatherFactory.transform;

            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var meshFilePath = Path.Combine(assemblyDir, "mesh.json");

            for (var i = 0; i < weatherFactoryTransform.childCount; i++)
            {
                var child = weatherFactoryTransform.GetChild(i);
                if (child.name == "Atmosphere")
                {
                    var mesh = child.GetComponent<MeshFilter>().sharedMesh;
                    var meshData = new MeshData()
                    {
                        Vertices = mesh.vertices,
                        Triangles = mesh.triangles,
                        Normals = mesh.normals,
                        Tangents = mesh.tangents,
                        UV = mesh.uv,
                    };
                    WriteJson(meshData, meshFilePath);
                    break;
                }
            }
        }

        public static void WriteJson<T>(T data, string filePath)
        {
            var json = JsonConvert.SerializeObject(data);
            SafeIO.WriteAllTextAsync(filePath, json);
        }
#endif
    }
}
