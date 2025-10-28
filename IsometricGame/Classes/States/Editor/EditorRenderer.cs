
using IsometricGame.Map;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace IsometricGame.States.Editor
{
	public class EditorRenderer
	{
		private SpriteFont _font;
		private Texture2D _pixelTexture;
		private Texture2D _triggerIconTexture;
		public EditorRenderer(SpriteFont font, Texture2D pixelTexture, Texture2D triggerIconTexture = null)
		{
			_font = font;
			_pixelTexture = pixelTexture ?? throw new ArgumentNullException(nameof(pixelTexture));			_triggerIconTexture = triggerIconTexture ?? _pixelTexture;		}
		public void DrawEditorWorld(SpriteBatch spriteBatch, EditorState editorState)
		{
			int currentZLevel = editorState.GetCurrentZLevel();			var mapSprites = editorState.GetMapSprites();			var triggers = editorState.GetCurrentMapTriggers();			var selectedTrigger = editorState.GetSelectedTrigger();			EditorMode currentMode = editorState.GetCurrentMode();			Vector3 cursorWorldPos = editorState.GetCursorWorldPos();			TileMappingEntry selectedTileInfo = editorState.GetSelectedTileInfo();			int selectedTileId = editorState.GetSelectedTileId();			if (mapSprites != null)
			{
				foreach (var sprite in mapSprites.Values.Where(s => s != null)
													.OrderBy(s => s.WorldPosition.Z)
													.ThenBy(s => IsoMath.GetDepth(s.WorldPosition)))
				{
					float alpha = (Math.Abs(sprite.WorldPosition.Z - currentZLevel) > 0.1f) ? 0.3f : 1.0f;
					spriteBatch.Draw(sprite.Texture, sprite.ScreenPosition, null, Color.White * alpha, 0f, sprite.Origin, 1f, SpriteEffects.None, IsoMath.GetDepth(sprite.WorldPosition));
				}
			}
			if (currentMode == EditorMode.Triggers && triggers != null)
			{
				foreach (var trigger in triggers)
				{
					float alpha = (Math.Abs(trigger.Position.Z - currentZLevel) < 0.1f) ? 1.0f : 0.2f;
					Color color = (trigger == selectedTrigger) ? Color.LimeGreen : Color.Magenta;
					DrawTriggerRepresentation(spriteBatch, trigger, color * alpha);
				}
			}
			DrawCursorIndicator(spriteBatch, cursorWorldPos, currentMode, selectedTileId, selectedTileInfo);
		}
		public void DrawEditorUI(SpriteBatch spriteBatch, EditorState editorState)
		{
			string currentMapFileName = editorState.GetCurrentMapFileName();
			int currentZLevel = editorState.GetCurrentZLevel();
			Vector3 cursorWorldPos = editorState.GetCursorWorldPos();
			EditorMode currentMode = editorState.GetCurrentMode();
			int selectedTileId = editorState.GetSelectedTileId();
			TileMappingEntry selectedTileInfo = editorState.GetSelectedTileInfo();
			MapTrigger selectedTrigger = editorState.GetSelectedTrigger();


			string modeString = $"MODE: {currentMode} (TAB to switch)";
			string tileName = selectedTileInfo?.AssetName ?? "N/A";
			Color tileColor = selectedTileInfo != null ? Color.Cyan : Color.Gray;
			Vector2 textPos = new Vector2(15, 10);
			spriteBatch.DrawString(_font, $"File: {currentMapFileName}", textPos, Color.White); textPos.Y += 30;
			spriteBatch.DrawString(_font, modeString, textPos, Color.Orange); textPos.Y += 30;
			spriteBatch.DrawString(_font, $"Z-Level: {currentZLevel} (Use 0-9)", textPos, Color.White); textPos.Y += 30;
			spriteBatch.DrawString(_font, $"Cursor: {cursorWorldPos.X}, {cursorWorldPos.Y}, {cursorWorldPos.Z}", textPos, Color.Yellow); textPos.Y += 40;
			if (currentMode == EditorMode.Tiles)
			{
				spriteBatch.DrawString(_font, $"Tile: [{selectedTileId}] {tileName}", textPos, tileColor); textPos.Y += 30;
				spriteBatch.DrawString(_font, $"Controls: LMB=Place | RMB=Erase | Palette: Q/E,PgUp/Dn", textPos, Color.LightGray); textPos.Y += 30;
			}
			else			{
				spriteBatch.DrawString(_font, $"Selected: {(selectedTrigger == null ? "None" : selectedTrigger.Id ?? "(no ID)")}", textPos, Color.LimeGreen); textPos.Y += 30;
				spriteBatch.DrawString(_font, $"Controls: LMB=Select | RMB=Add | DEL=Remove", textPos, Color.LightGray); textPos.Y += 30;

				if (selectedTrigger != null)
				{
					spriteBatch.DrawString(_font, $" -> Target: {selectedTrigger.TargetMap}", textPos, Color.Aqua); textPos.Y += 30;
					spriteBatch.DrawString(_font, $" -> TargetPos: {selectedTrigger.TargetPosition}", textPos, Color.Aqua); textPos.Y += 30;
					spriteBatch.DrawString(_font, $" -> Radius: {selectedTrigger.Radius}", textPos, Color.Aqua); textPos.Y += 30;
				}
			}
			textPos.Y += 10;
			spriteBatch.DrawString(_font, $"Common: Pan:Arrows | Zoom: +/- or Wheel | Save:Ctrl+S Load:Ctrl+L New:Ctrl+N | Exit:Esc", textPos, Color.DarkGray);
			if (currentMode == EditorMode.Tiles && selectedTileInfo != null && GameEngine.Assets.Images.TryGetValue(selectedTileInfo.AssetName, out var tex))
			{
				Vector2 previewPos = new Vector2(15, Constants.InternalResolution.Y - tex.Height - 15);
				Rectangle bgRect = new Rectangle((int)previewPos.X - 5, (int)previewPos.Y - 5, tex.Width + 10, tex.Height + 10);
				if (_pixelTexture != null) spriteBatch.Draw(_pixelTexture, bgRect, Color.DarkSlateGray * 0.9f);
				spriteBatch.Draw(tex, previewPos, Color.White);
				DrawUtils.DrawTextScreen(spriteBatch, "Selected (Q/E)", _font, previewPos + new Vector2(tex.Width / 2f, -25), Color.White, 0f);
			}
		}
		private void DrawTriggerRepresentation(SpriteBatch spriteBatch, MapTrigger trigger, Color color)
		{
			Vector2 screenPos = IsoMath.WorldToScreen(trigger.Position);
			int size = (int)(Constants.IsoTileSize.X * 0.6f);
			Rectangle rect = new Rectangle((int)(screenPos.X - size / 2f), (int)(screenPos.Y - size / 2f), size, size);

			if (_triggerIconTexture != null)
			{
				spriteBatch.Draw(_triggerIconTexture, rect, null, color, 0f, Vector2.Zero, SpriteEffects.None, 0.0f);
			}
		}

		private void DrawCursorIndicator(SpriteBatch spriteBatch, Vector3 cursorWorldPos, EditorMode currentMode, int selectedTileId, TileMappingEntry selectedTileInfo)
		{
			Texture2D cursorTexture = null;
			bool usePixelPlaceholder = false;
			Color cursorColor = Color.Yellow * 0.6f;

			if (currentMode == EditorMode.Tiles)
			{
				if (selectedTileId == 0) { cursorTexture = _pixelTexture; usePixelPlaceholder = true; cursorColor = Color.Red * 0.5f; }
				else if (selectedTileInfo != null) { GameEngine.Assets.Images.TryGetValue(selectedTileInfo.AssetName, out cursorTexture); }

				if (cursorTexture == null) { cursorTexture = _pixelTexture; usePixelPlaceholder = true; cursorColor = Color.Orange * 0.5f; }
			}
			else { cursorTexture = _pixelTexture; usePixelPlaceholder = true; cursorColor = Color.Cyan * 0.5f; }

			if (cursorTexture != null)
			{
				Vector2 screenPos = IsoMath.WorldToScreen(cursorWorldPos);
				if (usePixelPlaceholder)
				{
					int squareSize = (int)(Math.Min(Constants.IsoTileSize.X, Constants.IsoTileSize.Y) * 0.8f);
					Rectangle squareRect = new Rectangle((int)(screenPos.X - squareSize / 2f), (int)(screenPos.Y - squareSize / 2f), squareSize, squareSize);
					spriteBatch.Draw(cursorTexture, squareRect, null, cursorColor, 0f, Vector2.Zero, SpriteEffects.None, 0.0f);
				}
				else
				{
					Vector2 origin = new Vector2(cursorTexture.Width / 2f, cursorTexture.Height);
					spriteBatch.Draw(cursorTexture, new Vector2(MathF.Round(screenPos.X), MathF.Round(screenPos.Y)), null, cursorColor, 0f, origin, 1f, SpriteEffects.None, 0.0f);
				}
			}
			else { Debug.WriteLine("Erro crítico: Textura do cursor e _pixelTexture são nulos em DrawCursorIndicator!"); }
		}
	}
}