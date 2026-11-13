using System.Collections.Generic;

namespace MyriadCreation.Content.Modding
{
public sealed class ContentPackageValidationResult
{
	private readonly List<string> _errors = new List<string>();

	private readonly List<string> _warnings = new List<string>();

	public IReadOnlyList<string> Errors => _errors;

	public IReadOnlyList<string> Warnings => _warnings;

	public bool IsValid => _errors.Count == 0;

	internal void Error(string message)
	{
		_errors.Add(message);
	}

	internal void Warning(string message)
	{
		_warnings.Add(message);
	}
}
}
