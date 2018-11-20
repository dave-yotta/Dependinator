using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dependinator.Test
{
    [TestFixture]
    public class Tests
    {
        public static IEnumerable<ResolverTestCase> Cases()
        {
            var root = Path.GetDirectoryName(typeof(Tests).Assembly.Location);
            var tests = JArray.Parse(File.ReadAllText(Path.Combine(root, "ResolverTests.Json")));
            foreach (var token in tests)
            {
                yield return token.ToObject<ResolverTestCase>();
            }
        }

        [TestCaseSource("Cases")]
        public async Task JsonTest(ResolverTestCase tc)
        {
            var strategy = new TestAdvStrategy(tc.Arrange.Select(c => new TestState(c)).ToArray());

            int advancment = 0;
            strategy.OnAdvance(states =>
            {
                CollectionAssert.AreEqual(states.Select(x => x.Model.Id), tc.Assert[advancment].ToAdvance);
                advancment++;
            });

            var resolver = new Resolver<TestNode>(strategy);

            await resolver.Resolve();
        }
    }
}
