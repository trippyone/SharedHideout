using SharedHideoutServer.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;

namespace SharedHideoutServer;

// Reference: SPT 4.0.12

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.trippy.sharedhideoutserver";
    public override string Name { get; init; } = "SharedHideoutServer";
    public override string Author { get; init; } = "Trippy";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("0.0.1");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("4.0.13");


    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "MIT";
}

public static class LogHelper
{
    public static ISptLogger<SharedHideoutServer>? Logger;
}

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class SharedHideoutServer(ISptLogger<SharedHideoutServer> logger, ConfigService configService) : IOnLoad
{
    public static SharedHideoutServer Instance { get; private set; }

    public Task OnLoad()
    {
        Instance = this;
        LogHelper.Logger = logger;

        configService.Init();

        // Profile patches
        new CreateProfilePatch().Enable();
        //new LoadProfileAsyncPatch().Enable();

        // Hideout patches
        new StartUpgradePatch().Enable();
        new UpgradeCompletePatch().Enable();
        new ImproveAreaPatch().Enable();
        new SingleProductionStartPatch().Enable();
        new ContinuousProductionStartPatch().Enable();
        new ToggleAreaPatch().Enable();
        new PutItemsInAreaSlotsPatch().Enable();
        new TakeItemsFromAreaSlotsPatch().Enable();
        new TakeProductionPatch().Enable();
        new StartSacrificePatch().Enable();
        new CancelProductionPatch().Enable();
        new ScavCaseProductionStartPatch().Enable();

        logger.Info("SharedHideoutServer started.");

        return Task.CompletedTask;
    }
}