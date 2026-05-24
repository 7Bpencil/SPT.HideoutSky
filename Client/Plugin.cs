//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Newtonsoft.Json;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

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

        public static ConfigEntry<float> SunlightColorH;
        public static ConfigEntry<float> SunlightColorS;
        public static ConfigEntry<float> SunlightColorV;
        public static ConfigEntry<float> SunElevationAngle;
        public static ConfigEntry<float> SunAzimuthAngle;
        public static ConfigEntry<float> SunIntensity;
        public static ConfigEntry<LightShadows> SunShadowType;
        public static ConfigEntry<float> SunShadowStrength;

        public static ConfigEntry<float> SkyboxTintH;
        public static ConfigEntry<float> SkyboxTintS;
        public static ConfigEntry<float> SkyboxTintV;
        public static ConfigEntry<float> SkyboxExposure;
        public static ConfigEntry<float> SkyboxRotation;

        public Option<SkyData> SkyData;

        private void Awake()
        {
            Instance = this;
			LoggerInstance = Logger;

            SunlightColorH = Config.Bind<float>("Sunlight", "Color Hue", 0.08169935f, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            SunlightColorS = Config.Bind<float>("Sunlight", "Color Saturation", 0.12f, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            SunlightColorV = Config.Bind<float>("Sunlight", "Color Value", 1, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            SunElevationAngle = Config.Bind<float>("Sunlight", "Elevation Angle", 27, new ConfigDescription("", new AcceptableValueRange<float>(0, 90)));
            SunAzimuthAngle = Config.Bind<float>("Sunlight", "Azimuth Angle", 148, new ConfigDescription("", new AcceptableValueRange<float>(0, 360)));
            SunIntensity = Config.Bind<float>("Sunlight", "Intensity", 0.6f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 8f)));
            SunShadowType = Config.Bind<LightShadows>("Sunlight", "Shadow Type", LightShadows.Soft);
            SunShadowStrength = Config.Bind<float>("Sunlight", "Shadow Strength", 0.7f, new ConfigDescription("", new AcceptableValueRange<float>(0f, 1f)));

            SunlightColorH.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunlightColor(skyData.Sunlight); } };
            SunlightColorS.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunlightColor(skyData.Sunlight); } };
            SunlightColorV.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunlightColor(skyData.Sunlight); } };
            SunElevationAngle.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunAngle(skyData.SunlightTransform); } };
            SunAzimuthAngle.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunAngle(skyData.SunlightTransform); } };
            SunIntensity.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunIntensity(skyData.Sunlight); } };
            SunShadowType.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunShadowType(skyData.Sunlight); } };
            SunShadowStrength.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSunShadowStrength(skyData.Sunlight); } };

            SkyboxTintH = Config.Bind<float>("Skybox", "Tint Hue", 0, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            SkyboxTintS = Config.Bind<float>("Skybox", "Tint Saturation", 0, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            SkyboxTintV = Config.Bind<float>("Skybox", "Tint Value", 1, new ConfigDescription("", new AcceptableValueRange<float>(0, 1)));
            SkyboxExposure = Config.Bind<float>("Skybox", "Exposure", 0.375f, new ConfigDescription("", new AcceptableValueRange<float>(0, 8)));
            SkyboxRotation = Config.Bind<float>("Skybox", "Rotation", 190, new ConfigDescription("", new AcceptableValueRange<float>(0, 360)));

            SkyboxTintH.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSkyboxTint(skyData.SkyboxMaterial); } };
            SkyboxTintS.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSkyboxTint(skyData.SkyboxMaterial); } };
            SkyboxTintV.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSkyboxTint(skyData.SkyboxMaterial); } };
            SkyboxExposure.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSkyboxExposure(skyData.SkyboxMaterial); } };
            SkyboxRotation.SettingChanged += (_, _) => { if (SkyData.Some(out var skyData)) { SetSkyboxRotation(skyData.SkyboxMaterial); } };

            new Patch_HideoutController_HideoutAwake().Enable();
            new Patch_HideoutController_OnDestroy().Enable();
        }

        public void SetSunlightColor(Light sunlight)
        {
            sunlight.color = Color.HSVToRGB(SunlightColorH.Value, SunlightColorS.Value, SunlightColorV.Value);
        }

        public void SetSunAngle(Transform sunTransform)
        {
            sunTransform.eulerAngles = new(SunElevationAngle.Value, SunAzimuthAngle.Value, 0);
        }

        public void SetSunIntensity(Light sunlight)
        {
            sunlight.intensity = SunIntensity.Value;
        }

        public void SetSunShadowType(Light sunlight)
        {
            sunlight.shadows = SunShadowType.Value;
        }

        public void SetSunShadowStrength(Light sunlight)
        {
            sunlight.shadowStrength = SunShadowStrength.Value;
        }

        public void SetSkyboxTint(Material skyboxMaterial)
        {
            skyboxMaterial.SetColor(_Tint, Color.HSVToRGB(SkyboxTintH.Value, SkyboxTintS.Value, SkyboxTintV.Value));
        }

        public void SetSkyboxExposure(Material skyboxMaterial)
        {
            skyboxMaterial.SetFloat(_Exposure, SkyboxExposure.Value);
        }

        public void SetSkyboxRotation(Material skyboxMaterial)
        {
            skyboxMaterial.SetFloat(_Rotation, SkyboxRotation.Value);
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
                SetSkyboxTint(material);
                SetSkyboxExposure(material);
                SetSkyboxRotation(material);
                material.SetTexture(_Tex, cubemap);
                meshRenderer.material = material;
            }

            var sunlight = new GameObject("Sunlight");
            var light = sunlight.AddComponent<Light>();
            var lightTransform = light.transform;
            {
                light.type = LightType.Directional;
                SetSunlightColor(light);
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
            File.WriteAllTextAsync(filePath, json);
        }
#endif
    }
}
