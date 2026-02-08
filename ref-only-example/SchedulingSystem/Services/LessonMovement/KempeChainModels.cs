using SchedulingSystem.Models;

namespace SchedulingSystem.Services.LessonMovement
{
    /// <summary>
    /// Represents a Kempe chain - a set of lessons that are connected through shared resources
    /// (teachers, classes, rooms) in two specific timeslots
    /// </summary>
    public class KempeChain
    {
        /// <summary>
        /// First timeslot involved in the chain
        /// </summary>
        public TimeSlot Slot1 { get; set; } = default!;

        /// <summary>
        /// Second timeslot involved in the chain
        /// </summary>
        public TimeSlot Slot2 { get; set; } = default!;

        /// <summary>
        /// Lessons in Slot1 that are part of this chain
        /// </summary>
        public List<int> LessonsInSlot1 { get; set; } = new();

        /// <summary>
        /// Lessons in Slot2 that are part of this chain
        /// </summary>
        public List<int> LessonsInSlot2 { get; set; } = new();

        /// <summary>
        /// Total number of lessons in the chain
        /// </summary>
        public int ChainSize => LessonsInSlot1.Count + LessonsInSlot2.Count;

        /// <summary>
        /// Whether this chain includes the target lesson
        /// </summary>
        public bool ContainsLesson(int scheduledLessonId)
        {
            return LessonsInSlot1.Contains(scheduledLessonId) ||
                   LessonsInSlot2.Contains(scheduledLessonId);
        }
    }

    /// <summary>
    /// Represents a timeslot (day + period)
    /// </summary>
    public class TimeSlot
    {
        public DayOfWeek Day { get; set; }
        public int PeriodId { get; set; }

        public TimeSlot(DayOfWeek day, int periodId)
        {
            Day = day;
            PeriodId = periodId;
        }

        public override bool Equals(object? obj)
        {
            return obj is TimeSlot slot &&
                   Day == slot.Day &&
                   PeriodId == slot.PeriodId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Day, PeriodId);
        }

        public override string ToString()
        {
            return $"{Day}:{PeriodId}";
        }
    }

    /// <summary>
    /// Represents a conflict graph where nodes are scheduled lessons
    /// and edges connect lessons that share resources (teacher, class, room)
    /// </summary>
    public class ConflictGraph
    {
        /// <summary>
        /// Adjacency list representation: lessonId -> list of conflicting lessonIds
        /// </summary>
        private readonly Dictionary<int, HashSet<int>> _adjacencyList = new();

        /// <summary>
        /// Map from lessonId to its current timeslot
        /// </summary>
        private readonly Dictionary<int, TimeSlot> _lessonSlots = new();

        /// <summary>
        /// Add a lesson to the graph
        /// </summary>
        public void AddLesson(int lessonId, TimeSlot slot)
        {
            if (!_adjacencyList.ContainsKey(lessonId))
            {
                _adjacencyList[lessonId] = new HashSet<int>();
            }
            _lessonSlots[lessonId] = slot;
        }

        /// <summary>
        /// Add a conflict edge between two lessons
        /// </summary>
        public void AddConflict(int lesson1, int lesson2)
        {
            if (!_adjacencyList.ContainsKey(lesson1))
                _adjacencyList[lesson1] = new HashSet<int>();
            if (!_adjacencyList.ContainsKey(lesson2))
                _adjacencyList[lesson2] = new HashSet<int>();

            _adjacencyList[lesson1].Add(lesson2);
            _adjacencyList[lesson2].Add(lesson1);
        }

        /// <summary>
        /// Get lessons in a specific timeslot
        /// </summary>
        public List<int> GetLessonsInSlot(TimeSlot slot)
        {
            return _lessonSlots
                .Where(kvp => kvp.Value.Equals(slot))
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Get the timeslot for a lesson
        /// </summary>
        public TimeSlot? GetLessonSlot(int lessonId)
        {
            return _lessonSlots.TryGetValue(lessonId, out var slot) ? slot : null;
        }

        /// <summary>
        /// Check if two lessons conflict (share resources)
        /// </summary>
        public bool HasConflict(int lesson1, int lesson2)
        {
            return _adjacencyList.TryGetValue(lesson1, out var conflicts) &&
                   conflicts.Contains(lesson2);
        }

        /// <summary>
        /// Get all lessons that conflict with the given lesson
        /// </summary>
        public HashSet<int> GetConflictingLessons(int lessonId)
        {
            return _adjacencyList.TryGetValue(lessonId, out var conflicts)
                ? conflicts
                : new HashSet<int>();
        }

        /// <summary>
        /// Extract a Kempe chain for two timeslots starting from a specific lesson
        /// </summary>
        public KempeChain ExtractKempeChain(int startLessonId, TimeSlot slot1, TimeSlot slot2)
        {
            var chain = new KempeChain { Slot1 = slot1, Slot2 = slot2 };
            var visited = new HashSet<int>();
            var queue = new Queue<int>();

            // Start BFS from the given lesson
            queue.Enqueue(startLessonId);
            visited.Add(startLessonId);

            while (queue.Count > 0)
            {
                var currentLesson = queue.Dequeue();
                var currentSlot = GetLessonSlot(currentLesson);

                // Add to appropriate chain list
                if (currentSlot != null)
                {
                    if (currentSlot.Equals(slot1))
                        chain.LessonsInSlot1.Add(currentLesson);
                    else if (currentSlot.Equals(slot2))
                        chain.LessonsInSlot2.Add(currentLesson);
                }

                // Explore conflicting lessons in the two slots
                var conflicts = GetConflictingLessons(currentLesson);
                foreach (var conflictingLesson in conflicts)
                {
                    if (visited.Contains(conflictingLesson))
                        continue;

                    var conflictSlot = GetLessonSlot(conflictingLesson);

                    // Only include if in one of the two slots
                    if (conflictSlot != null &&
                        (conflictSlot.Equals(slot1) || conflictSlot.Equals(slot2)))
                    {
                        visited.Add(conflictingLesson);
                        queue.Enqueue(conflictingLesson);
                    }
                }
            }

            return chain;
        }
    }

    /// <summary>
    /// Represents a move in the tabu search (lesson moved from one slot to another)
    /// </summary>
    public class TabuMove
    {
        public int LessonId { get; set; }
        public TimeSlot FromSlot { get; set; } = default!;
        public TimeSlot ToSlot { get; set; } = default!;
        public int Iteration { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is TabuMove move &&
                   LessonId == move.LessonId &&
                   FromSlot.Equals(move.FromSlot) &&
                   ToSlot.Equals(move.ToSlot);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(LessonId, FromSlot, ToSlot);
        }
    }

    /// <summary>
    /// Tabu list to track recently made moves
    /// </summary>
    public class TabuList
    {
        private readonly Queue<TabuMove> _moves = new();
        private readonly HashSet<TabuMove> _moveSet = new();
        private readonly int _tenure; // How long a move stays tabu

        public TabuList(int tenure = 7)
        {
            _tenure = tenure;
        }

        /// <summary>
        /// Add a move to the tabu list
        /// </summary>
        public void AddMove(TabuMove move)
        {
            _moves.Enqueue(move);
            _moveSet.Add(move);

            // Remove old moves beyond tenure
            while (_moves.Count > _tenure)
            {
                var oldMove = _moves.Dequeue();
                _moveSet.Remove(oldMove);
            }
        }

        /// <summary>
        /// Check if a move is tabu
        /// </summary>
        public bool IsTabu(TabuMove move)
        {
            return _moveSet.Contains(move);
        }

        /// <summary>
        /// Clear the tabu list
        /// </summary>
        public void Clear()
        {
            _moves.Clear();
            _moveSet.Clear();
        }
    }
}
