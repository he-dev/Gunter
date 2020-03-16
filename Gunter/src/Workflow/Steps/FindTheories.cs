using System.Linq;
using System.Threading.Tasks;
using Gunter.Workflow.Data;
using Reusable;
using Reusable.Extensions;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Annotations;
using Reusable.IO;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Workflow.Steps
{
    internal class FindTheories : Step<SessionContext>
    {
        public FindTheories(ILogger<FindTheories> logger, IDirectoryTree directoryTree) : base(logger)
        {
            DirectoryTree = directoryTree;
        }

        private IDirectoryTree DirectoryTree { get; set; }

        protected override Task<bool> ExecuteBody(SessionContext context)
        {
            context.TheoryNames =
                DirectoryTree
                    .Walk(context.TheoryDirectoryName, DirectoryTreePredicates.MaxDepth(1), PhysicalDirectoryTree.IgnoreExceptions)
                    .WhereFiles(@"\.json$")
                    // .Where(node =>
                    // {
                    //     if (node.DirectoryName.Matches(context.TestFilter.DirectoryNamePatterns, RegexOptions.IgnoreCase))
                    //     {
                    //         return new DirectoryTreeNodeView();
                    //     }
                    //
                    //     context.TestFilter.DirectoryNamePatterns.Any(p => node.DirectoryName.Matches(p, RegexOptions.IgnoreCase));
                    //     context.TestFilter.FileNamePatterns.w
                    // })
                    .FullNames()
                    .ToHashSet(SoftString.Comparer);

            return true.ToTask();
        }
    }
}