using DVMultiplayer.Networking;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.UI;

namespace DVMultiplayer.Patches
{
    [HarmonyPatch(typeof(CanvasSpawner), "Update")]
    class UpdateImgControllersCanvasSpawner
    {
        static void Prefix(ref Image[] ___imageComponents)
        {
            if(Main.isInitialized && CustomUI.readyForCSUpdate && !CustomUI.CSUpdateFinished)
            {
                Main.DebugLog("Updating image components");
                ___imageComponents = (from img in SingletonBehaviour<CanvasSpawner>.Instance.CanvasGO.GetComponentsInChildren<Image>(true)
                                   where img.transform.name != "VRTK_UICANVAS_DRAGGABLE_PANEL"
                                   select img).ToArray<Image>();

                CustomUI.CSUpdateFinished = true;
            }
        }
    }
}
