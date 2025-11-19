using IsometricGame.Classes.Upgrades;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace IsometricGame.States
{
    public class LevelUpState : GameStateBase
    {
        private List<UpgradeOption> _options;
        private Rectangle[] _cardRects;
        private int _hoveredIndex = -1;
        private Texture2D _pixel;
        private SpriteFont _titleFont;
        private SpriteFont _descFont;
        private bool _soundPlayed = false;

        public override void Start()
        {
            base.Start();

            _options = GameEngine.CurrentUpgradeOptions ?? UpgradeManager.GetRandomOptions(3);

            _pixel = GameEngine.Assets.Images["pixel"];
            _titleFont = GameEngine.Assets.Fonts["captain_42"];
            _descFont = GameEngine.Assets.Fonts["captain_32"];

            Game1.Instance.IsMouseVisible = true;
            CalculateLayout();

            _soundPlayed = false;
        }

        private void CalculateLayout()
        {
            int count = _options.Count;
            _cardRects = new Rectangle[count];

            int cardWidth = 300;
            int cardHeight = 450;
            int gap = 40;

            int totalWidth = (cardWidth * count) + (gap * (count - 1));
            int startX = (Constants.InternalResolution.X - totalWidth) / 2;
            int centerY = Constants.InternalResolution.Y / 2;

            for (int i = 0; i < count; i++)
            {
                _cardRects[i] = new Rectangle(startX + i * (cardWidth + gap), centerY - (cardHeight / 2), cardWidth, cardHeight);
            }
        }

        public override void Update(GameTime gameTime, InputManager input)
        {
            if (!_soundPlayed)
            {
                GameEngine.Assets.Sounds["menu_confirm"].Play();
                _soundPlayed = true;
            }

            _hoveredIndex = -1;
            Vector2 mousePos = input.InternalMousePosition;
            Point mousePoint = new Point((int)mousePos.X, (int)mousePos.Y);

            for (int i = 0; i < _cardRects.Length; i++)
            {
                if (_cardRects[i].Contains(mousePoint))
                {
                    _hoveredIndex = i;

                    if (input.IsLeftMouseButtonPressed())
                    {
                        SelectUpgrade(i);
                    }
                }
            }
        }

        private void SelectUpgrade(int index)
        {
            if (GameEngine.Player != null)
            {
                _options[index].ApplyEffect(GameEngine.Player);
            }

            GameEngine.Assets.Sounds["menu_select"].Play();

            IsDone = true;
            NextState = "Game";
        }

        public override void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            spriteBatch.Draw(_pixel, new Rectangle(0, 0, Constants.InternalResolution.X, Constants.InternalResolution.Y), Color.Black * 0.7f);

            Vector2 titlePos = new Vector2(Constants.InternalResolution.X / 2, 80);
            DrawUtils.DrawTextScreen(spriteBatch, "LEVEL UP!", GameEngine.Assets.Fonts["captain_80"], titlePos, Constants.TitleYellow1);
            DrawUtils.DrawTextScreen(spriteBatch, "Choose an upgrade", _descFont, titlePos + new Vector2(0, 60), Color.LightGray);

            for (int i = 0; i < _options.Count; i++)
            {
                var opt = _options[i];
                var rect = _cardRects[i];
                bool isHovered = (i == _hoveredIndex);

                Rectangle drawRect = rect;
                if (isHovered)
                {
                    drawRect.Y -= 10;
                    spriteBatch.Draw(_pixel, new Rectangle(drawRect.X + 10, drawRect.Y + 10, drawRect.Width, drawRect.Height), Color.Black * 0.5f);
                }

                Color cardColor = isHovered ? Color.Lerp(Color.DarkSlateGray, opt.Color, 0.2f) : Color.DarkSlateGray;
                Color borderColor = isHovered ? opt.Color : Color.Gray;

                spriteBatch.Draw(_pixel, drawRect, cardColor);

                int borderThick = 4;
                DrawBorder(spriteBatch, drawRect, borderThick, borderColor);

                float centerX = drawRect.X + drawRect.Width / 2f;

                DrawUtils.DrawTextScreen(spriteBatch, opt.Title, _titleFont, new Vector2(centerX, drawRect.Y + 50), opt.Color);

                spriteBatch.Draw(_pixel, new Rectangle(drawRect.X + 20, drawRect.Y + 90, drawRect.Width - 40, 2), Color.White * 0.5f);

                string wrappedDesc = WrapText(_descFont, opt.Description, drawRect.Width - 30);
                DrawUtils.DrawTextScreen(spriteBatch, wrappedDesc, _descFont, new Vector2(centerX, drawRect.Y + 140), Color.White);

                if (isHovered)
                {
                    DrawUtils.DrawTextScreen(spriteBatch, "Click to Select", _descFont, new Vector2(centerX, drawRect.Y + drawRect.Height - 40), Color.Yellow * 0.8f);
                }
            }
        }

        private void DrawBorder(SpriteBatch sb, Rectangle rect, int thickness, Color color)
        {
            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y + rect.Height - thickness, rect.Width, thickness), color);            sb.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);            sb.Draw(_pixel, new Rectangle(rect.X + rect.Width - thickness, rect.Y, thickness, rect.Height), color);        }

        private string WrapText(SpriteFont font, string text, float maxLineWidth)
        {
            string[] words = text.Split(' ');
            string sb = "";
            float lineWidth = 0f;
            float spaceWidth = font.MeasureString(" ").X;

            foreach (string word in words)
            {
                Vector2 size = font.MeasureString(word);
                if (lineWidth + size.X < maxLineWidth)
                {
                    sb += word + " ";
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    sb += "\n" + word + " ";
                    lineWidth = size.X + spaceWidth;
                }
            }
            return sb;
        }
    }
}