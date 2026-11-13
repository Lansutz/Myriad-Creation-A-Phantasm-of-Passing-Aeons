using MyriadCreation.Save.Contracts;

namespace MyriadCreation.Infrastructure.Save
{
/// <summary>`ISaveSlotService` 的 Infrastructure 实现：委托既有 `SaveSystem` 静态入口。</summary>
public sealed class SaveSlotService : ISaveSlotService
{
	public string GetSaveDirectory()
	{
		return SaveSystem.GetSaveDirectory();
	}

	public string[] ListSaves()
	{
		return SaveSystem.ListSaves();
	}

	public bool DeleteSave(string saveName)
	{
		return SaveSystem.DeleteSave(saveName);
	}
}
}
