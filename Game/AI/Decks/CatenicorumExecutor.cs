namespace WindBot.Game.AI.Decks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindBot.Game;
using YGOSharp.OCGWrapper;
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

    // Portal Rune Summon from Deck
    private bool portalRuneFromDeckIsUsed = false;

    // Extra Material Effects from Deck
    private bool portalExtraMaterialUsed = false;

    private bool sanctumExtraMaterialUsed = false;

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

        // Starters
        AddExecutor(ExecutorType.Activate, CardId.PotOfProsperity, AvoidImpermanenceActivate(PotofProsperityActivate));

        // Extenders
        AddExecutor(ExecutorType.Activate, CardId.AussaEarthCharmerImmovable);
        AddExecutor(ExecutorType.Activate, CardId.EriaWaterCharmerGentle);
        AddExecutor(ExecutorType.Activate, CardId.BorrelswordDragon, BorrelswordEffect);
        AddExecutor(ExecutorType.Activate, CardId.Summoner, CatenicorumSummonerEffect);
        AddExecutor(ExecutorType.Activate, CardId.Shadow);
        AddExecutor(ExecutorType.Activate, CardId.Circle, CatenicorumCircleEffect);
        AddExecutor(ExecutorType.Activate, CardId.Binding, CatenicorumBindingEffect);
        AddExecutor(ExecutorType.Activate, CardId.Portal, AvoidImpermanenceActivate(() => true));
        AddExecutor(ExecutorType.Activate, CardId.Chains, AvoidImpermanenceActivate(() => true));
        AddExecutor(ExecutorType.Activate, CardId.Sanctum);

        // Other effects, to always activate
        AddExecutor(ExecutorType.Activate, CardId.GaiaBlazeForceOfTheSun);
        AddExecutor(ExecutorType.Activate, CardId.CyberseQuantumDragon);
        AddExecutor(ExecutorType.Activate, CardId.HopeHarbingerDragonTitanicGalaxy);
        AddExecutor(ExecutorType.Activate, CardId.SuperStarslayerTYPHON);

        // Priority Special Summons
        AddExecutor(ExecutorType.SpSummon, CardId.CrystalWingSynchroDragon); // We always want to summon Crystal Wing whenever it's possible.
        AddExecutor(ExecutorType.SpSummon, CardId.Manipulator, CatenicorumManipulatorRuneSummon);
        AddExecutor(ExecutorType.SpSummon, CardId.Serpent, CatenicorumSerpentRuneSummon);
        AddExecutor(ExecutorType.SpSummon, CardId.EtherealBeast);

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
        AddExecutor(ExecutorType.SpSummon, CardId.SuperStarslayerTYPHON, () => Duel.Phase is DuelPhase.Main2);

        // Catenicorum Normal Summons
        AddExecutor(ExecutorType.Summon, CardId.Summoner);
        AddExecutor(ExecutorType.Summon, CardId.Shadow);

        // Mulcharmy Normal Summons, if we can normal summon them. We can't activate their effects anyways so use them as material.
        //AddExecutor(ExecutorType.Summon, CardId.MulcharmyFuwalos, () => RuneIsSummonable(Card));
        //AddExecutor(ExecutorType.Summon, CardId.MulcharmyPurulia, () => RuneIsSummonable(Card));
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
        sanctumExtraMaterialUsed = false;
        shadowExtraMaterialUsed = false;
        summonerExtraMaterialUsed = false;

        // Reset all used as material flags
        foreach (var keyValue in CatenicorumUsedAsMaterialFlags)
        {
            CatenicorumUsedAsMaterialFlags[keyValue.Key] = false;
        }

        base.OnNewTurn();
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
        if (Card.Location is not CardLocation.SpellZone)
        {
            // If it's been used as material, always activate it whenever available in a zone not imperm'd.
            SelectSTPlace(Card);
            return true;
        }

        var negateTargets = Duel.CurrentChain.Where(card => card.Controller == 1 && card.Location is CardLocation.Onfield).ToList();
        var monsterTargets = Enemy.MonsterZone.Where(card => !negateTargets.Contains(card));
        var spellTrapTargets = Enemy.SpellZone.Where(card => !negateTargets.Contains(card));

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

    private bool CatenicorumCircleEffect()
    {
        if (Card.Location is not CardLocation.SpellZone)
        {
            // If it's been used as material, always activate it whenever available in a zone not imperm'd.
            SelectSTPlace(Card);
            return true;
        }

        // To improve after creating summon proc functions.
        var shouldSummonCard = Bot.Hand.Any(card => CatenicorumRunes.Contains(card.GetOriginCode()) && !Bot.HasInMonstersZone(card.GetOriginCode()));
        return shouldSummonCard;
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
        var hasNoManipulatorOrAllRunesExist = !Bot.HasInMonstersZone(CardId.Manipulator, true, false, true) || CatenicorumRunes.All(cardId => Bot.HasInMonstersZone(cardId, true, false, true));
        if (!hasNoManipulatorOrAllRunesExist)
        {
            return false;
        }

        // Don't summon this if it's in the deck, and already exists in the hand.
        if (Card.Location is CardLocation.Deck)
        {
            // If it is already in hand, skip summoning this monster.
            // If it is not in the monster zone, Ethereal Beast and Serpent must also already on the field.
            return !Bot.HasInHand(CardId.Serpent) && !Bot.HasInMonstersZone(CardId.Manipulator, true, false, true) && SelectMaterials(Card);
        }

        return SelectMaterials(Card);

        bool SelectMaterials(ClientCard summonCard)
        {
            var catenicorumSpellFilter = (ClientCard card) => card.HasSetcode(CatenicorumSetCode) && card.IsFaceup();
            var availableSpellMaterial = Bot.SpellZone.Where(catenicorumSpellFilter);

            ClientCard monsterMaterial = null;
            ClientCard spellMaterial = null;

            // Chains fulfills the full material for Manipulator always.
            var chainMaterials = SelectChainsMaterial();
            if (chainMaterials.HasValue)
            {
                AI.SelectMaterials([chainMaterials.Value.chainsCard, chainMaterials.Value.monsterCard]);
                return true;
            }

            // Using the opponent's cards is a priority over using own cards.
            IList<ClientCard> opponentCards = [.. Enemy.MonsterZone, .. Enemy.SpellZone.Where(catenicorumSpellFilter)];

            // Get Sanctum extra material card
            var sanctumMaterial = SelectSanctumExtraMaterial();
            if (sanctumMaterial is not null)
            {
                monsterMaterial ??= sanctumMaterial.IsMonster() ? sanctumMaterial : null;
                spellMaterial ??= sanctumMaterial.IsSpell() || sanctumMaterial.IsTrap() ? sanctumMaterial : null;

                var sanctumMaterials = new List<ClientCard>();
                sanctumMaterials.AddRange(opponentCards);

                monsterMaterial ??= Bot.MonsterZone.FirstOrDefault(card => !HasUsedCatenicorumAsMaterialEffect(card.GetOriginCode()));
                monsterMaterial ??= Bot.MonsterZone.FirstOrDefault(card => !card.HasSetcode(CatenicorumSetCode));
                monsterMaterial ??= Bot.MonsterZone.FirstOrDefault();

                if (monsterMaterial is not null)
                {
                    sanctumMaterials.Add(monsterMaterial);
                }

                spellMaterial ??= availableSpellMaterial.FirstOrDefault(card => !HasUsedCatenicorumAsMaterialEffect(card.GetOriginCode()));
                spellMaterial ??= availableSpellMaterial.FirstOrDefault();

                if (spellMaterial is not null)
                {
                    sanctumMaterials.Add(spellMaterial);
                }


                AI.SelectMaterials(sanctumMaterials);
                return true;
            }

            // Other extra materials
            var summonerMaterial = SelectSummonerExtraMaterial();
            if (summonerMaterial.HasValue)
            {
                AI.SelectMaterials([summonerMaterial.Value.summonerCard, summonerMaterial.Value.spellTrapCard]);
                return true;
            }

            var shadowMaterial = SelectShadowExtraMaterial();
            if (shadowMaterial.HasValue)
            {
                AI.SelectMaterials([shadowMaterial.Value.shadowCard, shadowMaterial.Value.spellTrapCard]);
                return true;
            }

            var portalMaterial = SelectPortalExtraMaterial();
            if (portalMaterial.HasValue)
            {
                AI.SelectMaterials([portalMaterial.Value.portalCard, portalMaterial.Value.monsterCard]);
                return true;
            }

            var materials = new List<ClientCard>();
            materials.AddRange(opponentCards);

            monsterMaterial ??= Bot.MonsterZone.FirstOrDefault(card => !HasUsedCatenicorumAsMaterialEffect(card.GetOriginCode()));
            monsterMaterial ??= Bot.MonsterZone.FirstOrDefault(card => !card.HasSetcode(CatenicorumSetCode));
            monsterMaterial ??= Bot.MonsterZone.FirstOrDefault();

            if (monsterMaterial is not null)
            {
                materials.Add(monsterMaterial);
            }

            spellMaterial ??= availableSpellMaterial.FirstOrDefault(card => !HasUsedCatenicorumAsMaterialEffect(card.GetOriginCode()));
            spellMaterial ??= availableSpellMaterial.FirstOrDefault();

            if (spellMaterial is not null)
            {
                materials.Add(spellMaterial);
            }


            AI.SelectMaterials(materials);
            return true;
        }
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
        var hasNoSerpentOrAllRunesExist = !Bot.HasInMonstersZone(CardId.Serpent, true, false, true) || CatenicorumRunes.All(cardId => Bot.HasInMonstersZone(cardId, true, false, true));
        if (!hasNoSerpentOrAllRunesExist)
        {
            return false;
        }

        // Don't summon this if it's in the deck, and already exists in the hand.
        if (Card.Location is CardLocation.Deck)
        {
            // If it is already in hand, skip summoning this monster.
            // If it is not in the monster zone, Ethereal Beast and Serpent must also already on the field.
            return !Bot.HasInHand(CardId.Serpent) && !Bot.HasInMonstersZone(CardId.Serpent, true, false, true) && SelectMaterials(Card);
        }

        return SelectMaterials(Card);

        bool SelectMaterials(ClientCard summonCard)
        {
            // Get all non-Token monsters
            var nonTokenFilter = (ClientCard card) => (card.Type & (int)CardType.Token) == 0;
            var availableBotMonsters = Bot.MonsterZone.Where(nonTokenFilter);

            // Get all Catenicorum Spell/Traps
            var catenicorumSpellFilter = (ClientCard card) => card.HasSetcode(CatenicorumSetCode) && card.IsFaceup();
            var availableSpellMaterial = Bot.SpellZone.Where(catenicorumSpellFilter);

            ClientCard monsterMaterial = null;
            ClientCard spellMaterial = null;

            // Chains fulfills the full material for Manipulator always.
            var chainMaterials = SelectChainsMaterial();
            if (chainMaterials.HasValue)
            {
                AI.SelectMaterials([chainMaterials.Value.chainsCard, chainMaterials.Value.monsterCard]);
                return true;
            }

            // Using the opponent's cards is a priority over using own cards.
            IList<ClientCard> opponentCards = [.. Enemy.MonsterZone.Where(nonTokenFilter), .. Enemy.SpellZone.Where(catenicorumSpellFilter)];

            // Get Sanctum extra material card
            var sanctumMaterial = SelectSanctumExtraMaterial();
            if (sanctumMaterial is not null)
            {
                monsterMaterial ??= sanctumMaterial.IsMonster() ? sanctumMaterial : null;
                spellMaterial ??= sanctumMaterial.IsSpell() || sanctumMaterial.IsTrap() ? sanctumMaterial : null;

                var sanctumMaterials = new List<ClientCard>();
                sanctumMaterials.AddRange(opponentCards);

                monsterMaterial ??= availableBotMonsters.FirstOrDefault(card => !HasUsedCatenicorumAsMaterialEffect(card.GetOriginCode()));
                monsterMaterial ??= availableBotMonsters.FirstOrDefault(card => !card.HasSetcode(CatenicorumSetCode));
                monsterMaterial ??= availableBotMonsters.FirstOrDefault();

                if (monsterMaterial is not null)
                {
                    sanctumMaterials.Add(monsterMaterial);
                }

                spellMaterial ??= availableSpellMaterial.FirstOrDefault(card => !HasUsedCatenicorumAsMaterialEffect(card.GetOriginCode()));
                spellMaterial ??= availableSpellMaterial.FirstOrDefault();

                if (spellMaterial is not null)
                {
                    sanctumMaterials.Add(spellMaterial);
                }


                AI.SelectMaterials(sanctumMaterials);
                return true;
            }

            // Other extra materials
            var summonerMaterial = SelectSummonerExtraMaterial();
            if (summonerMaterial.HasValue)
            {
                AI.SelectMaterials([summonerMaterial.Value.summonerCard, summonerMaterial.Value.spellTrapCard]);
                return true;
            }

            var shadowMaterial = SelectShadowExtraMaterial();
            if (shadowMaterial.HasValue)
            {
                AI.SelectMaterials([shadowMaterial.Value.shadowCard, shadowMaterial.Value.spellTrapCard]);
                return true;
            }

            var portalMaterial = SelectPortalExtraMaterial();
            if (portalMaterial.HasValue)
            {
                AI.SelectMaterials([portalMaterial.Value.portalCard, portalMaterial.Value.monsterCard]);
                return true;
            }

            var materials = new List<ClientCard>();
            materials.AddRange(opponentCards);

            monsterMaterial ??= availableBotMonsters.FirstOrDefault(card => !HasUsedCatenicorumAsMaterialEffect(card.GetOriginCode()));
            monsterMaterial ??= availableBotMonsters.FirstOrDefault(card => !card.HasSetcode(CatenicorumSetCode));
            monsterMaterial ??= availableBotMonsters.FirstOrDefault();

            if (monsterMaterial is not null)
            {
                materials.Add(monsterMaterial);
            }

            spellMaterial ??= availableSpellMaterial.FirstOrDefault(card => !HasUsedCatenicorumAsMaterialEffect(card.GetOriginCode()));
            spellMaterial ??= availableSpellMaterial.FirstOrDefault();

            if (spellMaterial is not null)
            {
                materials.Add(spellMaterial);
            }


            AI.SelectMaterials(materials);
            return true;
        }
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
        AddToSummonerList(CardId.Chains, cardId => Enemy.MonsterZone.Any(card => card.IsFaceup()));
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
        var tunerMaterials = Bot.MonsterZone.Where(card => card.Level == 1 && card.IsTuner()).ToList();
        var otherLevel6Material = Bot.MonsterZone.Where(card => card.Level == 6 && !card.IsOriginalCode(CardId.Manipulator));
        var manipulatorMaterial = Bot.MonsterZone.Where(card => card.IsOriginalCode(CardId.Manipulator));

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
        AddToProsperityList(CardId.Summoner, Bot.HasInHandOrHasInMonstersZone);
        AddToProsperityList(CardId.Shadow, Bot.HasInHandOrHasInMonstersZone);
        AddToProsperityList(CardId.Portal, Bot.HasInHandOrInSpellZone);
        AddToProsperityList(CardId.Sanctum, Bot.HasInHandOrInSpellZone);
        AddToProsperityList(CardId.Manipulator);
        AddToProsperityList(CardId.Serpent);
        AddToProsperityList(CardId.EtherealBeast);
        AddToProsperityList(CardId.Circle, cardId => Bot.IsFieldEmpty());
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

    private bool RuneIsSummonable(ClientCard extraMaterial = null)
    {
        var handCards = Bot.Hand.Where(card => CatenicorumRunes.Contains(card.GetOriginCode()) && CardIsSummonable(card.GetOriginCode()));
        var deckCards = portalRuneFromDeckIsUsed ? [] : Bot.Deck.Where(card => CatenicorumRunes.Contains(card.GetOriginCode()) && CardIsSummonable(card.GetOriginCode()));
        return handCards.Any() || deckCards.Any();

        bool CardIsSummonable(int cardId)
        {
            switch (cardId)
            {
                case CardId.Manipulator:
                    var manipMonsterMaterials = Bot.MonsterZone.Where(card => AvoidGenericMaterials.Contains(card.GetOriginCode())).ToList();
                    var manipSTMaterials = Bot.SpellZone.Where(card => card.HasSetcode(CatenicorumSetCode) && !UsedSpellTrapMaterial.Contains(card.GetOriginCode())).ToList();

                    // TODO: Improve to work with extra materials
                    if (extraMaterial.Location is CardLocation.MonsterZone)
                    {
                        manipMonsterMaterials.Add(extraMaterial);
                    }
                    else if (extraMaterial.Location is CardLocation.SpellZone)
                    {
                        manipSTMaterials.Add(extraMaterial);
                    }
                    return manipMonsterMaterials.Count > 0 && manipSTMaterials.Count > 0;
                case CardId.Serpent:
                    var serpentMonsterMaterials = Bot.MonsterZone.Where(card => (card.Type & (int) CardType.Token) == 0 && AvoidGenericMaterials.Contains(card.GetOriginCode())).ToList();
                    var serpentSTMaterials = Bot.SpellZone.Where(card => card.HasSetcode(CatenicorumSetCode) && !UsedSpellTrapMaterial.Contains(card.GetOriginCode())).ToList();

                    // TODO: Improve to work with extra materials
                    if (extraMaterial.Location is CardLocation.MonsterZone)
                    {
                        serpentMonsterMaterials.Add(extraMaterial);
                    }
                    else if (extraMaterial.Location is CardLocation.SpellZone)
                    {
                        serpentSTMaterials.Add(extraMaterial);
                    }

                    return serpentMonsterMaterials.Count > 0 && serpentSTMaterials.Count > 0;
                case CardId.EtherealBeast:
                    var etherealMonsterMaterials = Bot.MonsterZone.Where(card => (card.Type & (int)CardType.Token) == 0 && AvoidGenericMaterials.Contains(card.GetOriginCode())).ToList();
                    var etherealSTMaterials = Bot.SpellZone.Where(card => !UsedSpellTrapMaterial.Contains(card.GetOriginCode())).ToList();

                    // TODO: Improve to work with extra materials
                    if (extraMaterial.Location is CardLocation.MonsterZone)
                    {
                        etherealMonsterMaterials.Add(extraMaterial);
                    }
                    else if (extraMaterial.Location is CardLocation.SpellZone)
                    {
                        etherealSTMaterials.Add(extraMaterial);
                    }

                    return etherealMonsterMaterials.Count >= 2 && etherealSTMaterials.Count >= 2;
                default:
                    return false;
            }
        }
    }

    private (ClientCard chainsCard, ClientCard monsterCard)? SelectChainsMaterial()
    {
        if (!Bot.HasInSpellZone(CardId.Chains, true, true))
        {
            return null;
        }

        var chainsCard = Bot.SpellZone.FirstOrDefault(card => card.IsOriginalCode(CardId.Chains) && card.IsFaceup() && !card.IsDisabled());
        return (chainsCard, chainsCard.EquipTarget);
    }

    private (ClientCard portalCard, ClientCard monsterCard)? SelectPortalExtraMaterial()
    {
        if (portalExtraMaterialUsed || !Bot.HasInSpellZone(CardId.Portal, true, true))
        {
            return null;
        }

        var portalCard = Bot.SpellZone.FirstOrDefault(card => card.IsOriginalCode(CardId.Portal) && card.IsFaceup() && !card.IsDisabled());

        ClientCard portalMaterial = null;
        var availableDeckMaterial = Bot.Deck.Where(card => card.HasSetcode(CatenicorumSetCode) && card.IsMonster());
        var preferredDeckMaterial = availableDeckMaterial.Where(card => ShouldUseCatenicorumAsMaterialFromDeck(card));

        // Choose a preferred one.
        portalMaterial ??= PreferredPortalExtraMaterialOrder(preferredDeckMaterial);

        // If nothing is preferred, just choose in the same order.
        portalMaterial ??= PreferredPortalExtraMaterialOrder(availableDeckMaterial);

        return (portalCard, portalMaterial);

        ClientCard PreferredPortalExtraMaterialOrder(IEnumerable<ClientCard> clientCards)
        {
            // Prioritise Shadow and then Summoner as materials
            var sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Shadow));
            sanctumMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Summoner));

            // Prioritise Serpent next
            sanctumMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Serpent));

            // Manipulator after Serpent
            sanctumMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Manipulator));

            return sanctumMaterial;
        }
    }

    private ClientCard SelectSanctumExtraMaterial()
    {
        if (sanctumExtraMaterialUsed || !Bot.HasInSpellZone(CardId.Sanctum, true, true))
        {
            return null;
        }

        ClientCard sanctumMaterial = null;
        var availableDeckMaterial = Bot.Deck.Where(card => card.HasSetcode(CatenicorumSetCode));
        var preferredDeckMaterial = availableDeckMaterial.Where(card => !card.IsOriginalCode(CardId.Sanctum) && ShouldUseCatenicorumAsMaterialFromDeck(card));

        // If the bot has no other Rune monsters in hand, portal is the first one to be retrieved always so that the Bot can continue extending.
        if (!portalRuneFromDeckIsUsed && !portalExtraMaterialUsed && !Bot.Hand.Any(card => (card.Type & RuneMonsterType) > 0 && !card.Equals(Card)))
        {
            sanctumMaterial = availableDeckMaterial.FirstOrDefault(card => card.IsOriginalCode(CardId.Portal));
        }

        // Choose a preferred one.
        sanctumMaterial ??= PreferredSanctumExtraMaterialOrder(preferredDeckMaterial);

        // If nothing is preferred, just choose in the same order.
        sanctumMaterial ??= PreferredSanctumExtraMaterialOrder(availableDeckMaterial);

        return sanctumMaterial;

        ClientCard PreferredSanctumExtraMaterialOrder(IEnumerable<ClientCard> clientCards)
        {
            // Prioritise Shadow and then Summoner as materials
            var sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Shadow));
            sanctumMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Summoner));

            // Prioritise Serpent next
            sanctumMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Serpent));

            // Manipulator after Serpent
            sanctumMaterial ??= clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Manipulator));

            // Portal is preferred if no preferred monsters are available
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Portal));

            // Binding is preferred next
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Binding));

            // Circle is preferred next
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Circle));

            // Chains is preferred next
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Chains));

            return sanctumMaterial;
        }
    }

    private (ClientCard shadowCard, ClientCard spellTrapCard)? SelectShadowExtraMaterial()
    {
        if (shadowExtraMaterialUsed || !Bot.HasInMonstersZone(CardId.Shadow, true, true))
        {
            return null;
        }

        var portalCard = Bot.SpellZone.FirstOrDefault(card => card.IsOriginalCode(CardId.Shadow) && card.IsFaceup() && !card.IsDisabled());

        ClientCard sanctumMaterial = null;
        var availableDeckMaterial = Bot.Deck.Where(card => card.HasSetcode(CatenicorumSetCode) && card.IsMonster());
        var preferredDeckMaterial = availableDeckMaterial.Where(card => ShouldUseCatenicorumAsMaterialFromDeck(card));

        // Choose a preferred one.
        sanctumMaterial ??= PreferredPortalExtraMaterialOrder(preferredDeckMaterial);

        // If nothing is preferred, just choose in the same order.
        sanctumMaterial ??= PreferredPortalExtraMaterialOrder(availableDeckMaterial);

        return (portalCard, sanctumMaterial);

        ClientCard PreferredPortalExtraMaterialOrder(IEnumerable<ClientCard> clientCards)
        {
            // Portal is preferred if no preferred monsters are available
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Portal));

            // Binding is preferred next
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Binding));

            // Circle is preferred next
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Circle));

            // Chains is preferred next
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Chains));

            return sanctumMaterial;
        }
    }

    private (ClientCard summonerCard, ClientCard spellTrapCard)? SelectSummonerExtraMaterial()
    {
        if (summonerExtraMaterialUsed || !Bot.HasInMonstersZone(CardId.Summoner, true, true))
        {
            return null;
        }

        var portalCard = Bot.SpellZone.FirstOrDefault(card => card.IsOriginalCode(CardId.Summoner) && card.IsFaceup() && !card.IsDisabled());

        ClientCard sanctumMaterial = null;
        var availableDeckMaterial = Bot.Deck.Where(card => card.HasSetcode(CatenicorumSetCode) && card.IsMonster());
        var preferredDeckMaterial = availableDeckMaterial.Where(card => ShouldUseCatenicorumAsMaterialFromDeck(card));

        // Choose a preferred one.
        sanctumMaterial ??= PreferredPortalExtraMaterialOrder(preferredDeckMaterial);

        // If nothing is preferred, just choose in the same order.
        sanctumMaterial ??= PreferredPortalExtraMaterialOrder(availableDeckMaterial);

        return (portalCard, sanctumMaterial);

        ClientCard PreferredPortalExtraMaterialOrder(IEnumerable<ClientCard> clientCards)
        {
            // Portal is preferred if no preferred monsters are available
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Portal));

            // Binding is preferred next
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Binding));

            // Circle is preferred next
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Circle));

            // Chains is preferred next
            sanctumMaterial = clientCards.FirstOrDefault(card => card.IsOriginalCode(CardId.Chains));

            return sanctumMaterial;
        }
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
                return Enemy.MonsterZone.Any(card => card.IsFaceup() && !card.IsDisabled()) || Enemy.SpellZone.Any(card => card.IsFaceup() && !card.IsDisabled());
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
}
