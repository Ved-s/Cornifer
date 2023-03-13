using Cornifer;

#if !DEBUG
using System;
using System.Windows.Forms;

try
{
#endif

Platform.Start(args);
using var game = new Cornifer.Main();
game.Run();

#if !DEBUG
}
catch (Exception ex)
{
    MessageBox.Show($"Send this error when asking for help\n\n{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", "Cornifer has crashed!");
}
#endif