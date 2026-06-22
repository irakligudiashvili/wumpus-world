namespace WumpusWorld.Logic.Agent.Data {
    public enum ActionType {
        Move,
        Shoot
    }
    public record AgentAction (ActionType Type, Coordinate Target);
}
