﻿using Gunter.Data;
using Gunter.Services;
using Newtonsoft.Json;
using Reusable.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using Gunter.Data.Email;
using Gunter.Data.Email.Templates;
using Gunter.Data.Sections;
using Reusable;

namespace Gunter.Alerts
{
    public class EmailAlert : Alert
    {
        private static readonly Dictionary<Type, ISectionTemplate> _sectionTemplates = new Dictionary<Type, ISectionTemplate>
        {
            [typeof(TextSection)] = new TextTemplate(),
            [typeof(TableSection)] = new TableTemplate(),
        };

        private readonly FooterTemplate _footerRenderer = new FooterTemplate();

        public EmailAlert(ILogger logger) : base(logger) { }

        [JsonRequired]
        public string To { get; set; }

        [JsonRequired]
        public IEmailClient EmailClient { get; set; }

        protected override void PublishCore(IEnumerable<ISection> sections, IConstantResolver constants)
        {
            var body = sections.Select(section => _sectionTemplates[section.GetType()].Render(section, constants)).ToList();
            body.Add(_footerRenderer.Render("Gunter", DateTime.UtcNow));

            var email = new AlertEmail
            {
                Subject = new AlertEmailSubject(constants.Resolve(Title)),
                Body = new AlertEmailBody
                {
                    Sections = body
                },
                To = constants.Resolve(To)
            };
            EmailClient.Send(email);
        }
    }

}
