using System;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntPixelArt
    {
        /// <summary>Reference-sheet guardians: 48px cells, five explicit states and four frames.</summary>
        public Sprite ReferenceBossFrame(int variant, BossAnimationState state, int frame)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HauntPixelArt));
            if (variant < 0 || variant > 6) throw new ArgumentOutOfRangeException(nameof(variant));
            if (!Enum.IsDefined(typeof(BossAnimationState), state)) throw new ArgumentOutOfRangeException(nameof(state));
            frame = ((frame % 4) + 4) % 4;
            string key = "reference-boss/" + variant + "/" + state + "/" + frame;
            if (sprites.TryGetValue(key, out var cached)) return cached;
            string id = variant == 0 ? "boss" : variant == 1 ? "widow" : variant == 2 ? "bishop"
                : variant == 3 ? "matriarch" : variant == 4 ? "bell" : variant == 5 ? "iron-executioner" : "bog-oracle";
            Sprite sheetSprite = TryLoadSheetSprite(key, "bosses/" + id, frame, (int)state, 48, .5f);
            if (sheetSprite) return sheetSprite;
            var pixels = new Canvas(48, 48);
            if (state == BossAnimationState.Death) ReferenceGuardianDeath(pixels, variant, frame);
            else
            {
                bool angry = state == BossAnimationState.Enrage;
                if (angry) ReferenceFury(pixels, variant, frame);
                switch (variant)
                {
                    case 0: ReferencePumpkin(pixels, state, frame); break;
                    case 1: ReferenceWidow(pixels, state, frame); break;
                    case 2: ReferenceBishop(pixels, state, frame); break;
                    case 3: ReferenceMatriarch(pixels, state, frame); break;
                    case 4: ReferenceBell(pixels, state, frame); break;
                    case 5: ReferenceExecutioner(pixels, state, frame); break;
                    case 6: ReferenceOracle(pixels, state, frame); break;
                }
                pixels.Outline(Ink);
            }
            return CacheSprite(key, pixels);
        }

        /// <summary>16px environmental state cells. Stage 0 intact; 1 preparing/cracked;
        /// 2 detonation/spill/open; 3 remains/pool/looted. No gameplay state is changed.</summary>
        public Sprite PropFrame(string id, int stage, int frame = 0)
        {
            if (disposed) throw new ObjectDisposedException(nameof(HauntPixelArt));
            if (stage < 0 || stage > 3) throw new ArgumentOutOfRangeException(nameof(stage));
            if (id != "barrel-explosive" && id != "barrel-acid" && id != "decrepit-chest")
                throw new ArgumentException("Objeto desconhecido: " + id, nameof(id));
            frame = ((frame % 4) + 4) % 4;
            string key = "reference-prop/" + id + "/" + stage + "/" + frame;
            if (sprites.TryGetValue(key, out var cached)) return cached;
            Sprite sheetSprite = TryLoadSheetSprite(key, "props/" + id, frame, stage, 16, .5f);
            if (sheetSprite) return sheetSprite;
            Canvas pixels = id == "barrel-explosive" ? ReferenceWoodBarrel(stage, frame)
                : id == "barrel-acid" ? ReferenceAcidBarrel(stage, frame) : ReferenceChest(stage, frame);
            return CacheSprite(key, pixels);
        }

        static void ReferencePumpkin(Canvas c, BossAnimationState state, int f)
        {
            int bob = f == 1 || f == 2 ? 1 : 0;
            bool attack = state == BossAnimationState.Attack, tell = state == BossAnimationState.Telegraph;
            Color32 cloak = Hex(0x46334f), cloakLight = Hex(0x70506f), pumpkinShade = Hex(0xb55020);
            c.Polygon(new[] { 20,20+bob,31,21+bob,38,43,27,42,20,45,9,41,13,27 },cloak);
            c.Polygon(new[] { 16,25+bob,19,24+bob,15,40,11,40 },cloakLight);
            c.Polygon(new[] { 30,25+bob,34,29+bob,36,41,30,40 },Hex(0x271e36));
            c.Rect(21,28+bob,9,11,Hex(0x373d3b)); c.Rect(22,30+bob,6,1,Hex(0x788076));
            c.Rect(22,39+bob,4,5,Hex(0x493b34)); c.Rect(29,38+bob,4,5,Hex(0x493b34));
            c.Rect(21,43,6,2,Hex(0x201e26)); c.Rect(28,43,7,2,Hex(0x201e26));
            c.Ellipse(16,9+bob,19,17,pumpkinShade); c.Ellipse(18,10+bob,16,14,Orange);
            c.Ellipse(22,9+bob,8,16,Hex(0xe77826));
            c.Line(21,12+bob,20,21+bob,Hex(0xfba64a)); c.Line(29,11+bob,31,21+bob,Hex(0xc65c20));
            c.Rect(24,6+bob,3,5,Hex(0x486147)); c.Line(26,7+bob,29,5+bob,Hex(0x718951));
            c.Polygon(new[] { 19,15+bob,23,16+bob,21,19+bob },Ink);
            c.Polygon(new[] { 32,15+bob,28,16+bob,30,19+bob },Ink);
            c.Polygon(new[] { 20,20+bob,25,22+bob,31,20+bob,30,24+bob,22,24+bob },Ink);
            c.Set(22,21+bob,Gold); c.Set(27,22+bob,Gold); c.Set(24,24+bob,Orange);
            c.Rect(18,27+bob,3,5,Hex(0x806349)); c.Rect(32,26+bob,3,4,Hex(0x806349));
            int sx = attack ? 30-f*2 : 35, sy = tell ? 10-f : attack ? 20+f*3 : 16;
            c.Line(sx,sy,sx-3,42,Hex(0x7f5940)); c.Line(sx+1,sy,sx-2,42,Hex(0xbc8b54));
            c.Polygon(new[] { sx-1,sy,sx+1,sy-3,sx+7,sy-2,sx+9,sy+3,sx+5,sy+7,sx+6,sy+2,sx+2,sy },Hex(0x8d9f9d));
            c.Line(sx+2,sy-2,sx+7,sy,Hex(0xd6d8c2));
            if (attack)
            {
                c.Polygon(new[] { 20,20,38,19+f,43,27+f,37,35+f,34,29+f },Alpha(Hex(0xcac9db),190));
                c.Line(35,21+f,41,28+f,Pale);
            }
            if (tell) { c.Set(20,16+bob,Gold); c.Set(30,16+bob,Gold); ReferenceCross(c,37,8+f,Gold); }
            ReferenceBat(c,8+f*2,12+(f%2)*3,f); ReferenceBat(c,37-f,6+(f%3),f+1);
            if (state == BossAnimationState.Enrage)
            {
                c.Polygon(new[] { 17,10,16,3+f,21,7,24,1+f,27,7,33,3,33,11 },Orange);
                c.Set(20,7+f,Gold); c.Set(29,6+f,Pale);
            }
            // Moving cape tear makes the four breathing cells genuinely distinct.
            c.Line(12,39,13+f,42,Hex(0x8c617f));
        }

        static void ReferenceExecutioner(Canvas c, BossAnimationState state, int f)
        {
            Color32 iron = Hex(0x566a78), light = Hex(0x9aaab3), dark = Hex(0x2d3947);
            bool attack = state == BossAnimationState.Attack, tell = state == BossAnimationState.Telegraph;
            int bob = f == 1 || f == 2 ? 1 : 0;
            c.Polygon(new[] { 15,22+bob,31,21+bob,35,37,13,37 },dark);
            c.Rect(15,25+bob,17,12,iron); c.Rect(17,26+bob,12,3,light);
            c.Rect(18,30+bob,10,6,Hex(0x485662)); c.Rect(20,30+bob,2,6,Hex(0x7d8e98));
            c.Rect(14,35+bob,20,3,Hex(0x5f4d3a)); c.Rect(23,35+bob,4,3,Hex(0xa5a8a1));
            c.Rect(15,38,6,6,iron); c.Rect(27,38,6,6,iron);
            c.Rect(14,43,9,3,dark); c.Rect(26,43,9,3,dark);
            c.Rect(15,42,6,2,light); c.Rect(27,42,6,2,light);
            c.Polygon(new[] { 12,21+bob,18,20+bob,20,25+bob,17,29+bob,10,28+bob,9,24+bob },iron);
            c.Polygon(new[] { 30,20+bob,36,21+bob,39,25+bob,36,29+bob,29,27+bob },iron);
            c.Line(11,22+bob,17,22+bob,light); c.Line(32,22+bob,36,24+bob,light);
            c.Rect(11,28+bob,5,7,dark); c.Rect(12,29+bob,4,3,light);
            c.Rect(32,29+bob,5,6,dark); c.Rect(32,29+bob,4,3,light);
            c.Rect(17,8+bob,13,13,dark); c.Rect(18,9+bob,11,11,iron);
            c.Rect(19,8+bob,9,2,light); c.Rect(20,10+bob,2,9,Hex(0x8597a2));
            c.Rect(18,14+bob,11,3,Ink); c.Set(20,15+bob,state == BossAnimationState.Enrage ? Orange : Hex(0xd4b781));
            c.Set(26,15+bob,state == BossAnimationState.Enrage ? Orange : Hex(0xd4b781));
            c.Rect(22,18+bob,5,2,dark); c.Set(23+f%3,10+bob,Hex(0xc1c8c6));
            if (attack)
            {
                int hx = new[] { 11,33,38,34 }[f], hy = new[] { 10,13,26,34 }[f];
                ReferenceAxe(c,23,29,hx,hy,light,iron);
                c.Polygon(new[] { 19,17,36,11,45,20,43,32,34,40,25,41,36,33,38,24 },Gold);
                c.Polygon(new[] { 24,18,37,15,42,23,39,31,31,36,36,28,34,22 },Hex(0xfff1b0));
                c.Line(42,20+f,44,25+f,Pale);
                c.Set(38-f,41-f,Orange);
            }
            else
            {
                int hx = tell ? 13-f : 37, hy = tell ? 5+f : 16+f%2;
                ReferenceAxe(c,34,33,hx,hy,light,iron);
                if (tell) ReferenceCross(c,8+f,6+f,Gold);
            }
            if (state == BossAnimationState.Enrage)
            {
                c.Line(14,12,11,9,Hex(0xf34f40)); c.Line(32,12,35,8,Hex(0xf34f40));
                c.Line(18,7,18,3+f,Orange); c.Line(29,6,31,2+f,Orange);
                c.Rect(18,14+bob,11,1,Hex(0xec6a4b));
            }
        }

        static void ReferenceAxe(Canvas c,int handleX,int handleY,int headX,int headY,Color32 edge,Color32 iron)
        {
            c.Line(handleX,handleY,headX,headY,Hex(0x6b4432));
            c.Line(handleX+1,handleY,headX+1,headY,Hex(0xb38a53));
            c.Polygon(new[] { headX-7,headY-5,headX-1,headY-2,headX+1,headY-4,headX+6,headY-5,headX+7,headY+1,headX+4,headY+5,headX+1,headY+3,headX-2,headY+3,headX-7,headY+6,headX-9,headY+2 },iron);
            c.Line(headX-8,headY-2,headX-7,headY+4,edge); c.Line(headX+6,headY-3,headX+5,headY+2,edge);
            c.Rect(headX-1,headY-2,3,6,Hex(0x7e8e94)); c.Set(headX,headY,Hex(0xd2d4bb));
        }

        static void ReferenceWidow(Canvas c,BossAnimationState state,int f)
        {
            int bob=f%2; bool attack=state==BossAnimationState.Attack;
            for(int side=0;side<2;side++) for(int leg=0;leg<3;leg++)
            {
                int x=side==0?16:31, outer=side==0?4+leg*3:43-leg*3, foot=side==0?2+leg*5:45-leg*5;
                c.Line(x,23+leg*4,outer,14+leg*4+f%2,Hex(0x9b8da6)); c.Line(outer,14+leg*4+f%2,foot,36+leg*3,Hex(0x797189));
            }
            c.Polygon(new[] { 19,10+bob,28,10+bob,33,22,35,43,27,44,21,41,14,43,15,23 },Hex(0x4e4267));
            c.Polygon(new[] { 18,17,20,39,15,42,15,24 },Hex(0x9696b1));
            c.Polygon(new[] { 29,17,28,40,33,42,32,24 },Hex(0xada7bc));
            c.Ellipse(17,7+bob,14,15,Hex(0xb6a7c2)); c.Ellipse(20,11+bob,9,14,Ink);
            c.Rect(21,14+bob,2,2,Pink); c.Rect(26,14+bob,2,2,Pink);
            c.Rect(23,19+bob,3,6,Hex(0x682a56)); c.Set(24,19+bob,Pale);
            c.Ellipse(22,6+bob,5,5,Hex(0xd8c6d5)); c.Set(24,8+bob,Gold);
            c.Line(16,25,11,30+f,Hex(0xbab5c6)); c.Line(31,25,36,29-f,Hex(0xbab5c6));
            if(attack||state==BossAnimationState.Telegraph)
            {
                int radius=attack?8+f:4+f;
                for(int i=0;i<8;i++){double a=i*Math.PI/4; c.Line(24,29,24+(int)(Math.Cos(a)*radius),29+(int)(Math.Sin(a)*radius),Alpha(Bone,180));}
                c.Ellipse(22,27,5,5,Violet); c.Set(23+f%2,28,Pale);
            }
            c.Set(16+f,40-f,Hex(0xc8b8d7));
        }

        static void ReferenceBishop(Canvas c,BossAnimationState state,int f)
        {
            int bob=f%2; Color32 robe=Hex(0x763747), trim=Hex(0xc4a273);
            c.Polygon(new[] { 17,21+bob,30,21+bob,37,43,29,42,24,45,18,42,10,43 },robe);
            c.Polygon(new[] { 20,23,24,25,27,23,30,42,18,42 },Hex(0xa45b68));
            c.Line(20,26,21,40,trim); c.Line(27,26,28,40,trim); c.Rect(23,28,2,13,Hex(0xdec791));
            c.Ellipse(18,12+bob,12,13,Bone); c.Rect(20,17+bob,3,2,Ink); c.Rect(26,17+bob,3,2,Ink);
            c.Rect(23,21+bob,2,3,Ink); c.Line(21,25+bob,28,25+bob,Hex(0x83675f));
            c.Polygon(new[] { 16,13+bob,18,4+bob,23,8+bob,27,3+bob,31,14+bob },trim);
            c.Rect(20,11+bob,10,3,Gold); c.Rect(22,5+bob,2,8,Hex(0xf8e1a5));
            int hand=state==BossAnimationState.Attack?f-2:0;
            c.Rect(12,25+hand,5,4,Bone); c.Line(14,27+hand,7,24+hand,robe);
            c.Rect(32,25-hand,4,4,Bone); c.Line(36,10,36,43,Hex(0x8c713d));
            c.Rect(34,5+f%2,5,10,Gold); c.Rect(31,8+f%2,11,3,Gold); c.Set(36,9+f%2,Pale);
            if(state==BossAnimationState.Telegraph||state==BossAnimationState.Attack)
            { int y=state==BossAnimationState.Attack?25+f*2:20-f; c.Rect(3,y,9,2,Gold); c.Rect(6,y-4,2,10,Gold); c.Set(7,y,Pale); }
            c.Set(16+f,40,trim);
        }

        static void ReferenceMatriarch(Canvas c,BossAnimationState state,int f)
        {
            int bob=f%2; Color32 shell=Hex(0x536b56), shellLight=Hex(0x83976e);
            for(int leg=0;leg<4;leg++)
            {
                int y=21+leg*4;
                c.Line(15,y,5-leg%2*2,y-8+f%2,shellLight);c.Line(5-leg%2*2,y-8+f%2,2+leg*2,40,shell);
                c.Line(32,y,41+leg%2*2,y-8-f%2,shellLight);c.Line(41+leg%2*2,y-8-f%2,45-leg*2,40,shell);
            }
            c.Ellipse(13,20+bob,22,23,shell); c.Ellipse(16,22+bob,16,18,Hex(0x304c45));
            c.Ellipse(15,10+bob,18,20,shellLight); c.Ellipse(18,13+bob,13,14,shell);
            c.Rect(18,15+bob,3,3,Gold);c.Rect(24,15+bob,3,3,Gold);c.Set(19,16+bob,Ink);c.Set(25,16+bob,Ink);
            c.Polygon(new[]{18,22+bob,30,22+bob,28,29+bob,24,26+bob,20,30+bob},Ink);
            c.Set(20,23+bob,Bone);c.Set(24,23+bob,Bone);c.Set(28,23+bob,Bone);
            c.Line(19,31+bob,28,31+bob,Hex(0xadc39c));c.Line(20,34+bob,27,34+bob,Hex(0xadc39c));
            c.Ellipse(20,36,6,5,Hex(0x8eae78));c.Set(22+f%3,37,Gold);
            if(state==BossAnimationState.Attack||state==BossAnimationState.Telegraph)
            {int r=state==BossAnimationState.Attack?3+f:2; c.Ellipse(6-r,29-r,r*2,r*2,Mint); c.Ellipse(40-r,29-r,r*2,r*2,Teal);c.Set(7-f,27,Pale);}
        }

        static void ReferenceBell(Canvas c,BossAnimationState state,int f)
        {
            int sway=f==1?1:f==3?-1:0;Color32 bell=Hex(0xb38a4e), shade=Hex(0x70563c), rim=Hex(0xe5c57f);
            c.Line(19,4,17,1,Hex(0x69616c));c.Line(29,4,31,1,Hex(0x69616c));
            c.Rect(21+sway,5,6,4,shade);c.Rect(22+sway,6,4,2,rim);
            c.Polygon(new[]{18+sway,10,29+sway,10,33+sway,17,34+sway,31,40+sway,37,8+sway,37,14+sway,31,15+sway,18},shade);
            c.Polygon(new[]{20+sway,12,26+sway,12,30+sway,18,31+sway,32,35+sway,35,13+sway,35,18+sway,30,18+sway,18},bell);
            c.Line(19+sway,16,17+sway,30,rim);c.Line(20+sway,14,25+sway,13,rim);
            c.Rect(9+sway,36,31,4,rim);c.Rect(11+sway,38,27,2,shade);
            c.Rect(19+sway,22,4,4,Ink);c.Rect(27+sway,22,4,4,Ink);c.Rect(23+sway,28,5,6,Ink);
            c.Set(20+sway,23,state==BossAnimationState.Enrage?Orange:Gold);c.Set(28+sway,23,Gold);
            c.Ellipse(21+sway,40,7,5,Hex(0x82718b));c.Set(23+f,42,Bone);
            if(state==BossAnimationState.Telegraph||state==BossAnimationState.Attack)
            {
                int y=state==BossAnimationState.Attack?41-f*3:43;
                c.Line(4,y,11,y+1,Violet);c.Line(36,y+1,44,y,Violet);
                c.Set(3+f,19,Gold);c.Set(44-f,20,Pink);
            }
        }

        static void ReferenceOracle(Canvas c,BossAnimationState state,int f)
        {
            int bob=f%2;Color32 skin=Hex(0x719276),shade=Hex(0x35584d),light=Hex(0xa1ba8e);
            c.Polygon(new[]{16,30,31,30,36,41,42,43,31,45,23,43,16,46,7,43,13,39},shade);
            c.Ellipse(11,17+bob,27,23,skin);c.Ellipse(15,25+bob,20,15,shade);
            c.Ellipse(12,13+bob,11,11,light);c.Ellipse(27,13+bob,11,11,light);
            c.Rect(15,17+bob,5,3,Ink);c.Rect(29,17+bob,5,3,Ink);c.Set(17,18+bob,Gold);c.Set(31,18+bob,Gold);
            c.Polygon(new[]{17,25+bob,33,25+bob,31,37+bob,21,37+bob},Ink);
            c.Line(20,27+bob,20,30+bob,Bone);c.Line(25,27+bob,25,29+bob,Bone);c.Line(30,27+bob,30,31+bob,Bone);
            c.Rect(22,35+bob,2,2,Bone);c.Rect(28,35+bob,2,2,Bone);
            c.Line(16,14+bob,12,8,Hex(0x806e52));c.Line(12,8,8,7,Hex(0x806e52));c.Line(33,14+bob,36,7,Hex(0x806e52));c.Line(36,7,41,5,Hex(0x806e52));
            c.Ellipse(22,6+bob,8,8,Teal);c.Ellipse(23,7+bob,5,5,Mint);c.Set(24,8+bob,Pale);
            c.Line(12,29,7,25+f%2,skin);c.Line(36,29,41,24-f%2,skin);
            if(state==BossAnimationState.Attack||state==BossAnimationState.Telegraph)
            {
                int radius=state==BossAnimationState.Attack?3+f:2+f%2;
                c.Ellipse(5-radius,24-radius,radius*2,radius*2,Mint);c.Ellipse(41-radius,24-radius,radius*2,radius*2,Mint);
                c.Set(4,22-f%3,Pale);c.Set(40,21+f%3,Pale);
            }
            c.Set(15+f*2,39,light);
        }

        static void ReferenceFury(Canvas c,int variant,int f)
        {
            Color32 aura=variant==5?Hex(0xb83136):variant==0?Orange:variant==6||variant==3?Teal:Violet;
            for(int side=0;side<2;side++)for(int i=0;i<4;i++)
            {
                int x=side==0?7+i%2:39-i%2,y=10+i*8;
                c.Line(x,y+5,x+(side==0?-2:2),y-f%3,Alpha(aura,170));
                c.Set(x+(f%2),y-2,Alpha(aura,210));
            }
            c.Line(12,43,9+f,45,aura);c.Line(34,44,39-f,45,aura);
        }

        static void ReferenceGuardianDeath(Canvas c,int variant,int f)
        {
            Color32 tint=variant==5?Hex(0x7d8f9c):variant==0?Orange:variant==3||variant==6?Mint:Violet;
            int radius=4+f*4;
            if(f<2)
            {
                c.Ellipse(23-radius/2,24-radius/2,radius,radius,tint);
                c.Polygon(new[]{24,9-f*3,27,22,40+f*2,25,27,28,24,43+f,21,28,7-f*2,25,21,22},Gold);
                c.Rect(22,22,5,6,Pale);
            }
            for(int i=0;i<10;i++)
            {
                double angle=(i*36+f*13)*Math.PI/180;
                int x=24+(int)(Math.Cos(angle)*radius),y=26+(int)(Math.Sin(angle)*radius);
                c.Rect(x,y,i%3==0?2:1,i%2==0?2:1,tint);c.Set(x-1,y-1,Alpha(Bone,(byte)(240-f*45)));
            }
            c.Rect(16-f,43,17+f*2,2,Hex(0x3d333d));c.Rect(20,41,7,2,tint);c.Set(30+f,42,Gold);
            if(variant==5){c.Rect(14+f,38+f,5,4,Hex(0x596c78));c.Rect(27+f,40,5,2,Hex(0x9aaab3));}
            if(variant==0){c.Polygon(new[]{17,41,19,33-f,22,39,26,29+f,29,40,34,35,33,43},Orange);c.Rect(22,39,5,3,Gold);}
        }

        static void ReferenceBat(Canvas c,int x,int y,int f)
        {
            int lift=f%2==0?-2:1;
            c.Polygon(new[]{x,y+1,x-5,y-3+lift,x-4,y+2,x-1,y+3,x,y+5,x+2,y+3,x+5,y+2,x+6,y-3+lift,x+1,y+1},Hex(0x302d44));
            c.Set(x,y+2,Hex(0x8a7e9b));
        }

        static void ReferenceCross(Canvas c,int x,int y,Color32 color)
        {c.Line(x-2,y,x+2,y,color);c.Line(x,y-2,x,y+2,color);c.Set(x,y,Pale);}

        static Canvas ReferenceWoodBarrel(int stage,int f)
        {
            var c=new Canvas(16,16);
            if(stage>=2)
            {
                if(stage==2)
                {
                    int r=4+f%2;
                    c.Ellipse(7-r,8-r,r*2+1,r*2+1,Hex(0xb94324));
                    c.Polygon(new[]{2,13,5,6,5,10,8,1+f,11,9,13,6,14,14},Orange);
                    c.Polygon(new[]{5,14,7,9,8,5,10,11,12,13},Gold);c.Rect(7,10,3,4,Pale);
                    c.Set(1+f,3,Orange);c.Set(13-f,1+f,Gold);
                }
                else
                {
                    c.Ellipse(2+f%2,1+f,6,6,Alpha(Hex(0x69626a),180));
                    c.Ellipse(7-f%2,4,7,6,Alpha(Hex(0x4d4a53),200));
                    c.Rect(3,13,9,2,Hex(0x4c362c));c.Line(3,12,1,9,Hex(0x94704a));c.Line(11,12,14,10,Hex(0x94704a));
                    c.Set(5+f,13,Orange);
                }
                c.Rect(2,15,12,1,Alpha(Ink,140));return c;
            }
            c.Rect(3,4,10,10,Hex(0x543a2d));c.Rect(2,6,12,6,Hex(0x775034));
            c.Rect(3,6,2,6,Hex(0xa77d4b));c.Rect(6,5,2,8,Hex(0x8b5c37));c.Rect(11,5,2,8,Hex(0x3d302a));
            c.Line(5,6,5,12,Hex(0x4d382b));c.Line(9,6,9,12,Hex(0x4d382b));
            c.Ellipse(2,2,12,5,Hex(0xb49262));c.Ellipse(3,3,10,3,Hex(0x59402f));c.Line(4,4,11,4,Hex(0x846240));
            c.Rect(2,6,12,2,Hex(0x6f7277));c.Rect(3,6,9,1,Hex(0xb1b2a3));
            c.Rect(2,11,12,2,Hex(0x5a6068));c.Rect(3,11,9,1,Hex(0xa0a69c));
            c.Set(4,7,Gold);c.Set(11,12,Gold);c.Line(3,14,12,14,Hex(0x39302b));
            c.Rect(7,8,3,3,Bone);c.Set(7,9,Ink);c.Set(9,9,Ink);c.Set(8,11,Bone);
            c.Line(9,2,10,0,Hex(0xb28c5b));
            if(stage==1){c.Set(9+f%3,0,Gold);c.Set(12-f%2,1,Orange);c.Set(13-f,3-f%2,Gold);c.Set(10,2,Orange);}
            else c.Set(9+f%2,2,Hex(0xc8a16c));
            c.Set(4+f*2,6,Hex(0xc3c4af));
            c.Outline(Ink);return c;
        }

        static Canvas ReferenceAcidBarrel(int stage,int f)
        {
            var c=new Canvas(16,16);Color32 acid=Hex(0x8be33d),acidLight=Hex(0xc6f47f);
            if(stage==3)
            {
                c.Ellipse(1,11,14,4,Alpha(acid,220));c.Ellipse(3,10+f%2,9,4,acid);
                c.Set(2+f*3,12,acidLight);c.Set(13-f,14,acidLight);c.Ellipse(7-f%2,8,3,3,acid);return c;
            }
            c.Rect(3,3,10,11,Hex(0x4d6355));c.Rect(4,4,4,10,Hex(0x7a9275));c.Rect(11,4,2,10,Hex(0x304e3c));
            c.Ellipse(2,1,12,5,Hex(0x9ca58f));c.Ellipse(3,2,10,3,Hex(0x293f30));c.Ellipse(4,2,9,3,acid);
            c.Rect(2,5,12,2,Hex(0x687b68));c.Rect(3,5,9,1,Hex(0xb3b7a2));
            c.Rect(2,11,12,2,Hex(0x5b6f60));c.Rect(3,11,9,1,Hex(0x9baf8a));
            c.Rect(7,3,2,6,acid);c.Set(8,8+f%3,acidLight);c.Set(10,4,acidLight);
            c.Set(5+f*2,3,acidLight);
            c.Line(5,13,12,13,Hex(0x9aaa8b));c.Set(4,8,Hex(0xd5d2ad));
            if(stage>0){c.Line(10,7,8,9,Ink);c.Line(8,9,10,10,Ink);c.Line(10,10,9,13,Ink);c.Set(8,9,acid);}
            if(stage==2)
            {
                c.Rect(8,8,2,7,acid);c.Rect(9,11,2,4,acidLight);
                c.Ellipse(4,13,12,3,acid);c.Set(5+f*2,14,acidLight);c.Set(14-f,11,acid);
                c.Line(8,3,9,6,acidLight);
            }
            c.Outline(Ink);return c;
        }

        static Canvas ReferenceChest(int stage,int f)
        {
            var c=new Canvas(16,16);Color32 wood=Hex(0x5b5840),woodLight=Hex(0x7e7b56),iron=Hex(0xa0a18a),dark=Hex(0x343a34);
            int lift=stage==0?0:stage==1?1+f:5;
            c.Rect(2,7,12,7,dark);c.Rect(3,8,10,5,wood);c.Rect(3,8,3,4,woodLight);c.Rect(12,8,1,5,Hex(0x424633));
            c.Rect(1,8,2,6,iron);c.Rect(13,8,2,6,iron);c.Rect(2,13,12,2,iron);c.Line(3,13,12,13,Hex(0xc7c3a3));
            if(stage==0)
            {
                c.Rect(2,4,12,5,dark);c.Rect(3,4,10,4,wood);c.Rect(4,5,8,2,woodLight);c.Rect(1,5,2,4,iron);c.Rect(13,5,2,4,iron);
                c.Rect(7,7,3,4,Gold);c.Set(8,9,Ink);c.Set(4+f*2,5,Hex(0xa6a075));
            }
            else
            {
                c.Rect(2,7-lift,12,Math.Max(2,lift),dark);c.Rect(3,7-lift,10,2,woodLight);
                c.Rect(1,7-lift,2,lift+1,iron);c.Rect(13,7-lift,2,lift+1,iron);c.Line(3,7-lift,12,7-lift,iron);
                c.Rect(3,7,10,3,Ink);c.Rect(7,9,3,2,iron);
                if(stage<3)
                {
                    c.Rect(4,8,2,2,Gold);c.Set(7+f%3,7,Gold);c.Set(10,8,Bone);
                    if(stage==2){ReferenceCross(c,5+f%3,3+f%2,Gold);c.Set(11-f,5,Gold);}
                }
                else {c.Set(6+f,10,Hex(0x6a664c));c.Set(11,8,Hex(0x4e5141));}
            }
            c.Outline(Ink);return c;
        }
    }
}
