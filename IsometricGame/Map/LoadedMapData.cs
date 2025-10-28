
using IsometricGame.Classes;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace IsometricGame.Map
{
    public class LoadedMapData
    {
        public List<Sprite> TileSprites { get; private set; }
        public Dictionary<Vector3, Sprite> SolidTiles { get; private set; }
        public MapData OriginalMapData { get; private set; }
        public List<MapTrigger> Triggers { get; private set; }
        public LoadedMapData(List<Sprite> tileSprites, Dictionary<Vector3, Sprite> solidTiles, List<MapTrigger> triggers, MapData originalMapData = null)
        {
            TileSprites = tileSprites;
            SolidTiles = solidTiles;
            Triggers = triggers ?? new List<MapTrigger>();            OriginalMapData = originalMapData;
        }
    }
}