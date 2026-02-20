using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReignOfFear.Content.Systems.FearSystem.PlayerDebuffs;
using Terraria;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem.Assets.Visuals.Effects
{
    public class TraumaticStrikeEffect : ModSystem
    {
        private static Texture2D flashTexture;
        private static bool textureGenerated = false;

        public override void Unload()
        {
            flashTexture = null;
            textureGenerated = false;
        }

        private static Texture2D GenerateFlash(GraphicsDevice device, int width, int height)
        {
            Texture2D texture = new Texture2D(device, width, height);
            Color[] data = new Color[width * height];

            Color flashColor = new Color(60, 0, 0, 180);

            for (int i = 0; i < data.Length; i++)
            {
                data[i] = flashColor;
            }

            texture.SetData(data);
            return texture;
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;

            if (!player.HasBuff(ModContent.BuffType<TraumaticStrike>()))
                return;

            if (!textureGenerated)
            {
                flashTexture = GenerateFlash(Main.graphics.GraphicsDevice,
                    Main.screenWidth, Main.screenHeight);
                textureGenerated = true;
            }

            float pulseSpeed = 10f;
            float pulse = (float)System.Math.Sin(Main.GameUpdateCount / pulseSpeed) * 0.5f + 0.5f;

            float intensity = 0.2f + (pulse * 0.4f);

            spriteBatch.Draw(
                flashTexture,
                Vector2.Zero,
                null,
                Color.White * intensity,
                0f,
                Vector2.Zero,
                1f,
                SpriteEffects.None,
                0f
            );
        }
    }
}
