namespace CivilizationEvolution.Render
{
    public enum EditorTool
    {
        None,           // 无（观察模式）
        TerrainLand,    // 地形画笔：造陆
        TerrainSea,     // 地形画笔：填海
        TerrainMountain,// 地形画笔：山地
        TerrainPlain,   // 地形画笔：平原
        ProvincePaint,  // 省份画笔：绘制省份归属
        ProvinceErase,  // 省份橡皮擦：清除省份归属
        BurgPlace,      // 子地块放置：在指定位置放置Burg
        BurgRemove      // 子地块移除
    }
}
