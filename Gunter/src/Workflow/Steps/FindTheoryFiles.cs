using System.Linq;
using System.Threading.Tasks;
using Gunter.Workflows;
using Reusable;
using Reusable.Flowingo.Abstractions;
using Reusable.Flowingo.Annotations;
using Reusable.IO;
using Reusable.OmniLog.Abstractions;

namespace Gunter.Workflow.Steps
{
    internal class FindTheoryFiles : Step<SessionContext>
    {
        [Service]
        public ILogger<FindTheoryFiles> Logger { get; set; }

        [Service]
        public IDirectoryTree DirectoryTree { get; set; }

        public override async Task ExecuteAsync(SessionContext context)
        {
            context.TestFileNames =
                DirectoryTree
                    .Walk(context.TestDirectoryName, DirectoryTreePredicates.MaxDepth(1), PhysicalDirectoryTree.IgnoreExceptions)
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

            await ExecuteNextAsync(context);
        }
    }
}