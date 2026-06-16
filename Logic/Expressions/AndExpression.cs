namespace WumpusWorld.Logic.Expressions {
    public class AndExpression : IExpression {
        public IExpression Left { get; }
        public IExpression Right { get; }

        public AndExpression(IExpression left, IExpression right) {
            Left = left;
            Right = right;
        }

        public bool Evaluate(Dictionary<string, bool> model) => Left.Evaluate(model) && Right.Evaluate(model);

        public override string ToString() => $"({Left} ∧ {Right})";
    }
}
