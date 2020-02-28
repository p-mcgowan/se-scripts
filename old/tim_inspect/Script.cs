/*
Taleden's Inventory Manager
version 1.6.4 (2017-04-03)

"There are some who call me... TIM?"

Steam Workshop: http://steamcommunity.com/sharedfiles/filedetails/?id=546825757
User's Guide:   http://steamcommunity.com/sharedfiles/filedetails/?id=546909551


**********************
ADVANCED CONFIGURATION

The settings below may be changed if you like, but read the notes and remember
that any changes will be reverted when you update the script from the workshop.
*/

// Each "Type/" section can have multiple "/Subtype"s, which are formatted like
// "/Subtype,MinQta,PctQta,Label,Blueprint". Label and Blueprint specified only
// if different from Subtype, but Ingot and Ore have no Blueprint. Quota values
// are based on material requirements for various blueprints (some built in to
// the game, some from the community workshop).
const string DEFAULT_ITEMS = @"
AmmoMagazine/
/Missile200mm
/NATO_25x184mm,,,,NATO_25x184mmMagazine
/NATO_5p56x45mm,,,,NATO_5p56x45mmMagazine

Component/
/BulletproofGlass,50,2%
/Computer,30,5%,,ComputerComponent
/Construction,150,20%,,ConstructionComponent
/Detector,10,0.1%,,DetectorComponent
/Display,10,0.5%
/Explosives,5,0.1%,,ExplosivesComponent
/Girder,10,0.5%,,GirderComponent
/GravityGenerator,1,0.1%,GravityGen,GravityGeneratorComponent
/InteriorPlate,100,10%
/LargeTube,10,2%
/Medical,15,0.1%,,MedicalComponent
/MetalGrid,20,2%
/Motor,20,4%,,MotorComponent
/PowerCell,20,1%
/RadioCommunication,10,0.5%,RadioComm,RadioCommunicationComponent
/Reactor,25,2%,,ReactorComponent
/SmallTube,50,3%
/SolarCell,20,0.1%
/SteelPlate,150,40%
/Superconductor,10,1%
/Thrust,15,5%,,ThrustComponent

GasContainerObject/
/HydrogenBottle

Ingot/
/Cobalt,50,3.5%
/Gold,5,0.2%
/Iron,200,88%
/Magnesium,5,0.1%
/Nickel,30,1.5%
/Platinum,5,0.1%
/Silicon,50,2%
/Silver,20,1%
/Stone,50,2.5%
/Uranium,1,0.1%

Ore/
/Cobalt
/Gold
/Ice
/Iron
/Magnesium
/Nickel
/Platinum
/Scrap
/Silicon
/Silver
/Stone
/Uranium

OxygenContainerObject/
/OxygenBottle

PhysicalGunObject/
/AngleGrinderItem,,,,AngleGrinder
/AngleGrinder2Item,,,,AngleGrinder2
/AngleGrinder3Item,,,,AngleGrinder3
/AngleGrinder4Item,,,,AngleGrinder4
/AutomaticRifleItem,,,AutomaticRifle,AutomaticRifle
/HandDrillItem,,,,HandDrill
/HandDrill2Item,,,,HandDrill2
/HandDrill3Item,,,,HandDrill3
/HandDrill4Item,,,,HandDrill4
/PreciseAutomaticRifleItem,,,PreciseAutomaticRifle,PreciseAutomaticRifle
/RapidFireAutomaticRifleItem,,,RapidFireAutomaticRifle,RapidFireAutomaticRifle
/UltimateAutomaticRifleItem,,,UltimateAutomaticRifle,UltimateAutomaticRifle
/WelderItem,,,,Welder
/Welder2Item,,,,Welder2
/Welder3Item,,,,Welder3
/Welder4Item,,,,Welder4
";

// Item types which may have quantities which are not whole numbers.
static readonly HashSet<string> FRACTIONAL_TYPES = new HashSet<string> { "INGOT", "ORE" };

// Ore subtypes which refine into Ingots with a different subtype name, or
// which cannot be refined at all (if set to "").
static readonly Dictionary<string,string> ORE_PRODUCT = new Dictionary<string,string> { {"ICE",""}, {"ORGANIC",""}, {"SCRAP","IRON"} };

// Block types/subtypes which restrict item types/subtypes from their first
// inventory. Missing or "*" subtype indicates all subtypes of the given type.
const string DEFAULT_RESTRICTIONS =
MOB+"Assembler:AmmoMagazine,Component,GasContainerObject,Ore,OxygenContainerObject,PhysicalGunObject\n"+
MOB+"InteriorTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_25x184mm,"+NON_AMMO+
MOB+"LargeGatlingTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm,"+NON_AMMO+
MOB+"LargeMissileTurret:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm,"+NON_AMMO+
MOB+"OxygenGenerator:AmmoMagazine,Component,Ingot,Ore/Cobalt,Ore/Gold,Ore/Iron,Ore/Magnesium,Ore/Nickel,Ore/Organic,Ore/Platinum,Ore/Scrap,Ore/Silicon,Ore/Silver,Ore/Stone,Ore/Uranium,PhysicalGunObject\n"+
MOB+"OxygenTank:AmmoMagazine,Component,GasContainerObject,Ingot,Ore,PhysicalGunObject\n"+
MOB+"OxygenTank/LargeHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n"+
MOB+"OxygenTank/SmallHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n"+
MOB+"Reactor:AmmoMagazine,Component,GasContainerObject,Ingot/Cobalt,Ingot/Gold,Ingot/Iron,Ingot/Magnesium,Ingot/Nickel,Ingot/Platinum,Ingot/Scrap,Ingot/Silicon,Ingot/Silver,Ingot/Stone,Ore,OxygenContainerObject,PhysicalGunObject\n"+
MOB+"Refinery:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Ice,Ore/Organic,OxygenContainerObject,PhysicalGunObject\n"+
MOB+"Refinery/Blast Furnace:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Gold,Ore/Ice,Ore/Magnesium,Ore/Organic,Ore/Platinum,Ore/Silicon,Ore/Silver,Ore/Stone,Ore/Uranium,OxygenContainerObject,PhysicalGunObject\n"+
MOB+"SmallGatlingGun:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm,"+NON_AMMO+
MOB+"SmallMissileLauncher:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm,"+NON_AMMO+
MOB+"SmallMissileLauncherReload:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm,"+NON_AMMO
;

/* *************
SCRIPT INTERNALS

Do not edit anything below unless you're sure you know what you're doing!
*/

const int VERS_MAJ = 1, VERS_MIN = 6, VERS_REV = 4;
const string VERS_UPD = "2017-04-03";
const int VERSION = (VERS_MAJ*1000000)+(VERS_MIN*1000)+VERS_REV;

const int MAX_CYCLE_STEPS = 11, CYCLE_LENGTH = 1;
const bool REWRITE_TAGS = true, QUOTA_STABLE = true;
const char TAG_OPEN = '[', TAG_CLOSE = ']';
const string TAG_PREFIX = "TIM";
const bool SCAN_COLLECTORS = false, SCAN_DRILLS = false, SCAN_GRINDERS = false, SCAN_WELDERS = false;
const string MOB = "MyObjectBuilder_";
const string NON_AMMO = "Component,GasContainerObject,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n";
const StringComparison OIC = StringComparison.OrdinalIgnoreCase;
const StringSplitOptions REE = StringSplitOptions.RemoveEmptyEntries;
static readonly char[] SPACE = new char[] {' ','\t','\u00AD'}, COLON = new char[] {':'}, NEWLINE = new char[] {'\r','\n'}, SPACECOMMA = new char[] {' ','\t','\u00AD',','};
struct Quota { public int min; public float ratio; public Quota(int m, float r) { min=m; ratio=r; } }
struct Pair { public int a,b; public Pair(int aa, int bb) { a=aa; b=bb; } }
struct Item { public string itype, isub; public Item(string t, string s) { itype=t; isub=s; } }
struct Work { public Item item; public double qty; public Work(Item i, double q) { item=i; qty=q; } }

static int lastVersion = 0;
static string statsHeader = "";
static string[] statsLog = new string[12];
static long numCalls = 0;
static double sinceLast = 0.0;
static int numXfers, numRefs, numAsms;
static int cycleLength = CYCLE_LENGTH, cycleStep = 0;
static bool rewriteTags = REWRITE_TAGS;
static char tagOpen = TAG_OPEN, tagClose = TAG_CLOSE;
static string tagPrefix = TAG_PREFIX;
static System.Text.RegularExpressions.Regex tagRegex = null;
static string panelFiller = "";
static bool foundNewItem = false;

static Dictionary<Item,Quota> defaultQuota = new Dictionary<Item,Quota>();
static Dictionary<string,Dictionary<string,Dictionary<string,HashSet<string>>>> blockSubTypeRestrictions = new Dictionary<string,Dictionary<string,Dictionary<string,HashSet<string>>>>();
static HashSet<IMyCubeGrid> dockedgrids = new HashSet<IMyCubeGrid>();
static List<string> types = new List<string>();
static Dictionary<string,string> typeLabel = new Dictionary<string,string>();
static Dictionary<string,List<string>> typeSubs = new Dictionary<string,List<string>>();
static Dictionary<string,long> typeAmount = new Dictionary<string,long>();
static List<string> subs = new List<string>();
static Dictionary<string,string> subLabel = new Dictionary<string,string>();
static Dictionary<string,List<string>> subTypes = new Dictionary<string,List<string>>();
static Dictionary<string,Dictionary<string,ItemData>> typeSubData = new Dictionary<string,Dictionary<string,ItemData>>();
static Dictionary<MyDefinitionId,Item> blueprintItem = new Dictionary<MyDefinitionId,Item>();
static Dictionary<int,Dictionary<string,Dictionary<string,Dictionary<IMyInventory,long>>>> priTypeSubInvenRequest = new Dictionary<int,Dictionary<string,Dictionary<string,Dictionary<IMyInventory,long>>>>();
static Dictionary<IMyTextPanel,int> qpanelPriority = new Dictionary<IMyTextPanel,int>();
static Dictionary<IMyTextPanel,List<string>> qpanelTypes = new Dictionary<IMyTextPanel,List<string>>();
static Dictionary<IMyTextPanel,List<string>> ipanelTypes = new Dictionary<IMyTextPanel,List<string>>();
static List<IMyTextPanel> statusPanels = new List<IMyTextPanel>();
static List<IMyTextPanel> debugPanels = new List<IMyTextPanel>();
static HashSet<string> debugLogic = new HashSet<string>();
static List<string> debugText = new List<string>();
static Dictionary<IMyTerminalBlock,System.Text.RegularExpressions.Match> blockGtag = new Dictionary<IMyTerminalBlock,System.Text.RegularExpressions.Match>();
static Dictionary<IMyTerminalBlock,System.Text.RegularExpressions.Match> blockTag = new Dictionary<IMyTerminalBlock,System.Text.RegularExpressions.Match>();
static HashSet<IMyInventory> invenLocked = new HashSet<IMyInventory>();
static HashSet<IMyInventory> invenHidden = new HashSet<IMyInventory>();
static Dictionary<IMyRefinery,HashSet<string>> refineryOres = new Dictionary<IMyRefinery,HashSet<string>>();
static Dictionary<IMyAssembler,HashSet<Item>> assemblerItems = new Dictionary<IMyAssembler,HashSet<Item>>();
static Dictionary<IMyFunctionalBlock,Work> producerWork = new Dictionary<IMyFunctionalBlock,Work>();
static Dictionary<IMyFunctionalBlock,int> producerJam = new Dictionary<IMyFunctionalBlock,int>();
static Dictionary<IMyTextPanel,Pair> panelSpan = new Dictionary<IMyTextPanel,Pair>();
static Dictionary<IMyTerminalBlock,HashSet<IMyTerminalBlock>> blockErrors = new Dictionary<IMyTerminalBlock,HashSet<IMyTerminalBlock>>();


private class ItemData {
    public string itype, isub, label;
    public MyDefinitionId blueprint;
    public long amount, avail, locked, quota, minimum;
    public float ratio;
    public int qpriority, hold, jam;
    public Dictionary<IMyInventory,long> invenTotal;
    public Dictionary<IMyInventory,int> invenSlot;
    public HashSet<IMyFunctionalBlock> producers;
    public Dictionary<string,double> prdSpeed;

    public static void Init(string itype, string isub, long minimum=0L, float ratio=0.0f, string label="", string blueprint="") {
        string itypelabel=itype, isublabel=isub;
        itype = itype.ToUpper();
        isub = isub.ToUpper();

        // new type?
        if (!typeSubs.ContainsKey(itype)) {
            types.Add(itype);
            typeLabel[itype] = itypelabel;
            typeSubs[itype] = new List<string>();
            typeAmount[itype] = 0L;
            typeSubData[itype] = new Dictionary<string,ItemData>();
        }

        // new subtype?
        if (!subTypes.ContainsKey(isub)) {
            subs.Add(isub);
            subLabel[isub] = isublabel;
            subTypes[isub] = new List<string>();
        }

        // new type/subtype pair?
        if (!typeSubData[itype].ContainsKey(isub)) {
            foundNewItem = true;
            typeSubs[itype].Add(isub);
            subTypes[isub].Add(itype);
            typeSubData[itype][isub] = new ItemData(itype, isub, minimum, ratio, (label == "") ? isublabel : label, (blueprint == "") ? isublabel : blueprint);
            if (blueprint != null)
            blueprintItem[typeSubData[itype][isub].blueprint] = new Item(itype,isub);
        }
    } // Init()

    private ItemData(string itype, string isub, long minimum, float ratio, string label, string blueprint) {
        this.itype = itype;
        this.isub = isub;
        this.label = label;
        this.blueprint = (blueprint == null) ? default(MyDefinitionId) : MyDefinitionId.Parse("MyObjectBuilder_BlueprintDefinition/" + blueprint);
        this.amount = this.avail = this.locked = this.quota = 0L;
        this.minimum = (long)((double)minimum * 1000000.0 + 0.5);
        this.ratio = (ratio / 100.0f);
        this.qpriority = -1;
        this.hold = this.jam = 0;
        this.invenTotal = new Dictionary<IMyInventory,long>();
        this.invenSlot = new Dictionary<IMyInventory,int>();
        this.producers = new HashSet<IMyFunctionalBlock>();
        this.prdSpeed = new Dictionary<string,double>();
    } // ItemData()
} // ItemData


/*
* UTILITY FUNCTIONS
*/


void InitItems(string data) {
    string itype="";
    long minimum;
    float ratio;
    foreach (string line in data.Split(NEWLINE, REE)) {
        string[] words = (line.Trim()+",,,,").Split(SPACECOMMA, 6);
        words[0] = words[0].Trim();
        if (words[0].EndsWith("/")) {
            itype = words[0].Substring(0, words[0].Length - 1);
            } else if (itype != "" & words[0].StartsWith("/")) {
                long.TryParse(words[1], out minimum);
                float.TryParse(words[2].Substring(0, (words[2]+"%").IndexOf("%")), out ratio);
                ItemData.Init(itype, words[0].Substring(1), minimum, ratio, words[3].Trim(), (itype == "Ingot" | itype == "Ore") ? null : words[4].Trim());
            }
        }
} // InitItems()


void InitBlockRestrictions(string data) {
    foreach (string line in data.Split(NEWLINE, REE)) {
        string[] blockitems = (line+":").Split(':');
        string[] block = (blockitems[0]+"/*").Split('/');
        foreach (string item in blockitems[1].Split(',')) {
            string[] typesub = item.ToUpper().Split('/');
            AddBlockRestriction(block[0].Trim(SPACE), block[1].Trim(SPACE), typesub[0], ((typesub.Length > 1) ? typesub[1] : null), true);
        }
    }
} // InitBlockRestrictions()


void AddBlockRestriction(string btype, string bsub, string itype, string isub, bool init=false) {
    Dictionary<string,Dictionary<string,HashSet<string>>> bsubItypeRestr;
    Dictionary<string,HashSet<string>> itypeRestr;
    HashSet<string> restr;

    if (!blockSubTypeRestrictions.TryGetValue(btype.ToUpper(), out bsubItypeRestr))
    blockSubTypeRestrictions[btype.ToUpper()] = bsubItypeRestr = new Dictionary<string,Dictionary<string,HashSet<string>>> { { "*", new Dictionary<string,HashSet<string>>() } };
    if (!bsubItypeRestr.TryGetValue(bsub.ToUpper(), out itypeRestr)) {
        bsubItypeRestr[bsub.ToUpper()] = itypeRestr = new Dictionary<string,HashSet<string>>();
        if (bsub != "*" & !init) {
            foreach (KeyValuePair<string,HashSet<string>> pair in bsubItypeRestr["*"])
            itypeRestr[pair.Key] = ((pair.Value != null) ? (new HashSet<string>(pair.Value)) : null);
        }
    }
    if (isub == null | isub == "*") {
        itypeRestr[itype] = null;
    } else {
        (itypeRestr.TryGetValue(itype, out restr) ? restr : (itypeRestr[itype] = new HashSet<string>())).Add(isub);
    }
    if (!init) debugText.Add(btype+"/"+bsub+" does not accept "+typeLabel[itype]+"/"+subLabel[isub]);
} // AddBlockRestriction()


bool BlockAcceptsTypeSub(IMyCubeBlock block, string itype, string isub) {
    Dictionary<string,Dictionary<string,HashSet<string>>> bsubItypeRestr;
    Dictionary<string,HashSet<string>> itypeRestr;
    HashSet<string> restr;

    if (blockSubTypeRestrictions.TryGetValue(block.BlockDefinition.TypeIdString.ToUpper(), out bsubItypeRestr)) {
        bsubItypeRestr.TryGetValue(block.BlockDefinition.SubtypeName.ToUpper(), out itypeRestr);
        if ((itypeRestr ?? bsubItypeRestr["*"]).TryGetValue(itype, out restr))
        return !(restr == null || restr.Contains(isub));
    }
    return true;
} // BlockAcceptsTypeSub()


HashSet<string> GetBlockAcceptedSubs(IMyCubeBlock block, string itype, HashSet<string> mysubs=null) {
    Dictionary<string,Dictionary<string,HashSet<string>>> bsubItypeRestr;
    Dictionary<string,HashSet<string>> itypeRestr;
    HashSet<string> restr;

    mysubs = mysubs ?? new HashSet<string>(typeSubs[itype]);
    if (blockSubTypeRestrictions.TryGetValue(block.BlockDefinition.TypeIdString.ToUpper(), out bsubItypeRestr)) {
        bsubItypeRestr.TryGetValue(block.BlockDefinition.SubtypeName.ToUpper(), out itypeRestr);
        if ((itypeRestr ?? bsubItypeRestr["*"]).TryGetValue(itype, out restr))
        mysubs.ExceptWith(restr ?? mysubs);
    }
    return mysubs;
} // GetBlockAcceptedSubs()


string GetBlockImpliedType(IMyCubeBlock block, string isub) {
    string rtype = null;
    foreach (string itype in subTypes[isub]) {
        if (BlockAcceptsTypeSub(block, itype, isub)) {
            if (rtype != null)
            return null;
            rtype = itype;
        }
    }
    return rtype;
} // GetBlockImpliedType()


string GetShorthand(long amount) {
    long scale;
    if (amount <= 0L)
    return "0";
    if (amount < 10000L)
    return "< 0.01";
    if (amount >= 100000000000000L)
    return "" + (amount / 1000000000000L) + " M";
    scale = (long)Math.Pow(10.0, Math.Floor(Math.Log10(amount)) - 2.0);
    amount = (long)((double)amount / scale + 0.5) * scale;
    if (amount < 1000000000L)
    return (amount / 1e6).ToString("0.##");
    if (amount < 1000000000000L)
    return (amount / 1e9).ToString("0.##") + " K";
    return (amount / 1e12).ToString("0.##") + " M";
} // GetShorthand()


/*
* GRID FUNCTIONS
*/


void ScanGrids() {
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    IMyCubeGrid g1, g2;
    Dictionary<IMyCubeGrid,HashSet<IMyCubeGrid>> gridLinks = new Dictionary<IMyCubeGrid,HashSet<IMyCubeGrid>>();
    Dictionary<IMyCubeGrid,int> gridShip = new Dictionary<IMyCubeGrid,int>();
    List<HashSet<IMyCubeGrid>> shipGrids = new List<HashSet<IMyCubeGrid>>();
    List<string> shipName = new List<string>();
    HashSet<IMyCubeGrid> grids;
    List<IMyCubeGrid> gqueue = new List<IMyCubeGrid>(); // actual Queue lacks AddRange
    int q, s1, s2;
    IMyShipConnector conn2;
    HashSet<string> tags1 = new HashSet<string>();
    HashSet<string> tags2 = new HashSet<string>();
    System.Text.RegularExpressions.Match match;
    Dictionary<int,Dictionary<int,List<string>>> shipShipDocks = new Dictionary<int,Dictionary<int,List<string>>>();
    Dictionary<int,List<string>> shipDocks;
    List<string> docks;
    HashSet<int> ships = new HashSet<int>();
    Queue<int> squeue = new Queue<int>();

    // find mechanical links
    GridTerminalSystem.GetBlocksOfType<IMyMechanicalConnectionBlock>(blocks);
    foreach (IMyTerminalBlock block in blocks) {
        g1 = block.CubeGrid;
        g2 = (block as IMyMechanicalConnectionBlock).TopGrid;
        if (g2 == null)
        continue;
        (gridLinks.TryGetValue(g1, out grids) ? grids : (gridLinks[g1] = new HashSet<IMyCubeGrid>())).Add(g2);
        (gridLinks.TryGetValue(g2, out grids) ? grids : (gridLinks[g2] = new HashSet<IMyCubeGrid>())).Add(g1);
    }

    // each connected component of mechanical links is a "ship"
    foreach (IMyCubeGrid grid in gridLinks.Keys) {
        if (!gridShip.ContainsKey(grid)) {
            s1 = (grid.Max - grid.Min + Vector3I.One).Size;
            g1 = grid;
            gridShip[grid] = shipGrids.Count;
            grids = new HashSet<IMyCubeGrid> { grid };
            gqueue.Clear();
            gqueue.AddRange(gridLinks[grid]);
            for (q = 0;  q < gqueue.Count;  q++) {
                g2 = gqueue[q];
                if (!grids.Add(g2))
                continue;
                s2 = (g2.Max - g2.Min + Vector3I.One).Size;
                g1 = (s2 > s1) ? g2 : g1;
                s1 = (s2 > s1) ? s2 : s1;
                gridShip[g2] = shipGrids.Count;
                gqueue.AddRange(gridLinks[g2].Except(grids));
            }
            shipGrids.Add(grids);
            shipName.Add(g1.CustomName);
        }
    }

    // connectors require at least one shared dock tag, or no tags on either
    GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(blocks);
    foreach (IMyTerminalBlock block in blocks) {
        conn2 = (block as IMyShipConnector).OtherConnector;
        if (conn2 != null && (block.EntityId < conn2.EntityId & (block as IMyShipConnector).Status == MyShipConnectorStatus.Connected)) {
            tags1.Clear();
            tags2.Clear();
            if ((match = tagRegex.Match(block.CustomName)).Success) {
                foreach (string attr in match.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE)) {
                    if (attr.StartsWith("DOCK:", OIC))
                    tags1.UnionWith(attr.Substring(5).ToUpper().Split(COLON, REE));
                }
            }
            if ((match = tagRegex.Match(conn2.CustomName)).Success) {
                foreach (string attr in match.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE)) {
                    if (attr.StartsWith("DOCK:", OIC))
                    tags2.UnionWith(attr.Substring(5).ToUpper().Split(COLON, REE));
                }
            }
            if ((tags1.Count > 0 | tags2.Count > 0) & !tags1.Overlaps(tags2))
            continue;
            g1 = block.CubeGrid;
            g2 = conn2.CubeGrid;
            if (!gridShip.TryGetValue(g1, out s1)) {
                gridShip[g1] = s1 = shipGrids.Count;
                shipGrids.Add(new HashSet<IMyCubeGrid> { g1 });
                shipName.Add(g1.CustomName);
            }
            if (!gridShip.TryGetValue(g2, out s2)) {
                gridShip[g2] = s2 = shipGrids.Count;
                shipGrids.Add(new HashSet<IMyCubeGrid> { g2 });
                shipName.Add(g2.CustomName);
            }
            ((shipShipDocks.TryGetValue(s1, out shipDocks) ? shipDocks : (shipShipDocks[s1] = new Dictionary<int,List<string>>())).TryGetValue(s2, out docks) ? docks : (shipShipDocks[s1][s2] = new List<string>())).Add(block.CustomName);
            ((shipShipDocks.TryGetValue(s2, out shipDocks) ? shipDocks : (shipShipDocks[s2] = new Dictionary<int,List<string>>())).TryGetValue(s1, out docks) ? docks : (shipShipDocks[s2][s1] = new List<string>())).Add(conn2.CustomName);
        }
    }

    // starting "here", traverse all docked ships
    dockedgrids.Clear();
    dockedgrids.Add(Me.CubeGrid);
    if (!gridShip.TryGetValue(Me.CubeGrid, out s1))
    return;
    ships.Add(s1);
    dockedgrids.UnionWith(shipGrids[s1]);
    squeue.Enqueue(s1);
    while (squeue.Count > 0) {
        s1 = squeue.Dequeue();
        if (!shipShipDocks.TryGetValue(s1, out shipDocks))
        continue;
        foreach (int ship2 in shipDocks.Keys) {
            if (ships.Add(ship2)) {
                dockedgrids.UnionWith(shipGrids[ship2]);
                squeue.Enqueue(ship2);
                debugText.Add(shipName[ship2]+" docked to "+shipName[s1]+" at "+String.Join(", ",shipDocks[ship2]));
            }
        }
    }
} // ScanGrids()


/*
* INVENTORY FUNCTIONS
*/


void ScanGroups() {
    List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    System.Text.RegularExpressions.Match match;

    GridTerminalSystem.GetBlockGroups(groups);
    foreach (IMyBlockGroup group in groups) {
        if ((match = tagRegex.Match(group.Name)).Success) {
            group.GetBlocks(blocks);
            foreach (IMyTerminalBlock block in blocks)
            blockGtag[block] = match;
        }
    }
} // ScanGroups()


void ScanBlocks<T>() where T: class {
    List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
    System.Text.RegularExpressions.Match match;
    int i, s, n;
    IMyInventory inven;
    List<IMyInventoryItem> stacks;
    string itype, isub;
    ItemData data;
    long amount, total;

    GridTerminalSystem.GetBlocksOfType<T>(blocks);
    foreach (IMyTerminalBlock block in blocks) {
        if (!dockedgrids.Contains(block.CubeGrid))
        continue;
        match = tagRegex.Match(block.CustomName);
        if (match.Success) {
            blockGtag.Remove(block);
            blockTag[block] = match;
        } else if (blockGtag.TryGetValue(block, out match)) {
            blockTag[block] = match;
        }

        if ((block is IMySmallMissileLauncher & !(block is IMySmallMissileLauncherReload | block.BlockDefinition.SubtypeName == "LargeMissileLauncher")) | block is IMyLargeInteriorTurret) {
            // can't sort with no conveyor port
            invenLocked.Add(block.GetInventory(0));
        } else if ((block is IMyFunctionalBlock) && ((block as IMyFunctionalBlock).Enabled & block.IsFunctional)) {
            if ((block is IMyRefinery | block is IMyReactor | block is IMyGasGenerator) & !blockTag.ContainsKey(block)) {
                // don't touch input of enabled and untagged refineries, reactors or oxygen generators
                invenLocked.Add(block.GetInventory(0));
            } else if (block is IMyAssembler && !(block as IMyAssembler).IsQueueEmpty) {
                // don't touch input of enabled and active assemblers
                invenLocked.Add(block.GetInventory(((block as IMyAssembler).Mode == MyAssemblerMode.Disassembly) ? 1 : 0));
            }
        }

        i = block.InventoryCount;
        while (i-- > 0) {
            inven = block.GetInventory(i);
            stacks = inven.GetItems();
            s = stacks.Count;
            while (s-- > 0) {
                // identify the stacked item
                itype = ""+stacks[s].Content.TypeId;
                itype = itype.Substring(itype.LastIndexOf('_') + 1);
                isub = stacks[s].Content.SubtypeName;

                // new type or subtype?
                ItemData.Init(itype, isub, 0L, 0.0f, stacks[s].Content.SubtypeName, null);
                itype = itype.ToUpper();
                isub = isub.ToUpper();

                // update amounts
                amount = (long)((double)stacks[s].Amount * 1e6);
                typeAmount[itype] += amount;
                data = typeSubData[itype][isub];
                data.amount += amount;
                data.avail += amount;
                data.invenTotal.TryGetValue(inven, out total);
                data.invenTotal[inven] = total + amount;
                data.invenSlot.TryGetValue(inven, out n);
                data.invenSlot[inven] = Math.Max(n, s+1);
            }
        }
    }
} // ScanBlocks()


void AdjustAmounts() {
    string itype, isub;
    long amount;
    ItemData data;

    foreach (IMyInventory inven in invenHidden) {
        foreach (IMyInventoryItem stack in inven.GetItems()) {
            itype = ""+stack.Content.TypeId;
            itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
            isub = stack.Content.SubtypeName.ToUpper();

            amount = (long)((double)stack.Amount * 1e6);
            typeAmount[itype] -= amount;
            typeSubData[itype][isub].amount -= amount;
        }
    }

    foreach (IMyInventory inven in invenLocked) {
        foreach (IMyInventoryItem stack in inven.GetItems()) {
            itype = ""+stack.Content.TypeId;
            itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
            isub = stack.Content.SubtypeName.ToUpper();

            amount = (long)((double)stack.Amount * 1e6);
            data = typeSubData[itype][isub];
            data.avail -= amount;
            data.locked += amount;
        }
    }
} // AdjustAmounts()


/*
* TAG FUNCTIONS
*/


void ParseBlockTags() {
    StringBuilder name = new StringBuilder();
    IMyTextPanel blkPnl;
    IMyRefinery blkRfn;
    IMyAssembler blkAsm;
    System.Text.RegularExpressions.Match match;
    int i, priority, spanwide, spantall;
    string[] attrs, fields;
    string attr, itype, isub;
    long amount;
    float ratio;
    bool grouped, force, egg=false;

    // loop over all tagged blocks
    foreach (IMyTerminalBlock block in blockTag.Keys) {
        match = blockTag[block];
        attrs = match.Groups[1].Captures[0].Value.Split(SPACECOMMA, REE);
        name.Clear();
        if (!(grouped = blockGtag.ContainsKey(block))) {
            name.Append(block.CustomName, 0, match.Index);
            name.Append(tagOpen);
            if (tagPrefix != "")
            name.Append(tagPrefix + " ");
        }

        // loop over all tag attributes
        if ((blkPnl = (block as IMyTextPanel)) != null) {
            foreach (string a in attrs) {
                attr = a.ToUpper();
                if (lastVersion < 1005903 & (i = attr.IndexOf(":P")) > 0 & typeSubData.ContainsKey(attr.Substring(0, Math.Min(attr.Length, Math.Max(0,i))))) {
                    attr = "QUOTA:" + attr;
                } else if (lastVersion < 1005903 & typeSubData.ContainsKey(attr)) {
                    attr = "INVEN:" + attr;
                }
                fields = attr.Split(COLON);
                attr = fields[0];

                if (attr.Length >= 4 & "STATUS".StartsWith(attr)) {
                    if (blkPnl.Enabled) statusPanels.Add(blkPnl);
                    name.Append("STATUS ");
                    } else if (attr.Length >= 5 & "DEBUGGING".StartsWith(attr)) {
                        if (blkPnl.Enabled) debugPanels.Add(blkPnl);
                        name.Append("DEBUG ");
                        } else if (attr == "SPAN") {
                            if (fields.Length >= 3 && (int.TryParse(fields[1], out spanwide) & int.TryParse(fields[2], out spantall) & spanwide >= 1 & spantall >= 1)) {
                                panelSpan[blkPnl] = new Pair(spanwide, spantall);
                                name.Append("SPAN:" + spanwide + ":" + spantall + " ");
                            } else {
                                name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                                debugText.Add("Invalid panel span rule: " + attr);
                            }
                            } else if (attr == "THE") {
                                egg = true;
                                } else if (attr == "ENCHANTER" & egg) {
                                    egg = false;
                                    blkPnl.SetValueFloat("FontSize", 0.2f);
                                    blkPnl.WritePublicTitle("TIM the Enchanter", false);
                                    blkPnl.WritePublicText(panelFiller, false);
                                    blkPnl.ShowPublicTextOnScreen();
                                    name.Append("THE ENCHANTER ");
                                    } else if (attr.Length >= 3 & "QUOTAS".StartsWith(attr)) {
                                        if (blkPnl.Enabled & !qpanelPriority.ContainsKey(blkPnl)) qpanelPriority[blkPnl] = 0;
                                        if (blkPnl.Enabled & !qpanelTypes.ContainsKey(blkPnl)) qpanelTypes[blkPnl] = new List<string>();
                                        name.Append("QUOTA");
                                        i = 0;
                                        while (++i < fields.Length) {
                                            if (ParseItemTypeSub(null, true, fields[i], "", out itype, out isub) & itype != "ORE" & isub == "") {
                                                if (blkPnl.Enabled) qpanelTypes[blkPnl].Add(itype);
                                                name.Append(":" + typeLabel[itype]);
                                                } else if (fields[i].StartsWith("P") & int.TryParse(fields[i].Substring(Math.Min(1, fields[i].Length)), out priority)) {
                                                    if (blkPnl.Enabled) qpanelPriority[blkPnl] = Math.Max(0, priority);
                                                    if (priority > 0) name.Append(":P" + priority);
                                                } else {
                                                    name.Append(":" + fields[i].ToLower());
                                                    debugText.Add("Invalid quota panel rule: " + fields[i].ToLower());
                                                }
                                            }
                                            name.Append(" ");
                                            } else if (attr.Length >= 3 & "INVENTORY".StartsWith(attr)) {
                                                if (blkPnl.Enabled & !ipanelTypes.ContainsKey(blkPnl)) ipanelTypes[blkPnl] = new List<string>();
                                                name.Append("INVEN");
                                                i = 0;
                                                while (++i < fields.Length) {
                                                    if (ParseItemTypeSub(null, true, fields[i], "", out itype, out isub) & isub == "") {
                                                        if (blkPnl.Enabled) ipanelTypes[blkPnl].Add(itype);
                                                        name.Append(":" + typeLabel[itype]);
                                                    } else {
                                                        name.Append(":" + fields[i].ToLower());
                                                        debugText.Add("Invalid inventory panel rule: " + fields[i].ToLower());
                                                    }
                                                }
                                                name.Append(" ");
                                            } else {
                                                name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                                                debugText.Add("Invalid panel attribute: " + attr);
                                            }
                                        }
                                    } else {
                                        blkRfn = (block as IMyRefinery);
                                        blkAsm = (block as IMyAssembler);
                                        foreach (string a in attrs) {
                                            attr = a.ToUpper();
                                            if (lastVersion < 1005900 & ((blkRfn != null & attr == "ORE") | (blkAsm != null & typeSubData["COMPONENT"].ContainsKey(attr)))) {
                                                attr = "AUTO";
                                            }
                                            fields = attr.Split(COLON);
                                            attr = fields[0];

                if ((attr.Length >= 4 & "LOCKED".StartsWith(attr)) | attr == "EXEMPT") { // EXEMPT for AIS compat
                    i = block.InventoryCount;
                    while (i-- > 0)
                    invenLocked.Add(block.GetInventory(i));
                    name.Append(attr+" ");
                    } else if (attr == "HIDDEN") {
                        i = block.InventoryCount;
                        while (i-- > 0)
                        invenHidden.Add(block.GetInventory(i));
                        name.Append("HIDDEN ");
                        } else if ((block is IMyShipConnector) & attr == "DOCK") {
                    // handled in ScanGrids(), just rewrite
                            name.Append(String.Join(":", fields) + " ");
                            } else if ((blkRfn != null | blkAsm != null) & attr == "AUTO") {
                                name.Append("AUTO");
                                HashSet<string> ores, autoores = (blkRfn == null | fields.Length > 1) ? (new HashSet<string>()) : GetBlockAcceptedSubs(blkRfn, "ORE");
                                HashSet<Item> items, autoitems = new HashSet<Item>();
                                i = 0;
                                while (++i < fields.Length) {
                                    if (ParseItemTypeSub(null, true, fields[i], (blkRfn != null) ? "ORE" : "", out itype, out isub) & (blkRfn != null) == (itype == "ORE") & (blkRfn != null | itype != "INGOT")) {
                                        if (isub == "") {
                                            if (blkRfn != null) {
                                                autoores.UnionWith(typeSubs[itype]);
                                            } else {
                                                foreach (string s in typeSubs[itype])
                                                autoitems.Add(new Item(itype,s));
                                            }
                                            name.Append(":" + typeLabel[itype]);
                                        } else {
                                            if (blkRfn != null) {
                                                autoores.Add(isub);
                                            } else {
                                                autoitems.Add(new Item(itype,isub));
                                            }
                                            name.Append(":" + ((blkRfn == null & subTypes[isub].Count > 1) ? (typeLabel[itype] + "/") : "") + subLabel[isub]);
                                        }
                                    } else {
                                        name.Append(":" + fields[i].ToLower());
                                        debugText.Add("Unrecognized or ambiguous item: " + fields[i].ToLower());
                                    }
                                }
                                if (blkRfn != null) {
                                    if (blkRfn.Enabled)
                                    (refineryOres.TryGetValue(blkRfn, out ores) ? ores : (refineryOres[blkRfn] = new HashSet<string>())).UnionWith(autoores);
                                } else {
                                    if (lastVersion < 1005900) {
                                        blkAsm.ClearQueue();
                                        blkAsm.Repeating = false;
                                        blkAsm.Enabled = true;
                                    }
                                    if (blkAsm.Enabled)
                                    (assemblerItems.TryGetValue(blkAsm, out items) ? items : (assemblerItems[blkAsm] = new HashSet<Item>())).UnionWith(autoitems);
                                }
                                name.Append(" ");
                                } else if (!ParseItemValueText(block, fields, "", out itype, out isub, out priority, out amount, out ratio, out force)) {
                                    name.Append((attr = String.Join(":", fields).ToLower()) + " ");
                                    debugText.Add("Unrecognized or ambiguous item: " + attr);
                                    } else if (!block.HasInventory | (block is IMySmallMissileLauncher & !(block is IMySmallMissileLauncherReload | block.BlockDefinition.SubtypeName == "LargeMissileLauncher")) | block is IMyLargeInteriorTurret) {
                                        name.Append(String.Join(":", fields).ToLower() + " ");
                                        debugText.Add("Cannot sort items to "+block.CustomName+": no conveyor-connected inventory");
                                    } else {
                                        if (isub == "") {
                                            foreach (string s in (force ? (IEnumerable<string>)typeSubs[itype] : (IEnumerable<string>)GetBlockAcceptedSubs(block, itype)))
                                            AddInvenRequest(block, 0, itype, s, priority, amount);
                                        } else {
                                            AddInvenRequest(block, 0, itype, isub, priority, amount);
                                        }
                                        if (rewriteTags & !grouped) {
                                            if (force) {
                                                name.Append("FORCE:" + typeLabel[itype]);
                                                if (isub != "")
                                                name.Append("/" + subLabel[isub]);
                                                } else if (isub == "") {
                                                    name.Append(typeLabel[itype]);
                                                } else if (subTypes[isub].Count == 1 || GetBlockImpliedType(block, isub) == itype) {
                                                    name.Append(subLabel[isub]);
                                                } else {
                                                    name.Append(typeLabel[itype] + "/" + subLabel[isub]);
                                                }
                                                if (priority > 0 & priority < int.MaxValue)
                                                name.Append(":P" + priority);
                                                if (amount >= 0L)
                                                name.Append(":" + (amount / 1e6));
                                                name.Append(" ");
                                            }
                                        }
                                    }
                                }

                                if (rewriteTags & !grouped) {
                                    if (name[name.Length - 1] == ' ')
                                    name.Length--;
                                    name.Append(tagClose).Append(block.CustomName, match.Index + match.Length, block.CustomName.Length - match.Index - match.Length);
                                    block.CustomName = name.ToString();
                                }

                                if (block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.Owner & block.GetUserRelationToOwner(Me.OwnerId) != MyRelationsBetweenPlayerAndBlock.FactionShare)
                                debugText.Add("Cannot control \"" + block.CustomName + "\" due to differing ownership");
                            }
} // ParseBlockTags()


void ProcessQuotaPanels(bool quotaStable) {
    bool debug = debugLogic.Contains("quotas");
    int l, x, y, wide, size, spanx, spany, height, p, priority;
    long amount, round, total;
    float ratio;
    bool force;
    string itypeCur, itype, isub;
    string[] words, empty = new string[1] {" "};
    string[][] spanLines;
    IMyTextPanel panel2;
    IMySlimBlock slim;
    Matrix matrix = new Matrix();
    StringBuilder sb = new StringBuilder();
    List<string> qtypes = new List<string>(), errors = new List<string>(), scalesubs = new List<string>();
    Dictionary<string,SortedDictionary<string,string[]>> qtypeSubCols = new Dictionary<string,SortedDictionary<string,string[]>>();
    ItemData data;
    ScreenFormatter sf;

    // reset ore "quotas"
    foreach (ItemData d in typeSubData["ORE"].Values)
    d.minimum = (d.amount == 0L) ? 0L : Math.Max(d.minimum, d.amount);

    foreach (IMyTextPanel panel in qpanelPriority.Keys) {
        wide = panel.BlockDefinition.SubtypeName.EndsWith("Wide") ? 2 : 1;
        size = panel.BlockDefinition.SubtypeName.StartsWith("Small") ? 3 : 1;
        spanx = spany = 1;
        if (panelSpan.ContainsKey(panel)) {
            spanx = panelSpan[panel].a;
            spany = panelSpan[panel].b;
        }

        // (re?)assemble (spanned?) user quota text
        spanLines = new string[spanx][];
        panel.Orientation.GetMatrix(out matrix);
        sb.Clear();
        for (y = 0;  y < spany;  y++) {
            height = 0;
            for (x = 0;  x < spanx;  x++) {
                spanLines[x] = empty;
                slim = panel.CubeGrid.GetCubeBlock(new Vector3I(panel.Position  +  x * wide * size * matrix.Right  +  y * size * matrix.Down));
                panel2 = (slim != null) ? (slim.FatBlock as IMyTextPanel) : null;
                if (panel2 != null && (""+panel2.BlockDefinition == ""+panel.BlockDefinition & panel2.GetPublicTitle().ToUpper().Contains("QUOTAS"))) {
                    spanLines[x] = panel2.GetPublicText().Split('\n');
                    height = Math.Max(height, spanLines[x].Length);
                }
            }
            for (l = 0;  l < height;  l++) {
                for (x = 0;  x < spanx;  x++)
                sb.Append((l < spanLines[x].Length) ? spanLines[x][l] : " ");
                sb.Append("\n");
            }
        }

        // parse user quotas
        priority = qpanelPriority[panel];
        itypeCur = "";
        qtypes.Clear();
        qtypeSubCols.Clear();
        errors.Clear();
        foreach (string line in sb.ToString().Split('\n')) {
            words = line.ToUpper().Split(SPACE, 4, REE);
            if (words.Length < 1) {
                } else if (ParseItemValueText(null, words, itypeCur, out itype, out isub, out p, out amount, out ratio, out force) & itype == itypeCur & itype != "" & isub != "") {
                    data = typeSubData[itype][isub];
                    qtypeSubCols[itype][isub] = new string[] { data.label, ""+Math.Round(amount / 1e6, 2), ""+Math.Round(ratio * 100.0f, 2)+"%" };
                    if ((priority > 0 & (priority < data.qpriority | data.qpriority <= 0)) | (priority == 0 & data.qpriority < 0)) {
                        data.qpriority = priority;
                        data.minimum = amount;
                        data.ratio = ratio;
                    } else if (priority == data.qpriority) {
                        data.minimum = Math.Max(data.minimum, amount);
                        data.ratio = Math.Max(data.ratio, ratio);
                    }
                    } else if (ParseItemValueText(null, words, "", out itype, out isub, out p, out amount, out ratio, out force) & itype != itypeCur & itype != "" & isub == "") {
                        if (!qtypeSubCols.ContainsKey(itypeCur = itype)) {
                            qtypes.Add(itypeCur);
                            qtypeSubCols[itypeCur] = new SortedDictionary<string,string[]>();
                        }
                        } else if (itypeCur != "") {
                            qtypeSubCols[itypeCur][words[0]] = words;
                        } else {
                            errors.Add(line);
                        }
                    }

        // redraw quotas
                    sf = new ScreenFormatter(4, 2);
                    sf.SetAlign(1, 1);
                    sf.SetAlign(2, 1);
                    if (qtypes.Count == 0 & qpanelTypes[panel].Count == 0)
                    qpanelTypes[panel].AddRange(types);
                    foreach (string qtype in qpanelTypes[panel]) {
                        if (!qtypeSubCols.ContainsKey(qtype)) {
                            qtypes.Add(qtype);
                            qtypeSubCols[qtype] = new SortedDictionary<string,string[]>();
                        }
                    }
                    foreach (string qtype in qtypes) {
                        if (qtype == "ORE")
                        continue;
                        if (sf.GetNumRows() > 0)
                        sf.AddBlankRow();
                        sf.Add(0, typeLabel[qtype], true);
                        sf.Add(1, "  Min", true);
                        sf.Add(2, "  Pct", true);
                        sf.Add(3, "", true);
                        sf.AddBlankRow();
                        foreach (ItemData d in typeSubData[qtype].Values) {
                            if (!qtypeSubCols[qtype].ContainsKey(d.isub))
                            qtypeSubCols[qtype][d.isub] = new string[] { d.label, ""+Math.Round(d.minimum / 1e6, 2), ""+Math.Round(d.ratio * 100.0f, 2)+"%" };
                        }
                        foreach (string qsub in qtypeSubCols[qtype].Keys) {
                            words = qtypeSubCols[qtype][qsub];
                            sf.Add(0, typeSubData[qtype].ContainsKey(qsub) ? words[0] : words[0].ToLower(), true);
                            sf.Add(1, (words.Length > 1) ? words[1] : "", true);
                            sf.Add(2, (words.Length > 2) ? words[2] : "", true);
                            sf.Add(3, (words.Length > 3) ? words[3] : "", true);
                        }
                    }
                    WriteTableToPanel("TIM Quotas", sf, panel, true, ((errors.Count == 0) ? "" : (String.Join("\n", errors).Trim().ToLower() + "\n\n")), "");
                }

    // update effective quotas
                foreach (string qtype in types) {
                    round = 1L;
                    if (!FRACTIONAL_TYPES.Contains(qtype))
                    round = 1000000L;
                    total = typeAmount[qtype];
                    if (quotaStable & total > 0L) {
                        scalesubs.Clear();
                        foreach (ItemData d in typeSubData[qtype].Values) {
                            if (d.ratio > 0.0f & total >= (long)(d.minimum / d.ratio))
                            scalesubs.Add(d.isub);
                        }
                        if (scalesubs.Count > 0) {
                            scalesubs.Sort((string s1, string s2) => {
                                ItemData d1 = typeSubData[qtype][s1], d2 = typeSubData[qtype][s2];
                                long q1 = (long)(d1.amount / d1.ratio), q2 = (long)(d2.amount / d2.ratio);
                                return (q1 == q2) ? d1.ratio.CompareTo(d2.ratio) : q1.CompareTo(q2);
                                });
                            isub = scalesubs[(scalesubs.Count - 1) / 2];
                            data = typeSubData[qtype][isub];
                            total = (long)(data.amount / data.ratio + 0.5f);
                            if (debug) {
                                debugText.Add("median "+typeLabel[qtype]+" is "+subLabel[isub]+", "+(total/1e6)+" -> "+(data.amount/1e6/data.ratio));
                                foreach (string qsub in scalesubs) {
                                    data = typeSubData[qtype][qsub];
                                    debugText.Add("  "+subLabel[qsub]+" @ "+(data.amount/1e6)+" / "+data.ratio+" => "+(long)(data.amount/1e6/data.ratio+0.5f));
                                }
                            }
                        }
                    }
                    foreach (ItemData d in typeSubData[qtype].Values) {
                        amount = Math.Max(d.quota, Math.Max(d.minimum, (long)(d.ratio * total + 0.5f)));
                        d.quota = (amount / round) * round;
                    }
                }
} // ProcessQuotaPanels()


bool ParseItemTypeSub(IMyCubeBlock block, bool force, string typesub, string qtype, out string itype, out string isub) {
    int t, s, found;
    string[] parts;

    itype = "";
    isub = "";
    found = 0;
    parts = typesub.Trim().Split('/');
    if (parts.Length >= 2) {
        parts[0] = parts[0].Trim();
        parts[1] = parts[1].Trim();
        if (typeSubs.ContainsKey(parts[0]) && (parts[1] == "" | typeSubData[parts[0]].ContainsKey(parts[1]))) {
            // exact type/subtype
            if (force || BlockAcceptsTypeSub(block, parts[0], parts[1])) {
                found = 1;
                itype = parts[0];
                isub = parts[1];
            }
        } else {
            // type/subtype?
            t = types.BinarySearch(parts[0]);
            t = Math.Max(t, ~t);
            while ((found < 2 & t < types.Count) && types[t].StartsWith(parts[0])) {
                s = typeSubs[types[t]].BinarySearch(parts[1]);
                s = Math.Max(s, ~s);
                while ((found < 2 & s < typeSubs[types[t]].Count) && typeSubs[types[t]][s].StartsWith(parts[1])) {
                    if (force || BlockAcceptsTypeSub(block, types[t], typeSubs[types[t]][s])) {
                        found++;
                        itype = types[t];
                        isub = typeSubs[types[t]][s];
                    }
                    s++;
                }
                // special case for gravel
                if (found == 0 & types[t] == "INGOT" & "GRAVEL".StartsWith(parts[1]) & (force || BlockAcceptsTypeSub(block, "INGOT", "STONE"))) {
                    found++;
                    itype = "INGOT";
                    isub = "STONE";
                }
                t++;
            }
        }
    } else if (typeSubs.ContainsKey(parts[0])) {
        // exact type
        if (force || BlockAcceptsTypeSub(block, parts[0], "")) {
            found++;
            itype = parts[0];
            isub = "";
        }
    } else if (subTypes.ContainsKey(parts[0])) {
        // exact subtype
        if (qtype != "" && typeSubData[qtype].ContainsKey(parts[0])) {
            found++;
            itype = qtype;
            isub = parts[0];
        } else {
            t = subTypes[parts[0]].Count;
            while (found < 2 & t-- > 0) {
                if (force || BlockAcceptsTypeSub(block, subTypes[parts[0]][t], parts[0])) {
                    found++;
                    itype = subTypes[parts[0]][t];
                    isub = parts[0];
                }
            }
        }
        } else if (qtype != "") {
        // subtype of a known type
            s = typeSubs[qtype].BinarySearch(parts[0]);
            s = Math.Max(s, ~s);
            while ((found < 2 & s < typeSubs[qtype].Count) && typeSubs[qtype][s].StartsWith(parts[0])) {
                found++;
                itype = qtype;
                isub = typeSubs[qtype][s];
                s++;
            }
        // special case for gravel
            if (found == 0 & qtype == "INGOT" & "GRAVEL".StartsWith(parts[0])) {
                found++;
                itype = "INGOT";
                isub = "STONE";
            }
        } else {
        // type?
            t = types.BinarySearch(parts[0]);
            t = Math.Max(t, ~t);
            while ((found < 2 & t < types.Count) && types[t].StartsWith(parts[0])) {
                if (force || BlockAcceptsTypeSub(block, types[t], "")) {
                    found++;
                    itype = types[t];
                    isub = "";
                }
                t++;
            }
        // subtype?
            s = subs.BinarySearch(parts[0]);
            s = Math.Max(s, ~s);
            while ((found < 2 & s < subs.Count) && subs[s].StartsWith(parts[0])) {
                t = subTypes[subs[s]].Count;
                while (found < 2 & t-- > 0) {
                    if (force || BlockAcceptsTypeSub(block, subTypes[subs[s]][t], subs[s])) {
                        if (found != 1 || (itype != subTypes[subs[s]][t] | isub != "" | typeSubs[itype].Count != 1))
                        found++;
                        itype = subTypes[subs[s]][t];
                        isub = subs[s];
                    }
                }
                s++;
            }
        // special case for gravel
            if (found == 0 & "GRAVEL".StartsWith(parts[0]) & (force || BlockAcceptsTypeSub(block, "INGOT", "STONE"))) {
                found++;
                itype = "INGOT";
                isub = "STONE";
            }
        }

    // fill in implied subtype
        if (!force & block != null & found == 1 & isub == "") {
            HashSet<string> mysubs = GetBlockAcceptedSubs(block, itype);
            if (mysubs.Count == 1)
            isub = mysubs.First();
        }

        return (found == 1);
} // ParseItemTypeSub()


bool ParseItemValueText(IMyCubeBlock block, string[] fields, string qtype, out string itype, out string isub, out int priority, out long amount, out float ratio, out bool force) {
    int f, l;
    double val, mul;

    itype = "";
    isub = "";
    priority = 0;
    amount = -1L;
    ratio = -1.0f;
    force = (block == null);

    // identify the item
    f = 0;
    if (fields[0].Trim() == "FORCE") {
        if (fields.Length == 1)
        return false;
        force = true;
        f = 1;
    }
    if (!ParseItemTypeSub(block, force, fields[f], qtype, out itype, out isub))
    return false;

    // parse the remaining fields
    while (++f < fields.Length) {
        fields[f] = fields[f].Trim();
        l = fields[f].Length;

        if (l == 0) {
            } else if (fields[f] == "IGNORE") {
                amount = 0L;
                } else if (fields[f] == "OVERRIDE" | fields[f] == "SPLIT") {
            // these AIS tags are TIM's default behavior anyway
                    } else if (fields[f][l-1] == '%' & double.TryParse(fields[f].Substring(0,l-1), out val)) {
                        ratio = Math.Max(0.0f, (float)(val / 100.0));
                        } else if (fields[f][0] == 'P' & double.TryParse(fields[f].Substring(1), out val)) {
                            priority = Math.Max(1, (int)(val + 0.5));
                        } else {
            // check for numeric suffixes
                            mul = 1.0;
                            if (fields[f][l-1] == 'K') {
                                l--;
                                mul = 1e3;
                                } else if (fields[f][l-1] == 'M') {
                                    l--;
                                    mul = 1e6;
                                }

            // try parsing the field as an amount value
                                if (double.TryParse(fields[f].Substring(0,l), out val))
                                amount = Math.Max(0L, (long)(val * mul * 1e6 + 0.5));
                            }
                        }

                        return true;
} // ParseItemValueText()


void AddInvenRequest(IMyTerminalBlock block, int inv, string itype, string isub, int priority, long amount) {
    long a;
    Dictionary<string,Dictionary<string,Dictionary<IMyInventory,long>>> tsir;
    Dictionary<string,Dictionary<IMyInventory,long>> sir;
    Dictionary<IMyInventory,long> ir;

    // no priority -> last priority
    if (priority == 0)
    priority = int.MaxValue;

    // new priority/type/sub?
    tsir = (priTypeSubInvenRequest.TryGetValue(priority, out tsir) ? tsir : (priTypeSubInvenRequest[priority] = new Dictionary<string,Dictionary<string,Dictionary<IMyInventory,long>>>()));
    sir = (tsir.TryGetValue(itype, out sir) ? sir : (tsir[itype] = new Dictionary<string,Dictionary<IMyInventory,long>>()));
    ir = (sir.TryGetValue(isub, out ir) ? ir : (sir[isub] = new Dictionary<IMyInventory,long>()));

    // update request
    IMyInventory inven = block.GetInventory(inv);
    ir.TryGetValue(inven, out a);
    ir[inven] = amount;
    typeSubData[itype][isub].quota += Math.Min(0L, -a) + Math.Max(0L, amount);

    // disable conveyor for some block types
    // (IMyInventoryOwner is supposedly obsolete but there's no other way to do this for all of these block types at once)
    if (((block is IMyGasGenerator | block is IMyReactor | block is IMyRefinery | block is IMyUserControllableGun) & inven.Owner != null) && inven.Owner.UseConveyorSystem) {
        block.GetActionWithName("UseConveyor").Apply(block);
        debugText.Add("Disabling conveyor system for "+block.CustomName);
    }
} // AddInvenRequest()


/*
* TRANSFER FUNCTIONS
*/


void AllocateItems(bool limited) {
    List<int> priorities;

    // establish priority order, adding 0 for refinery management
    priorities = new List<int>(priTypeSubInvenRequest.Keys);
    priorities.Sort();
    foreach (int p in priorities) {
        foreach (string itype in priTypeSubInvenRequest[p].Keys) {
            foreach (string isub in priTypeSubInvenRequest[p][itype].Keys)
            AllocateItemBatch(limited, p, itype, isub);
        }
    }

    // if we just finished the unlimited requests, check for leftovers
    if (!limited) {
        foreach (string itype in types) {
            foreach (ItemData data in typeSubData[itype].Values) {
                if (data.avail > 0L)
                debugText.Add("No place to put " + GetShorthand(data.avail) + " " + typeLabel[itype] + "/" + subLabel[data.isub] + ", containers may be full");
            }
        }
    }
} // AllocateItems()


void AllocateItemBatch(bool limited, int priority, string itype, string isub) {
    bool debug = debugLogic.Contains("sorting");
    int locked, dropped;
    long totalrequest, totalavail, request, avail, amount, moved, round;
    List<IMyInventory> invens = null;
    Dictionary<IMyInventory,long> invenRequest;

    if (debug) debugText.Add("sorting "+typeLabel[itype]+"/"+subLabel[isub]+" lim="+limited+" p="+priority);

    round = 1L;
    if (!FRACTIONAL_TYPES.Contains(itype))
    round = 1000000L;
    invenRequest = new Dictionary<IMyInventory,long>();
    ItemData data = typeSubData[itype][isub];

    // sum up the requests
    totalrequest = 0L;
    foreach (IMyInventory reqInven in priTypeSubInvenRequest[priority][itype][isub].Keys) {
        request = priTypeSubInvenRequest[priority][itype][isub][reqInven];
        if (request != 0L & limited == (request >= 0L)) {
            if (request < 0L) {
                request = 1000000L;
                if (reqInven.MaxVolume != VRage.MyFixedPoint.MaxValue)
                request = (long)((double)reqInven.MaxVolume * 1e6);
            }
            invenRequest[reqInven] = request;
            totalrequest += request;
        }
    }
    if (debug) debugText.Add("total req="+(totalrequest/1e6));
    if (totalrequest <= 0L)
    return;
    totalavail = data.avail + data.locked;
    if (debug) debugText.Add("total avail="+(totalavail/1e6));

    // disqualify any locked invens which already have their share
    if (totalavail > 0L) {
        invens = new List<IMyInventory>(data.invenTotal.Keys);
        do {
            locked = 0;
            dropped = 0;
            foreach (IMyInventory amtInven in invens) {
                avail = data.invenTotal[amtInven];
                if (avail > 0L & invenLocked.Contains(amtInven)) {
                    locked++;
                    invenRequest.TryGetValue(amtInven, out request);
                    amount = (long)((double)request / totalrequest * totalavail);
                    if (limited)
                    amount = Math.Min(amount, request);
                    amount = (amount / round) * round;

                    if (avail >= amount) {
                        if (debug) debugText.Add("locked "+(amtInven.Owner==null?"???":(amtInven.Owner as IMyTerminalBlock).CustomName)+" gets "+(amount/1e6)+", has "+(avail/1e6));
                        dropped++;
                        totalrequest -= request;
                        invenRequest[amtInven] = 0L;
                        totalavail -= avail;
                        data.locked -= avail;
                        data.invenTotal[amtInven] = 0L;
                    }
                }
            }
        } while (locked > dropped & dropped > 0);
    }

    // allocate the remaining available items
    foreach (IMyInventory reqInven in invenRequest.Keys) {
        // calculate this inven's allotment
        request = invenRequest[reqInven];
        if (request <= 0L | totalrequest <= 0L | totalavail <= 0L) {
            if (limited & request > 0L) debugText.Add("Insufficient "+typeLabel[itype]+"/"+subLabel[isub]+" to satisfy "+(reqInven.Owner==null?"???":(reqInven.Owner as IMyTerminalBlock).CustomName));
            continue;
        }
        amount = (long)((double)request / totalrequest * totalavail);
        if (limited)
        amount = Math.Min(amount, request);
        amount = (amount / round) * round;
        if (debug) debugText.Add((reqInven.Owner==null?"???":(reqInven.Owner as IMyTerminalBlock).CustomName)+" gets "+(request/1e6)+" / "+(totalrequest/1e6)+" of "+(totalavail/1e6)+" = "+(amount/1e6));
        totalrequest -= request;

        // check how much it already has
        if (data.invenTotal.TryGetValue(reqInven, out avail)) {
            avail = Math.Min(avail, amount);
            amount -= avail;
            totalavail -= avail;
            if (invenLocked.Contains(reqInven)) {
                data.locked -= avail;
            } else {
                data.avail -= avail;
            }
            data.invenTotal[reqInven] -= avail;
        }

        // get the rest from other unlocked invens
        moved = 0L;
        foreach (IMyInventory amtInven in invens) {
            avail = Math.Min(data.invenTotal[amtInven], amount);
            moved = 0L;
            if (avail > 0L & invenLocked.Contains(amtInven) == false) {
                moved = TransferItem(itype, isub, avail, amtInven, reqInven);
                amount -= moved;
                totalavail -= moved;
                data.avail -= moved;
                data.invenTotal[amtInven] -= moved;
            }
            // if we moved some but not all, we're probably full
            if (amount <= 0L | (moved != 0L & moved != avail))
            break;
        }

        if (limited & amount > 0L) {
            debugText.Add("Insufficient "+typeLabel[itype]+"/"+subLabel[isub]+" to satisfy "+(reqInven.Owner==null?"???":(reqInven.Owner as IMyTerminalBlock).CustomName));
            continue;
        }
    }

    if (debug) debugText.Add(""+(totalavail/1e6)+" left over");
} // AllocateItemBatch()


long TransferItem(string itype, string isub, long amount, IMyInventory fromInven, IMyInventory toInven) {
    bool debug = debugLogic.Contains("sorting");
    List<IMyInventoryItem> stacks;
    int s;
    VRage.MyFixedPoint remaining, moved;
    uint id;
//    double volume;
    string stype, ssub;

    remaining = (VRage.MyFixedPoint)(amount / 1e6);
    stacks = fromInven.GetItems();
    s = Math.Min(typeSubData[itype][isub].invenSlot[fromInven], stacks.Count);
    while (remaining > 0 & s-- > 0) {
        stype = ""+stacks[s].Content.TypeId;
        stype = stype.Substring(stype.LastIndexOf('_') + 1).ToUpper();
        ssub = stacks[s].Content.SubtypeName.ToUpper();
        if (stype == itype & ssub == isub) {
            moved = stacks[s].Amount;
            id = stacks[s].ItemId;
//            volume = (double)fromInven.CurrentVolume;
            if (fromInven == toInven) {
                remaining -= moved;
                if (remaining < 0)
                remaining = 0;
            } else if (fromInven.TransferItemTo(toInven, s, null, true, remaining)) {
                stacks = fromInven.GetItems();
                if (s < stacks.Count && stacks[s].ItemId == id)
                moved -= stacks[s].Amount;
                if (moved <= 0) {
                    if ((double)toInven.CurrentVolume < (double)toInven.MaxVolume / 2 & toInven.Owner != null) {
                        var/*SerializableDefinitionId*/ bdef = (toInven.Owner as IMyCubeBlock).BlockDefinition;
                        AddBlockRestriction(bdef.TypeIdString, bdef.SubtypeName, itype, isub);
                    }
                    s = 0;
                } else {
                    numXfers++;
                    if (debug) debugText.Add(
                        "Transferred "+GetShorthand((long)((double)moved*1e6))+" "+typeLabel[itype]+"/"+subLabel[isub]+
                        " from "+(fromInven.Owner==null?"???":(fromInven.Owner as IMyTerminalBlock).CustomName)+" to "+(toInven.Owner==null?"???":(toInven.Owner as IMyTerminalBlock).CustomName)
                        );
//                    volume -= (double)fromInven.CurrentVolume;
//                    typeSubData[itype][isub].volume = (1000.0 * volume / (double)moved);
                }
                remaining -= moved;
            } else if (!fromInven.IsConnectedTo(toInven) & fromInven.Owner != null & toInven.Owner != null) {
                if (!blockErrors.ContainsKey(fromInven.Owner as IMyTerminalBlock))
                blockErrors[fromInven.Owner as IMyTerminalBlock] = new HashSet<IMyTerminalBlock>();
                blockErrors[fromInven.Owner as IMyTerminalBlock].Add(toInven.Owner as IMyTerminalBlock);
                s = 0;
            }
        }
    }

    return amount - (long)((double)remaining * 1e6 + 0.5);
} // TransferItem()


/*
* MANAGEMENT FUNCTIONS
*/


void ManageRefineries() {
    if (!typeSubs.ContainsKey("ORE") | !typeSubs.ContainsKey("INGOT"))
    return;

    bool debug = debugLogic.Contains("refineries");
    string itype, itype2, isub, isub2, isubIngot;
    ItemData data;
    int level, priority;
    List<string> ores = new List<string>();
    Dictionary<string,int> oreLevel = new Dictionary<string,int>();
    List<IMyInventoryItem> stacks;
    double speed, oldspeed;
    Work work;
    bool ready;
    List<IMyRefinery> refineries = new List<IMyRefinery>();

    if (debug) debugText.Add("Refinery management:");

    // scan inventory levels
    foreach (string isubOre in typeSubs["ORE"]) {
        if (!ORE_PRODUCT.TryGetValue(isubOre, out isubIngot))
        isubIngot = isubOre;
        if (isubIngot != "" & typeSubData["ORE"][isubOre].avail > 0L & typeSubData["INGOT"].TryGetValue(isubIngot, out data)) {
            if (data.quota > 0L) {
                level = (int)(100L * data.amount / data.quota);
                ores.Add(isubOre);
                oreLevel[isubOre] = level;
                if (debug) debugText.Add("  "+subLabel[isubIngot]+" @ "+(data.amount/1e6)+"/"+(data.quota/1e6)+","+((isubOre==isubIngot)?"":(" Ore/"+subLabel[isubOre]))+" L="+level+"%");
            }
        }
    }

    // identify refineries that are ready for a new assignment
    foreach (IMyRefinery rfn in refineryOres.Keys) {
        itype = itype2 = isub = isub2 = "";
        stacks = rfn.GetInventory(0).GetItems();
        if (stacks.Count > 0) {
            itype = ""+stacks[0].Content.TypeId;
            itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
            isub = stacks[0].Content.SubtypeName.ToUpper();
            if (itype == "ORE" & oreLevel.ContainsKey(isub))
            oreLevel[isub] += Math.Max(1, oreLevel[isub] / refineryOres.Count);
            if (stacks.Count > 1) {
                itype2 = ""+stacks[1].Content.TypeId;
                itype2 = itype2.Substring(itype2.LastIndexOf('_') + 1).ToUpper();
                isub2 = stacks[1].Content.SubtypeName.ToUpper();
                if (itype2 == "ORE" & oreLevel.ContainsKey(isub2))
                oreLevel[isub2] += Math.Max(1, oreLevel[isub2] / refineryOres.Count);
                AddInvenRequest(rfn, 0, itype2, isub2, -2, (long)((double)stacks[1].Amount*1e6+0.5));
            }
        }
        if (producerWork.TryGetValue(rfn, out work)) {
            data = typeSubData[work.item.itype][work.item.isub];
            oldspeed = (data.prdSpeed.TryGetValue(""+rfn.BlockDefinition, out oldspeed) ? oldspeed : 1.0);
            speed = ((work.item.isub == isub) ? Math.Max(work.qty - (double)stacks[0].Amount, 0.0) : Math.Max(work.qty, oldspeed));
            speed = Math.Min(Math.Max((speed + oldspeed) / 2.0, 0.2), 10000.0);
            data.prdSpeed[""+rfn.BlockDefinition] = speed;
            if (debug & (int)(oldspeed+0.5) != (int)(speed+0.5)) debugText.Add("  Update "+rfn.BlockDefinition.SubtypeName+":"+subLabel[work.item.isub]+" refine speed: "+((int)(oldspeed+0.5))+" -> "+((int)(speed+0.5))+"kg/cycle");
        }
        if (refineryOres[rfn].Count > 0) refineryOres[rfn].IntersectWith(oreLevel.Keys); else refineryOres[rfn].UnionWith(oreLevel.Keys);
        ready = (refineryOres[rfn].Count > 0);
        if (stacks.Count > 0) {
            speed = (itype == "ORE" ? (typeSubData["ORE"][isub].prdSpeed.TryGetValue(""+rfn.BlockDefinition, out speed) ? speed : 1.0) : 1e6);
            AddInvenRequest(rfn, 0, itype, isub, -1, (long)Math.Min((double)stacks[0].Amount*1e6+0.5, 10*speed*1e6+0.5));
            ready = (ready & itype == "ORE" & (double)stacks[0].Amount < 2.5*speed & stacks.Count == 1);
        }
        if (ready)
        refineries.Add(rfn);
        if (debug) debugText.Add(
            "  "+rfn.CustomName+((stacks.Count<1)?" idle":(
                " refining "+(int)stacks[0].Amount+"kg "+((isub=="")?"unknown":(
                    subLabel[isub]+(!oreLevel.ContainsKey(isub)?"":(" (L="+oreLevel[isub]+"%)"))
                    ))+((stacks.Count<2)?"":(
                    ", then "+(int)stacks[1].Amount+"kg "+((isub2=="")?"unknown":(
                        subLabel[isub2]+(!oreLevel.ContainsKey(isub2)?"":(" (L="+oreLevel[isub2]+"%)"))
                        ))
                    ))
                    ))+"; "+((oreLevel.Count==0)?"nothing to do":(ready?"ready":((refineryOres[rfn].Count==0)?"restricted":"busy")))
            );
    }

    // skip refinery:ore assignment if there are no ores or ready refineries
    if (ores.Count > 0 & refineries.Count > 0) {
        ores.Sort((string o1, string o2) => {
            string i1, i2;
            if (!ORE_PRODUCT.TryGetValue(o1,out i1)) i1=o1;
            if (!ORE_PRODUCT.TryGetValue(o2,out i2)) i2=o2;
            return -1*typeSubData["INGOT"][i1].quota.CompareTo(typeSubData["INGOT"][i2].quota);
            });
        refineries.Sort((IMyRefinery r1, IMyRefinery r2) => refineryOres[r1].Count.CompareTo(refineryOres[r2].Count));
        foreach (IMyRefinery rfn in refineries) {
            isub = "";
            level = int.MaxValue;
            foreach (string isubOre in ores) {
                if ((isub == "" | oreLevel[isubOre] < level) & refineryOres[rfn].Contains(isubOre)) {
                    isub = isubOre;
                    level = oreLevel[isub];
                }
            }
            if (isub != "") {
                numRefs++;
                rfn.UseConveyorSystem = false;
                priority = rfn.GetInventory(0).IsItemAt(0) ? -4 : -3;
                speed = (typeSubData["ORE"][isub].prdSpeed.TryGetValue(""+rfn.BlockDefinition, out speed) ? speed : 1.0);
                AddInvenRequest(rfn, 0, "ORE", isub, priority, (long)(5*speed*1e6+0.5));
                oreLevel[isub] += Math.Min(Math.Max((int)(oreLevel[isub]*0.41), 1), (100 / refineryOres.Count));
                if (debug) debugText.Add("  "+rfn.CustomName+" assigned "+((int)(5*speed+0.5))+"kg "+subLabel[isub]+" (L="+oreLevel[isub]+"%)");
                } else if (debug) debugText.Add("  "+rfn.CustomName+" unassigned, nothing to do");
            }
        }

        for (priority = -1;  priority >= -4;  priority--) {
            if (priTypeSubInvenRequest.ContainsKey(priority)) {
                foreach (string isubOre in priTypeSubInvenRequest[priority]["ORE"].Keys)
                AllocateItemBatch(true, priority, "ORE", isubOre);
            }
        }
} // ManageRefineries()


void ManageAssemblers() {
    if (!typeSubs.ContainsKey("INGOT"))
    return;

    bool debug = debugLogic.Contains("assemblers");
    long ttlCmp;
    int level, amount;
    ItemData data, data2;
    Item item, item2;
    List<Item> items;
    Dictionary<Item,int> itemLevel = new Dictionary<Item,int>(), itemPar = new Dictionary<Item,int>();
    List<MyProductionItem> queue = new List<MyProductionItem>();
    double speed, oldspeed;
    Work work;
    bool ready, jam;
    List<IMyAssembler> assemblers = new List<IMyAssembler>();

    if (debug) debugText.Add("Assembler management:");

    // scan inventory levels
    typeAmount.TryGetValue("COMPONENT", out ttlCmp);
    amount = 90 + (int)(10 * typeSubData["INGOT"].Values.Min(d => (d.isub != "URANIUM" & (d.minimum > 0L | d.ratio > 0.0f)) ? (d.amount / Math.Max((double)d.minimum, 17.5 * d.ratio * ttlCmp)) : 2.0));
    if (debug) debugText.Add("  Component par L="+amount+"%");
    foreach (string itype in types) {
        if (itype != "ORE" & itype != "INGOT") {
            foreach (string isub in typeSubs[itype]) {
                data = typeSubData[itype][isub];
                data.hold = Math.Max(0, data.hold - 1);
                item = new Item(itype, isub);
                itemPar[item] = ((itype == "COMPONENT" & data.ratio > 0.0f) ? amount : 100);
                level = (int)(100L * data.amount / Math.Max(1L, data.quota));
                if (data.quota > 0L & level < itemPar[item] & data.blueprint != default(MyDefinitionId)) {
                    if (data.hold == 0) itemLevel[item] = level;
                    if (debug) debugText.Add("  "+typeLabel[itype]+"/"+subLabel[isub]+((data.hold > 0) ? "" : (" @ "+(data.amount/1e6)+"/"+(data.quota/1e6)+", L="+level+"%"))+((data.hold > 0 | data.jam > 0) ? ("; HOLD "+data.hold+"/"+(10*data.jam)) : ""));
                }
            }
        }
    }

    // identify assemblers that are ready for a new assignment
    foreach (IMyAssembler asm in assemblerItems.Keys) {
        ready = jam = false;
        data = data2 = null;
        item = item2 = new Item("","");
        if (!asm.IsQueueEmpty) {
            asm.GetQueue(queue);
            data = (blueprintItem.TryGetValue(queue[0].BlueprintId, out item) ? typeSubData[item.itype][item.isub] : null);
            if (data != null & itemLevel.ContainsKey(item))
            itemLevel[item] += Math.Max(1, (int)(1e8 * (double)queue[0].Amount / data.quota + 0.5));
            if (queue.Count > 1 && (blueprintItem.TryGetValue(queue[1].BlueprintId, out item2) & itemLevel.ContainsKey(item2)))
            itemLevel[item2] += Math.Max(1, (int)(1e8 * (double)queue[1].Amount / typeSubData[item2.itype][item2.isub].quota + 0.5));
        }
        if (producerWork.TryGetValue(asm, out work)) {
            data2 = typeSubData[work.item.itype][work.item.isub];
            oldspeed = (data2.prdSpeed.TryGetValue(""+asm.BlockDefinition, out oldspeed) ? oldspeed : 1.0);
            if (work.item.itype != item.itype | work.item.isub != item.isub) {
                speed = Math.Max(oldspeed, (asm.IsQueueEmpty ? 2 : 1) * work.qty);
                producerJam.Remove(asm);
            } else if (asm.IsProducing) {
                speed = work.qty - (double)queue[0].Amount + asm.CurrentProgress;
                producerJam.Remove(asm);
            } else {
                speed = Math.Max(oldspeed, work.qty - (double)queue[0].Amount + asm.CurrentProgress);
                if ((producerJam[asm] = (producerJam.TryGetValue(asm, out level) ? level : 0) + 1) >= 3) {
                    debugText.Add("  "+asm.CustomName+" is jammed by "+subLabel[item.isub]);
                    producerJam.Remove(asm);
                    asm.ClearQueue();
                    data2.hold = 10 * ((data2.jam < 1 | data2.hold < 1) ? (data2.jam = Math.Min(10, data2.jam + 1)) : data2.jam);
                    jam = true;
                }
            }
            speed = Math.Min(Math.Max((speed + oldspeed) / 2.0, Math.Max(0.2, 0.5*oldspeed)), Math.Min(1000.0, 2.0*oldspeed));
            data2.prdSpeed[""+asm.BlockDefinition] = speed;
            if (debug & (int)(oldspeed+0.5) != (int)(speed+0.5)) debugText.Add("  Update "+asm.BlockDefinition.SubtypeName+":"+typeLabel[work.item.itype]+"/"+subLabel[work.item.isub]+" assemble speed: "+((int)(oldspeed*100)/100.0)+" -> "+((int)(speed*100)/100.0)+"/cycle");
        }
        if (assemblerItems[asm].Count == 0) assemblerItems[asm].UnionWith(itemLevel.Keys); else assemblerItems[asm].IntersectWith(itemLevel.Keys);
        speed = ((data != null && data.prdSpeed.TryGetValue(""+asm.BlockDefinition, out speed)) ? speed : 1.0);
        if (!jam & (asm.IsQueueEmpty || (((double)queue[0].Amount - asm.CurrentProgress) < 2.5*speed & queue.Count == 1 & asm.Mode == MyAssemblerMode.Assembly))) {
            if (data2 != null) data2.jam = Math.Max(0, data2.jam - ((data2.hold < 1) ? 1 : 0));
            if (ready = (assemblerItems[asm].Count > 0)) assemblers.Add(asm);
        }
        if (debug) debugText.Add(
            "  "+asm.CustomName+(asm.IsQueueEmpty?" idle":(
                ((asm.Mode==MyAssemblerMode.Assembly)?" making ":" breaking ")+queue[0].Amount+"x "+((item.itype=="")?"unknown":(
                    subLabel[item.isub]+(!itemLevel.ContainsKey(item)?"":(" (L="+itemLevel[item]+"%)"))
                    ))+((queue.Count<=1)?"":(
                    ", then "+queue[1].Amount+"x "+((item2.itype=="")?"unknown":(
                        subLabel[item2.isub]+(!itemLevel.ContainsKey(item2)?"":(" (L="+itemLevel[item2]+"%)"))
                        ))
                    ))
                    ))+"; "+((itemLevel.Count==0)?"nothing to do":(ready?"ready":((assemblerItems[asm].Count==0)?"restricted":"busy")))
            );
    }

    // skip assembler:item assignments if there are no needed items or ready assemblers
    if (itemLevel.Count > 0 & assemblers.Count > 0) {
        items = new List<Item>(itemLevel.Keys);
        items.Sort((i1,i2) => -1*typeSubData[i1.itype][i1.isub].quota.CompareTo(typeSubData[i2.itype][i2.isub].quota));
        assemblers.Sort((IMyAssembler a1, IMyAssembler a2) => assemblerItems[a1].Count.CompareTo(assemblerItems[a2].Count));
        foreach (IMyAssembler asm in assemblers) {
            item = new Item("","");
            level = int.MaxValue;
            foreach (Item i in items) {
                if (itemLevel[i] < Math.Min(level, itemPar[i]) & assemblerItems[asm].Contains(i) & typeSubData[i.itype][i.isub].hold < 1) {
                    item = i;
                    level = itemLevel[i];
                }
            }
            if (item.itype != "") {
                numAsms++;
                asm.UseConveyorSystem = true;
                asm.CooperativeMode = false;
                asm.Repeating = false;
                asm.Mode = MyAssemblerMode.Assembly;
                data = typeSubData[item.itype][item.isub];
                speed = (data.prdSpeed.TryGetValue(""+asm.BlockDefinition, out speed) ? speed : 1.0);
                amount = Math.Max((int)(5*speed), 1);
                asm.AddQueueItem(data.blueprint, (double)amount);
                itemLevel[item] += (int)Math.Ceiling(1e8 * (double)amount / data.quota);
                if (debug) debugText.Add("  "+asm.CustomName+" assigned "+amount+"x "+subLabel[item.isub]+" (L="+itemLevel[item]+"%)");
                } else if (debug) debugText.Add("  "+asm.CustomName+" unassigned, nothing to do");
            }
        }
} // ManageAssemblers()


/*
* PANEL DISPLAYS
*/


void ScanProduction() {
    List<IMyTerminalBlock> blocks1 = new List<IMyTerminalBlock>(), blocks2 = new List<IMyTerminalBlock>();
    List<IMyInventoryItem> stacks;
    string itype, isub, isubIng;
    List<MyProductionItem> queue = new List<MyProductionItem>();
    Item item;

    producerWork.Clear();

    GridTerminalSystem.GetBlocksOfType<IMyGasGenerator>(blocks1, blk => dockedgrids.Contains(blk.CubeGrid));
    GridTerminalSystem.GetBlocksOfType<IMyRefinery>(blocks2, blk => dockedgrids.Contains(blk.CubeGrid));
    foreach (IMyFunctionalBlock blk in blocks1.Concat(blocks2)) {
        stacks = blk.GetInventory(0).GetItems();
        if (stacks.Count > 0 & blk.Enabled) {
            itype = ""+stacks[0].Content.TypeId;
            itype = itype.Substring(itype.LastIndexOf('_') + 1).ToUpper();
            isub = stacks[0].Content.SubtypeName.ToUpper();
            if (typeSubs.ContainsKey(itype) & subTypes.ContainsKey(isub))
            typeSubData[itype][isub].producers.Add(blk);
            if (itype == "ORE" & (ORE_PRODUCT.TryGetValue(isub, out isubIng) ? isubIng : (isubIng = isub)) != "" & typeSubData["INGOT"].ContainsKey(isubIng))
            typeSubData["INGOT"][isubIng].producers.Add(blk);
            producerWork[blk] = new Work(new Item(itype, isub), (double)stacks[0].Amount);
        }
    }

    GridTerminalSystem.GetBlocksOfType<IMyAssembler>(blocks1, blk => dockedgrids.Contains(blk.CubeGrid));
    foreach (IMyAssembler blk in blocks1) {
        if (blk.Enabled & !blk.IsQueueEmpty & blk.Mode == MyAssemblerMode.Assembly) {
            blk.GetQueue(queue);
            if (blueprintItem.TryGetValue(queue[0].BlueprintId, out item)) {
                if (typeSubs.ContainsKey(item.itype) & subTypes.ContainsKey(item.isub))
                typeSubData[item.itype][item.isub].producers.Add(blk);
                producerWork[blk] = new Work(item, (double)queue[0].Amount - blk.CurrentProgress);
            }
        }
    }
} // ScanProduction()


void UpdateInventoryPanels() {
    string text, header2, header5;
    Dictionary<string,List<IMyTextPanel>> itypesPanels = new Dictionary<string,List<IMyTextPanel>>();
    ScreenFormatter sf;
    long maxamt, maxqta;

    foreach (IMyTextPanel panel in ipanelTypes.Keys) {
        text = String.Join("/", ipanelTypes[panel]);
        if (itypesPanels.ContainsKey(text)) itypesPanels[text].Add(panel); else itypesPanels[text] = new List<IMyTextPanel>() { panel };
    }
    foreach (List<IMyTextPanel> panels in itypesPanels.Values) {
        sf = new ScreenFormatter(6);
        sf.SetBar(0);
        sf.SetFill(0, 1);
        sf.SetAlign(2, 1);
        sf.SetAlign(3, 1);
        sf.SetAlign(4, 1);
        sf.SetAlign(5, 1);
        maxamt = maxqta = 0L;
        foreach (string itype in ((ipanelTypes[panels[0]].Count > 0) ? ipanelTypes[panels[0]] : types)) {
            header2 = " Asm ";
            header5 = "Quota";
            if (itype == "INGOT") {
                header2 = " Ref ";
                } else if (itype == "ORE") {
                    header2 = " Ref ";
                    header5 = "Max";
                }
                if (sf.GetNumRows() > 0)
                sf.AddBlankRow();
                sf.Add(0, "");
                sf.Add(1, typeLabel[itype], true);
                sf.Add(2, header2, true);
                sf.Add(3, "Qty", true);
                sf.Add(4, " / ", true);
                sf.Add(5, header5, true);
                sf.AddBlankRow();
                foreach (ItemData data in typeSubData[itype].Values) {
                    sf.Add(0, (data.amount == 0L) ? "0.0" : (""+((double)data.amount / data.quota)));
                    sf.Add(1, data.label, true);
                    text = ((data.producers.Count > 0) ? (data.producers.Count + " " + (data.producers.All(blk => (!(blk is IMyProductionBlock) || (blk as IMyProductionBlock).IsProducing)) ? " " : "!")) : ((data.hold > 0) ? "-  " : ""));
                    sf.Add(2, text, true);
                    sf.Add(3, (data.amount > 0L | data.quota > 0L) ? GetShorthand(data.amount) : "");
                    sf.Add(4, (data.quota > 0L) ? " / " : "", true);
                    sf.Add(5, (data.quota > 0L) ? GetShorthand(data.quota) : "");
                    maxamt = Math.Max(maxamt, data.amount);
                    maxqta = Math.Max(maxqta, data.quota);
                }
            }
            sf.SetWidth(3, ScreenFormatter.GetWidth("8.88" + ((maxamt >= 1000000000000L) ? " M" : ((maxamt >= 1000000000L) ? " K" : "")), true));
            sf.SetWidth(5, ScreenFormatter.GetWidth("8.88" + ((maxqta >= 1000000000000L) ? " M" : ((maxqta >= 1000000000L) ? " K" : "")), true));
            foreach (IMyTextPanel panel in panels)
            WriteTableToPanel("TIM Inventory", sf, panel, true);
        }
} // UpdateInventoryPanels()


void UpdateStatusPanels() {
    long r;
    StringBuilder sb;

    if (statusPanels.Count > 0) {
        sb = new StringBuilder();
        sb.Append(statsHeader);
        for (r = Math.Max(1, numCalls - statsLog.Length + 1);  r <= numCalls;  r++)
        sb.Append(statsLog[r % statsLog.Length]);

        foreach (IMyTextPanel panel in statusPanels) {
            panel.WritePublicTitle("Script Status", false);
            if (panelSpan.ContainsKey(panel))
            debugText.Add("Status panels cannot be spanned");
            panel.WritePublicText(sb.ToString(), false);
            panel.ShowPublicTextOnScreen();
        }
    }

    if (debugPanels.Count > 0) {
        foreach (IMyTerminalBlock blockFrom in blockErrors.Keys) {
            foreach (IMyTerminalBlock blockTo in blockErrors[blockFrom])
            debugText.Add("No conveyor connection from " + blockFrom.CustomName + " to " + blockTo.CustomName);
        }
        foreach (IMyTextPanel panel in debugPanels) {
            panel.WritePublicTitle("Script Debugging", false);
            if (panelSpan.ContainsKey(panel))
            debugText.Add("Debug panels cannot be spanned");
            panel.WritePublicText(String.Join("\n", debugText), false);
            panel.ShowPublicTextOnScreen();
        }
    }
    blockErrors.Clear();
} // UpdateStatusPanels()


void WriteTableToPanel(string title, ScreenFormatter sf, IMyTextPanel panel, bool allowspan=true, string before="", string after="") {
    int spanx, spany, rows, wide, size, width, height;
    int x, y, r;
    float fontsize;
    string[][] spanLines;
    string text;
    Matrix matrix;
    IMySlimBlock slim;
    IMyTextPanel spanpanel;

    // get the spanning dimensions, if any
    wide = panel.BlockDefinition.SubtypeName.EndsWith("Wide") ? 2 : 1;
    size = panel.BlockDefinition.SubtypeName.StartsWith("Small") ? 3 : 1;
    spanx = spany = 1;
    if (allowspan & panelSpan.ContainsKey(panel)) {
        spanx = panelSpan[panel].a;
        spany = panelSpan[panel].b;
    }

    // reduce font size to fit everything
    x = sf.GetMinWidth();
    x = (x / spanx) + ((x % spanx > 0) ? 1 : 0);
    y = sf.GetNumRows();
    y = (y / spany) + ((y % spany > 0) ? 1 : 0);
    width = 658 * wide; // TODO monospace 26x17.5 chars
    fontsize = panel.GetValueFloat("FontSize");
    if (fontsize < 0.25f)
    fontsize = 1.0f;
    if (x > 0)
    fontsize = Math.Min(fontsize, Math.Max(0.5f, (float)(width * 100 / x) / 100.0f));
    if (y > 0)
    fontsize = Math.Min(fontsize, Math.Max(0.5f, (float)(1760 / y) / 100.0f));

    // calculate how much space is available on each panel
    width = (int)((float)width / fontsize);
    height = (int)(17.6f / fontsize);

    // write to each panel
    if (spanx > 1 | spany > 1) {
        spanLines = sf.ToSpan(width, spanx);
        matrix = new Matrix();
        panel.Orientation.GetMatrix(out matrix);
        for (x = 0;  x < spanx;  x++) {
            r = 0;
            for (y = 0;  y < spany;  y++) {
                slim = panel.CubeGrid.GetCubeBlock(new Vector3I(panel.Position  +  x * wide * size * matrix.Right  +  y * size * matrix.Down));
                if (slim != null && (slim.FatBlock is IMyTextPanel) && ""+slim.FatBlock.BlockDefinition == ""+panel.BlockDefinition) {
                    spanpanel = slim.FatBlock as IMyTextPanel;
                    rows = Math.Max(0, spanLines[x].Length - r);
                    if (y + 1 < spany)
                    rows = Math.Min(rows, height);
                    text = "";
                    if (r < spanLines[x].Length)
                    text = String.Join("\n", spanLines[x], r, rows);
                    if (x == 0)
                    text += ((y == 0) ? before : (((y + 1) == spany) ? after : ""));
                    spanpanel.SetValueFloat("FontSize", fontsize);
                    spanpanel.WritePublicTitle(title + " (" + (x+1) + "," + (y+1) + ")", false);
                    spanpanel.WritePublicText(text, false);
                    spanpanel.ShowPublicTextOnScreen();
                }
                r += height;
            }
        }
    } else {
        panel.SetValueFloat("FontSize", fontsize);
        panel.WritePublicTitle(title, false);
        panel.WritePublicText(before + sf.ToString(width) + after, false);
        panel.ShowPublicTextOnScreen();
    }
} // WriteTableToPanel()


/*
* MAIN
*/


public Program() {
    int ext;

    // parse stored data
    foreach (string line in Me.CustomData.Split(NEWLINE, REE)) {
        string[] kv = line.Trim().Split('=');
        if (kv[0].Equals("TIM_version", OIC)) {
            if (!int.TryParse(kv[1], out lastVersion) | lastVersion > VERSION) {
                Echo("Invalid prior version: "+lastVersion);
                lastVersion = 0;
            }
        }
    }

    // initialize panel data
    ScreenFormatter.Init();
    statsHeader = (
        "Taleden's Inventory Manager\n" +
        "v"+VERS_MAJ+"."+VERS_MIN+"."+VERS_REV+" ("+VERS_UPD+")\n\n" +
        ScreenFormatter.Format("Run", 80, out ext, 1) +
        ScreenFormatter.Format("Step", 125+ext, out ext, 1) +
        ScreenFormatter.Format("Time", 145+ext, out ext, 1) +
        ScreenFormatter.Format("Load", 105+ext, out ext, 1) +
        ScreenFormatter.Format("S", 65+ext, out ext, 1) +
        ScreenFormatter.Format("R", 65+ext, out ext, 1) +
        ScreenFormatter.Format("A", 65+ext, out ext, 1) +
        "\n\n"
        );

    // initialize default items, quotas, labels and blueprints
    // (TIM can also learn new items it sees in inventory)
    InitItems(DEFAULT_ITEMS);

    // initialize block:item restrictions
    // (TIM can also learn new restrictions whenever item transfers fail)
    InitBlockRestrictions(DEFAULT_RESTRICTIONS);

    Echo("Compiled TIM v"+VERS_MAJ+"."+VERS_MIN+"."+VERS_REV+" ("+VERS_UPD+")");
} // Program()


public void Save() {
} // Save()


void Main(string argument) {
    // throttle interval
    if (numCalls > 0 & (sinceLast += Runtime.TimeSinceLastRun.TotalSeconds) < 0.5)
    return;
    sinceLast = 0.0;

    DateTime dtStart = DateTime.Now;
    int i, j, argCycle, step, time, load;
    bool argRewriteTags, argScanCollectors, argScanDrills, argScanGrinders, argScanWelders, argQuotaStable, toggle;
    char argTagOpen, argTagClose;
    string argTagPrefix, msg;
    StringBuilder sb = new StringBuilder();
    List<IMyTerminalBlock> blocks;

    // output terminal info
    numCalls++;
    Echo("Taleden's Inventory Manager");
    Echo("v"+VERS_MAJ+"."+VERS_MIN+"."+VERS_REV+" ("+VERS_UPD+")");
    Echo("Last Run: #"+numCalls+" at "+dtStart.ToString("h:mm:ss tt"));
    if (lastVersion > 0 & lastVersion < VERSION)
    Echo("Upgraded from v"+(lastVersion/1000000)+"."+(lastVersion/1000%1000)+"."+(lastVersion%1000));

    // reset status and debugging data every cycle
    debugText.Clear();
    debugLogic.Clear();
    step = numXfers = numRefs = numAsms = 0;

    // parse arguments
    toggle = true;
    argRewriteTags = REWRITE_TAGS;
    argTagOpen = TAG_OPEN;
    argTagClose = TAG_CLOSE;
    argTagPrefix = TAG_PREFIX;
    argCycle = CYCLE_LENGTH;
    argScanCollectors = SCAN_COLLECTORS;
    argScanDrills = SCAN_DRILLS;
    argScanGrinders = SCAN_GRINDERS;
    argScanWelders = SCAN_WELDERS;
    argQuotaStable = QUOTA_STABLE;
    foreach (string arg in argument.Split(SPACE, REE)) {
        if (arg.Equals("rewrite", OIC)) {
            argRewriteTags = true;
            debugText.Add("Tag rewriting enabled");
            } else if (arg.Equals("norewrite", OIC)) {
                argRewriteTags = false;
                debugText.Add("Tag rewriting disabled");
                } else if (arg.StartsWith("tags=", OIC)) {
                    msg = arg.Substring(5);
                    if (msg.Length != 2) {
                        Echo("Invalid 'tags=' delimiters \"" + msg + "\": must be exactly two characters");
                        toggle = false;
                        } else if (msg[0] == ' ' || msg[1] == ' ') {
                            Echo("Invalid 'tags=' delimiters \"" + msg + "\": cannot be spaces");
                            toggle = false;
                        } else if (char.ToUpper(msg[0]) == char.ToUpper(msg[1])) {
                            Echo("Invalid 'tags=' delimiters \"" + msg + "\": characters must be different");
                            toggle = false;
                        } else {
                            argTagOpen = char.ToUpper(msg[0]);
                            argTagClose = char.ToUpper(msg[1]);
                            debugText.Add("Tags are delimited by \"" + argTagOpen + "\" and \"" + argTagClose + "\"");
                        }
                        } else if (arg.StartsWith("prefix=", OIC)) {
                            argTagPrefix = arg.Substring(7).Trim().ToUpper();
                            if (argTagPrefix == "") {
                                debugText.Add("Tag prefix disabled");
                            } else {
                                debugText.Add("Tag prefix is \"" + argTagPrefix + "\"");
                            }
                            } else if (arg.StartsWith("cycle=", OIC)) {
                                if (int.TryParse(arg.Substring(6), out argCycle) == false || argCycle < 1) {
                                    Echo("Invalid 'cycle=' length \"" + arg.Substring(6) + "\": must be a positive integer");
                                    toggle = false;
                                } else {
                                    argCycle = Math.Min(Math.Max(argCycle, 1), MAX_CYCLE_STEPS);
                                    if (argCycle < 2) {
                                        debugText.Add("Function cycling disabled");
                                    } else {
                                        debugText.Add("Cycle length is " + argCycle);
                                    }
                                }
                                } else if (arg.StartsWith("scan=", OIC)) {
                                    msg = arg.Substring(5);
                                    if (msg.Equals("collectors", OIC)) {
                                        argScanCollectors = true;
                                        debugText.Add("Enabled scanning of Collectors");
                                        } else if (msg.Equals("drills", OIC)) {
                                            argScanDrills = true;
                                            debugText.Add("Enabled scanning of Drills");
                                            } else if (msg.Equals("grinders", OIC)) {
                                                argScanGrinders = true;
                                                debugText.Add("Enabled scanning of Grinders");
                                                } else if (msg.Equals("welders", OIC)) {
                                                    argScanWelders = true;
                                                    debugText.Add("Enabled scanning of Welders");
                                                } else {
                                                    Echo("Invalid 'scan=' block type '" + msg + "': must be 'collectors', 'drills', 'grinders' or 'welders'");
                                                    toggle = false;
                                                }
                                                } else if (arg.StartsWith("quota=", OIC)) {
                                                    msg = arg.Substring(6);
                                                    if (msg.Equals("literal", OIC)) {
                                                        argQuotaStable = false;
                                                        debugText.Add("Disabled stable dynamic quotas");
                                                        } else if (msg.Equals("stable", OIC)) {
                                                            argQuotaStable = true;
                                                            debugText.Add("Enabled stable dynamic quotas");
                                                        } else {
                                                            Echo("Invalid 'quota=' mode '" + msg + "': must be 'literal' or 'stable'");
                                                            toggle = false;
                                                        }
                                                        } else if (arg.StartsWith("debug=", OIC)) {
                                                            msg = arg.Substring(6);
                                                            if (msg.Length >= 1 & "quotas".StartsWith(msg, OIC)) {
                                                                debugLogic.Add("quotas");
                                                                } else if (msg.Length >= 1 & "sorting".StartsWith(msg, OIC)) {
                                                                    debugLogic.Add("sorting");
                                                                    } else if (msg.Length >= 1 & "refineries".StartsWith(msg, OIC)) {
                                                                        debugLogic.Add("refineries");
                                                                        } else if (msg.Length >= 1 & "assemblers".StartsWith(msg, OIC)) {
                                                                            debugLogic.Add("assemblers");
                                                                        } else {
                                                                            Echo("Invalid 'debug=' type '" + msg + "': must be 'quotas', 'sorting', 'refineries', or 'assemblers'");
                                                                            toggle = false;
                                                                        }
                                                                    } else {
                                                                        Echo("Unrecognized argument: " + arg);
                                                                        toggle = false;
                                                                    }
                                                                }
                                                                if (toggle == false)
                                                                return;

    // apply changed arguments
                                                                toggle = (tagOpen != argTagOpen) | (tagClose != argTagClose) | (tagPrefix != argTagPrefix);
                                                                if ((toggle | (rewriteTags != argRewriteTags) | (cycleLength != argCycle)) && (cycleStep > 0)) {
                                                                    cycleStep = 0;
                                                                    Echo(msg = "Options changed; cycle step reset.");
                                                                    debugText.Add(msg);
                                                                }
                                                                rewriteTags = argRewriteTags;
                                                                tagOpen = argTagOpen;
                                                                tagClose = argTagClose;
                                                                tagPrefix = argTagPrefix;
                                                                cycleLength = argCycle;
                                                                if (tagRegex == null | toggle) {
                                                                    msg = "\\" + tagOpen;
                                                                    if (tagPrefix != "") {
                                                                        msg += " *" + System.Text.RegularExpressions.Regex.Escape(tagPrefix) + "(|[ ,]+[^\\" + tagClose + "]*)";
                                                                    } else {
                                                                        msg += "([^\\" + tagClose + "]*)";
                                                                    }
                                                                    msg += "\\" + tagClose;
                                                                    tagRegex = new System.Text.RegularExpressions.Regex(msg, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                                                                }

    // scan connectors before PGs! if another TIM is on a grid that is *not* correctly docked, both still need to run
                                                                if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS) {
                                                                    if (cycleLength > 1) {
                                                                        Echo(msg = "Scanning grid connectors ...");
                                                                        debugText.Add(msg);
                                                                    }
                                                                    ScanGrids();
                                                                }

    // search for other TIMs
                                                                blocks = new List<IMyTerminalBlock>();
                                                                GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(blocks, (IMyTerminalBlock blk) => (blk == Me) | (tagRegex.IsMatch(blk.CustomName) & dockedgrids.Contains(blk.CubeGrid)));
                                                                i = blocks.IndexOf(Me);
                                                                j = blocks.FindIndex(block => block.IsFunctional & block.IsWorking);
                                                                msg = tagOpen + tagPrefix + ((blocks.Count > 1) ? (" #"+(i+1)) : "") + tagClose;
                                                                Me.CustomName = tagRegex.IsMatch(Me.CustomName) ? tagRegex.Replace(Me.CustomName, msg, 1) : (Me.CustomName + " " + msg);
                                                                if (i != j) {
                                                                    Echo("TIM #" + (j + 1) + " is on duty. Standing by.");
                                                                    if (("" + (blocks[j] as IMyProgrammableBlock).TerminalRunArgument).Trim() != ("" + Me.TerminalRunArgument).Trim())
                                                                    Echo("WARNING: Script arguments do not match TIM #" + (j + 1) + ".");
                                                                    return;
                                                                }

    // TODO: API testing
/**
    GridTerminalSystem.GetBlocksOfType<IMyShipController>(blocks);
    Echo(""+blocks[0].GetInventory(0).Owner);
/**/

    if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS) {
        if (cycleLength > 1) {
            Echo(msg = "Scanning inventories ...");
            debugText.Add(msg);
        }

        // reset everything that we'll check during this step
        foreach (string itype in types) {
            typeAmount[itype] = 0;
            foreach (ItemData data in typeSubData[itype].Values) {
                data.amount = 0L;
                data.avail = 0L;
                data.locked = 0L;
                data.invenTotal.Clear();
                data.invenSlot.Clear();
            }
        }
        blockTag.Clear();
        blockGtag.Clear();
        invenLocked.Clear();
        invenHidden.Clear();

        // scan inventories
        ScanGroups();
        ScanBlocks<IMyAssembler>();
        ScanBlocks<IMyCargoContainer>();
        if (argScanCollectors)
        ScanBlocks<IMyCollector>();
        ScanBlocks<IMyGasGenerator>();
        ScanBlocks<IMyGasTank>();
        ScanBlocks<IMyReactor>();
        ScanBlocks<IMyRefinery>();
        ScanBlocks<IMyShipConnector>();
        ScanBlocks<IMyShipController>();
        if (argScanDrills)
        ScanBlocks<IMyShipDrill>();
        if (argScanGrinders)
        ScanBlocks<IMyShipGrinder>();
        if (argScanWelders)
        ScanBlocks<IMyShipWelder>();
        ScanBlocks<IMyTextPanel>();
        ScanBlocks<IMyUserControllableGun>();

        // if we found any new item type/subtypes, re-sort the lists
        if (foundNewItem) {
            foundNewItem = false;
            types.Sort();
            foreach (string itype in types)
            typeSubs[itype].Sort();
            subs.Sort();
            foreach (string isub in subs)
            subTypes[isub].Sort();
        }
    }

    if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS) {
        if (cycleLength > 1) {
            Echo(msg = "Scanning tags ...");
            debugText.Add(msg);
        }

        // reset everything that we'll check during this step
        foreach (string itype in types) {
            foreach (ItemData data in typeSubData[itype].Values) {
                data.qpriority = -1;
                data.quota = 0L;
                data.producers.Clear();
            }
        }
        qpanelPriority.Clear();
        qpanelTypes.Clear();
        ipanelTypes.Clear();
        priTypeSubInvenRequest.Clear();
        statusPanels.Clear();
        debugPanels.Clear();
        refineryOres.Clear();
        assemblerItems.Clear();
        panelSpan.Clear();

        // parse tags
        ParseBlockTags();
    }

    if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS) {
        if (cycleLength > 1) {
            Echo(msg = "Adjusting tallies ...");
            debugText.Add(msg);
        }
        AdjustAmounts();
    }

    if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS) {
        if (cycleLength > 1) {
            Echo(msg = "Scanning quota panels ...");
            debugText.Add(msg);
        }
        ProcessQuotaPanels(argQuotaStable);
    }

    if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS) {
        if (cycleLength > 1) {
            Echo(msg = "Processing limited item requests ...");
            debugText.Add(msg);
        }
        AllocateItems(true); // limited requests
    }

    if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS) {
        if (cycleLength > 1) {
            Echo(msg = "Managing refineries ...");
            debugText.Add(msg);
        }
        ManageRefineries();
    }

    if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS) {
        if (cycleLength > 1) {
            Echo(msg = "Processing remaining item requests ...");
            debugText.Add(msg);
        }
        AllocateItems(false); // unlimited requests
    }

    if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS) {
        if (cycleLength > 1) {
            Echo(msg = "Managing assemblers ...");
            debugText.Add(msg);
        }
        ManageAssemblers();
    }

    if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS) {
        if (cycleLength > 1) {
            Echo(msg = "Scanning production ...");
            debugText.Add(msg);
        }
        ScanProduction();
    }

    if (cycleStep == step++ * cycleLength / MAX_CYCLE_STEPS) {
        if (cycleLength > 1) {
            Echo(msg = "Updating inventory panels ...");
            debugText.Add(msg);
        }
        UpdateInventoryPanels();

        // update persistent data after one full cycle
        Me.CustomData = "TIM_version=" + (lastVersion = VERSION);
    }

    if (step != MAX_CYCLE_STEPS)
    debugText.Add("ERROR: step"+step+" of "+MAX_CYCLE_STEPS);

    // update script status and debug panels on every cycle step
    cycleStep++;
    time = (int)((DateTime.Now - dtStart).TotalMilliseconds + 0.5);
    load = (int)(100.0f * Runtime.CurrentInstructionCount / Runtime.MaxInstructionCount + 0.5);
    i = 0;
    statsLog[numCalls % statsLog.Length] = (
        ScreenFormatter.Format(""+numCalls, 80, out i, 1) +
        ScreenFormatter.Format(cycleStep+" / "+cycleLength, 125+i, out i, 1, true) +
        ScreenFormatter.Format(time+" ms", 145+i, out i, 1) +
        ScreenFormatter.Format(load+"%", 105+i, out i, 1, true) +
        ScreenFormatter.Format(""+numXfers, 65+i, out i, 1, true) +
        ScreenFormatter.Format(""+numRefs, 65+i, out i, 1, true) +
        ScreenFormatter.Format(""+numAsms, 65+i, out i, 1, true) +
        "\n"
        );
    Echo(msg = ((cycleLength > 1) ? ("Cycle "+cycleStep+" of "+cycleLength+" completed in ") : "Completed in ")+time+" ms, "+load+"% load ("+Runtime.CurrentInstructionCount+" instructions)");
    debugText.Add(msg);
    UpdateStatusPanels();
    if (cycleStep >= cycleLength)
    cycleStep = 0;

    // if we can spare the cycles, render the filler
    if (panelFiller == "" & numCalls > cycleLength)
    panelFiller = "This easter egg will return when Keen raises the 100kb script code size limit!\n";
} // Main()


/*
* ScreenFormatter
*/


public class ScreenFormatter
{
    private static Dictionary<char,byte> charWidth = new Dictionary<char,byte>();
    private static Dictionary<string,int> textWidth = new Dictionary<string,int>();
    private static byte SZ_SPACE;
    private static byte SZ_SHYPH;

    public static int GetWidth(string text, bool memoize=false) {
        int width;
        if (!textWidth.TryGetValue(text, out width)) {
            // this isn't faster (probably slower) but it's less "complex"
            // according to SE's silly branch count metric
            Dictionary<char,byte> cW = charWidth;
            string t = text + "\0\0\0\0\0\0\0";
            int i = t.Length - (t.Length % 8);
            byte w0, w1, w2, w3, w4, w5, w6, w7;
            while (i > 0) {
                cW.TryGetValue(t[i-1], out w0);
                cW.TryGetValue(t[i-2], out w1);
                cW.TryGetValue(t[i-3], out w2);
                cW.TryGetValue(t[i-4], out w3);
                cW.TryGetValue(t[i-5], out w4);
                cW.TryGetValue(t[i-6], out w5);
                cW.TryGetValue(t[i-7], out w6);
                cW.TryGetValue(t[i-8], out w7);
                width += w0+w1+w2+w3+w4+w5+w6+w7;
                i -= 8;
            }
            if (memoize)
            textWidth[text] = width;
        }
        return width;
    } // GetWidth()

    public static string Format(string text, int width, out int unused, int align=-1, bool memoize=false) {
        int spaces, bars;

        // '\u00AD' is a "soft hyphen" in UTF16 but Panels don't wrap lines so
        // it's just a wider space character ' ', useful for column alignment
        unused = width - GetWidth(text, memoize);
        if (unused <= SZ_SPACE / 2)
        return text;
        spaces = unused / SZ_SPACE;
        bars = 0;
        unused -= spaces * SZ_SPACE;
        if (2 * unused <= SZ_SPACE + (spaces * (SZ_SHYPH - SZ_SPACE))) {
            bars = Math.Min(spaces, (int)((float)unused / (SZ_SHYPH - SZ_SPACE) + 0.4999f));
            spaces -= bars;
            unused -= bars * (SZ_SHYPH - SZ_SPACE);
        } else if (unused > SZ_SPACE / 2) {
            spaces++;
            unused -= SZ_SPACE;
        }
        if (align > 0)
        return new String(' ', spaces) + new String('\u00AD', bars) + text;
        if (align < 0)
        return text + new String('\u00AD', bars) + new String(' ', spaces);
        if ((spaces % 2) > 0 & (bars % 2) == 0)
        return new String(' ', spaces / 2) + new String('\u00AD', bars / 2) + text + new String('\u00AD', bars / 2) + new String(' ', spaces - (spaces / 2));
        return new String(' ', spaces - (spaces / 2)) + new String('\u00AD', bars / 2) + text + new String('\u00AD', bars - (bars / 2)) + new String(' ', spaces / 2);
    } // Format()

    public static string Format(double value, int width, out int unused) {
        int spaces, bars;
        value = Math.Min(Math.Max(value, 0.0f), 1.0f);
        spaces = width / SZ_SPACE;
        bars = (int)(spaces * value + 0.5f);
        unused = width - (spaces * SZ_SPACE);
        return new String('I', bars) + new String(' ', spaces - bars);
    } // Format()

    public static void Init() {
        InitChars( 0, "\u2028\u2029\u202F");
        InitChars( 7, "'|\u00A6\u02C9\u2018\u2019\u201A");
        InitChars( 8, "\u0458");
        InitChars( 9, " !I`ijl\u00A0\u00A1\u00A8\u00AF\u00B4\u00B8\u00CC\u00CD\u00CE\u00CF\u00EC\u00ED\u00EE\u00EF\u0128\u0129\u012A\u012B\u012E\u012F\u0130\u0131\u0135\u013A\u013C\u013E\u0142\u02C6\u02C7\u02D8\u02D9\u02DA\u02DB\u02DC\u02DD\u0406\u0407\u0456\u0457\u2039\u203A\u2219");
        InitChars(10, "(),.1:;[]ft{}\u00B7\u0163\u0165\u0167\u021B");
        InitChars(11, "\"-r\u00AA\u00AD\u00BA\u0140\u0155\u0157\u0159");
        InitChars(12, "*\u00B2\u00B3\u00B9");
        InitChars(13, "\\\u00B0\u201C\u201D\u201E");
        InitChars(14, "\u0491");
        InitChars(15, "/\u0133\u0442\u044D\u0454");
        InitChars(16, "L_vx\u00AB\u00BB\u0139\u013B\u013D\u013F\u0141\u0413\u0433\u0437\u043B\u0445\u0447\u0490\u2013\u2022");
        InitChars(17, "7?Jcz\u00A2\u00BF\u00E7\u0107\u0109\u010B\u010D\u0134\u017A\u017C\u017E\u0403\u0408\u0427\u0430\u0432\u0438\u0439\u043D\u043E\u043F\u0441\u044A\u044C\u0453\u0455\u045C");
        InitChars(18, "3FKTabdeghknopqsuy\u00A3\u00B5\u00DD\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E8\u00E9\u00EA\u00EB\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF\u00FF\u0101\u0103\u0105\u010F\u0111\u0113\u0115\u0117\u0119\u011B\u011D\u011F\u0121\u0123\u0125\u0127\u0136\u0137\u0144\u0146\u0148\u0149\u014D\u014F\u0151\u015B\u015D\u015F\u0161\u0162\u0164\u0166\u0169\u016B\u016D\u016F\u0171\u0173\u0176\u0177\u0178\u0219\u021A\u040E\u0417\u041A\u041B\u0431\u0434\u0435\u043A\u0440\u0443\u0446\u044F\u0451\u0452\u045B\u045E\u045F");
        InitChars(19, "+<=>E^~\u00AC\u00B1\u00B6\u00C8\u00C9\u00CA\u00CB\u00D7\u00F7\u0112\u0114\u0116\u0118\u011A\u0404\u040F\u0415\u041D\u042D\u2212");
        InitChars(20, "#0245689CXZ\u00A4\u00A5\u00C7\u00DF\u0106\u0108\u010A\u010C\u0179\u017B\u017D\u0192\u0401\u040C\u0410\u0411\u0412\u0414\u0418\u0419\u041F\u0420\u0421\u0422\u0423\u0425\u042C\u20AC");
        InitChars(21, "$&GHPUVY\u00A7\u00D9\u00DA\u00DB\u00DC\u00DE\u0100\u011C\u011E\u0120\u0122\u0124\u0126\u0168\u016A\u016C\u016E\u0170\u0172\u041E\u0424\u0426\u042A\u042F\u0436\u044B\u2020\u2021");
        InitChars(22, "ABDNOQRS\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D8\u0102\u0104\u010E\u0110\u0143\u0145\u0147\u014C\u014E\u0150\u0154\u0156\u0158\u015A\u015C\u015E\u0160\u0218\u0405\u040A\u0416\u0444");
        InitChars(23, "\u0459");
        InitChars(24, "\u044E");
        InitChars(25, "%\u0132\u042B");
        InitChars(26, "@\u00A9\u00AE\u043C\u0448\u045A");
        InitChars(27, "M\u041C\u0428");
        InitChars(28, "mw\u00BC\u0175\u042E\u0449");
        InitChars(29, "\u00BE\u00E6\u0153\u0409");
        InitChars(30, "\u00BD\u0429");
        InitChars(31, "\u2122");
        InitChars(32, "W\u00C6\u0152\u0174\u2014\u2026\u2030");
        SZ_SPACE = charWidth[' '];
        SZ_SHYPH = charWidth['\u00AD'];
    } // Init()

    private static void InitChars(byte width, string text) {
        // more silly loop-unrolling, as in GetWidth()
        Dictionary<char,byte> cW = charWidth;
        string t = text + "\0\0\0\0\0\0\0";
        byte w = Math.Max((byte)0, width);
        int i = t.Length - (t.Length % 8);
        while (i > 0) {
            cW[t[--i]] = w;
            cW[t[--i]] = w;
            cW[t[--i]] = w;
            cW[t[--i]] = w;
            cW[t[--i]] = w;
            cW[t[--i]] = w;
            cW[t[--i]] = w;
            cW[t[--i]] = w;
        }
        cW['\0'] = 0;
    } // InitChars()

    private int numCols;
    private int numRows;
    private int padding;
    private List<string>[] colRowText;
    private List<int>[] colRowWidth;
    private int[] colAlign;
    private int[] colFill;
    private bool[] colBar;
    private int[] colWidth;

    public ScreenFormatter(int numCols, int padding=1) {
        this.numCols = numCols;
        this.numRows = 0;
        this.padding = padding;
        this.colRowText = new List<string>[numCols];
        this.colRowWidth = new List<int>[numCols];
        this.colAlign = new int[numCols];
        this.colFill = new int[numCols];
        this.colBar = new bool[numCols];
        this.colWidth = new int[numCols];
        for (int c = 0;  c < numCols;  c++) {
            this.colRowText[c] = new List<string>();
            this.colRowWidth[c] = new List<int>();
            this.colAlign[c] = -1;
            this.colFill[c] = 0;
            this.colBar[c] = false;
            this.colWidth[c] = 0;
        }
    } // ScreenFormatter()

    public void Add(int col, string text, bool memoize=false) {
        int width = 0;
        this.colRowText[col].Add(text);
        if (this.colBar[col] == false) {
            width = GetWidth(text, memoize);
            this.colWidth[col] = Math.Max(this.colWidth[col], width);
        }
        this.colRowWidth[col].Add(width);
        this.numRows = Math.Max(this.numRows, this.colRowText[col].Count);
    } // Add()

    public void AddBlankRow() {
        for (int c = 0;  c < this.numCols;  c++) {
            this.colRowText[c].Add("");
            this.colRowWidth[c].Add(0);
        }
        this.numRows++;
    } // AddBlankRow()

    public int GetNumRows() {
        return this.numRows;
    } // GetNumRows()

    public int GetMinWidth() {
        int width = this.padding * SZ_SPACE;
        for (int c = 0;  c < this.numCols;  c++)
        width += this.padding * SZ_SPACE + this.colWidth[c];
        return width;
    } // GetMinWidth()

    public void SetAlign(int col, int align) {
        this.colAlign[col] = align;
    } // SetAlign()

    public void SetFill(int col, int fill = 1) {
        this.colFill[col] = fill;
    } // SetFill()

    public void SetBar(int col, bool bar = true) {
        this.colBar[col] = bar;
    } // SetBar()

    public void SetWidth(int col, int width) {
        this.colWidth[col] = width;
    } // SetWidth()

    public string[][] ToSpan(int width=0, int span=1) {
        int c, r, s, i, j, textwidth, unused, remaining;
        int[] colWidth;
        byte w;
        double value;
        string text;
        StringBuilder sb;
        string[][] spanLines;

        // clone the user-defined widths and tally fill columns
        colWidth = (int[])this.colWidth.Clone();
        unused = width * span - this.padding * SZ_SPACE;
        remaining = 0;
        for (c = 0;  c < this.numCols;  c++) {
            unused -= this.padding * SZ_SPACE;
            if (this.colFill[c] == 0)
            unused -= colWidth[c];
            remaining += this.colFill[c];
        }

        // distribute remaining width to fill columns
        for (c = 0;  c < this.numCols & remaining > 0;  c++) {
            if (this.colFill[c] > 0) {
                colWidth[c] = Math.Max(colWidth[c], this.colFill[c] * unused / remaining);
                unused -= colWidth[c];
                remaining -= this.colFill[c];
            }
        }

        // initialize output arrays
        spanLines = new string[span][];
        for (s = 0;  s < span;  s++)
        spanLines[s] = new string[this.numRows];
        span--; // make "span" inclusive so "s < span" implies one left

        // render all rows and columns
        i = 0;
        sb = new StringBuilder();
        for (r = 0;  r < this.numRows;  r++) {
            sb.Clear();
            s = 0;
            remaining = width;
            unused = 0;
            for (c = 0;  c < this.numCols;  c++) {
                unused += this.padding * SZ_SPACE;
                if (r >= this.colRowText[c].Count || colRowText[c][r] == "") {
                    unused += colWidth[c];
                } else {
                    // render the bar, or fetch the cell text
                    text = this.colRowText[c][r];
                    charWidth.TryGetValue(text[0], out w);
                    textwidth = this.colRowWidth[c][r];
                    if (this.colBar[c] == true) {
                        value = 0.0;
                        if (double.TryParse(text, out value))
                        value = Math.Min(Math.Max(value, 0.0), 1.0);
                        i = (int)((colWidth[c] / SZ_SPACE) * value + 0.5);
                        w = SZ_SPACE;
                        textwidth = i * SZ_SPACE;
                    }

                    // if the column is not left-aligned, calculate left spacing
                    if (this.colAlign[c] > 0) {
                        unused += (colWidth[c] - textwidth);
                    } else if (this.colAlign[c] == 0) {
                        unused += (colWidth[c] - textwidth) / 2;
                    }

                    // while the left spacing leaves no room for text, adjust it
                    while (s < span & unused > remaining - w) {
                        sb.Append(' ');
                        spanLines[s][r] = sb.ToString();
                        sb.Clear();
                        s++;
                        unused -= remaining;
                        remaining = width;
                    }

                    // add left spacing
                    remaining -= unused;
                    sb.Append(Format("", unused, out unused));
                    remaining += unused;

                    // if the column is not right-aligned, calculate right spacing
                    if (this.colAlign[c] < 0) {
                        unused += (colWidth[c] - textwidth);
                    } else if (this.colAlign[c] == 0) {
                        unused += (colWidth[c] - textwidth) - ((colWidth[c] - textwidth) / 2);
                    }

                    // while the bar or text runs to the next span, split it
                    if (this.colBar[c] == true) {
                        while (s < span & textwidth > remaining) {
                            j = remaining / SZ_SPACE;
                            remaining -= j * SZ_SPACE;
                            textwidth -= j * SZ_SPACE;
                            sb.Append(new String('I', j));
                            spanLines[s][r] = sb.ToString();
                            sb.Clear();
                            s++;
                            unused -= remaining;
                            remaining = width;
                            i -= j;
                        }
                        text = new String('I', i);
                    } else {
                        while (s < span & textwidth > remaining) {
                            i = 0;
                            while (remaining >= w) {
                                remaining -= w;
                                textwidth -= w;
                                charWidth.TryGetValue(text[++i], out w);
                            }
                            sb.Append(text, 0, i);
                            spanLines[s][r] = sb.ToString();
                            sb.Clear();
                            s++;
                            unused -= remaining;
                            remaining = width;
                            text = text.Substring(i);
                        }
                    }

                    // add cell text
                    remaining -= textwidth;
                    sb.Append(text);
                }
            }
            spanLines[s][r] = sb.ToString();
        }

        return spanLines;
    } // ToSpan()

    public string ToString(int width=0) {
        return String.Join("\n", this.ToSpan(width, 1)[0]);
    } // ToString()

} // ScreenFormatter

// UPDATED MINIFIED
/*
 *   R e a d m e
 *   -----------
 * 
 *   Uses https://github.com/malware-dev/MDK-SE to minify
 *   
 *   
     Taleden's Inventory Manager
     version 1.7.0 (2018-10-30)

     Updated by Therian

     "There are some who call me... TIM?"

     Steam Workshop: http://steamcommunity.com/sharedfiles/filedetails/?id=546825757
     User's Guide:   http://steamcommunity.com/sharedfiles/filedetails/?id=546909551
     Therian's Updated version: https://steamcommunity.com/sharedfiles/filedetails/?id=1552258272 


     **********************
     ADVANCED CONFIGURATION

     The settings below may be changed if you like, but read the notes and remember
     that any changes will be reverted when you update the script from the workshop.
     

         Each "Type/" section can have multiple "/Subtype"s, which are formatted like
         "/Subtype,MinQta,PctQta,Label,Blueprint". Label and Blueprint specified only
         if different from Subtype, but Ingot and Ore have no Blueprint. Quota values
         are based on material requirements for various blueprints (some built in to
         the game, some from the community workshop).
 *   
 * 
 */
const string Ʀ=@"
AmmoMagazine/
/Missile200mm
/NATO_25x184mm,,,,NATO_25x184mmMagazine
/NATO_5p56x45mm,,,,NATO_5p56x45mmMagazine

Component/
/BulletproofGlass,50,2%
/Computer,30,5%,,ComputerComponent
/Construction,150,20%,,ConstructionComponent
/Detector,10,0.1%,,DetectorComponent
/Display,10,0.5%
/Explosives,5,0.1%,,ExplosivesComponent
/Girder,10,0.5%,,GirderComponent
/GravityGenerator,1,0.1%,GravityGen,GravityGeneratorComponent
/InteriorPlate,100,10%
/LargeTube,10,2%
/Medical,15,0.1%,,MedicalComponent
/MetalGrid,20,2%
/Motor,20,4%,,MotorComponent
/PowerCell,20,1%
/RadioCommunication,10,0.5%,RadioComm,RadioCommunicationComponent
/Reactor,25,2%,,ReactorComponent
/SmallTube,50,3%
/SolarCell,20,0.1%
/SteelPlate,150,40%
/Superconductor,10,1%
/Thrust,15,5%,,ThrustComponent

GasContainerObject/
/HydrogenBottle

Ingot/
/Cobalt,50,3.5%
/Gold,5,0.2%
/Iron,200,88%
/Magnesium,5,0.1%
/Nickel,30,1.5%
/Platinum,5,0.1%
/Silicon,50,2%
/Silver,20,1%
/Stone,50,2.5%
/Uranium,1,0.1%

Ore/
/Cobalt
/Gold
/Ice
/Iron
/Magnesium
/Nickel
/Platinum
/Scrap
/Silicon
/Silver
/Stone
/Uranium

OxygenContainerObject/
/OxygenBottle

PhysicalGunObject/
/AngleGrinderItem,,,,AngleGrinder
/AngleGrinder2Item,,,,AngleGrinder2
/AngleGrinder3Item,,,,AngleGrinder3
/AngleGrinder4Item,,,,AngleGrinder4
/AutomaticRifleItem,,,AutomaticRifle,AutomaticRifle
/HandDrillItem,,,,HandDrill
/HandDrill2Item,,,,HandDrill2
/HandDrill3Item,,,,HandDrill3
/HandDrill4Item,,,,HandDrill4
/PreciseAutomaticRifleItem,,,PreciseAutomaticRifle,PreciseAutomaticRifle
/RapidFireAutomaticRifleItem,,,RapidFireAutomaticRifle,RapidFireAutomaticRifle
/UltimateAutomaticRifleItem,,,UltimateAutomaticRifle,UltimateAutomaticRifle
/WelderItem,,,,Welder
/Welder2Item,,,,Welder2
/Welder3Item,,,,Welder3
/Welder4Item,,,,Welder4
";static HashSet<string>Ƨ=new HashSet<string>{"INGOT","ORE"};static Dictionary<string,string>ƨ=new Dictionary<string,
string>{{"ICE",""},{"ORGANIC",""},{"SCRAP","IRON"}};const string Ʃ=Ɵ+
"Assembler:AmmoMagazine,Component,GasContainerObject,Ore,OxygenContainerObject,PhysicalGunObject\n"+Ɵ+"InteriorTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_25x184mm,"+Ƙ+Ɵ+
"LargeGatlingTurret:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm,"+Ƙ+Ɵ+"LargeMissileTurret:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm,"+Ƙ+Ɵ+"OxygenGenerator:AmmoMagazine,Component,Ingot,Ore/Cobalt,Ore/Gold,Ore/Iron,Ore/Magnesium,Ore/Nickel,Ore/Organic,Ore/Platinum,Ore/Scrap,Ore/Silicon,Ore/Silver,Ore/Stone,Ore/Uranium,PhysicalGunObject\n"
+Ɵ+"OxygenTank:AmmoMagazine,Component,GasContainerObject,Ingot,Ore,PhysicalGunObject\n"+Ɵ+
"OxygenTank/LargeHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n"+Ɵ+"OxygenTank/SmallHydrogenTank:AmmoMagazine,Component,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n"+Ɵ+"Reactor:AmmoMagazine,Component,GasContainerObject,Ingot/Cobalt,Ingot/Gold,Ingot/Iron,Ingot/Magnesium,Ingot/Nickel,Ingot/Platinum,Ingot/Scrap,Ingot/Silicon,Ingot/Silver,Ingot/Stone,Ore,OxygenContainerObject,PhysicalGunObject\n"
+Ɵ+"Refinery:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Ice,Ore/Organic,OxygenContainerObject,PhysicalGunObject\n"
+Ɵ+"Refinery/Blast Furnace:AmmoMagazine,Component,GasContainerObject,Ingot,Ore/Gold,Ore/Ice,Ore/Magnesium,Ore/Organic,Ore/Platinum,Ore/Silicon,Ore/Silver,Ore/Stone,Ore/Uranium,OxygenContainerObject,PhysicalGunObject\n"
+Ɵ+"SmallGatlingGun:AmmoMagazine/Missile200mm,AmmoMagazine/NATO_5p56x45mm,"+Ƙ+Ɵ+
"SmallMissileLauncher:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm,"+Ƙ+Ɵ+"SmallMissileLauncherReload:AmmoMagazine/NATO_25x184mm,AmmoMagazine/NATO_5p56x45mm,"+Ƙ;const int ƪ=1,ƫ=7,Ƭ=0;const
string ƭ="2018-10-30";const int Ʈ=(ƪ*1000000)+(ƫ*1000)+Ƭ;const int Ư=11,ư=1;const bool Ʊ=true,Ʋ=true;const char Ƴ='[',ƴ=']';
const string Ƶ="TIM";const bool ƶ=false,Ʒ=false,ƥ=false,Ɨ=false;const string Ɵ="MyObjectBuilder_";const string Ƙ=
"Component,GasContainerObject,Ingot,Ore,OxygenContainerObject,PhysicalGunObject\n";const StringComparison ƙ=StringComparison.OrdinalIgnoreCase;const StringSplitOptions ƚ=StringSplitOptions.
RemoveEmptyEntries;static char[]ƛ=new char[]{' ','\t','\u00AD'},Ɯ=new char[]{':'},Ɲ=new char[]{'\r','\n'},ƞ=new char[]{' ','\t','\u00AD',
','};struct Ơ{public int Ƥ;public float é;public Ơ(int ơ,float Ĥ){Ƥ=ơ;é=Ĥ;}}struct Ƣ{public int Ô,ƣ;public Ƣ(int Ƹ,int Ǘ){Ô
=Ƹ;ƣ=Ǘ;}}struct Ǎ{public string Ä,É;public Ǎ(string á,string ò){Ä=á;É=ò;}}struct ǎ{public Ǎ J;public double Ǐ;public ǎ(Ǎ
Í,double ǐ){J=Í;Ǐ=ǐ;}}static int Ǒ=0;static string ǒ="";static string[]Ǔ=new string[12];static long ǔ=0;static double Ǖ=
0.0;static int ǖ,ǘ,ǌ;static int ƹ=ư,ǁ=0;static bool ƺ=Ʊ;static char ƻ=Ƴ,Ƽ=ƴ;static string ƽ=Ƶ;static System.Text.
RegularExpressions.Regex ƾ=null;static string ƿ="";static bool ǀ=false;static Dictionary<Ǎ,Ơ>ǂ=new Dictionary<Ǎ,Ơ>();static Dictionary<
string,Dictionary<string,Dictionary<string,HashSet<string>>>>Ǌ=new Dictionary<string,Dictionary<string,Dictionary<string,
HashSet<string>>>>();static HashSet<IMyCubeGrid>ǃ=new HashSet<IMyCubeGrid>();static List<string>Ǆ=new List<string>();static
Dictionary<string,string>ǅ=new Dictionary<string,string>();static Dictionary<string,List<string>>ǆ=new Dictionary<string,List<
string>>();static Dictionary<string,long>Ǉ=new Dictionary<string,long>();static List<string>ǈ=new List<string>();static
Dictionary<string,string>ǉ=new Dictionary<string,string>();static Dictionary<string,List<string>>ǋ=new Dictionary<string,List<
string>>();static Dictionary<string,Dictionary<string,Ź>>Ɩ=new Dictionary<string,Dictionary<string,Ź>>();static Dictionary<
MyDefinitionId,Ǎ>ű=new Dictionary<MyDefinitionId,Ǎ>();static Dictionary<int,Dictionary<string,Dictionary<string,Dictionary<
IMyInventory,long>>>>Ɣ=new Dictionary<int,Dictionary<string,Dictionary<string,Dictionary<IMyInventory,long>>>>();static Dictionary<
IMyTextPanel,int>Ų=new Dictionary<IMyTextPanel,int>();static Dictionary<IMyTextPanel,List<string>>ų=new Dictionary<IMyTextPanel,List
<string>>();static Dictionary<IMyTextPanel,List<string>>Ŵ=new Dictionary<IMyTextPanel,List<string>>();static List<
IMyTextPanel>ŵ=new List<IMyTextPanel>();static List<IMyTextPanel>Ŷ=new List<IMyTextPanel>();static HashSet<string>ŷ=new HashSet<
string>();static List<string>Ÿ=new List<string>();static Dictionary<IMyTerminalBlock,System.Text.RegularExpressions.Match>ź=
new Dictionary<IMyTerminalBlock,System.Text.RegularExpressions.Match>();static Dictionary<IMyTerminalBlock,System.Text.
RegularExpressions.Match>Ƃ=new Dictionary<IMyTerminalBlock,System.Text.RegularExpressions.Match>();static HashSet<IMyInventory>Ż=new
HashSet<IMyInventory>();static HashSet<IMyInventory>ż=new HashSet<IMyInventory>();static Dictionary<IMyRefinery,HashSet<string>
>Ž=new Dictionary<IMyRefinery,HashSet<string>>();static Dictionary<IMyAssembler,HashSet<Ǎ>>ž=new Dictionary<IMyAssembler,
HashSet<Ǎ>>();static Dictionary<IMyFunctionalBlock,ǎ>ſ=new Dictionary<IMyFunctionalBlock,ǎ>();static Dictionary<
IMyFunctionalBlock,int>ƀ=new Dictionary<IMyFunctionalBlock,int>();static Dictionary<IMyTextPanel,Ƣ>Ɓ=new Dictionary<IMyTextPanel,Ƣ>();
static Dictionary<IMyTerminalBlock,HashSet<IMyTerminalBlock>>ƃ=new Dictionary<IMyTerminalBlock,HashSet<IMyTerminalBlock>>();
class Ź{public string Ä,É,ŧ;public MyDefinitionId Ũ;public long G,Ă,ý,ũ,Ū;public float é;public int Ű,ū,Æ;public Dictionary<
IMyInventory,long>Ŭ;public Dictionary<IMyInventory,int>ŭ;public HashSet<IMyFunctionalBlock>Ů;public Dictionary<string,double>ů;
public static void ġ(string Ä,string É,long Ū=0L,float é=0.0f,string ŧ="",string Ũ=""){string Ƒ=Ä,ƒ=É;Ä=Ä.ToUpper();É=É.
ToUpper();if(!ǆ.ContainsKey(Ä)){Ǆ.Add(Ä);ǅ[Ä]=Ƒ;ǆ[Ä]=new List<string>();Ǉ[Ä]=0L;Ɩ[Ä]=new Dictionary<string,Ź>();}if(!ǋ.
ContainsKey(É)){ǈ.Add(É);ǉ[É]=ƒ;ǋ[É]=new List<string>();}if(!Ɩ[Ä].ContainsKey(É)){ǀ=true;ǆ[Ä].Add(É);ǋ[É].Add(Ä);Ɩ[Ä][É]=new Ź(Ä,É,
Ū,é,(ŧ=="")?ƒ:ŧ,(Ũ=="")?ƒ:Ũ);if(Ũ!=null)ű[Ɩ[Ä][É].Ũ]=new Ǎ(Ä,É);}}private Ź(string Ä,string É,long Ū,float é,string ŧ,
string Ũ){this.Ä=Ä;this.É=É;this.ŧ=ŧ;this.Ũ=(Ũ==null)?default(MyDefinitionId):MyDefinitionId.Parse(
"MyObjectBuilder_BlueprintDefinition/"+Ũ);this.G=this.Ă=this.ý=this.ũ=0L;this.Ū=(long)((double)Ū*1000000.0+0.5);this.é=(é/100.0f);this.Ű=-1;this.ū=this.Æ=0;
this.Ŭ=new Dictionary<IMyInventory,long>();this.ŭ=new Dictionary<IMyInventory,int>();this.Ů=new HashSet<IMyFunctionalBlock>(
);this.ů=new Dictionary<string,double>();}}void Ɠ(string I){string Ä="";long Ū;float é;foreach(string Ļ in I.Split(Ɲ,ƚ)){
string[]ƕ=(Ļ.Trim()+",,,,").Split(ƞ,6);ƕ[0]=ƕ[0].Trim();if(ƕ[0].EndsWith("/")){Ä=ƕ[0].Substring(0,ƕ[0].Length-1);}else if(Ä!=
""&ƕ[0].StartsWith("/")){long.TryParse(ƕ[1],out Ū);float.TryParse(ƕ[2].Substring(0,(ƕ[2]+"%").IndexOf("%")),out é);Ź.ġ(Ä,ƕ
[0].Substring(1),Ū,é,ƕ[3].Trim(),(Ä=="Ingot"|Ä=="Ore")?null:ƕ[4].Trim());}}}void Ɛ(string I){foreach(string Ļ in I.Split(
Ɲ,ƚ)){string[]Ƅ=(Ļ+":").Split(':');string[]à=(Ƅ[0]+"/*").Split('/');foreach(string J in Ƅ[1].Split(',')){string[]ƅ=J.
ToUpper().Split('/');Ɔ(à[0].Trim(ƛ),à[1].Trim(ƛ),ƅ[0],((ƅ.Length>1)?ƅ[1]:null),true);}}}void Ɔ(string Ƈ,string ƈ,string Ä,
string É,bool Ɖ=false){Dictionary<string,Dictionary<string,HashSet<string>>>Ɗ;Dictionary<string,HashSet<string>>Ƌ;HashSet<
string>ƌ;if(!Ǌ.TryGetValue(Ƈ.ToUpper(),out Ɗ))Ǌ[Ƈ.ToUpper()]=Ɗ=new Dictionary<string,Dictionary<string,HashSet<string>>>{{"*",
new Dictionary<string,HashSet<string>>()}};if(!Ɗ.TryGetValue(ƈ.ToUpper(),out Ƌ)){Ɗ[ƈ.ToUpper()]=Ƌ=new Dictionary<string,
HashSet<string>>();if(ƈ!="*"&!Ɖ){foreach(KeyValuePair<string,HashSet<string>>ƍ in Ɗ["*"])Ƌ[ƍ.Key]=((ƍ.Value!=null)?(new HashSet
<string>(ƍ.Value)):null);}}if(É==null|É=="*"){Ƌ[Ä]=null;}else{(Ƌ.TryGetValue(Ä,out ƌ)?ƌ:(Ƌ[Ä]=new HashSet<string>())).Add
(É);}if(!Ɖ)Ÿ.Add(Ƈ+"/"+ƈ+" does not accept "+ǅ[Ä]+"/"+ǉ[É]);}bool Ǝ(IMyCubeBlock à,string Ä,string É){Dictionary<string,
Dictionary<string,HashSet<string>>>Ɗ;Dictionary<string,HashSet<string>>Ƌ;HashSet<string>ƌ;if(Ǌ.TryGetValue(à.BlockDefinition.
TypeIdString.ToUpper(),out Ɗ)){Ɗ.TryGetValue(à.BlockDefinition.SubtypeName.ToUpper(),out Ƌ);if((Ƌ??Ɗ["*"]).TryGetValue(Ä,out ƌ))
return!(ƌ==null||ƌ.Contains(É));}return true;}HashSet<string>ȃ(IMyCubeBlock à,string Ä,HashSet<string>ä=null){Dictionary<
string,Dictionary<string,HashSet<string>>>Ɗ;Dictionary<string,HashSet<string>>Ƌ;HashSet<string>ƌ;ä=ä??new HashSet<string>(ǆ[Ä]
);if(Ǌ.TryGetValue(à.BlockDefinition.TypeIdString.ToUpper(),out Ɗ)){Ɗ.TryGetValue(à.BlockDefinition.SubtypeName.ToUpper()
,out Ƌ);if((Ƌ??Ɗ["*"]).TryGetValue(Ä,out ƌ))ä.ExceptWith(ƌ??ä);}return ä;}string Ȅ(IMyCubeBlock à,string É){string ȅ=null
;foreach(string Ä in ǋ[É]){if(Ǝ(à,Ä,É)){if(ȅ!=null)return null;ȅ=Ä;}}return ȅ;}string Ȇ(long G){long ȇ;if(G<=0L)return"0"
;if(G<10000L)return"< 0.01";if(G>=100000000000000L)return""+(G/1000000000000L)+" M";ȇ=(long)Math.Pow(10.0,Math.Floor(Math
.Log10(G))-2.0);G=(long)((double)G/ȇ+0.5)*ȇ;if(G<1000000000L)return(G/1e6).ToString("0.##");if(G<1000000000000L)return(G/
1e9).ToString("0.##")+" K";return(G/1e12).ToString("0.##")+" M";}void Ȉ(){List<IMyTerminalBlock>Ť=new List<IMyTerminalBlock
>();IMyCubeGrid Ȃ,Ǵ;Dictionary<IMyCubeGrid,HashSet<IMyCubeGrid>>Ǻ=new Dictionary<IMyCubeGrid,HashSet<IMyCubeGrid>>();
Dictionary<IMyCubeGrid,int>ǵ=new Dictionary<IMyCubeGrid,int>();List<HashSet<IMyCubeGrid>>Ƕ=new List<HashSet<IMyCubeGrid>>();List<
string>Ƿ=new List<string>();HashSet<IMyCubeGrid>Ǹ;List<IMyCubeGrid>ǹ=new List<IMyCubeGrid>();int ǐ,ǥ,Ǧ;IMyShipConnector ȁ;
HashSet<string>ǻ=new HashSet<string>();HashSet<string>Ǽ=new HashSet<string>();System.Text.RegularExpressions.Match ǳ;Dictionary
<int,Dictionary<int,List<string>>>ǽ=new Dictionary<int,Dictionary<int,List<string>>>();Dictionary<int,List<string>>Ǿ;List
<string>ǿ;HashSet<int>Ȁ=new HashSet<int>();Queue<int>ȉ=new Queue<int>();GridTerminalSystem.GetBlocksOfType<
IMyMechanicalConnectionBlock>(Ť);foreach(IMyTerminalBlock à in Ť){Ȃ=à.CubeGrid;Ǵ=(à as IMyMechanicalConnectionBlock).TopGrid;if(Ǵ==null)continue;(Ǻ.
TryGetValue(Ȃ,out Ǹ)?Ǹ:(Ǻ[Ȃ]=new HashSet<IMyCubeGrid>())).Add(Ǵ);(Ǻ.TryGetValue(Ǵ,out Ǹ)?Ǹ:(Ǻ[Ǵ]=new HashSet<IMyCubeGrid>())).Add(Ȃ
);}foreach(IMyCubeGrid Ș in Ǻ.Keys){if(!ǵ.ContainsKey(Ș)){ǥ=(Ș.Max-Ș.Min+Vector3I.One).Size;Ȃ=Ș;ǵ[Ș]=Ƕ.Count;Ǹ=new
HashSet<IMyCubeGrid>{Ș};ǹ.Clear();ǹ.AddRange(Ǻ[Ș]);for(ǐ=0;ǐ<ǹ.Count;ǐ++){Ǵ=ǹ[ǐ];if(!Ǹ.Add(Ǵ))continue;Ǧ=(Ǵ.Max-Ǵ.Min+Vector3I.
One).Size;Ȃ=(Ǧ>ǥ)?Ǵ:Ȃ;ǥ=(Ǧ>ǥ)?Ǧ:ǥ;ǵ[Ǵ]=Ƕ.Count;ǹ.AddRange(Ǻ[Ǵ].Except(Ǹ));}Ƕ.Add(Ǹ);Ƿ.Add(Ȃ.CustomName);}}
GridTerminalSystem.GetBlocksOfType<IMyShipConnector>(Ť);foreach(IMyTerminalBlock à in Ť){ȁ=(à as IMyShipConnector).OtherConnector;if(ȁ!=
null&&(à.EntityId<ȁ.EntityId&(à as IMyShipConnector).Status==MyShipConnectorStatus.Connected)){ǻ.Clear();Ǽ.Clear();if((ǳ=ƾ.
Match(à.CustomName)).Success){foreach(string Ǡ in ǳ.Groups[1].Captures[0].Value.Split(ƞ,ƚ)){if(Ǡ.StartsWith("DOCK:",ƙ))ǻ.
UnionWith(Ǡ.Substring(5).ToUpper().Split(Ɯ,ƚ));}}if((ǳ=ƾ.Match(ȁ.CustomName)).Success){foreach(string Ǡ in ǳ.Groups[1].Captures[0
].Value.Split(ƞ,ƚ)){if(Ǡ.StartsWith("DOCK:",ƙ))Ǽ.UnionWith(Ǡ.Substring(5).ToUpper().Split(Ɯ,ƚ));}}if((ǻ.Count>0|Ǽ.Count>0
)&!ǻ.Overlaps(Ǽ))continue;Ȃ=à.CubeGrid;Ǵ=ȁ.CubeGrid;if(!ǵ.TryGetValue(Ȃ,out ǥ)){ǵ[Ȃ]=ǥ=Ƕ.Count;Ƕ.Add(new HashSet<
IMyCubeGrid>{Ȃ});Ƿ.Add(Ȃ.CustomName);}if(!ǵ.TryGetValue(Ǵ,out Ǧ)){ǵ[Ǵ]=Ǧ=Ƕ.Count;Ƕ.Add(new HashSet<IMyCubeGrid>{Ǵ});Ƿ.Add(Ǵ.
CustomName);}((ǽ.TryGetValue(ǥ,out Ǿ)?Ǿ:(ǽ[ǥ]=new Dictionary<int,List<string>>())).TryGetValue(Ǧ,out ǿ)?ǿ:(ǽ[ǥ][Ǧ]=new List<string
>())).Add(à.CustomName);((ǽ.TryGetValue(Ǧ,out Ǿ)?Ǿ:(ǽ[Ǧ]=new Dictionary<int,List<string>>())).TryGetValue(ǥ,out ǿ)?ǿ:(ǽ[Ǧ
][ǥ]=new List<string>())).Add(ȁ.CustomName);}}ǃ.Clear();ǃ.Add(Me.CubeGrid);if(!ǵ.TryGetValue(Me.CubeGrid,out ǥ))return;Ȁ.
Add(ǥ);ǃ.UnionWith(Ƕ[ǥ]);ȉ.Enqueue(ǥ);while(ȉ.Count>0){ǥ=ȉ.Dequeue();if(!ǽ.TryGetValue(ǥ,out Ǿ))continue;foreach(int ȑ in Ǿ
.Keys){if(Ȁ.Add(ȑ)){ǃ.UnionWith(Ƕ[ȑ]);ȉ.Enqueue(ȑ);Ÿ.Add(Ƿ[ȑ]+" docked to "+Ƿ[ǥ]+" at "+String.Join(", ",Ǿ[ȑ]));}}}}void
Ȓ(){List<IMyBlockGroup>ȓ=new List<IMyBlockGroup>();List<IMyTerminalBlock>Ť=new List<IMyTerminalBlock>();System.Text.
RegularExpressions.Match ǳ;GridTerminalSystem.GetBlockGroups(ȓ);foreach(IMyBlockGroup Ȕ in ȓ){if((ǳ=ƾ.Match(Ȕ.Name)).Success){Ȕ.GetBlocks(
Ť);foreach(IMyTerminalBlock à in Ť)ź[à]=ǳ;}}}void ȕ<Ȗ>()where Ȗ:class{List<IMyTerminalBlock>Ť=new List<IMyTerminalBlock>(
);System.Text.RegularExpressions.Match ǳ;int Í,ò,ȗ;IMyInventory Þ;List<IMyInventoryItem>Y;string Ä,É;Ź I;long G,Ǚ;
GridTerminalSystem.GetBlocksOfType<Ȗ>(Ť);foreach(IMyTerminalBlock à in Ť){if(!ǃ.Contains(à.CubeGrid))continue;ǳ=ƾ.Match(à.CustomName);if(ǳ
.Success){ź.Remove(à);Ƃ[à]=ǳ;}else if(ź.TryGetValue(à,out ǳ)){Ƃ[à]=ǳ;}if((à is IMySmallMissileLauncher&!(à is
IMySmallMissileLauncherReload|à.BlockDefinition.SubtypeName=="LargeMissileLauncher"))|à is IMyLargeInteriorTurret){Ż.Add(à.GetInventory(0));}else if(
(à is IMyFunctionalBlock)&&((à as IMyFunctionalBlock).Enabled&à.IsFunctional)){if((à is IMyRefinery|à is IMyReactor|à is
IMyGasGenerator)&!Ƃ.ContainsKey(à)){Ż.Add(à.GetInventory(0));}else if(à is IMyAssembler&&!(à as IMyAssembler).IsQueueEmpty){Ż.Add(à.
GetInventory(((à as IMyAssembler).Mode==MyAssemblerMode.Disassembly)?1:0));}}Í=à.InventoryCount;while(Í-->0){Þ=à.GetInventory(Í);Y=Þ
.GetItems();ò=Y.Count;while(ò-->0){Ä=""+Y[ò].Content.TypeId;Ä=Ä.Substring(Ä.LastIndexOf('_')+1);É=Y[ò].Content.SubtypeId.
ToString();Ź.ġ(Ä,É,0L,0.0f,Y[ò].Content.SubtypeId.ToString(),null);Ä=Ä.ToUpper();É=É.ToUpper();G=(long)((double)Y[ò].Amount*1e6)
;Ǉ[Ä]+=G;I=Ɩ[Ä][É];I.G+=G;I.Ă+=G;I.Ŭ.TryGetValue(Þ,out Ǚ);I.Ŭ[Þ]=Ǚ+G;I.ŭ.TryGetValue(Þ,out ȗ);I.ŭ[Þ]=Math.Max(ȗ,ò+1);}}}}
void Ȋ(){string Ä,É;long G;Ź I;foreach(IMyInventory Þ in ż){foreach(IMyInventoryItem ȋ in Þ.GetItems()){Ä=""+ȋ.Content.
TypeId;Ä=Ä.Substring(Ä.LastIndexOf('_')+1).ToUpper();É=ȋ.Content.SubtypeId.ToString().ToUpper();G=(long)((double)ȋ.Amount*1e6)
;Ǉ[Ä]-=G;Ɩ[Ä][É].G-=G;}}foreach(IMyInventory Þ in Ż){foreach(IMyInventoryItem ȋ in Þ.GetItems()){Ä=""+ȋ.Content.TypeId;Ä=
Ä.Substring(Ä.LastIndexOf('_')+1).ToUpper();É=ȋ.Content.SubtypeId.ToString().ToUpper();G=(long)((double)ȋ.Amount*1e6);I=Ɩ
[Ä][É];I.Ă-=G;I.ý+=G;}}}void Ȍ(){StringBuilder ȍ=new StringBuilder();IMyTextPanel Ȏ;IMyRefinery ȏ;IMyAssembler Ȑ;System.
Text.RegularExpressions.Match ǳ;int Í,V,ǝ,Ǟ;string[]ǟ,æ;string Ǡ,Ä,É;long G;float é;bool ǡ,ê,Ǣ=false;foreach(
IMyTerminalBlock à in Ƃ.Keys){ǳ=Ƃ[à];ǟ=ǳ.Groups[1].Captures[0].Value.Split(ƞ,ƚ);ȍ.Clear();if(!(ǡ=ź.ContainsKey(à))){ȍ.Append(à.
CustomName,0,ǳ.Index);ȍ.Append(ƻ);if(ƽ!="")ȍ.Append(ƽ+" ");}if((Ȏ=(à as IMyTextPanel))!=null){foreach(string Ô in ǟ){Ǡ=Ô.ToUpper()
;if(Ǒ<1005903&(Í=Ǡ.IndexOf(":P"))>0&Ɩ.ContainsKey(Ǡ.Substring(0,Math.Min(Ǡ.Length,Math.Max(0,Í))))){Ǡ="QUOTA:"+Ǡ;}else if
(Ǒ<1005903&Ɩ.ContainsKey(Ǡ)){Ǡ="INVEN:"+Ǡ;}æ=Ǡ.Split(Ɯ);Ǡ=æ[0];if(Ǡ.Length>=4&"STATUS".StartsWith(Ǡ)){if(Ȏ.Enabled)ŵ.Add(
Ȏ);ȍ.Append("STATUS ");}else if(Ǡ.Length>=5&"DEBUGGING".StartsWith(Ǡ)){if(Ȏ.Enabled)Ŷ.Add(Ȏ);ȍ.Append("DEBUG ");}else if(
Ǡ=="SPAN"){if(æ.Length>=3&&(int.TryParse(æ[1],out ǝ)&int.TryParse(æ[2],out Ǟ)&ǝ>=1&Ǟ>=1)){Ɓ[Ȏ]=new Ƣ(ǝ,Ǟ);ȍ.Append(
"SPAN:"+ǝ+":"+Ǟ+" ");}else{ȍ.Append((Ǡ=String.Join(":",æ).ToLower())+" ");Ÿ.Add("Invalid panel span rule: "+Ǡ);}}else if(Ǡ==
"THE"){Ǣ=true;}else if(Ǡ=="ENCHANTER"&Ǣ){Ǣ=false;Ȏ.SetValueFloat("FontSize",0.2f);Ȏ.WritePublicTitle("TIM the Enchanter",
false);Ȏ.WritePublicText(ƿ,false);Ȏ.ShowPublicTextOnScreen();ȍ.Append("THE ENCHANTER ");}else if(Ǡ.Length>=3&"QUOTAS".
StartsWith(Ǡ)){if(Ȏ.Enabled&!Ų.ContainsKey(Ȏ))Ų[Ȏ]=0;if(Ȏ.Enabled&!ų.ContainsKey(Ȏ))ų[Ȏ]=new List<string>();ȍ.Append("QUOTA");Í=0;
while(++Í<æ.Length){if(Ǫ(null,true,æ[Í],"",out Ä,out É)&Ä!="ORE"&É==""){if(Ȏ.Enabled)ų[Ȏ].Add(Ä);ȍ.Append(":"+ǅ[Ä]);}else if(
æ[Í].StartsWith("P")&int.TryParse(æ[Í].Substring(Math.Min(1,æ[Í].Length)),out V)){if(Ȏ.Enabled)Ų[Ȏ]=Math.Max(0,V);if(V>0)
ȍ.Append(":P"+V);}else{ȍ.Append(":"+æ[Í].ToLower());Ÿ.Add("Invalid quota panel rule: "+æ[Í].ToLower());}}ȍ.Append(" ");}
else if(Ǡ.Length>=3&"INVENTORY".StartsWith(Ǡ)){if(Ȏ.Enabled&!Ŵ.ContainsKey(Ȏ))Ŵ[Ȏ]=new List<string>();ȍ.Append("INVEN");Í=0;
while(++Í<æ.Length){if(Ǫ(null,true,æ[Í],"",out Ä,out É)&É==""){if(Ȏ.Enabled)Ŵ[Ȏ].Add(Ä);ȍ.Append(":"+ǅ[Ä]);}else{ȍ.Append(":"
+æ[Í].ToLower());Ÿ.Add("Invalid inventory panel rule: "+æ[Í].ToLower());}}ȍ.Append(" ");}else{ȍ.Append((Ǡ=String.Join(":"
,æ).ToLower())+" ");Ÿ.Add("Invalid panel attribute: "+Ǡ);}}}else{ȏ=(à as IMyRefinery);Ȑ=(à as IMyAssembler);foreach(
string Ô in ǟ){Ǡ=Ô.ToUpper();if(Ǒ<1005900&((ȏ!=null&Ǡ=="ORE")|(Ȑ!=null&Ɩ["COMPONENT"].ContainsKey(Ǡ)))){Ǡ="AUTO";}æ=Ǡ.Split(Ɯ)
;Ǡ=æ[0];if((Ǡ.Length>=4&"LOCKED".StartsWith(Ǡ))|Ǡ=="EXEMPT"){Í=à.InventoryCount;while(Í-->0)Ż.Add(à.GetInventory(Í));ȍ.
Append(Ǡ+" ");}else if(Ǡ=="HIDDEN"){Í=à.InventoryCount;while(Í-->0)ż.Add(à.GetInventory(Í));ȍ.Append("HIDDEN ");}else if((à is
IMyShipConnector)&Ǡ=="DOCK"){ȍ.Append(String.Join(":",æ)+" ");}else if((ȏ!=null|Ȑ!=null)&Ǡ=="AUTO"){ȍ.Append("AUTO");HashSet<string>W,ǣ=
(ȏ==null|æ.Length>1)?(new HashSet<string>()):ȃ(ȏ,"ORE");HashSet<Ǎ>L,ǜ=new HashSet<Ǎ>();Í=0;while(++Í<æ.Length){if(Ǫ(null,
true,æ[Í],(ȏ!=null)?"ORE":"",out Ä,out É)&(ȏ!=null)==(Ä=="ORE")&(ȏ!=null|Ä!="INGOT")){if(É==""){if(ȏ!=null){ǣ.UnionWith(ǆ[Ä]
);}else{foreach(string ò in ǆ[Ä])ǜ.Add(new Ǎ(Ä,ò));}ȍ.Append(":"+ǅ[Ä]);}else{if(ȏ!=null){ǣ.Add(É);}else{ǜ.Add(new Ǎ(Ä,É))
;}ȍ.Append(":"+((ȏ==null&ǋ[É].Count>1)?(ǅ[Ä]+"/"):"")+ǉ[É]);}}else{ȍ.Append(":"+æ[Í].ToLower());Ÿ.Add(
"Unrecognized or ambiguous item: "+æ[Í].ToLower());}}if(ȏ!=null){if(ȏ.Enabled)(Ž.TryGetValue(ȏ,out W)?W:(Ž[ȏ]=new HashSet<string>())).UnionWith(ǣ);}else{
if(Ǒ<1005900){Ȑ.ClearQueue();Ȑ.Repeating=false;Ȑ.Enabled=true;}if(Ȑ.Enabled)(ž.TryGetValue(Ȑ,out L)?L:(ž[Ȑ]=new HashSet<Ǎ>
())).UnionWith(ǜ);}ȍ.Append(" ");}else if(!å(à,æ,"",out Ä,out É,out V,out G,out é,out ê)){ȍ.Append((Ǡ=String.Join(":",æ).
ToLower())+" ");Ÿ.Add("Unrecognized or ambiguous item: "+Ǡ);}else if(!à.HasInventory|(à is IMySmallMissileLauncher&!(à is
IMySmallMissileLauncherReload|à.BlockDefinition.SubtypeName=="LargeMissileLauncher"))|à is IMyLargeInteriorTurret){ȍ.Append(String.Join(":",æ).
ToLower()+" ");Ÿ.Add("Cannot sort items to "+à.CustomName+": no conveyor-connected inventory");}else{if(É==""){foreach(string ò
in(ê?(IEnumerable<string>)ǆ[Ä]:(IEnumerable<string>)ȃ(à,Ä)))è(à,0,Ä,ò,V,G);}else{è(à,0,Ä,É,V,G);}if(ƺ&!ǡ){if(ê){ȍ.Append(
"FORCE:"+ǅ[Ä]);if(É!="")ȍ.Append("/"+ǉ[É]);}else if(É==""){ȍ.Append(ǅ[Ä]);}else if(ǋ[É].Count==1||Ȅ(à,É)==Ä){ȍ.Append(ǉ[É]);}
else{ȍ.Append(ǅ[Ä]+"/"+ǉ[É]);}if(V>0&V<int.MaxValue)ȍ.Append(":P"+V);if(G>=0L)ȍ.Append(":"+(G/1e6));ȍ.Append(" ");}}}}if(ƺ&!
ǡ){if(ȍ[ȍ.Length-1]==' ')ȍ.Length--;ȍ.Append(Ƽ).Append(à.CustomName,ǳ.Index+ǳ.Length,à.CustomName.Length-ǳ.Index-ǳ.Length
);à.CustomName=ȍ.ToString();}if(à.GetUserRelationToOwner(Me.OwnerId)!=MyRelationsBetweenPlayerAndBlock.Owner&à.
GetUserRelationToOwner(Me.OwnerId)!=MyRelationsBetweenPlayerAndBlock.FactionShare)Ÿ.Add("Cannot control \""+à.CustomName+
"\" due to differing ownership");}}void ǚ(bool Ǜ){bool D=ŷ.Contains("quotas");int ì,ĵ,ĺ,ŋ,Ō,ň,ŉ,ŀ,Ý,V;long G,ă,Ǚ;float é;bool ê;string Ǭ,Ä,É;string[]ƕ,
ǭ=new string[1]{" "};string[][]ĩ;IMyTextPanel Ǯ;IMySlimBlock ĸ;Matrix ķ=new Matrix();StringBuilder Ĩ=new StringBuilder();
List<string>ǯ=new List<string>(),ǰ=new List<string>(),Ǳ=new List<string>();Dictionary<string,SortedDictionary<string,string[
]>>ǲ=new Dictionary<string,SortedDictionary<string,string[]>>();Ź I;Ŏ º;foreach(Ź È in Ɩ["ORE"].Values)È.Ū=(È.G==0L)?0L:
Math.Max(È.Ū,È.G);foreach(IMyTextPanel Â in Ų.Keys){ŋ=Â.BlockDefinition.SubtypeName.EndsWith("Wide")?2:1;Ō=Â.BlockDefinition
.SubtypeName.StartsWith("Small")?3:1;ň=ŉ=1;if(Ɓ.ContainsKey(Â)){ň=Ɓ[Â].Ô;ŉ=Ɓ[Â].ƣ;}ĩ=new string[ň][];Â.Orientation.
GetMatrix(out ķ);Ĩ.Clear();for(ĺ=0;ĺ<ŉ;ĺ++){ŀ=0;for(ĵ=0;ĵ<ň;ĵ++){ĩ[ĵ]=ǭ;ĸ=Â.CubeGrid.GetCubeBlock(new Vector3I(Â.Position+ĵ*ŋ*Ō*ķ
.Right+ĺ*Ō*ķ.Down));Ǯ=(ĸ!=null)?(ĸ.FatBlock as IMyTextPanel):null;if(Ǯ!=null&&(""+Ǯ.BlockDefinition==""+Â.BlockDefinition
&Ǯ.GetPublicTitle().ToUpper().Contains("QUOTAS"))){ĩ[ĵ]=Ǯ.GetPublicText().Split('\n');ŀ=Math.Max(ŀ,ĩ[ĵ].Length);}}for(ì=0
;ì<ŀ;ì++){for(ĵ=0;ĵ<ň;ĵ++)Ĩ.Append((ì<ĩ[ĵ].Length)?ĩ[ĵ][ì]:" ");Ĩ.Append("\n");}}V=Ų[Â];Ǭ="";ǯ.Clear();ǲ.Clear();ǰ.Clear(
);foreach(string Ļ in Ĩ.ToString().Split('\n')){ƕ=Ļ.ToUpper().Split(ƛ,4,ƚ);if(ƕ.Length<1){}else if(å(null,ƕ,Ǭ,out Ä,out É
,out Ý,out G,out é,out ê)&Ä==Ǭ&Ä!=""&É!=""){I=Ɩ[Ä][É];ǲ[Ä][É]=new string[]{I.ŧ,""+Math.Round(G/1e6,2),""+Math.Round(é*
100.0f,2)+"%"};if((V>0&(V<I.Ű|I.Ű<=0))|(V==0&I.Ű<0)){I.Ű=V;I.Ū=G;I.é=é;}else if(V==I.Ű){I.Ū=Math.Max(I.Ū,G);I.é=Math.Max(I.é,é
);}}else if(å(null,ƕ,"",out Ä,out É,out Ý,out G,out é,out ê)&Ä!=Ǭ&Ä!=""&É==""){if(!ǲ.ContainsKey(Ǭ=Ä)){ǯ.Add(Ǭ);ǲ[Ǭ]=new
SortedDictionary<string,string[]>();}}else if(Ǭ!=""){ǲ[Ǭ][ƕ[0]]=ƕ;}else{ǰ.Add(Ļ);}}º=new Ŏ(4,2);º.ĭ(1,1);º.ĭ(2,1);if(ǯ.Count==0&ų[Â].
Count==0)ų[Â].AddRange(Ǆ);foreach(string ç in ų[Â]){if(!ǲ.ContainsKey(ç)){ǯ.Add(ç);ǲ[ç]=new SortedDictionary<string,string[]>
();}}foreach(string ç in ǯ){if(ç=="ORE")continue;if(º.ī()>0)º.Ī();º.Ĕ(0,ǅ[ç],true);º.Ĕ(1,"  Min",true);º.Ĕ(2,"  Pct",true
);º.Ĕ(3,"",true);º.Ī();foreach(Ź È in Ɩ[ç].Values){if(!ǲ[ç].ContainsKey(È.É))ǲ[ç][È.É]=new string[]{È.ŧ,""+Math.Round(È.Ū
/1e6,2),""+Math.Round(È.é*100.0f,2)+"%"};}foreach(string Ǥ in ǲ[ç].Keys){ƕ=ǲ[ç][Ǥ];º.Ĕ(0,Ɩ[ç].ContainsKey(Ǥ)?ƕ[0]:ƕ[0].
ToLower(),true);º.Ĕ(1,(ƕ.Length>1)?ƕ[1]:"",true);º.Ĕ(2,(ƕ.Length>2)?ƕ[2]:"",true);º.Ĕ(3,(ƕ.Length>3)?ƕ[3]:"",true);}}Ń(
"TIM Quotas",º,Â,true,((ǰ.Count==0)?"":(String.Join("\n",ǰ).Trim().ToLower()+"\n\n")),"");}foreach(string ç in Ǆ){ă=1L;if(!Ƨ.
Contains(ç))ă=1000000L;Ǚ=Ǉ[ç];if(Ǜ&Ǚ>0L){Ǳ.Clear();foreach(Ź È in Ɩ[ç].Values){if(È.é>0.0f&Ǚ>=(long)(È.Ū/È.é))Ǳ.Add(È.É);}if(Ǳ.
Count>0){Ǳ.Sort((string ǥ,string Ǧ)=>{Ź ǧ=Ɩ[ç][ǥ],Ǩ=Ɩ[ç][Ǧ];long ǩ=(long)(ǧ.G/ǧ.é),ǫ=(long)(Ǩ.G/Ǩ.é);return(ǩ==ǫ)?ǧ.é.
CompareTo(Ǩ.é):ǩ.CompareTo(ǫ);});É=Ǳ[(Ǳ.Count-1)/2];I=Ɩ[ç][É];Ǚ=(long)(I.G/I.é+0.5f);if(D){Ÿ.Add("median "+ǅ[ç]+" is "+ǉ[É]+", "+
(Ǚ/1e6)+" -> "+(I.G/1e6/I.é));foreach(string Ǥ in Ǳ){I=Ɩ[ç][Ǥ];Ÿ.Add("  "+ǉ[Ǥ]+" @ "+(I.G/1e6)+" / "+I.é+" => "+(long)(I.
G/1e6/I.é+0.5f));}}}}foreach(Ź È in Ɩ[ç].Values){G=Math.Max(È.ũ,Math.Max(È.Ū,(long)(È.é*Ǚ+0.5f)));È.ũ=(G/ă)*ă;}}}bool Ǫ(
IMyCubeBlock à,bool ê,string ƅ,string ç,out string Ä,out string É){int á,ò,â;string[]ã;Ä="";É="";â=0;ã=ƅ.Trim().Split('/');if(ã.
Length>=2){ã[0]=ã[0].Trim();ã[1]=ã[1].Trim();if(ǆ.ContainsKey(ã[0])&&(ã[1]==""|Ɩ[ã[0]].ContainsKey(ã[1]))){if(ê||Ǝ(à,ã[0],ã[1]
)){â=1;Ä=ã[0];É=ã[1];}}else{á=Ǆ.BinarySearch(ã[0]);á=Math.Max(á,~á);while((â<2&á<Ǆ.Count)&&Ǆ[á].StartsWith(ã[0])){ò=ǆ[Ǆ[á
]].BinarySearch(ã[1]);ò=Math.Max(ò,~ò);while((â<2&ò<ǆ[Ǆ[á]].Count)&&ǆ[Ǆ[á]][ò].StartsWith(ã[1])){if(ê||Ǝ(à,Ǆ[á],ǆ[Ǆ[á]][ò
])){â++;Ä=Ǆ[á];É=ǆ[Ǆ[á]][ò];}ò++;}if(â==0&Ǆ[á]=="INGOT"&"GRAVEL".StartsWith(ã[1])&(ê||Ǝ(à,"INGOT","STONE"))){â++;Ä=
"INGOT";É="STONE";}á++;}}}else if(ǆ.ContainsKey(ã[0])){if(ê||Ǝ(à,ã[0],"")){â++;Ä=ã[0];É="";}}else if(ǋ.ContainsKey(ã[0])){if(ç
!=""&&Ɩ[ç].ContainsKey(ã[0])){â++;Ä=ç;É=ã[0];}else{á=ǋ[ã[0]].Count;while(â<2&á-->0){if(ê||Ǝ(à,ǋ[ã[0]][á],ã[0])){â++;Ä=ǋ[ã[
0]][á];É=ã[0];}}}}else if(ç!=""){ò=ǆ[ç].BinarySearch(ã[0]);ò=Math.Max(ò,~ò);while((â<2&ò<ǆ[ç].Count)&&ǆ[ç][ò].StartsWith(
ã[0])){â++;Ä=ç;É=ǆ[ç][ò];ò++;}if(â==0&ç=="INGOT"&"GRAVEL".StartsWith(ã[0])){â++;Ä="INGOT";É="STONE";}}else{á=Ǆ.
BinarySearch(ã[0]);á=Math.Max(á,~á);while((â<2&á<Ǆ.Count)&&Ǆ[á].StartsWith(ã[0])){if(ê||Ǝ(à,Ǆ[á],"")){â++;Ä=Ǆ[á];É="";}á++;}ò=ǈ.
BinarySearch(ã[0]);ò=Math.Max(ò,~ò);while((â<2&ò<ǈ.Count)&&ǈ[ò].StartsWith(ã[0])){á=ǋ[ǈ[ò]].Count;while(â<2&á-->0){if(ê||Ǝ(à,ǋ[ǈ[ò]]
[á],ǈ[ò])){if(â!=1||(Ä!=ǋ[ǈ[ò]][á]|É!=""|ǆ[Ä].Count!=1))â++;Ä=ǋ[ǈ[ò]][á];É=ǈ[ò];}}ò++;}if(â==0&"GRAVEL".StartsWith(ã[0])&
(ê||Ǝ(à,"INGOT","STONE"))){â++;Ä="INGOT";É="STONE";}}if(!ê&à!=null&â==1&É==""){HashSet<string>ä=ȃ(à,Ä);if(ä.Count==1)É=ä.
First();}return(â==1);}bool å(IMyCubeBlock à,string[]æ,string ç,out string Ä,out string É,out int V,out long G,out float é,
out bool ê){int ë,ì;double í,î;Ä="";É="";V=0;G=-1L;é=-1.0f;ê=(à==null);ë=0;if(æ[0].Trim()=="FORCE"){if(æ.Length==1)return
false;ê=true;ë=1;}if(!Ǫ(à,ê,æ[ë],ç,out Ä,out É))return false;while(++ë<æ.Length){æ[ë]=æ[ë].Trim();ì=æ[ë].Length;if(ì==0){}
else if(æ[ë]=="IGNORE"){G=0L;}else if(æ[ë]=="OVERRIDE"|æ[ë]=="SPLIT"){}else if(æ[ë][ì-1]=='%'&double.TryParse(æ[ë].Substring
(0,ì-1),out í)){é=Math.Max(0.0f,(float)(í/100.0));}else if(æ[ë][0]=='P'&double.TryParse(æ[ë].Substring(1),out í)){V=Math.
Max(1,(int)(í+0.5));}else{î=1.0;if(æ[ë][ì-1]=='K'){ì--;î=1e3;}else if(æ[ë][ì-1]=='M'){ì--;î=1e6;}if(double.TryParse(æ[ë].
Substring(0,ì),out í))G=Math.Max(0L,(long)(í*î*1e6+0.5));}}return true;}void è(IMyTerminalBlock à,int Ø,string Ä,string É,int V,
long G){long Ô;Dictionary<string,Dictionary<string,Dictionary<IMyInventory,long>>>Õ;Dictionary<string,Dictionary<
IMyInventory,long>>Ö;Dictionary<IMyInventory,long>Ù;if(V==0)V=int.MaxValue;Õ=(Ɣ.TryGetValue(V,out Õ)?Õ:(Ɣ[V]=new Dictionary<string,
Dictionary<string,Dictionary<IMyInventory,long>>>()));Ö=(Õ.TryGetValue(Ä,out Ö)?Ö:(Õ[Ä]=new Dictionary<string,Dictionary<
IMyInventory,long>>()));Ù=(Ö.TryGetValue(É,out Ù)?Ù:(Ö[É]=new Dictionary<IMyInventory,long>()));IMyInventory Þ=à.GetInventory(Ø);Ù.
TryGetValue(Þ,out Ô);Ù[Þ]=G;Ɩ[Ä][É].ũ+=Math.Min(0L,-Ô)+Math.Max(0L,G);if(((à is IMyGasGenerator|à is IMyReactor|à is IMyRefinery|à
is IMyUserControllableGun)&Þ.Owner!=null)&&Þ.Owner.UseConveyorSystem){à.GetActionWithName("UseConveyor").Apply(à);Ÿ.Add(
"Disabling conveyor system for "+à.CustomName);}}void Ú(bool Û){List<int>Ü;Ü=new List<int>(Ɣ.Keys);Ü.Sort();foreach(int Ý in Ü){foreach(string Ä in Ɣ[Ý]
.Keys){foreach(string É in Ɣ[Ý][Ä].Keys)ß(Û,Ý,Ä,É);}}if(!Û){foreach(string Ä in Ǆ){foreach(Ź I in Ɩ[Ä].Values){if(I.Ă>0L)
Ÿ.Add("No place to put "+Ȇ(I.Ă)+" "+ǅ[Ä]+"/"+ǉ[I.É]+", containers may be full");}}}}void ß(bool Û,int V,string Ä,string É
){bool D=ŷ.Contains("sorting");int ý,þ;long ÿ,Ā,ā,Ă,G,ú,ă;List<IMyInventory>Ą=null;Dictionary<IMyInventory,long>ą;if(D)Ÿ.
Add("sorting "+ǅ[Ä]+"/"+ǉ[É]+" lim="+Û+" p="+V);ă=1L;if(!Ƨ.Contains(Ä))ă=1000000L;ą=new Dictionary<IMyInventory,long>();Ź I
=Ɩ[Ä][É];ÿ=0L;foreach(IMyInventory Ć in Ɣ[V][Ä][É].Keys){ā=Ɣ[V][Ä][É][Ć];if(ā!=0L&Û==(ā>=0L)){if(ā<0L){ā=1000000L;if(Ć.
MaxVolume!=VRage.MyFixedPoint.MaxValue)ā=(long)((double)Ć.MaxVolume*1e6);}ą[Ć]=ā;ÿ+=ā;}}if(D)Ÿ.Add("total req="+(ÿ/1e6));if(ÿ<=0L
)return;Ā=I.Ă+I.ý;if(D)Ÿ.Add("total avail="+(Ā/1e6));if(Ā>0L){Ą=new List<IMyInventory>(I.Ŭ.Keys);do{ý=0;þ=0;foreach(
IMyInventory ü in Ą){Ă=I.Ŭ[ü];if(Ă>0L&Ż.Contains(ü)){ý++;ą.TryGetValue(ü,out ā);G=(long)((double)ā/ÿ*Ā);if(Û)G=Math.Min(G,ā);G=(G/ă)
*ă;if(Ă>=G){if(D)Ÿ.Add("locked "+(ü.Owner==null?"???":(ü.Owner as IMyTerminalBlock).CustomName)+" gets "+(G/1e6)+", has "
+(Ă/1e6));þ++;ÿ-=ā;ą[ü]=0L;Ā-=Ă;I.ý-=Ă;I.Ŭ[ü]=0L;}}}}while(ý>þ&þ>0);}foreach(IMyInventory Ć in ą.Keys){ā=ą[Ć];if(ā<=0L|ÿ
<=0L|Ā<=0L){if(Û&ā>0L)Ÿ.Add("Insufficient "+ǅ[Ä]+"/"+ǉ[É]+" to satisfy "+(Ć.Owner==null?"???":(Ć.Owner as IMyTerminalBlock
).CustomName));continue;}G=(long)((double)ā/ÿ*Ā);if(Û)G=Math.Min(G,ā);G=(G/ă)*ă;if(D)Ÿ.Add((Ć.Owner==null?"???":(Ć.Owner
as IMyTerminalBlock).CustomName)+" gets "+(ā/1e6)+" / "+(ÿ/1e6)+" of "+(Ā/1e6)+" = "+(G/1e6));ÿ-=ā;if(I.Ŭ.TryGetValue(Ć,
out Ă)){Ă=Math.Min(Ă,G);G-=Ă;Ā-=Ă;if(Ż.Contains(Ć)){I.ý-=Ă;}else{I.Ă-=Ă;}I.Ŭ[Ć]-=Ă;}ú=0L;foreach(IMyInventory ü in Ą){Ă=
Math.Min(I.Ŭ[ü],G);ú=0L;if(Ă>0L&Ż.Contains(ü)==false){ú=ï(Ä,É,Ă,ü,Ć);G-=ú;Ā-=ú;I.Ă-=ú;I.Ŭ[ü]-=ú;}if(G<=0L|(ú!=0L&ú!=Ă))break
;}if(Û&G>0L){Ÿ.Add("Insufficient "+ǅ[Ä]+"/"+ǉ[É]+" to satisfy "+(Ć.Owner==null?"???":(Ć.Owner as IMyTerminalBlock).
CustomName));continue;}}if(D)Ÿ.Add(""+(Ā/1e6)+" left over");}long ï(string Ä,string É,long G,IMyInventory ð,IMyInventory ñ){bool D
=ŷ.Contains("sorting");List<IMyInventoryItem>Y;int ò;VRage.MyFixedPoint ó,ú;uint ô;string õ,ö;ó=(VRage.MyFixedPoint)(G/
1e6);Y=ð.GetItems();ò=Math.Min(Ɩ[Ä][É].ŭ[ð],Y.Count);while(ó>0&ò-->0){õ=""+Y[ò].Content.TypeId;õ=õ.Substring(õ.LastIndexOf(
'_')+1).ToUpper();ö=Y[ò].Content.SubtypeId.ToString().ToUpper();if(õ==Ä&ö==É){ú=Y[ò].Amount;ô=Y[ò].ItemId;if(ð==ñ){ó-=ú;if(
ó<0)ó=0;}else if(ð.TransferItemTo(ñ,ò,null,true,ó)){Y=ð.GetItems();if(ò<Y.Count&&Y[ò].ItemId==ô)ú-=Y[ò].Amount;if(ú<=0){
if((double)ñ.CurrentVolume<(double)ñ.MaxVolume/2&ñ.Owner!=null){var ø=(ñ.Owner as IMyCubeBlock).BlockDefinition;Ɔ(ø.
TypeIdString,ø.SubtypeName,Ä,É);}ò=0;}else{ǖ++;if(D)Ÿ.Add("Transferred "+Ȇ((long)((double)ú*1e6))+" "+ǅ[Ä]+"/"+ǉ[É]+" from "+(ð.
Owner==null?"???":(ð.Owner as IMyTerminalBlock).CustomName)+" to "+(ñ.Owner==null?"???":(ñ.Owner as IMyTerminalBlock).
CustomName));}ó-=ú;}else if(!ð.IsConnectedTo(ñ)&ð.Owner!=null&ñ.Owner!=null){if(!ƃ.ContainsKey(ð.Owner as IMyTerminalBlock))ƃ[ð.
Owner as IMyTerminalBlock]=new HashSet<IMyTerminalBlock>();ƃ[ð.Owner as IMyTerminalBlock].Add(ñ.Owner as IMyTerminalBlock);ò=
0;}}}return G-(long)((double)ó*1e6+0.5);}void ù(){if(!ǆ.ContainsKey("ORE")|!ǆ.ContainsKey("INGOT"))return;bool D=ŷ.
Contains("refineries");string Ä,û,É,Ó,Ñ;Ź I;int F,V;List<string>W=new List<string>();Dictionary<string,int>X=new Dictionary<
string,int>();List<IMyInventoryItem>Y;double P,A;ǎ R;bool e;List<IMyRefinery>g=new List<IMyRefinery>();if(D)Ÿ.Add(
"Refinery management:");foreach(string B in ǆ["ORE"]){if(!ƨ.TryGetValue(B,out Ñ))Ñ=B;if(Ñ!=""&Ɩ["ORE"][B].Ă>0L&Ɩ["INGOT"].TryGetValue(Ñ,out I)
){if(I.ũ>0L){F=(int)(100L*I.G/I.ũ);W.Add(B);X[B]=F;if(D)Ÿ.Add("  "+ǉ[Ñ]+" @ "+(I.G/1e6)+"/"+(I.ũ/1e6)+","+((B==Ñ)?"":(
" Ore/"+ǉ[B]))+" L="+F+"%");}}}foreach(IMyRefinery H in Ž.Keys){Ä=û=É=Ó="";Y=H.GetInventory(0).GetItems();if(Y.Count>0){Ä=""+Y[
0].Content.TypeId;Ä=Ä.Substring(Ä.LastIndexOf('_')+1).ToUpper();É=Y[0].Content.SubtypeId.ToString().ToUpper();if(Ä=="ORE"
&X.ContainsKey(É))X[É]+=Math.Max(1,X[É]/Ž.Count);if(Y.Count>1){û=""+Y[1].Content.TypeId;û=û.Substring(û.LastIndexOf('_')+
1).ToUpper();Ó=Y[1].Content.SubtypeId.ToString().ToUpper();if(û=="ORE"&X.ContainsKey(Ó))X[Ó]+=Math.Max(1,X[Ó]/Ž.Count);è(
H,0,û,Ó,-2,(long)((double)Y[1].Amount*1e6+0.5));}}if(ſ.TryGetValue(H,out R)){I=Ɩ[R.J.Ä][R.J.É];A=(I.ů.TryGetValue(""+H.
BlockDefinition,out A)?A:1.0);P=((R.J.É==É)?Math.Max(R.Ǐ-(double)Y[0].Amount,0.0):Math.Max(R.Ǐ,A));P=Math.Min(Math.Max((P+A)/2.0,0.2),
10000.0);I.ů[""+H.BlockDefinition]=P;if(D&(int)(A+0.5)!=(int)(P+0.5))Ÿ.Add("  Update "+H.BlockDefinition.SubtypeName+":"+ǉ[R.J.
É]+" refine speed: "+((int)(A+0.5))+" -> "+((int)(P+0.5))+"kg/cycle");}if(Ž[H].Count>0)Ž[H].IntersectWith(X.Keys);else Ž[
H].UnionWith(X.Keys);e=(Ž[H].Count>0);if(Y.Count>0){P=(Ä=="ORE"?(Ɩ["ORE"][É].ů.TryGetValue(""+H.BlockDefinition,out P)?P:
1.0):1e6);è(H,0,Ä,É,-1,(long)Math.Min((double)Y[0].Amount*1e6+0.5,10*P*1e6+0.5));e=(e&Ä=="ORE"&(double)Y[0].Amount<2.5*P&Y.
Count==1);}if(e)g.Add(H);if(D)Ÿ.Add("  "+H.CustomName+((Y.Count<1)?" idle":(" refining "+(int)Y[0].Amount+"kg "+((É=="")?
"unknown":(ǉ[É]+(!X.ContainsKey(É)?"":(" (L="+X[É]+"%)"))))+((Y.Count<2)?"":(", then "+(int)Y[1].Amount+"kg "+((Ó=="")?"unknown":
(ǉ[Ó]+(!X.ContainsKey(Ó)?"":(" (L="+X[Ó]+"%)"))))))))+"; "+((X.Count==0)?"nothing to do":(e?"ready":((Ž[H].Count==0)?
"restricted":"busy"))));}if(W.Count>0&g.Count>0){W.Sort((string h,string k)=>{string o,u;if(!ƨ.TryGetValue(h,out o))o=h;if(!ƨ.
TryGetValue(k,out u))u=k;return-1*Ɩ["INGOT"][o].ũ.CompareTo(Ɩ["INGOT"][u].ũ);});g.Sort((IMyRefinery Z,IMyRefinery S)=>Ž[Z].Count.
CompareTo(Ž[S].Count));foreach(IMyRefinery H in g){É="";F=int.MaxValue;foreach(string B in W){if((É==""|X[B]<F)&Ž[H].Contains(B))
{É=B;F=X[É];}}if(É!=""){ǘ++;H.UseConveyorSystem=false;V=H.GetInventory(0).IsItemAt(0)?-4:-3;P=(Ɩ["ORE"][É].ů.TryGetValue(
""+H.BlockDefinition,out P)?P:1.0);è(H,0,"ORE",É,V,(long)(5*P*1e6+0.5));X[É]+=Math.Min(Math.Max((int)(X[É]*0.41),1),(100/Ž
.Count));if(D)Ÿ.Add("  "+H.CustomName+" assigned "+((int)(5*P+0.5))+"kg "+ǉ[É]+" (L="+X[É]+"%)");}else if(D)Ÿ.Add("  "+H.
CustomName+" unassigned, nothing to do");}}for(V=-1;V>=-4;V--){if(Ɣ.ContainsKey(V)){foreach(string B in Ɣ[V]["ORE"].Keys)ß(true,V,
"ORE",B);}}}void C(){if(!ǆ.ContainsKey("INGOT"))return;bool D=ŷ.Contains("assemblers");long E;int F,G;Ź I,Q;Ǎ J,K;List<Ǎ>L;
Dictionary<Ǎ,int>M=new Dictionary<Ǎ,int>(),N=new Dictionary<Ǎ,int>();List<MyProductionItem>O=new List<MyProductionItem>();double P
,A;ǎ R;bool e,Æ;List<IMyAssembler>Ç=new List<IMyAssembler>();if(D)Ÿ.Add("Assembler management:");Ǉ.TryGetValue(
"COMPONENT",out E);G=90+(int)(10*Ɩ["INGOT"].Values.Min(È=>(È.É!="URANIUM"&(È.Ū>0L|È.é>0.0f))?(È.G/Math.Max((double)È.Ū,17.5*È.é*E))
:2.0));if(D)Ÿ.Add("  Component par L="+G+"%");foreach(string Ä in Ǆ){if(Ä!="ORE"&Ä!="INGOT"){foreach(string É in ǆ[Ä]){I=
Ɩ[Ä][É];I.ū=Math.Max(0,I.ū-1);J=new Ǎ(Ä,É);N[J]=((Ä=="COMPONENT"&I.é>0.0f)?G:100);F=(int)(100L*I.G/Math.Max(1L,I.ũ));if(I
.ũ>0L&F<N[J]&I.Ũ!=default(MyDefinitionId)){if(I.ū==0)M[J]=F;if(D)Ÿ.Add("  "+ǅ[Ä]+"/"+ǉ[É]+((I.ū>0)?"":(" @ "+(I.G/1e6)+
"/"+(I.ũ/1e6)+", L="+F+"%"))+((I.ū>0|I.Æ>0)?("; HOLD "+I.ū+"/"+(10*I.Æ)):""));}}}}foreach(IMyAssembler Ê in ž.Keys){e=Æ=
false;I=Q=null;J=K=new Ǎ("","");if(!Ê.IsQueueEmpty){Ê.GetQueue(O);I=(ű.TryGetValue(O[0].BlueprintId,out J)?Ɩ[J.Ä][J.É]:null);
if(I!=null&M.ContainsKey(J))M[J]+=Math.Max(1,(int)(1e8*(double)O[0].Amount/I.ũ+0.5));if(O.Count>1&&(ű.TryGetValue(O[1].
BlueprintId,out K)&M.ContainsKey(K)))M[K]+=Math.Max(1,(int)(1e8*(double)O[1].Amount/Ɩ[K.Ä][K.É].ũ+0.5));}if(ſ.TryGetValue(Ê,out R))
{Q=Ɩ[R.J.Ä][R.J.É];A=(Q.ů.TryGetValue(""+Ê.BlockDefinition,out A)?A:1.0);if(R.J.Ä!=J.Ä|R.J.É!=J.É){P=Math.Max(A,(Ê.
IsQueueEmpty?2:1)*R.Ǐ);ƀ.Remove(Ê);}else if(Ê.IsProducing){P=R.Ǐ-(double)O[0].Amount+Ê.CurrentProgress;ƀ.Remove(Ê);}else{P=Math.Max(
A,R.Ǐ-(double)O[0].Amount+Ê.CurrentProgress);if((ƀ[Ê]=(ƀ.TryGetValue(Ê,out F)?F:0)+1)>=3){Ÿ.Add("  "+Ê.CustomName+
" is jammed by "+ǉ[J.É]);ƀ.Remove(Ê);Ê.ClearQueue();Q.ū=10*((Q.Æ<1|Q.ū<1)?(Q.Æ=Math.Min(10,Q.Æ+1)):Q.Æ);Æ=true;}}P=Math.Min(Math.Max((P+
A)/2.0,Math.Max(0.2,0.5*A)),Math.Min(1000.0,2.0*A));Q.ů[""+Ê.BlockDefinition]=P;if(D&(int)(A+0.5)!=(int)(P+0.5))Ÿ.Add(
"  Update "+Ê.BlockDefinition.SubtypeName+":"+ǅ[R.J.Ä]+"/"+ǉ[R.J.É]+" assemble speed: "+((int)(A*100)/100.0)+" -> "+((int)(P*100)/
100.0)+"/cycle");}if(ž[Ê].Count==0)ž[Ê].UnionWith(M.Keys);else ž[Ê].IntersectWith(M.Keys);P=((I!=null&&I.ů.TryGetValue(""+Ê.
BlockDefinition,out P))?P:1.0);if(!Æ&(Ê.IsQueueEmpty||(((double)O[0].Amount-Ê.CurrentProgress)<2.5*P&O.Count==1&Ê.Mode==MyAssemblerMode
.Assembly))){if(Q!=null)Q.Æ=Math.Max(0,Q.Æ-((Q.ū<1)?1:0));if(e=(ž[Ê].Count>0))Ç.Add(Ê);}if(D)Ÿ.Add("  "+Ê.CustomName+(Ê.
IsQueueEmpty?" idle":(((Ê.Mode==MyAssemblerMode.Assembly)?" making ":" breaking ")+O[0].Amount+"x "+((J.Ä=="")?"unknown":(ǉ[J.É]+(!M
.ContainsKey(J)?"":(" (L="+M[J]+"%)"))))+((O.Count<=1)?"":(", then "+O[1].Amount+"x "+((K.Ä=="")?"unknown":(ǉ[K.É]+(!M.
ContainsKey(K)?"":(" (L="+M[K]+"%)"))))))))+"; "+((M.Count==0)?"nothing to do":(e?"ready":((ž[Ê].Count==0)?"restricted":"busy"))));
}if(M.Count>0&Ç.Count>0){L=new List<Ǎ>(M.Keys);L.Sort((o,u)=>-1*Ɩ[o.Ä][o.É].ũ.CompareTo(Ɩ[u.Ä][u.É].ũ));Ç.Sort((
IMyAssembler Ë,IMyAssembler Ì)=>ž[Ë].Count.CompareTo(ž[Ì].Count));foreach(IMyAssembler Ê in Ç){J=new Ǎ("","");F=int.MaxValue;foreach
(Ǎ Í in L){if(M[Í]<Math.Min(F,N[Í])&ž[Ê].Contains(Í)&Ɩ[Í.Ä][Í.É].ū<1){J=Í;F=M[Í];}}if(J.Ä!=""){ǌ++;Ê.UseConveyorSystem=
true;Ê.CooperativeMode=false;Ê.Repeating=false;Ê.Mode=MyAssemblerMode.Assembly;I=Ɩ[J.Ä][J.É];P=(I.ů.TryGetValue(""+Ê.
BlockDefinition,out P)?P:1.0);G=Math.Max((int)(5*P),1);Ê.AddQueueItem(I.Ũ,(double)G);M[J]+=(int)Math.Ceiling(1e8*(double)G/I.ũ);if(D)Ÿ.
Add("  "+Ê.CustomName+" assigned "+G+"x "+ǉ[J.É]+" (L="+M[J]+"%)");}else if(D)Ÿ.Add("  "+Ê.CustomName+
" unassigned, nothing to do");}}}void Î(){List<IMyTerminalBlock>Ï=new List<IMyTerminalBlock>(),Ð=new List<IMyTerminalBlock>();List<IMyInventoryItem>
Y;string Ä,É,Ò;List<MyProductionItem>O=new List<MyProductionItem>();Ǎ J;ſ.Clear();GridTerminalSystem.GetBlocksOfType<
IMyGasGenerator>(Ï,U=>ǃ.Contains(U.CubeGrid));GridTerminalSystem.GetBlocksOfType<IMyRefinery>(Ð,U=>ǃ.Contains(U.CubeGrid));foreach(
IMyFunctionalBlock U in Ï.Concat(Ð)){Y=U.GetInventory(0).GetItems();if(Y.Count>0&U.Enabled){Ä=""+Y[0].Content.TypeId;Ä=Ä.Substring(Ä.
LastIndexOf('_')+1).ToUpper();É=Y[0].Content.SubtypeId.ToString().ToUpper();if(ǆ.ContainsKey(Ä)&ǋ.ContainsKey(É))Ɩ[Ä][É].Ů.Add(U);
if(Ä=="ORE"&(ƨ.TryGetValue(É,out Ò)?Ò:(Ò=É))!=""&Ɩ["INGOT"].ContainsKey(Ò))Ɩ["INGOT"][Ò].Ů.Add(U);ſ[U]=new ǎ(new Ǎ(Ä,É),(
double)Y[0].Amount);}}GridTerminalSystem.GetBlocksOfType<IMyAssembler>(Ï,U=>ǃ.Contains(U.CubeGrid));foreach(IMyAssembler U in
Ï){if(U.Enabled&!U.IsQueueEmpty&U.Mode==MyAssemblerMode.Assembly){U.GetQueue(O);if(ű.TryGetValue(O[0].BlueprintId,out J))
{if(ǆ.ContainsKey(J.Ä)&ǋ.ContainsKey(J.É))Ɩ[J.Ä][J.É].Ů.Add(U);ſ[U]=new ǎ(J,(double)O[0].Amount-U.CurrentProgress);}}}}
void v(){string z,ª,µ;Dictionary<string,List<IMyTextPanel>>Å=new Dictionary<string,List<IMyTextPanel>>();Ŏ º;long À,Á;
foreach(IMyTextPanel Â in Ŵ.Keys){z=String.Join("/",Ŵ[Â]);if(Å.ContainsKey(z))Å[z].Add(Â);else Å[z]=new List<IMyTextPanel>(){Â}
;}foreach(List<IMyTextPanel>Ã in Å.Values){º=new Ŏ(6);º.İ(0);º.Į(0,1);º.ĭ(2,1);º.ĭ(3,1);º.ĭ(4,1);º.ĭ(5,1);À=Á=0L;foreach(
string Ä in((Ŵ[Ã[0]].Count>0)?Ŵ[Ã[0]]:Ǆ)){ª=" Asm ";µ="Quota";if(Ä=="INGOT"){ª=" Ref ";}else if(Ä=="ORE"){ª=" Ref ";µ="Max";}
if(º.ī()>0)º.Ī();º.Ĕ(0,"");º.Ĕ(1,ǅ[Ä],true);º.Ĕ(2,ª,true);º.Ĕ(3,"Qty",true);º.Ĕ(4," / ",true);º.Ĕ(5,µ,true);º.Ī();foreach(
Ź I in Ɩ[Ä].Values){º.Ĕ(0,(I.G==0L)?"0.0":(""+((double)I.G/I.ũ)));º.Ĕ(1,I.ŧ,true);z=((I.Ů.Count>0)?(I.Ů.Count+" "+(I.Ů.
All(U=>(!(U is IMyProductionBlock)||(U as IMyProductionBlock).IsProducing))?" ":"!")):((I.ū>0)?"-  ":""));º.Ĕ(2,z,true);º.Ĕ
(3,(I.G>0L|I.ũ>0L)?Ȇ(I.G):"");º.Ĕ(4,(I.ũ>0L)?" / ":"",true);º.Ĕ(5,(I.ũ>0L)?Ȇ(I.ũ):"");À=Math.Max(À,I.G);Á=Math.Max(Á,I.ũ)
;}}º.ĳ(3,Ŏ.œ("8.88"+((À>=1000000000000L)?" M":((À>=1000000000L)?" K":"")),true));º.ĳ(5,Ŏ.œ("8.88"+((Á>=1000000000000L)?
" M":((Á>=1000000000L)?" K":"")),true));foreach(IMyTextPanel Â in Ã)Ń("TIM Inventory",º,Â,true);}}void ć(){long Ĥ;
StringBuilder Ĩ;if(ŵ.Count>0){Ĩ=new StringBuilder();Ĩ.Append(ǒ);for(Ĥ=Math.Max(1,ǔ-Ǔ.Length+1);Ĥ<=ǔ;Ĥ++)Ĩ.Append(Ǔ[Ĥ%Ǔ.Length]);
foreach(IMyTextPanel Â in ŵ){Â.WritePublicTitle("Script Status",false);if(Ɓ.ContainsKey(Â))Ÿ.Add(
"Status panels cannot be spanned");Â.WritePublicText(Ĩ.ToString(),false);Â.ShowPublicTextOnScreen();}}if(Ŷ.Count>0){foreach(IMyTerminalBlock Ł in ƃ.Keys)
{foreach(IMyTerminalBlock ł in ƃ[Ł])Ÿ.Add("No conveyor connection from "+Ł.CustomName+" to "+ł.CustomName);}foreach(
IMyTextPanel Â in Ŷ){Â.WritePublicTitle("Script Debugging",false);if(Ɓ.ContainsKey(Â))Ÿ.Add("Debug panels cannot be spanned");Â.
WritePublicText(String.Join("\n",Ÿ),false);Â.ShowPublicTextOnScreen();}}ƃ.Clear();}void Ń(string ń,Ŏ º,IMyTextPanel Â,bool Ņ=true,
string ņ="",string Ň=""){int ň,ŉ,Ŋ,ŋ,Ō,ĕ,ŀ;int ĵ,ĺ,Ĥ;float Ķ;string[][]ĩ;string z;Matrix ķ;IMySlimBlock ĸ;IMyTextPanel Ĺ;ŋ=Â.
BlockDefinition.SubtypeName.EndsWith("Wide")?2:1;Ō=Â.BlockDefinition.SubtypeName.StartsWith("Small")?3:1;ň=ŉ=1;if(Ņ&Ɓ.ContainsKey(Â)){ň
=Ɓ[Â].Ô;ŉ=Ɓ[Â].ƣ;}ĵ=º.Ĭ();ĵ=(ĵ/ň)+((ĵ%ň>0)?1:0);ĺ=º.ī();ĺ=(ĺ/ŉ)+((ĺ%ŉ>0)?1:0);ĕ=658*ŋ;Ķ=Â.GetValueFloat("FontSize");if(Ķ<
0.25f)Ķ=1.0f;if(ĵ>0)Ķ=Math.Min(Ķ,Math.Max(0.5f,(float)(ĕ*100/ĵ)/100.0f));if(ĺ>0)Ķ=Math.Min(Ķ,Math.Max(0.5f,(float)(1760/ĺ)/
100.0f));ĕ=(int)((float)ĕ/Ķ);ŀ=(int)(17.6f/Ķ);if(ň>1|ŉ>1){ĩ=º.Ħ(ĕ,ň);ķ=new Matrix();Â.Orientation.GetMatrix(out ķ);for(ĵ=0;ĵ<ň
;ĵ++){Ĥ=0;for(ĺ=0;ĺ<ŉ;ĺ++){ĸ=Â.CubeGrid.GetCubeBlock(new Vector3I(Â.Position+ĵ*ŋ*Ō*ķ.Right+ĺ*Ō*ķ.Down));if(ĸ!=null&&(ĸ.
FatBlock is IMyTextPanel)&&""+ĸ.FatBlock.BlockDefinition==""+Â.BlockDefinition){Ĺ=ĸ.FatBlock as IMyTextPanel;Ŋ=Math.Max(0,ĩ[ĵ].
Length-Ĥ);if(ĺ+1<ŉ)Ŋ=Math.Min(Ŋ,ŀ);z="";if(Ĥ<ĩ[ĵ].Length)z=String.Join("\n",ĩ[ĵ],Ĥ,Ŋ);if(ĵ==0)z+=((ĺ==0)?ņ:(((ĺ+1)==ŉ)?Ň:""));
Ĺ.SetValueFloat("FontSize",Ķ);Ĺ.WritePublicTitle(ń+" ("+(ĵ+1)+","+(ĺ+1)+")",false);Ĺ.WritePublicText(z,false);Ĺ.
ShowPublicTextOnScreen();}Ĥ+=ŀ;}}}else{Â.SetValueFloat("FontSize",Ķ);Â.WritePublicTitle(ń,false);Â.WritePublicText(ņ+º.Ə(ĕ)+Ň,false);Â.
ShowPublicTextOnScreen();}}Program(){int Ŀ;foreach(string Ļ in Me.CustomData.Split(Ɲ,ƚ)){string[]ļ=Ļ.Trim().Split('=');if(ļ[0].Equals(
"TIM_version",ƙ)){if(!int.TryParse(ļ[1],out Ǒ)|Ǒ>Ʈ){Echo("Invalid prior version: "+Ǒ);Ǒ=0;}}}Ŏ.ġ();ǒ=("Taleden's Inventory Manager\n"
+"v"+ƪ+"."+ƫ+"."+Ƭ+" ("+ƭ+")\n\n"+Ŏ.ę("Run",80,out Ŀ,1)+Ŏ.ę("Step",125+Ŀ,out Ŀ,1)+Ŏ.ę("Time",145+Ŀ,out Ŀ,1)+Ŏ.ę("Load",
105+Ŀ,out Ŀ,1)+Ŏ.ę("S",65+Ŀ,out Ŀ,1)+Ŏ.ę("R",65+Ŀ,out Ŀ,1)+Ŏ.ę("A",65+Ŀ,out Ŀ,1)+"\n\n");Ɠ(Ʀ);Ɛ(Ʃ);Echo("Compiled TIM v"+ƪ+
"."+ƫ+"."+Ƭ+" ("+ƭ+")");}void Save(){}void Main(string Ľ){if(ǔ>0&(Ǖ+=Runtime.TimeSinceLastRun.TotalSeconds)<0.5)return;Ǖ=
0.0;DateTime ľ=DateTime.Now;int Í,ĥ,ō,Ŧ,ŗ,Ř;bool ř,Ś,ś,Ŝ,ŝ,Ş,ş;char Š,š;string Ţ,ţ;StringBuilder Ĩ=new StringBuilder();List
<IMyTerminalBlock>Ť;ǔ++;Echo("Taleden's Inventory Manager");Echo("v"+ƪ+"."+ƫ+"."+Ƭ+" ("+ƭ+")");Echo("Last Run: #"+ǔ+
" at "+ľ.ToString("h:mm:ss tt"));if(Ǒ>0&Ǒ<Ʈ)Echo("Upgraded from v"+(Ǒ/1000000)+"."+(Ǒ/1000%1000)+"."+(Ǒ%1000));Ÿ.Clear();ŷ.
Clear();Ŧ=ǖ=ǘ=ǌ=0;ş=true;ř=Ʊ;Š=Ƴ;š=ƴ;Ţ=Ƶ;ō=ư;Ś=ƶ;ś=Ʒ;Ŝ=ƥ;ŝ=Ɨ;Ş=Ʋ;foreach(string ť in Ľ.Split(ƛ,ƚ)){if(ť.Equals("rewrite",ƙ)){
ř=true;Ÿ.Add("Tag rewriting enabled");}else if(ť.Equals("norewrite",ƙ)){ř=false;Ÿ.Add("Tag rewriting disabled");}else if(
ť.StartsWith("tags=",ƙ)){ţ=ť.Substring(5);if(ţ.Length!=2){Echo("Invalid 'tags=' delimiters \""+ţ+
"\": must be exactly two characters");ş=false;}else if(ţ[0]==' '||ţ[1]==' '){Echo("Invalid 'tags=' delimiters \""+ţ+"\": cannot be spaces");ş=false;}else if
(char.ToUpper(ţ[0])==char.ToUpper(ţ[1])){Echo("Invalid 'tags=' delimiters \""+ţ+"\": characters must be different");ş=
false;}else{Š=char.ToUpper(ţ[0]);š=char.ToUpper(ţ[1]);Ÿ.Add("Tags are delimited by \""+Š+"\" and \""+š+"\"");}}else if(ť.
StartsWith("prefix=",ƙ)){Ţ=ť.Substring(7).Trim().ToUpper();if(Ţ==""){Ÿ.Add("Tag prefix disabled");}else{Ÿ.Add("Tag prefix is \""+Ţ
+"\"");}}else if(ť.StartsWith("cycle=",ƙ)){if(int.TryParse(ť.Substring(6),out ō)==false||ō<1){Echo(
"Invalid 'cycle=' length \""+ť.Substring(6)+"\": must be a positive integer");ş=false;}else{ō=Math.Min(Math.Max(ō,1),Ư);if(ō<2){Ÿ.Add(
"Function cycling disabled");}else{Ÿ.Add("Cycle length is "+ō);}}}else if(ť.StartsWith("scan=",ƙ)){ţ=ť.Substring(5);if(ţ.Equals("collectors",ƙ)){Ś=
true;Ÿ.Add("Enabled scanning of Collectors");}else if(ţ.Equals("drills",ƙ)){ś=true;Ÿ.Add("Enabled scanning of Drills");}else
if(ţ.Equals("grinders",ƙ)){Ŝ=true;Ÿ.Add("Enabled scanning of Grinders");}else if(ţ.Equals("welders",ƙ)){ŝ=true;Ÿ.Add(
"Enabled scanning of Welders");}else{Echo("Invalid 'scan=' block type '"+ţ+"': must be 'collectors', 'drills', 'grinders' or 'welders'");ş=false;}}
else if(ť.StartsWith("quota=",ƙ)){ţ=ť.Substring(6);if(ţ.Equals("literal",ƙ)){Ş=false;Ÿ.Add("Disabled stable dynamic quotas")
;}else if(ţ.Equals("stable",ƙ)){Ş=true;Ÿ.Add("Enabled stable dynamic quotas");}else{Echo("Invalid 'quota=' mode '"+ţ+
"': must be 'literal' or 'stable'");ş=false;}}else if(ť.StartsWith("debug=",ƙ)){ţ=ť.Substring(6);if(ţ.Length>=1&"quotas".StartsWith(ţ,ƙ)){ŷ.Add("quotas");
}else if(ţ.Length>=1&"sorting".StartsWith(ţ,ƙ)){ŷ.Add("sorting");}else if(ţ.Length>=1&"refineries".StartsWith(ţ,ƙ)){ŷ.Add
("refineries");}else if(ţ.Length>=1&"assemblers".StartsWith(ţ,ƙ)){ŷ.Add("assemblers");}else{Echo(
"Invalid 'debug=' type '"+ţ+"': must be 'quotas', 'sorting', 'refineries', or 'assemblers'");ş=false;}}else{Echo("Unrecognized argument: "+ť);ş=
false;}}if(ş==false)return;ş=(ƻ!=Š)|(Ƽ!=š)|(ƽ!=Ţ);if((ş|(ƺ!=ř)|(ƹ!=ō))&&(ǁ>0)){ǁ=0;Echo(ţ=
"Options changed; cycle step reset.");Ÿ.Add(ţ);}ƺ=ř;ƻ=Š;Ƽ=š;ƽ=Ţ;ƹ=ō;if(ƾ==null|ş){ţ="\\"+ƻ;if(ƽ!=""){ţ+=" *"+System.Text.RegularExpressions.Regex.Escape(ƽ)+
"(|[ ,]+[^\\"+Ƽ+"]*)";}else{ţ+="([^\\"+Ƽ+"]*)";}ţ+="\\"+Ƽ;ƾ=new System.Text.RegularExpressions.Regex(ţ,System.Text.RegularExpressions
.RegexOptions.IgnoreCase);}if(ǁ==Ŧ++*ƹ/Ư){if(ƹ>1){Echo(ţ="Scanning grid connectors ...");Ÿ.Add(ţ);}Ȉ();}Ť=new List<
IMyTerminalBlock>();GridTerminalSystem.GetBlocksOfType<IMyProgrammableBlock>(Ť,(IMyTerminalBlock U)=>(U==Me)|(ƾ.IsMatch(U.CustomName)&ǃ.
Contains(U.CubeGrid)));Í=Ť.IndexOf(Me);ĥ=Ť.FindIndex(à=>à.IsFunctional&à.IsWorking);ţ=ƻ+ƽ+((Ť.Count>1)?(" #"+(Í+1)):"")+Ƽ;Me.
CustomName=ƾ.IsMatch(Me.CustomName)?ƾ.Replace(Me.CustomName,ţ,1):(Me.CustomName+" "+ţ);if(Í!=ĥ){Echo("TIM #"+(ĥ+1)+
" is on duty. Standing by.");if((""+(Ť[ĥ]as IMyProgrammableBlock).TerminalRunArgument).Trim()!=(""+Me.TerminalRunArgument).Trim())Echo(
"WARNING: Script arguments do not match TIM #"+(ĥ+1)+".");return;}if(ǁ==Ŧ++*ƹ/Ư){if(ƹ>1){Echo(ţ="Scanning inventories ...");Ÿ.Add(ţ);}foreach(string Ä in Ǆ){Ǉ[Ä]=0;
foreach(Ź I in Ɩ[Ä].Values){I.G=0L;I.Ă=0L;I.ý=0L;I.Ŭ.Clear();I.ŭ.Clear();}}Ƃ.Clear();ź.Clear();Ż.Clear();ż.Clear();Ȓ();ȕ<
IMyAssembler>();ȕ<IMyCargoContainer>();if(Ś)ȕ<IMyCollector>();ȕ<IMyGasGenerator>();ȕ<IMyGasTank>();ȕ<IMyReactor>();ȕ<IMyRefinery>();
ȕ<IMyShipConnector>();ȕ<IMyShipController>();if(ś)ȕ<IMyShipDrill>();if(Ŝ)ȕ<IMyShipGrinder>();if(ŝ)ȕ<IMyShipWelder>();ȕ<
IMyTextPanel>();ȕ<IMyUserControllableGun>();if(ǀ){ǀ=false;Ǆ.Sort();foreach(string Ä in Ǆ)ǆ[Ä].Sort();ǈ.Sort();foreach(string É in ǈ)
ǋ[É].Sort();}}if(ǁ==Ŧ++*ƹ/Ư){if(ƹ>1){Echo(ţ="Scanning tags ...");Ÿ.Add(ţ);}foreach(string Ä in Ǆ){foreach(Ź I in Ɩ[Ä].
Values){I.Ű=-1;I.ũ=0L;I.Ů.Clear();}}Ų.Clear();ų.Clear();Ŵ.Clear();Ɣ.Clear();ŵ.Clear();Ŷ.Clear();Ž.Clear();ž.Clear();Ɓ.Clear();
Ȍ();}if(ǁ==Ŧ++*ƹ/Ư){if(ƹ>1){Echo(ţ="Adjusting tallies ...");Ÿ.Add(ţ);}Ȋ();}if(ǁ==Ŧ++*ƹ/Ư){if(ƹ>1){Echo(ţ=
"Scanning quota panels ...");Ÿ.Add(ţ);}ǚ(Ş);}if(ǁ==Ŧ++*ƹ/Ư){if(ƹ>1){Echo(ţ="Processing limited item requests ...");Ÿ.Add(ţ);}Ú(true);}if(ǁ==Ŧ++*ƹ/Ư
){if(ƹ>1){Echo(ţ="Managing refineries ...");Ÿ.Add(ţ);}ù();}if(ǁ==Ŧ++*ƹ/Ư){if(ƹ>1){Echo(ţ=
"Processing remaining item requests ...");Ÿ.Add(ţ);}Ú(false);}if(ǁ==Ŧ++*ƹ/Ư){if(ƹ>1){Echo(ţ="Managing assemblers ...");Ÿ.Add(ţ);}C();}if(ǁ==Ŧ++*ƹ/Ư){if(ƹ>1){
Echo(ţ="Scanning production ...");Ÿ.Add(ţ);}Î();}if(ǁ==Ŧ++*ƹ/Ư){if(ƹ>1){Echo(ţ="Updating inventory panels ...");Ÿ.Add(ţ);}v(
);Me.CustomData="TIM_version="+(Ǒ=Ʈ);}if(Ŧ!=Ư)Ÿ.Add("ERROR: step"+Ŧ+" of "+Ư);ǁ++;ŗ=(int)((DateTime.Now-ľ).
TotalMilliseconds+0.5);Ř=(int)(100.0f*Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount+0.5);Í=0;Ǔ[ǔ%Ǔ.Length]=(Ŏ.ę(""+ǔ,80,out
Í,1)+Ŏ.ę(ǁ+" / "+ƹ,125+Í,out Í,1,true)+Ŏ.ę(ŗ+" ms",145+Í,out Í,1)+Ŏ.ę(Ř+"%",105+Í,out Í,1,true)+Ŏ.ę(""+ǖ,65+Í,out Í,1,
true)+Ŏ.ę(""+ǘ,65+Í,out Í,1,true)+Ŏ.ę(""+ǌ,65+Í,out Í,1,true)+"\n");Echo(ţ=((ƹ>1)?("Cycle "+ǁ+" of "+ƹ+" completed in "):
"Completed in ")+ŗ+" ms, "+Ř+"% load ("+Runtime.CurrentInstructionCount+" instructions)");Ÿ.Add(ţ);ć();if(ǁ>=ƹ)ǁ=0;if(ƿ==""&ǔ>ƹ)ƿ=
"This easter egg will return when Keen raises the 100kb script code size limit!\n";}class Ŏ{private static Dictionary<char,byte>ŏ=new Dictionary<char,byte>();private static Dictionary<string,int>Ő=new
Dictionary<string,int>();private static byte ő;private static byte Œ;public static int œ(string z,bool ĝ=false){int ĕ;if(!Ő.
TryGetValue(z,out ĕ)){Dictionary<char,byte>ĉ=ŏ;string á=z+"\0\0\0\0\0\0\0";int Í=á.Length-(á.Length%8);byte Ŕ,ŕ,Ŗ,Ĵ,Ė,Ĳ,ė,Ę;while(Í
>0){ĉ.TryGetValue(á[Í-1],out Ŕ);ĉ.TryGetValue(á[Í-2],out ŕ);ĉ.TryGetValue(á[Í-3],out Ŗ);ĉ.TryGetValue(á[Í-4],out Ĵ);ĉ.
TryGetValue(á[Í-5],out Ė);ĉ.TryGetValue(á[Í-6],out Ĳ);ĉ.TryGetValue(á[Í-7],out ė);ĉ.TryGetValue(á[Í-8],out Ę);ĕ+=Ŕ+ŕ+Ŗ+Ĵ+Ė+Ĳ+ė+Ę;Í
-=8;}if(ĝ)Ő[z]=ĕ;}return ĕ;}public static string ę(string z,int ĕ,out int Ě,int ě=-1,bool ĝ=false){int Ġ,Ğ;Ě=ĕ-œ(z,ĝ);if(Ě
<=ő/2)return z;Ġ=Ě/ő;Ğ=0;Ě-=Ġ*ő;if(2*Ě<=ő+(Ġ*(Œ-ő))){Ğ=Math.Min(Ġ,(int)((float)Ě/(Œ-ő)+0.4999f));Ġ-=Ğ;Ě-=Ğ*(Œ-ő);}else if(
Ě>ő/2){Ġ++;Ě-=ő;}if(ě>0)return new String(' ',Ġ)+new String('\u00AD',Ğ)+z;if(ě<0)return z+new String('\u00AD',Ğ)+new
String(' ',Ġ);if((Ġ%2)>0&(Ğ%2)==0)return new String(' ',Ġ/2)+new String('\u00AD',Ğ/2)+z+new String('\u00AD',Ğ/2)+new String(
' ',Ġ-(Ġ/2));return new String(' ',Ġ-(Ġ/2))+new String('\u00AD',Ğ/2)+z+new String('\u00AD',Ğ-(Ğ/2))+new String(' ',Ġ/2);}
public static string ę(double ğ,int ĕ,out int Ě){int Ġ,Ğ;ğ=Math.Min(Math.Max(ğ,0.0f),1.0f);Ġ=ĕ/ő;Ğ=(int)(Ġ*ğ+0.5f);Ě=ĕ-(Ġ*ő);
return new String('I',Ğ)+new String(' ',Ġ-Ğ);}public static void ġ(){Ĝ(0,"\u2028\u2029\u202F");Ĝ(7,
"'|\u00A6\u02C9\u2018\u2019\u201A");Ĝ(8,"\u0458");Ĝ(9," !I`ijl\u00A0\u00A1\u00A8\u00AF\u00B4\u00B8\u00CC\u00CD\u00CE\u00CF\u00EC\u00ED\u00EE\u00EF\u0128\u0129\u012A\u012B\u012E\u012F\u0130\u0131\u0135\u013A\u013C\u013E\u0142\u02C6\u02C7\u02D8\u02D9\u02DA\u02DB\u02DC\u02DD\u0406\u0407\u0456\u0457\u2039\u203A\u2219"
);Ĝ(10,"(),.1:;[]ft{}\u00B7\u0163\u0165\u0167\u021B");Ĝ(11,"\"-r\u00AA\u00AD\u00BA\u0140\u0155\u0157\u0159");Ĝ(12,
"*\u00B2\u00B3\u00B9");Ĝ(13,"\\\u00B0\u201C\u201D\u201E");Ĝ(14,"\u0491");Ĝ(15,"/\u0133\u0442\u044D\u0454");Ĝ(16,
"L_vx\u00AB\u00BB\u0139\u013B\u013D\u013F\u0141\u0413\u0433\u0437\u043B\u0445\u0447\u0490\u2013\u2022");Ĝ(17,"7?Jcz\u00A2\u00BF\u00E7\u0107\u0109\u010B\u010D\u0134\u017A\u017C\u017E\u0403\u0408\u0427\u0430\u0432\u0438\u0439\u043D\u043E\u043F\u0441\u044A\u044C\u0453\u0455\u045C"
);Ĝ(18,"3FKTabdeghknopqsuy\u00A3\u00B5\u00DD\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E8\u00E9\u00EA\u00EB\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF\u00FF\u0101\u0103\u0105\u010F\u0111\u0113\u0115\u0117\u0119\u011B\u011D\u011F\u0121\u0123\u0125\u0127\u0136\u0137\u0144\u0146\u0148\u0149\u014D\u014F\u0151\u015B\u015D\u015F\u0161\u0162\u0164\u0166\u0169\u016B\u016D\u016F\u0171\u0173\u0176\u0177\u0178\u0219\u021A\u040E\u0417\u041A\u041B\u0431\u0434\u0435\u043A\u0440\u0443\u0446\u044F\u0451\u0452\u045B\u045E\u045F"
);Ĝ(19,"+<=>E^~\u00AC\u00B1\u00B6\u00C8\u00C9\u00CA\u00CB\u00D7\u00F7\u0112\u0114\u0116\u0118\u011A\u0404\u040F\u0415\u041D\u042D\u2212"
);Ĝ(20,"#0245689CXZ\u00A4\u00A5\u00C7\u00DF\u0106\u0108\u010A\u010C\u0179\u017B\u017D\u0192\u0401\u040C\u0410\u0411\u0412\u0414\u0418\u0419\u041F\u0420\u0421\u0422\u0423\u0425\u042C\u20AC"
);Ĝ(21,"$&GHPUVY\u00A7\u00D9\u00DA\u00DB\u00DC\u00DE\u0100\u011C\u011E\u0120\u0122\u0124\u0126\u0168\u016A\u016C\u016E\u0170\u0172\u041E\u0424\u0426\u042A\u042F\u0436\u044B\u2020\u2021"
);Ĝ(22,"ABDNOQRS\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D8\u0102\u0104\u010E\u0110\u0143\u0145\u0147\u014C\u014E\u0150\u0154\u0156\u0158\u015A\u015C\u015E\u0160\u0218\u0405\u040A\u0416\u0444"
);Ĝ(23,"\u0459");Ĝ(24,"\u044E");Ĝ(25,"%\u0132\u042B");Ĝ(26,"@\u00A9\u00AE\u043C\u0448\u045A");Ĝ(27,"M\u041C\u0428");Ĝ(28,
"mw\u00BC\u0175\u042E\u0449");Ĝ(29,"\u00BE\u00E6\u0153\u0409");Ĝ(30,"\u00BD\u0429");Ĝ(31,"\u2122");Ĝ(32,"W\u00C6\u0152\u0174\u2014\u2026\u2030");ő=ŏ
[' '];Œ=ŏ['\u00AD'];}private static void Ĝ(byte ĕ,string z){Dictionary<char,byte>ĉ=ŏ;string á=z+"\0\0\0\0\0\0\0";byte Ċ=
Math.Max((byte)0,ĕ);int Í=á.Length-(á.Length%8);while(Í>0){ĉ[á[--Í]]=Ċ;ĉ[á[--Í]]=Ċ;ĉ[á[--Í]]=Ċ;ĉ[á[--Í]]=Ċ;ĉ[á[--Í]]=Ċ;ĉ[á[
--Í]]=Ċ;ĉ[á[--Í]]=Ċ;ĉ[á[--Í]]=Ċ;}ĉ['\0']=0;}private int ċ;private int Č;private int č;private List<string>[]Ď;private List
<int>[]ē;private int[]ď;private int[]Đ;private bool[]đ;private int[]Ē;public Ŏ(int ċ,int č=1){this.ċ=ċ;this.Č=0;this.č=č;
this.Ď=new List<string>[ċ];this.ē=new List<int>[ċ];this.ď=new int[ċ];this.Đ=new int[ċ];this.đ=new bool[ċ];this.Ē=new int[ċ];
for(int Ĉ=0;Ĉ<ċ;Ĉ++){this.Ď[Ĉ]=new List<string>();this.ē[Ĉ]=new List<int>();this.ď[Ĉ]=-1;this.Đ[Ĉ]=0;this.đ[Ĉ]=false;this.Ē
[Ĉ]=0;}}public void Ĕ(int Ģ,string z,bool ĝ=false){int ĕ=0;this.Ď[Ģ].Add(z);if(this.đ[Ģ]==false){ĕ=œ(z,ĝ);this.Ē[Ģ]=Math.
Max(this.Ē[Ģ],ĕ);}this.ē[Ģ].Add(ĕ);this.Č=Math.Max(this.Č,this.Ď[Ģ].Count);}public void Ī(){for(int Ĉ=0;Ĉ<this.ċ;Ĉ++){this.
Ď[Ĉ].Add("");this.ē[Ĉ].Add(0);}this.Č++;}public int ī(){return this.Č;}public int Ĭ(){int ĕ=this.č*ő;for(int Ĉ=0;Ĉ<this.ċ
;Ĉ++)ĕ+=this.č*ő+this.Ē[Ĉ];return ĕ;}public void ĭ(int Ģ,int ě){this.ď[Ģ]=ě;}public void Į(int Ģ,int į=1){this.Đ[Ģ]=į;}
public void İ(int Ģ,bool ı=true){this.đ[Ģ]=ı;}public void ĳ(int Ģ,int ĕ){this.Ē[Ģ]=ĕ;}public string[][]Ħ(int ĕ=0,int ģ=1){int
Ĉ,Ĥ,ò,Í,ĥ,ħ,Ě,ó;int[]Ē;byte Ċ;double ğ;string z;StringBuilder Ĩ;string[][]ĩ;Ē=(int[])this.Ē.Clone();Ě=ĕ*ģ-this.č*ő;ó=0;
for(Ĉ=0;Ĉ<this.ċ;Ĉ++){Ě-=this.č*ő;if(this.Đ[Ĉ]==0)Ě-=Ē[Ĉ];ó+=this.Đ[Ĉ];}for(Ĉ=0;Ĉ<this.ċ&ó>0;Ĉ++){if(this.Đ[Ĉ]>0){Ē[Ĉ]=Math
.Max(Ē[Ĉ],this.Đ[Ĉ]*Ě/ó);Ě-=Ē[Ĉ];ó-=this.Đ[Ĉ];}}ĩ=new string[ģ][];for(ò=0;ò<ģ;ò++)ĩ[ò]=new string[this.Č];ģ--;Í=0;Ĩ=new
StringBuilder();for(Ĥ=0;Ĥ<this.Č;Ĥ++){Ĩ.Clear();ò=0;ó=ĕ;Ě=0;for(Ĉ=0;Ĉ<this.ċ;Ĉ++){Ě+=this.č*ő;if(Ĥ>=this.Ď[Ĉ].Count||Ď[Ĉ][Ĥ]==""){Ě+=
Ē[Ĉ];}else{z=this.Ď[Ĉ][Ĥ];ŏ.TryGetValue(z[0],out Ċ);ħ=this.ē[Ĉ][Ĥ];if(this.đ[Ĉ]==true){ğ=0.0;if(double.TryParse(z,out ğ))
ğ=Math.Min(Math.Max(ğ,0.0),1.0);Í=(int)((Ē[Ĉ]/ő)*ğ+0.5);Ċ=ő;ħ=Í*ő;}if(this.ď[Ĉ]>0){Ě+=(Ē[Ĉ]-ħ);}else if(this.ď[Ĉ]==0){Ě+=
(Ē[Ĉ]-ħ)/2;}while(ò<ģ&Ě>ó-Ċ){Ĩ.Append(' ');ĩ[ò][Ĥ]=Ĩ.ToString();Ĩ.Clear();ò++;Ě-=ó;ó=ĕ;}ó-=Ě;Ĩ.Append(ę("",Ě,out Ě));ó+=Ě
;if(this.ď[Ĉ]<0){Ě+=(Ē[Ĉ]-ħ);}else if(this.ď[Ĉ]==0){Ě+=(Ē[Ĉ]-ħ)-((Ē[Ĉ]-ħ)/2);}if(this.đ[Ĉ]==true){while(ò<ģ&ħ>ó){ĥ=ó/ő;ó
-=ĥ*ő;ħ-=ĥ*ő;Ĩ.Append(new String('I',ĥ));ĩ[ò][Ĥ]=Ĩ.ToString();Ĩ.Clear();ò++;Ě-=ó;ó=ĕ;Í-=ĥ;}z=new String('I',Í);}else{while
(ò<ģ&ħ>ó){Í=0;while(ó>=Ċ){ó-=Ċ;ħ-=Ċ;ŏ.TryGetValue(z[++Í],out Ċ);}Ĩ.Append(z,0,Í);ĩ[ò][Ĥ]=Ĩ.ToString();Ĩ.Clear();ò++;Ě-=ó;
ó=ĕ;z=z.Substring(Í);}}ó-=ħ;Ĩ.Append(z);}}ĩ[ò][Ĥ]=Ĩ.ToString();}return ĩ;}public string Ə(int ĕ=0){return String.Join(
"\n",this.Ħ(ĕ,1)[0]);}}
