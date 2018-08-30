﻿/*
© Siemens AG, 2018
Author: Suzannah Smith (suzannah.smith@siemens.com)

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

<http://www.apache.org/licenses/LICENSE-2.0>.

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
*/
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RosSharp.Urdf
{
    public static class UrdfMaterialHandler
    {
        private const string defaultMaterialName = "Default";
        private const string materialFolderName = "Materials";

        public static void InitializeRobotMaterials(Robot robot)
        {
            if (!AssetDatabase.IsValidFolder(Path.Combine(UrdfAssetPathHandler.GetPackageRoot(), materialFolderName)))
                AssetDatabase.CreateFolder(UrdfAssetPathHandler.GetPackageRoot(), materialFolderName);

            CreateDefaultMaterialAsset();
            foreach (var material in robot.materials)
                CreateMaterialAsset(material);
        }

        public static void MoveMaterialsToNewLocation(string oldPackageRoot) 
        {
            //TODO: fix issue where moving materials causes texture references to be lost
            if (AssetDatabase.IsValidFolder(Path.Combine(oldPackageRoot, materialFolderName)))
                AssetDatabase.MoveAsset(
                    Path.Combine(oldPackageRoot, materialFolderName),
                    Path.Combine(UrdfAssetPathHandler.GetPackageRoot(), materialFolderName));
            else
                AssetDatabase.CreateFolder(UrdfAssetPathHandler.GetPackageRoot(), materialFolderName);
        }

        private static string GetMaterialAssetPath(string materialName)
        {
            var path = Path.Combine(materialFolderName, Path.GetFileName(materialName) + ".mat");
            return Path.Combine(UrdfAssetPathHandler.GetPackageRoot(), path);
        }

        #region CreateMaterialAssets
        private static Material CreateMaterialAsset(this Link.Visual.Material urdfMaterial)
        {
            if (urdfMaterial.name == "")
                urdfMaterial.name = GenerateMaterialName(urdfMaterial);

            var material = AssetDatabase.LoadAssetAtPath<Material>(GetMaterialAssetPath(urdfMaterial.name));
            if (material != null) //material already exists
                return material;
            
            material = InitializeMaterial();

            if (urdfMaterial.color != null)
                material.color = CreateColor(urdfMaterial.color);
            else if (urdfMaterial.texture != null)
                material.mainTexture = LoadTexture(urdfMaterial.texture.filename);

            AssetDatabase.CreateAsset(material, GetMaterialAssetPath(urdfMaterial.name));
            return material;
        }

        private static void CreateDefaultMaterialAsset()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(GetMaterialAssetPath(defaultMaterialName));
            if (material != null)
                return;

            material = InitializeMaterial();
            material.color = new Color(0.33f, 0.33f, 0.33f, 0.0f);

            AssetDatabase.CreateAsset(material, GetMaterialAssetPath(defaultMaterialName));
        }
        
        private static Material InitializeMaterial()
        {
            var material = new Material(Shader.Find("Standard"));
            material.SetFloat("_Metallic", 0.75f);
            material.SetFloat("_Glossiness", 0.75f);
            return material;
        }

        private static string GenerateMaterialName(Link.Visual.Material urdfMaterial)
        {
            var materialName = "";
            if (urdfMaterial.color != null)
            {
                materialName = "rgba-";
                for (var i = 0; i < urdfMaterial.color.rgba.Length; i++)
                {
                    materialName += urdfMaterial.color.rgba[i];
                    if (i != urdfMaterial.color.rgba.Length - 1)
                        materialName += "-";
                }
            }
            else if (urdfMaterial.texture != null)
            {
                materialName = "texture-" + Path.GetFileName(urdfMaterial.texture.filename);
            }
            return materialName;
        }

        private static Color CreateColor(Link.Visual.Material.Color urdfColor)
        {
            return new Color(
                (float)urdfColor.rgba[0],
                (float)urdfColor.rgba[1],
                (float)urdfColor.rgba[2],
                (float)urdfColor.rgba[3]);
        }

        private static Texture LoadTexture(string filename)
        {
            return filename == "" ? null : LocateAssetHandler.FindUrdfAsset<Texture>(filename);
        }
        #endregion CreateMaterialAssets

        #region SetMaterial
        public static void SetUrdfMaterial(GameObject gameObject, Link.Visual.Material urdfMaterial)
        {
            if (urdfMaterial != null)
            { 
                var material = CreateMaterialAsset(urdfMaterial);
                SetMaterial(gameObject, material);
            }
            else
            {
                //If the URDF material is not defined, and the renderer is missing
                //a material, assign the default material.
                Renderer renderer = gameObject.GetComponentInChildren<Renderer>();
                if (renderer != null && renderer.sharedMaterial == null)
                    SetDefaultMaterial(gameObject);
            }
        }

        private static void SetDefaultMaterial(GameObject gameObject)
        {
            var defaultMaterial = AssetDatabase.LoadAssetAtPath<Material>(GetMaterialAssetPath(defaultMaterialName));
            SetMaterial(gameObject, defaultMaterial);
        }

        private static void SetMaterial(GameObject gameObject, Material material)
        {
            var renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
                renderer.sharedMaterial = material;
        }
        #endregion
    }
}
