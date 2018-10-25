﻿using Autofac;
using Gunter.Messaging.Emails;
using Gunter.Messaging.Emails.Internal;
using Gunter.Messaging.Emails.Internal.Factories;

namespace Gunter.DependencyInjection.Internal
{
    internal class HtmlEmail : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<LevelFactory>()
                .As<IModuleFactory>();

            builder
                .RegisterType<GreetingFactory>()
                .As<IModuleFactory>();

            builder
                .RegisterType<TableFactory>()
                .As<IModuleFactory>();

            builder
                .RegisterType<SignatureFactory>()
                .As<IModuleFactory>();

            builder
                .RegisterType<Messaging.Emails.HtmlEmail>();
        }
    }
}