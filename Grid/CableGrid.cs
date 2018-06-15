using System.Collections.Generic;
using TheOneLibrary.Energy.Energy;

namespace PotentiaTransport.Grid
{
	public class CableGrid
	{
		public List<Cable> tiles = new List<Cable>();
		public EnergyStorage energy = new EnergyStorage();

		public float GetEnergySharePerNode() => (float)energy.GetEnergy() / tiles.Count;

		public void AddTile(Cable tile)
		{
			if (!tiles.Contains(tile))
			{
				energy.AddCapacity(tile.MaxIO * 2);
				energy.ModifyEnergyStored((long)tile.grid.GetEnergySharePerNode());
				tile.grid = this;
				tiles.Add(tile);
			}
		}

		public void RemoveTile(Cable tile)
		{
			if (tiles.Contains(tile))
			{
				tiles.Remove(tile);
				ReformGrid();
			}
		}

		public void MergeGrids(CableGrid wireGrid)
		{
			for (int i = 0; i < wireGrid.tiles.Count; i++) AddTile(wireGrid.tiles[i]);
		}

		public void ReformGrid()
		{
			long share = (long)GetEnergySharePerNode();
			for (int i = 0; i < tiles.Count; i++)
			{
				tiles[i].grid = new CableGrid
				{
					energy = new EnergyStorage(tiles[i].MaxIO * 2, tiles[i].MaxIO),
					tiles = new List<Cable> { tiles[i] }
				};
				tiles[i].grid.energy.ModifyEnergyStored(share);
			}

			for (int i = 0; i < tiles.Count; i++) tiles[i].Merge();
		}
	}
}