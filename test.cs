using System;
using DockWindows.Infrastructure.Windows;

public class Program {
    public static void Main() {
        var apps = AppSearchService.BuscarAppsInstalados();
        Console.WriteLine($"Found {apps.Count} apps.");
    }
}
