namespace Blackjack.App.Controls;

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

[TemplatePart(Name = TemplateParts.HardHandGrid, Type = typeof(Grid))]
[TemplatePart(Name = TemplateParts.SoftHandGrid, Type = typeof(Grid))]
[TemplatePart(Name = TemplateParts.PairHandGrid, Type = typeof(Grid))]
public class StrategyControl : HeaderedControl
{
    private static class TemplateParts
    {
        public const string HardHandGrid = "HardHandGrid";
        public const string SoftHandGrid = "SoftHandGrid";
        public const string PairHandGrid = "PairHandGrid";
    }

    public static readonly DependencyProperty RulesProperty =
        DependencyProperty.Register(nameof(Rules), typeof(string),
            typeof(StrategyControl), new PropertyMetadata(null, (d, e) => ((StrategyControl)d).OnRulesChanged(e)));

    private readonly Dictionary<string, HandMoveControl> moveControls;

    static StrategyControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StrategyControl), new FrameworkPropertyMetadata(typeof(StrategyControl)));
    }

    public StrategyControl()
    {
        this.moveControls = new Dictionary<string, HandMoveControl>();
    }

    public string Rules
    {
        get { return (string)GetValue(RulesProperty); }
        set { SetValue(RulesProperty, value); }
    }

    public override void OnApplyTemplate()
    {
        this.moveControls.Clear();

        if (GetTemplateChild(TemplateParts.HardHandGrid) is Grid hardHandGrid)
        {
            SetupRuleGrid(hardHandGrid, Hand.Deals.Where(h => h.Kind == HandKind.Hard), nameof(HandKind.Hard));
        }
        if (GetTemplateChild(TemplateParts.SoftHandGrid) is Grid softHandGrid)
        {
            SetupRuleGrid(softHandGrid, Hand.Deals.Where(h => h.Kind == HandKind.Soft), nameof(HandKind.Soft));
        }
        if (GetTemplateChild(TemplateParts.PairHandGrid) is Grid pairHandGrid)
        {
            SetupRuleGrid(pairHandGrid, Hand.Deals.Where(h => h.Kind == HandKind.Pair), nameof(HandKind.Pair));
        }

        OnRulesChanged(RulesProperty.ChangedEventArgs(this.Rules));
    }

    private void SetupRuleGrid(Grid grid, IEnumerable<Hand> deals, string caption)
    {
        var length = GridLength.Auto;
        grid.Children.Clear();
        grid.ColumnDefinitions.Clear();
        grid.RowDefinitions.Clear();

        grid.RowDefinitions.Add(new RowDefinition { Height = length });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = length });

        var captionCtrl = new TextBlock
        {
            Text = caption,
            Foreground = SystemColors.GrayTextBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = new RotateTransform(-45),
            RenderTransformOrigin = new Point(0.5, 0.5),
        };
        grid.Children.Add(captionCtrl);

        for (var i = 0; i != Dealer.Upcards.Length; ++i)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = length });

            var upcardCtrl = new HandMoveControl { EmptyText = Dealer.Upcards[i].ToString(), Background = Brushes.White };
            Grid.SetColumn(upcardCtrl, i + 1);
            grid.Children.Add(upcardCtrl);
        }

        var row = 0;
        foreach (var deal in deals)
        {
            ++row;
            grid.RowDefinitions.Add(new RowDefinition { Height = length });

            var dealCtrl = new HandMoveControl { EmptyText = deal.ToString(), Background = Brushes.White };
            Grid.SetRow(dealCtrl, row);
            grid.Children.Add(dealCtrl);

            for (var i = 0; i != Dealer.Upcards.Length; ++i)
            {
                var moveControl = new HandMoveControl();
                Grid.SetColumn(moveControl, i + 1);
                Grid.SetRow(moveControl, row);

                this.moveControls[Player.GetLayout(deal, Dealer.Upcards[i])] = moveControl;
                grid.Children.Add(moveControl);
            }
        }
    }

    private void OnRulesChanged(DependencyPropertyChangedEventArgs e)
    {
        foreach (var moveControl in this.moveControls.Values)
        {
            moveControl.Value = null;
        }

        if (e.NewValue is string movesStr && this.moveControls.Count > 0)
        {
            var moves = Player.FromString(movesStr);
            foreach (var move in moves)
            {
                if (!this.moveControls.TryGetValue(move.Key, out var moveControl))
                {
                    moveControl = new HandMoveControl();
                    this.moveControls[move.Key] = moveControl;
                }

                moveControl.SetCurrentValue(HandMoveControl.ValueProperty, move.Value);
            }
        }
    }
}