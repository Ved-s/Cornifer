using Cornifer.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cornifer.UI.Modals
{
    public class StringEdit : Modal<StringEdit, StringEdit.Result>
    {
        static UILabel Title = null!;
        static UIInput Input = null!;

        static string OrigString = "";

        public StringEdit() 
        {
            Width = 250;
            Height = 80;

            Elements = new(this)
            {
                new UILabel
                {
                    Top = 8,
                    Height = 0,
                    TextAlign = new(.5f),
                    Text = "Title"
                }.Assign(out Title),

                new UIInput 
                {
                    Top = 30,
                    Margin = new(0, 15),
                    Height = 20,
                    Multiline = false,
                }.Assign(out Input),

                new UIButton
                {
                    Top = new(-5, 1, -1),
                    Left = new(-2, .5f, -1),
                    Width = 70,
                    Height = 20,
                    Text = "Ok",
                    TextAlign = new(.5f)
                }.OnClick(_ => ReturnResult(new(false, Input.Text))),

                new UIButton
                {
                    Top = new(-5, 1, -1),
                    Left = new(2, .5f),
                    Width = 70,
                    Height = 20,
                    Text = "Cancel",
                    TextAlign = new(.5f)
                }.OnClick(_ => ReturnResult(new(true, OrigString)))
            };
        }

        public static async Task<Result> ShowDialog(string title, string value)
        {
            await Interface.WaitModal();

            ModalVisible = false;
            Instance ??= new();

            OrigString = value;
            Title.Text = title;
            Input.Text = value;

            ModalVisible = true;

            return await Task;
        }


        public record struct Result(bool Cancel, string String);
    }
}
