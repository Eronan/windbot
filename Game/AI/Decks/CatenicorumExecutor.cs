namespace WindBot.Game.AI.Decks;

using System;
using System.Collections.Generic;
using System.Linq;
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
        CardId.CrystalWingSynchroDragon
    ];

    private readonly List<int> ImpermanenceZonesThisTurn = [];

    private readonly List<int> SerpentNegated = [];

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

        // Catenicorum Special Summons
        AddExecutor(ExecutorType.SpSummon, CardId.Manipulator);
        AddExecutor(ExecutorType.SpSummon, CardId.Serpent);
        AddExecutor(ExecutorType.SpSummon, CardId.EtherealBeast);

        // Generic Special Summons
        AddExecutor(ExecutorType.SpSummon, CardId.GaiaBlazeForceOfTheSun);
        AddExecutor(ExecutorType.SpSummon, CardId.CyberseQuantumDragon);
        AddExecutor(ExecutorType.SpSummon, CardId.ClearWingSynchroDragon);
        AddExecutor(ExecutorType.SpSummon, CardId.CrystalWingSynchroDragon);
        AddExecutor(ExecutorType.SpSummon, CardId.UtopiaBeyond);
        AddExecutor(ExecutorType.SpSummon, CardId.HopeHarbingerDragonTitanicGalaxy);
        AddExecutor(ExecutorType.SpSummon, CardId.AussaEarthCharmerImmovable, CharmerSpecial(CardId.AussaEarthCharmerImmovable, CardAttribute.Earth));
        AddExecutor(ExecutorType.SpSummon, CardId.EriaWaterCharmerGentle, CharmerSpecial(CardId.EriaWaterCharmerGentle, CardAttribute.Water));
        AddExecutor(ExecutorType.SpSummon, CardId.HiitaFireCharmerAblaze, CharmerSpecial(CardId.HiitaFireCharmerAblaze, CardAttribute.Fire));
        AddExecutor(ExecutorType.SpSummon, CardId.DecodeTalker, GenericLinkSummon);
        AddExecutor(ExecutorType.SpSummon, CardId.SuperStarslayerTYPHON, () => Duel.Phase is DuelPhase.Main2);
    }

    public override void OnNewTurn()
    {
        ImpermanenceZonesThisTurn.Clear();
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

    public int SelectSTPlace(ClientCard card = null, bool avoid_Impermanence = false)
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
