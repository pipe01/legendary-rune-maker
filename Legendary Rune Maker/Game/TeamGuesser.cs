using Legendary_Rune_Maker.Data;
using Legendary_Rune_Maker.Data.Providers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nito.AsyncEx;
using RestSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Legendary_Rune_Maker.Game
{
    public interface ITeamGuesser
    {
        Task Load(IProgress<float> progress);

        IDictionary<Position, int> Guess(int[] team);
    }

    public class TeamGuesser : ITeamGuesser
    {
        private class TeamChampion
        {
            public string Name { get; set; }
            public string Key { get; set; }

            [JsonIgnore]
            public int ID => int.Parse(Key);
        }

        private class Player
        {
            public int ChampionID { get; set; }
            public List<PositionData> Positions { get; set; }
            public PositionData PositionInTeam { get; set; }

            public Player()
            {
            }

            public Player(int championID, List<PositionData> positions, PositionData positionInTeam)
            {
                this.ChampionID = championID;
                this.Positions = positions;
                this.PositionInTeam = positionInTeam;
            }
        }

        private IDictionary<int, PositionData[]> ChampionPositions = new ConcurrentDictionary<int, PositionData[]>();

        public async Task Load(IProgress<float> progress)
        {
            if (!File.Exists("guesser_data.json"))
            {
                var ugg = new UGGProvider();

                var version = await Riot.GetLatestVersionAsync();
                var data = JObject.Parse(await WebCache.String($"http://ddragon.leagueoflegends.com/cdn/{version}/data/en_US/champion.json"));
                var champions = data["data"].ToObject<Dictionary<string, TeamChampion>>();

                var ev = new AsyncCountdownEvent(8);

                int champCount = 0;
                int totalChamps = champions.Count;
                foreach (var champs in Partitioner.Create(champions.Values).GetPartitions(8))
                {
                    new Thread(async () =>
                    {
                        while (champs.MoveNext())
                        {
                            var champ = champs.Current;
                            var roles = (await ugg.GetDeepRoles(champ.ID)).ToArray();

                            Console.WriteLine($"{champ.Name}: {string.Join(", ", roles)}");
                            ChampionPositions[champ.ID] = roles;

                            progress?.Report((float)champCount / totalChamps);
                        }

                        ev.Signal();
                    }).Start();
                }

                await ev.WaitAsync();

                ugg = null;
                GC.Collect();

                File.WriteAllText("guesser_data.json", JsonConvert.SerializeObject(ChampionPositions));
            }
            else
            {
                ChampionPositions = JsonConvert.DeserializeObject<Dictionary<int, PositionData[]>>(File.ReadAllText("guesser_data.json"));
            }
        }

        public IDictionary<Position, int> Guess(int[] team)
        {
            var teamData = new Queue<Player>(team.Select(o => new Player(o, ChampionPositions[o].ToList(), default)));
            var positions = new Dictionary<Position, Player>();

            Player player;
            while (teamData.Count > 0)
            {
                player = teamData.Dequeue();

                int i = 0;
                PositionData probablePos;

                //Loop through every position that the champion can go in and select the most probable one
                while (true)
                {
                    if (i == player.Positions.Count)
                    {
                        throw new Exception("Nowhere to go");
                    }

                    probablePos = player.Positions[i++];

                    //If there is already a champion in this position
                    if (positions.TryGetValue(probablePos.Position, out var existing))
                    {
                        //Check if the current champion is more likely to go on this position than the champion
                        //that is already there
                        if (probablePos.Weight > existing.PositionInTeam.Weight)
                        {
                            //If so, re-add the existing champion's position to its pool and to the team queue
                            existing.Positions.Add(existing.PositionInTeam);
                            teamData.Enqueue(existing);

                            break;
                        }
                    }
                    else //Otherwise, the position is free so break out
                    {
                        break;
                    }
                }

                //Set this champion to its position and remove the position from the champion's pool
                positions[probablePos.Position] = player;
                player.PositionInTeam = probablePos;
                player.Positions.Remove(probablePos);
            }

            return positions.ToDictionary(o => o.Key, o => o.Value.ChampionID);
        }
    }
}
