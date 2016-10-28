using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.math;

namespace CSGO_Analytics.src.encounterdetect.datasource
{
    public enum Hitgroup
    {
        Generic = 0,
        Head = 1,
        Chest = 2,
        Stomach = 3,
        LeftArm = 4,
        RightArm = 5,
        LeftLeg = 6,
        RightLeg = 7,
        Gear = 10,
    };

    public enum NadeEventType
    {
        HEGRENADE, SMOKE, FIRE, FLASH, DECOY
    };

    public class GameEvent
    {
        /// <summary>
        /// Ticks in range N around this gameevent
        /// </summary>
        public List<Tick> nearest_N_Ticks { get; set; } //(maybe clone them? or leave them)

        /// <summary>
        /// Every gameevent has a actor. The player this gameevent is concerning.
        /// </summary>
        public Player actor { get; set; }

        /// <summary>
        /// Tickid where gameevent took place
        /// </summary>
        public int starttick_id { get; set; }

        public virtual Link createLink() { return null; }

        public static GameEvent build(dynamic devent)
        {
            string eventtype = devent.gameevent;
            switch (eventtype)
            {
                //ALL CSGO EVENTS
                case "player_jumped":
                    return new PlayerJumped();
                case "player_position":
                    return new PlayerPositionUpdate();
                case "player_stepped":
                    return new PlayerStepped();
                case "player_spotted":
                    return new PlayerSpotted();
                case "player_killed":
                    return new PlayerKilledEvent();
                case "player_hurt":
                    return new PlayerHurtEvent();
                case "weapon_fire":
                    return new WeaponFireEvent();
                case "smoke_started":
                case "smoke_ended":
                case "fire_started":
                case "fire_ended":
                case "flashbang_exploded":
                    return new NadeEvent();
                default:
                    return null;
            }
        }

    }

    public class PlayerJumped : GameEvent { }

    public class PlayerStepped : GameEvent { }

    public class PlayerPositionUpdate : GameEvent { }

    /// <summary>
    /// Event for the case, that a player saw another player from a different team.
    /// Here we build a combatlink directly
    /// </summary>
    public class PlayerSpotted : GameEvent {
        /// <summary>
        /// Player who spotted the actor
        /// </summary>
        public Player spotter { get; set; }

        override public Link createLink()
        {
            Console.WriteLine("createLink PlayerSpotted");
            return new Link(actor, spotter, ComponentType.COMBATLINK, Direction.DEFAULT);
        }
    }

    public class WeaponFireEvent : GameEvent // TODO:Check if a playerspotted or playerhurt event is near, if so he tried to shoot at sb but missed
    {
        /// <summary>
        /// Weapon which was fired in the event
        /// </summary>
        public Weapon weapon { get; set; }

        override public Link createLink()
        {
            Console.WriteLine("createLink WeaponFireEvent");
            return null;
        }
    }

    /// <summary>
    /// Event for the case, that a player hit another player from a different team.
    /// Here we build a combatlink directly
    /// </summary>
    public class PlayerHurtEvent : WeaponFireEvent
    {
        /// <summary>
        /// Victim hit by the actor
        /// </summary>
        public Player victim { get; set; }

        /// <summary>
        /// Damage actor did to the victim
        /// </summary>
        public int damage { get; set; }

        /// <summary>
        /// Hitgroup where the shoot hit
        /// </summary>
        public Hitgroup hitgroup { get; set; }

        override public Link createLink()
        {
            Console.WriteLine("createLink PlayerHurtEvent");
            return new Link(actor,victim, ComponentType.COMBATLINK, Direction.DEFAULT);
        }
    }

    /// <summary>
    /// Event for the case, that a player killed another player from a different team.
    /// Here we build a combatlink directly
    /// </summary>
    public class PlayerKilledEvent : PlayerHurtEvent
    {
        /// <summary>
        /// Whether the bullet who killed penetrated a gameobject first
        /// </summary>
        public bool penetrated { get; set; }

        override public Link createLink()
        {
            Console.WriteLine("createLink PlayerKilledEvent");
            return new Link(actor, victim, ComponentType.COMBATLINK, Direction.DEFAULT);
        }

    }

    /// <summary>
    /// 
    /// </summary>
    public class NadeEvent : GameEvent
    {
        /// <summary>
        /// Player who eventually got supported by the actor//TODO:! wie finden!!!
        /// </summary>
        public Player supportedplayer { get; set; }

        /// <summary>
        /// Position from where the nade has been thrown
        /// </summary>
        public Vector startpositon { get; set; }
        /// <summary>
        /// Position where nade came to rest(Smoke,Fire,Decoy)
        /// </summary>
        public Vector endposition { get; set; }

        /// <summary>
        /// Position where nade exploded
        /// </summary>
        public Vector explosionposition { get; set; }

        /// <summary>
        /// Tickid where nade ended
        /// </summary>
        public int endtick_id { get; set; }

        /// <summary>
        /// Tickid where nade exploded
        /// </summary>
        public int explodetick_id { get; set; }

        override public Link createLink()
        {
            Console.WriteLine("createLink PlayerKilledEvent");
            return new Link(actor, supportedplayer, ComponentType.SUPPORTLINK, Direction.DEFAULT);
        }
    }
}
