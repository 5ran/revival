using System;
using System.Collections.Generic;
using Client.Services.Fishing;

internal sealed class OffsetTable
{
	private readonly IOffsetsSource source;

	public OffsetTable(IOffsetsSource source)
	{
		this.source = source ?? throw new ArgumentNullException(nameof(source));
	}

	public ulong Get(string namespaceName, string valueName)
	{
		string key = namespaceName + "." + valueName;
		if (!source.TryGetOffset(key, out var value))
			throw new KeyNotFoundException("Missing offset: Offsets::" + namespaceName + "::" + valueName);
		return value;
	}
}
