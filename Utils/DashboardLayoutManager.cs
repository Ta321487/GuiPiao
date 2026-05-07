using GuiPiao.Model;
using System;
using System.Collections.Generic;

namespace GuiPiao.Utils
{
    /// <summary>
    /// 仪表盘布局管理器 - 处理不同布局类型的卡片位置计算
    /// </summary>
    public static class DashboardLayoutManager
    {
        /// <summary>
        /// 根据布局类型应用卡片位置
        /// </summary>
        public static void ApplyLayout(IList<DashboardCard> cards, LayoutType layoutType)
        {
            if (cards == null || cards.Count == 0) return;

            switch (layoutType)
            {
                case LayoutType.ThreeColumn:
                    ApplyThreeColumnLayout(cards);
                    break;
                case LayoutType.TwoColumn:
                    ApplyTwoColumnLayout(cards);
                    break;
                case LayoutType.LeftOneRightTwo:
                    ApplyLeftOneRightTwoLayout(cards);
                    break;
                case LayoutType.TopOneBottomTwo:
                    ApplyTopOneBottomTwoLayout(cards);
                    break;

                default:
                    ApplyTwoColumnLayout(cards);
                    break;
            }
        }

        /// <summary>
        /// 三列等宽布局
        /// </summary>
        private static void ApplyThreeColumnLayout(IList<DashboardCard> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].GridRow = i / 3;
                cards[i].GridColumn = i % 3;
                cards[i].GridRowSpan = 1;
                cards[i].GridColumnSpan = 1;
            }
        }

        /// <summary>
        /// 两列等宽布局
        /// </summary>
        private static void ApplyTwoColumnLayout(IList<DashboardCard> cards)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                cards[i].GridRow = i / 2;
                cards[i].GridColumn = i % 2;
                cards[i].GridRowSpan = 1;
                cards[i].GridColumnSpan = 1;
            }
        }

        /// <summary>
        /// 左一右二布局
        /// 左侧一个大卡片，右侧上下两个小卡片
        /// </summary>
        private static void ApplyLeftOneRightTwoLayout(IList<DashboardCard> cards)
        {
            if (cards.Count >= 1)
            {
                // 第一个卡片：左侧，占2行1列
                cards[0].GridRow = 0;
                cards[0].GridColumn = 0;
                cards[0].GridRowSpan = 2;
                cards[0].GridColumnSpan = 1;
            }

            if (cards.Count >= 2)
            {
                // 第二个卡片：右上
                cards[1].GridRow = 0;
                cards[1].GridColumn = 1;
                cards[1].GridRowSpan = 1;
                cards[1].GridColumnSpan = 1;
            }

            if (cards.Count >= 3)
            {
                // 第三个卡片：右下
                cards[2].GridRow = 1;
                cards[2].GridColumn = 1;
                cards[2].GridRowSpan = 1;
                cards[2].GridColumnSpan = 1;
            }

            // 剩余卡片按两列布局继续排列
            for (int i = 3; i < cards.Count; i++)
            {
                int remainingIndex = i - 3;
                cards[i].GridRow = 2 + remainingIndex / 2;
                cards[i].GridColumn = remainingIndex % 2;
                cards[i].GridRowSpan = 1;
                cards[i].GridColumnSpan = 1;
            }
        }

        /// <summary>
        /// 上一下二布局
        /// 上方一个大卡片，下方左右两个小卡片
        /// </summary>
        private static void ApplyTopOneBottomTwoLayout(IList<DashboardCard> cards)
        {
            if (cards.Count >= 1)
            {
                // 第一个卡片：上方，占1行2列
                cards[0].GridRow = 0;
                cards[0].GridColumn = 0;
                cards[0].GridRowSpan = 1;
                cards[0].GridColumnSpan = 2;
            }

            if (cards.Count >= 2)
            {
                // 第二个卡片：左下
                cards[1].GridRow = 1;
                cards[1].GridColumn = 0;
                cards[1].GridRowSpan = 1;
                cards[1].GridColumnSpan = 1;
            }

            if (cards.Count >= 3)
            {
                // 第三个卡片：右下
                cards[2].GridRow = 1;
                cards[2].GridColumn = 1;
                cards[2].GridRowSpan = 1;
                cards[2].GridColumnSpan = 1;
            }

            // 剩余卡片按两列布局继续排列
            for (int i = 3; i < cards.Count; i++)
            {
                int remainingIndex = i - 3;
                cards[i].GridRow = 2 + remainingIndex / 2;
                cards[i].GridColumn = remainingIndex % 2;
                cards[i].GridRowSpan = 1;
                cards[i].GridColumnSpan = 1;
            }
        }

        /// <summary>
        /// 获取布局所需的行数
        /// </summary>
        public static int GetRequiredRows(IList<DashboardCard> cards, LayoutType layoutType)
        {
            if (cards == null || cards.Count == 0) return 1;

            return layoutType switch
            {
                LayoutType.ThreeColumn => (int)Math.Ceiling(cards.Count / 3.0),
                LayoutType.TwoColumn => (int)Math.Ceiling(cards.Count / 2.0),
                LayoutType.LeftOneRightTwo => Math.Max(2, 2 + (int)Math.Ceiling((cards.Count - 3) / 2.0)),
                LayoutType.TopOneBottomTwo => Math.Max(2, 2 + (int)Math.Ceiling((cards.Count - 3) / 2.0)),
                _ => (int)Math.Ceiling(cards.Count / 2.0)
            };
        }

        /// <summary>
        /// 获取布局所需的列数
        /// </summary>
        public static int GetRequiredColumns(LayoutType layoutType)
        {
            return layoutType switch
            {
                LayoutType.ThreeColumn => 3,
                LayoutType.TwoColumn => 2,
                LayoutType.LeftOneRightTwo => 2,
                LayoutType.TopOneBottomTwo => 2,
                _ => 2
            };
        }
    }
}
