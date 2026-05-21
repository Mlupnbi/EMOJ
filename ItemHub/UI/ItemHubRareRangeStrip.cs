using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent;
using Terraria.UI;
using EvenMoreOverpoweredJourney;
using EvenMoreOverpoweredJourney.Shell.UI;

namespace EvenMoreOverpoweredJourney.ItemHub.UI
{
    /// <summary>ϡ�ж�˫�˻�����Ĭ�ϸ���ԭ�� rare ��λ��������������չ����ǰ����ģ�����ʵ��Χ��</summary>
    public sealed class ItemHubRareRangeStrip : UIElement
    {
        public const int DefaultSliderMin = -11;
        public const int DefaultSliderMax = 11;

        public static int SliderMin { get; private set; } = DefaultSliderMin;
        public static int SliderMax { get; private set; } = DefaultSliderMax;
        private static readonly List<int> ValidRarities = new List<int>();

        private readonly OPJourneyUI _shell;
        private int _drag;

        public ItemHubRareRangeStrip(OPJourneyUI shell)
        {
            _shell = shell;
            Width.Set(0, 1f);
            Height.Set(36, 0);
        }

        public static void ConfigureDynamicBounds(int minRare, int maxRare)
        {
            ConfigureValidRarities(new[] { minRare, maxRare });
        }

        public static void ConfigureValidRarities(IEnumerable<int> rareValues)
        {
            ValidRarities.Clear();
            if (rareValues != null)
                ValidRarities.AddRange(rareValues.Distinct().OrderBy(x => x));

            if (ValidRarities.Count == 0)
            {
                for (int i = DefaultSliderMin; i <= DefaultSliderMax; i++)
                    ValidRarities.Add(i);
            }

            SliderMin = ValidRarities[0];
            SliderMax = ValidRarities[ValidRarities.Count - 1];
        }

        private static int IndexOfNearest(int value)
        {
            if (ValidRarities.Count == 0)
                ConfigureValidRarities(null);

            int idx = ValidRarities.BinarySearch(value);
            if (idx >= 0)
                return idx;

            idx = ~idx;
            if (idx <= 0)
                return 0;
            if (idx >= ValidRarities.Count)
                return ValidRarities.Count - 1;

            int below = ValidRarities[idx - 1];
            int above = ValidRarities[idx];
            return value - below <= above - value ? idx - 1 : idx;
        }

        private static float NormValue(int v)
        {
            if (ValidRarities.Count <= 1)
                return 0f;
            return IndexOfNearest(v) / (float)(ValidRarities.Count - 1);
        }

        private static int ValueFromT(float t)
        {
            if (ValidRarities.Count == 0)
                ConfigureValidRarities(null);
            t = MathHelper.Clamp(t, 0f, 1f);
            int idx = (int)Math.Round(t * (ValidRarities.Count - 1));
            idx = Math.Clamp(idx, 0, ValidRarities.Count - 1);
            return ValidRarities[idx];
        }

        public static int NormalizeRarityValue(int value) => ValueFromT(NormValue(value));

        private static void ClampPair(ref int minR, ref int maxR)
        {
            minR = NormalizeRarityValue(minR);
            maxR = NormalizeRarityValue(maxR);
            if (minR > maxR)
                (minR, maxR) = (maxR, minR);
        }

        public override void Update(GameTime gameTime)
        {
            _shell.ItemHubSecondary.NormalizeRareFilterBounds();

            CalculatedStyle d = GetDimensions();
            float x0 = d.X + 12f;
            float x1 = d.X + d.Width - 12f;
            float trW = x1 - x0;

            if (!Main.mouseLeft)
                _drag = 0;
            else if (IsMouseHovering && trW >= 24f)
            {
                float pxMin = x0 + NormValue(_shell.ItemHubSecondary.RareFilterMin) * trW;
                float pxMax = x0 + NormValue(_shell.ItemHubSecondary.RareFilterMax) * trW;
                float mx = Main.MouseScreen.X;
                if (_drag == 0)
                {
                    float dL = Math.Abs(mx - pxMin);
                    float dR = Math.Abs(mx - pxMax);
                    if (dL <= 14f && dL <= dR)
                        _drag = 1;
                    else if (dR <= 14f)
                        _drag = 2;
                }

                if (_drag != 0)
                {
                    float t = (mx - x0) / trW;
                    int idxVal = ValueFromT(t);
                    int oldMin = _shell.ItemHubSecondary.RareFilterMin;
                    int oldMax = _shell.ItemHubSecondary.RareFilterMax;
                    if (_drag == 1)
                    {
                        if (idxVal > oldMax)
                            idxVal = oldMax;
                        _shell.ItemHubSecondary.RareFilterMin = idxVal;
                        _shell.ItemHubSecondary.RareFilterMax = oldMax;
                    }
                    else
                    {
                        if (idxVal < oldMin)
                            idxVal = oldMin;
                        _shell.ItemHubSecondary.RareFilterMax = idxVal;
                        _shell.ItemHubSecondary.RareFilterMin = oldMin;
                    }

                    ClampPair(ref _shell.ItemHubSecondary.RareFilterMin, ref _shell.ItemHubSecondary.RareFilterMax);
                    if (oldMin != _shell.ItemHubSecondary.RareFilterMin || oldMax != _shell.ItemHubSecondary.RareFilterMax)
                    {
                        _shell.ItemHubSecondary.RareFilterCustomized = true;
                        _shell.NotifyItemHubFiltersChanged();
                    }
                }
            }

            base.Update(gameTime);
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
            _shell.ItemHubSecondary.NormalizeRareFilterBounds();
            CalculatedStyle d = GetDimensions();
            float x0 = d.X + 12f;
            float x1 = d.X + d.Width - 12f;
            float y = d.Y + 24f;
            float trW = x1 - x0;
            Texture2D px = TextureAssets.MagicPixel.Value;
            spriteBatch.Draw(px, new Rectangle((int)x0, (int)(y - 2), (int)trW, 4), Color.Gray * 0.85f);

            float pxMin = x0 + NormValue(_shell.ItemHubSecondary.RareFilterMin) * trW;
            float pxMax = x0 + NormValue(_shell.ItemHubSecondary.RareFilterMax) * trW;
            foreach (float cx in new[] { pxMin, pxMax })
                spriteBatch.Draw(px, new Rectangle((int)(cx - 5), (int)(y - 8), 10, 16), Color.Gold);

            int lo = _shell.ItemHubSecondary.RareFilterMin;
            int hi = _shell.ItemHubSecondary.RareFilterMax;
            string hiText = hi >= SliderMax ? $"{hi}+" : hi.ToString();
            string line = EOPJText.UIFormat("ItemHubRareSliderFmt", lo, hiText);
            const float textScale = 0.68f * 1.5f;
            Utils.DrawBorderString(spriteBatch, line, new Vector2(d.X + 6, d.Y - 1f), Color.LightGray, textScale);
        }
    }
}
