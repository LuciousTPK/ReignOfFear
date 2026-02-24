using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ReignOfFear.Content.Systems.FearSystem.Assets.Visuals.Effects
{
    /// <summary>
    /// Basic, temporary effect for the Terror Radius phobia debuff
    /// This is still a WIP
    /// </summary>

    public class TerrorRadiusEffect : ModSystem
    {
        private static Texture2D vignetteTexture;
        private static bool textureGenerated = false;

        public override void Unload()
        {
            vignetteTexture = null;
            textureGenerated = false;
        }

        private static Texture2D GenerateVignette(GraphicsDevice device, int width, int height)
        {
            Texture2D texture = new Texture2D(device, width, height);
            Color[] data = new Color[width * height];

            Vector2 center = new Vector2(width / 2f, height / 2f);
            float maxDistance = Vector2.Distance(Vector2.Zero, center);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);

                    float normalizedDistance = distance / maxDistance;

                    float alpha = MathHelper.Clamp((float)System.Math.Pow(normalizedDistance, 2), 0f, 1f);

                    data[y * width + x] = new Color(20, 0, 0, (int)(alpha * 255 * 0.6f));
                }
            }

            texture.SetData(data);
            return texture;
        }

        public static float GetTerrorIntensity(Player player)
        {
            float closestDistance = float.MaxValue;
            bool foundZombie = false;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active) continue;

                if (PhobiaData.NPCPhobiaMap.TryGetValue(npc.type, out var phobias))
                {
                    if (phobias.Contains(PhobiaID.Kinemortophobia))
                    {
                        float distance = Vector2.Distance(player.Center, npc.Center) / 16f;
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            foundZombie = true;
                        }
                    }
                }
            }

            if (!foundZombie) return 0f;

            if (closestDistance >= 50) return 0f;
            if (closestDistance <= 25) return 1f;

            return 1f - (closestDistance - 25f) / 25f;
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            Player player = Main.LocalPlayer;
            FearSystemPlayer modPlayer = player.GetModPlayer<FearSystemPlayer>();

            if (!modPlayer.HasDebuff(PhobiaID.Kinemortophobia, PhobiaDebuff.PhobiaDebuffID.TerrorRadius))
                return;

            if (!textureGenerated)
            {
                vignetteTexture = GenerateVignette(Main.graphics.GraphicsDevice,
                    Main.screenWidth, Main.screenHeight);
                textureGenerated = true;
            }

            float intensity = GetTerrorIntensity(player);
            if (intensity <= 0f) return;

            spriteBatch.Draw(
                vignetteTexture,
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
