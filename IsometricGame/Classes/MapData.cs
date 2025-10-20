using System.Collections.Generic;
using Newtonsoft.Json; // Importa a biblioteca

namespace IsometricGame.Classes // Ou apenas IsometricGame se estiver na raiz
{
    // Mapeia a entrada "tileMapping" no JSON
    public class TileMappingEntry
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("assetName")]
        public string AssetName { get; set; }
    }

    // Mapeia cada objeto dentro da lista "layers" no JSON
    public class MapLayer
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("zLevel")] // Nível Z desta camada
        public int ZLevel { get; set; }

        [JsonProperty("data")] // Array 1D com os IDs dos tiles (índice = y * width + x)
        public List<int> Data { get; set; }
    }

    // Classe principal que representa todo o arquivo JSON
    public class MapData
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("tileMapping")] // Lista que associa IDs a nomes de assets
        public List<TileMappingEntry> TileMapping { get; set; }

        [JsonProperty("layers")] // Lista das camadas do mapa
        public List<MapLayer> Layers { get; set; }
    }
}