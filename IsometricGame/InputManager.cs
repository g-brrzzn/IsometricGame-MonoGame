
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;using Microsoft.Xna.Framework;
using System;
namespace IsometricGame
{
    public class InputManager
    {
        private KeyboardState _currentKeyState, _previousKeyState;
        private MouseState _currentMouseState, _previousMouseState;

        private Rectangle _renderDestination;
        private Point _internalResolution;
        private int _previousScrollWheelValue = 0;

        public Vector2 InternalMousePosition { get; private set; }        public int ScrollWheelDelta { get; private set; }
        private static readonly Dictionary<string, Keys[]> _controls = new Dictionary<string, Keys[]>
        {
            { "UP", new[] { Keys.W, Keys.Up } },
            { "DOWN", new[] { Keys.S, Keys.Down } },
            { "LEFT", new[] { Keys.A, Keys.Left } },
            { "RIGHT", new[] { Keys.D, Keys.Right } },

            { "FIRE", new[] { Keys.Space } },            { "START", new[] { Keys.Enter, Keys.Space } },
            { "ESC", new[] { Keys.Escape } },
            { "ZOOM_IN", new[] { Keys.OemPlus, Keys.Add } },
            { "ZOOM_OUT", new[] { Keys.OemMinus, Keys.Subtract } },
            { "NEXT_TILE", new[] { Keys.PageDown, Keys.E } },
            { "PREV_TILE", new[] { Keys.PageUp, Keys.Q } },
            { "SAVE_MODIFIER", new[] { Keys.LeftControl, Keys.RightControl } },
            { "SAVE_ACTION", new[] { Keys.S } },
            { "LOAD_MODIFIER", new[] { Keys.LeftControl, Keys.RightControl } },
            { "LOAD_ACTION", new[] { Keys.L } },
            { "NEW_MODIFIER", new[] { Keys.LeftControl, Keys.RightControl } },
            { "NEW_ACTION", new[] { Keys.N } },
            { "SWITCH_MODE", new[] { Keys.Tab } },            
            { "DELETE_TRIGGER", new[] { Keys.Delete } },
            { "D0", new[] { Keys.D0, Keys.NumPad0 } },
            { "D1", new[] { Keys.D1, Keys.NumPad1 } },
            { "D2", new[] { Keys.D2, Keys.NumPad2 } },
            { "D3", new[] { Keys.D3, Keys.NumPad3 } },
            { "D4", new[] { Keys.D4, Keys.NumPad4 } },
            { "D5", new[] { Keys.D5, Keys.NumPad5 } },
            { "D6", new[] { Keys.D6, Keys.NumPad6 } },
            { "D7", new[] { Keys.D7, Keys.NumPad7 } },
            { "D8", new[] { Keys.D8, Keys.NumPad8 } },
            { "D9", new[] { Keys.D9, Keys.NumPad9 } },
			{ "EDIT_TRIGGER_TARGET_MAP", new[] { Keys.T } },
            { "EDIT_TRIGGER_TARGET_POS", new[] { Keys.P } },
            { "EDIT_TRIGGER_RADIUS", new[] { Keys.R } },
            { "EDIT_TRIGGER_ID", new[] { Keys.I } },
		};

        public void SetScreenConversion(Rectangle renderDestination, Point internalResolution)
        {
            _renderDestination = renderDestination;
            _internalResolution = internalResolution;
        }

        public void Update()
        {
            _previousKeyState = _currentKeyState;
            _previousMouseState = _currentMouseState;

            _currentKeyState = Keyboard.GetState();
            _currentMouseState = Mouse.GetState();

            ScrollWheelDelta = _currentMouseState.ScrollWheelValue - _previousScrollWheelValue;
            _previousScrollWheelValue = _currentMouseState.ScrollWheelValue;
            if (_renderDestination.Width > 0 && _renderDestination.Height > 0 && _internalResolution.X > 0 && _internalResolution.Y > 0)
            {
                float mouseInRenderX = _currentMouseState.X - _renderDestination.X;
                float mouseInRenderY = _currentMouseState.Y - _renderDestination.Y;

                mouseInRenderX = MathHelper.Clamp(mouseInRenderX, 0, _renderDestination.Width);
                mouseInRenderY = MathHelper.Clamp(mouseInRenderY, 0, _renderDestination.Height);


                float internalX = (mouseInRenderX / _renderDestination.Width) * _internalResolution.X;
                float internalY = (mouseInRenderY / _renderDestination.Height) * _internalResolution.Y;


                InternalMousePosition = new Vector2(internalX, internalY);
            }
            else
            {
                InternalMousePosition = new Vector2(_currentMouseState.X, _currentMouseState.Y);
            }

            /*
            var pressedKeys = _currentKeyState.GetPressedKeys();
            var prevPressedKeys = _previousKeyState.GetPressedKeys();
            if (!pressedKeys.SequenceEqual(prevPressedKeys))
            {
                 if (pressedKeys.Length > 0)
                      Debug.WriteLine($"InputManager.Update - Current Keys: {string.Join(", ", pressedKeys)}");
                 else
                      Debug.WriteLine("InputManager.Update - No keys pressed.");
            }
            if(_currentMouseState != _previousMouseState) {
                 Debug.WriteLine($"Mouse State: Pos({_currentMouseState.X},{_currentMouseState.Y}) Internal({InternalMousePosition.X:F0},{InternalMousePosition.Y:F0}) L:{_currentMouseState.LeftButton} R:{_currentMouseState.RightButton} Scroll:{_currentMouseState.ScrollWheelValue} Delta:{ScrollWheelDelta}");
            }
            */
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
                    return true;            }
            return false;        }

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
                    return true;                }
            }
            return false;        }

        public bool IsLeftMouseButtonDown()
        {
            return _currentMouseState.LeftButton == ButtonState.Pressed;
        }

        public bool IsRightMouseButtonDown()
        {
            return _currentMouseState.RightButton == ButtonState.Pressed;
        }

        public bool IsLeftMouseButtonPressed()
        {
            return _currentMouseState.LeftButton == ButtonState.Pressed &&
                   _previousMouseState.LeftButton == ButtonState.Released;
        }

        public bool IsRightMouseButtonPressed()
        {
            return _currentMouseState.RightButton == ButtonState.Pressed &&
                   _previousMouseState.RightButton == ButtonState.Released;
        }
    }
}