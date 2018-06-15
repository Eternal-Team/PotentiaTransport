using System.IO;
using Microsoft.Xna.Framework.Graphics;
using PotentiaTransport.Global;
using PotentiaTransport.Grid;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TheOneLibrary.Base;
using TheOneLibrary.Utils;

namespace PotentiaTransport
{
	public class PotentiaTransport : Mod
	{
		public struct Textures
		{
			public const string Path = "PotentiaTransport/Textures/";
			public const string TilePath = Path + "Tiles/";

			[Texture(Path + "CableGrid/BasicWire")] public static Texture2D cableTexture;
			[Texture(Path + "CableGrid/Connections")] public static Texture2D cableIOTexture;
		}

		public static PotentiaTransport Instance;

		public static CableSerializer serializer = new CableSerializer();

		public override void Load()
		{
			Instance = this;

			TagSerializer.AddSerializer(serializer);

			Utility.LoadTextures();
		}

		public override void HandlePacket(BinaryReader reader, int whoAmI) => Net.HandlePacket(reader, whoAmI);
	}
}