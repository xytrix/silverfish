﻿using System;
using System.Collections.Generic;
using System.Text;

namespace HREngine.Bots
{
    class Sim_AT_098 : SimTemplate //Sideshow Spelleater
    {

        //Battlecry:Copy your opponent's Hero Power.

        public override void getBattlecryEffect(Playfield p, Minion own, Minion target, int choice)
        {

            if (own.own)
            {
                p.ownHeroAblility = new Handmanager.Handcard(p.enemyHeroAblility.card);

            }
            else
            {
                p.enemyHeroAblility = new Handmanager.Handcard(p.ownHeroAblility.card);
            }

            if (own.own == p.isOwnTurn) p.heroPowerActivationsThisTurn = 0;

        }

       

    }
}