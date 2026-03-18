// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;

namespace Polytoria.Misc;

public partial class BetaDumpCurrentState : Button
{
	public override void _Pressed()
	{
		Node rootClient = GetNode("/root/Client/");
		Recurse(rootClient, rootClient);
		PackedScene packed = new();
		packed.Pack(rootClient);

		ResourceSaver.Save(packed, "user://logs/dump.tscn", ResourceSaver.SaverFlags.None);
		OS.ShellShowInFileManager(ProjectSettings.GlobalizePath("user://logs/dump.tscn"), false);
		base._Pressed();
	}

	private static void Recurse(Node n, Node root)
	{
		n.Owner = root;
		foreach (Node cn in n.GetChildren())
		{
			Recurse(cn, root);
		}
	}
}
