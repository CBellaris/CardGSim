namespace Cards.Rules.Interactions
{
    public interface IInteractionRule
    {
        int Priority { get; }
        bool Validate(InteractionRequest request);
        void BeforeExecute(InteractionRequest request);
        void Execute(InteractionRequest request);
    }
}
