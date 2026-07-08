using Game.Data.Rules;
using NUnit.Framework;

namespace Game.Data.Tests
{
    public class MapRulesTests
    {
        [Test]
        public void SceneNameFor_Utopia_ReturnsUtopiaScene()
        {
            Assert.AreEqual("Utopia", MapRules.SceneNameFor("utopia"));
        }

        [Test]
        public void SceneNameFor_UnavailableOrUnknownMaps_FallBackToUtopia()
        {
            Assert.AreEqual("Utopia", MapRules.SceneNameFor("portal"));
            Assert.AreEqual("Utopia", MapRules.SceneNameFor("urban"));
            Assert.AreEqual("Utopia", MapRules.SceneNameFor(null));
            Assert.AreEqual("Utopia", MapRules.SceneNameFor("inconnu"));
        }

        [Test]
        public void IsAvailable_TrueOnlyForUtopia()
        {
            Assert.IsTrue(MapRules.IsAvailable("utopia"));
            Assert.IsFalse(MapRules.IsAvailable("urban"));
            Assert.IsFalse(MapRules.IsAvailable("portal"));
            Assert.IsFalse(MapRules.IsAvailable(null));
            Assert.IsFalse(MapRules.IsAvailable("inconnu"));
        }
    }
}
