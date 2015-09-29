using System;
using System.Collections.Generic;
using System.Text;

namespace HREngine.Bots
{
    class Sim_BRM_002 : SimTemplate //Flamewaker
    {


        //    After you cast a spell, deal 2 damage randomly split among all enemies.

        public override void onCardWasPlayed(Playfield p, CardDB.Card c, bool wasOwnCard, Minion triggerEffectMinion)
        {
            if (triggerEffectMinion.own == wasOwnCard)
            {
                if (p.isServer)
                {
                    int timesS = 2;
                    for (int iS = 0; iS < timesS; iS++)
                    {
                        Minion poortarget = p.getRandomMinionFromSide_SERVER(!wasOwnCard, true);
                        if (poortarget != null) p.minionGetDamageOrHeal(poortarget, 1);
                    }
                    return;
                }

                List<Minion> targets = (triggerEffectMinion.own) ? new List<Minion>(p.enemyMinions) : new List<Minion>(p.ownMinions);

                if (triggerEffectMinion.own)
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
                while (i < 2)
                {
                    int loc = i % targets.Count;
                    p.minionGetDamageOrHeal(targets[loc], 1);
                    i++;
                }

                triggerEffectMinion.stealth = false;
            }
        }

    }
}