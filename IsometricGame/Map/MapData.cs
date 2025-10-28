
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;
namespace IsometricGame.Map
{
    public class TileMappingEntry
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("assetName")]
        public string AssetName { get; set; }

        [JsonProperty("solid")]
        public bool Solid { get; set; }
    }

    public class MapLayer
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("zLevel")] public int ZLevel { get; set; }

        [JsonProperty("data")] public List<int> Data { get; set; }
    }

    public class MapTrigger
    {
        [JsonProperty("id")]        public string Id { get; set; }

        [JsonProperty("position")]        public Vector3 Position { get; set; }

        [JsonProperty("targetMap")]        public string TargetMap { get; set; }

        [JsonProperty("targetPosition")]        public Vector3 TargetPosition { get; set; }

        [JsonProperty("radius")]        public float Radius { get; set; } = 0.5f;    }

    public class MapData
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("tileMapping")] public List<TileMappingEntry> TileMapping { get; set; }

        [JsonProperty("layers")] public List<MapLayer> Layers { get; set; }

        [JsonProperty("triggers")]
        public List<MapTrigger> Triggers { get; set; } = new List<MapTrigger>();    }
}