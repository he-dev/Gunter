﻿using System;
using System.IO;
using Autofac;
using Gunter.Data;
using Gunter.Messaging.Email;
using Gunter.Messaging.Email.ModuleRenderers;
using Gunter.Services;
using Reusable.Logging;
using Reusable.Logging.Loggex;
using Reusable.Markup.Html;

namespace Gunter.AutofacModules
{
    internal class HtmlModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<GreetingRenderer>()
                .As<ModuleRenderer>();

            builder
                .RegisterType<TableRenderer>()
                .As<ModuleRenderer>();

            builder
                .RegisterType<SignatureRenderer>()
                .As<ModuleRenderer>();

            builder
                .RegisterType<CssInliner>();
            //.As<ICssInliner>();

            builder
                .RegisterType<SimpleCssParser>()
                .As<ICssParser>();

            builder
                .Register<Func<string, Css>>(c =>
                {
                    var context = c.Resolve<IComponentContext>();

                    return cssFileName =>
                    {
                        cssFileName = Path.Combine(context.Resolve<Workspace>().Themes, cssFileName);
                        var cssFullName = context.Resolve<IPathResolver>().ResolveFilePath(cssFileName);
                        var fileSystem = context.Resolve<IFileSystem>();
                        var css = context.Resolve<ICssParser>().Parse(fileSystem.ReadAllText(cssFullName));
                        return css;
                    };
                });

            builder
                .RegisterType<HtmlEmail>()
                .WithParameter(new TypedParameter(typeof(ILogger), Logger.Create<HtmlEmail>()));
        }
    }
}