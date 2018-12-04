using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Providers;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker_Tests
{
    [TestFixture]
    internal class ProvidersTest
    {
        private static IDictionary<int, Position> TestData = new Dictionary<int, Position>
        {
            [6] = Position.Top, //Urgot
            [64] = Position.Jungle, //Lee Sin
            [84] = Position.Mid, //Akali
            [18] = Position.Bottom, //Tristana
            [16] = Position.Support //Soraka
        };

        private static object[] GetTestProviders(Provider.Options options)
        {
            return typeof(Provider).Assembly.GetTypes()
                .Where(o => o.BaseType == typeof(Provider) && o != typeof(ClientProvider))
                .Select(o => (Provider)Activator.CreateInstance(o))
                .Where(o => o.Supports(options))
                .SelectMany(o => TestData.Select(i => new object[] { o, i.Key, i.Value }))
                .ToArray();
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            WebCache.InTestMode = true;
        }

        [TestCaseSource(nameof(GetTestProviders), new object[] { Provider.Options.RunePages })]
        public async Task RunePages(Provider provider, int champion, Position position)
        {
            var page = await provider.GetRunePage(champion, position);

            Assert.AreEqual(page.ChampionID, champion);
            Assert.AreNotEqual(page.PrimaryTree, 0);
            Assert.AreNotEqual(page.SecondaryTree, 0);
            Assert.IsTrue(page.RuneIDs.Length == 9 || page.RuneIDs.Length == 6);
            Assert.IsTrue(page.RuneIDs.All(o => o > 0));
        }

        [TestCaseSource(nameof(GetTestProviders), new object[] { Provider.Options.ItemSets })]
        public async Task ItemSets(Provider provider, int champion, Position position)
        {
            var set = await provider.GetItemSet(champion, position);

            Assert.AreEqual(set.Champion, champion);
        }

        [TestCaseSource(nameof(GetTestProviders), new object[] { Provider.Options.SkillOrder })]
        public async Task SkillOrder(Provider provider, int champion, Position position)
        {
            var order = await provider.GetSkillOrder(champion, position);

            Assert.IsNotNull(order);
            Assert.IsNotEmpty(order.Trim());
        }
    }
}
