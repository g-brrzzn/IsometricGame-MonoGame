
using IsometricGame.Classes;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;

namespace IsometricGame.Map
{
    public class MapManager
    {
        private MapLoader _mapLoader;
        private List<Sprite> _currentMapSprites;        private LoadedMapData _currentLoadedMapData;        public string CurrentMapName { get; private set; }

        public MapManager()
        {
            _mapLoader = new MapLoader();
            _currentMapSprites = new List<Sprite>();
            CurrentMapName = null;
        }

        public bool LoadMap(string mapFileName)
        {
            Debug.WriteLine($"MapManager: Tentando carregar mapa '{mapFileName}'...");
            UnloadCurrentMap();

            _currentLoadedMapData = _mapLoader.LoadMapDataFromFile($"Content/maps/{mapFileName}");
            if (_currentLoadedMapData == null)
            {
                Debug.WriteLine($"MapManager: Falha ao carregar dados de '{mapFileName}'.");
                CurrentMapName = null;
                return false;
            }

            CurrentMapName = mapFileName;

            _currentMapSprites.AddRange(_currentLoadedMapData.TileSprites);
            GameEngine.AllSprites.AddRange(_currentMapSprites);

            foreach (var kvp in _currentLoadedMapData.SolidTiles)
            {
                GameEngine.SolidTiles[kvp.Key] = kvp.Value;
            }

            Debug.WriteLine($"MapManager: Mapa '{mapFileName}' carregado. Adicionados {_currentMapSprites.Count} sprites e {GameEngine.SolidTiles.Count} tiles sólidos ao GameEngine.");
            return true;
        }

        public void UnloadCurrentMap()
        {
            if (CurrentMapName == null || _currentMapSprites.Count == 0)
            {
                return;
            }

            Debug.WriteLine($"MapManager: Descarregando mapa '{CurrentMapName}'...");

            int removedCount = 0;
            for (int i = _currentMapSprites.Count - 1; i >= 0; i--)
            {
                if (GameEngine.AllSprites.Remove(_currentMapSprites[i]))
                {
                    removedCount++;
                }
            }


            Debug.WriteLine($"MapManager: Removidos {removedCount} sprites do mapa de GameEngine.AllSprites.");

            _currentMapSprites.Clear();

            GameEngine.SolidTiles.Clear();
            Debug.WriteLine($"MapManager: GameEngine.SolidTiles limpo.");


            _currentLoadedMapData = null;
            CurrentMapName = null;
            Debug.WriteLine($"MapManager: Mapa descarregado.");
        }

        public LoadedMapData GetCurrentMapData()
        {
            return _currentLoadedMapData;
        }

        public List<MapTrigger> GetCurrentTriggers()
        {
            return _currentLoadedMapData?.Triggers ?? new List<MapTrigger>();        }
    }
}