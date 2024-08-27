namespace Blackjack;

using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using static System.String;
using DealerPlay = (Card Upcard, int Score);
using PlayRule = System.Collections.Generic.KeyValuePair<string, HandMove>; //todo: (string Layout, HandMove Value)

public static class PlayerExtensions
{
    public static IList<Hand> Play(this Player player, int handCount = 4, int bank = Hand.DefaultBank) =>
        EnumerateHands(player, handCount, bank).ToList();

    private static IEnumerable<Hand> EnumerateHands(this Player player, int handCount, int bank)
    {
        for (var i = 0; i != handCount; ++i)
        {
            yield return player.BeginPlay(bank);
        }
    }
}

public abstract class Player : IEnumerable<Hand>
{
    protected static readonly HandMove[] AllMoves = [HandMove.Stand, HandMove.Hit, HandMove.Double, HandMove.Split];

    public static readonly Player Basic = new BasicPlayer();
    public static readonly Player None = new Bystander();

    protected const char CardSeparator = '/';
    protected const char MoveSeparator = ':';
    protected const char RuleSeparator = ',';

    private readonly List<Hand> hands;

    protected Player()
    {
        this.hands = [];
    }

    public abstract string Name { get; }

    public static char MoveToSymbol(HandMove move) => move switch
    {
        HandMove.Double => 'D',
        HandMove.Hit => 'H',
        HandMove.Split => 'P',
        _ => 'S'
    };

    public static IReadOnlyDictionary<string, HandMove> FromString(string strategy)
    {
        var rules = strategy
            .Split(RuleSeparator)
            .Where(rule => !IsNullOrWhiteSpace(rule))
            .Select(rule => rule.Split(MoveSeparator))
            .ToDictionary(rule => rule[0], rule => SymbolToMove(rule[1][0]), StringComparer.Ordinal);
        return rules;
    }

    public static string GetPlay(Hand hand, Card upcard)
    {
        return Concat(hand, CardSeparator, upcard);
    }

    public virtual Hand BeginPlay(int bank = Hand.DefaultBank)
    {
        return new Hand(this, bank);
    }

    public HandMove Move(Hand hand, Card upcard, int dealerScore)
    {
        var move = MoveOverride(hand, upcard, dealerScore);
        OnHandMoved(hand, upcard, dealerScore, move);
        return move;
    }

    public void EndPlay(Hand hand)
    {
        this.hands.Add(hand);
        OnPlayEnded(hand);
    }

    public IEnumerator<Hand> GetEnumerator() => ((IEnumerable<Hand>)this.hands).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)this.hands).GetEnumerator();

    protected static HandMove SymbolToMove(char symbol) => symbol switch
    {
        'D' => HandMove.Double,
        'H' => HandMove.Hit,
        'P' => HandMove.Split,
        _ => HandMove.Stand
    };

    protected static char MoveIndexToScoreSymbol(int i) => Card.RankToSymbol(Dealer.Upcards[i].Rank);

    protected static string ToString(IEnumerable<PlayRule> strategy) =>
        Join(RuleSeparator, strategy.Select(play => Concat(play.Key, MoveSeparator, MoveToSymbol(play.Value))));

    protected virtual void OnPlayEnded(Hand hand)
    {
    }

    protected abstract HandMove MoveOverride(Hand hand, Card upcard, int dealerScore);

    protected virtual void OnHandMoved(Hand hand, Card upcard, int dealerScore, HandMove move)
    {
    }
}

file sealed class BasicPlayer : Player
{
    public static readonly FrozenDictionary<string, HandMove> Strategy;

    private const string BasicStrategyTable =
        //  23456789TA
        """
        A-A=PPPPPPPPPP
        T-T=SSSSSSSSSS
        9-9=PPPPPSPPSS
        8-8=PPPPPPPPPP
        7-7=PPPPPPHHHH
        6-6=PPPPPHHHHH
        5-5=DDDDDDDDHH
        4-4=HHHPPHHHHH
        3-3=PPPPPPHHHH
        2-2=PPPPPPHHHH
        A-9=SSSSSSSSSS
        A-8=SSSSDSSSSS
        A-7=DDDDDSSHHH
        A-6=HDDDDHHHHH
        A-5=HHDDDHHHHH
        A-4=HHDDDHHHHH
        A-3=HHHDDHHHHH
        A-2=HHHDDHHHHH
         20=SSSSSSSSSS
         19=SSSSSSSSSS
         18=SSSSSSSSSS
         17=SSSSSSSSSS
         16=SSSSSHHHHH
         15=SSSSSHHHHH
         14=SSSSSHHHHH
         13=SSSSSHHHHH
         12=HHSSSHHHHH
         11=DDDDDDDDDD
         10=DDDDDDDDHH
          9=HDDDDHHHHH
          8=HHHHHHHHHH
          7=HHHHHHHHHH
          6=HHHHHHHHHH
          5=HHHHHHHHHH
        """;

    static BasicPlayer()
    {
        var basicStrategyMap = BasicStrategyTable
            .Split('\n')
            .Select(rules => rules.Split('='))
            .Select(rule =>
            new
            {
                Cards = rule[0].Trim(),
                Moves = rule[1].Trim().AsEnumerable().Select(m => SymbolToMove(m))
            })
            .SelectMany
            (
                rule => rule.Moves.Select((m, i) =>
                new
                {
                    Play = Concat(rule.Cards, CardSeparator, MoveIndexToScoreSymbol(i)),
                    Move = m
                })
            )
            .ToDictionary(rule => rule.Play, rule => rule.Move, StringComparer.Ordinal);

        Strategy = basicStrategyMap.ToFrozenDictionary(StringComparer.Ordinal);
    }

    public override string Name => "Basic";

    public override string ToString() => ToString(Strategy);

    protected override HandMove MoveOverride(Hand hand, Card upcard, int dealerScore)
    {
        return Strategy.GetValueOrDefault(GetPlay(hand, upcard));
    }
}

file sealed class Bystander : Player
{
    public override string Name => "Bystander";

    protected override HandMove MoveOverride(Hand hand, Card upcard, int dealerScore)
    {
        return HandMove.Stand;
    }
}

public class CustomPlayer : Player
{
    private readonly string name;
    private readonly FrozenDictionary<string, HandMove> ruleMap;

    private CustomPlayer() { }

    private protected CustomPlayer(string name, IEnumerable<PlayRule> rules)
    {
        this.name = name;
        this.ruleMap = rules.ToFrozenDictionary(StringComparer.Ordinal);
    }

    public override string Name => this.name;

    protected override HandMove MoveOverride(Hand hand, Card upcard, int dealerScore)
    {
        return this.ruleMap.GetValueOrDefault(GetPlay(hand, upcard));
    }

    public override string ToString() => ToString(this.ruleMap);

    public static CustomPlayer CreateRandom() => new("Random", MakeRandomRules());

    public static CustomPlayer CreateHitman() => new("Hitman",
        from hand in Hand.Deals
        from upcard in Dealer.Upcards
        select new PlayRule(GetPlay(hand, upcard), HandMove.Hit));

    public static IEnumerable<CustomPlayer> MakeChildren(CustomPlayer player1, CustomPlayer player2)
    {
        /**
         * P1 P2 C1 C2 C3 C4
         *
         * X1 Y1 X1 Y1 X1 Y3
         * X2 Y2 X2 Y2 X2 Y4
         * X3 Y3 Y1 X1 Y3 X3
         * X4 Y4 Y2 X2 Y4 X4
         */

        const int numChildren = 4;

        var player1RulesQuarterCount = player1.ruleMap.Count / numChildren;
        var x = Enumerable
            .Range(0, numChildren)
            .Select(i => player1.ruleMap.Skip(player1RulesQuarterCount * i).Take(player1RulesQuarterCount))
            .ToArray();

        var player2RulesQuarterCount = player2.ruleMap.Count / numChildren;
        var y = Enumerable
            .Range(0, numChildren)
            .Select(i => player2.ruleMap.Skip(player2RulesQuarterCount * i).Take(player2RulesQuarterCount))
            .ToArray();

        var children = new List<CustomPlayer>(capacity: numChildren);

        for (var xi = 0; xi != x.Length; ++xi)
        {
            for (var xii = xi + 1; xii != x.Length; ++xii)
            {
                for (var yi = 0; yi != y.Length; ++yi)
                {
                    for (var yii = yi + 1; yii != y.Length; ++yii)
                    {
                        children.Add(new CustomPlayer("Child", x[xi].Concat(x[xii]).Concat(y[yi]).Concat(y[yii])));
                        if (children.Count == numChildren)
                            return children.ToArray();
                    }
                }
            }
        }

        return children.ToArray();
    }

    public static IEnumerable<PlayRule> MakeRandomRules()
    {
        foreach (var play in from hand in Hand.Deals
                             from upcard in Dealer.Upcards
                             select (hand, upcard))
        {
            yield return new(GetPlay(play.hand, play.upcard), GetRandomMove(play.hand));
        }
    }

    public static HandMove GetRandomMove(Hand hand)
    {
        return AllMoves[RandomNumberGenerator.GetInt32(0,
            hand.FirstCard.Rank == hand.SecondCard.Rank ? AllMoves.Length : AllMoves.Length - 1)];
    }
}

public class AdaptivePlayer : Player
{
    protected static readonly HandMove[] NoSplitMoves = AllMoves[..^1];

    private sealed class LuckyHand : Hand
    {
        private DealerPlay? dealerPlay;

        public LuckyHand(AdaptivePlayer player, int bank) : base(player, bank) { }

        public override string ToString()
        {
            var isHard = this.FirstCard.Rank != this.SecondCard.Rank &&
                this.FirstCard.Rank != CardRank.Ace && this.SecondCard.Rank != CardRank.Ace;

            if (isHard)
            {
                return (this.FirstCard.Score + this.SecondCard.Score).ToString(CultureInfo.InvariantCulture);
            }

            return Join("-", this.cards.Take(2).OrderByDescending(card => card.Order));
        }

        public bool HasMoved => this.dealerPlay is not null;

        public string Layout => GetPlay(this, this.dealerPlay?.Upcard ?? Card.Joker);

        public override HandMove Move(Card upcard, int dealerScore)
        {
            var move = base.Move(upcard, dealerScore);

            if (this.Count == 2)
            {
                this.dealerPlay ??= (upcard, dealerScore);
            }

            return move;
        }
    }

    private sealed class LuckyHandMove
    {
        private readonly Stack<HandMove> moves;
        private int weight;

        public LuckyHandMove(Hand hand)
        {
            this.moves = new(hand.Kind == HandKind.Pair ? Shoe.Shuffle(AllMoves) : Shoe.Shuffle(NoSplitMoves));
        }

        public HandMove Value => this.moves.Peek();

        public int Weight
        {
            get => this.weight;
            set => Interlocked.Exchange(ref this.weight, value);
        }

        public bool TryChange()
        {
            var lockTaken = false;
            try
            {
                Monitor.TryEnter(((ICollection)this.moves).SyncRoot, ref lockTaken);

                if (lockTaken)
                {
                    if (this.moves.Count > 1)
                    {
                        this.moves.Pop();
                        this.weight = 0;
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(((ICollection)this.moves).SyncRoot);
                }
            }
        }
    }

    private readonly Dictionary<string, LuckyHandMove> moves;

    public AdaptivePlayer()
    {
        this.moves = (from hand in Hand.Deals
                      from upcard in Dealer.Upcards
                      select (Layout: GetPlay(hand, upcard), Hand: hand))
                    .ToDictionary(e => e.Layout, e => new LuckyHandMove(e.Hand), StringComparer.Ordinal);
    }

    public override string Name => "Adaptive";

    public override string ToString() => ToString(this.moves.Select(m => new PlayRule(m.Key, m.Value.Value)));

    public override Hand BeginPlay(int bank = Hand.DefaultBank)
    {
        return new LuckyHand(this, bank);
    }

    protected override HandMove MoveOverride(Hand hand, Card upcard, int dealerScore)
    {
        return this.moves.GetValueOrDefault(GetPlay(hand, upcard))?.Value ?? HandMove.Stand;
    }

    protected override void OnPlayEnded(Hand hand)
    {
        if (hand is LuckyHand { IsNatural: false, HasMoved: true } lucky)
        {
            ref var move = ref CollectionsMarshal.GetValueRefOrNullRef(this.moves, lucky.Layout);
            move.Weight += lucky.Play switch
            {
                HandPlay.Blackjack => 3,
                HandPlay.Win => 2,
                HandPlay.Tie => 1,
                HandPlay.Loss => -1,
                HandPlay.Bust => -2,
                _ => -3
            };

            if (move.Weight < -12)
            {
                move.TryChange();
            }
        }
    }

    private static HandMove GetRandomMoveExcept(Hand hand, HandMove excludedMove)
    {
        // make sure it's a new move
        var move = excludedMove;
        do
        {
            move = CustomPlayer.GetRandomMove(hand);
        } while (move == excludedMove);
        return move;
    }
}
