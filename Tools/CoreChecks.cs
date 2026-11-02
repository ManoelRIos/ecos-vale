using Ecos;
using System;
using System.Collections.Generic;
class CoreChecks {
 static int passed;
 static void Check(string name,Action test){test();passed++;Console.WriteLine("PASS "+name);}
 static void Assert(bool condition,string message){if(!condition)throw new Exception(message);}
 static void Main(){
 Check("hash repeatable including negative coordinates",()=>{for(int x=-100;x<100;x++)Assert(WorldMath.Hash(x,-53,71319)==WorldMath.Hash(x,-53,71319),"nondeterministic hash");Assert(WorldMath.Hash(0,0,1)!=WorldMath.Hash(0,0,2),"seed ignored");});
 Check("height deterministic, finite and seed-sensitive",()=>{bool changed=false;for(int z=-4000;z<=4000;z+=137)for(int x=-4000;x<=4000;x+=139){float a=WorldMath.Height(x,z,71319);Assert(float.IsFinite(a),"nonfinite height");Assert(a==WorldMath.Height(x,z,71319),"height changed");changed|=a!=WorldMath.Height(x,z,82);}Assert(changed,"seed ignored");});
 Check("climate covers six biomes in initial 1km",()=>{var found=new HashSet<int>();for(int z=-500;z<=500;z+=25)for(int x=-500;x<=500;x+=25){int b=WorldMath.Biome(x,z,71319);Assert(b>=0&&b<6,"out of range");found.Add(b);}Assert(found.Count==6,"missing biome");});
 Check("noise continuous across negative lattice boundaries",()=>{for(int i=-100;i<100;i++){float a=WorldMath.Noise(i-.0001f,.37f,4),b=WorldMath.Noise(i+.0001f,.37f,4);Assert(Math.Abs(a-b)<.002,"noise discontinuity");}});
 Check("shared chunk edge heights exact",()=>{const int size=128,res=24;for(int chunk=-30;chunk<=30;chunk++)for(int z=0;z<=res;z++){float a=chunk*size+res*(float)size/res,b=(chunk+1)*size;Assert(a==b,"coordinate seam");Assert(WorldMath.Height(a,z*(float)size/res,7)==WorldMath.Height(b,z*(float)size/res,7),"height seam");}});
 Check("random values bounded",()=>{for(int i=0;i<10000;i++){float n=WorldMath.Random(i,-i,5);Assert(n>=0&&n<=1,"random range");}});
 Check("inventory stacks across slots",()=>{var i=new InventoryState{capacity=3};Assert(i.Add("crystal",120),"add failed");Assert(i.stacks.Count==3&&i.Count("crystal")==120,"stacking");Assert(i.stacks[0].count==50&&i.stacks[2].count==20,"limits");});
 Check("inventory overflow atomic",()=>{var i=new InventoryState{capacity=1};i.Add("wood",49);Assert(!i.Add("wood",2),"overflow accepted");Assert(i.Count("wood")==49,"partial addition");Assert(!i.Add("stone",1),"slot overflow");});
 Check("inventory remove atomic and removes empty slots",()=>{var i=new InventoryState();i.Add("crystal",75);Assert(!i.Remove("crystal",76)&&i.Count("crystal")==75,"partial removal");Assert(i.Remove("crystal",51)&&i.Count("crystal")==24,"remove");Assert(i.Remove("crystal",24)&&i.stacks.Count==0,"empty slots");});
 Check("inventory rejects invalid commands",()=>{var i=new InventoryState();Assert(!i.Add("",1)&&!i.Add("x",0)&&!i.Add("x",-1)&&!i.Add("x",2,0),"invalid addition");Assert(!i.Remove("x",0)&&!i.Remove("x",-1),"invalid removal");});
 Check("spawn ground and world bounds",()=>{Assert(Math.Abs(WorldMath.Height(0,0,71319)-2)<.001,"unsafe spawn");for(int x=-4096;x<=4096;x+=128)Assert(float.IsFinite(WorldMath.Height(x,4096,71319)),"boundary height");});

 Check("camp cannot consume partial costs",()=>{var s=new ProgressionState();s.inventory.Add("wood",4);s.inventory.Add("stone",2);Assert(!ProgressionRules.BuildCamp(s,out _),"camp built without stone");Assert(!s.campBuilt&&s.inventory.Count("wood")==4&&s.inventory.Count("stone")==2,"partial camp cost");});
 Check("complete staged exploration loop grants reward once",()=>{var s=new ProgressionState();s.inventory.Add("wood",6);s.inventory.Add("stone",7);s.inventory.Add("herb",5);s.inventory.Add("crystal",5);Assert(!ProgressionRules.CraftLens(s,out _),"lens before camp");Assert(ProgressionRules.BuildCamp(s,out _),"camp");Assert(!ProgressionRules.BuildCamp(s,out _),"duplicate camp");Assert(ProgressionRules.CraftLens(s,out _),"lens");for(int n=0;n<3;n++)Assert(ProgressionRules.Restore(s,"shrine-"+n,out _),"shrine");Assert(ProgressionRules.CompleteQuest(s),"quest");Assert(!ProgressionRules.CompleteQuest(s)&&!ProgressionRules.Restore(s,"shrine-0",out _),"duplicate reward");Assert(s.inventory.Count("artifact")==1&&s.experience==150,"reward or xp");Assert(s.inventory.Count("wood")==0&&s.inventory.Count("stone")==0&&s.inventory.Count("herb")==0&&s.inventory.Count("crystal")==0,"recipe budget");});
 Check("cache inventory failure is atomic",()=>{var s=new ProgressionState{lensCrafted=true,inventory=new InventoryState{capacity=1}};s.inventory.Add("wood",50);Assert(!ProgressionRules.OpenCache(s,"cache-0",out _),"full cache granted");Assert(s.inventory.Count("wood")==50&&s.openedCaches.Count==0&&s.experience==0,"partial cache change");s.inventory.Remove("wood",50);Assert(!ProgressionRules.OpenCache(s,"cache-0",out _)&&s.inventory.stacks.Count==0,"partial multi reward");s.inventory.capacity=3;Assert(ProgressionRules.OpenCache(s,"cache-0",out _),"cache reward");Assert(!ProgressionRules.OpenCache(s,"cache-0",out _)&&s.inventory.Count("artifact")==1,"repeat cache");});
 Check("unknown shrine IDs cannot complete quest",()=>{var s=new ProgressionState{lensCrafted=true};s.restored.AddRange(new[]{"fake-0","fake-1","fake-2"});Assert(!ProgressionRules.Restore(s,"fake-3",out _)&&!ProgressionRules.CompleteQuest(s),"fake shrines accepted");});
 Check("discovery and journals grant experience once",()=>{var s=new ProgressionState();Assert(ProgressionRules.Discover(s,"camp"),"discovery");Assert(!ProgressionRules.Discover(s,"camp"),"repeat discovery");Assert(ProgressionRules.ReadClue(s,"journal-0",out _),"journal");Assert(!ProgressionRules.ReadClue(s,"journal-0",out _)&&s.experience==18,"repeat journalxp");ProgressionRules.GainExperience(s,10000);Assert(s.inventory.capacity==20,"capacity cap");s.inventory.capacity=40;ProgressionRules.GainExperience(s,10);Assert(s.inventory.capacity==40,"custom capacity shrank");});
 Check("duplicate recipe costs cannot overspend",()=>{var i=new InventoryState();i.Add("wood",5);Assert(!ProgressionRules.TryTrade(i,new[]{new ItemAmount("wood",3),new ItemAmount("wood",3)},Array.Empty<ItemAmount>(),out _),"overspend");Assert(i.Count("wood")==5,"recipe partial removal");});
 Check("legacy progress migration preserves inventory and unlocks access",()=>{var s=new ProgressionState();s.inventory.Add("wood",73);s.restored.Add("shrine-1");s.discovered.Add("shrine-1");s.collected.Add("crystal-1");s.openedCaches=null;s.readClues=null;ProgressionRules.MigrateLegacy(s);Assert(s.campBuilt&&s.lensCrafted&&s.inventory.Count("wood")==73,"legacy progress lost");Assert(s.experience==35&&s.readClues!=null&&s.openedCaches!=null&&s.collected.Contains("crystal-1"),"legacy lists/xp");});
 Check("compact grove plans deterministic bounded and dense",()=>{float[] density={1,.85f,1,1,1,1};var a=PropDistribution.Generate(-2,-1,128,280,71319,density,System.Threading.CancellationToken.None);var b=PropDistribution.Generate(-2,-1,128,280,71319,density,System.Threading.CancellationToken.None);Assert(a.Length==b.Length&&a.Length>150,"plan density");for(int j=0;j<a.Length;j++){Assert(a[j].Equals(b[j]),"plan not deterministic");Assert(a[j].x>=0&&a[j].x<128&&a[j].z>=0&&a[j].z<128,"placement bounds");Assert(a[j].kind>=0&&a[j].kind<=3,"kind bounds");float width=a[j].kind==0?3.7f:a[j].kind==1?2.2f:1.5f;Assert(WorldMath.PathDistance(-256+a[j].x,-128+a[j].z)>=width,"trail obstructed");}var points=new List<(float x,float z)>();int details=0;for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)foreach(var v in PropDistribution.Generate(x,z,128,280,71319,density,System.Threading.CancellationToken.None)){if(v.kind==0)points.Add((x*128+v.x,z*128+v.z));else details++;}Assert(points.Count>900&&details>700,"sparse initial area");double total=0;foreach(var v in points){double nearest=double.MaxValue;foreach(var w in points){if(v==w)continue;double d=(v.x-w.x)*(v.x-w.x)+(v.z-w.z)*(v.z-w.z);nearest=Math.Min(nearest,d);}total+=Math.Sqrt(nearest);}double spacing=total/points.Count;Assert(spacing>=6&&spacing<12,"trunk spacing "+spacing);Console.WriteLine($"  Initial grove candidates: {points.Count} trees, {details} details; nearest mean {spacing:F2}m before POI clearings");});
 Check("compact decoration cancellation and zero budget",()=>{float[] density={1,1,1,1,1,1};Assert(PropDistribution.Generate(0,0,128,0,7,density,System.Threading.CancellationToken.None).Length==0,"zero budget");using(var c=new System.Threading.CancellationTokenSource()){c.Cancel();bool cancelled=false;try{PropDistribution.Generate(0,0,128,280,7,density,c.Token);}catch(OperationCanceledException){cancelled=true;}Assert(cancelled,"cancel ignored");}});
 Check("wildlife observations reject unknown species and reward each species once",()=>{
    var state=new ProgressionState();
    Assert(!ProgressionRules.ObserveSpecies(state,"dragon",out _)&&state.experience==0&&state.observedSpecies.Count==0,"unknown species accepted");
    var unique=new HashSet<string>();
    foreach(string species in CreatureCatalog.SpeciesIds){Assert(unique.Add(species)&&CreatureCatalog.IsSpecies(species),"invalid catalog");Assert(!string.IsNullOrEmpty(CreatureCatalog.Name(species))&&!string.IsNullOrEmpty(CreatureCatalog.Description(species)),"missing field entry");Assert(ProgressionRules.ObserveSpecies(state,species,out _),"observation failed");Assert(!ProgressionRules.ObserveSpecies(state,species,out _),"repeat xp");}
    Assert(unique.Count==6&&state.experience==72&&ProgressionRules.ObservedSpeciesCount(state)==6,"field guide count/xp");
 });
 Check("complete field guide grants its reward exactly once",()=>{
    var state=new ProgressionState();Assert(!ProgressionRules.CompleteFieldGuide(state),"empty guide rewarded");
    state.observedSpecies.AddRange(new[]{"fake-a","fake-b","fake-c","fake-d","fake-e","fake-f"});
    Assert(!ProgressionRules.CompleteFieldGuide(state),"unknown sightings completed guide");state.observedSpecies.Clear();
    foreach(string species in CreatureCatalog.SpeciesIds)ProgressionRules.ObserveSpecies(state,species,out _);
    Assert(ProgressionRules.CompleteFieldGuide(state)&&state.guideRewarded&&state.inventory.Count("artifact")==1&&state.experience==97,"guide reward");
    Assert(!ProgressionRules.CompleteFieldGuide(state)&&state.inventory.Count("artifact")==1&&state.experience==97,"duplicate guide reward");
 });
 Check("full field guide defers reward atomically when inventory is full",()=>{
    var state=new ProgressionState();foreach(string species in CreatureCatalog.SpeciesIds)ProgressionRules.ObserveSpecies(state,species,out _);
    state.inventory.capacity=1;state.inventory.Add("wood",50);
    Assert(!ProgressionRules.CompleteFieldGuide(state)&&!state.guideRewarded&&state.experience==72&&state.inventory.Count("wood")==50,"partial guide reward");
    state.inventory.Remove("wood",50);Assert(ProgressionRules.CompleteFieldGuide(state)&&state.inventory.Count("artifact")==1,"reward retry failed");
 });
 Check("groves contain four deterministic silhouettes without obstructing trails",()=>{
    var variants=new HashSet<int>();float[] density={1,.85f,1,1,1,1};
    for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)foreach(var p in PropDistribution.Generate(x,z,128,280,71319,density,System.Threading.CancellationToken.None)){
      Assert(p.variant>=0&&p.variant<4,"variant bounds");if(p.kind==0)variants.Add(p.variant);
    }
    Assert(variants.Count==4,"repeated tree family");
 });
 Check("wildlife habitats are seeded and cover day and night species",()=>{
    var day=new HashSet<string>();var night=new HashSet<string>();bool changed=false;
    for(int z=-18;z<=18;z++)for(int x=-18;x<=18;x++){
      int biome=WorldMath.Biome(x*28,z*28,71319);string a=CreatureCatalog.ChooseSpecies(x,z,71319,biome,false),b=CreatureCatalog.ChooseSpecies(x,z,71319,biome,true);
      Assert(a==CreatureCatalog.ChooseSpecies(x,z,71319,biome,false)&&b==CreatureCatalog.ChooseSpecies(x,z,71319,biome,true),"habitat not deterministic");
      Assert(CreatureCatalog.IsSpecies(a)&&CreatureCatalog.IsSpecies(b)&&!CreatureCatalog.IsNocturnal(a),"invalid habitat species");day.Add(a);night.Add(b);
      changed|=a!=CreatureCatalog.ChooseSpecies(x,z,42,biome,false);
    }
    Assert(day.Count==4&&night.Count==6&&changed,"species unreachable or seed ignored");
 });
 Console.WriteLine($"{passed} checks passed. These compile production pure C# only, not Unity components.");
 }
}
