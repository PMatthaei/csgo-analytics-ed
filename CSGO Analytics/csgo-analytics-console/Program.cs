using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSGO_Analytics.src.data.gameobjects;
using CSGO_Analytics.src.json;

namespace csgo_analytics_console
{
    class Program
    {
        static void Main(string[] args)
        {
            dynamic deserializedTick = CSGOReplayDeserializer.deserializeJSONString("{\"tick_id\": 1,\"tickevents\": [{ \"player\": {\"HP\": 14,\"armor\": 0,\"hasHelmet\": false,\"hasDefuser\": false, \"hasBomb\": false,\"isDucking\": false,\"isWalking\": false,\"isSpotted\": false,\"isScoped\": false,\"velocity\": 0.0,\"playername\": \"iNSANEwithin\",\"player_id\": 3,\"team\": \"Terrorist\",\"position\": {\"x\": -35.4354248,\"y\": -1883.96875,\"z\": -167.96875},\"facing\": {\"yaw\": 5.976563,\"pitch\": 278.085938}},\"gameevent\": \"bomb_picked\"}]}");
            var tick_id = deserializedTick.tick_id;
            Console.WriteLine(tick_id);

            foreach (var e in deserializedTick.tickevents){
                Console.WriteLine(e.gameevent);
                Console.WriteLine(e.player.position.x);
            }
            Console.ReadLine();
        }
    }
}
