using WumpusWorld.Logic.Expressions;
using WumpusWorld.Logic.Inference;

namespace WumpusWorld.Logic {
    public class CnfConverter {
        public List<Clause> ConvertToCnfClauses(IEnumerable<IExpression> expressions) {
            var result = new List<Clause>();

            foreach(var ex in expressions) {
                IExpression curr = ex;

                curr = EliminateBiConditionals(curr);
                curr = EliminateImplications(curr);
                curr = DeMorgansLaw(curr);
                curr = DistributeOrOverAnd(curr);

                ShredIntoClauses(curr, result);
            }

            return result;
        }

        private IExpression EliminateBiConditionals(IExpression ex) {
            return ex switch {
                Symbol sym => sym,

                BiConditional bi => new AndExpression(
                    new Implication(EliminateBiConditionals(bi.Left), EliminateBiConditionals(bi.Right)),
                    new Implication(EliminateBiConditionals(bi.Right), EliminateBiConditionals(bi.Left))
                ),

                Implication imp => new Implication(
                    EliminateBiConditionals(imp.Left),
                    EliminateBiConditionals(imp.Right)
                ),

                Negation neg => new Negation(EliminateBiConditionals(neg.Argument)),

                AndExpression and => new AndExpression(
                    EliminateBiConditionals(and.Left),
                    EliminateBiConditionals(and.Right)
                ),

                OrExpression or => new OrExpression(
                    EliminateBiConditionals(or.Left),
                    EliminateBiConditionals(or.Right)
                ),

                _ => throw new ArgumentException($"Unhandled type: {ex.GetType()}")
            };
        }

        private IExpression EliminateImplications(IExpression ex) {
            return ex switch {
                Symbol sym => sym,

                Implication imp => new OrExpression(
                    new Negation(EliminateImplications(imp.Left)),
                    EliminateImplications(imp.Right)
                ),

                Negation neg => new Negation(EliminateImplications(neg.Argument)),

                AndExpression and => new AndExpression(
                    EliminateImplications(and.Left),
                    EliminateImplications(and.Right)
                ),

                OrExpression or => new OrExpression(
                    EliminateImplications(or.Left),
                    EliminateImplications(or.Right)
                ),

                _ => throw new ArgumentException($"Unhandled type: {ex.GetType()}")
            };
        }

        private IExpression DeMorgansLaw(IExpression ex) {
            if(ex is not Negation neg) {
                return ex switch {
                    Symbol sym => sym,

                    AndExpression and => new AndExpression(
                        DeMorgansLaw(and.Left), 
                        DeMorgansLaw(and.Right)
                    ),

                    OrExpression or => new OrExpression(
                        DeMorgansLaw(or.Left),
                        DeMorgansLaw(or.Right)
                    ),

                    _ => throw new ArgumentException($"Unhandled type: {ex.GetType()}")
                };
            }

            return neg.Argument switch {
                Symbol sym => new Negation(DeMorgansLaw(sym)),

                Negation doubleNeg => DeMorgansLaw(doubleNeg.Argument),

                AndExpression and => new OrExpression(
                    DeMorgansLaw(new Negation(and.Left)),
                    DeMorgansLaw(new Negation(and.Right))
                ),

                OrExpression or => new AndExpression(
                    DeMorgansLaw(new Negation(or.Left)),
                    DeMorgansLaw(new Negation(or.Right))
                ),

                _ => throw new ArgumentException($"Unhandled type: {ex.GetType()}")
            };
        }

        private IExpression DistributeOrOverAnd(IExpression ex) {
            return ex switch {
                Symbol sym => sym,
                Negation neg => neg,

                AndExpression and => new AndExpression(
                    DistributeOrOverAnd(and.Left),
                    DistributeOrOverAnd(and.Right)
                ),

                OrExpression or => DistributeOr(
                    DistributeOrOverAnd(or.Left),
                    DistributeOrOverAnd(or.Right)
                ),

                _ => throw new ArgumentException($"Unhandled Type: {ex.GetType()}")
            };
        }

        private IExpression DistributeOr(IExpression left, IExpression right) {
            if (left is AndExpression andLeft) {
                return new AndExpression(
                    DistributeOr(andLeft.Left, right),
                    DistributeOr(andLeft.Right, right)
                );
            }

            if (right is AndExpression andRight) {
                return new AndExpression(
                    DistributeOr(left, andRight.Left),
                    DistributeOr(left, andRight.Right)
                );
            }

            return new OrExpression(left, right);
        }

        private void ShredIntoClauses(IExpression ex, List<Clause> list) {
            if (ex is AndExpression and) {
                ShredIntoClauses(and.Left, list);
                ShredIntoClauses(and.Right, list);
            } else {
                var clause = new Clause();
                CollectLiterals(ex, clause.Literals);
                list.Add(clause);
            }
        }

        private void CollectLiterals(IExpression ex, HashSet<Literal> literals) {
            switch (ex) {
                case Symbol sym:
                    literals.Add(new Literal(sym.Name, true));
                    break;
                case Negation neg when neg.Argument is Symbol sym:
                    literals.Add(new Literal(sym.Name, false));
                    break;
                case OrExpression or:
                    CollectLiterals(or.Left, literals);
                    CollectLiterals(or.Right, literals);
                    break;
                default:
                    throw new ArgumentException($"Unhandled type: {ex.GetType()}");
            }
        }
    }
}
