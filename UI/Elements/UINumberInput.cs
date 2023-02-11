using Cornifer.UI.Helpers;
using Cornifer.UI.Structures;
using Microsoft.Xna.Framework;
using System;
using System.Globalization;
using System.Text;

namespace Cornifer.UI.Elements
{
    internal class UINumberInput : UIInput
    {
        public delegate bool ValueValidatorDelegate(ref double value);
        public static readonly ElementEvent<Empty, UINumberInput> ValueChanged = new();

        public override bool Multiline => false;

        public override string Text
        {
            get => base.Text;
            set => Value = double.Parse(value);
        }

        public double Value
        {
            get
            {
                StringBuilder line = Lines[0];
                if (line.Length == 0
                 || line.Length == 1 && (line[0] == '-' || line[0] == '.')
                 || line.Length == 2 && line[0] == '-' && line[1] == '.')
                    return 0;

                double result;

                if (line[^1] == '.')
                    result = double.Parse(line.ToString(0, line.Length - 1));
                else
                    result = double.Parse(line.ToString(), CultureInfo.InvariantCulture);

                if (!UseValidatorInValue || !Active)
                    return result;

                ValueValidator?.Invoke(ref result);
                return result;
            }
            set
            {
                if (!AllowNegative)
                    value = Math.Abs(value);
                if (!AllowDecimal)
                    value = Math.Floor(value);

                Lines[0].Clear();
                Lines[0].Append(value);
            }
        }

        public bool AllowNegative = true;
        public bool AllowDecimal = true;

        public ValueValidatorDelegate? ValueValidator = null;
        bool UseValidatorInValue = true;

        public UINumberInput()
        {
            base.Text = "0";
        }

        protected override void TextInput(object? sender, TextInputEventArgs e)
        {
            if (!Events.PreCall(ValueChanged, default))
                return;

            if (e.Character == '-' && AllowNegative)
            {
                if (HasChar('-', out _))
                {
                    Lines[0].Replace("-", "");
                    CaretPos.X = Math.Max(0, CaretPos.X - 1);
                }
                else
                {
                    Lines[0].Insert(0, '-');
                    CaretPos.X++;
                }
                Events.PostCall(ValueChanged, default);
            }
            if (CaretPos.X == 0 && HasChar('-', out _))
                return;

            if (e.Character == '.' && AllowDecimal)
            {
                if (HasChar('.', out int ind))
                {
                    Lines[0].Remove(ind, 1);
                    if (ind < CaretPos.X)
                        CaretPos.X--;
                }
                base.TextInput(sender, e);
            }
            else if (char.IsDigit(e.Character))
                base.TextInput(sender, e);
        }

        protected override bool PreTextChanged()
        {
            if (!Events.PreCall(ValueChanged, default))
                return false;

            return base.PreTextChanged();
        }
        protected override void PostTextChanged()
        {
            base.PostTextChanged();

            Events.PostCall(ValueChanged, default);
        }

        protected override void ActiveChanged()
        {
            base.ActiveChanged();
            if (!Active)
            {
                if (Lines[0].Length == 0)
                    Value = 0;

                if (ValueValidator is not null)
                {
                    UseValidatorInValue = false;
                    double value = Value;
                    UseValidatorInValue = true;

                    if (!ValueValidator(ref value))
                        Value = value;
                }
            }
        }

        bool HasChar(char c, out int index)
        {
            for (index = 0; index < Lines[0].Length; index++)
                if (Lines[0][index] == c)
                    return true;
            return false;
        }
    }
}
