//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using SystemObject = System.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SevenBoldPencil.HideoutSky
{
    public class Plugin : MonoBehaviour
    {
#if UNITY_EDITOR
        public class MeshData
        {
            public Vector3[] Vertices;
            public int[] Triangles;
            public Vector3[] Normals;
            public Vector4[] Tangents;
            public Vector2[] UV;
        }

        private void Awake()
        {
            // load dump from the game and turn it into proper asset
            var meshDataJson = Resources.Load<TextAsset>("mesh").text;
            var meshData = JsonConvert.DeserializeObject<MeshData>(meshDataJson);

            var mesh = new Mesh();

            mesh.vertices = meshData.Vertices;
            mesh.uv = meshData.UV;
            mesh.normals = meshData.Normals;
            mesh.tangents = meshData.Tangents;
            mesh.triangles = meshData.Triangles;
            mesh.RecalculateBounds();

            AssetDatabase.CreateAsset(mesh, "Assets/HideoutSky/Meshes/atmosphere.mesh");
            AssetDatabase.SaveAssets();
        }
#endif
    }
}
