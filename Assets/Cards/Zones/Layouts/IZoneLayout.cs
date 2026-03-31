using System.Collections.Generic;
using UnityEngine;
using Cards.Core;

namespace Cards.Zones.Layouts
{
    public interface IZoneLayout
    {
        /// <summary>
        /// Whether this layout can handle a single card addition without re-arranging the entire zone.
        /// If true, OnCardAdded handles individual additions; if false, Arrange is called instead.
        /// </summary>
        bool SupportsIncrementalAdd { get; }

        void Arrange(List<CardEntity> cards, bool useAnimation = false);
        
        void OnCardAdded(CardEntity card, int index, bool useAnimation = false);
    }
}