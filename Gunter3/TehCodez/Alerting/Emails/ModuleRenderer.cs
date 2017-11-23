﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gunter.Data;
using Gunter.Reporting;
using JetBrains.Annotations;
using Reusable.MarkupBuilder;
using Reusable.MarkupBuilder.Html;

namespace Gunter.Alerting.Emails
{
    [PublicAPI]
    public interface IModuleRenderer
    {
        bool CanRender(IModule module);
        IEnumerable<IHtmlElement> Render([NotNull] IModule module, [NotNull] TestContext context);
    }

    public abstract class ModuleRenderer : IModuleRenderer
    {
        public bool CanRender(IModule module)
        {
            var moduleType = module.GetType();
            return
                GetType()
                    .GetCustomAttribute<CanRenderAttribute>()
                    .Any(type => type.IsAssignableFrom(moduleType));
        }

        public abstract IEnumerable<IHtmlElement> Render(IModule module, TestContext context);

        protected IHtmlElement Html => HtmlElement.Builder;
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CanRenderAttribute : Attribute, IEnumerable<Type>
    {
        private readonly IEnumerable<Type> _renderables;

        public CanRenderAttribute(params Type[] renderables) => _renderables = renderables;

        public bool Contains(Type type) => _renderables.Contains(type);

        public IEnumerator<Type> GetEnumerator() => _renderables.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}