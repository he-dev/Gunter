﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gunter.Data;
using Gunter.Reporting;
using Reusable.Markup;
using Reusable.Markup.Html;

namespace Gunter.Messaging.Email.Templates
{
    internal class FooterTemplate : HtmlTemplate
    {
        public FooterTemplate() : base(new Dictionary<string, string>
        {
            [Style.Author] = "color: #247BA0;",
            [Style.Paragraph] = "font-family: sans-serif; font-size: 12px; color: #A0A0A0;",
            [Style.HorizonalRule] = "border: 0; border-bottom: 1px solid #ccc; background: #CCC"
        })
        { }

        public override string Render(TestUnit context, ISection section)
        {
            return null;
        }

        public string Render(string name, DateTime timestamp)
        {
            var footer = 
                Html
                    .Element("p", p => p
                        .Append("Auto-generated by ")
                        .Element("span", span => span.Append(name).Style(Styles[Style.Author]))
                        .Append($" {timestamp.ToString(CultureInfo.InvariantCulture)} (UTC)")
                        .Style(Styles[Style.Paragraph]))
                    .ToHtml();

            return 
                new StringBuilder()
                    .AppendLine(Html.Element("hr", string.Empty).Style(Styles[Style.HorizonalRule]).ToHtml())
                    .AppendLine(footer)
                    .ToString();
        }

        private static class Style
        {
            public const string Author = nameof(Author);
            public const string Paragraph = nameof(Paragraph);
            public const string HorizonalRule = nameof(HorizonalRule);
        }
    }
}