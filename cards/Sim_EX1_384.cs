using System;
using System.Collections.Generic;
using System.Text;

namespace HREngine.Bots
{
    class Sim_EX1_384 : SimTemplate //avengingwrath
    {

        //    verursacht $8 schaden, der zufällig auf feindliche charaktere verteilt wird.
        //todo for enemy
        public override void onCardPlay(Playfield p, bool ownplay, Minion target, int choice)
        {

            if (p.isServer)
            {
                int timesS = (ownplay) ? p.getSpellDamageDamage(8) : p.getEnemySpellDamageDamage(8);
                for (int iS = 0; iS < timesS; iS++)
                {
                    Minion poortarget = p.getRandomMinionFromSide_SERVER(!ownplay, true);
                    if (poortarget != null) p.minionGetDamageOrHeal(poortarget, 1);
                }
                return;
            }

            List<Minion> targets = (ownplay) ? new List<Minion>(p.enemyMinions) : new List<Minion>(p.ownMinions);
            int times = (ownplay) ? p.getSpellDamageDamage(8) : p.getEnemySpellDamageDamage(8);

            if (ownplay)
            {
                targets.Add(p.enemyHero);
                targets.Sort((a, b) => -a.Hp.CompareTo(b.Hp));  // most hp -> least
            }
            else
            {
                targets.Add(p.ownHero);
                targets.Sort((a, b) => a.Hp.CompareTo(b.Hp));  // least hp -> most
            }

            // Distribute the damage evenly among the targets
            int i = 0;
            while (i < times)
            {
                int loc = i % targets.Count;
                p.minionGetDamageOrHeal(targets[loc], 1);
                i++;
            }
        }

    }

}