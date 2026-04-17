// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
using Polytoria.Datamodel;
using Polytoria.Shared;
using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Polytoria.Client.ClientSettingsData;

namespace Polytoria.Client;

public sealed partial class ClientSettings : Node
{
	public event Action<string>? OnSettingChanged;
	private const string ClientSettingsPath = "user://client_settings";
	public static ClientSettings Singleton { get; private set; } = null!;
	public ClientSettingsData Settings { get; private set; }

	public ClientSettings()
	{
		Singleton = this;
		Settings = new();
		if (FileAccess.FileExists(ClientSettingsPath))
		{
			PT.Print("Loading client settings...");
			try
			{
				Settings = JsonSerializer.Deserialize(FileAccess.GetFileAsString(ClientSettingsPath), ClientSettingsGenerationContext.Default.ClientSettingsData)!;
				PT.Print("Client settings loaded!");
			}
			catch (Exception ex)
			{
				PT.PrintErr(ex);
				Settings = new();
			}
		}
	}

	public override void _Ready()
	{
		// Prevent settings on Creator
		if (Globals.CurrentAppEntry != Globals.AppEntryEnum.Client)
		{
			return;
		}
		OnSettingChanged += SelfOnSettingsChanged;
		UpdateAllSettings();
		Globals.BeforeQuit += SaveSettings;
		RenderingDeviceSwitcher.Switch(Settings.RenderingMethod);
		base._Ready();
	}

	private void SelfOnSettingsChanged(string propName)
	{
		// Prevent settings on Creator
		if (Globals.CurrentAppEntry != Globals.AppEntryEnum.Client)
		{
			return;
		}
		GetViewport().DebugDraw = Settings.DevRenderWireframe ? Viewport.DebugDrawEnum.Wireframe : Viewport.DebugDrawEnum.Disabled;
		if (propName == "SoundVolume")
		{
			UpdateSoundVolume();
		}
		else if (propName == "UIScale")
		{
			UpdateUIScale();
		}
		else if (propName == "UseVSync")
		{
			UpdateVSync();
		}
		else if (propName == "UseFullscreen")
		{
			UpdateFullscreen();
		}
	}

	private void UpdateAllSettings()
	{
		// Prevent settings on Creator
		if (Globals.CurrentAppEntry != Globals.AppEntryEnum.Client)
		{
			return;
		}
		UpdateSoundVolume();
		UpdateUIScale();
		UpdateVSync();
		if (!Globals.IsInGDEditor)
		{
			UpdateFullscreen();
		}
	}

	private void UpdateSoundVolume()
	{
		int masterBus = AudioServer.GetBusIndex("Master");
		AudioServer.SetBusVolumeLinear(masterBus, (float)(Settings.SoundVolume / 100));
	}

	private void UpdateVSync()
	{
		DisplayServer.WindowSetVsyncMode(Settings.UseVSync ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
		OS.LowProcessorUsageMode = Settings.UseVSync;
	}

	private void UpdateUIScale()
	{
		float finalScale;

		if (Globals.IsMobileBuild)
		{
			// Mobile, Use mobile-specific scale
			finalScale = Settings.UIScale * Globals.MobileScale;
		}
		else
		{
			// Desktop, Use OS display scale factor
			int screenId = DisplayServer.WindowGetCurrentScreen();
			float osScale = DisplayServer.ScreenGetScale(screenId);
			finalScale = Settings.UIScale * osScale;
		}

		PT.Print($"Applying UI Scale: {finalScale}");
		GetTree().Root.ContentScaleFactor = finalScale;
	}

	private void UpdateFullscreen()
	{
		if (Globals.IsMobileBuild)
		{
			return;
		}
		DisplayServer.WindowSetMode(Settings.UseFullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Maximized);
	}

	public void SaveSettings()
	{
		// Prevent saving on creator
		if (Globals.CurrentAppEntry != Globals.AppEntryEnum.Client)
		{
			return;
		}
		// Prevent saving on server
		if (World.Current != null && World.Current.Network != null)
		{
			if (World.Current.Network.IsServer) return;
		}
		using FileAccess settingsFile = FileAccess.Open(ClientSettingsPath, FileAccess.ModeFlags.Write);
		settingsFile.StoreString(JsonSerializer.Serialize(Settings, ClientSettingsGenerationContext.Default.ClientSettingsData));
		settingsFile.Close();
	}

	public void SetSetting(string propertyName, object value)
	{
		Settings.SetValue(propertyName, value);
		OnSettingChanged?.Invoke(propertyName);
	}

	public object? GetSetting(string propertyName)
	{
		return Settings.GetValue(propertyName);
	}

	public T? GetSetting<T>(string propertyName)
	{
		return (T?)GetSetting(propertyName);
	}
}

public sealed class ClientSettingsData
{
	public bool UseCtrlLock { get; set; } = true;
	public double SoundVolume { get; set; } = 100;
	public double CameraSensitivity { get; set; } = 0.6;
	public bool PhotoMode { get; set; } = false;
	public bool PostProcessing { get; set; } = false;
	public float UIScale { get; set; } = 1;
	public bool UseFullscreen { get; set; } = false;
	public bool UseVSync { get; set; } = true;
	public RenderingDeviceSwitcher.RenderingDeviceEnum RenderingMethod { get; set; } = RenderingDeviceSwitcher.RenderingDeviceEnum.Forward;
	public bool ShowConnectionIndicators { get; set; } = true;
	public PerformanceOverlayModeEnum PerformanceOverlayMode { get; set; } = PerformanceOverlayModeEnum.None;

	[JsonIgnore]
	public bool DevRenderWireframe { get; set; } = false;

	public void SetValue(string propertyName, object value)
	{
		PropertyInfo? prop = GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
		if (prop == null || !prop.CanWrite)
			throw new ArgumentException($"Property '{propertyName}' not found or not writable.");

		prop.SetValue(this, value);
	}

	public object? GetValue(string name)
	{
		PropertyInfo? prop = GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
		if (prop != null)
		{
			return prop.GetValue(this);
		}

		throw new ArgumentException($"No settings named '{name}' found.");
	}

	public enum PerformanceOverlayModeEnum
	{
		None,
		Minimal,
		Full
	}
}

[JsonSerializable(typeof(ClientSettingsData))]
[JsonSerializable(typeof(PerformanceOverlayModeEnum))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(float))]
[JsonSerializable(typeof(bool))]
public partial class ClientSettingsGenerationContext : JsonSerializerContext { }
