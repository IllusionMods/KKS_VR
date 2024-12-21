

// Collection of patches that are to be enabled only during development.

namespace KK_VR
{
#if false
    /// Report every time TransformDebug.targetTransform is rotated.
    [HarmonyPatch]
    class TransformPatches
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.PropertySetter(typeof(Transform), "rotation");
            yield return AccessTools.PropertySetter(typeof(Transform), "localRotation");
            yield return AccessTools.PropertySetter(typeof(Transform), "eulerAngles");
            yield return AccessTools.Method(typeof(Transform), "SetPositionAndRotation");
            yield return AccessTools.Method(typeof(Transform), "Rotate", new[] { typeof(Vector3), typeof(Space) });
            yield return AccessTools.Method(typeof(Transform), "Rotate", new[] { typeof(float), typeof(float), typeof(float), typeof(Space) });
            yield return AccessTools.Method(typeof(Transform), "Rotate", new[] { typeof(Vector3), typeof(float), typeof(Space) });
            yield return AccessTools.Method(typeof(Transform), "RotateAround", new[] { typeof(Vector3), typeof(Vector3), typeof(float) });
            yield return AccessTools.Method(typeof(Transform), "LookAt", new[] { typeof(Transform), typeof(Vector3) });
            yield return AccessTools.Method(typeof(Transform), "LookAt", new[] { typeof(Vector3), typeof(Vector3) });
            yield break;
        }

        static void Prefix(Transform __instance)
        {
            if (__instance == TransformDebug.targetTransform)
            {
                VRLog.Info("Someone is modifying the target transform!");
                VRLog.Info(new StackTrace(false));
            }
        }
    }

    // Dump human-readable ADV scenarios under the "scenario_dump/" folder.
    [HarmonyPatch(typeof(AssetBundleManager))]
    class AssetBundleManagerPatches
    {
        [HarmonyPatch("LoadAsset", new[] { typeof(string), typeof(string), typeof(Type), typeof(string) })]
        [HarmonyPostfix]
        static void PostLoadAsset(string assetBundleName, string assetName, Type type, AssetBundleLoadAssetOperation __result)
        {
            if (type == typeof(ADV.ScenarioData))
            {
                try
                {
                    var scenarioData = __result.GetAsset<ADV.ScenarioData>();
                    Directory.CreateDirectory("scenario_dump");
                    StreamWriter writer = new StreamWriter($"scenario_dump/{assetBundleName.Replace('/', '_')}-{assetName.Replace('/', '_')}.txt");
                    foreach (var param in scenarioData.list)
                    {
                        writer.WriteLine($"{(param.Multi ? "M" : " ")} {param.Command} {string.Join(", ", param.Args)}");
                    }
                    writer.Close();
                }
                catch(Exception e)
                {
                    VRLog.Error(e);
                }
            }
        }
    }

    // Log every time a scenario command is executed.
    [HarmonyPatch(typeof(ADV.CommandList))]
    class CommandListPatches
    {
        [HarmonyPatch("Add", new[] { typeof(ADV.ScenarioData.Param), typeof(int) })]
        [HarmonyPrefix]
        static void PreAdd(ADV.ScenarioData.Param item)
        {
            VRLog.Info($"{(item.Multi ? "M" : " ")} {item.Command} {string.Join(", ", item.Args)}");
        }
    }
#endif
}
