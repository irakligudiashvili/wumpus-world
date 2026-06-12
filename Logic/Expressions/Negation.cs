namespace WumpusWorld.Logic.Expressions {
    public class Negation {
        public IExpression Argument { get; }

        public Negation(IExpression arg) {
            Argument = arg;
        }

        public bool Evaluate(Dictionary<string, bool> model) => !Argument.Evaluate(model);

        public override string ToString() => $"¬({Argument})";
    }
}
