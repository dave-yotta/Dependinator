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
            List<List<int>> actuals = new List<List<int>>();
            strategy.OnAdvance(states =>
            {
                var actual = states.Select(x => x.Model.Id).ToList();
                actuals.Add(actual);
                advancment++;
            });

            var resolver = new Resolver<TestNode>(strategy);

            await resolver.Resolve();

            var expect = tc.Assert.Select(x => x.ToAdvance).ToList();

            var expectString = $"[{string.Join(", ", expect.Select(x => $"[{string.Join(", ", x)}]"))}]";
            var actualString = $"[{string.Join(", ", actuals.Select(x => $"[{string.Join(", ", x)}]"))}]";

            Assert.AreEqual(expectString, actualString, $"\r\nExpected: {expectString}\r\n     Actual: {actualString}");
        }
    }
}
