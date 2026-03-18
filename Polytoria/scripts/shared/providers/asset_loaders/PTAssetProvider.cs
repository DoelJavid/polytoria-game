// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

using Godot;
#if CREATOR
using Polytoria.Creator.Utils;
#endif
using Polytoria.Shared.AssetLoaders;
using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Polytoria.Shared.Providers.AssetLoaders;

public class PTAssetProvider : IAssetProvider
{
	private const string RootUrl = Globals.ApiEndpoint + "v1/assets/";
	private const string ServeURL = RootUrl + "serve/";
	private const string ServeMeshURL = RootUrl + "serve-mesh/";
	private const string ServeAudioURL = RootUrl + "serve-audio/";
	private readonly PTHttpClient _client = new();

	public async Task<CacheItem> LoadResource(CacheItem item)
	{
#if CREATOR
		_client.DefaultRequestHeaders["Authorization"] = PolyCreatorAPI.Token;
#endif

		string url = item.Type switch
		{
			ResourceType.Mesh => ServeMeshURL + item.ID,
			ResourceType.Asset => ServeURL + item.ID + "/asset",
			ResourceType.Decal => ServeURL + item.ID + "/decal",
			ResourceType.Audio => ServeAudioURL + item.ID,
			ResourceType.AssetThumbnail => ServeURL + item.ID + "/assetThumbnail",
			ResourceType.PlaceThumbnail => ServeURL + item.ID + "/placeThumbnail",
			ResourceType.PlaceIcon => ServeURL + item.ID + "/placeIcon",
			ResourceType.UserThumbnail => ServeURL + item.ID + "/userAvatar",
			ResourceType.UserHeadshot => ServeURL + item.ID + "/userAvatarHeadshot",
			ResourceType.GuildThumbnail => ServeURL + item.ID + "/guildIcon",
			ResourceType.GuildBanner => ServeURL + item.ID + "/guildBanner",
			_ => throw new NotImplementedException()
		};

		ServeResponse response = await _client.GetFromJsonAsync(url, ServeResponseGenerationContext.Default.ServeResponse);
		byte[] buffer = await _client.GetByteArrayAsync(response.Url);

		item.DirectURL = response.Url;

		switch (item.Type)
		{
			case ResourceType.Mesh:
				{
					GltfDocument document = new();
					GltfState state = new() { CreateAnimations = true };

					document.AppendFromBuffer(buffer, null, state);

					Node3D scene = (Node3D)document.GenerateScene(state);

					TaskCompletionSource<PackedScene> callback = new();

					Callable.From(() =>
					{
						PackedScene mesh = new();
						mesh.Pack(scene);
						scene.Free();

						callback.SetResult(mesh);
					}).CallDeferred();

					item.Resource = await callback.Task;

					return item;
				}
			case ResourceType.Audio:
				{
					item.Resource = new AudioStreamMP3() { Data = buffer };

					return item;
				}
			case ResourceType.Asset:
			case ResourceType.Decal:
			case ResourceType.AssetThumbnail:
			case ResourceType.PlaceThumbnail:
			case ResourceType.PlaceIcon:
			case ResourceType.UserThumbnail:
			case ResourceType.UserHeadshot:
			case ResourceType.GuildThumbnail:
			case ResourceType.GuildBanner:
				{
					Image image = new();
					image.LoadPngFromBuffer(buffer);
					image.GenerateMipmaps();
					image.FixAlphaEdges();

					if (item.Resize != null)
					{
						image.Resize(item.Resize.Value.X, item.Resize.Value.Y, Image.Interpolation.Lanczos);
					}

					item.Resource = ImageTexture.CreateFromImage(image);

					return item;
				}
			default: throw new NotImplementedException();
		}
	}

	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}

internal struct ServeResponse
{
	[JsonPropertyName("url")]
	public string Url { get; set; }
}

[JsonSerializable(typeof(ServeResponse))]
[JsonSerializable(typeof(string))]
internal partial class ServeResponseGenerationContext : JsonSerializerContext { }
