namespace Blackjack.App.Controls;

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

/// <summary>
/// The base class for controls that have a header.
/// </summary>
[Localizability(LocalizationCategory.Text)]
public class HeaderedControl : Control
{
    static HeaderedControl()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderedControl), new FrameworkPropertyMetadata(typeof(HeaderedControl)));
    }

    public HeaderedControl() { }

    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(nameof(Header), typeof(object), typeof(HeaderedControl),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnHeaderChanged)));

    [Bindable(true), Category("Content"), Localizability(LocalizationCategory.Label)]
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (HeaderedControl)d;

        ctrl.SetValue(HasHeaderPropertyKey, e.NewValue is not null);
        ctrl.OnHeaderChanged(e.OldValue, e.NewValue);
    }

    protected virtual void OnHeaderChanged(object? oldHeader, object? newHeader)
    {
        RemoveLogicalChild(oldHeader);
        AddLogicalChild(newHeader);
    }

    internal static readonly DependencyPropertyKey HasHeaderPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(HasHeader), typeof(bool), typeof(HeaderedControl),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty HasHeaderProperty = HasHeaderPropertyKey.DependencyProperty;

    [Bindable(false), Browsable(false)]
    public bool HasHeader => (bool)GetValue(HasHeaderProperty);

    public static readonly DependencyProperty HeaderTemplateProperty =
        DependencyProperty.Register(nameof(HeaderTemplate), typeof(DataTemplate), typeof(HeaderedControl),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnHeaderTemplateChanged)));

    [Bindable(true), Category("Content")]
    public DataTemplate? HeaderTemplate
    {
        get => GetValue(HeaderTemplateProperty) as DataTemplate;
        set => SetValue(HeaderTemplateProperty, value);
    }

    private static void OnHeaderTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (HeaderedControl)d;
        ctrl.OnHeaderTemplateChanged(e.OldValue as DataTemplate, e.NewValue as DataTemplate);
    }

    protected virtual void OnHeaderTemplateChanged(DataTemplate? oldHeaderTemplate, DataTemplate? newHeaderTemplate)
    {
    }

    public static readonly DependencyProperty HeaderTemplateSelectorProperty =
        DependencyProperty.Register(nameof(HeaderTemplateSelector), typeof(DataTemplateSelector), typeof(HeaderedControl),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnHeaderTemplateSelectorChanged)));

    [Bindable(true), Category("Content")]
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => GetValue(HeaderTemplateSelectorProperty) as DataTemplateSelector;
        set => SetValue(HeaderTemplateSelectorProperty, value);
    }

    private static void OnHeaderTemplateSelectorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (HeaderedControl)d;
        ctrl.OnHeaderTemplateSelectorChanged(e.OldValue as DataTemplateSelector, e.NewValue as DataTemplateSelector);
    }

    protected virtual void OnHeaderTemplateSelectorChanged(DataTemplateSelector? oldHeaderTemplateSelector, DataTemplateSelector? newHeaderTemplateSelector)
    {
    }

    public static readonly DependencyProperty HeaderStringFormatProperty =
        DependencyProperty.Register(nameof(HeaderStringFormat), typeof(string), typeof(HeaderedControl),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnHeaderStringFormatChanged)));

    [Bindable(true), Category("Content")]
    public string? HeaderStringFormat
    {
        get => GetValue(HeaderStringFormatProperty) as string;
        set => SetValue(HeaderStringFormatProperty, value);
    }

    private static void OnHeaderStringFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctrl = (HeaderedControl)d;
        ctrl.OnHeaderStringFormatChanged(e.OldValue as string, e.NewValue as string);
    }

    protected virtual void OnHeaderStringFormatChanged(string? oldHeaderStringFormat, string? newHeaderStringFormat)
    {
    }
}