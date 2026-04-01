using System.Collections.Generic;
using UnityEngine;
using Cards.Core;

namespace Cards.Zones.Layouts
{
    public class LineLayout : IZoneLayout
    {
        public bool SupportsIncrementalAdd => false;

        private Transform centerTransform;
        private float spacing;
        private Vector3 localOffsetAxis;

        public LineLayout(Transform center, float spacing, Vector3 axis)
        {
            this.centerTransform = center;
            this.spacing = spacing;
            this.localOffsetAxis = axis.normalized;
        }

        public void Arrange(IReadOnlyList<CardEntityView> cards, bool useAnimation = false)
        {
            if (centerTransform == null) return;

            float totalWidth = (cards.Count - 1) * spacing;
            float startOffset = -totalWidth / 2f;

            Quaternion targetRot = centerTransform.rotation;

            for (int i = 0; i < cards.Count; i++)
            {
                Vector3 localOffset = localOffsetAxis * (startOffset + i * spacing);
                Vector3 targetPosition = centerTransform.position + centerTransform.TransformDirection(localOffset);
                
                if (useAnimation)
                {
                    cards[i].MoveTo(targetPosition, targetRot, 0.3f);
                }
                else
                {
                    cards[i].transform.position = targetPosition;
                    cards[i].transform.rotation = targetRot;
                }
            }
        }

        public void OnCardAdded(CardEntityView card, int index, bool useAnimation = false)
        {
            // 对于线性布局，加牌通常需要整体重新排列，所以这里可以留空，
            // 依赖区域 (CardZone) 调用总的 Arrange 方法
        }
    }
}
