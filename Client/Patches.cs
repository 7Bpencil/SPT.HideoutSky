//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using Comfort.Common;
using Diz.Skinning;
using Diz.Jobs;
using EFT;
using EFT.AssetsManager;
using EFT.InventoryLogic;
using EFT.Hideout;
using EFT.Visual;
using EFT.UI;
using EFT.UI.WeaponModding;
using SevenBoldPencil.Common;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using SPT.Reflection.Patching;
using HarmonyLib;
using UnityEngine;

namespace SevenBoldPencil.HideoutSky
{
	public struct HideoutCustomizationItemsInstaller_Proxy
	{
		private static TypedFieldInfo<HideoutCustomizationItemsInstaller, HideoutCustomizationItemModelSpawnPoint> __ceilingPoint = new("_ceilingPoint");

		public HideoutCustomizationItemModelSpawnPoint _ceilingPoint { get { return __ceilingPoint.Get(__instance); } set { __ceilingPoint.Set(__instance, value); } }

        private HideoutCustomizationItemsInstaller __instance;

        public HideoutCustomizationItemsInstaller_Proxy(HideoutCustomizationItemsInstaller instance)
        {
            __instance = instance;
        }
	}

	public class Patch_HideoutController_HideoutAwake : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), nameof(HideoutController.HideoutAwake));
        }

        [PatchPostfix]
        public static void Postfix(HideoutCustomizationItemsInstaller ___CustomizationItemsInstaller)
		{
			var ceiling = new HideoutCustomizationItemsInstaller_Proxy(___CustomizationItemsInstaller)._ceilingPoint;
			ceiling.gameObject.SetActive(false);
			Plugin.Instance.LoadAtmosphere();
		}
	}

	public class Patch_HideoutController_OnDestroy : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), nameof(HideoutController.OnDestroy));
        }

        [PatchPostfix]
        public static void Postfix()
		{
			Logger.LogError($"Patch_HideoutController_OnDestroy");
			// TODO unload skybox texture
		}
	}
}
