namespace MyriadCreation.Core.Constants
{
public static class GameConstants
{
	public const string GameNameEn = "Myriad Creation: A Phantasm of Passing Aeons";

	public const string GameNameZh = "纷繁的世界：一场流逝的幻景";

	public const string GameNameShort = "Myriad Creation";

	public const string Version = "0.1.0-alpha";

	// v18：FaithSaveData 增 religionId / denominationId / lineageFaithId，并保存宗派运行态。
	// religion 与 denomination 对无统一组织的 faith 均可为 -1。
	public const int SaveVersion = 18;

	public static string FullTitle => "Myriad Creation: A Phantasm of Passing Aeons\n纷繁的世界：一场流逝的幻景";
}
}
