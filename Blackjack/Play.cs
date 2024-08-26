namespace Blackjack;

using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using static System.String;

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
            .ToDictionary(rule => rule[0], rule => SymbolToMove(rule[1][0]));
        return rules;
    }

    public static string GetPlay(Hand hand, Card upcard)
    {
        return Concat(hand, CardSeparator, upcard);
    }

    public Hand BeginPlay(int bank = Hand.DefaultBank)
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

    protected static string ToString(IEnumerable<KeyValuePair<string, HandMove>> strategy) =>
        Join(RuleSeparator, strategy.Select(play => Concat(play.Key, MoveSeparator, MoveToSymbol(play.Value))));

    protected virtual void OnPlayEnded(Hand hand)
    {
    }

    protected abstract HandMove MoveOverride(Hand hand, Card upcard, int dealerScore);

    protected virtual void OnHandMoved(Hand hand, Card upcard, int dealerScore, HandMove move)
    {
    }
}

public class CustomPlayer : Player
{
    private readonly Dictionary<string, HandMove> ruleMap;

    public CustomPlayer() : this([])
    {
    }

    private CustomPlayer(IEnumerable<KeyValuePair<string, HandMove>> rules)
    {
        this.ruleMap = new Dictionary<string, HandMove>(rules);
    }

    public static CustomPlayer CreateHitman()
    {
        var hitman = new CustomPlayer();

        foreach (var deal in from hand in Hand.Deals
                             from upcard in Dealer.Upcards
                             select new { hand, upcard })
        {
            hitman.MakeRule(GetPlay(deal.hand, deal.upcard), HandMove.Hit);
        }

        return hitman;
    }

    public static CustomPlayer CreateRandom()
    {
        var random = new CustomPlayer();

        MakeRandomRules(random);

        return random;
    }

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
                        children.Add(new CustomPlayer(x[xi].Concat(x[xii]).Concat(y[yi]).Concat(y[yii])));
                        if (children.Count == numChildren)
                            return children.ToArray();
                    }
                }
            }
        }

        return children.ToArray();
    }

    public override string ToString() => ToString(this.ruleMap.ToArray());

    protected static void MakeRandomRules(CustomPlayer player)
    {
        foreach (var play in from hand in Hand.Deals
                             from upcard in Dealer.Upcards
                             select new { hand, upcard })
        {
            player.MakeRule(GetPlay(play.hand, play.upcard),
                Shoe.Shuffle(HandMove.Hit, HandMove.Stand, HandMove.Double, HandMove.Split).First());
        }
    }

    protected override HandMove MoveOverride(Hand hand, Card upcard, int dealerScore)
    {
        if (this.ruleMap.TryGetValue(GetPlay(hand, upcard), out var move))
            return move;

        return HandMove.Stand;
    }

    protected void MakeRule(string play, HandMove move)
    {
        this.ruleMap[play] = move;
    }
}

public class AdaptivePlayer : CustomPlayer
{
    private static readonly HandMove[] AllMoves = { HandMove.Stand, HandMove.Hit, HandMove.Double, HandMove.Split };
    private readonly Dictionary<Hand, Dictionary<string, HandMove>> moves;
    private readonly Dictionary<string, HashSet<HandMove>> wrongMoves;

    public AdaptivePlayer()
    {
        this.moves = [];
        this.wrongMoves = [];

        MakeRandomRules(this);
    }

    protected override void OnPlayEnded(Hand hand)
    {
        if (hand.Play == HandPlay.Wrong)
        {
            var moves = this.moves[hand];
            var lastMove = moves.Last();

            lock (((ICollection)this.wrongMoves).SyncRoot)
            {
                if (!this.wrongMoves.TryGetValue(lastMove.Key, out var playWrongMoves))
                {
                    playWrongMoves = [];
                    this.wrongMoves.Add(lastMove.Key, playWrongMoves);
                }
                if (playWrongMoves.Add(lastMove.Value))
                {
                    MakeRule(lastMove.Key, Shoe.Shuffle(AllMoves.Except(playWrongMoves)).First());
                }
            }
        }
    }

    protected override void OnHandMoved(Hand hand, Card upcard, int dealerScore, HandMove move)
    {
        lock (((ICollection)this.moves).SyncRoot)
        {
            if (!this.moves.TryGetValue(hand, out var handMoves))
            {
                handMoves = [];
                this.moves.Add(hand, handMoves);
            }
            handMoves.Add(GetPlay(hand, upcard), move);
        }
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
            .ToDictionary(rule => rule.Play, rule => rule.Move);

        Strategy = basicStrategyMap.ToFrozenDictionary();
    }

    public override string ToString() => ToString(Strategy);

    protected override HandMove MoveOverride(Hand hand, Card upcard, int dealerScore)
    {
        return Strategy.GetValueOrDefault(GetPlay(hand, upcard));
    }
}

file sealed class Bystander : Player
{
    protected override HandMove MoveOverride(Hand hand, Card upcard, int dealerScore)
    {
        return HandMove.Stand;
    }
}