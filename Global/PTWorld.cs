using System;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;
using PotentiaTransport.Grid;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace PotentiaTransport.Global
{
	public class PTWorld : ModWorld
	{
		public static PTWorld Instance;

		public CableLayer layer = new CableLayer();

		public override void Initialize()
		{
			Instance = this;
		}

		public override void PreUpdate()
		{
			layer.Update();
		}

		public override void PostDrawTiles()
		{
			RasterizerState rasterizer = Main.gameMenu || Math.Abs(Main.LocalPlayer.gravDir - 1.0) < 0.1 ? RasterizerState.CullCounterClockwise : RasterizerState.CullClockwise;
			Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

			layer.Draw(Main.spriteBatch);

			Main.spriteBatch.End();
		}

		public override TagCompound Save() => new TagCompound
		{
			["Layer"] = layer.Save()
		};

		public override void Load(TagCompound tag)
		{
			layer.Load(tag.GetList<TagCompound>("Layer").ToList());
		}

		public override void NetSend(BinaryWriter writer)
		{
			using (MemoryStream stream = new MemoryStream())
			{
				TagIO.ToStream(Save(), stream);
				byte[] data = stream.ToArray();
				writer.Write(data.Length);
				writer.Write(data);
			}
		}

		public override void NetReceive(BinaryReader reader)
		{
			int count = reader.ReadInt32();
			using (MemoryStream stream = new MemoryStream(reader.ReadBytes(count)))
			{
				Load(TagIO.FromStream(stream));
			}
		}
	}
}