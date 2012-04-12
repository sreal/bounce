using System.Collections.Generic;
using System.Linq;

namespace Bounce.Framework.Obsolete {
    public static class TargetsRetrieverExtensions {
        public static IEnumerable<Target> ToTargets(this IDictionary<string, IObsoleteTask> targetsDictionary) {
            return targetsDictionary.Select(t => new Target { Name = t.Key, Task = t.Value });
        }
    }
}
