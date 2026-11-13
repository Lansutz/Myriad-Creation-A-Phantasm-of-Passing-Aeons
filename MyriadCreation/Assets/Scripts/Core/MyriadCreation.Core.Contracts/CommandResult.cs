namespace MyriadCreation.Core.Contracts
{
public readonly struct CommandResult(bool success, string code = null, string message = null)
{
	public readonly bool Success = success;

	public readonly string Code = code ?? string.Empty;

	public readonly string Message = message ?? string.Empty;

	public static CommandResult Succeeded(string code = "ok")
	{
		return new CommandResult(success: true, code);
	}

	public static CommandResult Failed(string code, string message = null)
	{
		return new CommandResult(success: false, code, message);
	}
}
}
