// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;

namespace Polytoria.Creator.UI.Components;

public partial class CodeEditorButton : Button
{
	[Export] public PreferredEditorEnum Editor;

	public override void _Pressed()
	{
		if (!IsVisibleInTree()) return;
		CreatorSettings.Singleton.SetSetting("CodeEditor.PreferredEditor", Editor);
		base._Pressed();
	}
}
