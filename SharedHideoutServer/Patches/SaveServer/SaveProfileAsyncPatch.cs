using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Servers;
using System.Reflection;

public class SaveProfileAsyncPatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(SaveServer).GetMethod(nameof(SaveServer.SaveProfileAsync));
    }

    /*
    [PatchPrefix]
    public static bool Prefix(MongoId sessionID, ref ConcurrentDictionary<MongoId, SptProfile> ___profiles)
    {
        // Don't affect SharedHideout profile
        if (sessionID == SharedHideoutInfo.ProfileId)
        {
            return true;
        }

        var sharedHideout = ___profiles[SharedHideoutInfo.ProfileId].CharacterData.PmcData.Hideout;

        ___profiles[sessionID].CharacterData.PmcData.Hideout = sharedHideout;

        LogHelper.Logger.Info($"Saved SharedHideout to : {___profiles[sessionID].ProfileInfo.Username}");

        return true;
    }
    */
}