using Terraria.Localization;
using EvenMoreOverpoweredJourney.FurnitureBlueprint;

namespace EvenMoreOverpoweredJourney.Core.Localization
{
    internal static class EOPJText
    {
        private const string Root = "Mods.EvenMoreOverpoweredJourney.";

        public static string UI(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            if (key.StartsWith(Root, System.StringComparison.Ordinal))
                return Language.GetTextValue(key);

            return Language.GetTextValue(Root + "UI." + key);
        }

        public static string UIFormat(string key, params object[] args)
        {
            if (key.StartsWith(Root, System.StringComparison.Ordinal))
                return Language.GetTextValue(key, args);

            return Language.GetTextValue(Root + "UI." + key, args);
        }

        /// <summary>键未加载时 Language 会返回完整键路径，用回退文案避免 UI 显示 Mods.*。</summary>
        public static string UIOr(string key, string fallback)
        {
            string value = UI(key);
            if (value.StartsWith(Root, System.StringComparison.Ordinal))
                return fallback;
            return value;
        }

        /// <summary>家具蓝图 UI 文案（与 hjson <c>UI.Blueprint.*</c> 对应）。</summary>
        public static string BlueprintOr(string key, string fallback) => UIOr("Blueprint." + key, fallback);

        public static string SlotLabel(FurnitureSlotKind kind) =>
            UIOr("Blueprint.Slot." + kind, kind switch
            {
                FurnitureSlotKind.Block => "方块",
                FurnitureSlotKind.Wall => "墙壁",
                FurnitureSlotKind.Bathtub => "浴缸",
                FurnitureSlotKind.Bed => "床铺",
                FurnitureSlotKind.Bookcase => "书柜",
                FurnitureSlotKind.Candelabra => "烛台",
                FurnitureSlotKind.Candle => "蜡烛",
                FurnitureSlotKind.Chandelier => "吊灯",
                FurnitureSlotKind.Chair => "椅子",
                FurnitureSlotKind.Chest => "箱子",
                FurnitureSlotKind.Clock => "挂钟",
                FurnitureSlotKind.Door => "房门",
                FurnitureSlotKind.Dresser => "衣柜",
                FurnitureSlotKind.Lamp => "台灯",
                FurnitureSlotKind.Lantern => "灯笼",
                FurnitureSlotKind.Piano => "钢琴",
                FurnitureSlotKind.Platform => "平台",
                FurnitureSlotKind.Sink => "水槽",
                FurnitureSlotKind.Sofa => "沙发",
                FurnitureSlotKind.Table => "桌子",
                FurnitureSlotKind.Toilet => "马桶",
                FurnitureSlotKind.Workbench => "工台",
                _ => kind.ToString()
            });

        public static string RecipeEnv(string key) => Language.GetTextValue(Root + "RecipeEnv." + key);
    }
}
