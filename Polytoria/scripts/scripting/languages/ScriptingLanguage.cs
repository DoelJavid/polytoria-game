// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using System;
using System.Threading.Tasks;
using Script = Polytoria.Datamodel.Script;

namespace Polytoria.Scripting;

// didn't mark this abstract cuz assembly reload keeps failing
public partial class ScriptLanguageProvider : Node
{
	public virtual void Run(Script script) { throw new NotImplementedException(); }
	public virtual void Close(Script script) { throw new NotImplementedException(); }
	public virtual Task CallAsync(Script script, string funcName, object?[]? args) { throw new NotImplementedException(); }
	public virtual void CallUpdate(Script script, double delta) { throw new NotImplementedException(); }
	public virtual void CallFixedUpdate(Script script, double delta) { throw new NotImplementedException(); }
	public virtual void FreePTCallback(PTCallback callback) { throw new NotImplementedException(); }
}

public enum ScriptLanguagesEnum
{
	Luau
}
