using System;
using UnityEngine;

namespace Antchemy
{
    // Picture previews are drawn at runtime from the level files, one page of 100 at a time.
    public sealed class LevelThumbnailAtlas : IDisposable
    {
        const int Columns = 10, PerPage = 100, Cell = 24;
        Texture2D texture;
        int currentPage;

        public Texture2D GetPage(int page)
        {
            if (texture != null && currentPage == page) return texture;
            int size = Columns * Cell;
            var pixels = new Color32[size * size];
            int first = (page - 1) * PerPage + 1;
            int total = GameManager.Instance != null ? GameManager.Instance.LevelCount : 0;
            for (int i = 0; i < PerPage; i++)
            {
                int level = first + i;
                if (total > 0 && level > total) break;
                var asset = Resources.Load<TextAsset>("Levels/Level_" + level);
                if (asset == null) continue;
                var data = JsonUtility.FromJson<LevelData>(asset.text);
                Resources.UnloadAsset(asset);
                if (data == null || data.SizeX <= 0 || data.SizeY <= 0 || data.ColorData == null) continue;
                Draw(pixels, size, (i % Columns) * Cell, (Columns - 1 - i / Columns) * Cell, data);
            }
            var next = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Puzzle thumbnails page " + page,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            next.SetPixels32(pixels);
            next.Apply(false, false);
            Dispose(); texture = next; currentPage = page;
            return texture;
        }

        static void Draw(Color32[] pixels, int size, int originX, int originY, LevelData data)
        {
            float scale = Mathf.Min((Cell - 2f) / data.SizeX, (Cell - 2f) / data.SizeY);
            float offsetX = (Cell - data.SizeX * scale) * .5f, offsetY = (Cell - data.SizeY * scale) * .5f;
            int block = Mathf.Max(1, Mathf.CeilToInt(scale));
            foreach (var group in data.ColorData)
            {
                Color32 color = ColorPalette.GetColor(group.ColorIndex);
                foreach (int id in group.CubeDataIds)
                {
                    int tx = id / data.SizeY, ty = id % data.SizeY;
                    int px = originX + Mathf.FloorToInt(offsetX + tx * scale);
                    int py = originY + Mathf.FloorToInt(offsetY + ty * scale);
                    for (int dx = 0; dx < block; dx++)
                        for (int dy = 0; dy < block; dy++)
                        {
                            int x = px + dx, y = py + dy;
                            if (x < originX || y < originY || x >= originX + Cell || y >= originY + Cell) continue;
                            pixels[y * size + x] = color;
                        }
                }
            }
        }

        public static Rect GetUV(int level)
        {
            int index = (level - 1) % PerPage;
            return new Rect((index % Columns) / (float)Columns, (Columns - 1 - index / Columns) / (float)Columns, 1f / Columns, 1f / Columns);
        }

        public void Dispose()
        {
            if (texture != null) UnityEngine.Object.Destroy(texture);
            texture = null; currentPage = 0;
        }
    }
}
