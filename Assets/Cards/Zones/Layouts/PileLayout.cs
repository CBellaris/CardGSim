using System.Collections.Generic;
using UnityEngine;
using Cards.Core;

namespace Cards.Zones.Layouts
{
    public class PileLayout : IZoneLayout
    {
        public bool SupportsIncrementalAdd => true;

        private Transform baseTransform;
        private float stackZOffset;

        public PileLayout(Transform transform, float offset)
        {
            this.baseTransform = transform;
            this.stackZOffset = offset;
        }

        public void Arrange(IReadOnlyList<CardEntityView> cards, bool useAnimation = false)
        {
            if (baseTransform == null) return;

            for (int i = 0; i < cards.Count; i++)
            {
                Vector3 offset = new Vector3(0, 0, stackZOffset * i);
                Vector3 targetPosition = baseTransform.position + baseTransform.TransformDirection(offset);
                
                if (useAnimation)
                {
                    cards[i].MoveTo(targetPosition, baseTransform.rotation, 0.3f);
                }
                else
                {
                    cards[i].transform.position = targetPosition;
                    cards[i].transform.rotation = baseTransform.rotation;
                }
            }
        }

        public void OnCardAdded(CardEntityView card, int index, bool useAnimation = false)
        {
            if (baseTransform == null) return;

            Vector3 offset = new Vector3(0, 0, stackZOffset * index);
            Vector3 targetPosition = baseTransform.position + baseTransform.TransformDirection(offset);
            
            if (useAnimation)
            {
                card.MoveTo(targetPosition, baseTransform.rotation, 0.3f);
            }
            else
            {
                card.transform.position = targetPosition;
                card.transform.rotation = baseTransform.rotation;
            }
        }
    }
}
