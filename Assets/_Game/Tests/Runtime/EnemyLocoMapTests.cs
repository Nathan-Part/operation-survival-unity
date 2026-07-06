using Game.Data;
using Game.Runtime;
using NUnit.Framework;

namespace Game.Runtime.Tests
{
    public class EnemyLocoMapTests
    {
        [Test]
        public void MapsFsmStatesToClipRoles()
        {
            Assert.AreEqual(AnimRole.Idle, EnemyLocoMap.RoleFor(EnemyState.Idle));
            Assert.AreEqual(AnimRole.Run, EnemyLocoMap.RoleFor(EnemyState.Chase));
            Assert.AreEqual(AnimRole.Attack, EnemyLocoMap.RoleFor(EnemyState.Attack));
            Assert.AreEqual(AnimRole.Dead, EnemyLocoMap.RoleFor(EnemyState.Dead));
        }
    }
}
