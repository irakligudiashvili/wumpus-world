using WumpusWorld.Logic.Inference;

namespace WumpusWorld.Logic {
    public class ResolutionEngine {
        private const int Limit = 20000;

        public bool Evaluate(List<Clause> clauses) {
            var clauseSet = new HashSet<string>();
            var workingClauses = new List<Clause>();

            foreach (var c in clauses) {
                string signature = c.ToString();

                if (!clauseSet.Contains(signature)) {
                    clauseSet.Add(signature);
                    workingClauses.Add(c);
                }
            }

            workingClauses = workingClauses.OrderBy(c => c.Literals.Count).ToList();
            var newestGeneration = new List<Clause>(workingClauses);
            int totalLoops = 0;

            while (true) {
                var nextGeneration = new List<Clause>();

                for (int i = 0; i < workingClauses.Count; i++) {
                    for (int j = 0; j < newestGeneration.Count; j++) {
                        if (workingClauses[i] == newestGeneration[j])
                            continue;

                        totalLoops++;
                        if (totalLoops > Limit)
                            return false;

                        List<Clause> resolvents = ResolvePairs(workingClauses[i], newestGeneration[j]);

                        foreach (var resolvent in resolvents) {
                            if (resolvent.IsEmpty)
                                return true;

                            string signature = resolvent.ToString();

                            if (!clauseSet.Contains(signature)) {
                                if (IsRedundant(resolvent, workingClauses) || IsRedundant(resolvent, nextGeneration))
                                    continue;

                                clauseSet.Add(signature);
                                nextGeneration.Add(resolvent);
                            }
                        }
                    }
                }

                if (nextGeneration.Count == 0)
                    return false;

                workingClauses.AddRange(nextGeneration);
                newestGeneration = nextGeneration;
            }
        }

        private List<Clause> ResolvePairs(Clause c1, Clause c2) {
            var resolvents = new List<Clause>();

            foreach (var lit1 in c1.Literals) {
                foreach (var lit2 in c2.Literals) {
                    if (lit1.SymbolName == lit2.SymbolName && lit1.IsPositive != lit2.IsPositive) {
                        var newLiterals = new HashSet<Literal>();

                        foreach (var l in c1.Literals)
                            if (!l.Equals(lit1))
                                newLiterals.Add(l);

                        foreach (var l in c2.Literals)
                            if (!l.Equals(lit2))
                                newLiterals.Add(l);

                        if (ContainsTautology(newLiterals))
                            continue;

                        resolvents.Add(new Clause(newLiterals));
                    }
                }
            }

            return resolvents;
        }
        

        private bool ContainsTautology(HashSet<Literal> literals) {
            foreach(var lit in literals) {
                if(literals.Contains(lit.Negate())) {
                    return true;
                }
            }

            return false;
        }

        private bool IsRedundant(Clause target, List<Clause> globalSet) {
            foreach (var existing in globalSet) {
                if(existing.Literals.Count <= target.Literals.Count && existing.Literals.IsSubsetOf(target.Literals)) {
                    return true;
                }
            }

            return false;
        }
    }
}
