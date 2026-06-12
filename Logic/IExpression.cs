namespace WumpusWorld.Logic {
    public interface IExpression {
        bool Evaluate(Dictionary<string, bool> model);
        string ToString();
    }
}
