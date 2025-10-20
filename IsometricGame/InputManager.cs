using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
// --- ADIÇÃO 1: Adicionar namespace do XNA para Point e Rectangle ---
using Microsoft.Xna.Framework;

namespace IsometricGame
{
    public class InputManager
    {
        private KeyboardState _currentKeyState, _previousKeyState;
        // --- ADIÇÃO 2: Variáveis para o Mouse ---
        private MouseState _currentMouseState, _previousMouseState;
        private Rectangle _renderDestination;
        private Point _internalResolution;
        public Vector2 InternalMousePosition { get; private set; }
        // --- FIM DA ADIÇÃO 2 ---


        private static readonly Dictionary<string, Keys[]> _controls = new Dictionary<string, Keys[]>
    {
      { "UP", new[] { Keys.W, Keys.Up } },
      { "DOWN", new[] { Keys.S, Keys.Down } },
      { "LEFT", new[] { Keys.A, Keys.Left } },
      { "RIGHT", new[] { Keys.D, Keys.Right } },
      { "FIRE", new[] { Keys.Space } },
      { "START", new[] { Keys.Enter, Keys.Space } },
      { "ESC", new[] { Keys.Escape } }
    };

        // --- ADIÇÃO 3: Método para Game1 informar a área de renderização ---
        /// <summary>
        /// Atualiza o InputManager com a área de renderização atual e a resolução interna.
        /// Isso é vital para converter a posição do mouse da janela para o mundo do jogo.
        /// </summary>
        public void SetScreenConversion(Rectangle renderDestination, Point internalResolution)
        {
            _renderDestination = renderDestination;
            _internalResolution = internalResolution;
        }
        // --- FIM DA ADIÇÃO 3 ---

        public void Update()
        {
            _previousKeyState = _currentKeyState;
            _currentKeyState = Keyboard.GetState();

            // --- ADIÇÃO 4: Atualizar estado do mouse e calcular posição interna ---
            _previousMouseState = _currentMouseState;
            _currentMouseState = Mouse.GetState();

            // Converte a posição do mouse da janela para a posição na resolução interna
            if (_renderDestination.Width > 0 && _renderDestination.Height > 0)
            {
                // 1. Remove o offset (barras pretas)
                float mouseInRenderX = _currentMouseState.X - _renderDestination.X;
                float mouseInRenderY = _currentMouseState.Y - _renderDestination.Y;

                // 2. Converte de volta para a resolução interna
                float internalX = mouseInRenderX / (float)_renderDestination.Width * _internalResolution.X;
                float internalY = mouseInRenderY / (float)_renderDestination.Height * _internalResolution.Y;

                InternalMousePosition = new Vector2(internalX, internalY);
            }
            else
            {
                // Failsafe antes do _renderDestination ser calculado
                InternalMousePosition = new Vector2(_currentMouseState.X, _currentMouseState.Y);
            }
            // --- FIM DA ADIÇÃO 4 ---

            var pressedKeys = _currentKeyState.GetPressedKeys();
            if (pressedKeys.Length > 0)
            {
                Debug.WriteLine($"InputManager.Update - Current Keys: {string.Join(", ", pressedKeys)}");
            }
        }

        public bool IsKeyDown(string action)
        {
            if (!_controls.ContainsKey(action))
            {
                Debug.WriteLine($"Warning: Input action '{action}' not found in controls map.");
                return false;
            }

            foreach (var key in _controls[action])
            {
                if (_currentKeyState.IsKeyDown(key))
                    return true;
            }
            return false;
        }

        public bool IsKeyPressed(string action)
        {
            if (!_controls.ContainsKey(action))
            {
                return false;
            }

            foreach (var key in _controls[action])
            {
                if (_currentKeyState.IsKeyDown(key) && _previousKeyState.IsKeyUp(key))
                {
                    Debug.WriteLine($"InputManager.IsKeyPressed TRUE for action: {action}, key: {key}");
                    return true;
                }
            }
            return false;
        }

        // --- ADIÇÃO 5: Métodos para verificar cliques do mouse ---
        public bool IsLeftMouseButtonDown()
        {
            return _currentMouseState.LeftButton == ButtonState.Pressed;
        }

        public bool IsLeftMouseButtonPressed()
        {
            return _currentMouseState.LeftButton == ButtonState.Pressed &&
                   _previousMouseState.LeftButton == ButtonState.Released;
        }
        // --- FIM DA ADIÇÃO 5 ---
    }
}