//
// Copyright (c) 2026 7Bpencil
//
// This source code is licensed under the MIT license found in the
// LICENSE file in the root directory of this source tree.
//

using EFT;
using EFT.Interactive;
using EFT.Hideout;
using SevenBoldPencil.Common;
using System;
using System.Collections.Generic;
using System.Reflection;
using SPT.Reflection.Patching;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

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

	public struct LightLevel_Proxy
	{
		private static TypedFieldInfo<LightLevel, List<GInterface467>> __lightSwitchers = new("_lightSwitchers");

		public List<GInterface467> _lightSwitchers { get { return __lightSwitchers.Get(__instance); } set { __lightSwitchers.Set(__instance, value); } }

        private LightLevel __instance;

        public LightLevel_Proxy(LightLevel instance)
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
        public static void Postfix(HideoutController __instance, HideoutCustomizationItemsInstaller ___CustomizationItemsInstaller)
		{
			var ceiling = new HideoutCustomizationItemsInstaller_Proxy(___CustomizationItemsInstaller)._ceilingPoint;
			ceiling.gameObject.SetActive(false);

			foreach (var (areaType, area) in __instance.Areas)
			{
				var areaLevel = area.CurrentLevel;
				if (!areaLevel)
				{
					continue;
				}
				foreach (var lightLevel in areaLevel.LightingLevels.Values)
				{
					if (!lightLevel)
					{
						continue;
					}
					var lightSwitchers = new LightLevel_Proxy(lightLevel)._lightSwitchers;
					foreach (var lightSwitcher in lightSwitchers)
					{
						HideLightSwitcherMesh(lightSwitcher);
					}
				}
				if (areaType == EAreaType.Security)
				{
					var securityRoot = areaLevel.HighlightTransform;
					var childCount = securityRoot.childCount;
					for (var i = 0; i < childCount; i++)
					{
						var child = securityRoot.GetChild(i);
						if (child.name.Contains("metall_patch"))
						{
							DisableAllCeilingMeshRenderers(child);
						}
					}
				}
			}

			var hideoutScene = SceneManager.GetSceneByName(SceneResourceKeyAbstractClass.HideoutSceneName);
			if (hideoutScene.IsValid())
			{
				foreach (var root in hideoutScene.GetRootGameObjects())
				{
					if (root.name == "SceneContent")
					{
						var baseLamps = root.transform.Find("cut_lamp");
						if (baseLamps)
						{
							DisableAllCeilingMeshRenderers(baseLamps);
						}
					}
					if (root.name == "!wires_light")
					{
						root.SetActive(false);
					}
				}
			}

			Plugin.Instance.LoadSkybox();
		}

		public static void HideLightSwitcherMesh(GInterface467 lightSwitcher)
		{
			// lightSwitcher can be: CandleSwitcher, GarlandSwitcher, LampController,
			// but only garland and lamps can be on the ceiling
			if (lightSwitcher is GarlandSwitcher garland)
			{
				DisableAllCeilingMeshRenderers(garland);
			}
			else if (lightSwitcher is LampController lamp)
			{
				DisableAllCeilingMeshRenderers(lamp);
			}
		}

		public static void DisableAllCeilingMeshRenderers(Component component)
		{
			foreach (var meshRenderer in component.GetComponentsInChildren<MeshRenderer>())
			{
				if (meshRenderer.transform.position.y > 2.8f)
				{
					meshRenderer.enabled = false;
				}
			}
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
			Plugin.Instance.UnloadSkybox();
		}
	}

	public class Patch_HideoutScreenOverlay_Show : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
			Type[] parameters = [typeof(HideoutPlayerOwner), typeof(bool), typeof(ISession), typeof(AreaData[]), typeof(HideoutScreenRear)];
            return AccessTools.Method(typeof(HideoutScreenOverlay), nameof(HideoutScreenOverlay.Show));
        }

        [PatchPostfix]
        public static void Postfix()
		{
			Plugin.Instance.OnHideoutScreenShow();
		}
	}

	public class Patch_HideoutScreenOverlay_method_11 : ModulePatch
	{
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutScreenOverlay), nameof(HideoutScreenOverlay.method_11));
        }

        [PatchPostfix]
        public static void Postfix()
		{
			Plugin.Instance.OnHideoutScreenHide();
		}
	}
}
