using System;

namespace Domain.Attributes
{
    public sealed class EnumNameAttribute : Attribute
    {
        public readonly string Text;
        public EnumNameAttribute(string text)
        {
            Text = text;
        }
    }
}