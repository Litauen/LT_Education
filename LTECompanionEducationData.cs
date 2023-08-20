using LT.UI.MapNotification;
using System;
using System.Collections.Generic;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace LT_Education
{

    public class LTECompanionEducationData
    {
        [SaveableField(1)]
        public MBGUID Id;

        [SaveableField(2)]
        public float CanRead;

        [SaveableField(3)]
        public float[] BookProgress;

        [SaveableField(4)]
        public int BookInProgress;

        // select next book automatically? 0 - no, 1 - random, 2 - by highest skill, 3 - by lowest skill
        [SaveableField(5)]
        public int AutoRead;

        public LTECompanionEducationData(MBGUID HeroId)
        {
            this.Id = HeroId;
            this.CanRead = 0;
            this.BookInProgress = -1;
            this.BookProgress = new float[100];
            this.AutoRead = 0;
            for (int i = 0; i < 100; i++)
            {
                this.BookProgress[i] = 0f;
            }
        }

    }



    public class CustomSaveDefiner : SaveableTypeDefiner
    {
        public CustomSaveDefiner() : base(1885468684)  { }

        protected override void DefineClassTypes()
        {
            base.AddClassDefinition(typeof(LTECompanionEducationData), 1);
            base.AddClassDefinition(typeof(LTECanReadMapNotification), 10);
        }

        protected override void DefineContainerDefinitions()
        {
            base.ConstructContainerDefinition(typeof(List<LTECompanionEducationData>));
        }
    }

}

