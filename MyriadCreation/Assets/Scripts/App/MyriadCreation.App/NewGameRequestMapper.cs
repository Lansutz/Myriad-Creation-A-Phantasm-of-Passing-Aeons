using MyriadCreation.Core.Contracts;
using MyriadCreation.World.Generation;

namespace MyriadCreation.App
{
    /// <summary>
    /// App 内部 DTO↔WorldSetupOptions 转换：契约层只依赖 Core.NewGameRequestDto；
    /// 默认/预设语义与旧 WorldSetupOptions.CreateDefault/Create(mode) 保持一致。
    /// </summary>
    internal static class NewGameRequestMapper
    {
        public static WorldSetupOptions ToWorldSetup(NewGameRequestDto request)
        {
            if (request == null)
            {
                return WorldSetupOptions.CreateDefault();
            }

            // DefaultContentModeSentinel（-1，兼容任何负值）：未指定自定义内容，沿用默认世界选项。
            if (request.contentMode < 0)
            {
                return WorldSetupOptions.CreateDefault();
            }

            var options = new WorldSetupOptions
            {
                initialContent = (InitialContentMode)request.contentMode,
                generatePolities = request.generatePolities,
                generateSettlements = request.generateSettlements,
                generateInitialUnits = request.generateInitialUnits,
                initialPolityCount = request.initialPolityCount,
                initialPopulationScale = request.initialPopulationScale,
                initialSettlementCount = request.initialSettlementCount,
                initialUnitCount = request.initialUnitCount,
                spawnRates = new SandboxSpawnRates
                {
                    enableNaturalSpawning = request.enableNaturalSpawning,
                    mapActorMultiplier = request.mapActorMultiplier,
                    spawnSettlements = request.spawnSettlements,
                    spreadPopulation = request.spreadPopulation,
                },
            };

            if (options.initialContent == InitialContentMode.Custom)
            {
                options.Normalize();
            }
            else
            {
                // Create(mode) 的预设语义：由 WorldSetupOptions 自己按预设重填生成参数。
                options.ApplyPreset();
            }
            return options;
        }

        public static NewGameRequestDto FromWorldSetup(WorldSetupOptions options)
        {
            if (options == null)
            {
                return null;
            }

            return new NewGameRequestDto
            {
                contentMode = (int)options.initialContent,
                generatePolities = options.generatePolities,
                generateSettlements = options.generateSettlements,
                generateInitialUnits = options.generateInitialUnits,
                initialPolityCount = options.initialPolityCount,
                initialPopulationScale = options.initialPopulationScale,
                initialSettlementCount = options.initialSettlementCount,
                initialUnitCount = options.initialUnitCount,
                enableNaturalSpawning = options.spawnRates != null && options.spawnRates.enableNaturalSpawning,
                mapActorMultiplier = options.spawnRates != null ? options.spawnRates.mapActorMultiplier : 1f,
                spawnSettlements = options.spawnRates == null || options.spawnRates.spawnSettlements,
                spreadPopulation = options.spawnRates == null || options.spawnRates.spreadPopulation,
            };
        }
    }
}
