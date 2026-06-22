using WumpusWorld.Logic.Expressions;
using WumpusWorld.Logic.Inference;
using WumpusWorld.Logic.Propositions;
using WumpusWorld.Logging; // Add logging reference

namespace WumpusWorld.Logic {
    public class KnowledgeBase {
        private readonly List<IExpression> _sentences = new List<IExpression>();
        private readonly CnfConverter _converter = new CnfConverter();
        private readonly ResolutionEngine _resolution = new ResolutionEngine();

        public void Tell(IKnowledgeFact fact) {
            foreach (var sentence in fact.ToSentences()) {
                KbLogger.Log($"[TELL] Fact added: {sentence}");
                _sentences.Add(sentence);
            }
        }

        public void Tell(IExpression sentence) {
            if (sentence == null) {
                throw new ArgumentException(nameof(sentence));
            }

            KbLogger.Log($"[TELL] Sentence added: {sentence}");
            _sentences.Add(sentence);
        }

        public bool Ask(IExpression query) {
            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }

            KbLogger.Log($"[ASK] Querying: {query}");

            // 1. negate the query
            IExpression negatedQuery = new Negation(query);

            // 2. combine KB sentences and negated query
            var sentencesToConvert = new List<IExpression>(_sentences) { negatedQuery };

            // 3. convert sentences into CNF clauses
            List<Clause> clauses = _converter.ConvertToCnfClauses(sentencesToConvert);

            // 4. run resolution
            bool result = _resolution.Evaluate(clauses);
            KbLogger.Log($"[RESOLUTION] Result: {result}");

            return result;
        }
    }
}