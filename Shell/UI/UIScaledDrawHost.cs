using System;
using Terraria.UI;



namespace EvenMoreOverpoweredJourney.Shell.UI

{

    /// <summary>以逻辑尺寸构建子树，外框与内容按 <see cref="Scale"/> 等比缩小。</summary>

    internal sealed class UIScaledDrawHost : UIElement

    {

        public float Scale { get; }

        public float LogicalWidth { get; private set; }

        public float LogicalHeight { get; private set; }

        public readonly UIElement Content;



        public UIScaledDrawHost(UIElement content, float logicalWidth, float logicalHeight, float scale)

        {

            Content = content;

            Scale = scale;

            SetLogicalSize(logicalWidth, logicalHeight);

            Append(Content);

        }



        public void SetLogicalSize(float logicalWidth, float logicalHeight)

        {

            LogicalWidth = Math.Max(1f, logicalWidth);

            LogicalHeight = Math.Max(1f, logicalHeight);

            float w = LogicalWidth * Scale;

            float h = LogicalHeight * Scale;

            Width.Set(w, 0f);

            Height.Set(h, 0f);

            Content.Left.Set(0f, 0f);

            Content.Top.Set(0f, 0f);

            Content.Width.Set(w, 0f);

            Content.Height.Set(h, 0f);

        }

    }

}


