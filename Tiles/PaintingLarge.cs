using ClickerClass.Items.Placeable;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace ClickerClass.Tiles
{
	public class PaintingLarge : ModTile
	{
		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileLavaDeath[Type] = true;
			TileID.Sets.FramesOnKillWall[Type] = true;
			TileID.Sets.DisableSmartCursor[Type] = true;
			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3Wall);
			TileObjectData.newTile.Height = 3;
			TileObjectData.newTile.Width = 4;
			TileObjectData.newTile.StyleHorizontal = true;
			TileObjectData.newTile.StyleWrapLimit = 36;
			TileObjectData.addTile(Type);
			DustType = 7;
			ModTranslation name = CreateMapEntryName();
			name.SetDefault("Painting");
			AddMapEntry(new Color(90, 50, 30), name);
		}

		public override void KillMultiTile(int i, int j, int frameX, int frameY) 
		{
			int item = 0;
			switch (frameX / 54)
			{
				case 0:
					item = ModContent.ItemType<OutsideTheCave>();
					break;
			}
			if (item > 0) 
			{
				Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 48, 48, item);
			}
		}
	}
}