using SharedHideout.Models.Config;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;
using System.Reflection;

namespace SharedHideoutServer.Services;

[Injectable(InjectionType.Singleton)]
public class ConfigService(ISptLogger<ConfigService> logger, ModHelper modHelper, JsonUtil jsonUtil)
{
    public SharedHideoutServerConfig Config { get; private set; } = new();
    private readonly string _modPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

    public void Init()
    {
        var configPath = Path.Join(_modPath, "config.json");

        if (!File.Exists(configPath))
        {
            File.WriteAllText(configPath, jsonUtil.Serialize(Config, true));
        }
        else
        {
            Config = jsonUtil.Deserialize<SharedHideoutServerConfig>(File.ReadAllText(configPath)) ?? new();
        }
    }
}
