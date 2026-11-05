using System;
using UnityEngine;

namespace Ecos.Haunt
{
    public sealed partial class HauntPixelArt
    {
        /// <summary>Reference-sheet guardians: 96px cells, five explicit states and four frames.</summary>
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
            Sprite sheetSprite = TryLoadSheetSprite(key, "bosses/" + id, frame, (int)state, 96, .5f);
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
            return CacheSprite(key, DetailedReferenceGuardian(pixels, variant, state, frame), 32);
        }

        /// <summary>32px environmental state cells. Stage 0 intact; 1 preparing/cracked;
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
            Sprite sheetSprite = TryLoadSheetSprite(key, "props/" + id, frame, stage, 32, .5f);
            if (sheetSprite) return sheetSprite;
            Canvas pixels = id == "barrel-explosive" ? ReferenceWoodBarrel(stage, frame)
                : id == "barrel-acid" ? ReferenceAcidBarrel(stage, frame) : ReferenceChest(stage, frame);
            return CacheSprite(key, pixels, 32);
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

        // Pose construction retains its original 48px skeleton, but material,
        // anatomy and ornament are authored here on the native 96px grid.
        static Canvas DetailedReferenceGuardian(Canvas pose, int variant, BossAnimationState state, int frame)
        {
            var c = new Canvas(96, 96);
            for (int y=0;y<48;y++) for (int x=0;x<48;x++)
            {
                Color32 color = pose.Pixels[(47-y)*48+x];
                if (color.a > 0) c.Rect(x*2,y*2,2,2,color);
            }
            if (state == BossAnimationState.Death) { ReferenceDeathDetail(c,variant,frame); return c; }
            switch(variant)
            {
                case 0: ReferencePumpkinDetail(c,state,frame); break;
                case 1: ReferenceWidowDetail(c,state,frame); break;
                case 2: ReferenceBishopDetail(c,state,frame); break;
                case 3: ReferenceMatriarchDetail(c,state,frame); break;
                case 4: ReferenceBellDetail(c,state,frame); break;
                case 5: ReferenceExecutionerDetail(c,state,frame); break;
                case 6: ReferenceOracleDetail(c,state,frame); break;
            }
            return c;
        }

        static void ReferenceSolidLine(Canvas c,int x0,int y0,int x1,int y1,Color32 color)
        {
            int dx=Math.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Math.Abs(y1-y0),sy=y0<y1?1:-1,error=dx+dy;
            while(true)
            {
                if(x0>=0&&x0<c.Width&&y0>=0&&y0<c.Height && c.Pixels[(c.Height-1-y0)*c.Width+x0].a>0) c.Set(x0,y0,color);
                if(x0==x1&&y0==y1) break;
                int twice=2*error;if(twice>=dy){error+=dy;x0+=sx;}if(twice<=dx){error+=dx;y0+=sy;}
            }
        }
        static void ReferenceRivet(Canvas c,int x,int y,Color32 light,Color32 dark)
        { c.Rect(x,y,2,2,dark);c.Set(x,y,light); }
        static void ReferenceMetalEtching(Canvas c,int x,int y,int width,int height,Color32 bright,Color32 dark)
        {
            ReferenceSolidLine(c,x,y,x+width,y,dark);ReferenceSolidLine(c,x,y+1,x+width-1,y+1,bright);
            ReferenceSolidLine(c,x+1,y+2,x+1,y+height-1,bright);ReferenceSolidLine(c,x+width,y+2,x+width,y+height,dark);
        }

        static void ReferencePumpkinDetail(Canvas c,BossAnimationState state,int f)
        {
            int b=(f==1||f==2)?2:0;
            Color32 rind=Hex(0xffb44c),groove=Hex(0x98451f),moss=Hex(0x73844b),fold=Hex(0x92708f);
            // Curved ribs and the carved surfaces follow the head rather than a flat texture.
            c.Line(39,23+b,36,30+b,groove);c.Line(36,30+b,37,41+b,groove);
            c.Line(44,21+b,41,28+b,Hex(0xffb445));c.Line(41,28+b,40,35+b,rind);
            c.Line(51,20+b,52,27+b,rind);c.Line(60,24+b,64,31+b,groove);
            c.Line(65,30+b,64,42+b,Hex(0xe16b23));c.Set(62,45+b,rind);
            c.Line(48,13+b,48,19+b,Hex(0x324b35));c.Line(51,13+b,50,18+b,moss);
            c.Line(53,13+b,58,11+b,moss);c.Line(57,11+b,60,14+b,Hex(0x526c3c));
            c.Rect(40,32+b,3,2,Gold);c.Set(40,32+b,Pale);c.Line(40,34+b,43,34+b,Hex(0xaa331f));
            c.Rect(58,32+b,3,2,Gold);c.Set(59,32+b,Pale);c.Line(57,34+b,60,34+b,Hex(0xaa331f));
            c.Polygon(new[]{42,42+b,49,45+b,60,41+b,58,48+b,46,49+b,41,46+b},Hex(0x332126));
            c.Line(43,43+b,48,45+b,Hex(0xe6a345));c.Line(50,46+b,58,43+b,Hex(0xc86d29));
            c.Rect(45,44+b,2,3,Hex(0xffd681));c.Rect(50,46+b,2,3,Hex(0xffe7a0));c.Rect(56,43+b,2,3,Gold);
            c.Line(45,52+b,43,75,Hex(0x242936));c.Line(37,51+b,30,78,fold);
            c.Line(32,57+b,25,77,Hex(0x5c425e));c.Line(59,53+b,64,79,Hex(0x261f36));
            for(int i=0;i<6;i++) c.Line(23+i*3,80+(i%2),24+i*3,81+(i%2),Hex(0x96738c));
            c.Rect(44,60+b,13,3,Hex(0x705c41));c.Rect(47,59+b,6,5,Hex(0xc5ac70));c.Rect(49,60+b,2,3,Ink);
            c.Line(43,66+b,50,64+b,Hex(0x7a8473));c.Line(51,65+b,57,64+b,Hex(0x2a3432));
            // Scythe edge and wood have single-pixel bevels at the moving weapon position.
            bool attack=state==BossAnimationState.Attack,tell=state==BossAnimationState.Telegraph;
            int sx=(attack?30-f*2:35)*2,sy=(tell?10-f:attack?20+f*3:16)*2;
            c.Line(sx+1,sy+2,sx-5,81,Hex(0xd1a574));c.Line(sx-1,sy+10,sx-7,78,Hex(0x483631));
            c.Line(sx+4,sy-3,sx+13,sy,Hex(0xe9ead7));c.Line(sx+13,sy,sx+17,sy+6,Hex(0xc0d0c8));
            ReferenceSolidLine(c,sx+15,sy+6,sx+10,sy+13,Hex(0x667e79));
            // Tiny wing membranes and eyes make the orbiting bats readable at the new scale.
            int bx=(8+f*2)*2,by=(12+f%2*3)*2;
            c.Line(bx-8,by-1,bx-3,by+3,Hex(0x635271));c.Line(bx+4,by+3,bx+8,by-1,Hex(0x635271));
            c.Set(bx,by+4,Gold);c.Set(bx+2,by+4,Gold);
            if(state==BossAnimationState.Enrage){c.Set(39,13+f*2,Pale);c.Set(58,9+f,Pale);c.Line(47,6+f,46,12,Gold);}
        }

        static void ReferenceExecutionerDetail(Canvas c,BossAnimationState state,int f)
        {
            int b=(f==1||f==2)?2:0;Color32 steel=Hex(0xb5c4c9),blue=Hex(0x748d9b),dark=Hex(0x283543),rust=Hex(0x806552);
            // Helmet planes, slotted visor and battered brow.
            c.Polygon(new[]{37,20+b,42,18+b,53,19+b,56,24+b,54,27+b,38,26+b},Hex(0x637d8e));
            c.Line(39,19+b,53,19+b,steel);c.Line(40,21+b,40,27+b,Hex(0xc2d1d1));
            c.Line(54,22+b,55,27+b,dark);c.Line(38,26+b,55,26+b,blue);
            c.Rect(37,29+b,20,5,Hex(0x121b27));
            for(int i=0;i<4;i++) { c.Rect(40+i*4,30+b,2,3,Hex(0x3c4b57));c.Set(41+i*4,30+b,state==BossAnimationState.Enrage?Orange:Hex(0xd5bc7c)); }
            c.Line(38,35+b,43,37+b,blue);c.Line(51,36+b,55,34+b,dark);
            c.Line(47,21+b,48,23+b,Hex(0xd5dddd));c.Set(46+f%3,20+b,Hex(0xe3e8da));
            ReferenceRivet(c,37,22+b,steel,dark);ReferenceRivet(c,55,23+b,steel,dark);
            // Separate breastplate facets, embossed funerary medallion and overlapping tassets.
            c.Polygon(new[]{34,51+b,48,48+b,62,51+b,59,59+b,36,59+b},Hex(0x708896));
            c.Line(35,51+b,47,49+b,steel);c.Line(49,49+b,60,52+b,blue);
            c.Line(48,50+b,48,60+b,dark);c.Line(49,51+b,49,59+b,Hex(0xa3b7bd));
            c.Polygon(new[]{38,61+b,48,63+b,58,61+b,57,69+b,40,70+b},Hex(0x3b4d5b));
            c.Line(39,62+b,47,64+b,blue);c.Line(50,64+b,56,62+b,Hex(0x617e8e));
            c.Ellipse(44,55+b,9,9,Hex(0x293743));c.Ellipse(46,56+b,5,5,Hex(0xb7b5a0));
            c.Set(46,58+b,Ink);c.Set(49,58+b,Ink);c.Rect(47,61+b,2,2,Hex(0xc6bf9f));
            c.Rect(31,70+b,35,4,Hex(0x53443b));c.Line(32,70+b,63,70+b,Hex(0x9c8062));
            c.Rect(45,69+b,9,6,Hex(0xadb0a1));c.Rect(47,71+b,5,2,Hex(0x30343d));
            for(int i=0;i<3;i++) {int x=33+i*10;ReferenceMetalEtching(c,x,76,8,7,blue,dark);ReferenceRivet(c,x+2,78,steel,dark);}
            // Pauldron rims and rivets establish curvature; scratches are placed on plates.
            c.Line(22,44+b,32,42+b,steel);c.Line(22,46+b,34,44+b,blue);
            c.Line(64,43+b,72,47+b,steel);c.Line(62,45+b,71,49+b,blue);
            foreach(int x in new[]{23,30,65,71})ReferenceRivet(c,x,49+b,steel,dark);
            c.Line(28,45+b,25,49+b,Hex(0x354957));c.Set(28,45+b,Hex(0xd4d9d1));
            c.Line(67,51+b,69,54+b,rust);c.Line(34,57+b,37,55+b,Hex(0x839ca9));
            for(int side=0;side<2;side++) {int x=side==0?30:54;ReferenceMetalEtching(c,x,80,10,5,steel,dark);c.Line(x+2,83,x+8,83,blue);}
            int hx,hy;
            if(state==BossAnimationState.Attack){hx=new[]{11,33,38,34}[f]*2;hy=new[]{10,13,26,34}[f]*2;}
            else {hx=(state==BossAnimationState.Telegraph?13-f:37)*2;hy=(state==BossAnimationState.Telegraph?5+f:16+f%2)*2;}
            // Steel axe has a keen rim, socket, bevel scratches and wrapped wood shaft.
            ReferenceSolidLine(c,hx-15,hy-5,hx-14,hy+7,Hex(0xe1e5d8));
            ReferenceSolidLine(c,hx+11,hy-5,hx+10,hy+4,Hex(0xd9dfd9));
            ReferenceSolidLine(c,hx-12,hy-2,hx-8,hy+4,blue);ReferenceSolidLine(c,hx+5,hy-5,hx+7,hy-2,dark);
            c.Rect(hx-1,hy-2,3,4,Hex(0x344c5b));c.Set(hx,hy-1,Hex(0xd8d3b6));
            if(state!=BossAnimationState.Attack)for(int i=0;i<4;i++){int y=42+i*5;ReferenceSolidLine(c,70,y,72,y+1,rust);}
            if(state==BossAnimationState.Enrage){c.Line(38,29+b,55,29+b,Hex(0xe7754d));c.Set(39,30+b,Pale);c.Set(54,30+b,Pale);}
        }

        static void ReferenceWidowDetail(Canvas c,BossAnimationState state,int f)
        {
            int b=f%2*2;Color32 silk=Hex(0xc1b8d1),fold=Hex(0x756884),shadow=Hex(0x30293d);
            c.Line(37,25+b,39,20+b,silk);c.Line(39,20+b,46,17+b,Hex(0xe2d3df));
            c.Line(48,18+b,56,22+b,silk);c.Line(56,22+b,59,31+b,fold);
            c.Rect(42,29+b,4,3,Hex(0x863b7b));c.Rect(52,29+b,4,3,Hex(0x863b7b));
            c.Set(43,29+b,Hex(0xff9fce));c.Set(53,29+b,Hex(0xff9fce));c.Set(44,30+b,Pale);
            c.Line(49,35+b,49,43+b,Hex(0x592944));c.Set(48,37+b,Bone);c.Set(51,40+b,Bone);
            for(int side=0;side<2;side++)
            {
                int x=side==0?33:62,sgn=side==0?1:-1;
                c.Line(x,43,x+sgn*3,70,fold);c.Line(x+sgn*2,45,x+sgn*4,71,silk);
                for(int j=0;j<7;j++){int y=46+j*4;c.Line(x+sgn*2,y,x+sgn*5,y+2,Alpha(silk,180));}
            }
            c.Line(44,52,42,78,shadow);c.Line(47,57,48,81,Hex(0x7f6b91));c.Line(55,53,59,81,Hex(0x393046));
            for(int i=0;i<7;i++){int x=31+i*5;c.Line(x,82-(i%3),x+2,83-(i%3),silk);c.Set(x+1,84-(i%3),fold);}
            // Socketed jewels and articulated spider legs.
            c.Ellipse(46,13+b,6,6,Hex(0x63536e));c.Rect(48,14+b,2,3,Pink);c.Set(48,14+b,Pale);
            for(int side=0;side<2;side++)for(int leg=0;leg<3;leg++)
            {
                int x=side==0?(4+leg*3)*2:(43-leg*3)*2,y=(14+leg*4+f%2)*2;
                c.Ellipse(x-1,y-1,3,3,fold);c.Set(x,y-1,silk);c.Set(x+(side==0?1:-1),y+2,Hex(0x4c405d));
            }
            c.Rect(47,52,4,5,Hex(0x9f759c));c.Set(48,53,Pale);c.Set(49,56,Pink);
            c.Set(34+f*3,81-f,Hex(0xd7c7e0));
        }

        static void ReferenceBishopDetail(Canvas c,BossAnimationState state,int f)
        {
            int b=f%2*2;Color32 gold=Hex(0xe7c477),thread=Hex(0xdeb999),shadow=Hex(0x522b3b);
            // Ornate miter and skull anatomy: orbital shadows, teeth, nasal cavity.
            c.Line(35,25+b,38,13+b,gold);c.Line(38,13+b,44,17+b,Hex(0xb88952));
            c.Line(46,16+b,54,9+b,Hex(0xffe1a1));c.Set(53,9+b,Pale);
            c.Rect(43,23+b,13,3,Hex(0x8f663c));c.Rect(46,22+b,4,5,Gold);c.Set(47,23+b,Pale);
            c.Line(39,31+b,43,30+b,Hex(0x8f7068));c.Line(52,30+b,56,32+b,Hex(0x8f7068));
            c.Rect(41,35+b,3,2,Ink);c.Rect(53,35+b,3,2,Ink);c.Set(42,35+b,Hex(0x9f6193));c.Set(54,35+b,Hex(0x9f6193));
            c.Line(48,40+b,47,43+b,Ink);c.Set(49,43+b,Ink);
            c.Line(43,47+b,55,47+b,Hex(0x68575a));for(int i=0;i<5;i++) c.Line(44+i*2,46+b,44+i*2,49+b,Bone);
            // Woven stole, embroidered crosses and dimensional robe pleats.
            for(int side=0;side<2;side++)
            {
                int x=side==0?40:54;c.Line(x,53,x+2,79,thread);c.Line(x+2,54,x+4,79,shadow);
                for(int j=0;j<4;j++){int y=56+j*6;c.Line(x,y,x+4,y,gold);c.Line(x+2,y-2,x+2,y+3,gold);}
            }
            c.Line(29,62,24,83,Hex(0x9e4f61));c.Line(33,60,31,82,shadow);c.Line(62,59,69,83,shadow);
            c.Line(49,57,50,81,Hex(0xf2d4a5));c.Rect(46,65,7,2,gold);c.Rect(48,62,2,8,gold);
            for(int i=0;i<9;i++) c.Set(26+i*5,84-(i%2),gold);
            // Staff coils, jewel setting and single-pixel speculars.
            c.Line(72,22,72,84,Hex(0xd3b67c));for(int i=0;i<6;i++)c.Line(71,31+i*7,74,33+i*7,Hex(0x6f5438));
            c.Rect(69,17+f%2*2,6,4,Hex(0xb48d4b));c.Rect(71,17+f%2*2,2,3,Pale);
        }

        static void ReferenceMatriarchDetail(Canvas c,BossAnimationState state,int f)
        {
            int b=f%2*2;Color32 jade=Hex(0xa7b78b),dark=Hex(0x29463f),rim=Hex(0x778e65);
            c.Line(32,27+b,34,23+b,jade);c.Line(34,23+b,43,21+b,jade);c.Line(49,23+b,58,27+b,rim);
            c.Rect(37,31+b,5,4,Hex(0xdbc67e));c.Rect(49,31+b,5,4,Hex(0xdbc67e));
            c.Rect(39,31+b,1,4,Ink);c.Rect(51,31+b,1,4,Ink);c.Set(38,31+b,Pale);c.Set(50,31+b,Pale);
            c.Polygon(new[]{36,44+b,60,44+b,56,58+b,47,54+b,40,59+b},Ink);
            for(int i=0;i<6;i++){int x=38+i*4;c.Line(x,45+b,x+1,49+b,Bone);c.Set(x,45+b,Pale);}
            c.Line(42,56+b,45,51+b,Hex(0xb2b79a));c.Line(52,52+b,54,56+b,Hex(0x718574));
            // Abdomen armor plates and sockets make shell volume explicit.
            for(int row=0;row<4;row++)
            {
                int y=57+row*5,left=35+row,right=59-row;
                c.Line(left,y,46,y+2,rim);c.Line(47,y+2,right,y,jade);
                c.Line(left+1,y+2,46,y+4,dark);c.Line(48,y+4,right-1,y+2,dark);
            }
            c.Ellipse(43,74,7,5,Hex(0x90aa70));c.Rect(45,75,2,2,Gold);c.Set(45,75,Pale);
            for(int side=0;side<2;side++)for(int leg=0;leg<4;leg++)
            {
                int x=side==0?(5-leg%2*2)*2:(41+leg%2*2)*2,y=(21+leg*4-8+(side==0?f%2:-f%2))*2;
                c.Ellipse(x-1,y-1,3,3,rim);c.Set(x,y-1,jade);
            }
            c.Set(33,48+b,Hex(0xb3c2a1));c.Set(62,46+b,Hex(0xb3c2a1));
            c.Set(44+f*2,75,Gold);c.Set(43+f*2,75,Hex(0xd4e0b5));
        }

        static void ReferenceBellDetail(Canvas c,BossAnimationState state,int f)
        {
            int sway=(f==1?1:f==3?-1:0)*2;Color32 bronze=Hex(0x997144),bright=Hex(0xf0d18c),dark=Hex(0x584539);
            c.Line(40+sway,26,48+sway,24,bright);c.Line(38+sway,29,35+sway,59,Hex(0xd7b873));
            c.Line(62+sway,35,65+sway,62,dark);c.Line(60+sway,37,63+sway,60,bronze);
            // Raised cast bands, worn hammer marks and engraved occult script.
            c.Line(34+sway,35,63+sway,35,Hex(0xc49b5d));c.Line(34+sway,36,63+sway,36,dark);
            c.Line(31+sway,65,67+sway,65,bright);c.Line(29+sway,67,70+sway,67,dark);
            c.Line(23+sway,74,75+sway,74,Hex(0xffe7a8));c.Line(25+sway,77,74+sway,77,bronze);
            for(int i=0;i<8;i++)
            {
                int x=30+sway+i*5;c.Line(x,68,x+2,70,bronze);c.Line(x+2,68,x,70,dark);
                if(i%2==0)c.Set(x+1,67,bright);
            }
            c.Line(42+sway,32,45+sway,33,Hex(0xb18e5a));c.Line(56+sway,39,59+sway,38,dark);
            c.Rect(39+sway,45,5,5,Ink);c.Rect(55+sway,45,5,5,Ink);
            c.Set(40+sway,46,state==BossAnimationState.Enrage?Orange:Gold);c.Set(56+sway,46,Gold);
            c.Line(47+sway,57,51+sway,57,Hex(0x412f31));c.Rect(48+sway,59,2,5,Hex(0x241e2c));
            c.Line(45+sway,81,47+sway,87,Hex(0xb0a1b0));c.Set(47+sway,84,Pale);
            c.Line(38,4,35,1,Hex(0x958e96));c.Line(58,5,61,2,Hex(0x958e96));
        }

        static void ReferenceOracleDetail(Canvas c,BossAnimationState state,int f)
        {
            int b=f%2*2;Color32 pale=Hex(0xb5c59d),moss=Hex(0x7c985d),dark=Hex(0x294b40);
            // Brow folds, reflective eyes, moist lips and individually shaped teeth.
            c.Line(27,33+b,31,28+b,pale);c.Line(31,28+b,41,31+b,moss);c.Line(56,30+b,66,28+b,pale);
            c.Rect(31,35+b,8,3,Ink);c.Rect(59,35+b,8,3,Ink);
            c.Rect(34,35+b,2,3,Gold);c.Rect(62,35+b,2,3,Gold);c.Set(33,35+b,Pale);c.Set(61,35+b,Pale);
            c.Line(37,47+b,44,49+b,Hex(0x9eb795));c.Line(49,49+b,64,46+b,Hex(0x7f9c79));
            c.Line(36,50+b,41,71+b,dark);c.Line(63,51+b,60,72+b,Hex(0x172e30));
            for(int i=0;i<5;i++){int x=40+i*5;c.Polygon(new[]{x,53+b,x+3,53+b,x+2,59+(i%2)*3+b},Bone);c.Set(x,53+b,Pale);}
            c.Polygon(new[]{44,69+b,47,65+b,49,73+b,44,73+b},Hex(0xb7bda0));c.Polygon(new[]{56,70+b,59,65+b,61,72+b},Bone);
            c.Line(40,75+b,59,76+b,Hex(0x55705d));c.Line(41,78,46,80,moss);
            // Wart clusters have a lit cap and shadowed underside, never flat noise.
            foreach(var p in new[]{new[]{27,49},new[]{29,56},new[]{65,43},new[]{69,53},new[]{34,74},new[]{61,77}})
            {c.Ellipse(p[0],p[1]+b,3,3,Hex(0x4f7557));c.Set(p[0],p[1]+b,Hex(0x9caf80));}
            c.Line(26,25+b,23,16,moss);c.Line(23,16,17,14,pale);c.Line(68,25+b,73,14,moss);c.Line(73,14,80,10,pale);
            c.Ellipse(47,15+b,9,9,Hex(0x3f9c87));c.Ellipse(48,16+b,6,6,Mint);c.Set(49,17+b,Pale);c.Set(50,17+b,Pale);
            c.Line(31,80,23,85,moss);c.Line(58,82,69,86,Hex(0x24473d));
            c.Set(31+f*3,79,Hex(0xb0c69e));
        }

        static void ReferenceDeathDetail(Canvas c,int variant,int f)
        {
            Color32 metal=variant==5?Hex(0xa8bdc2):variant==0?Orange:variant==3||variant==6?Mint:Violet;
            int r=10+f*8;
            for(int i=0;i<12;i++)
            {
                double a=(i*30+f*13)*Math.PI/180;
                int x=48+(int)(Math.Cos(a)*r),y=50+(int)(Math.Sin(a)*r);
                c.Line(x,y,x+(i%2==0?3:-2),y+2,metal);c.Set(x+1,y-1,Alpha(Pale,(byte)(230-f*40)));
                if(i%3==0){c.Set(x-1,y+1,Hex(0x67555e));c.Set(x+2,y+2,Alpha(metal,140));}
            }
            if(variant==5){c.Line(30+f*2,78+f*2,35+f*2,80+f*2,Hex(0xc0cac6));c.Set(32+f*2,77+f*2,Ink);}
            if(variant==0){c.Line(45,79-f*3,48,69+f,Gold);c.Set(47,74+f,Pale);c.Set(51+f,81-f,Gold);}
            c.Set(42-f*4,84-f*3,metal);c.Set(61+f*2,86-f*4,Pale);
        }

        static Canvas ReferenceWoodBarrel(int stage,int f)
        {
            var c=new Canvas(32,32);
            Color32 wood=Hex(0x715035),grain=Hex(0xb58a54),edge=Hex(0x58616b),steel=Hex(0xbac0af);
            if(stage>=2)
            {
                if(stage==2)
                {
                    c.Ellipse(6-f%2,6,23+f%2,24,Hex(0x9e3825));
                    c.Polygon(new[]{3,28,7,17,9,20,10,7+f,14,15,17,1+f*2,20,15,25,8+f,26,20,29,17,30,29},Hex(0xf66d29));
                    c.Polygon(new[]{6,28,11,19,12,12+f,16,19,18,6+f,20,18,23,13,26,28},Gold);
                    c.Polygon(new[]{12,28,15,20,16,14+f,18,20,21,24,20,29},Hex(0xfff2b2));
                    c.Rect(15,24,3,5,Pale);c.Set(7+f*3,6,Gold);c.Set(27-f*2,3+f,Orange);
                    c.Line(5,23,1,18-f,grain);c.Line(27,24,30,21-f,wood);c.Line(4,28,1,27,Hex(0x293744));
                    c.Set(8+f,12-f,Pale);c.Set(25-f,17,Gold);
                }
                else
                {
                    c.Ellipse(3+f,3+f,13,13,Alpha(Hex(0x6b6670),180));c.Ellipse(12-f%2,6,15,16,Alpha(Hex(0x41404b),210));
                    c.Line(7+f,5+f,12+f,4+f,Alpha(Hex(0xaaa0a0),115));c.Line(20-f,10,24-f,12,Alpha(Hex(0x817982),120));
                    c.Rect(7,27,17,3,Hex(0x45352e));c.Line(5,26,2,20,grain);c.Line(25,27,30,23,wood);
                    c.Line(10,28,13,29,Hex(0x937153));c.Line(17,27,21,28,Hex(0x18232c));
                    c.Set(11+f,27,Orange);c.Set(16+f,28,Gold);c.Set(23-f,25,Hex(0xcb632d));
                }
                c.Ellipse(4,29,24,2,Alpha(Ink,140));return c;
            }
            // Bulging stave geometry, rounded cap and two curved iron hoops.
            c.Polygon(new[]{7,8,25,8,28,15,28,22,25,28,7,28,4,22,4,15},Hex(0x49372c));
            c.Polygon(new[]{7,9,12,9,11,27,7,27,5,21,5,15},Hex(0xa47c49));
            c.Polygon(new[]{12,9,21,9,23,27,11,27},wood);c.Polygon(new[]{23,9,26,14,27,22,24,27,22,24},Hex(0x4d3d31));
            for(int x=8;x<=23;x+=5){c.Line(x,11,x-1,24,Hex(0x3f3028));c.Line(x+1,12,x,23,Hex(0x987048));}
            c.Line(6,18,8,15,grain);c.Line(11,23,12,20,Hex(0x533a2a));c.Line(23,18,24,22,Hex(0x2c302e));
            c.Ellipse(4,3,24,10,Hex(0x4b4336));c.Ellipse(5,3,22,8,grain);c.Ellipse(7,5,18,5,Hex(0x61462f));
            c.Line(9,6,22,6,Hex(0xa67a4a));c.Line(8,8,23,8,Hex(0x3e332b));c.Line(14,5,14,9,Hex(0x41342b));
            c.Line(5,7,6,5,Hex(0xdfbc7b));c.Line(7,4,21,4,Hex(0xc29a63));
            c.Rect(4,12,24,4,edge);c.Rect(5,12,20,1,steel);c.Line(5,15,25,15,Hex(0x303c49));
            c.Rect(4,24,24,4,Hex(0x47535d));c.Rect(5,24,20,1,Hex(0xa5b3ad));c.Line(7,28,24,28,Hex(0x282c32));
            foreach(int x in new[]{6,13,23}){ReferenceRivet(c,x,13,Hex(0xd4ccb0),Hex(0x35434c));ReferenceRivet(c,x,25,Hex(0xc8c8b0),Hex(0x2a3843));}
            c.Ellipse(12,16,9,7,Bone);c.Rect(14,21,5,3,Bone);c.Rect(14,18,2,2,Ink);c.Rect(18,18,2,2,Ink);
            c.Set(16,20,Ink);c.Set(15,23,Ink);c.Set(17,23,Ink);c.Line(12,22,10,23,Hex(0xbfa474));
            c.Line(20,4,23,1,Hex(0x755034));c.Line(21,4,24,1,Hex(0xc9a470));
            if(stage==1)
            {
                int sx=23+f%3,sy=1+f%2;c.Set(sx,sy,Pale);c.Line(sx-2,sy,sx+2,sy,Gold);c.Set(sx,sy+2,Orange);
                c.Set(29-f,4+f,Gold);c.Set(19+f,0,Orange);c.Set(27-f,7,Orange);
            }
            else c.Set(21+f%3,4,Hex(0xe2be7b));
            c.Set(8+f*3,12,Hex(0xe0dcc8));c.Outline(Ink);return c;
        }

        static Canvas ReferenceAcidBarrel(int stage,int f)
        {
            var c=new Canvas(32,32);Color32 acid=Hex(0x82dc37),lit=Hex(0xc1f77d),deep=Hex(0x45952e),metal=Hex(0x64746c);
            if(stage==3)
            {
                c.Ellipse(1,24,30,7,Alpha(deep,200));c.Ellipse(3,23+f%2,25,6,acid);c.Ellipse(8,25,15,3,lit);
                c.Line(4,26,8,25,lit);c.Line(19,28,26,27,deep);c.Ellipse(8-f%2,19,5,5,acid);
                c.Set(7+f*4,23,lit);c.Set(28-f,26,Pale);c.Set(25-f*3,29,lit);c.Set(3+f,28,acid);
                c.Ellipse(19+f%2,22,3,3,deep);c.Set(20+f%2,22,lit);
                c.Line(13,23,17,22,metal);c.Set(14,22,Hex(0xabb3a0));return c;
            }
            c.Polygon(new[]{6,6,25,6,27,10,27,26,24,29,6,29,4,25,4,10},Hex(0x394e45));
            c.Rect(6,8,19,18,metal);c.Rect(7,10,4,15,Hex(0x93a08b));c.Rect(12,10,3,15,Hex(0x718474));
            c.Rect(23,9,3,17,Hex(0x354f43));c.Line(8,11,8,24,Hex(0xb3b8a1));c.Line(24,13,25,25,Hex(0x243e36));
            c.Ellipse(4,2,24,9,Hex(0x7d8c80));c.Ellipse(5,2,22,7,Hex(0xb5b8a0));c.Ellipse(7,4,19,5,Hex(0x2f4c34));
            c.Ellipse(8,4,17,4,acid);c.Line(10,4,20,4,lit);c.Set(22,5,lit);c.Ellipse(14+f%2,3,3,3,deep);c.Set(14+f%2,3,lit);
            c.Rect(4,11,24,3,Hex(0x50685b));c.Line(5,11,25,11,Hex(0xaab6a0));
            c.Rect(4,24,24,3,Hex(0x445c50));c.Line(5,24,25,24,Hex(0x98ad92));c.Line(7,28,24,28,Hex(0x899c83));
            ReferenceRivet(c,6,12,Hex(0xd1d5b7),Hex(0x304e3f));ReferenceRivet(c,24,25,Hex(0xb6c3a4),Hex(0x304e3f));
            // Dented panel, corroded edges and a faded hazard stencil.
            c.Line(21,16,18,18,Hex(0x304e42));c.Line(18,18,20,21,Hex(0x304e42));c.Line(19,18,22,17,Hex(0xa5b391));
            c.Rect(12,16,7,6,Hex(0xa7b18a));c.Polygon(new[]{15,16,19,21,11,21},Hex(0x526d43));c.Set(15,18,acid);c.Set(15,20,acid);
            c.Line(6,21,7,23,Hex(0x8a8061));c.Set(23,20,Hex(0xb0b48e));
            // Fluid wraps the cap and breaks into animated single-pixel droplets.
            c.Rect(14,7,3,9,acid);c.Line(14,8,14,15,lit);c.Rect(15,16,2,2,deep);c.Set(15,17+f%3,lit);
            c.Set(10+f*3,5,Pale);c.Set(20+f%2,8,acid);
            if(stage>0)
            {
                c.Line(21,13,17,17,Ink);c.Line(17,17,20,20,Ink);c.Line(20,20,18,25,Ink);
                c.Line(20,14,18,17,acid);c.Set(18,18,lit);c.Set(19,21,acid);
            }
            if(stage==2)
            {
                c.Rect(17,18,3,10,acid);c.Line(18,20,18,29,lit);c.Line(20,22,22,28,deep);
                c.Ellipse(8,27,23,4,acid);c.Line(11,28,18,28,lit);c.Set(20+f*2,29,lit);c.Set(27-f,24,acid);
            }
            c.Outline(Ink);return c;
        }

        static Canvas ReferenceChest(int stage,int f)
        {
            var c=new Canvas(32,32);Color32 wood=Hex(0x5b5941),light=Hex(0x8f8a5b),iron=Hex(0x8c9587),ironLight=Hex(0xc5c7a9),dark=Hex(0x2b3734);
            int lift=stage==0?0:stage==1?2+f*2:10;
            c.Polygon(new[]{4,15,28,15,29,18,28,28,4,28,3,18},dark);c.Rect(6,17,21,9,wood);
            c.Rect(6,18,5,7,light);c.Rect(24,17,3,9,Hex(0x424b37));
            c.Line(10,18,10,26,Hex(0x454633));c.Line(18,17,18,26,Hex(0x3e4433));c.Line(21,18,22,23,Hex(0x9b9160));
            c.Line(7,22,9,21,Hex(0xb2a576));c.Line(13,24,16,23,Hex(0x737149));
            c.Rect(3,16,3,12,iron);c.Rect(27,16,3,12,Hex(0x6b7a70));c.Rect(4,26,24,4,iron);
            c.Line(5,26,26,26,ironLight);c.Line(5,29,27,29,dark);
            foreach(int x in new[]{4,27}){ReferenceRivet(c,x,18,ironLight,dark);ReferenceRivet(c,x,26,ironLight,dark);}
            if(stage==0)
            {
                c.Ellipse(4,6,24,13,dark);c.Ellipse(5,7,22,10,light);c.Rect(4,13,24,5,wood);
                c.Line(8,9,24,9,Hex(0xb0a570));c.Line(6,13,26,13,Hex(0x3f4333));c.Line(10,9,10,16,Hex(0x686545));c.Line(20,9,20,16,Hex(0x4d5139));
                c.Rect(4,11,3,7,iron);c.Rect(25,10,3,8,iron);c.Line(5,10,6,8,ironLight);c.Line(26,9,27,12,ironLight);
                c.Rect(13,15,7,8,Hex(0x9f8148));c.Rect(14,15,5,6,Gold);c.Rect(15,17,3,2,Ink);c.Set(16,19,Ink);c.Set(14,16,Pale);
                c.Set(9+f*4,10,Hex(0xc5b57c));
            }
            else
            {
                int top=14-lift;
                c.Rect(4,top,24,lift+2,dark);c.Rect(6,top+1,20,3,wood);c.Line(6,top,26,top,ironLight);
                c.Rect(3,top,3,lift+3,iron);c.Rect(27,top,3,lift+3,iron);c.Line(5,top+3,5,15,Hex(0xc5c7a6));
                c.Line(9,top+2,23,top+2,light);c.Line(13,top+2,13,top+5,Hex(0x3e4533));
                c.Rect(6,15,21,6,Ink);c.Line(7,15,26,15,Hex(0x555b45));c.Rect(14,19,6,3,iron);c.Set(15,19,ironLight);
                c.Rect(7,14,4,3,Hex(0xb49c5d));c.Rect(22,14,3,3,Hex(0x8c865c));
                if(stage<3)
                {
                    c.Ellipse(8,17,6,3,Gold);c.Set(9,17,Pale);c.Ellipse(17+f%2,16,4,3,Hex(0xbe9853));c.Set(22,17,Bone);
                    if(stage==2){ReferenceCross(c,11+f*2,7+f%2,Gold);c.Set(24-f,9,Gold);c.Line(14,10,14,12,Alpha(Orange,160));}
                }
                else {c.Line(9+f,19,12+f,18,Hex(0x4d553e));c.Set(22,18,Hex(0x6c7050));}
            }
            // Verdigris is clustered around hinges; scratches stay on wood planes.
            c.Set(5,23,Hex(0x627e64));c.Set(28,20,Hex(0x5a7b64));c.Set(24,27,Hex(0x637d61));
            c.Outline(Ink);return c;
        }
    }
}
