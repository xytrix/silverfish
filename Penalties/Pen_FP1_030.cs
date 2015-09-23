using System;
using System.Collections.Generic;
using System.Text;

namespace HREngine.Bots
{
	class Pen_FP1_030 : PenTemplate //loatheb
	{
		public override float getPlayPenalty(Playfield p, Handmanager.Handcard hc, Minion target, int choice, bool isLethal)
		{
            // penalty for playing Loatheb as a vanilla 5/5, i.e. on an empty board (should hold for offensive finishing or preventing AoE/removal)
            if (p.ownHero.Hp + p.ownHero.armor > 15 && p.enemyMinions.Count == 0 && p.ownMinions.Count < 3)
                return 10;

			return 0;
		}
	}
}
