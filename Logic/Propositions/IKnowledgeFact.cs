namespace WumpusWorld.Logic.Propositions {
    public interface IKnowledgeFact {
        IEnumerable<IExpression> ToSentences();
    }
}
