namespace SchedulingSystem.Services.LessonMovement
{
    /// <summary>
    /// Represents a debug event during recursive conflict resolution execution
    /// </summary>
    public class RecursiveDebugEvent
    {
        public string NodeId { get; set; } = string.Empty;
        public string? ParentId { get; set; }
        public string Type { get; set; } = "attempt";
        public int Depth { get; set; }
        public int LessonId { get; set; }
        public string LessonDescription { get; set; } = string.Empty;
        public string TargetSlot { get; set; } = string.Empty;
        public string OriginalPosition { get; set; } = string.Empty;
        public int Conflicts { get; set; }
        public List<ConflictingLessonInfo> ConflictingLessons { get; set; } = new();
        public int[] VisitedLessons { get; set; } = Array.Empty<int>();
        public List<ProposedMoveInfo> ProposedMoves { get; set; } = new();
        public long ElapsedMs { get; set; }
        public double? QualityScore { get; set; }
        public string Result { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class ConflictingLessonInfo
    {
        public int LessonId { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Slot { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
    }

    public class ProposedMoveInfo
    {
        public int LessonId { get; set; }
        public string Slot { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result container that includes both solutions and debug information
    /// </summary>
    public class RecursiveDebugResult
    {
        public List<KempeChainSolution> Solutions { get; set; } = new();
        public List<RecursiveDebugEvent> DebugEvents { get; set; } = new();
        public int NodesExplored { get; set; }
        public int MaxDepth { get; set; }
        public long ElapsedMs { get; set; }
    }
}
