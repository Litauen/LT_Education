using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Localization;
//using LT.Logger;

namespace LT_Education
{

    // Setting minimum learning rate to 0.05 to always be able to learn (but very slowly)
    [HarmonyPatch(typeof(DefaultCharacterDevelopmentModel))]
    [HarmonyPatch("CalculateLearningRate", typeof(int), typeof(int), typeof(int), typeof(int), typeof(TextObject), typeof(bool))]
    public class LearningRatePatch
    {
        static void Postfix(ref ExplainedNumber __result)
        {
            __result.LimitMin(0.05f);
            //LTLogger.IMGreen("Harmony patch active!");
        }
    }
}
