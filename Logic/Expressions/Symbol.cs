namespace WumpusWorld.Logic.Expressions {
    public class Symbol {
        public string Name { get; }

        public Symbol(string name) => Name = name;

        public bool Evaluate(Dictionary<string, bool> model) {
            if(model.TryGetValue(Name, out bool value)) {
                return value;
            }

            throw new Exception($"Symbol '{Name}' is not defined");
        }

        public override string ToString() => Name;
    }
}
