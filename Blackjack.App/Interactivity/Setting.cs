namespace Blackjack.App.Interactivity;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

public class Setting : Binding
{
    private static readonly Dictionary<string, object?> settings;

    static Setting()
    {
        Application.Current.Exit += HandleApplicationExit;
        var data = AppData.LoadJson<Dictionary<string, JsonElement>>(nameof(settings));
        settings = data.ToDictionary<KeyValuePair<string, JsonElement>, string, object?>(p => p.Key, p => p.Value.ValueKind switch
        {
            JsonValueKind.String => p.Value.GetString(),
            JsonValueKind.Number => p.Value.GetDouble(),
            JsonValueKind.True or JsonValueKind.False => p.Value.GetBoolean(),
            _ => null,
        }, StringComparer.Ordinal);
    }

    public Setting(string path)
    : base()
    {
        if (!settings.ContainsKey(path))
            settings.Add(path, null);

        this.Path = new PropertyPath($".[{path}]");
        this.Source = settings;
        this.Mode = BindingMode.TwoWay;
        this.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
    }

    private static void HandleApplicationExit(object sender, ExitEventArgs e)
    {
        NormalizeValues();
        AppData.SaveJson(nameof(settings), settings);
    }

    private static void NormalizeValues()
    {
        foreach (var key in settings.Keys)
        {
            if (settings[key] is GridLength gridLength)
            {
                settings[key] = gridLength.IsAbsolute ? gridLength.Value : 100d;
            }
        }
    }
}

public sealed class NumberConverter : MarkupExtension, IValueConverter
{
    public static readonly NumberConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Convert.ChangeType(value ?? 0d, targetType, culture);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value;
    }

    public override object ProvideValue(IServiceProvider serviceProvider) => Instance;
}

static class AppData
{
    private const string DataExtension = ".json";
    private const string BackupExtension = ".bak";

    private static readonly string appDataFolder;

    static AppData()
    {
        appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Assembly.GetEntryAssembly()!.GetName().Name!);

        if (!Directory.Exists(appDataFolder))
        {
            Directory.CreateDirectory(appDataFolder);
        }
    }

    public static T LoadJson<T>(string name)
        where T : new()
    {
        try
        {
            var fileName = GetFileName(name);
            if (!File.Exists(fileName))
            {
                fileName = Path.ChangeExtension(fileName, BackupExtension);
                if (!File.Exists(fileName))
                    return new();
            }

            using var file = File.OpenRead(fileName);
            return JsonSerializer.Deserialize<T>(file) ?? new();
        }
        catch
        {
            return new();
        }
    }

    public static void SaveJson<T>(string name, T data)
    {
        try
        {
            var fileName = name;
            if (!Path.IsPathRooted(fileName))
            {
                fileName = GetFileName(name);
                if (File.Exists(fileName))
                {
                    var backupFileName = Path.ChangeExtension(fileName, BackupExtension);
                    File.Delete(backupFileName);
                    File.Copy(fileName, backupFileName, true);
                }
            }

            using var file = File.Create(fileName);
            JsonSerializer.Serialize(file, data);
            file.Flush();
        }
        catch { }
    }

    private static string GetFileName(string name)
    {
        return Path.Combine(appDataFolder, Path.ChangeExtension(name, DataExtension));
    }
}
