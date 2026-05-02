namespace Diguifi.Application.Common;

public sealed record ErrorContract(string Code, string Message, IReadOnlyCollection<string>? Details = null);
