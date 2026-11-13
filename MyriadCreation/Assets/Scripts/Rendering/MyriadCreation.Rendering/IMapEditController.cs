using System.Collections.Generic;

namespace MyriadCreation.Rendering
{
    /// <summary>
    /// 地图编辑控制器的运行时接口。
    /// 渲染程序集不能引用 Editor 程序集，编辑器侧通过实现本接口把画笔操作转进来；
    /// 游戏运行时该引用为 null，所有相关输入处理自动跳过。
    /// </summary>
    public interface IMapEditController
    {
        bool IsEditMode { get; }
        bool IsPainting { get; }
        bool IsPreviewToolActive { get; }
        void OnPaintStart(int tileIndex);
        void OnPaintDrag(int tileIndex);
        void OnPaintEnd();
        List<int> GetBrushTileIndices(int centerTile);
    }
}
