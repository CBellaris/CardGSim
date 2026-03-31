using System.Collections;
using Cards.Core;
using UnityEngine;

namespace Cards.Actions
{
    public class DamageAction : GameAction
    {
        private ICombatable target;
        private int damage;
        private string sourceName;

        public DamageAction(ICombatable target, int damage, string sourceName = "Unknown")
        {
            this.target = target;
            this.damage = damage;
            this.sourceName = sourceName;
        }

        public override IEnumerator ExecuteRoutine()
        {
            // 1. 可以在这里播放前摇动画、特效等 (这里用 Log 代替)
            Debug.Log($"[Action] {sourceName} 的攻击即将命中 {target.CombatName}...");
            
            // 模拟飞弹/攻击特效飞行的时间
            yield return new WaitForSeconds(0.2f);

            // 2. 执行核心数据扣血
            target.TakeDamage(damage);

            // 3. 可以在这里播放受击特效、飘字等
            // 模拟受击表现的时间硬直
            yield return new WaitForSeconds(0.3f);

            IsCompleted = true;
        }
    }
}