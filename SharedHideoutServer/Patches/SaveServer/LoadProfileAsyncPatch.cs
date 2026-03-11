using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Servers;
using System.Reflection;

public class LoadProfileAsyncPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(SaveServer).GetMethod(nameof(SaveServer.LoadProfileAsync));
    }

    /*
    [PatchPostfix]
    public static async Task Postfix(Task __result, MongoId sessionID, SaveServer __instance, ConcurrentDictionary<MongoId, SptProfile> ___profiles)
    {
        // Wait for original task to complete
        await __result;

        // Don't affect SharedHideout profile
        if (sessionID == SharedHideoutInfo.ProfileId)
        {
            return;
        }

        // Load SharedHideout Profile if not already loaded
        if (!___profiles.ContainsKey(SharedHideoutInfo.ProfileId))
        {
            await __instance.LoadProfileAsync(SharedHideoutInfo.ProfileId);
        }

        // Get shared hideout from SharedHideout profile
        var sharedHideout = ___profiles[SharedHideoutInfo.ProfileId].CharacterData.PmcData.Hideout;

        // Apply SharedHideout to player hideout
        ___profiles[sessionID].CharacterData.PmcData.Hideout = sharedHideout;

        LogHelper.Logger.Info($"Loaded SharedHideout to : {___profiles[sessionID].ProfileInfo.Username}");

    }
    */
}