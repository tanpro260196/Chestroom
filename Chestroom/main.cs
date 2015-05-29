using System;
using Terraria;
using TShockAPI;
using TerrariaApi.Server;
using System.Reflection;
using System.IO;
using System.Data;
using System.Text;
using TShockAPI.DB;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using Newtonsoft.Json;

namespace CHESTROOM
{
    [ApiVersion(1, 17)]
    public class Chestroom : TerrariaPlugin
    {
        public static Config config = new Config();

        static short[] torchframey = new short[] { 0, 22, 44, 66, 88, 110, 132, 154, 176, 198, 220, 242 };
        static short[] platformframey = new short[] { 0, 18, 36, 54, 72, 90, 108, 144, 234, 228 };
        static byte[] tiles = new byte[] { 38, 39, 41, 43, 44, 45, 47, 54, 118, 119, 121, 122, 140, 145, 146, 148, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 175, 176, 177, 189, 190, 191, 193, 194, 195, 196, 197, 198, 202, 206, 208, 225, 226, 229, 230, 248, 249, 250, 251, 252, 253 };
        static byte[] walls = new byte[] { 4, 5, 6, 7, 9, 10, 11, 12, 19, 21, 22, 23, 24, 25, 26, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 41, 42, 43, 45, 46, 47, 72, 73, 74, 75, 76, 78, 82, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 105, 109, 110, 113, 114, 115 };
        static short[] chests = new short[] { 1, 3, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 };

        static int MaxItems { get { return Main.maxItemTypes + 47; } }
        static int NumberOfChests { get { return (int)Math.Ceiling((decimal)MaxItems / 40); } }
        static int ChestsPerRow { get { return config.ChestsPerRow; } }
        static int Rows { get { return (int)Math.Ceiling((decimal)NumberOfChests / ChestsPerRow); } }
        static int Width { get { return 4 * ChestsPerRow + 2; } }
        static int Height { get { return Rows * 5 + 1; } }

        Random rnd = new Random();
        IDbConnection Database;
        bool usinginfchests;

        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public override string Author
        {
            get { return "Ancientgods"; }
        }

        public override string Name
        {
            get { return "Chestroom"; }
        }

        public override string Description
        {
            get { return "Generates a chestroom containing all items"; }
        }

        public override void Initialize()
        {
            Commands.ChatCommands.Add(new Command("chestroom.create", chestroom, "chestroom", "cr") { AllowServer = false });
            Commands.ChatCommands.Add(new Command("chestroom.reload", Reload_Config, "crreload"));

            if (File.Exists(Path.Combine(Environment.CurrentDirectory, "ServerPlugins\\InfiniteChests.dll")))
            {
                switch (TShock.Config.StorageType.ToLower())
                {
                    case "mysql":
                        string[] host = TShock.Config.MySqlHost.Split(':');
                        Database = new MySqlConnection()
                        {
                            ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
                                    host[0],
                                    host.Length == 1 ? "3306" : host[1],
                                    TShock.Config.MySqlDbName,
                                    TShock.Config.MySqlUsername,
                                    TShock.Config.MySqlPassword)
                        };
                        break;
                    case "sqlite":
                        string sql = Path.Combine(TShock.SavePath, "chests.sqlite");
                        Database = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
                        break;
                }
                usinginfchests = true;
            }

            ReadConfig();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            base.Dispose(disposing);
        }

        public Chestroom(Main game)
            : base(game)
        {
            Order = 1;
        }

        private void chestroom(CommandArgs args)
        {
            Stopwatch sw = Stopwatch.StartNew();

            int X = args.Player.TileX;
            int Y = args.Player.TileY;

            string cmd = args.Parameters.Count > 0 ? args.Parameters[0] : "";
            switch (cmd)
            {
                case "tl":
                    X -= 2;
                    Y -= 2;
                    break;
                case "tr":
                    X -= (Width - 4);
                    Y -= 2;
                    break;
                case "tc":
                    X -= (Width - 2) / 2;
                    break;

                case "bl":
                    X -= 2;
                    Y -= (Height - 4);
                    break;
                case "br":
                    X -= (Width - 4);
                    Y -= (Height - 4);
                    break;
                case "bc":
                    X -= (Width - 2) / 2;
                    Y -= (Height - 4);
                    break;
                default:
                    args.Player.SendErrorMessage("Invalid syntax! Proper syntax: /chestroom <tl/tr/bl/br/tc/bc>");
                    args.Player.SendErrorMessage("t = top, l = left, r = right, b = bottom, c = center");
                    args.Player.SendErrorMessage("This is where you will stand when the chestroom spawns.");
                    return;
            }
            args.Player.SendSuccessMessage("Creating Chestroom...");

            byte tileid = config.CustomRoom ? config.TileId : tiles[rnd.Next(0, tiles.Length)];
            short chestid = config.CustomRoom ? config.ChestId : chests[rnd.Next(0, chests.Length)];
            byte bgwall = config.CustomRoom ? config.BgWall : walls[rnd.Next(0, walls.Length)];
            short pframey = config.CustomRoom ? config.pFrameY : platformframey[rnd.Next(0, platformframey.Length)];
            short tframey = config.CustomRoom ? config.tFrameY : torchframey[rnd.Next(0, torchframey.Length)];
            int count = 0;
            for (int ch = 0; ch < 1000; ch++)
            {
                if (Main.chest[ch] != null)
                    count++;
            }
            if (count > (1000 - NumberOfChests))
            {
                args.Player.SendInfoMessage("Creating this chestroom would exceed the chest limit, chestroom cancelled.");
                return;
            }
            bool first = true;
            int placed = ChestsPerRow * Rows;
            int chestitem = Main.maxItemTypes - 1;

            for (int y = Height - 1; y >= 0; y--)
            {
                for (int x = Width - 1; x >= 0; x--)
                {
                    if (x > 0 && x < Width - 1 && y > 0 && y < Height - 1)
                    {
                        Main.tile[x + X, y + Y] = new Tile() { wall = bgwall };
                    }
                    if (x == 0 || x == Width - 1 || y == 0 || y == Height - 1)
                    {
                        Main.tile[x + X, y + Y] = new Tile() { type = tileid };
                        Main.tile[x + X, y + Y].active(true);
                    }
                    if (y % 5 == 0 && y > 0 && y < Height - 1)
                    {
                        Main.tile[x + X, y + Y].active(true);
                        if ((x % 4 == 2 || x % 4 == 3 || x == 1 || x == Width - 2))
                        {
                            Main.tile[x + X, y + Y].type = tileid;
                        }
                        else if ((x % 4 == 0 || x % 4 == 1) && x > 1 && x < Width - 2)
                        {
                            Main.tile[x + X, y + Y].type = (byte)19;
                            Main.tile[x + X, y + Y].frameY = pframey;
                        }
                    }
                    if (y % 5 == 1 && (x == 1 || x == Width - 2))
                    {
                        Main.tile[x + X, y + Y].active(true);
                        Main.tile[x + X, y + Y].type = 4;
                        Main.tile[x + X, y + Y].frameY = tframey;
                    }
                    if (y % 5 == 2 && x % 4 == 3 && x > 0 && x < Width - 2)
                    {
                        placed--;
                        if (placed < NumberOfChests)
                        {
                            WorldGen.AddBuriedChest(x + X, y + Y, 1, false, chestid);
                            if (Main.chest[count] != null)
                            {
                                for (int i = 39; i >= 0; i--)
                                {
                                    if (chestitem == 0)
                                        chestitem--;

                                    if (chestitem < -48)
                                        break;

                                    if (first)
                                    {
                                        i -= 40 - MaxItems % 40;
                                        first = false;
                                    }
                                    Item itm = TShock.Utils.GetItemById(chestitem);
                                    itm.stack = itm.maxStack;
                                    Main.chest[count].item[i] = itm;
                                    chestitem--;
                                }
                            }
                            count++;
                        }
                    }
                }
            }
            if (usinginfchests)
            {
                ConvRefillChests(count - NumberOfChests, NumberOfChests);
            }
            informplayers();
            sw.Stop();
            args.Player.SendInfoMessage(string.Format("Chestroom created in {0} seconds. ({1} items in {2} chests)", sw.Elapsed.TotalSeconds, MaxItems, NumberOfChests));
        }

        public void ConvRefillChests(int s, int e)
        {
            var items = new StringBuilder();
            for (int i = s; i < s + e; i++)
            {
                Terraria.Chest c = Main.chest[i];
                if (c != null)
                {
                    for (int j = 0; j < 40; j++)
                        items.Append(c.item[j].netID).Append(",").Append(c.item[j].stack).Append(",").Append(c.item[j].prefix).Append(",");
                    Database.Query("INSERT INTO Chests (X, Y, Account, Items, Flags, WorldID) VALUES (@0, @1, '', @2, @3, @4)",
                        c.x, c.y, items.ToString(0, items.Length - 1), 5, Main.worldID);
                    items.Clear();
                    Main.chest[i] = null;
                }
            }
        }

        public static void informplayers(bool hard = false)
        {
            foreach (TSPlayer ts in TShock.Players)
            {
                if ((ts != null) && (ts.Active))
                {
                    for (int i = 0; i < 255; i++)
                    {
                        for (int j = 0; j < Main.maxSectionsX; j++)
                        {
                            for (int k = 0; k < Main.maxSectionsY; k++)
                            {
                                Netplay.serverSock[i].tileSection[j, k] = false;
                            }
                        }
                    }
                }
            }
        }

        public class Config
        {
            public int ChestsPerRow = (int)Math.Ceiling(Math.Sqrt(NumberOfChests));
            public bool CustomRoom;
            public byte TileId = 38;
            public short ChestId = 1;
            public byte BgWall = 4;
            public short pFrameY = 18;
            public short tFrameY = 22;
        }

        static bool ReadConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "Chestroom.json");
            try
            {
                if (File.Exists(filepath))
                {
                    using (var stream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (var sr = new StreamReader(stream))
                        {
                            var configString = sr.ReadToEnd();
                            config = JsonConvert.DeserializeObject<Config>(configString);
                            config.ChestsPerRow = config.ChestsPerRow < 2 ? 2 : Math.Min(NumberOfChests, config.ChestsPerRow);
                        }
                        stream.Close();
                    }
                    return true;
                }
                else
                {
                    Log.ConsoleError("Chestroom config not found. Creating new one...");
                    CreateConfig();
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
            }

            return false;
        }

        static void CreateConfig()
        {
            string filepath = Path.Combine(TShock.SavePath, "Chestroom.json");
            try
            {
                using (var stream = new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    using (var sr = new StreamWriter(stream))
                    {
                        config = new Config();
                        var configString = JsonConvert.SerializeObject(config, Formatting.Indented);
                        sr.Write(configString);
                    }
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.Message);
                config = new Config();
            }
        }

        void Reload_Config(CommandArgs args)
        {
            if (ReadConfig())
                args.Player.SendMessage("Chestroom config reloaded sucessfully.", Color.Green);

            else
                args.Player.SendErrorMessage("Chestroom config reloaded unsucessfully. Check logs for details.");
        }
    }
}
