namespace HREngine.Bots
{
    using System;
    using System.Collections.Generic;

    public class EnemyTurnSimulator
    {

        public int thread = 0;

        private List<Playfield> posmoves = new List<Playfield>(7000);
        //public int maxwide = 20;
        Movegenerator movegen = Movegenerator.Instance;
        public int maxwide = 20;

        private PenalityManager penmanager = PenalityManager.Instance;

        public void setMaxwideFirstStep(bool firstTurn)
        {
            maxwide = Settings.Instance.enemyTurnMaxWide;
            if (!firstTurn) maxwide = Settings.Instance.enemyTurnMaxWide;
        }

        public void setMaxwideSecondStep(bool firstTurn)
        {
            maxwide = Settings.Instance.enemyTurnMaxWideSecondTime;
            if (!firstTurn) maxwide = Settings.Instance.enemyTurnMaxWide;
        }

        public void simulateEnemysTurn(Playfield rootfield, bool simulateTwoTurns, bool playaround, bool print, int pprob, int pprob2)
        {

            bool havedonesomething = true;
            posmoves.Clear();
            if (print)
            {
                Helpfunctions.Instance.ErrorLog("board at enemyturn start-----------------------------");
                rootfield.printBoard();
            }
            posmoves.Add(new Playfield(rootfield));
            //posmoves[0].prepareNextTurn(false);
            List<Playfield> temp = new List<Playfield>();
            int deep = 0;

            //playing aoe-effects if activated (and we didnt play loatheb)
            if (playaround && rootfield.ownloatheb == 0)
            {
                Playfield aoeField = new Playfield(rootfield);
                float oldval = Ai.Instance.botBase.getPlayfieldValue(aoeField);
                aoeField.value = int.MinValue;
                aoeField.mana = aoeField.EnemyCardPlaying(rootfield.enemyHeroName, rootfield.mana, rootfield.enemyAnzCards, pprob, pprob2);
                float newval = Ai.Instance.botBase.getPlayfieldValue(aoeField);
                aoeField.value = int.MinValue;
                aoeField.enemyAnzCards--;
                aoeField.triggerCardsChanged(false);
                if (oldval > newval) posmoves.Add(aoeField);
            }



            //play ability!
            for (int n = 0; n < posmoves.Count; n++)  // allows playing multiple hero abilities (TGT) + ability after AoE w/ playaround
            {
                Playfield pipi = posmoves[n];
                if (pipi.enemyAbilityReady && pipi.enemyHeroAblility.card.canplayCard(pipi, 2) && pipi.ownSaboteur == 0)
                {
                    if (penmanager.TargetAbilitysDatabase.ContainsKey(pipi.enemyHeroAblility.card.name))
                    {
                        List<Minion> trgts = pipi.enemyHeroAblility.card.getTargetsForCardEnemy(pipi);
                        foreach (Minion trgt in trgts)
                        {
                            Action a = new Action(actionEnum.useHeroPower, pipi.enemyHeroAblility, null, 0, trgt, 0, 0);
                            Playfield pf = new Playfield(pipi);
                            pf.doAction(a);
                            posmoves.Add(pf);
                        }
                    }
                    else
                    {
                        // the other classes dont have to target####################################################
                        if ((pipi.enemyHeroName == HeroEnum.thief && pipi.enemyWeaponDurability == 0) || pipi.enemyHeroName != HeroEnum.thief || pipi.enemyMinions.Find(m => m.handcard.card.hasInspire) != null)
                        {
                            Action a = new Action(actionEnum.useHeroPower, pipi.enemyHeroAblility, null, 0, null, 0, 0);
                            Playfield pf = new Playfield(pipi);
                            pf.doAction(a);
                            posmoves.Add(pf);
                        }
                    }

                }
            }

            //kill to strong minions with low hp

            /*if (enemMana >= 4)
            {
                foreach (Playfield pf in posmoves)
                {
                    Minion lowest = null;
                    foreach (Minion m in pf.ownMinions)
                    {
                        if (m.Angr >= 4 && m.Hp <= 2 && m.Hp>=1)
                        {
                            pf.minionGetDamageOrHeal(m, 100);
                            if (lowest == null || lowest.Angr <= m.Angr)
                            {
                            //    lowest = m;
                            }
                        }
                    }
                    pf.doDmgTriggers();
                    if (lowest != null)
                    {
                        pf.minionGetDamageOrHeal(lowest, lowest.Hp);
                        pf.doDmgTriggers();
                    }
                }
            }*/

            foreach (Minion m in posmoves[0].enemyMinions)
            {
                if (m.Angr == 0) continue;
                m.numAttacksThisTurn = 0;
                m.playedThisTurn = false;
                m.updateReadyness();
            }

            //might be more than just one
            foreach (Playfield pipi in posmoves)
            {
                doSomeBasicEnemyAi(pipi);
            }

            int boardcount = 0;
            //movegen...

            int i = 0;
            int count = 0;
            Playfield p = null;

            while (havedonesomething)
            {

                temp.Clear();
                temp.AddRange(posmoves);
                havedonesomething = false;
                Playfield bestold = null;
                float bestoldval = 20000000;

                //foreach (Playfield p in temp)
                count = temp.Count;
                for (i = 0; i < count; i++)
                {
                    p = temp[i];
                    if (p.complete)
                    {
                        continue;
                    }

                    List<Action> actions = movegen.getEnemyMoveList(p, false, true, true, 1);// 1 for not using ability moves

                    foreach (Action a in actions)
                    {
                        havedonesomething = true;
                        Playfield pf = new Playfield(p);
                        pf.doAction(a);
                        posmoves.Add(pf);

                        /*if (print)
                        {
                            a.print();
                        }*/
                        boardcount++;
                    }

                    p.endEnemyTurn();
                    //p.guessingHeroHP = rootfield.guessingHeroHP;
                    if (Ai.Instance.botBase.getPlayfieldValueEnemy(p) < bestoldval) // want the best enemy-play-> worst for us
                    {
                        bestoldval = Ai.Instance.botBase.getPlayfieldValueEnemy(p);
                        bestold = p;
                    }
                    posmoves.Remove(p);

                    if (boardcount >= maxwide) break;
                }

                if (bestoldval <= 10000 && bestold != null)
                {
                    posmoves.Add(bestold);
                }

                deep++;
                if (boardcount >= maxwide) break;
            }

            //foreach (Playfield p in posmoves)
            count = posmoves.Count;
            for (i = 0; i < count; i++)
            {

                if (!posmoves[i].complete) posmoves[i].endEnemyTurn();
            }

            float bestval = int.MaxValue;
            Playfield bestplay = rootfield;// posmoves[0];

            //foreach (Playfield p in posmoves)
            count = posmoves.Count;
            for (i = 0; i < count; i++)
            {
                p = posmoves[i];
                //p.guessingHeroHP = rootfield.guessingHeroHP;
                float val = Ai.Instance.botBase.getPlayfieldValueEnemy(p);
                if (bestval > val)// we search the worst value
                {
                    bestplay = p;
                    bestval = val;
                }
                /*if (print)
                {
                    Helpfunctions.Instance.ErrorLog(""+val);
                    p.printBoard();
                }*/
            }
            if (print)
            {
                Helpfunctions.Instance.ErrorLog("best enemy board----------------------------------");
                bestplay.printBoard();
            }
            rootfield.value = bestplay.value;
            if (simulateTwoTurns && bestplay.ownHero.Hp > 0 && bestplay.value > -1000)
            {
                bestplay.prepareNextTurn(true);
                rootfield.value = Settings.Instance.firstweight * bestval + Settings.Instance.secondweight * Ai.Instance.nextTurnSimulator[this.thread].doallmoves(bestplay, false, print);
            }


        }

        CardDB.Card flame = CardDB.Instance.getCardDataFromID(CardDB.cardIDEnum.EX1_614t);
        //CardDB.Card warsong = CardDB.Instance.getCardDataFromID(CardDB.cardIDEnum.EX1_084);// RIP little friend
        CardDB.Card warriorweapon = CardDB.Instance.getCardDataFromID(CardDB.cardIDEnum.CS2_106);

        private void doSomeBasicEnemyAi(Playfield p)
        {
            if (p.enemyHeroName == HeroEnum.mage)
            {
                if (Probabilitymaker.Instance.enemyCardsPlayed.ContainsKey(CardDB.cardIDEnum.EX1_561)) p.ownHero.Hp = Math.Max(5, p.ownHero.Hp - 7);
            }

            //play some cards (to not overdraw)
            if (p.enemyAnzCards >= 8)
            {
                p.enemyAnzCards--;
                p.triggerCardsChanged(false);
            }
            if (p.enemyAnzCards >= 4)
            {
                p.enemyAnzCards--;
                p.triggerCardsChanged(false);
            }
            if (p.enemyAnzCards >= 2)
            {
                p.enemyAnzCards--;
                p.triggerCardsChanged(false);
            }
            
            //if warrior, equip a weapon
            if (p.enemyHeroName == HeroEnum.warrior && p.enemyWeaponDurability == 0 && p.mana >= 4)
            {
                p.equipWeapon(this.warriorweapon, false);
                if (p.ownHero.Hp>=1 && p.ownHero.Hp <= p.ownHero.maxHp - 3)  p.ownHero.Hp += 3; //to not change lethal
            }
            if (p.enemyHeroName == HeroEnum.thief && p.enemyWeaponDurability != 0 && p.mana >= 4)
            {
                p.enemyWeaponAttack++;
                if (p.ownHero.Hp >= 1 && p.ownHero.Hp <= p.ownHero.maxHp - 1) p.ownHero.Hp += 1;//to not change lethal
            }
            if (p.enemyHeroName == HeroEnum.pala && p.enemyWeaponDurability == 0 && p.mana >= 4)
            {
                p.equipWeapon(this.warriorweapon, false);//warrion weapon is ok for pala 
                if (p.ownHero.Hp >= 1 && p.ownHero.Hp <= p.ownHero.maxHp - 3) p.ownHero.Hp += 3;//to not change lethal
            }

            //int i = 0;
            //int count = 0;
            bool hasmech=false;
            foreach (Minion m in p.enemyMinions)
            {
                if(m.handcard.card.race == TAG_RACE.MECHANICAL) hasmech=true;
            }
            

            foreach (Minion m in p.enemyMinions.ToArray())
            {
                if (m.silenced)
                    continue;

                switch (m.name)
                {
                    /*case CardDB.cardName.grimpatron:
                        if(p.enemyMinions.Count<=6 && p.enemyHeroName == HeroEnum.warrior)
                        {
                            bool hascharger = false;
                            foreach (Minion mini in p.enemyMinions)
                            {
                                if (!mini.silenced && mini.name == CardDB.cardName.warsongcommander) hascharger=true;
                            }
                            if (!hascharger)
                            {
                                p.callKid(warsong, p.enemyMinions.Count, false);
                            }

                        }
                        break;
                    */
                    case CardDB.cardName.fjolalightbane:
                        if (p.enemyAnzCards >= 2 && p.mana>=2)
                        {
                            m.divineshild = true;
                            //p.mana -= 2;
                        }
                        break;

                    case CardDB.cardName.junkbot:
                        if (hasmech)
                        {
                            p.minionGetBuffed(m, 2, 2);
                        }
                        break;

                    case CardDB.cardName.siegeengine:
                        if (p.enemyHeroName == HeroEnum.warrior)
                        {
                            p.minionGetBuffed(m, 1, 0);
                        }
                        break;

                    case CardDB.cardName.gahzrilla:
                        if (m.Hp >= 2)
                        {
                            p.minionGetBuffed(m, m.Angr, 0);
                        }
                        break;
                        //draw cards if he has gadgetzanauctioneer or starving buzzard
                    case CardDB.cardName.gadgetzanauctioneer:
                    case CardDB.cardName.starvingbuzzard:
                        if (p.enemyAnzCards >= 2 && p.enemyDeckSize >= 1)
                        {
                            p.drawACard(CardDB.cardIDEnum.None, false);
                        }
                        break;

                        //if there is something to heal... draw a card with northshirecleric
                    case CardDB.cardName.northshirecleric:
                        {
                            //if (p.mana <= 2) break;
                            //p.mana -= 2;
                            int anz = 0;
                            foreach (Minion mnn in p.enemyMinions)
                            {
                                if (mnn.wounded) anz++;
                            }
                            foreach (Minion mnn in p.ownMinions)
                            {
                                if (mnn.wounded) anz++;
                            }
                            //anz = Math.Min(anz, 3);
                            if (anz > 0 && p.enemyDeckSize >= 1)
                            {
                                p.drawACard(CardDB.cardIDEnum.None, false);
                            }
                            break;
                        }

                        // spawn new minion when he have illidan
                    case CardDB.cardName.illidanstormrage:
                        if (p.enemyAnzCards >= 1)
                        {
                            p.callKid(flame, p.enemyMinions.Count, false);
                        }
                        break;

                        //buff his questingadventurer
                    case CardDB.cardName.questingadventurer:
                        if (p.enemyAnzCards >= 1)
                        {
                            p.minionGetBuffed(m, 1, 1);
                            if (p.enemyAnzCards >= 3 && p.enemyMaxMana >= 5)
                            {
                                p.minionGetBuffed(m, 1, 1);
                            }
                        }
                        break;

                        //buff his manaaddict
                    case CardDB.cardName.manaaddict:
                        if (p.enemyAnzCards >= 1)
                        {
                            p.minionGetTempBuff(m, 2, 0);
                            if (p.enemyAnzCards >= 3 && p.enemyMaxMana >= 5)
                            {
                                p.minionGetTempBuff(m, 2, 0);
                            }
                        }
                        break;

                    case CardDB.cardName.manawyrm:
                        if (p.enemyAnzCards >= 1)
                        {
                            p.minionGetBuffed(m, 1, 0);
                            if (p.enemyAnzCards >= 3 && p.enemyMaxMana >= 5)
                            {
                                p.minionGetBuffed(m, 1, 0);
                            }
                        }
                        break;

                    case CardDB.cardName.tinyknightofevil:
                    case CardDB.cardName.crowdfavorite:
                    case CardDB.cardName.secretkeeper:
                    case CardDB.cardName.unboundelemental:
                        if (p.enemyAnzCards >= 2)
                        {
                            p.minionGetBuffed(m, 1, 1);
                        }
                        break;

                    case CardDB.cardName.floatingwatcher:
                        if (p.enemyHeroName == HeroEnum.warlock && p.enemyAnzCards >= 3)  // hero power use is covered in dmgTriggers()
                        {
                            p.minionGetBuffed(m, 2, 2);
                        }
                        break;

                    case CardDB.cardName.murloctidecaller:
                    case CardDB.cardName.undertaker:
                        if (p.enemyAnzCards >= 2)
                        {
                            p.minionGetBuffed(m, 1, 0);
                            if (p.enemyAnzCards >= 4 && p.enemyMaxMana >= 4)
                            {
                                p.minionGetBuffed(m, 1, 0);
                            }
                        }
                        break;

                    case CardDB.cardName.frothingberserker:
                        if (p.enemyMinions.Count + p.ownMinions.Count >= 3)
                        {
                            p.minionGetBuffed(m, 1, 0);
                        }
                        break;

                    case CardDB.cardName.gurubashiberserker:
                        if (m.Hp >= 2 && (p.enemyAnzCards >= 1 || p.enemyHeroName == HeroEnum.mage ||
                            (p.anzEnemyAuchenaiSoulpriest > 0 && p.enemyHeroName == HeroEnum.priest)
                            || (p.enemyHeroName == HeroEnum.priest && p.enemyHeroAblility.card.name != CardDB.cardName.lesserheal && p.enemyHeroAblility.card.name != CardDB.cardName.heal))) // shadow form
                        {
                            p.minionGetBuffed(m, 3, 0);
                        }
                        break;

                    case CardDB.cardName.holychampion:
                    case CardDB.cardName.lightwarden:
                        {
                            int anz = 0;
                            foreach (Minion mnn in p.enemyMinions)
                            {
                                if (mnn.wounded) anz++;
                            }
                            if (p.enemyHero.wounded) anz++;
                            if (anz > 0) p.minionGetBuffed(m, 2, 0);
                            break;
                        }
                }
            }

            //enemy will shure play a minion
            if (p.enemyMinions.Count < 7 && p.mana>=2)
            {
                p.callKid(this.flame, p.enemyMinions.Count, false);
                int bval = 1;  // 2mana => 2/2
                if (p.mana > 3) bval = 2; // 3mana => 3/3
                if (p.mana > 4) bval = 3; // 4mana => 4/4
                if (p.mana > 5) bval = 4; // 5mana => 5/5
                if (p.mana > 6) bval = 5; // 6+ => 6/6
                if (p.enemyMinions.Count >= 1)
                {
                    p.minionGetBuffed(p.enemyMinions[p.enemyMinions.Count - 1], bval - 1, bval);
                    p.enemyMinions[p.enemyMinions.Count - 1].cantBeTargetedBySpellsOrHeroPowers = true;  // prevent the bot from assuming it can efficiently remove whatever this minion is with spells
                }
            }
        }


    }

}