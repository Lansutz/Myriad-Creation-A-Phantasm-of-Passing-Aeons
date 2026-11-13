namespace MyriadCreation.Core.Enums
{
/// <summary>
/// Bootstrap/UI 的地图尺寸档（Large/Huge/Reference/Enormous），直接决定启动时的地图宽高。
/// 生成配置内部使用的是另一种尺寸预设 Generator 侧 GenerationMapSize（Medium/Large/Huge），两者不是同一概念。
/// </summary>
public enum MapSizePreset
{
	Large,
	Huge,
	Reference,
	Enormous
}
}
