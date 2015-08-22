#region
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

#endregion

namespace Leblanc
{
    internal class Program
    {
        public const string ChampionName = "Leblanc";

        private static readonly List<Slide> ExistingSlide = new List<Slide>();
        private static bool leBlancClone;

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q, W, E, R;

        private static ComboType vComboType = ComboType.ComboQR;
        private static ComboKill vComboKill = ComboKill.FullCombo;
        private static bool _isComboCompleted = true;

        public static SpellSlot IgniteSlot = ObjectManager.Player.GetSpellSlot("SummonerDot");
        public static Items.Item Fqc = new Items.Item(3092, 750);

        //Menu
        public static Menu Config;
        public static Menu MenuExtras;

        private static readonly string[] LeBlancIsWeakAgainst =
        {
            "Galio", "Karma", "Sion", "Annie", "Syndra", "Diana",
            "Aatrox", "Mordekaiser", "Talon", "Morgana"
        };

        private static readonly string[] LeBlancIsStrongAgainst =
        {
            "Velkoz", "Ahri", "Karthus", "Fizz", "Ziggs",
            "Katarina", "Orianna", "Nidalee", "Yasuo", "Akali"
        };

        public static bool LeBlancClone
        {
            get { return leBlancClone; }
            set { leBlancClone = value; }
        }

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != ChampionName)
                return;

            Q = new Spell(SpellSlot.Q, 720);
            Q.SetTargetted(0.5f, 1500f);

            W = new Spell(SpellSlot.W, 670);
            W.SetSkillshot(0.6f, 220f, 1450f, false, SkillshotType.SkillshotCircle);

            E = new Spell(SpellSlot.E, 900);
            E.SetSkillshot(0.3f, 55f, 1650f, true, SkillshotType.SkillshotLine);

            R = new Spell(SpellSlot.R, 720);
            {
                SpellList.Add(Q);
                SpellList.Add(W);
                SpellList.Add(E);
                SpellList.Add(R);
            }

            Config = new Menu(ChampionName, ChampionName, true);
            {
                Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));
            }

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            var menuTargetSelector = new Menu("Target Selector", "TargetSelector");
            {
                TargetSelector.AddToMenu(menuTargetSelector);
                Config.AddSubMenu(menuTargetSelector);
            }
            new AssassinManager();

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            {
                Config.SubMenu("Combo")
                    .AddItem(
                        new MenuItem("ComboSetOption", "Combo").SetValue(
                            new StringList(new[] { "Auto", "Q-R Combo", "W-R Combo", "E-R Combo", }, 1)));
                Config.SubMenu("Combo")
                    .AddItem(
                        new MenuItem("ComboSetEHitCh", "E Hit").SetValue(
                            new StringList(new[] { "Low", "Medium", "High", "Very High", "Immobile" }, 2)));
                Config.SubMenu("Combo").AddSubMenu(new Menu("Don't Use Combo on", "DontCombo"));
                {
                    foreach (
                        var enemy in
                            ObjectManager.Get<Obj_AI_Hero>().Where(enemy => enemy.Team != ObjectManager.Player.Team))
                    {
                        Config.SubMenu("Combo")
                            .SubMenu("DontCombo")
                            .AddItem(new MenuItem("DontCombo" + enemy.BaseSkinName, enemy.BaseSkinName).SetValue(false));
                    }
                }
                Config.SubMenu("Combo")
                    .AddItem(
                        new MenuItem("ComboDblStun", "Double Stun!").SetValue(
                            new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                Config.SubMenu("Combo").AddItem(new MenuItem("ComboShowInfo", "Show Combo Status").SetValue(true));
                Config.SubMenu("Combo")
                    .AddItem(
                        new MenuItem("ComboActive", "Combo!").SetValue(
                            new KeyBind(Config.Item("Orbwalk").GetValue<KeyBind>().Key, KeyBindType.Press)));
            }

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            {
                Config.SubMenu("Harass").AddSubMenu(new Menu("Q", "HarassQ"));
                Config.SubMenu("Harass").SubMenu("HarassQ").AddItem(new MenuItem("HarassUseQ", "Use Q").SetValue(true));
                Config.SubMenu("Harass")
                    .SubMenu("HarassQ")
                    .AddItem(new MenuItem("HarassManaQ", "Q Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
                Config.SubMenu("Harass")
                    .SubMenu("HarassQ")
                    .AddItem(
                        new MenuItem("HarassUseTQ", "Use Q (toggle)!").SetValue(
                            new KeyBind("J".ToCharArray()[0], KeyBindType.Toggle)));
                Config.SubMenu("Harass").AddSubMenu(new Menu("W", "HarassW"));
                Config.SubMenu("Harass").SubMenu("HarassW").AddItem(new MenuItem("HarassUseW", "Use W").SetValue(true));
                Config.SubMenu("Harass")
                    .SubMenu("HarassW")
                    .AddItem(new MenuItem("HarassManaW", "W Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
                Config.SubMenu("Harass")
                    .SubMenu("HarassW")
                    .AddItem(
                        new MenuItem("HarassUseTW", "Use W (toggle)!").SetValue(
                            new KeyBind("K".ToCharArray()[0], KeyBindType.Toggle)));
                Config.SubMenu("Harass").AddSubMenu(new Menu("E", "HarassE"));
                Config.SubMenu("Harass").SubMenu("HarassE").AddItem(new MenuItem("HarassUseE", "Use E").SetValue(true));
                Config.SubMenu("Harass")
                    .SubMenu("HarassE")
                    .AddItem(new MenuItem("HarassManaE", "E Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
                Config.SubMenu("Harass")
                    .SubMenu("HarassE")
                    .AddItem(
                        new MenuItem("HarassUseTE", "Use E (toggle)!").SetValue(
                            new KeyBind("L".ToCharArray()[0], KeyBindType.Toggle)));

                Config.SubMenu("Harass")
                    .AddItem(new MenuItem("HarassShowInfo", "Show Harass Toggle Status").SetValue(true));
                Config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("HarassActive", "Harass!").SetValue(
                            new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            }

            Config.AddSubMenu(new Menu("Lane Clear", "LaneClear"));
            {
                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseQ", "Use Q").SetValue(false));
                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseW", "Use W").SetValue(false));
                Config.SubMenu("LaneClear").AddItem(new MenuItem("LaneClearUseE", "Use E").SetValue(false));
                Config.SubMenu("LaneClear")
                    .AddItem(new MenuItem("LaneClearMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
                Config.SubMenu("LaneClear")
                    .AddItem(
                        new MenuItem("LaneClearActive", "Harass!").SetValue(
                            new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            }

            Config.AddSubMenu(new Menu("JungleFarm", "JungleFarm"));
            {
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseQ", "Use Q").SetValue(true));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseW", "Use W").SetValue(true));
                Config.SubMenu("JungleFarm").AddItem(new MenuItem("JungleFarmUseE", "Use E").SetValue(true));
                Config.SubMenu("JungleFarm")
                    .AddItem(new MenuItem("JungleFarmMana", "Min. Mana Percent: ").SetValue(new Slider(50, 100, 0)));
                Config.SubMenu("JungleFarm")
                    .AddItem(
                        new MenuItem("JungleFarmActive", "Harass!").SetValue(
                            new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
            }

            var menuRun = new Menu("Run", "Run");
            {
                menuRun.AddItem(new MenuItem("RunUseW", "Use W").SetValue(true));
                menuRun.AddItem(new MenuItem("RunUseR", "Use R").SetValue(true));
                menuRun.AddItem(
                    new MenuItem("RunActive", "Run!").SetValue(new KeyBind("A".ToCharArray()[0], KeyBindType.Press)));
                Config.AddSubMenu(menuRun);
            }

            MenuExtras = new Menu("Extras", "Extras");
            {
                Config.AddSubMenu(MenuExtras);
                MenuExtras.AddItem(new MenuItem("InterruptSpells", "Interrupt Spells").SetValue(true));
                new PotionManager();
            }

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            {
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("QRange", "Q Range").SetValue(new Circle(false, Color.Honeydew)));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("WRange", "W Range").SetValue(new Circle(true, Color.Honeydew)));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("ERange", "E Range").SetValue(new Circle(false, Color.Honeydew)));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("RRange", "R Range").SetValue(new Circle(false, Color.Honeydew)));

                Config.SubMenu("Drawings")
                    .AddItem(
                        new MenuItem("ActiveERange", "Active E Range").SetValue(new Circle(false, Color.GreenYellow)));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("WObjPosition", "W Obj. Pos.").SetValue(new Circle(true, Color.GreenYellow)));
                Config.SubMenu("Drawings").AddItem(new MenuItem("WObjTimeTick", "W Obj. Tick").SetValue(true));
                Config.SubMenu("Drawings")
                    .AddItem(new MenuItem("WQRange", "W+Q Range").SetValue(new Circle(false, Color.GreenYellow)));

                var dmgAfterComboItem = new MenuItem("DamageAfterCombo", "Damage After Combo").SetValue(true);
                Config.SubMenu("Drawings").AddItem(dmgAfterComboItem);

                Utility.HpBarDamageIndicator.DamageToUnit = GetComboDamage;
                Utility.HpBarDamageIndicator.Enabled = dmgAfterComboItem.GetValue<bool>();
                dmgAfterComboItem.ValueChanged += delegate(object sender, OnValueChangeEventArgs eventArgs)
                {
                    Utility.HpBarDamageIndicator.Enabled = eventArgs.GetNewValue<bool>();
                };

            }

            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnUpdate;
            {
                Drawing.OnDraw += Drawing_OnDraw;
                GameObject.OnCreate += GameObject_OnCreate;
                GameObject.OnDelete += GameObject_OnDelete;
                Interrupter.OnPossibleToInterrupt += Interrupter_OnPosibleToInterrupt;
            }

            Game.PrintChat(
                String.Format(
                    "<font color='#70DBDB'>xQx</font> <font color='#FFFFFF'>{0}</font> <font color='#70DBDB'>Loaded!</font>",
                    ChampionName));
                    
           // ShineCommon.Maths.Prediction.Initialize();
        }

        private static int FindCounterStatusForTarget(string enemyBaseSkinName)
        {
            if (LeBlancIsWeakAgainst.Contains(enemyBaseSkinName))
                return 1;

            if (LeBlancIsStrongAgainst.Contains(enemyBaseSkinName))
                return 2;

            return 0;
        }

        private static Obj_AI_Hero EnemyHaveSoulShackle
        {
            get
            {
                return
                    (from hero in
                        ObjectManager.Get<Obj_AI_Hero>().Where(hero => ObjectManager.Player.Distance(hero) <= 1100)
                        where hero.IsEnemy
                        from buff in hero.Buffs
                        where buff.Name.Contains("LeblancSoulShackle")
                        select hero).FirstOrDefault();
            }
        }

        private static bool DrawEnemySoulShackle
        {
            get
            {
                return
                    (from hero in
                        ObjectManager.Get<Obj_AI_Hero>().Where(hero => ObjectManager.Player.Distance(hero) <= 1100)
                        where hero.IsEnemy
                        from buff in hero.Buffs
                        select (buff.Name.Contains("LeblancSoulShackle"))).FirstOrDefault();
            }
        }

        private static void Interrupter_OnPosibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!Config.Item("InterruptSpells").GetValue<bool>())
                return;

            var isValidTarget = unit.IsValidTarget(E.Range) && spell.DangerLevel == InterruptableDangerLevel.High;

            if (E.IsReady() && isValidTarget)
            {
                E.CastIfHitchanceEquals(unit, GetEHitChance);
            }
            else if (R.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSoulShackleM" &&
                     isValidTarget)
            {
                R.Cast(unit);
            }
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            leBlancClone = sender.Name.Contains("LeBlanc_MirrorImagePoff.troy");

            if (sender.Name.Contains("displacement_blink_indicator"))
            {
                ExistingSlide.Add(
                    new Slide
                    {
                        Object = sender,
                        NetworkId = sender.NetworkId,
                        Position = sender.Position,
                        ExpireTime = Game.Time + 4
                    });
            }
        }

        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("displacement_blink_indicator"))
                return;

            for (var i = 0; i < ExistingSlide.Count; i++)
            {
                if (ExistingSlide[i].NetworkId == sender.NetworkId)
                {
                    ExistingSlide.RemoveAt(i);
                    return;
                }
            }
        }

        public static bool LeBlancStillJumped
        {
            get
            {
                return !W.IsReady() || ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "leblancslidereturn";
            }
        }

        private static void UserSummoners(Obj_AI_Base t)
        {

            if (Fqc.IsReady())
                Fqc.Cast(t.ServerPosition);

        }

        private enum ComboType
        {
            Auto,
            ComboQR,
            ComboWR,
            ComboER
        }

        private enum ComboKill
        {
            None,
            FullCombo,
            WithoutW
        }

        private static void ExecuteCombo()
        {
            if (!R.IsReady())
                return;

            _isComboCompleted = false;

            Obj_AI_Hero t;
            var cdQEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires;
            var cdWEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;
            var cdEEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;

            var cdQ = Game.Time < cdQEx ? cdQEx - Game.Time : 0;
            var cdW = Game.Time < cdWEx ? cdWEx - Game.Time : 0;
            var cdE = Game.Time < cdEEx ? cdEEx - Game.Time : 0;

            if (vComboType == ComboType.ComboQR && Q.IsReady())
            {

                t = GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (t == null)
                    return;

                Q.CastOnUnit(t, true);
                R.CastOnUnit(t, true);
            }

            if (vComboType == ComboType.ComboWR && W.IsReady())
            {
                t = GetTarget(W.Range, TargetSelector.DamageType.Magical);
                if (t == null)
                    return;

                if (!LeBlancStillJumped)
                    W.Cast(t, true, true);

                R.Cast(t, true, true);
            }

            if (vComboType == ComboType.ComboER && E.IsReady())
            {
                t = GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (t == null)
                    return;

                E.Cast(t);
                R.Cast(t);
            }
            _isComboCompleted = true;

            t = GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            UserSummoners(t);
        }

        private static void Combo()
        {
            var cdQEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).CooldownExpires;
            var cdWEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;
            var cdEEx = ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).CooldownExpires;

            var cdQ = Game.Time < cdQEx ? cdQEx - Game.Time : 0;
            var cdW = Game.Time < cdWEx ? cdWEx - Game.Time : 0;
            var cdE = Game.Time < cdEEx ? cdEEx - Game.Time : 0;

            var t = GetTarget(Q.Range * 2, TargetSelector.DamageType.Magical);
            var useR = (Config.Item("DontCombo" + t.BaseSkinName) != null &&
                        Config.Item("DontCombo" + t.BaseSkinName).GetValue<bool>() == false);

            if (!t.IsValidTarget())
                return;
                
            if (vComboKill == ComboKill.WithoutW && !LeBlancStillJumped)
            {
                W.Cast(t.Position);
            }

            if (R.IsReady())
            {
                if (vComboType == ComboType.Auto)
                {
                    if (Q.Level > W.Level)
                    {
                        if (Q.IsReady())
                            ExecuteCombo();
                    }
                    else
                    {
                        if (W.IsReady())
                            ExecuteCombo();
                    }
                }
                else if ((vComboType == ComboType.ComboQR && Q.IsReady()) ||
                         (vComboType == ComboType.ComboWR && W.IsReady()) ||
                         (vComboType == ComboType.ComboER && E.IsReady()))
                    ExecuteCombo();
                else
                {
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancChaosOrbM") // R-Q
                    {
                        t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                        if (t.IsValidTarget(Q.Range) &&
                            t.Health < GetRQDamage + ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q))
                            R.CastOnUnit(t);
                    }
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM") // R-W
                    {
                        t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                        if (t.IsValidTarget(W.Range) &&
                            t.Health < GetRQDamage + ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q))
                            R.Cast(t, false, true);
                    }
                    if (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSoulShackleM") // R-E
                    {
                        t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                        if (t.IsValidTarget(E.Range) &&
                            t.Health < GetRQDamage + ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q))
                            R.CastIfHitchanceEquals(t, GetEHitChance);
                    }
                    _isComboCompleted = true;
                }
                return;
            }

            if (Q.IsReady() && t.IsValidTarget(Q.Range) && _isComboCompleted)
            {
                if (vComboType == ComboType.ComboQR)
                {
                    if (!R.IsReady())
                        Q.CastOnUnit(t);
                }
                else
                {
                    Q.CastOnUnit(t);
                }
            }

            if (W.IsReady() && t.IsValidTarget(W.Range) && !LeBlancStillJumped && _isComboCompleted)
            {
                if (vComboType == ComboType.ComboWR)
                {
                    if (!R.IsReady())
                        W.Cast(t, true, true);
                }
                else
                {
                    W.Cast(t, true, true);
                }
            }

            if (E.IsReady() && t.IsValidTarget(E.Range) && _isComboCompleted)
            {
                if (vComboType == ComboType.ComboER)
                {
                    if (!R.IsReady())
                        E.CastIfHitchanceEquals(t, GetEHitChance);
                }
                else
                {
                    E.CastIfHitchanceEquals(t, GetEHitChance);
                }
            }

            if (t != null && IgniteSlot != SpellSlot.Unknown &&
                ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready)
            {
                if (ObjectManager.Player.Distance(t) < 650 &&
                    ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite) >= t.Health)
                {
                    ObjectManager.Player.Spellbook.CastSpell(IgniteSlot, t);
                }
            }

        }

        private static void Harass()
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);

            var useQ = Config.Item("HarassUseQ").GetValue<bool>() &&
                       ObjectManager.Player.ManaPercentage() >= Config.Item("HarassManaQ").GetValue<Slider>().Value;
            var useW = Config.Item("HarassUseW").GetValue<bool>() &&
                       ObjectManager.Player.ManaPercentage() >= Config.Item("HarassManaW").GetValue<Slider>().Value;
            var useE = Config.Item("HarassUseE").GetValue<bool>() &&
                       ObjectManager.Player.ManaPercentage() >= Config.Item("HarassManaE").GetValue<Slider>().Value;

            if (useQ && qTarget != null && Q.IsReady())
                Q.CastOnUnit(qTarget);

            if (useW && wTarget != null && W.IsReady())
                W.Cast(wTarget, true, true);

            if (useE && eTarget != null && E.IsReady())
                E.CastIfHitchanceEquals(eTarget, GetEHitChance);
        }

        private static float GetRQDamage
        {
            get
            {
                var xDmg = 0f;
                var perDmg = new[] { 100f, 200f, 300 };

                xDmg += ((ObjectManager.Player.BaseAbilityDamage + ObjectManager.Player.FlatMagicDamageMod) * .65f) +
                        perDmg[R.Level - 1];
                var t = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget(2000))
                    xDmg +=
                        (float)
                            ObjectManager.Player.GetSpellDamage(
                                t, (vComboType == ComboType.ComboQR ? SpellSlot.Q : SpellSlot.E));
                return xDmg;
            }
        }

        private static float GetRWDamage
        {
            get
            {
                var xDmg = 0f;
                var perDmg = new[] { 150f, 300f, 450f };
                xDmg += ((ObjectManager.Player.BaseAbilityDamage + ObjectManager.Player.FlatMagicDamageMod) * .98f) +
                        perDmg[R.Level - 1];

                var t = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Magical);
                if (t.IsValidTarget(2000))
                    xDmg += (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.W);

                return xDmg;
            }
        }

        private static float GetComboDamage(Obj_AI_Hero t)
        {
            var fComboDamage = 0f;

            if (!t.IsValidTarget(2000))
                return 0f;

            fComboDamage += Q.IsReady() ? (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q) : 0;

            fComboDamage += W.IsReady() ? (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.W) : 0;

            fComboDamage += E.IsReady() ? (float) ObjectManager.Player.GetSpellDamage(t, SpellSlot.E) : 0;

            if (R.IsReady())
            {
                if (vComboType == ComboType.ComboQR || vComboType == ComboType.ComboER)
                {
                    fComboDamage += GetRQDamage;
                }

                if (vComboType == ComboType.ComboWR)
                {
                    fComboDamage += GetRWDamage;
                }
            }

            fComboDamage += IgniteSlot != SpellSlot.Unknown &&
                            ObjectManager.Player.Spellbook.CanUseSpell(IgniteSlot) == SpellState.Ready
                ? (float) ObjectManager.Player.GetSummonerSpellDamage(t, Damage.SummonerSpell.Ignite)
                : 0f;

            fComboDamage += Items.CanUseItem(3092)
                ? (float) ObjectManager.Player.GetItemDamage(t, Damage.DamageItems.FrostQueenClaim)
                : 0;

            return (float) fComboDamage;
        }

        private static bool xEnemyHaveSoulShackle(Obj_AI_Hero vTarget)
        {
            return (vTarget.HasBuff("LeblancSoulShackle"));
        }

        private static void Run()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            var useW = Config.Item("RunUseW").GetValue<bool>();
            var useR = Config.Item("RunUseR").GetValue<bool>();

            if (useW && W.IsReady() && !LeBlancStillJumped)
                W.Cast(Game.CursorPos);

            if (useR && R.IsReady() && ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSlideM")
                R.Cast(Game.CursorPos);
        }

        private static void DoubleStun()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

            if (Config.Item("ComboDblStun").GetValue<KeyBind>().Active)
            {
                Drawing.DrawText(Drawing.Width * 0.45f, Drawing.Height * 0.78f, Color.Red, "Double Stun Active!");

                foreach (var enemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            enemy =>
                                enemy.IsEnemy && !enemy.IsDead && enemy.IsVisible &&
                                ObjectManager.Player.Distance(enemy) < E.Range + 200 && !xEnemyHaveSoulShackle(enemy)))
                {
                    if (E.IsReady() && ObjectManager.Player.Distance(enemy) < E.Range)
                    {
                        E.CastIfHitchanceEquals(enemy, GetEHitChance);
                    }
                    else if (R.IsReady() && ObjectManager.Player.Distance(enemy) < E.Range &&
                             ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "LeblancSoulShackleM")
                    {
                        R.CastIfHitchanceEquals(enemy, GetEHitChance);
                    }
                }
            }
        }

        private static void SmartW()
        {

            if (!Config.Item("ComboSmartW").GetValue<bool>())
                return;

            var vTarget = EnemyHaveSoulShackle;
            foreach (var existingSlide in ExistingSlide)
            {
                var slide = existingSlide;

                var onSlidePositionEnemyCount =
                    (from enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                enemy =>
                                    enemy.Team != ObjectManager.Player.Team && !enemy.IsDead &&
                                    enemy.Distance(slide.Position) < 300f)
                        select enemy).Count();

                var onPlayerPositionEnemyCount =
                    (from enemy in
                        ObjectManager.Get<Obj_AI_Hero>()
                            .Where(
                                enemy =>
                                    enemy.Team != ObjectManager.Player.Team &&
                                    ObjectManager.Player.Distance(enemy) < Q.Range)
                        select enemy).Count();


                if (Config.Item("ComboDblStun").GetValue<KeyBind>().Active && E.IsReady() && R.IsReady())
                {
                    var onPlayerPositionEnemyCount2 =
                        (from enemy in
                            ObjectManager.Get<Obj_AI_Hero>()
                                .Where(
                                    enemy =>
                                        enemy.Team != ObjectManager.Player.Team &&
                                        ObjectManager.Player.Distance(enemy) < E.Range)
                            select enemy).Count();

                    if (onPlayerPositionEnemyCount2 == 2) {}
                }
                if (onPlayerPositionEnemyCount > onSlidePositionEnemyCount)
                {
                    if (LeBlancStillJumped)
                    {
                        var qTarget = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                        if (qTarget == null)
                            return;
                        if ((ObjectManager.Player.Health < qTarget.Health || ObjectManager.Player.Level < qTarget.Level))
                            W.Cast();
                        else
                        {
                            if (Q.IsReady())
                                Q.CastOnUnit(qTarget);
                            if (R.IsReady())
                                R.CastOnUnit(qTarget);
                            if (E.IsReady())
                                E.Cast(qTarget);
                            W.Cast();
                        }
                    }
                }
                Game.PrintChat(slide.Position.ToString());
                Render.Circle.DrawCircle(slide.Position, 400f, Color.Red);

                Game.PrintChat("Slide Pos. Enemy Count: " + onSlidePositionEnemyCount);
                Game.PrintChat("ObjectManager.Player Pos. Enemy Count: " + onPlayerPositionEnemyCount);

                Game.PrintChat("W Posision : " + existingSlide.Position);
                Game.PrintChat("Target Position : " + vTarget.Position);
            }
        }


        private static void LaneClear()
        {
            if (!Orbwalking.CanMove(40))
                return;

            var useQ = Config.Item("LaneClearUseQ").GetValue<bool>();
            var useW = Config.Item("LaneClearUseW").GetValue<bool>();

            if (useQ && Q.IsReady())
            {
                var minionsQ = MinionManager.GetMinions(
                    ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.NotAlly);
                foreach (Obj_AI_Base vMinion in 
                    from vMinion in minionsQ
                    let vMinionQDamage = ObjectManager.Player.GetSpellDamage(vMinion, SpellSlot.Q)
                    where
                        vMinion.Health <= vMinionQDamage &&
                        vMinion.Health > ObjectManager.Player.GetAutoAttackDamage(vMinion)
                    select vMinion)
                {
                    Q.CastOnUnit(vMinion);
                }
            }

            var rangedMinionsW = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, W.Range + W.Width + 20);
            if (!useW || !W.IsReady())
                return;

            var minionsW = W.GetCircularFarmLocation(rangedMinionsW, W.Width * 0.75f);

            if (minionsW.MinionsHit < 2 || !W.IsInRange(minionsW.Position.To3D()))
                return;

            W.Cast(minionsW.Position);

        }

        private static void JungleFarm()
        {
            var useQ = Config.Item("JungleFarmUseQ").GetValue<bool>();
            var useW = Config.Item("JungleFarmUseW").GetValue<bool>();
            var useE = Config.Item("JungleFarmUseE").GetValue<bool>();

            var mobs = MinionManager.GetMinions(
                ObjectManager.Player.ServerPosition, Q.Range, MinionTypes.All, MinionTeam.Neutral,
                MinionOrderTypes.MaxHealth);

            if (mobs.Count <= 0)
                return;
            var mob = mobs[0];
            if (useQ && Q.IsReady())
                Q.CastOnUnit(mob);

            if (useW && W.IsReady() && mobs.Count >= 2)
                W.Cast(mob.Position);

            if (useE && E.IsReady())
                E.Cast(mob);
        }

        private static void DoToggleHarass()
        {
            if (Config.SubMenu("Harass").Item("HarassUseTQ").GetValue<KeyBind>().Active)
            {
                var t = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);
                if (t != null && Q.IsReady() &&
                    ObjectManager.Player.ManaPercentage() >=
                    Config.SubMenu("Harass").Item("HarassManaQ").GetValue<Slider>().Value)
                    Q.CastOnUnit(t);
            }

            if (Config.SubMenu("Harass").Item("HarassUseTW").GetValue<KeyBind>().Active)
            {
                var t = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Magical);
                if (t != null && W.IsReady() && !LeBlancStillJumped &&
                    ObjectManager.Player.ManaPercentage() >=
                    Config.SubMenu("Harass").Item("HarassManaW").GetValue<Slider>().Value)
                    W.Cast(t, true, true);
            }

            if (Config.SubMenu("Harass").Item("HarassUseTE").GetValue<KeyBind>().Active)
            {
                var t = TargetSelector.GetTarget(E.Range, TargetSelector.DamageType.Magical);
                if (t != null && E.IsReady() &&
                    ObjectManager.Player.ManaPercentage() >=
                    Config.SubMenu("Harass").Item("HarassManaE").GetValue<Slider>().Value)
                    E.CastIfHitchanceEquals(t, GetEHitChance);
            }
        }

        private static void RefreshComboType()
        {
            var xCombo = Config.Item("ComboSetOption").GetValue<StringList>().SelectedIndex;
            switch (xCombo)
            {
                case 0:
                    vComboType = Q.Level > W.Level ? ComboType.ComboQR : ComboType.ComboWR;
                    break;
                case 1: //Q-R
                    vComboType = ComboType.ComboQR;
                    break;
                case 2: //W-R
                    vComboType = ComboType.ComboWR;
                    break;
                case 3: //E-R
                    vComboType = ComboType.ComboER;
                    break;
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead)
                return;

            RefreshComboType();

            var t = TargetSelector.GetTarget(W.Range * 2, TargetSelector.DamageType.Physical);
            {
                var xComboText = "Combo Kill";
                if (t.IsValidTarget(W.Range))
                {
                    if (t.Health < GetComboDamage(t))
                    {
                        vComboKill = ComboKill.FullCombo;
                        Drawing.DrawText(t.HPBarPosition.X + 145, t.HPBarPosition.Y + 20, Color.Beige, xComboText);
                    }
                }

                else if (t.IsValidTarget(W.Range * 2 - 30))
                {
                    if (t.Health < GetComboDamage(t) - ObjectManager.Player.GetSpellDamage(t, SpellSlot.W))
                    {
                        vComboKill = ComboKill.WithoutW;
                        xComboText = "Jump + " + xComboText;
                        Drawing.DrawText(t.HPBarPosition.X + 145, t.HPBarPosition.Y + 20, Color.Beige, xComboText);
                    }
                }
            }

            _isComboCompleted = !R.IsReady();

            if (Config.Item("ComboDblStun").GetValue<KeyBind>().Active)
                DoubleStun();


            if (Config.Item("RunActive").GetValue<KeyBind>().Active)
                Run();

            if (Config.Item("ComboActive").GetValue<KeyBind>().Active)
                Combo();

            DoToggleHarass();

            if (Config.Item("HarassActive").GetValue<KeyBind>().Active)
                Harass();

            if (Config.Item("LaneClearActive").GetValue<KeyBind>().Active)
            {
                if (ObjectManager.Player.ManaPercentage() >= Config.Item("LaneClearMana").GetValue<Slider>().Value)
                    LaneClear();
            }

            if (Config.Item("JungleFarmActive").GetValue<KeyBind>().Active)
            {
                if (ObjectManager.Player.ManaPercentage() >= Config.Item("JungleFarmMana").GetValue<Slider>().Value)
                    JungleFarm();
            }
        }

        #region Drawing_OnDraw

        private static void Drawing_OnDraw(EventArgs args)
        {

            if (Config.SubMenu("Combo").Item("ComboShowInfo").GetValue<bool>())
            {
                var xComboStr = "Combo Mode: ";
                var xCombo = Config.Item("ComboSetOption").GetValue<StringList>().SelectedIndex;
                switch (xCombo)
                {
                    case 0:
                        xComboStr += "Auto";
                        break;

                    case 1: //Q-R
                        xComboStr += "Q-R";
                        break;

                    case 2: //W-R
                        xComboStr += "W-R";
                        break;

                    case 3: //E-R
                        xComboStr += "E-R";
                        break;
                }
                Drawing.DrawText(Drawing.Width * 0.45f, Drawing.Height * 0.80f, Color.GreenYellow, xComboStr);
            }

            if (Config.SubMenu("Harass").Item("HarassShowInfo").GetValue<bool>())
            {
                var xHarassInfo = "";
                if (Config.SubMenu("Harass").Item("HarassUseTQ").GetValue<KeyBind>().Active)
                    xHarassInfo += "Q - ";

                if (Config.SubMenu("Harass").Item("HarassUseTW").GetValue<KeyBind>().Active)
                    xHarassInfo += "W - ";

                if (Config.SubMenu("Harass").Item("HarassUseTE").GetValue<KeyBind>().Active)
                    xHarassInfo += "E - ";
                if (xHarassInfo.Length < 1)
                {
                    xHarassInfo = "Harass Toggle: OFF   ";
                }
                else
                {
                    xHarassInfo = "Harass Toggle: " + xHarassInfo;
                }
                xHarassInfo = xHarassInfo.Substring(0, xHarassInfo.Length - 3);
                Drawing.DrawText(Drawing.Width * 0.44f, Drawing.Height * 0.82f, Color.Wheat, xHarassInfo);
            }

            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && spell.Level > 0)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, spell.Range, menuItem.Color);
            }

            var wqRange = Config.Item("WQRange").GetValue<Circle>();
            if (wqRange.Active && Q.IsReady() && W.IsReady())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range + Q.Range, wqRange.Color);
            }

            var activeERange = Config.Item("ActiveERange").GetValue<Circle>();
            if (activeERange.Active && EnemyHaveSoulShackle != null)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, 1100f, activeERange.Color);
            }
            /*
            var wObjPosition = Config.Item("WObjPosition").GetValue<Circle>();
            var wObjTimeTick = Config.Item("WObjTimeTick").GetValue<bool>();

             * foreach (var existingSlide in ExistingSlide)
            {
                if (wObjPosition.Active)
                    Render.Circle.DrawCircle(existingSlide.Position, 110f, wObjPosition.Color);

                if (!wObjTimeTick) continue;
                if (!(existingSlide.ExpireTime > Game.Time)) continue;

                var time = TimeSpan.FromSeconds(existingSlide.ExpireTime - Game.Time);
                var pos = Drawing.WorldToScreen(existingSlide.Position);
                var display = string.Format("{0}:{1:D2}", time.Minutes, time.Seconds);
                Drawing.DrawText(pos.X - display.Length * 3, pos.Y - 65, Color.GreenYellow, display);
            }

            foreach (
                var enemy in
                    ObjectManager.Get<Obj_AI_Hero>()
                        .Where(
                            enemy =>
                                enemy.IsEnemy && !enemy.IsDead && enemy.IsVisible &&
                                ObjectManager.Player.Distance(enemy) < E.Range + 1400 &&
                                !xEnemyHaveSoulShackle(enemy))) 
            {
                Render.Circle.DrawCircle(enemy.Position, 75f, Color.GreenYellow);
            }
             */
        }

        #endregion

        #region GetEnemy

        private static Obj_AI_Hero GetTarget(float vDefaultRange = 0,
            TargetSelector.DamageType vDefaultDamageType = TargetSelector.DamageType.Physical)
        {
            if (vDefaultRange < 0.00001)
                vDefaultRange = Q.Range;

            if (!Config.Item("AssassinActive").GetValue<bool>())
                return TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType);

            var assassinRange = Config.Item("AssassinSearchRange").GetValue<Slider>().Value;

            var vEnemy =
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        enemy =>
                            enemy.Team != ObjectManager.Player.Team && !enemy.IsDead && enemy.IsVisible &&
                            Config.Item("Assassin" + enemy.ChampionName) != null &&
                            Config.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                            ObjectManager.Player.Distance(enemy) < assassinRange);

            if (Config.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
            {
                vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
            }

            Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

            Obj_AI_Hero t = !objAiHeroes.Any()
                ? TargetSelector.GetTarget(vDefaultRange, vDefaultDamageType)
                : objAiHeroes[0];

            return t;
        }

        #endregion

        #region GetEHitChance

        private static HitChance GetEHitChance
        {
            get
            {
                HitChance hitChance;
                var eHitChance = Config.Item("ComboSetEHitCh").GetValue<StringList>().SelectedIndex;
                switch (eHitChance)
                {
                    case 0:
                    {
                        hitChance = HitChance.Low;
                        break;
                    }
                    case 1:
                    {
                        hitChance = HitChance.Medium;
                        break;
                    }
                    case 2:
                    {
                        hitChance = HitChance.High;
                        break;
                    }
                    case 3:
                    {
                        hitChance = HitChance.VeryHigh;
                        break;
                    }
                    case 4:
                    {
                        hitChance = HitChance.Immobile;
                        break;
                    }
                    default:
                    {
                        hitChance = HitChance.High;
                        break;
                    }
                }
                return hitChance;
            }
        }

        #endregion
    }
}
