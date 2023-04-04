using Cornifer;
using System;
using System.Windows.Forms;


Platform.Start(args);
var game = new Main();

#if DEBUG

game.Run();

#else
try
{
    game.Run();
}
catch (Exception ex)
{
    Platform.DetachWindow();
    await Platform.MessageBox(
        $"Uncaught exception!\n" +
        $"After clicking Ok you will be prompted to save map state.\n" +
        $"Don't overwrite your existing state as it may be corrupted.\n" +
        $"Send this error when asking for help\n" +
        $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", "Cornifer has crashed!");
    await Main.SaveStateAs();
    Platform.Stop();
    Environment.Exit(1);
}
Platform.Stop();
Environment.Exit(0);
#endif