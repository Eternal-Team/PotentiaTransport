using System.Collections.Generic;
using System.IO;
using System.Linq;
using PotentiaTransport.Grid;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using TheOneLibrary.Energy.Energy;

namespace PotentiaTransport.Global
{
	public static class Net
	{
		internal enum MessageType : byte
		{
			CablePlacement,
			CableRemovement,
			CableModification,
			GridEnergy,
			GridReform,
			GridMerge
		}

		public static void HandlePacket(BinaryReader reader, int sender)
		{
			MessageType type = (MessageType)reader.ReadByte();
			switch (type)
			{
				case MessageType.CablePlacement:
					ReceiveCablePlacement(reader, sender);
					break;
				case MessageType.CableRemovement:
					ReceiveCableRemovement(reader, sender);
					break;
				case MessageType.CableModification:
					ReceiveCableModification(reader, sender);
					break;
				case MessageType.GridEnergy:
					ReceiveGridEnergy(reader, sender);
					break;
				case MessageType.GridReform:
					ReceiveGridReform(reader, sender);
					break;
				case MessageType.GridMerge:
					ReceiveGridMerge(reader, sender);
					break;
			}
		}

		public static void SendCablePlacement(Cable cable, int excludedPlayer = -1)
		{
			if (Main.netMode == NetmodeID.SinglePlayer) return;

			ModPacket packet = PotentiaTransport.Instance.GetPacket();
			packet.Write((byte)MessageType.CablePlacement);
			TagIO.Write(new TagCompound
			{
				["Position"] = cable.position,
				["Name"] = cable.name
			}, packet);
			packet.Send(ignoreClient: excludedPlayer);
		}

		public static void ReceiveCablePlacement(BinaryReader reader, int sender)
		{
			TagCompound tag = TagIO.Read(reader);
			Point16 position = tag.Get<Point16>("Position");
			string name = tag.GetString("Name");

			Cable cable = new Cable();
			cable.SetDefaults(name);
			cable.position = position;
			cable.layer = PTWorld.Instance.layer;
			cable.grid = new CableGrid
			{
				energy = new EnergyStorage(cable.MaxIO * 2, cable.MaxIO),
				tiles = new List<Cable> { cable }
			};
			PTWorld.Instance.layer.Add(position, cable);

			cable.Merge();
			cable.Frame();

			foreach (Cable merge in Cable.sides.Select(x => x + position).Where(PTWorld.Instance.layer.ContainsKey).Select(x => PTWorld.Instance.layer[x]).Where(x => x.name == name)) merge.Frame();

			if (Main.netMode == NetmodeID.Server) SendCablePlacement(cable, sender);
		}

		public static void SendCableRemovement(Cable cable, int excludedPlayer = -1)
		{
			if (Main.netMode == NetmodeID.SinglePlayer) return;

			ModPacket packet = PotentiaTransport.Instance.GetPacket();
			packet.Write((byte)MessageType.CableRemovement);
			TagIO.Write(new TagCompound
			{
				["Position"] = cable.position
			}, packet);
			packet.Send(ignoreClient: excludedPlayer);
		}

		public static void ReceiveCableRemovement(BinaryReader reader, int sender)
		{
			Cable cable = PTWorld.Instance.layer[TagIO.Read(reader).Get<Point16>("Position")];

			cable.grid.RemoveTile(cable);
			PTWorld.Instance.layer.Remove(cable.position);

			foreach (Point16 point in Cable.sides.Select(x => x + cable.position).Where(x => PTWorld.Instance.layer.ContainsKey(x))) PTWorld.Instance.layer[point].Frame();

			if (Main.netMode == NetmodeID.Server) SendCableRemovement(cable, sender);
		}

		public static void SendCableModification(Cable cable, int excludedPlayer = -1)
		{
			if (Main.netMode == NetmodeID.SinglePlayer) return;

			ModPacket packet = PotentiaTransport.Instance.GetPacket();
			packet.Write((byte)MessageType.CableModification);
			TagIO.Write(new TagCompound
			{
				["Position"] = cable.position,
				["Connections"] = cable.connections,
				["IO"] = (int)cable.IO
			}, packet);
			packet.Send(ignoreClient: excludedPlayer);
		}

		public static void ReceiveCableModification(BinaryReader reader, int sender)
		{
			TagCompound tag = TagIO.Read(reader);
			Cable cable = PTWorld.Instance.layer[tag.Get<Point16>("Position")];
			cable.connections = tag.GetList<bool>("Connections").ToList();
			cable.IO = (IO)tag.GetInt("IO");
			cable.Frame();

			if (Main.netMode == NetmodeID.Server) SendCableModification(cable, sender);
		}

		public static void SendGridEnergy(Point16 position, long delta, int excludedPlayer = -1)
		{
			if (Main.netMode == NetmodeID.SinglePlayer) return;

			ModPacket packet = PotentiaTransport.Instance.GetPacket();
			packet.Write((byte)MessageType.GridEnergy);
			TagIO.Write(new TagCompound
			{
				["Position"] = position,
				["Energy"] = delta
			}, packet);
			packet.Send(ignoreClient: excludedPlayer);
		}

		public static void ReceiveGridEnergy(BinaryReader reader, int sender)
		{
			TagCompound tag = TagIO.Read(reader);
			Point16 position = tag.Get<Point16>("Position");
			long delta = tag.GetLong("Energy");

			if (PTWorld.Instance.layer.ContainsKey(position))
			{
				PTWorld.Instance.layer[position].grid.energy.ModifyEnergyStored(delta);

				if (Main.netMode == NetmodeID.Server) SendGridEnergy(position, delta, sender);
			}
		}

		public static void SendGridReform(Cable cable, int excludedPlayer = -1)
		{
			if (Main.netMode == NetmodeID.SinglePlayer) return;

			ModPacket packet = PotentiaTransport.Instance.GetPacket();
			packet.Write((byte)MessageType.GridReform);
			TagIO.Write(new TagCompound
			{
				["Position"] = cable.position
			}, packet);
			packet.Send(ignoreClient: excludedPlayer);
		}

		public static void ReceiveGridReform(BinaryReader reader, int sender)
		{
			TagCompound tag = TagIO.Read(reader);
			Cable cable = PTWorld.Instance.layer[tag.Get<Point16>("Position")];

			cable.grid.ReformGrid();

			if (Main.netMode == NetmodeID.Server) SendGridReform(cable, sender);
		}

		public static void SendGridMerge(Cable cable1, Cable cable2, int excludedPlayer = -1)
		{
			if (Main.netMode == NetmodeID.SinglePlayer) return;

			ModPacket packet = PotentiaTransport.Instance.GetPacket();
			packet.Write((byte)MessageType.GridMerge);
			TagIO.Write(new TagCompound
			{
				["Position1"] = cable1.position,
				["Position2"] = cable2.position
			}, packet);
			packet.Send(ignoreClient: excludedPlayer);
		}

		public static void ReceiveGridMerge(BinaryReader reader, int sender)
		{
			TagCompound tag = TagIO.Read(reader);
			Cable cable1 = PTWorld.Instance.layer[tag.Get<Point16>("Position1")];
			Cable cable2 = PTWorld.Instance.layer[tag.Get<Point16>("Position2")];

			cable1.grid.MergeGrids(cable2.grid);

			if (Main.netMode == NetmodeID.Server) SendGridMerge(cable1, cable2, sender);
		}
	}
}