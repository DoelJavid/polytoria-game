// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Polytoria.Scripting.LogDispatcher;

namespace Polytoria.Creator.UI;

public partial class DebugConsole : Control
{
	public const string ErrorColorHex = "#F95D5D";
	public const string WarningColorHex = "#FFBC58";
	public const string ServerColorHex = "#0097FF";
	public const string ClientColorHex = "#F95D5D";
	public const string AddonColorHex = "#4FE883";
	public const string NoneColorHex = "#575757";

	private const int MaxLogLength = 16384;

	private readonly StringBuilder _textBuilder = new(MaxLogLength * 100);

	[Export] private RichTextLabel _richLabel = null!;
	[Export] private LineEdit _searchEdit = null!;
	[Export] private Button _clearBtn = null!;
	public static DebugConsole Singleton { get; private set; } = null!;
	private bool _needsRebuild = false;

	public List<LogData> Logs = [];
	public HashSet<LogData> ShownLogs = [];
	public string SerachQuery = "";

	public DebugConsole()
	{
		Singleton = this;
	}

	public override void _Ready()
	{
		VisibilityChanged += OnVisibilityChanged;
		_clearBtn.Pressed += Clear;
		_searchEdit.TextChanged += (_) => OnSearch();
		_richLabel.Text = "";
	}

	public override void _Process(double delta)
	{
		if (_needsRebuild && IsVisibleInTree())
		{
			UpdateText();
		}
		base._Process(delta);
	}

	private void OnSearch()
	{
		SerachQuery = _searchEdit.Text;
		QueueRebuild();
	}

	public void Clear()
	{
		Logs.Clear();
		ShownLogs.Clear();
		QueueRebuild();
	}

	public void NewLog(LogData data)
	{
		data.LoggedAt = DateTime.Now;
		if (ShownLogs.Contains(data)) return;

		ShownLogs.Add(data);

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
				ShownLogs.Remove(Logs[0]);
				Logs.RemoveAt(0);
			}
		}

		QueueRebuild();
	}

	private void QueueRebuild()
	{
		_needsRebuild = true;
	}

	private void OnVisibilityChanged()
	{
		if (IsVisibleInTree() && _needsRebuild)
		{
			UpdateText();
		}
	}

	private void UpdateText()
	{
		if (!_needsRebuild) return;

		IEnumerable<LogData> logsToShow = Logs;

		if (!string.IsNullOrEmpty(SerachQuery))
		{
			logsToShow = Logs.Where(l => l.Content.Find(SerachQuery, caseSensitive: false) != -1);
		}

		_textBuilder.Clear();

		foreach (LogData item in logsToShow)
		{
			string dotColor = item.LogFrom switch
			{
				LogFromEnum.None => NoneColorHex,
				LogFromEnum.Client => ClientColorHex,
				LogFromEnum.Server => ServerColorHex,
				LogFromEnum.Addon => AddonColorHex,
				_ => NoneColorHex
			};

			_textBuilder.Append("[color=")
				.Append(dotColor)
				.Append("]•[/color] ");

			if (item.LogType == LogTypeEnum.Warning)
			{
				_textBuilder.Append("[color=")
					.Append(WarningColorHex)
					.Append(']');
			}
			else if (item.LogType == LogTypeEnum.Error)
			{
				_textBuilder.Append("[color=")
					.Append(ErrorColorHex)
					.Append(']');
			}

			_textBuilder.Append('[')
				.Append(item.LoggedAt.ToLongTimeString())
				.Append("] ")
				.Append(item.Content);

			if (item.LogType != LogTypeEnum.Info)
			{
				_textBuilder.Append("[/color]");
			}

			_textBuilder.Append('\n');
		}

		_richLabel.Text = _textBuilder.ToString();
		_needsRebuild = false;
	}
}
