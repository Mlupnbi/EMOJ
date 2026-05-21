using System;
using EvenMoreOverpoweredJourney.Integration.ImproveGame;
using EvenMoreOverpoweredJourney.Integration.Session;
using EvenMoreOverpoweredJourney.Integration.Browser;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace EvenMoreOverpoweredJourney
{
    public class EvenMoreOverpoweredJourney : Mod
    {
        /// <summary>与 build.txt 同步；悬停版本以打包元数据为准，此处作运行时兜底。</summary>
        public override Version Version => new Version(0, 4, 70);

        /// <summary>未由 ImproveGame 提供额外栏位时，由 EMOJ 配置贡献（与 ImproveGame 叠加会重复，故委托时为 0）。</summary>
        public override uint ExtraPlayerBuffSlots =>
            (uint)BuffInfrastructureSettings.GetOwnExtraBuffSlotsContribution();

        /// <summary>物品中枢模组筛选：原版小图标（使用模组内 Assets/UI/ModBrandVanilla.png）。</summary>
        public const string ModBrandVanilla = "Assets/UI/ModBrandVanilla";

        /// <summary>物品中枢模组筛选：tModLoader 小图标（使用模组内 Assets/UI/ModBrandTModLoader.png）。</summary>
        public const string ModBrandTModLoader = "Assets/UI/ModBrandTModLoader";

        /// <summary>主窗右下角拉伸手柄图标（Assets/UI/Handle.png，18×18）。</summary>
        public const string ResizeHandleCursor = "Assets/UI/Handle";

        /// <summary>主界面标签页图标路径（同名 .png 自动引用；缺省时显示文字）。</summary>
        public const string TabIconResearch = "Assets/UI/TabResearch";
        public const string TabIconBuff = "Assets/UI/TabBuff";
        public const string TabIconStorage = "Assets/UI/TabStorage";
        public const string TabIconBestiary = "Assets/UI/TabBestiary";

        /// <summary>物品中枢「筛选」按钮图标（30×30 PNG，放于 Assets/UI/ItemHubFilterButton.png）。</summary>
        public const string ItemHubFilterButton = "Assets/UI/ItemHubFilterButton";

        /// <summary>升序排序图标（Assets/UI/ItemHubSortOrderAsc.png）。</summary>
        public const string ItemHubSortOrderAsc = "Assets/UI/ItemHubSortOrderAsc";

        /// <summary>降序排序图标（Assets/UI/ItemHubSortOrderDesc.png）。</summary>
        public const string ItemHubSortOrderDesc = "Assets/UI/ItemHubSortOrderDesc";

        /// <summary>降序图标备用路径（文件名大小写不一致时可放大图）。</summary>
        public const string ItemHubSortOrderDescAlt = "Assets/UI/ItemHubSortOrderDESC";

        /// <summary>卡片视图图标（Assets/UI/ItemHubViewCard.png）。</summary>
        public const string ItemHubViewCard = "Assets/UI/ItemHubViewCard";

        /// <summary>列表视图图标（Assets/UI/ItemHubViewList.png）。</summary>
        public const string ItemHubViewList = "Assets/UI/ItemHubViewList";

        /// <summary>可选：物品中枢顶部四个分类按钮贴图路径（当前未绘制；有 PNG 时可做 Strip 图片按钮）。</summary>
        public const string ItemHubBtnCategory = "Assets/UI/ItemHubBtnCategory";
        public const string ItemHubBtnMod = "Assets/UI/ItemHubBtnMod";
        public const string ItemHubBtnSort = "Assets/UI/ItemHubBtnSort";
        public const string ItemHubBtnViewMode = "Assets/UI/ItemHubBtnViewMode";

        public static ModKeybind OpenResearchPanelKey { get; private set; }
        public static ModKeybind OpenBuffPanelKey { get; private set; }
        public static ModKeybind OpenItemHubPanelKey { get; private set; }
        public static ModKeybind QuickItemQueryKey { get; private set; }
        public static ModKeybind OpenBestiaryPanelKey { get; private set; }

        public override void Load()
        {
            if (!Main.dedServ)
            {
                try
                {
                    if (HasAsset(ResizeHandleCursor))
                        Assets.Request<Texture2D>(ResizeHandleCursor, ReLogic.Content.AssetRequestMode.ImmediateLoad);
                }
                catch
                {
                    // 开发期贴图异常时不阻断加载；运行时用手柄 fallback
                }
            }

            OpenResearchPanelKey = KeybindLoader.RegisterKeybind(this, "OpenResearchPanel", "R");
            OpenBuffPanelKey = KeybindLoader.RegisterKeybind(this, "OpenBuffPanel", "T");
            OpenItemHubPanelKey = KeybindLoader.RegisterKeybind(this, "OpenItemHubPanel", "Y");
            OpenBestiaryPanelKey = KeybindLoader.RegisterKeybind(this, "OpenBestiaryPanel", "U");
            QuickItemQueryKey = KeybindLoader.RegisterKeybind(this, "QuickItemQuery", "Mouse5");
        }

        public override void Unload()
        {
            OpenResearchPanelKey = null;
            OpenBuffPanelKey = null;
            OpenItemHubPanelKey = null;
            OpenBestiaryPanelKey = null;
            QuickItemQueryKey = null;
        }
    }
}
