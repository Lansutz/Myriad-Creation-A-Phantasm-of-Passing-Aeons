namespace MyriadCreation.Save.Contracts
{
/// <summary>
/// 存档槽只读/删除契约（P2.5 B）：UI 只依赖此接口列出/删除存档，
/// 具体文件布局与读写在 Infrastructure 实现，UI 不再引用 Infrastructure。
/// </summary>
public interface ISaveSlotService
{
	string GetSaveDirectory();

	string[] ListSaves();

	bool DeleteSave(string saveName);
}
}
