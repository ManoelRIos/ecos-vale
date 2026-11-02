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
 Check("quest resource budget",()=>{var i=new InventoryState();i.Add("crystal",3);for(int j=0;j<3;j++)Assert(i.Remove("crystal",1),"altar budget");Assert(i.Count("crystal")==0&&i.Add("artifact",1),"reward space");});
 Check("spawn ground and world bounds",()=>{Assert(Math.Abs(WorldMath.Height(0,0,71319)-2)<.001,"unsafe spawn");for(int x=-4096;x<=4096;x+=128)Assert(float.IsFinite(WorldMath.Height(x,4096,71319)),"boundary height");});
 Console.WriteLine($"{passed} checks passed. These compile production pure C# only, not Unity components.");
 }
}
