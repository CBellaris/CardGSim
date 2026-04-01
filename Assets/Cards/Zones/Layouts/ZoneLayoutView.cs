using System.Collections.Generic;
using Cards.Core;
using Cards.Services;

namespace Cards.Zones.Layouts
{
    public class ZoneLayoutView
    {
        private readonly CardZone zone;
        private readonly IZoneLayout layout;
        private readonly GameContext context;
        private readonly Dictionary<CardInstance, CardEntityView> cardViews = new Dictionary<CardInstance, CardEntityView>();

        public ZoneLayoutView(CardZone zone, IZoneLayout layout, GameContext context)
        {
            this.zone = zone;
            this.layout = layout;
            this.context = context;

            if (zone == null || layout == null)
            {
                return;
            }

            zone.OnCardAdded += HandleCardAdded;
            zone.OnCardRemoved += HandleCardRemoved;
            zone.OnShuffled += HandleShuffled;
        }

        public void RegisterCardView(CardInstance card, CardEntityView view)
        {
            if (card == null || view == null)
            {
                return;
            }

            cardViews[card] = view;

            if (zone.Contains(card))
            {
                Rearrange(false);
            }
        }

        private void HandleCardAdded(CardInstance card, int index)
        {
            if (!cardViews.TryGetValue(card, out CardEntityView view))
            {
                return;
            }

            if (layout.SupportsIncrementalAdd)
            {
                layout.OnCardAdded(view, index, ShouldAnimate());
            }
            else
            {
                Rearrange(ShouldAnimate());
            }
        }

        private void HandleCardRemoved(CardInstance card)
        {
            Rearrange(ShouldAnimate());
        }

        private void HandleShuffled()
        {
            Rearrange(ShouldAnimate());
        }

        private void Rearrange(bool useAnimation)
        {
            var orderedViews = new List<CardEntityView>();
            foreach (CardInstance card in zone.Cards)
            {
                if (card != null && cardViews.TryGetValue(card, out CardEntityView view) && view != null)
                {
                    orderedViews.Add(view);
                }
            }

            layout.Arrange(orderedViews, useAnimation);
        }

        private bool ShouldAnimate()
        {
            return context?.AnimationPolicy?.IsEnabled != false;
        }
    }
}
