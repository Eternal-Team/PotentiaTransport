using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PotentiaTransport.Global;
using PotentiaTransport.Items.Cables;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TheOneLibrary.Energy.Energy;
using TheOneLibrary.Layer;
using TheOneLibrary.Utils;

namespace PotentiaTransport.Grid
{
	public class Cable : ModLayerElement
	{
		#region Readonly fields
		public static readonly Point16[] sides =
		{
			new Point16(-1, 0),
			new Point16(1, 0),
			new Point16(0, -1),
			new Point16(0, 1)
		};

		public static readonly Point16[] frameOffset =
		{
			new Point16(18, 0),
			new Point16(36, 0),
			new Point16(0, 18),
			new Point16(0, 36)
		};

		public static readonly Point[][] connectionTriangles =
		{
			new[] { new Point(0, 0), new Point(8, 8), new Point(0, 16) },
			new[] { new Point(16, 0), new Point(16, 16), new Point(8, 8) },
			new[] { new Point(0, 0), new Point(16, 0), new Point(8, 8) },
			new[] { new Point(0, 16), new Point(8, 8), new Point(16, 16) }
		};
		#endregion

		public CableGrid grid;
		public CableLayer layer;

		public Point16 position;
		public Point16 frame;
		public string name;
		public long MaxIO;
		public IO IO = IO.In;
		public List<bool> connections = new List<bool> { true, true, true, true };

		public float Share
		{
			get { return grid.GetEnergySharePerNode(); }
			set { grid.energy.ModifyEnergyStored((long)value); }
		}

		public void SetDefaults(string name)
		{
			this.name = name;
			MaxIO = ((BaseCable)PotentiaTransport.Instance.GetItem(name)).MaxIO;
		}

		public void Frame()
		{
			frame = sides.Select(x => x + position).Select((x, i) => layer.ContainsKey(x) && layer[x].name == name && connections[i] && layer[x].connections[i.Counterpart()] ? frameOffset[i] : Point16.Zero).Aggregate((x, y) => x + y);
		}

		public void Modify()
		{
			Point point = new Point((int)Main.MouseWorld.X - position.X * 16, (int)Main.MouseWorld.Y - position.Y * 16);

			Rectangle io = new Rectangle(4, 4, 8, 8);

			if (!io.Contains(point))
			{
				for (int i = 0; i < 4; i++)
				{
					if (point.InTriangle(connectionTriangles[i]))
					{
						connections[i] = !connections[i];
						Net.SendCableModification(this);

						if (!connections[i])
						{
							grid.ReformGrid();
							Net.SendGridReform(this);
						}

						if (layer.ContainsKey(position + sides[i]))
						{
							Cable secCable = layer[position + sides[i]];
							int counterpart = i.Counterpart();
							secCable.connections[counterpart] = !secCable.connections[counterpart];
							Net.SendCableModification(secCable);
							if (connections[i])
							{
								grid.MergeGrids(secCable.grid);
								Net.SendGridMerge(this, secCable);
							}

							secCable.Frame();
						}
					}

					Frame();
					Net.SendCableModification(this);
				}
			}
			else
			{
				IO = IO.NextEnum();
				Net.SendCableModification(this);
			}
		}

		public void Remove()
		{
			grid.RemoveTile(this);
			layer.Remove(position);

			int i = Item.NewItem(position.ToVector2() * 16, new Vector2(16), PotentiaTransport.Instance.ItemType(name));
			NetMessage.SendData(MessageID.SyncItem, -1, -1, null, i);

			foreach (Point16 point in sides.Select(x => x + position).Where(x => layer.ContainsKey(x))) layer[point].Frame();

			Net.SendCableRemovement(this);
		}

		public void Merge()
		{
			List<Point16> list = sides.Select(x => x + position).ToList();
			for (int i = 0; i < 4; i++)
			{
				if (layer.ContainsKey(list[i]) && connections[i] && layer[list[i]].connections[i.Counterpart()]) layer[list[i]].grid.MergeGrids(grid);
			}
		}

		public void Update()
		{
			Point16 check = Utility.TileEntityTopLeft(position);

			if (TileEntity.ByPosition.ContainsKey(check))
			{
				TileEntity te = TileEntity.ByPosition[check];

				if (IO == IO.In && te is IEnergyProvider)
				{
					IEnergyProvider provider = (IEnergyProvider)te;
					long delta = grid.energy.ReceiveEnergy(Utility.Min(grid.energy.GetMaxReceive(), provider.GetEnergyStorage().GetMaxExtract(), provider.GetEnergy()));
					provider.GetEnergyStorage().ModifyEnergyStored(-delta);
					Net.SendGridEnergy(position, delta);
				}
				else if (IO == IO.Out && te is IEnergyReceiver)
				{
					IEnergyReceiver receiver = (IEnergyReceiver)te;
					long delta = -grid.energy.ExtractEnergy(Utility.Min(grid.energy.GetMaxExtract(), receiver.GetEnergyStorage().GetMaxReceive(), receiver.GetCapacity() - receiver.GetEnergy()));
					receiver.GetEnergyStorage().ModifyEnergyStored(-delta);
					Net.SendGridEnergy(position, delta);
				}
			}
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			Vector2 position = -Main.screenPosition + this.position.ToVector2() * 16;
			Point16 pos = Utility.TileEntityTopLeft(this.position.X, this.position.Y);
			Color color = Lighting.GetColor(pos.X, pos.Y);
			spriteBatch.Draw(PotentiaTransport.Textures.cableTexture, position, new Rectangle(frame.X, frame.Y, 16, 16), color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);

			TileEntity te = TileEntity.ByPosition.ContainsKey(pos) ? TileEntity.ByPosition[pos] : null;
			if (te != null && (te is IEnergyReceiver || te is IEnergyProvider)) spriteBatch.Draw(PotentiaTransport.Textures.cableIOTexture, new Rectangle((int)(position.X + 4), (int)(position.Y + 4), 8, 8), new Rectangle((int)IO * 16, 0, 16, 16), Color.White);
		}
	}
}