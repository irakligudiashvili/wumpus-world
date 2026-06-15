namespace WumpusWorld.Logic.Expressions {
    public interface IExpression {
        bool Evaluate(Dictionary<string, bool> model);
        string ToString();
    }
}
