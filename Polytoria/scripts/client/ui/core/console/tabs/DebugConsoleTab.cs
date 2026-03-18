// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Scripting;
using System.Collections.Generic;
using System.Text;
using static Polytoria.Scripting.LogDispatcher;

namespace Polytoria.Client.UI;

public partial class DebugConsoleTab : Control
{
	private const int MaxLogLength = 16384;
	public const string ErrorColorHex = "#F95D5D";
	public const string WarningColorHex = "#FFBC58";
	public const string ServerColorHex = "#0097FF";
	public const string ClientColorHex = "#F95D5D";

	private readonly StringBuilder _textBuilder = new(MaxLogLength * 100);
	private bool _needsRebuild = false;

	public List<LogData> Logs = [];
	public HashSet<string> ShownLogs = [];

	[Export] public RichTextLabel TextLabel = null!;
	public LogDispatcher Logger = null!;

	public override void _Ready()
	{
		Logger = CoreUIRoot.Singleton.Root.ScriptService.Logger;
		TextLabel.Text = "";
		Logger.NewLog += OnNewLog;
		Logger.LogSynchronized += OnLogSynchronized;
	}

	public override void _Process(double delta)
	{
		if (_needsRebuild && IsVisibleInTree())
		{
			_needsRebuild = false;
			UpdateText();
		}
		base._Process(delta);
	}

	private void OnLogSynchronized(LogData[] logs)
	{
		foreach (LogData item in logs)
		{
			OnNewLog(item);
		}
	}

	private void OnNewLog(LogData data)
	{
		if (ShownLogs.Contains(data.ID)) return;

		ShownLogs.Add(data.ID);

		// Binary search insertion to maintain sorted order
		int index = Logs.BinarySearch(data, Comparer<LogData>.Create((a, b) => a.LoggedAt.CompareTo(b.LoggedAt)));
		if (index < 0) index = ~index;
		Logs.Insert(index, data);

		// Trim old logs if exceeding limit
		if (Logs.Count > MaxLogLength)
		{
			int removeCount = Logs.Count - MaxLogLength;
			for (int i = 0; i < removeCount; i++)
			{
				ShownLogs.Remove(Logs[0].ID);
				Logs.RemoveAt(0);
			}
		}

		_needsRebuild = true;
	}

	private void UpdateText()
	{
		_textBuilder.Clear();

		foreach (LogData item in Logs)
		{
			string dotColor = (item.LogFrom == LogFromEnum.Client) ? ClientColorHex : ServerColorHex;

			_textBuilder.Append("[color=")
				.Append(dotColor)
				.Append("]•[/color] ");

			if (item.LogType == LogTypeEnum.Error)
			{
				_textBuilder.Append("[color=")
					.Append(ErrorColorHex)
					.Append(']');
			}
			else if (item.LogType == LogTypeEnum.Warning)
			{
				_textBuilder.Append("[color=")
					.Append(WarningColorHex)
					.Append(']');
			}


			_textBuilder.Append('[')
				.Append(item.LoggedAt.ToLongTimeString())
				.Append("] ")
				.Append(item.Content);

			if (item.LogType == LogTypeEnum.Error || item.LogType == LogTypeEnum.Warning)
			{
				_textBuilder.Append("[/color]");
			}

			_textBuilder.Append('\n');
		}

		TextLabel.Text = _textBuilder.ToString();
		_needsRebuild = false;
	}
}
