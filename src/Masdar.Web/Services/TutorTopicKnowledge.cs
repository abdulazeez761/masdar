using System.Text.RegularExpressions;

namespace Masdar.Web.Services;

internal sealed record TutorTopicGuide(
    string Topic,
    IReadOnlyList<string> Aliases,
    string PlainLanguage,
    string Steps,
    string Example,
    string Misconception,
    string Checkpoint);

internal static partial class TutorTopicKnowledge
{
    private static readonly IReadOnlyList<TutorTopicGuide> Guides =
    [
        new(
            "integration techniques",
            ["integration techniques", "integration by parts", "u substitution", "substitution", "partial fractions", "integral"],
            "Integration techniques are a toolbox for rewriting an integral into a form you can evaluate. Match the structure first: reverse the chain rule with substitution, use integration by parts for a product, and decompose a rational expression before integrating it term by term.",
            "1. Simplify the integrand and name its structure.\n2. Test substitution by looking for an inner expression and its derivative.\n3. For a product, choose u and dv, then apply ∫u dv = uv − ∫v du.\n4. Differentiate your result to check it.",
            "For ∫x·eˣ dx, choose u = x and dv = eˣdx. Then du = dx and v = eˣ, so the result is x·eˣ − ∫eˣdx = eˣ(x − 1) + C. Differentiating eˣ(x − 1) returns x·eˣ.",
            "A common mistake is choosing a method from a memorized keyword instead of the integrand's structure. Always verify that the remaining integral became simpler.",
            "Can you state why your chosen method reduces the difficulty, and can you verify the answer by differentiation?"),
        new(
            "applications of integration",
            ["applications of integration", "area between curves", "volume", "disk method", "washer method", "shell method"],
            "Applications of integration turn many small contributions into a total. The main modeling decision is what one thin slice contributes and which variable describes the slice.",
            "1. Sketch and label the region or quantity.\n2. Choose vertical or horizontal slices.\n3. Write one slice: height difference for area, or a cross-sectional area for volume.\n4. Find bounds and integrate.\n5. Check units and whether the sign makes sense.",
            "For the area between y = x and y = x² on [0,1], the line is above the parabola. The area is ∫₀¹(x − x²)dx = 1/2 − 1/3 = 1/6 square unit.",
            "Students often subtract in the wrong order or mix x-bounds with an integral in y. A quick sketch prevents both errors.",
            "What does a single slice represent, and do its dimensions combine into the units required by the problem?"),
        new(
            "infinite series",
            ["infinite series", "series convergence", "convergence test", "ratio test", "comparison test", "geometric series"],
            "An infinite series studies the limit of its partial sums—not merely whether its terms approach zero. Convergence tests provide different ways to decide whether those partial sums settle to a finite value.",
            "1. Check whether aₙ tends to zero; if not, the series diverges.\n2. Recognize special forms such as geometric or p-series.\n3. For positive terms, try comparison, limit comparison, ratio, or root tests.\n4. For alternating signs, test absolute convergence and then the alternating-series conditions.",
            "For Σ(1/2)ⁿ from n = 0, the partial sums form a geometric series with ratio 1/2. Because |r| < 1, it converges to 1/(1 − 1/2) = 2.",
            "The nth-term test can prove divergence, but aₙ → 0 does not prove convergence; the harmonic series is the standard counterexample.",
            "Can you justify why the chosen test applies before doing its algebra?"),
        new(
            "Taylor series",
            ["Taylor series", "Maclaurin series", "power series", "Taylor polynomial", "remainder"],
            "A Taylor series represents a function near a center using its derivatives there. Each added term matches one more derivative and usually improves the local approximation.",
            "1. Identify the center a.\n2. Compute or recognize f⁽ⁿ⁾(a).\n3. Substitute into Σ f⁽ⁿ⁾(a)(x−a)ⁿ/n!.\n4. Determine where the series converges.\n5. If approximating, bound or estimate the remainder.",
            "For eˣ about 0, every derivative equals eˣ and is 1 at x = 0. Thus eˣ = 1 + x + x²/2! + x³/3! + ···. Using the first three terms at x = 0.1 gives 1.105.",
            "A common mistake is writing the correct coefficients but forgetting powers of (x − a), especially when the center is not zero.",
            "Can you explain what the center is and how you would estimate the approximation error?"),
        new(
            "complexity analysis",
            ["complexity analysis", "big o", "big-o", "time complexity", "space complexity", "asymptotic"],
            "Complexity analysis describes how resource use grows as input size n grows. Big-O is an upper growth bound; it ignores constant factors so algorithms can be compared at scale.",
            "1. Define the input-size variable.\n2. Count the dominant repeated operation.\n3. Combine sequential blocks by addition and nested blocks by multiplication.\n4. Keep the fastest-growing term and drop constant factors.\n5. State whether the result is worst, average, or best case.",
            "A loop that runs n times and performs a binary search each time costs n·log n operations, so its time complexity is O(n log n). Two separate loops of n and n² operations combine to O(n²).",
            "Nested-looking code is not automatically O(n²); the number of iterations matters. Two pointers that only move forward can still be O(n).",
            "What is the dominant operation, how many times can it run, and which case are you analyzing?"),
        new(
            "balanced trees",
            ["balanced trees", "AVL", "AVL tree", "tree rotation", "binary search tree", "red black tree"],
            "A balanced search tree keeps its height proportional to log n so search, insertion, and deletion remain efficient. An AVL tree checks each node's balance factor—left height minus right height—and restores balance with rotations.",
            "1. Insert as in an ordinary binary search tree.\n2. Walk back toward the root and update heights.\n3. Find the first node with balance factor outside −1…1.\n4. Classify the path as LL, RR, LR, or RL.\n5. Apply one rotation for LL/RR or two for LR/RL, then recheck heights.",
            "Insert 30, 20, 10. Node 30 becomes left-left heavy, so rotate right at 30. The subtree root becomes 20, with 10 on the left and 30 on the right.",
            "Choosing the rotation from the unbalanced node alone is insufficient; use the direction of the child and grandchild path.",
            "After rotating, is the in-order sequence unchanged and does every affected node have a balance factor between −1 and 1?"),
        new(
            "graph traversal",
            ["graph traversal", "BFS", "breadth first search", "DFS", "depth first search", "traversal"],
            "Graph traversal systematically visits reachable vertices. BFS explores one distance layer at a time with a queue; DFS follows a path deeply before backtracking with a stack or recursion.",
            "1. Choose a start vertex and mark it visited immediately.\n2. Use a queue for BFS or a stack/recursive call for DFS.\n3. Remove the next vertex, process it, and add each unvisited neighbor.\n4. Store a parent when you need a traversal tree or path.\n5. Restart from an unvisited vertex if the graph may be disconnected.",
            "With edges A–B, A–C, B–D, BFS from A visits A, then B and C, then D. The BFS parent links also give a shortest unweighted path from A to D: A–B–D.",
            "Marking a vertex visited only when it is removed can add the same vertex many times. Mark it when it enters the queue or stack.",
            "Which data structure controls the frontier, and what guarantee does that choice give you?"),
        new(
            "shortest paths",
            ["shortest paths", "shortest path", "Dijkstra", "Bellman Ford", "Floyd Warshall"],
            "A shortest-path algorithm maintains the best distance known so far and improves it by relaxing edges. Dijkstra is correct when all edge weights are nonnegative; negative weights require a different algorithm such as Bellman–Ford.",
            "1. Set the source distance to 0 and every other distance to infinity.\n2. Select the unsettled vertex with smallest distance.\n3. Relax each outgoing edge: if dist[u] + w(u,v) is smaller, update dist[v] and parent[v].\n4. Mark u settled and repeat.\n5. Reconstruct a path by following parent links backward.",
            "If A→B costs 4, A→C costs 1, and C→B costs 2, relaxing from A gives B=4 and C=1. Processing C improves B to 3, so A→C→B is the shortest route.",
            "Dijkstra can finalize the wrong vertex when a negative edge later creates a cheaper route. Check the weight assumptions before selecting it.",
            "Can you show every successful relaxation and then reconstruct the path, not only report its distance?"),
        new(
            "normalization",
            ["normalization", "first normal form", "second normal form", "third normal form", "3NF", "functional dependency"],
            "Database normalization uses functional dependencies to organize attributes so each fact is stored in the right place, reducing update, insertion, and deletion anomalies.",
            "1. State the candidate keys and functional dependencies.\n2. Make values atomic for 1NF.\n3. Remove partial dependencies on part of a composite key for 2NF.\n4. Remove non-key attributes that depend on other non-key attributes for 3NF.\n5. Verify that joins can reconstruct the required information.",
            "In Enrollment(StudentId, CourseId, StudentName, CourseName, Grade), the key is (StudentId, CourseId). StudentName depends only on StudentId and CourseName only on CourseId, so move those facts to Student and Course tables.",
            "Splitting tables without first stating dependencies is decomposition, not a normalization argument.",
            "For every non-key attribute, can you say exactly which key determines it?"),
        new(
            "transactions",
            ["transactions", "database transaction", "ACID", "isolation level", "deadlock"],
            "A transaction groups operations into one logical unit. ACID describes the goal: atomic completion, valid state transitions, controlled interaction between concurrent work, and durable committed results.",
            "1. Define the invariant the operation must preserve.\n2. Begin the transaction as late as practical.\n3. Read and write all related state under a suitable isolation level.\n4. Commit only after every check succeeds; otherwise roll back.\n5. Keep transactions short and handle deadlocks with safe retry logic.",
            "A bank transfer debits one account and credits another in the same transaction. If the credit fails, rolling back the debit preserves the total balance.",
            "Atomicity does not by itself prevent two concurrent transactions from reading stale state; isolation controls that interaction.",
            "What invariant could concurrency violate, and which reads must be protected until commit?"),
        new(
            "gradient descent",
            ["gradient descent", "learning rate", "loss function", "optimization step"],
            "Gradient descent reduces a loss function by moving parameters in the direction opposite its gradient. The learning rate controls the size of each update.",
            "1. Initialize parameters.\n2. Compute predictions and loss.\n3. Differentiate the loss with respect to each parameter.\n4. Update θ ← θ − α∇J(θ).\n5. Repeat while monitoring training and validation behavior.",
            "For J(w)=(w−3)², the gradient is 2(w−3). Starting at w=0 with α=0.1 gives w=0−0.1(−6)=0.6, moving toward the minimum at 3.",
            "A learning rate that is too large may overshoot or diverge; one that is too small can make useful progress extremely slow.",
            "Is the loss trending down, and are you evaluating generalization rather than training loss alone?"),
        new(
            "subnetting",
            ["subnetting", "CIDR", "subnet mask", "network prefix", "IP range"],
            "Subnetting divides an IP address into a network prefix and host portion. A /p prefix fixes p bits; the remaining bits determine the address block size.",
            "1. Convert the prefix length to a mask.\n2. Compute block size from the changing octet.\n3. Find the network boundary containing the address.\n4. Identify network, usable host range, and broadcast address.\n5. Check that the requested host count fits.",
            "192.168.1.70/26 has blocks of 64 addresses: 0–63, 64–127, and so on. The network is .64, broadcast is .127, and usable hosts are .65 through .126.",
            "The number of addresses is not normally the number of usable IPv4 hosts; network and broadcast addresses are reserved in a conventional subnet.",
            "Can you derive the boundary from the block size instead of guessing from the address?"),
        new(
            "dynamic programming",
            ["dynamic programming", "memoization", "tabulation", "optimal substructure", "overlapping subproblems"],
            "Dynamic programming avoids recomputing overlapping subproblems by storing their answers. It applies when a solution can be built from optimal solutions to smaller states.",
            "1. Define a state with all information needed for the remaining decision.\n2. Write the recurrence and base cases.\n3. Choose memoized recursion or bottom-up tabulation.\n4. Determine a valid evaluation order.\n5. Store choices too if the actual solution must be reconstructed.",
            "For Fibonacci numbers, F(n)=F(n−1)+F(n−2). Caching each F(k) reduces exponential repeated work to O(n) time and O(n) stored states.",
            "Caching arbitrary recursion does not guarantee a good DP; an oversized or incomplete state can make the solution incorrect or unnecessarily expensive.",
            "Does your state uniquely determine the future, and how many distinct states can exist?"),
        new(
            "responsive CSS",
            ["responsive CSS", "responsive design", "media query", "flexbox", "CSS grid"],
            "Responsive CSS lets content adapt to available space. Start from a readable small-screen layout, use flexible sizing, and add a breakpoint only when the content—not a device name—requires one.",
            "1. Use semantic structure and a fluid base layout.\n2. Prefer flexible tracks, wrapping, min/max constraints, and relative units.\n3. Resize until the layout genuinely breaks.\n4. Add a focused media or container query there.\n5. Verify zoom, long text, keyboard focus, and both text directions.",
            "A card grid can use grid-template-columns: repeat(auto-fit, minmax(16rem, 1fr)); so columns appear only when there is enough room, often without multiple device-specific breakpoints.",
            "Shrinking text to make a layout fit trades a layout problem for an accessibility problem. Reflow the content instead.",
            "At 200% zoom, can every control still be reached and understood without horizontal page scrolling?"),
        new(
            "unit testing",
            ["unit testing", "unit test", "test double", "mock", "assertion"],
            "A unit test checks one observable behavior through a small, controlled boundary. It should explain the scenario, action, and expected outcome without depending on unrelated infrastructure.",
            "1. Name the behavior and edge case.\n2. Arrange only the inputs and collaborators it needs.\n3. Perform one meaningful action.\n4. Assert the public result or state change.\n5. Keep failures diagnostic and refactoring-friendly.",
            "For a discount rule, test a boundary such as subtotal=99.99 versus 100.00 and assert the final price. This communicates the rule more clearly than asserting private helper calls.",
            "Excessive mocking can test implementation choreography instead of behavior, making harmless refactors break the suite.",
            "Would the test still be valuable if the internal implementation changed but the public behavior stayed the same?"),
        new(
            "limits",
            ["limits", "limit", "continuity", "one sided limit", "L'Hopital"],
            "A limit describes the value a function approaches as its input approaches a point; the function need not equal that value at the point. Left and right behavior must agree for a two-sided limit to exist.",
            "1. Try direct substitution.\n2. If an indeterminate form appears, simplify algebraically or use a known limit.\n3. Check one-sided behavior when signs, roots, or piecewise definitions matter.\n4. Use L'Hôpital only for an eligible 0/0 or ∞/∞ form.\n5. State whether the limit is finite, infinite, or does not exist.",
            "lim x→2 (x²−4)/(x−2) is 0/0 by substitution. Factoring gives (x−2)(x+2)/(x−2)=x+2 for x≠2, so the limit is 4.",
            "Canceling terms at the point does not redefine the original function there; it only creates an equivalent expression nearby for evaluating the limit.",
            "Did you identify the indeterminate form and justify the transformation used to resolve it?")
    ];

    public static TutorTopicGuide? Find(string question, IReadOnlyCollection<string> activeTopics)
    {
        var normalizedQuestion = Normalize(question);
        var normalizedTopics = activeTopics.Select(Normalize).ToArray();

        return Guides
            .Where(guide => normalizedTopics.Any(topic =>
                topic.Contains(Normalize(guide.Topic), StringComparison.Ordinal) ||
                Normalize(guide.Topic).Contains(topic, StringComparison.Ordinal)))
            .Select(guide => new
            {
                Guide = guide,
                MatchLength = guide.Aliases
                    .Where(alias => ContainsPhrase(normalizedQuestion, Normalize(alias)))
                    .Select(alias => alias.Length)
                    .DefaultIfEmpty(0)
                    .Max()
            })
            .Where(match => match.MatchLength > 0)
            .OrderByDescending(match => match.MatchLength)
            .Select(match => match.Guide)
            .FirstOrDefault();
    }

    private static bool ContainsPhrase(string text, string phrase) =>
        (" " + text + " ").Contains(" " + phrase + " ", StringComparison.Ordinal);

    private static string Normalize(string value) =>
        WhitespacePattern().Replace(NonWordPattern().Replace(value.ToLowerInvariant(), " "), " ").Trim();

    [GeneratedRegex(@"[^\p{L}\p{N}]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonWordPattern();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespacePattern();
}
