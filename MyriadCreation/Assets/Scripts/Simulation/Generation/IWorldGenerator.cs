using MyriadCreation.Core;
using MyriadCreation.Core.Constants;
using MyriadCreation.Core.Data;
using MyriadCreation.Core.Dto;
using MyriadCreation.Core.Enums;
using MyriadCreation.Simulation.Economy;
using MyriadCreation.Simulation.Events;
using MyriadCreation.Simulation.Modding;
using MyriadCreation.Simulation.Politics;
using MyriadCreation.Simulation.Society;
using MyriadCreation.Simulation.WorldState;



namespace MyriadCreation.Simulation.Generation
{
    /// <summary>
    /// 世界生成器统一接口（参考 Azgaar FMG 的模块化生成器架构）。
    /// 每个生成器负责一个独立的生成阶段，有明确的输入输出，支持增量重算。
    /// 数据层（TileData/Province/BurgData）纯数据，生成逻辑在实现类中。
    /// </summary>
    public interface IWorldGenerator
    {
        /// <summary>生成器对应的管线阶段</summary>
        GenerationStage Stage { get; }

        /// <summary>执行生成，写入 GameWorld 的对应数据字段</summary>
        void Generate(GameWorld world);
    }
}
