using Cornifer.UI.Elements;

namespace Cornifer.UI.Modals
{
    public class RegionSelect : Modal<RegionSelect, RegionSelect.Result?>
    {
        UIList RegionList;

        public RegionSelect()
        {
            Width = 300;
            Height = new(0, .9f);

            Margin = 5;
            Padding = new(5, 40);

            Elements = new(this)
            {
                new UILabel()
                {
                    Top = 10,
                    Height = 20,

                    Text = "Select region",
                    TextAlign = new(.5f)
                },
                new UIList()
                {
                    Top = 40,
                    Height = new(-100, 1),
                    ElementSpacing = 5,

                }.Assign(out RegionList),
                //new UIButton
                //{
                //    Top = new(-50, 1),
                //
                //    Height = 20,
                //    Text = "Manual select",
                //    TextAlign = new(.5f)
                //
                //}.OnEvent(UIElement.ClickEvent, (_, _) =>
                //{
                //    Thread dirSelect = new(() =>
                //    {
                //        System.Windows.Forms.FolderBrowserDialog fd = new();
                //        fd.UseDescriptionForTitle = true;
                //        fd.Description = "Select Rain World region folder. For example RainWorld_Data/StreamingAssets/world/su";
                //        if (fd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                //            return;
                //
                //        ModalVisible = false;
                //        Main.LoadRegion(fd.SelectedPath);
                //    });
                //    dirSelect.SetApartmentState(ApartmentState.STA);
                //    dirSelect.Start();
                //    dirSelect.Join();
                //}),
                new UIButton
                {
                    Top = new(-20, 1),
                    Left = new(0, .5f, -.5f),
                    Width = 80,
                    Height = 20,
                    Text = "Close",
                    TextAlign = new(.5f)
                }.OnEvent(UIElement.ClickEvent, (_, _) => ReturnResult(null))
            };
        }

        protected override void Shown()
        {
            RegionList.Elements.Clear();
            foreach (var (id, name, path) in Main.FindRegions())
            {
                RegionList.Elements.Add(new UIButton
                {
                    Text = $"{name} ({id})",
                    Height = 20,
                    TextAlign = new(.5f)
                }.OnEvent(UIElement.ClickEvent, (_, _) =>
                {
                    ReturnResult(new()
                    {
                        Path = path
                    });
                }));
            }

            RegionList.Recalculate();
        }

        public struct Result
        {
            public string Path;
        }
    }
}
