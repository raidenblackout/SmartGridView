using Uno.UI.Runtime.Skia.Gtk;

namespace SmartGridApp;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        App.InitializeLogging();

        var host = new GtkHost(() => new App());
        host.Run();
    }
}
