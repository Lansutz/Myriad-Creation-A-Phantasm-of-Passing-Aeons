using MyriadCreation.Core.Enums;
using MyriadCreation.Core.Contracts;

namespace MyriadCreation.Rendering.Editing
{
    /// <summary>
    /// 地图编辑命令服务契约（Rendering 只做输入/射线/画笔；执行由 App 的 WorldEditService 实现）。
    /// 所有方法在 World/服务不可用时返回 false 或静默返回，不抛异常。
    /// 注：B7 后 Rendering 不再引用 Simulation，因此本接口不包含 BindWorld(GameWorld)；
    /// 世界绑定由 App 侧实现（WorldEditService 的构造 provider/BindWorld）完成，Rendering 只持接口。
    /// </summary>
    public interface IMapEditCommandService
    {
        /// <summary>抬升单个地块（delta 为增量，结果限制在 [-1,1]）。</summary>
        bool ElevateTile(int tileIndex, float delta);

        /// <summary>降低单个地块（amount 为正数，结果限制在 [-1,1]）。</summary>
        bool LowerTile(int tileIndex, float amount);

        /// <summary>以中心地块为圆心，按半径内已有地块的平均高程做平滑。</summary>
        bool SmoothTile(int tileIndex, int brushRadius);

        /// <summary>设置地块高程（elevation01，限制在 [-1,1]）。</summary>
        bool SetTerrain(int tileIndex, float elevation01);

        /// <summary>设置地块生物群系（不改变高程）。</summary>
        bool SetBiome(int tileIndex, GameEnums.BiomeType biome);

        /// <summary>新增地块（不存在时创建，等价 GameWorld.CreateTile）。</summary>
        bool AddTile(int tileIndex);

        /// <summary>移除地块（等价 GameWorld.RemoveTile）。</summary>
        bool RemoveTile(int tileIndex);

        /// <summary>平移选区内大陆块（保持旧 MapEditHistory 行为）。</summary>
        bool MoveSelection(MapSelection selection, int dx, int dy);

        /// <summary>抬升选区（保持旧 RaiseSelectionCommand 行为）。</summary>
        bool RaiseSelection(MapSelection selection, float delta);

        /// <summary>撤销最近一次选区命令。</summary>
        bool Undo();

        /// <summary>重做最近一次撤销的选区命令。</summary>
        bool Redo();

        /// <summary>标记全部地块 terrain dirty 并触发世界重算（等价旧 MarkDirty）。</summary>
        void Recalculate();
    }
}
