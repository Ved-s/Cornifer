using Cornifer;
using System;
using System.IO;

//PSDFile psd = new()
//{
//    Height = 1,
//    Width = 4,
//    Layers =
//    {
//        new()
//        {
//            Data = new byte[] 
//            {
//                0xFF, 0xFF, 0xFF, 0xFF,
//                0x00, 0x00, 0x00, 0x00,
//                0xff, 0x00, 0x00, 0xff,
//                0x00, 0xff, 0x00, 0xff
//            },
//            Name = "layername1",
//            Opacity = 128,
//            Visible = true,
//        },
//        //new()
//        //{
//        //    Data = new byte[] { 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0xFF },
//        //    Name = "layername2",
//        //    Opacity = 255,
//        //    Visible = false,
//        //}
//    }
//};
//using var fs = System.IO.File.Create("test.psd");
//psd.Write(fs);
//return;

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
#endif
Platform.Stop();
Environment.Exit(0);