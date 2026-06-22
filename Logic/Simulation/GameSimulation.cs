namespace WumpusWorld.Logic.Simulation;

public class GameSimulation {
    public WorldEnvironment Environment { get; init; }
    public AgentState AgentState { get; init; }
    public AgentBrain Brain { get; init; }
    private bool Bumped { get; set; } = false;

    public GameSimulation(WorldEnvironment environment, AgentBrain brain, AgentState? initialState = null) {
        Environment = environment;
        Brain = brain;
        AgentState = initialState ?? new AgentState();
    }

    public Coordinate? ProcessNextMove() {
        if (!AgentState.IsAlive || AgentState.HasWon)
            return null;

        Percept currentPercept = Environment.GetPerceptAt(AgentState.Position, Bumped);

        Logger.Log($"[AGENT] Current Position: ({AgentState.Position.X}, {AgentState.Position.Y})");
        Logger.Log($"[AGENT] Orientation: {AgentState.Facing}");
        Logger.Log($"[SENSORS] Stench={(currentPercept.Stench ? "YES" : "NO")}, Breeze={(currentPercept.Breeze ? "YES" : "NO")}, Glitter={(currentPercept.Glitter ? "YES" : "NO")}, Bump={(Bumped ? "YES" : "NO")}, Scream={(currentPercept.Scream ? "YES" : "NO")}");

        Bumped = false;
        Logger.Log("[ENGINE] Processing next action...");

        Coordinate? targetCoordinate = Brain.ProcessTurn(AgentState.Position, currentPercept, AgentState);

        if(Environment.wumpusScreamed)
            Environment.ClearScream();

        if (targetCoordinate != null) {
            if (Environment.IsWumpusAlive && Environment.WumpusPosition != null && targetCoordinate.Value.Equals(Environment.WumpusPosition)) {
                Logger.Log($"[ENGINE] Attack target identified at: ({targetCoordinate.Value.X}, {targetCoordinate.Value.Y})");
            } else {
                Logger.Log($"[ENGINE] Next action target step: ({targetCoordinate.Value.X}, {targetCoordinate.Value.Y})");
            }
        } else {
            Logger.Log($"[ENGINE] Process complete, no available action target.");
        }

        return targetCoordinate;
    }

    public void ExecuteAction(Coordinate? target, int turnNum) {
        Coordinate current = AgentState.Position;

        // 1. Do we intend to shoot? map it to shooting logic
        if (Environment.IsWumpusAlive && Environment.WumpusPosition != null && Brain.intentToShoot) {
            Direction targetDirection = GetDirectionTo(current, target);
            if (AgentState.Facing == targetDirection) {
                ShootArrow(turnNum);
            } else {
                RotateTowards(targetDirection, turnNum);
            }
            return;
        }

        // 2. Is this target coordinate our own cell? Check for gold
        if (current.Equals(target)) {
            if (current.X == Environment.GoldPosition.X && current.Y == Environment.GoldPosition.Y && !AgentState.HasGold) {
                GrabGold(turnNum);
            }
            return;
        }

        // 3. Standard movement step
        Direction requiredDirection = GetDirectionTo(current, target);
        if (AgentState.Facing == requiredDirection) {
            MoveForward(turnNum);
        } else {
            RotateTowards(requiredDirection, turnNum);
        }
    }

    private void RotateTowards(Direction targetDir, int turnNum) {
        int currentWeight = GetDirectionWeight(AgentState.Facing);
        int targetWeight = GetDirectionWeight(targetDir);
        int clockwiseDistance = (targetWeight - currentWeight + 4) % 4;

        if (clockwiseDistance == 1) {
            RotateAgentRight(turnNum);
        } else {
            RotateAgentLeft(turnNum);
        }
    }

    private Direction GetDirectionTo(Coordinate? origin, Coordinate? target) {
        int targetX = target?.X ?? 0;
        int originX = origin?.X ?? 0;
        int targetY = target?.Y ?? 0;
        int originY = origin?.Y ?? 0;

        if (targetX > originX) return Direction.Right;
        if (targetX < originX) return Direction.Left;
        if (targetY > originY) return Direction.Up;
        if (targetY < originY) return Direction.Down;

        return AgentState.Facing;
    }

    private int GetDirectionWeight(Direction dir) => dir switch {
        Direction.Up => 0,
        Direction.Right => 1,
        Direction.Down => 2,
        Direction.Left => 3,
        _ => 0
    };

    public void MoveForward(int turnNum = 0) {
        if (!AgentState.IsAlive) return;

        Coordinate nextPos = AgentState.Facing switch {
            Direction.Up => new Coordinate(AgentState.Position.X, AgentState.Position.Y + 1),
            Direction.Down => new Coordinate(AgentState.Position.X, AgentState.Position.Y - 1),
            Direction.Left => new Coordinate(AgentState.Position.X - 1, AgentState.Position.Y),
            Direction.Right => new Coordinate(AgentState.Position.X + 1, AgentState.Position.Y),
            _ => AgentState.Position
        };

        if (nextPos.X >= 1 && nextPos.X <= Environment.Grid.Size && nextPos.Y >= 1 && nextPos.Y <= Environment.Grid.Size) {
            Logger.Log($"[ACTION] Executing MoveForward into ({nextPos.X}, {nextPos.Y})");
            AgentState.MoveForward();
            CheckDanger(turnNum);
        } else {
            Bumped = true;
            Logger.Log($"[ACTION] Executing MoveForward, bumped a wall at ({nextPos.X}, {nextPos.Y})");
            AgentState.AdjustScore(-1);
        }
    }

    public void RotateAgentLeft(int turnNum = 0) {
        if (!AgentState.IsAlive) return;
        Logger.Log($"[ACTION] Rotating Left. Changing alignment from {AgentState.Facing}...");
        AgentState.TurnLeft();
    }

    public void RotateAgentRight(int turnNum = 0) {
        if (!AgentState.IsAlive) return;
        Logger.Log($"[ACTION] Rotating Right. Changing alignment from {AgentState.Facing}...");
        AgentState.TurnRight();
    }

    public void GrabGold(int turnNum = 0) {
        if (!AgentState.IsAlive) return;
        AgentState.AdjustScore(-1);

        if (AgentState.Position.X == Environment.GoldPosition.X && AgentState.Position.Y == Environment.GoldPosition.Y) {
            Logger.Log($"[ACTION] Grabbed Gold at ({AgentState.Position.X}, {AgentState.Position.Y})!");
            AgentState.HasGold = true;
            AgentState.AdjustScore(1000);
        } else {
            Logger.Log($"[ACTION] Attempted GrabGold, but no gold exists on tile ({AgentState.Position.X}, {AgentState.Position.Y}).");
        }
    }

    public void ShootArrow(int turnNum = 0) {
        if (!AgentState.IsAlive || !AgentState.HasArrow) return;

        Logger.Log($"[ACTION] Shot Arrow towards {AgentState.Facing} from ({AgentState.Position.X}, {AgentState.Position.Y}).");

        AgentState.ShootArrow();

        Coordinate intendedTarget = AgentState.Facing switch {
            Direction.Up => new Coordinate(AgentState.Position.X, AgentState.Position.Y + 1),
            Direction.Down => new Coordinate(AgentState.Position.X, AgentState.Position.Y - 1),
            Direction.Left => new Coordinate(AgentState.Position.X - 1, AgentState.Position.Y),
            Direction.Right => new Coordinate(AgentState.Position.X + 1, AgentState.Position.Y),
            _ => AgentState.Position
        };

        bool hit = false;
        Coordinate arrowPos = AgentState.Position;

        while (arrowPos.X >= 1 && arrowPos.X <= Environment.Grid.Size && arrowPos.Y >= 1 && arrowPos.Y <= Environment.Grid.Size) {
            if (arrowPos.X == Environment.WumpusPosition.X && arrowPos.Y == Environment.WumpusPosition.Y) {
                hit = true;
                break;
            }

            arrowPos = AgentState.Facing switch {
                Direction.Up => new Coordinate(arrowPos.X, arrowPos.Y + 1),
                Direction.Down => new Coordinate(arrowPos.X, arrowPos.Y - 1),
                Direction.Left => new Coordinate(arrowPos.X - 1, arrowPos.Y),
                Direction.Right => new Coordinate(arrowPos.X + 1, arrowPos.Y),
                _ => arrowPos
            };
        }

        if (hit) {
            Logger.Log($"[COMBAT] Arrow hit! A scream is heard in the cave.");
            Environment.TriggerScream();
        } else {
            Logger.Log($"[COMBAT] Arrow shot, but no scream was heard");

            Brain.HandleMissedShot(intendedTarget);
        }
    }

    private void CheckDanger(int turnNum = 0) {
        if (Environment.IsDangerous(AgentState.Position)) {
            Logger.Log($"[FATAL] Agent died at ({AgentState.Position.X}, {AgentState.Position.Y})!");
            AgentState.IsAlive = false;
            AgentState.AdjustScore(-1000);
        }
    }
}