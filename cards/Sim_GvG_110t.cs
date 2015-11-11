using System;
using System.Collections.Generic;
using System.Text;

namespace HREngine.Bots
{
    class Sim_GVG_110t : SimTemplate //Boom Bot
    {

        //  Deathrattle: Deal 1-4 damage to a random enemy.

        

        public override void onDeathrattle(Playfield p, Minion m)
        {
            if (p.isServer)
            {
                int randdmg = p.getRandomNumber_SERVER(1, 4);
                Minion poortarget = p.getRandomMinionFromSide_SERVER(!m.own, true);
                if (poortarget != null) p.minionGetDamageOrHeal(poortarget, randdmg);
                return;
            }

            List<Minion> temp = (m.own ? p.enemyMinions : p.ownMinions);
            int dmg = (m.own ? 2 : 3);  // not exactly best/worse case scenario damage, but above average-case
            Minion target = p.searchRandomMinionForDamage(temp, dmg, m.own, true);  // will pick a best/worst-case scenario minion however
            p.minionGetDamageOrHeal(target, dmg);

        }


    }

}