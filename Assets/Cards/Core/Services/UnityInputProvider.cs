using UnityEngine;
using Cards.Services;

namespace Cards.Core.Services
{
    public class UnityInputProvider : IInputProvider
    {
        public bool WasPressed(GameInputAction action)
        {
            switch (action)
            {
                case GameInputAction.DrawCard:
                    return Input.GetKeyDown(KeyCode.Space);
                case GameInputAction.EndTurn:
                    return Input.GetKeyDown(KeyCode.Return);
                default:
                    return false;
            }
        }
    }
}
