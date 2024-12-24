namespace WindBot.Game.AI.Decks;

using System;
using System.Collections.Generic;
using System.Linq;
using WindBot.Game;
using YGOSharp.OCGWrapper.Enums;

[Deck("Catenicorum", "AI_Catenicorum")]
public sealed class CatenicorumExecutor : DefaultExecutor
{
    public sealed class CardId
    {
        public const int EtherealBeast = 927210204;
        public const int Serpent = 927210203;
        public const int Manipulator = 927210202;
        public const int Summoner = 927210200;
        public const int Shadow = 927210201;
        public const int Portal = 927210205;
        public const int Chains = 927210206;
        public const int Sanctum = 927210207;
        public const int Circle = 927210208;
        public const int Binding = 927210209;
        public const int PotOfProsperity = 84211599;
        public const int MulcharmyFuwalos = 42141493;
        public const int MulcharmyPurulia = 84192580;
        public const int RunedRecovery = 952510934;
        public const int ChangeOfHeart = 4031928;
        public const int SuperStarslayerTYPHON = 93039339;
        public const int HopeHarbingerDragonTitanicGalaxy = 63767246;
        public const int UtopiaBeyond = 21521304;
        public const int CrystalWingSynchroDragon = 50954680;
        public const int CyberseQuantumDragon = 63533837;
        public const int ClearWingSynchroDragon = 82044280;
        public const int GaiaBlazeForceOfTheSun = 9709452;
        public const int BorrelswordDragon = 85289965;
        public const int DecodeTalker = 1861629;
        public const int EriaWaterCharmerGentle = 73309655;
        public const int AussaEarthCharmerImmovable = 97661969;
        public const int HiitaFireCharmerAblaze = 48815792;
        public const int ArtemisMagistusMoonMaiden = 34755994;
    }

    public const long RuneMonsterType = 0x80000000;

    public const int RuneMaterialHint = 600;

    public const int CatenicorumSetCode = 0xfcf;

    private readonly List<int> CatenicorumPlaceFromGY = [
        CardId.Shadow,
        CardId.Portal,
        CardId.Chains,
        CardId.Sanctum,
        CardId.Circle,
        CardId.Binding
    ];

    private readonly List<int> CatenicorumRunes = [
        CardId.EtherealBeast,
        CardId.Serpent,
        CardId.Manipulator
    ];

    private readonly List<int> AvoidAllMaterials = [
        CardId.EtherealBeast,
        CardId.CrystalWingSynchroDragon,
        CardId.BorrelswordDragon,
    ];

    private readonly List<int> AvoidGenericMaterials = [
        CardId.Serpent,
        CardId.ClearWingSynchroDragon,
        CardId.CyberseQuantumDragon,
    ];

    private readonly List<int> ImpermanenceZonesThisTurn = [];

    private readonly List<int> SerpentNegated = [];

    private readonly List<int> UsedSpellTrapMaterial = [];

    private bool enemyActivateInfiniteImpermanenceFromHand = false;

    // For checking if we attempted to cancel already
    private bool attemptedRuneCancel = false;

    // For counting the number of materials used within a Rune Summon.
    private (int monsterCount, int spellCount, int bothCount) runeMaterialCount = (0, 0, 0);

    // For counting the number of in-archetype materials used for a Rune Summon.
    private int runeMaterialCatenicorumCount = 0;

    // Portal Rune Summon from Deck
    private bool portalRuneFromDeckIsUsed = false;

    // Extra Material Effects from Deck
    private bool portalExtraMaterialUsed = false;

    private bool shadowExtraMaterialUsed = false;

    private bool summonerExtraMaterialUsed = false;

    // Used as Rune Material Effects
    private Dictionary<int, bool> CatenicorumUsedAsMaterialFlags = new()
    {
        { CardId.Circle, false },
        { CardId.Chains, false },
        { CardId.EtherealBeast, false },
        { CardId.Manipulator, false },
        { CardId.Portal, false },
        { CardId.Sanctum, false },
        { CardId.Serpent, false },
        { CardId.Shadow, false },
        { CardId.Summoner, false },
    };

    public CatenicorumExecutor(GameAI ai, Duel duel)
        : base(ai, duel)
    {
        AvoidGenericMaterials.AddRange(AvoidAllMaterials);

        // Generic Interaction
        AddExecutor(ExecutorType.Activate, _CardId.AshBlossom, DefaultAshBlossomAndJoyousSpring);
        AddExecutor(ExecutorType.Activate, _CardId.InfiniteImpermanence, AvoidImpermanenceActivate(InfiniteImpermanenceActivate));
        AddExecutor(ExecutorType.Activate, _CardId.CalledByTheGrave, AvoidImpermanenceActivate(DefaultCalledByTheGrave));
        AddExecutor(ExecutorType.Activate, _CardId.CrystalWingSynchroDragon, InstantlyNegateEffect);
        AddExecutor(ExecutorType.Activate, CardId.ClearWingSynchroDragon, InstantlyNegateEffect);
        AddExecutor(ExecutorType.Activate, CardId.MulcharmyFuwalos, DefaultMaxxC);
        AddExecutor(ExecutorType.Activate, CardId.MulcharmyPurulia, DefaultMaxxC);
        AddExecutor(ExecutorType.Activate, CardId.DecodeTalker);

        // Always activate after negates
        AddExecutor(ExecutorType.Activate, CardId.Manipulator, CatenicorumManipulatorEffect);
        AddExecutor(ExecutorType.Activate, CardId.EtherealBeast);
        AddExecutor(ExecutorType.Activate, CardId.Serpent, CatenicorumSerpentEffect);

        // Going second blowouts
        AddExecutor(ExecutorType.Activate, _CardId.HarpiesFeatherDuster, AvoidImpermanenceActivate(DefaultHarpiesFeatherDusterFirst));
        AddExecutor(ExecutorType.Activate, CardId.ChangeOfHeart, AvoidImpermanenceActivate(() => true));
        AddExecutor(ExecutorType.Activate, CardId.SuperStarslayerTYPHON, SuperStarslayerTYPHONActivate);

        // Starters
        AddExecutor(ExecutorType.Activate, CardId.PotOfProsperity, AvoidImpermanenceActivate(PotofProsperityActivate));
        AddExecutor(ExecutorType.Activate, CardId.Circle, CatenicorumCircleEffect);
        AddExecutor(ExecutorType.Activate, CardId.RunedRecovery, AvoidImpermanenceActivate(RunedRecoveryEffectActivate));

        // Extenders
        AddExecutor(ExecutorType.Activate, CardId.AussaEarthCharmerImmovable);
        AddExecutor(ExecutorType.Activate, CardId.EriaWaterCharmerGentle);
        AddExecutor(ExecutorType.Activate, CardId.BorrelswordDragon, BorrelswordEffect);
        AddExecutor(ExecutorType.Activate, CardId.Summoner, CatenicorumSummonerEffect);
        AddExecutor(ExecutorType.Activate, CardId.Shadow);
        AddExecutor(ExecutorType.Activate, CardId.Binding, CatenicorumBindingEffect);
        AddExecutor(ExecutorType.Activate, CardId.Portal, AvoidImpermanenceActivate(() => true));
        AddExecutor(ExecutorType.Activate, CardId.Chains, CatenicorumChainsActivate);
        AddExecutor(ExecutorType.Activate, CardId.Sanctum);

        // Other effects, to always activate
        AddExecutor(ExecutorType.Activate, CardId.GaiaBlazeForceOfTheSun);
        AddExecutor(ExecutorType.Activate, CardId.CyberseQuantumDragon);
        AddExecutor(ExecutorType.Activate, CardId.HopeHarbingerDragonTitanicGalaxy);

        // Catenicorum Normal Summons
        AddExecutor(ExecutorType.Summon, CardId.Summoner);
        AddExecutor(ExecutorType.Summon, CardId.Shadow);

        // Priority Special Summons
        AddExecutor(ExecutorType.SpSummon, CardId.CrystalWingSynchroDragon); // We always want to summon Crystal Wing whenever it's possible.
        AddExecutor(ExecutorType.SpSummon, CardId.Manipulator, CatenicorumManipulatorRuneSummon);
        AddExecutor(ExecutorType.SpSummon, CardId.Serpent, CatenicorumSerpentRuneSummon);
        AddExecutor(ExecutorType.SpSummon, CardId.EtherealBeast, CatenicorumEtherealBeastRuneSummon);

        // Generic Special Summons
        AddExecutor(ExecutorType.SpSummon, CardId.CyberseQuantumDragon, CrystalWingRampSummon);
        AddExecutor(ExecutorType.SpSummon, CardId.ClearWingSynchroDragon, CrystalWingRampSummon);
        AddExecutor(ExecutorType.SpSummon, CardId.GaiaBlazeForceOfTheSun, CrystalWingRampSummon);
        AddExecutor(ExecutorType.SpSummon, CardId.UtopiaBeyond);
        AddExecutor(ExecutorType.SpSummon, CardId.HopeHarbingerDragonTitanicGalaxy);
        AddExecutor(ExecutorType.SpSummon, CardId.BorrelswordDragon, GenericLinkSummon);
        AddExecutor(ExecutorType.SpSummon, CardId.AussaEarthCharmerImmovable, CharmerSpecial(CardId.AussaEarthCharmerImmovable, CardAttribute.Earth));
        AddExecutor(ExecutorType.SpSummon, CardId.EriaWaterCharmerGentle, CharmerSpecial(CardId.EriaWaterCharmerGentle, CardAttribute.Water));
        AddExecutor(ExecutorType.SpSummon, CardId.HiitaFireCharmerAblaze, CharmerSpecial(CardId.HiitaFireCharmerAblaze, CardAttribute.Fire));
        AddExecutor(ExecutorType.SpSummon, CardId.DecodeTalker, GenericLinkSummon);
        AddExecutor(ExecutorType.SpSummon, CardId.SuperStarslayerTYPHON, SuperStarslayerTYPHONSpSummon);

        // Mulcharmy Normal Summons, if we can normal summon them. We can't activate their effects anyways so use them as material.
        AddExecutor(ExecutorType.Summon, CardId.MulcharmyPurulia);
        AddExecutor(ExecutorType.Summon, CardId.MulcharmyFuwalos);

        // Set Spell/Trap cards if they can't be activated.
        AddExecutor(ExecutorType.SpellSet, CardId.Circle);
        AddExecutor(ExecutorType.SpellSet, CardId.Binding);
        AddExecutor(ExecutorType.SpellSet, _CardId.InfiniteImpermanence);
        AddExecutor(ExecutorType.SpellSet, _CardId.CalledByTheGrave);
        AddExecutor(ExecutorType.SpellSet, CardId.Sanctum);
        AddExecutor(ExecutorType.SpellSet, CardId.Portal);
        AddExecutor(ExecutorType.SpellSet, CardId.Chains);
        AddExecutor(ExecutorType.SpellSet, CardId.ChangeOfHeart);
        AddExecutor(ExecutorType.SpellSet, _CardId.HarpiesFeatherDuster);
        AddExecutor(ExecutorType.SpellSet, CardId.PotOfProsperity);
    }

    public override void OnChaining(int player, ClientCard card)
    {
        if (player == 1)
        {
            if (card.IsOriginalCode(_CardId.InfiniteImpermanence))
            {
                if (enemyActivateInfiniteImpermanenceFromHand)
                {
                    enemyActivateInfiniteImpermanenceFromHand = false;
                }
                else
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        if (Enemy.SpellZone[i] == card)
                        {
                            ImpermanenceZonesThisTurn.Add(4 - i);
                            break;
                        }
                    }
                }
            }
        }

        base.OnChaining(player, card);
    }

    public override void OnChainSolved(int chainIndex)
    {
        var card = Duel.CurrentChain[chainIndex];

        switch (card.GetOriginCode())
        {
            case _CardId.InfiniteImpermanence:
                ImpermanenceZonesThisTurn.Add(4 - card.Sequence);
                break;
        }

        base.OnChainSolved(chainIndex);
    }

    public override void OnChainEnd()
    {
        enemyActivateInfiniteImpermanenceFromHand = false;
        base.OnChainEnd();
    }

    public override void OnMove(ClientCard card, int previousControler, int previousLocation, int currentControler, int currentLocation)
    {
        if (previousControler == 1)
        {
            if (card != null)
            {
                if (card.IsCode(_CardId.InfiniteImpermanence) && previousLocation == (int)CardLocation.Hand && currentLocation == (int)CardLocation.SpellZone)
                {
                    enemyActivateInfiniteImpermanenceFromHand = true;
                }
            }
        }

        if (currentLocation == (int)CardLocation.MonsterZone && card.IsSpecialSummoned)
        {
            // Reset the material count, because it's already been summoned.
            runeMaterialCount = (0, 0, 0);
        }

        base.OnMove(card, previousControler, previousLocation, currentControler, currentLocation);
    }

    public override void OnNewTurn()
    {
        ImpermanenceZonesThisTurn.Clear();
        SerpentNegated.Clear();
        UsedSpellTrapMaterial.Clear();

        // Reset portal summoned from deck flag
        portalRuneFromDeckIsUsed = false;

        // Reset extra material flags
        portalExtraMaterialUsed = false;
        shadowExtraMaterialUsed = false;
        summonerExtraMaterialUsed = false;

        // Reset all used as material flags
        var flagKeys = CatenicorumUsedAsMaterialFlags.Keys.ToArray();
        foreach (var key in flagKeys)
        {
            CatenicorumUsedAsMaterialFlags[key] = false;
        }

        base.OnNewTurn();
    }

    public override void OnNewPhase()
    {
        Logger.DebugWriteLine($"Hand: {{{string.Join(", ", Bot.Hand.Select(card => card.Name))}}}");
        base.OnNewPhase();
    }

    public override IList<ClientCard> OnSelectCard(IList<ClientCard> cards, int min, int max, long hint, bool cancelable)
    {
        // Rune Summon Summon
        if (hint == RuneMaterialHint)
        {
            return OnSelectRuneMaterial(cards, min, max, hint, cancelable);
        }

        return base.OnSelectCard(cards, min, max, hint, cancelable);
    }

    public IList<ClientCard> OnSelectRuneMaterial(IList<ClientCard> cards, int min, int max, long hint, bool cancelable)
    {
        Logger.WriteLine($"Selecting materials for Rune Monster {Card.Name}.");

        // Prefer selecting opponent's cards.
        var enemyMonsterMaterials = Util.SelectPreferredCards(Enemy.MonsterZone.OrderBy(card => card?.GetDefensePower() ?? 0).ToList(), cards, min, max);
        var enemySpellMaterials = Util.SelectPreferredCards(Enemy.SpellZone.OrderBy(card => card?.IsFloodgate() == true ? 99 : 0).ToList(), cards, min, max);

        // Refresh the Rune Selection list, until we've selected all of the opponent's cards.
        if (enemyMonsterMaterials.Any() || enemySpellMaterials.Any())
        {
            return CheckSelectCountAndIncrement([.. enemyMonsterMaterials, .. enemySpellMaterials], cards, min, max);
        }

        // If we don't have details as to the Rune monster, just summon it with priority to the opponent's cards.
        if (!CatenicorumRunes.Contains(Card.GetOriginCode()))
        {
            return CheckSelectCountAndIncrement([], cards, min, max);
        }

        //If Ethereal Beast is being summoned, and its conditions have been fulilled cancel the summon.
        if (Card.IsOriginalCode(CardId.EtherealBeast) && RuneConditionFulfilled(2,2) && runeMaterialCatenicorumCount >= 2 && !attemptedRuneCancel)
        {
            attemptedRuneCancel = true;
            return null;
        }

        var nextDeckCard = SelectNextRuneMaterialFromDeck(Card, cards);
        if (nextDeckCard is not null)
        {
            return CheckSelectCountAndIncrement([nextDeckCard], cards, min, max);
        }

        var nextFieldCard = SelectNextMaterialOnField(Card, cards);
        if (nextFieldCard is not null)
        {
            return CheckSelectCountAndIncrement([nextFieldCard], cards, min, max);
        }

        return Util.CheckSelectCount([], cards, min, max);

        IList<ClientCard> CheckSelectCountAndIncrement(IList<ClientCard> _selected, IList<ClientCard> selectableCards, int min, int max)
        {
            var selectedCards = Util.CheckSelectCount(_selected, selectableCards, min, max);

            foreach (var card in selectedCards)
            {
                if (card.IsMonster() && (card.IsSpell() || card.IsTrap()))
                {
                    runeMaterialCount.bothCount++;
                }
                else if (card.IsMonster())
                {
                    runeMaterialCount.monsterCount++;
                }
                else
                {
                    runeMaterialCount.spellCount++;
                }

                if (card.HasSetcode(CatenicorumSetCode))
                {
                    runeMaterialCatenicorumCount++;
                }

                attemptedRuneCancel = false;
            }

            return selectedCards;
        }

        bool RuneConditionFulfilled(int monsterMinimum, int spellMinimum)
        {
            return (runeMaterialCount.monsterCount + runeMaterialCount.bothCount) >= monsterMinimum &&
                (runeMaterialCount.spellCount + runeMaterialCount.bothCount) >= spellMinimum &&
                (runeMaterialCount.monsterCount + runeMaterialCount.spellCount + runeMaterialCount.bothCount) >= (monsterMinimum + spellMinimum);
        }
    }

    /// <summary>
    /// go first
    /// </summary>
    public override bool OnSelectHand()
    {
        return true;
    }

    public Func<bool> AvoidImpermanenceActivate(Func<bool> func)
    {
        return () =>
        {
            var activate = func();

            // If it can't be activated, we don't care.
            if (!activate)
            {
                return false;
            }

            // If it's in the hand and we can control the zone to choose, avoid the impermanence zone.
            if (Card.Location is CardLocation.Hand)
            {
                AI.SelectPlace(SelectSTPlace(Card, true));
            }

            return true;
        };
    }

    private bool BorrelswordEffect()
    {
        if (ActivateDescription == -1) return true;
        else if ((Duel.Phase > DuelPhase.Main1 && Duel.Phase < DuelPhase.Main2) || Util.IsChainTarget(Card))
        {
            List<ClientCard> enemy_list = Enemy.GetMonsters();
            enemy_list.Sort(CardContainer.CompareCardAttack);
            enemy_list.Reverse();
            foreach (ClientCard card in enemy_list)
            {
                if (card.HasPosition(CardPosition.Attack) && !card.HasType(CardType.Link))
                {
                    AI.SelectCard(card);
                    return true;
                }
            }
            List<ClientCard> bot_list = Bot.GetMonsters();
            bot_list.Sort(CardContainer.CompareCardAttack);
            //bot_list.Reverse();
            foreach (ClientCard card in bot_list)
            {
                if (card.HasPosition(CardPosition.Attack) && !card.HasType(CardType.Link))
                {
                    AI.SelectCard(card);
                    return true;
                }
            }
        }
        return false;
    }

    private bool CatenicorumBindingEffect()
    {
        if (Card.Location is CardLocation.Grave or CardLocation.Removed)
        {
            // If it's been used as material, always activate it whenever available in a zone not imperm'd.
            SelectSTPlace(Card, true);
            return true;
        }


        // Always activate the card at the first opportunity.
        if (Card.Location is CardLocation.Hand || (Card.IsFacedown() && Card.Location is CardLocation.SpellZone))
        {
            SelectSTPlace(Card, true);
            return true;
        }

        // If it's disabled, it should only be used to increase the chain link when no other copies of it exist.
        if (Card.IsDisabled() && (Bot.SpellZone.Count(card => card != null && card.IsOriginalCode(CardId.Binding)) > 1 || Duel.CurrentChain.Count > 2))
        {
            return false;
        }

        var negateTargets = Duel.CurrentChain.Where(card => card.Controller == 1 && card.Location is CardLocation.Onfield).ToList();
        var monsterTargets = Enemy.MonsterZone.Where(card => card != null && !negateTargets.Contains(card));
        var spellTrapTargets = Enemy.SpellZone.Where(card => card != null && !negateTargets.Contains(card));

        negateTargets.AddRange(monsterTargets);
        negateTargets.AddRange(spellTrapTargets);

        // Choose target to negate
        if (negateTargets.Count == 0)
        {
            return false;
        }

        AI.SelectCard(negateTargets);
        return true;
    }

    private bool CatenicorumChainsActivate()
    {
        // If it's the targeting effect, we want to choose the opponent's best monster.
        if (Card.Location is CardLocation.Hand || (Card.Location is CardLocation.SpellZone && Card.IsFacedown()))
        {
            // Choose the most problematic monster.
            var problematicCard = Util.GetProblematicEnemyCard(0, true);
            if (problematicCard is not null)
            {
                AI.SelectCard(problematicCard);
            }

            // Avoid Infinite Impermanence.
            if (Card.Location is CardLocation.Hand)
            {
                SelectSTPlace(Card, true);
            }

            // Even if we don't have a problematic card, we want to activate it if we can.
            return true;
        }

        // If it's been used as material, always activate it whenever available in a zone not imperm'd.
        SelectSTPlace(Card, true);
        return true;
    }

    private bool CatenicorumCircleEffect()
    {
        if (Card.Location is CardLocation.Grave or CardLocation.Removed)
        {
            // If it's been used as material, always activate it whenever available in a zone not imperm'd.
            SelectSTPlace(Card, true);
            return true;
        }

        // Always activate the card at the first opportunity.
        if (Card.Location is CardLocation.Hand || (Card.IsFacedown() && Card.Location is CardLocation.SpellZone))
        {
            SelectSTPlace(Card, true);
            return true;
        }

        // If it's disabled, it should only be used to increase the chain link when no other copies of it exist.
        if (Card.IsDisabled() && (Bot.SpellZone.Count(card => card != null && card.IsOriginalCode(CardId.Circle)) > 1 || Duel.CurrentChain.Count > 2))
        {
            return false;
        }

        var onlyCardOnField = Bot.MonsterZone.All(card => card == null) && Bot.SpellZone.All(card => card == null || card.Equals(Card));
        var opponentsTurn = Duel.Player == 1;

        // To improve after creating summon proc functions.
        var shouldSummonCard = Bot.Hand.Any(card => CatenicorumRunes.Contains(card.GetOriginCode()) && !Bot.HasInMonstersZone(card.GetOriginCode()));
        return shouldSummonCard && (opponentsTurn || onlyCardOnField);
    }

    private bool CatenicorumEtherealBeastRuneSummon()
    {
        // If it is already in the monster zone, Manipulator and Serpent must also already on the field.
        var hasNoSerpentOrAllRunesExist = !Bot.HasInMonstersZone(CardId.EtherealBeast, true, false, true) || CatenicorumRunes.All(cardId => Bot.HasInMonstersZone(cardId, true, false, true));
        if (!hasNoSerpentOrAllRunesExist)
        {
            return false;
        }

        if (Card.Location is not CardLocation.Deck)
        {
            return true;
        }

        // If it is already in hand, skip summoning this monster from the deck.
        return !Bot.HasInHand(CardId.Serpent);
    }

    private bool CatenicorumManipulatorEffect()
    {
        if (Card.Location is not CardLocation.MonsterZone)
        {
            // Should always activate the negate or banish effect, if opponent has a face-up card.
            return true;
        }

        // Handle return to deck effect.
        if (Bot.Graveyard.Count + Enemy.Graveyard.Count < 3)
        {
            return false;
        }

        var avoidSelect = Duel.CurrentChain.Where(card => CatenicorumPlaceFromGY.Contains(card.GetOriginCode()));
        var prioritySelect = Duel.LastChainPlayer == 1 ? Duel.LastChainTargets : [];

        // Choose from priority to the other cards, to the avoid cards
        var selectOrder = new List<ClientCard>();
        selectOrder.AddRange(prioritySelect);
        selectOrder.AddRange(Bot.Graveyard.Where(card => !prioritySelect.Contains(card) && !avoidSelect.Contains(card)));
        selectOrder.AddRange(Enemy.Graveyard.Where(card => !prioritySelect.Contains(card) && !avoidSelect.Contains(card)));

        // If there's nothing we desparately need to remove from the Graveyard, we shouldn't activate it.
        if (prioritySelect.Count == 0 && selectOrder.Count < 3)
        {
            return false;
        }

        selectOrder.AddRange(avoidSelect);
        AI.SelectCard(selectOrder);

        return true;
    }

    private bool CatenicorumManipulatorRuneSummon()
    {
        // If it is already in the monster zone, Ethereal Beast and Serpent must also already on the field.
        var hasNoManipulatorOrAllRunesExist = !Bot.HasInMonstersZone(CardId.Manipulator, true, false, true) || CatenicorumRunes.All(cardId => Bot.HasInMonstersZone(cardId, true, false, true));
        if (!hasNoManipulatorOrAllRunesExist)
        {
            return false;
        }

        if (Card.Location is not CardLocation.Deck)
        {
            return true;
        }

        // If it is already in hand, skip summoning this monster from the deck.
        return !Bot.HasInHand(CardId.Serpent);
    }

    private bool CatenicorumSerpentEffect()
    {
        if (Card.Location is not CardLocation.MonsterZone)
        {
            // If Serpent is used as material, we should only activate the effect if the level 4 or lower target is not in the chain.
            var availableGYTargets = Bot.Graveyard.Where(card => card.HasSetcode(CatenicorumSetCode) && !(Duel.CurrentChain.Contains(card) && CatenicorumPlaceFromGY.Contains(card.GetOriginCode())));
            var specialSummonTargets = availableGYTargets.Where(card => card.IsMonster() && card.Level <= 4).ToList();
            if (specialSummonTargets.Count == 0)
            {
                return false;
            }

            AI.SelectCard(specialSummonTargets);

            var toHandTargets = availableGYTargets.Where(card => !card.IsMonster()).ToList();
            AI.SelectNextCard(toHandTargets);
            return true;
        }

        // Get opponent cards that have been activated from the graveyard as a priority before banishing other cards.
        var negateTargets = Duel.CurrentChain.Where(card => card.Controller == 1 && card.Location is CardLocation.Grave).ToList();
        var banishTargets = Enemy.Graveyard.Where(card => !negateTargets.Contains(card) && !SerpentNegated.Contains(card.GetOriginCode()));
        negateTargets.AddRange(banishTargets);

        // Choose target to negate
        var chosenTarget = negateTargets.FirstOrDefault();
        if (chosenTarget is null)
        {
            return false;
        }

        SerpentNegated.Add(chosenTarget.GetOriginCode());
        AI.SelectCard(chosenTarget);
        return true;
    }

    private bool CatenicorumSerpentRuneSummon()
    {
        // If it is already in the monster zone, Ethereal Beast and Manipulator must also already on the field.
        var hasNoSerpentOrAllRunesExist = !Bot.HasInMonstersZone(CardId.Serpent, true, false, true) || CatenicorumRunes.All(cardId => Bot.HasInMonstersZone(cardId, true, false, true));
        if (!hasNoSerpentOrAllRunesExist)
        {
            return false;
        }

        if (Card.Location is not CardLocation.Deck)
        {
            return true;
        }

        // If it is already in hand, skip summoning this monster.
        return !Bot.HasInHand(CardId.Serpent);
    }

    private bool CatenicorumSummonerEffect()
    {
        // To add to hand
        List<int> cardsId = [];
        AddToSummonerList(CardId.Manipulator);
        AddToSummonerList(CardId.Serpent);
        AddToSummonerList(CardId.EtherealBeast);
        AddToSummonerList(CardId.Portal, Bot.HasInHandOrInSpellZone);
        AddToSummonerList(CardId.Sanctum, Bot.HasInHandOrInSpellZone);
        AddToSummonerList(CardId.Circle, cardId => Bot.IsFieldEmpty());
        AddToSummonerList(CardId.Shadow, Bot.HasInHandOrHasInMonstersZone);
        cardsId.Add(CardId.Summoner);
        AddToSummonerList(CardId.Chains, cardId => Enemy.MonsterZone.Any(card => card != null && card.IsFaceup()));
        AI.SelectNextCard(cardsId);
        return true;

        void AddToSummonerList(int cardId, Func<int, bool> condition = null)
        {
            condition ??= Bot.HasInHand;

            if (condition(cardId))
            {
                cardsId.Add(cardId);
            }
        }
    }

    private Func<bool> CharmerSpecial(int cardId, CardAttribute cardAttribute)
    {
        return () =>
        {
            IList<ClientCard> material_list = [];

            // Get materials that the bot is allowed to use for the Charmer.
            var usableMaterials = Bot.GetMonsters().Where(mst => mst.LinkCount <= 1 && !AvoidGenericMaterials.Any(id => mst.GetOriginCode() == id));

            // Get the monster to be used for the same attribute
            var sameAttribute = GetWorstMonster(usableMaterials.Where(mst => mst.HasAttribute(cardAttribute)));

            // No materials are usable for this summon.
            if (sameAttribute is null)
            {
                return false;
            }

            material_list.Add(sameAttribute);

            // Get non-same attribute material
            var otherMaterial = GetWorstMonster(usableMaterials.Where(card => !card.Equals(sameAttribute)));
            if (otherMaterial is null)
            {
                return false;
            }

            material_list.Add(otherMaterial);

            if (Bot.HasInMonstersZone(cardId)) return false;
            AI.SelectMaterials(material_list);
            if (Bot.MonsterZone[0] == null && Bot.MonsterZone[2] == null && Bot.MonsterZone[5] == null)
                AI.SelectPlace(Zones.z5);
            else
                AI.SelectPlace(Zones.z6);
            return true;
        };
    }

    private bool CrystalWingRampSummon()
    {
        var tunerMaterials = Bot.MonsterZone.Where(card => card != null && card.Level == 1 && card.IsTuner()).ToList();
        var otherLevel6Material = Bot.MonsterZone.Where(card => card != null && card.Level == 6 && !card.IsOriginalCode(CardId.Manipulator));
        var manipulatorMaterial = Bot.MonsterZone.Where(card => card != null && card.IsOriginalCode(CardId.Manipulator));

        // If there's another monster besides Catenicorum Manipulator to use as material, summon this Synchro monster using that card.
        if (otherLevel6Material.Any() || manipulatorMaterial.Count() > 1)
        {
            tunerMaterials.AddRange(otherLevel6Material);
            tunerMaterials.Add(manipulatorMaterial.First());
            AI.SelectMaterials(tunerMaterials);
            return true;
        }

        // If we have Crystal Wing Synchro Dragon in the Extra Deck and 2 Level 1 Tuners, we don't mind using the only Manipulator on the field.
        var crystalWingAvailable = Bot.ExtraDeck.Any(card => card.GetOriginCode() == CardId.CrystalWingSynchroDragon);
        return crystalWingAvailable && tunerMaterials.Count >= 2 && manipulatorMaterial.Any();
    }

    private bool GenericLinkSummon()
    {
        // If the bot already has a better monster, don't bother summoning this monster.
        if (!Util.IsTurn1OrMain2() && !Util.IsOneEnemyBetter()
            || Util.GetBestAttack(Bot) >= Card.Attack)
        {
            return false;
        }

        var usableMaterials = Bot.GetMonsters().Where(mst => mst.LinkCount <= Card.LinkCount
                && !AvoidGenericMaterials.Any(id => mst.GetOriginCode() == id)
                && mst.Attack < Card.Attack);

        // Get the materials to be used
        var materials = usableMaterials.OrderBy(card => card.GetDefensePower()).Take(Card.LinkCount);

        if (materials.Count() < Card.LinkCount)
        {
            return false;
        }

        AI.SelectMaterials(materials.ToList());

        return true;
    }

    private bool HasUsedCatenicorumAsMaterialEffect(int cardId)
    {
        var used = false;
        CatenicorumUsedAsMaterialFlags.TryGetValue(cardId, out used);
        return used;
    }

    public bool InfiniteImpermanenceActivate()
    {
        var usedInfiniteImpermanence = DefaultInfiniteImpermanence();
        if (Card.Location is not CardLocation.SpellZone || !usedInfiniteImpermanence)
        {
            return usedInfiniteImpermanence;
        }

        ImpermanenceZonesThisTurn.Add(Card.Sequence);
        return usedInfiniteImpermanence;
    }

    private bool InstantlyNegateEffect()
    {
        return Duel.LastChainPlayer != 0;
    }

    /// <summary>
    /// Used for selecting the worst monster out of a list of monsters.
    /// </summary>
    /// <param name="clientCards">The list of monsters that can be selected.</param>
    /// <param name="keySelector">The way to choose the worst option, by default it uses <see cref="ClientCard.GetDefensePower"/></param>
    /// <returns>The first card with the worst stat.</returns>
    private static ClientCard GetWorstMonster(IEnumerable<ClientCard> clientCards, Func<ClientCard, int> keySelector = null)
    {
        using var iterator = clientCards.GetEnumerator();

        if (!iterator.MoveNext())
        {
            return null;
        }

        var minElement = iterator.Current;
        Func<ClientCard, int> minKeySelector = keySelector is null ? card => card.GetDefensePower() : keySelector;
        var minKey = minKeySelector(minElement);

        while (iterator.MoveNext())
        {
            var currentElement = iterator.Current;
            var currentKey = minKeySelector(currentElement);

            if (currentKey < minKey)
            {
                minKey = currentKey;
                minElement = currentElement;
            }
        }

        return minElement;
    }

    private ClientCard SelectNextMaterialOnField(ClientCard summonCard, IEnumerable<ClientCard> cards)
    {
        ClientCard nextMaterial = null;
        var availableMaterial = cards.Where(card => card != null && card.Location is CardLocation.Onfield && card.IsFaceup());
        var priorityMaterial = availableMaterial.Where(card => !HasUsedCatenicorumAsMaterialEffect(card.GetOriginCode()));

        // Choose a card that provides us with extra material to use.
        nextMaterial ??= PreferredExtraMaterialTrigger(cards);

        // Choose one that hasn't used its effect yet.
        nextMaterial ??= PreferredMaterialOrder(priorityMaterial);

        // If nothing is preferred, just choose in the same order.
        nextMaterial ??= PreferredMaterialOrder(availableMaterial);

        return nextMaterial;

        ClientCard PreferredExtraMaterialTrigger(IEnumerable<ClientCard> clientCards)
        {
            // Always choose Chains first, if it's equipped to an opponent's monster.
            ClientCard nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Chains) && card.Controller == 0 && card.EquipTarget?.Controller == 1);

            // Choose Shadow and Summoner next, to prepare Spell/Traps for use from the Deck.
            nextMaterial ??= !shadowExtraMaterialUsed ? clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Shadow)) : null;
            if (nextMaterial is not null)
            {
                // As there's no way to set this to true, at any other point. We will find behaviour where it hasn't used its extra material but still believes it has.
                shadowExtraMaterialUsed = true;
            }

            nextMaterial ??= !summonerExtraMaterialUsed ? clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Summoner)) : null;
            if (nextMaterial is not null)
            {
                // As there's no way to set this to true, at any other point. We will find behaviour where it hasn't used its extra material but still believes it has.
                summonerExtraMaterialUsed = true;
            }

            // Portal should be used for extra material
            nextMaterial ??= !portalExtraMaterialUsed ? clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Portal)) : null;
            if (nextMaterial is not null)
            {
                // As there's no way to set this to true, at any other point. We will find behaviour where it hasn't used its extra material but still believes it has.
                portalExtraMaterialUsed = true;
            }

            return nextMaterial;
        }

        ClientCard PreferredMaterialOrder(IEnumerable<ClientCard> clientCards)
        {
            // Prioritise Shadow and then Summoner as materials
            var nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Shadow));
            nextMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Summoner));

            // Portal is preferred if no preferred monsters are available
            nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Portal));

            // Binding is preferred next
            nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Binding));

            // Circle is preferred next
            nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Circle));

            // Chains is preferred next
            nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Chains));

            // Prioritise Serpent next
            nextMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Serpent));

            // Manipulator after Serpent
            nextMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Manipulator));

            return nextMaterial;
        }
    }

    private ClientCard SelectNextRuneMaterialFromDeck(ClientCard summonCard, IEnumerable<ClientCard> cards)
    {
        ClientCard nextMaterial = null;
        var availableDeckMaterial = cards.Where(card => card != null && card.Location is CardLocation.Deck);
        var preferredDeckMaterial = availableDeckMaterial.Where(card => !card.IsOriginalCode(CardId.Sanctum) && ShouldUseCatenicorumAsMaterialFromDeck(card));

        // If the bot has no other Rune monsters in hand, portal is the first one to be retrieved always so that the Bot can continue extending.
        if (!portalRuneFromDeckIsUsed && !portalExtraMaterialUsed && !Bot.Hand.Any(card => (card.Type & RuneMonsterType) > 0 && !card.Equals(Card)))
        {
            nextMaterial = availableDeckMaterial.FirstOrDefault(card => card.IsOriginalCode(CardId.Portal));
        }

        // Choose a preferred one.
        nextMaterial ??= PreferredMaterialOrder(preferredDeckMaterial);

        // If nothing is preferred, just choose in the same order.
        nextMaterial ??= PreferredMaterialOrder(availableDeckMaterial);

        return nextMaterial;

        ClientCard PreferredMaterialOrder(IEnumerable<ClientCard> clientCards)
        {
            // Prioritise Shadow and then Summoner as materials
            var nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Shadow));
            nextMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Summoner));

            // Prioritise Serpent next
            nextMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Serpent));

            // Manipulator after Serpent
            nextMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Manipulator));

            // Sanctum is preferred if no preferred monsters are available
            nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Sanctum));

            // Portal is preferred if no preferred monsters are available
            nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Portal));

            // Binding is preferred next
            nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Binding));

            // Circle is preferred next
            nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Circle));

            // Chains is preferred next
            nextMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Chains));

            return nextMaterial;
        }
    }

    private bool PotofProsperityActivate()
    {
        if (Bot.ExtraDeck.Count <= 3) return false;

        // Select in order from top to bottom
        List<int> costCardsOrder = [
            CardId.ArtemisMagistusMoonMaiden,
            CardId.AussaEarthCharmerImmovable,
            CardId.EriaWaterCharmerGentle,
            CardId.DecodeTalker,
            CardId.GaiaBlazeForceOfTheSun,
            CardId.UtopiaBeyond,
            CardId.SuperStarslayerTYPHON,
            CardId.HopeHarbingerDragonTitanicGalaxy,
            CardId.HiitaFireCharmerAblaze,
            CardId.ClearWingSynchroDragon,
            CardId.BorrelswordDragon,
            CardId.CrystalWingSynchroDragon,
            CardId.CyberseQuantumDragon,
        ];
        AI.SelectCard(costCardsOrder);

        // To add to hand
        List<int> cardsId = [];
        AddToProsperityList(CardId.Summoner, cardId => !Bot.HasInHandOrHasInMonstersZone(cardId));
        AddToProsperityList(CardId.Shadow, cardId => !Bot.HasInHandOrHasInMonstersZone(cardId));
        AddToProsperityList(CardId.Portal, cardId => !Bot.HasInHandOrInSpellZone(cardId));
        AddToProsperityList(CardId.Sanctum, cardId => !Bot.HasInSpellZone(cardId));
        AddToProsperityList(CardId.Manipulator);
        AddToProsperityList(CardId.Serpent);
        AddToProsperityList(CardId.EtherealBeast);
        AddToProsperityList(CardId.Circle, cardId => Bot.MonsterZone.All(card => card == null) && Bot.SpellZone.All(card => card == null || card.Equals(Card)));
        AddToProsperityList(CardId.ChangeOfHeart);
        AddToProsperityList(_CardId.HarpiesFeatherDuster);
        cardsId.AddRange(new List<int>() { _CardId.CalledByTheGrave, _CardId.InfiniteImpermanence, _CardId.AshBlossom });
        AI.SelectNextCard(cardsId);
        return true;

        void AddToProsperityList(int cardId, Func<int, bool> condition = null)
        {
            condition ??= Bot.HasInHand;

            if (condition(cardId))
            {
                cardsId.Add(cardId);
            }
        }
    }

    private bool RunedRecoveryEffectActivate()
    {
        // If it's being activated from face-up field, always activate.
        if (Card.Location is CardLocation.SpellZone && Card.IsFaceup())
        {
            return true;
        }

        // Unknown effect, don't activate.
        if (Card.Location is not CardLocation.Hand or CardLocation.SpellZone)
        {
            return false;
        }

        if (Card.Location is CardLocation.Hand)
        {
            SelectSTPlace(Card, true);
        }

        AI.SelectYesNo(true);

        var catenicorumRunes = Bot.Deck.Where(card => CatenicorumRunes.Contains(card.GetOriginCode()));
        var priorityRunes = catenicorumRunes.Where(card => !Bot.HasInHandOrHasInMonstersZone(card.GetOriginCode()));
        AI.SelectCard([..priorityRunes, ..catenicorumRunes, ..Bot.Deck]);

        return false;
    }

    private bool ShouldUseCatenicorumAsMaterialFromDeck(ClientCard clientCard)
    {
        var code = clientCard.GetOriginCode();
        if (HasUsedCatenicorumAsMaterialEffect(code))
        {
            return false;
        }

        switch (code)
        {
            case CardId.Shadow:
                return !shadowExtraMaterialUsed && !Bot.HasInHand(code);
            case CardId.Summoner:
                return !Bot.HasInHand(code);
            case CardId.Manipulator:
                return Enemy.MonsterZone.Any(card => card != null && card.IsFaceup() && !card.IsDisabled()) || Enemy.SpellZone.Any(card => card != null && card.IsFaceup() && !card.IsDisabled());
            case CardId.Serpent:
                return Bot.HasInGraveyard([CardId.Shadow, CardId.Summoner]);
            case CardId.EtherealBeast:
                return Bot.HasInHand(CatenicorumRunes);
            case CardId.Portal:
                return !Bot.HasInHandOrInSpellZone(CardId.Portal);
            case CardId.Sanctum:
                return !Bot.HasInHandOrInSpellZone(CardId.Sanctum);
            case CardId.Circle:
                return !Bot.HasInHandOrInSpellZone(CardId.Circle);
            case CardId.Binding:
                return !Bot.HasInHandOrInSpellZone(CardId.Binding);
            case CardId.Chains:
                return true;
            default:
                return false;
        }
    }

    private int SelectSTPlace(ClientCard card = null, bool avoid_Impermanence = false)
    {
        List<int> list = new List<int> { 0, 1, 2, 3, 4 };
        int n = list.Count;
        while (n-- > 1)
        {
            int index = Program.Rand.Next(n + 1);
            int temp = list[index];
            list[index] = list[n];
            list[n] = temp;
        }
        foreach (int seq in list)
        {
            int zone = (int)System.Math.Pow(2, seq);
            if (Bot.SpellZone[seq] == null)
            {
                if (card != null && card.Location == CardLocation.Hand && avoid_Impermanence && ImpermanenceZonesThisTurn.Contains(seq)) continue;
                return zone;
            };
        }
        return 0;
    }

    private IList<ClientCard> SuperStarslayerTYPHONTarget(int attack = 0)
    {
        List<ClientCard> targetList =
        [
            .. Enemy.GetMonsters()
                .Where(c => c.IsFloodgate() &&c.IsFaceup())
                .OrderByDescending(card => card.Attack),
            .. Enemy.GetMonsters()
                .Where(c => c.IsMonsterDangerous() && c.IsFaceup())
                .OrderByDescending(card => card.Attack),
        ];

        if (Duel.Phase >= DuelPhase.Main2)
        {
            targetList.AddRange(Enemy.GetMonsters()
                .Where(c => c.IsMonsterInvincible() && c.IsFaceup())
                .OrderByDescending(card => card.Attack));
            targetList.AddRange(Enemy.GetMonsters()
                .Where(c => c.GetDefensePower() >= Util.GetBestAttack(Bot) && c.IsAttack())
                .OrderByDescending(card => card.Attack));
            targetList.AddRange(Enemy.GetMonsters()
                .Where(c => c.HasType(CardType.Fusion | CardType.Synchro | CardType.Xyz | CardType.Link | CardType.SpSummon))
                .OrderByDescending(card => card.Attack));
        }

        var monsterTarget = Util.GetProblematicEnemyMonster(attack, false);
        if (monsterTarget is not null)
        {
            targetList.Add(monsterTarget);
        }

        if (targetList.Count > 0)
        {
            targetList.AddRange(Enemy.GetMonsters().Where(card => card.IsFaceup() && !targetList.Contains(card)).OrderByDescending(card => card.Attack));
            targetList.AddRange(Bot.GetMonsters().Where(card => card.IsFaceup() && !targetList.Contains(card)).OrderBy(card => card.Attack));
            return targetList;
        }

        return targetList;
    }

    private bool SuperStarslayerTYPHONActivate()
    {
        if (Card.IsDisabled())
        {
            return false;
        }

        var targetList = SuperStarslayerTYPHONTarget();

        // Best case scenario targeting.
        if (targetList.Count > 0)
        {
            AI.SelectCard(Card.Overlays);
            Logger.DebugWriteLine("TYPHON first target: " + targetList[0]?.Name ?? "UNKNOWN");
            AI.SelectNextCard(targetList);
            return true;
        }

        // No target to use it against, don't use it.
        return false;
    }

    private bool SuperStarslayerTYPHONSpSummon()
    {
        ClientCard material = Bot.GetMonsters().Where(card => card.IsFaceup()).OrderByDescending(card => card.Attack).FirstOrDefault();
        var problematicCards = SuperStarslayerTYPHONTarget(material.Attack);

        // TY-PHON ignores the AvoidAllMaterials condition in Main Phase 2
        if (material == null || problematicCards.Count == 0 || AvoidGenericMaterials.Contains(material.GetOriginCode()))
        {
            return false;
        }

        bool checkFlag = problematicCards.Count > 0;
        checkFlag |= material.Level <= 4;
        checkFlag &= !(material.HasType(CardType.Link) && Duel.Phase >= DuelPhase.Main2);
        if (checkFlag)
        {
            Logger.DebugWriteLine("*** TYPHON select: " + material.Name ?? "UnkonwCard");
            AI.SelectMaterials(material);
            return true;
        }

        return false;
    }
}
