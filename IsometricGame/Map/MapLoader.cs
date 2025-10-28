
using IsometricGame.Classes;using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
namespace IsometricGame.Map{
    public class MapLoader
    {
        public LoadedMapData LoadMapDataFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.WriteLine($"Erro: Arquivo de mapa não encontrado em {filePath}");
                return null;            }

            string jsonContent = File.ReadAllText(filePath);
            MapData mapData = null;

            try
            {
                mapData = JsonConvert.DeserializeObject<MapData>(jsonContent);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"Erro ao desserializar o mapa {filePath}: {ex.Message}");
                return null;            }


            if (mapData == null)
            {
                Debug.WriteLine($"Erro: Falha ao desserializar o mapa {filePath} (resultado nulo).");
                return null;
            }

            List<Sprite> loadedTileSprites = new List<Sprite>();
            Dictionary<Vector3, Sprite> loadedSolidTiles = new Dictionary<Vector3, Sprite>();
            List<MapTrigger> loadedTriggers = mapData.Triggers ?? new List<MapTrigger>();
            Dictionary<int, TileMappingEntry> tileLookup = new Dictionary<int, TileMappingEntry>();
            if (mapData.TileMapping != null)
            {
                try
                {
                    tileLookup = mapData.TileMapping.ToDictionary(entry => entry.Id, entry => entry);
                }
                catch (ArgumentException ex)
                {
                    Debug.WriteLine($"Erro no tileMapping do mapa {filePath}: IDs duplicados? {ex.Message}");
                    return null;
                }
            }
            else
            {
                Debug.WriteLine($"Aviso: tileMapping está nulo ou vazio no mapa {filePath}.");
            }


            if (mapData.Layers != null)            {
                foreach (var layer in mapData.Layers)
                {
                    if (layer.Data == null)                    {
                        Debug.WriteLine($"Aviso: Camada '{layer.Name}' em {filePath} não possui dados (array 'data' nulo).");
                        continue;
                    }

                    for (int i = 0; i < layer.Data.Count; i++)
                    {
                        int tileId = layer.Data[i];
                        if (tileId == 0) continue;
                        int x = i % mapData.Width;
                        int y = i / mapData.Width;

                        if (tileLookup.TryGetValue(tileId, out TileMappingEntry tileInfo))
                        {
                            if (GameEngine.Assets.Images.TryGetValue(tileInfo.AssetName, out Texture2D texture))
                            {
                                Vector3 worldPos = new Vector3(x, y, layer.ZLevel);
                                var tileSprite = new Sprite(texture, worldPos);

                                loadedTileSprites.Add(tileSprite);

                                bool isSolid = false;

                                if (tileInfo.Solid)
                                {
                                    isSolid = true;
                                }
                                else
                                {
                                    if (layer.ZLevel > 0)
                                    {
                                        isSolid = true;
                                    }
                                    else if (layer.ZLevel == 0 && tileInfo.AssetName.Contains("water_"))
                                    {
                                        isSolid = true;
                                    }
                                }

                                if (isSolid)
                                {
                                    loadedSolidTiles[worldPos] = tileSprite;
                                }
                            }
                            else
                            {
                                Debug.WriteLine($"Aviso: Textura '{tileInfo.AssetName}' (ID: {tileId}) não encontrada no AssetManager para o mapa {filePath}.");
                            }
                        }
                        else
                        {
                            Debug.WriteLine($"Aviso: Tile ID {tileId} na camada '{layer.Name}' não encontrado no tileMapping do mapa {filePath}.");
                        }
                    }                }            }            else
            {
                Debug.WriteLine($"Aviso: Nenhuma camada ('layers') encontrada no mapa {filePath}.");
            }

            Debug.WriteLine($"Dados do mapa {filePath} processados. {loadedTileSprites.Count} sprites de tile, {loadedSolidTiles.Count} tiles sólidos, {loadedTriggers.Count} triggers.");

            return new LoadedMapData(loadedTileSprites, loadedSolidTiles, loadedTriggers, mapData);
        }
    }
}