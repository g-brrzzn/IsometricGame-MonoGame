
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Text;
namespace IsometricGame.States.Utility
{
    public class TextInputState : GameStateBase
    {
        private SpriteFont _font;
        private Texture2D _pixelTexture;

        private string _prompt = "Enter text:";
        private StringBuilder _currentText;
        private string _returnState = "Menu";
        private Action<string> _onCompleteAction;

        private KeyboardState _prevKeyState;
        private int _cursorIndex = 0;        
        private double _cursorBlinkTimer = 0;
        private bool _showCursor = true;
        private const double CURSOR_BLINK_TIME = 0.5;
        public override void Start()
        {
            base.Start();
            _font = GameEngine.Assets.Fonts["captain_42"];
            _pixelTexture = GameEngine.Assets.Images["pixel"];

            _prompt = GameEngine.TextInputPrompt;
            _currentText = new StringBuilder(GameEngine.TextInputDefaultValue ?? "");
            _returnState = GameEngine.TextInputReturnState ?? "Menu";
            _onCompleteAction = GameEngine.OnTextInputComplete;
            _cursorIndex = _currentText.Length;

            GameEngine.OnTextInputComplete = null;
            GameEngine.TextInputPrompt = "";
            GameEngine.TextInputDefaultValue = "";
            GameEngine.TextInputReturnState = "Menu";

            _prevKeyState = Keyboard.GetState();
            _cursorBlinkTimer = 0;
            _showCursor = true;
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            KeyboardState currentKeyState = Keyboard.GetState();
            _cursorBlinkTimer += gameTime.ElapsedGameTime.TotalSeconds;
            if (_cursorBlinkTimer >= CURSOR_BLINK_TIME)
            {
                _showCursor = !_showCursor;
                _cursorBlinkTimer -= CURSOR_BLINK_TIME;
            }

            bool cursorMoved = false;
            if (IsKeyJustPressed(currentKeyState, Keys.Enter))
            {
                _onCompleteAction?.Invoke(_currentText.ToString());
                IsDone = true;
                NextState = _returnState;
                return;
            }

            if (IsKeyJustPressed(currentKeyState, Keys.Escape))
            {
                IsDone = true;
                NextState = _returnState;
                return;
            }
            if (IsKeyJustPressed(currentKeyState, Keys.Left))
            {
                _cursorIndex = Math.Max(0, _cursorIndex - 1);
                cursorMoved = true;
            }

            if (IsKeyJustPressed(currentKeyState, Keys.Right))
            {
                _cursorIndex = Math.Min(_currentText.Length, _cursorIndex + 1);
                cursorMoved = true;
            }
            if (IsKeyJustPressed(currentKeyState, Keys.Home))
            {
                _cursorIndex = 0;
                cursorMoved = true;
            }
            if (IsKeyJustPressed(currentKeyState, Keys.End))
            {
                _cursorIndex = _currentText.Length;
                cursorMoved = true;
            }
            if (IsKeyJustPressed(currentKeyState, Keys.Back) && _cursorIndex > 0)
            {
                _cursorIndex--;
                _currentText.Remove(_cursorIndex, 1);
                cursorMoved = true;
            }

            if (IsKeyJustPressed(currentKeyState, Keys.Delete) && _cursorIndex < _currentText.Length)
            {
                _currentText.Remove(_cursorIndex, 1);
                cursorMoved = true;
            }
            Keys[] keys = currentKeyState.GetPressedKeys();
            bool shift = currentKeyState.IsKeyDown(Keys.LeftShift) || currentKeyState.IsKeyDown(Keys.RightShift);

            foreach (Keys key in keys)
            {
                if (IsKeyJustPressed(currentKeyState, key))
                {
                    char c = GetCharFromKey(key, shift);
                    if (c != '\0')                    {
                        _currentText.Insert(_cursorIndex, c);                        
                        _cursorIndex++;                        
                        cursorMoved = true;
                    }
                }
            }
            if (cursorMoved)
            {
                _showCursor = true;
                _cursorBlinkTimer = 0;
            }

            _prevKeyState = currentKeyState;
        }

        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            Vector2 center = new Vector2(Constants.InternalResolution.X / 2f, Constants.InternalResolution.Y / 2f);

            Rectangle bgRect = new Rectangle(0, 0, Constants.InternalResolution.X, Constants.InternalResolution.Y);
            spriteBatch.Draw(_pixelTexture, bgRect, Color.Black * 0.7f);

            DrawUtils.DrawTextScreen(spriteBatch, _prompt, _font, center - new Vector2(0, 50), Color.White, 0f);

            string textToDraw = _currentText.ToString();
            Vector2 fullTextSize = _font.MeasureString(textToDraw);

            Rectangle textBoxRect = new Rectangle((int)(center.X - 300), (int)(center.Y - 25), 600, 50);
            spriteBatch.Draw(_pixelTexture, textBoxRect, Color.DarkSlateGray);
            Vector2 textPosition = new Vector2(textBoxRect.X + 10, center.Y);
            spriteBatch.DrawString(_font, textToDraw, textPosition, Color.White, 0f, new Vector2(0, _font.MeasureString("A").Y / 2f), 1f, SpriteEffects.None, 0f);
            if (_showCursor)
            {
                string textBeforeCursor = _currentText.ToString(0, _cursorIndex);
                Vector2 cursorOffset = _font.MeasureString(textBeforeCursor);
                Vector2 cursorPosition = new Vector2(textBoxRect.X + 10 + cursorOffset.X, center.Y);
                Vector2 cursorSize = _font.MeasureString("_");
                spriteBatch.DrawString(_font, "_", cursorPosition, Color.White, 0f, new Vector2(0, cursorSize.Y / 2f), 1f, SpriteEffects.None, 0f);
            }
        }

        private bool IsKeyJustPressed(KeyboardState current, Keys key)
        {
            return current.IsKeyDown(key) && _prevKeyState.IsKeyUp(key);
        }

        private char GetCharFromKey(Keys key, bool shift)
        {
            if (key >= Keys.D0 && key <= Keys.D9)
            {
                if (shift)
                {
                    switch (key)
                    {
                        case Keys.D0: return ')';
                        case Keys.D1: return '!';
                        case Keys.D2: return '@';
                        case Keys.D3: return '#';
                        case Keys.D4: return '$';
                        case Keys.D5: return '%';
                        case Keys.D6: return '^';
                        case Keys.D7: return '&';
                        case Keys.D8: return '*';
                        case Keys.D9: return '(';
                    }
                }
                else return (char)('0' + (key - Keys.D0));
            }
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                return (char)('0' + (key - Keys.NumPad0));
            }
            if (key >= Keys.A && key <= Keys.Z)
            {
                return shift ? (char)key : char.ToLower((char)key);
            }
            if (shift)
            {
                switch (key)
                {
                    case Keys.OemPeriod: return '>';
                    case Keys.OemComma: return '<';
                    case Keys.OemMinus: return '_';
                    case Keys.OemPlus: return '+';                }
            }
            else
            {
                switch (key)
                {
                    case Keys.OemPeriod: return '.';
                    case Keys.OemComma: return ',';
                    case Keys.OemMinus: return '-';
                    case Keys.OemPlus: return '=';                    
                    case Keys.Space: return ' ';
                }
            }

            return '\0';
        }
    }
}