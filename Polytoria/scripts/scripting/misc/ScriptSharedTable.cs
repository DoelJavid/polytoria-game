// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Polytoria.Attributes;
using System.Collections.Generic;

namespace Polytoria.Scripting;

public partial class ScriptSharedTable : IScriptObject
{
	public Dictionary<string, object> SharedDict = [];

	[ScriptMethod]
	public void Clear()
	{
		SharedDict.Clear();
	}

	[ScriptMethod]
	public void Remove(string key)
	{
		SharedDict.Remove(key);
	}

	[ScriptMethod]
	public void ClearPrefix(string prefix)
	{
		foreach ((string key, _) in SharedDict)
		{
			if (key.StartsWith(prefix))
			{
				SharedDict.Remove(key);
			}
		}
	}

	[ScriptMethod]
	public void ClearSuffix(string suffix)
	{
		foreach ((string key, _) in SharedDict)
		{
			if (key.EndsWith(suffix))
			{
				SharedDict.Remove(key);
			}
		}
	}

	[ScriptMetamethod(ScriptObjectMetamethod.Index)]
	public object? Index(string index)
	{
		if (SharedDict.TryGetValue(index, out object? value))
		{
			return value;
		}
		return null;
	}

	[ScriptMetamethod(ScriptObjectMetamethod.NewIndex)]
	public void NewIndex(string index, object val)
	{
		SharedDict[index] = val;
		if (val == null)
		{
			SharedDict.Remove(index);
		}
	}
}
