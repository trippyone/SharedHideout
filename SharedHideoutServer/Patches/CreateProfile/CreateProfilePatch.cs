using SharedHideoutServer;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;
using System.Reflection;

public class CreateProfilePatch : AbstractPatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(CreateProfileService).GetMethod(nameof(CreateProfileService.CreateProfile));
    }

    [PatchPostfix]
    public static async ValueTask<string> Postfix(ValueTask<string> __result, MongoId sessionId, ProfileCreateRequestData request)
    {
        var result = await __result;

        if (ServiceLocator.ServiceProvider.GetService<ICloner>() is ICloner cloner
            && ServiceLocator.ServiceProvider.GetService<SaveServer>() is SaveServer saveServer
            && ServiceLocator.ServiceProvider.GetService<ILogger<CreateProfileService>>() is ILogger<CreateProfileService> logger)
        {
            var profiles = saveServer.GetProfiles();

            if (profiles.Count == 0)
                return result;

            var referenceProfile = profiles.OrderBy(x => x.Key.ToString()).First().Value;

            LogHelper.Logger.Info($"Picked {referenceProfile.ProfileInfo.Username} as reference profile");

            if (referenceProfile.ProfileInfo.ProfileId == sessionId)
                return result;

            var clonedHideout = cloner.Clone(referenceProfile.CharacterData.PmcData.Hideout);

            var createdProfile = saveServer.GetProfile(sessionId);

            createdProfile.CharacterData.PmcData.Hideout = clonedHideout;

            LogHelper.Logger.Info($"Applied {referenceProfile.ProfileInfo.Username}'s hideout to: {createdProfile.ProfileInfo.Username}");
        }

        return result;
    }
}