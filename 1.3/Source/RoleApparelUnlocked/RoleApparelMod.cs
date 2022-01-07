using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
using RimWorld;
using Verse;

namespace RoleApparelUnlocked
{
    public class RoleApparelMod : Mod
    {
        public static RoleApparelMod mod;
        public static RoleApparelSettings settings;

        public static Vector2 scrollPosition = Vector2.zero;

        public List<string> currentApparelLoaded = new List<string>();

        public List<string> CurrentApparelLoaded 
        { 
            get 
            {
                if (currentApparelLoaded.NullOrEmpty())
                {
                    List<string> list = (from x in settings.apparelDictionary.Keys.ToList() orderby x descending select x).ToList();
                    for(int i = list.Count - 1; i >= 0; i--)
                    {
                        ThingDef apparel = DefDatabase<ThingDef>.GetNamedSilentFail(list[i]);
                        if(apparel != null && !currentApparelLoaded.Contains(list[i]))
                        {
                            currentApparelLoaded.Add(list[i]);
                        }
                    }
                }
                return currentApparelLoaded;
            }
        }

        public RoleApparelMod(ModContentPack content) : base(content)
        {
            mod = this;
            settings = GetSettings<RoleApparelSettings>();
        }

        public override string SettingsCategory() => "Role Apparel Unlocker";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect outRect = new Rect(inRect.x, inRect.y, inRect.width, inRect.height);
            Rect rect = new Rect(0f, 0f, inRect.width - 30f, (float)((CurrentApparelLoaded.Count / 2 + 2) * 24));
            Widgets.BeginScrollView(outRect, ref scrollPosition, rect, true);
            listing_Standard.ColumnWidth = rect.width / 2.2f;
            listing_Standard.Begin(rect);
            for (int i = CurrentApparelLoaded.Count - 1; i >= 0; i--)
            {
                if (i == CurrentApparelLoaded.Count / 2)
                {
                    listing_Standard.NewColumn();
                }

                ThingDef apparel = DefDatabase<ThingDef>.GetNamedSilentFail(CurrentApparelLoaded[i]);
                if(apparel != null)
                {
                    bool value = settings.apparelDictionary[CurrentApparelLoaded[i]];
                    listing_Standard.CheckboxLabeled(apparel.LabelCap, ref value, null);
                    settings.apparelDictionary[CurrentApparelLoaded[i]] = value;
                }
            }
            listing_Standard.End();
            Widgets.EndScrollView();

            RoleApparelStartup.GenerateApparelListing(settings);
            RoleApparelStartup.AddAllApparelToRole();
        }
    }

    public class RoleApparelSettings : ModSettings
    {
        public Dictionary<string, bool> apparelDictionary = new Dictionary<string, bool>();

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref apparelDictionary, "apparelDictionary");
        }
    }

    [StaticConstructorOnStartup]
    public static class RoleApparelStartup
    {
        [Unsaved]
        public static List<PreceptApparelRequirement> RoleApparelRequirementsFull = new List<PreceptApparelRequirement>();

        static RoleApparelStartup()
        {
            RoleApparelSettings settings = RoleApparelMod.settings;

            CheckForNewApparel(settings);

            GenerateApparelListing(settings);

            AddAllApparelToRole();
        }

        public static void CheckForNewApparel(RoleApparelSettings settings)
        {
            List<ThingDef> list = (from x in DefDatabase<ThingDef>.AllDefs
                                   where x.IsApparel
                                   select x).ToList();
            foreach (ThingDef def in list)
            {
                if (!settings.apparelDictionary.ContainsKey(def.defName) && IsViableApparel(def))
                {
                    bool headwear = def.apparel.layers.Contains(ApparelLayerDefOf.Overhead);
                    settings.apparelDictionary.Add(def.defName, headwear);
                }
            }
        }

        public static bool IsViableApparel(ThingDef def)
        {
            if(def?.apparel != null && !def.apparel.bodyPartGroups.NullOrEmpty() && !HasBlockedTag(def))
            {
                return true;
            }
            return false;
        }

        public static bool HasBlockedTag(ThingDef def)
        {
            if(def?.apparel != null && !def.apparel.tags.NullOrEmpty())
            {
                if (def.apparel.tags.Contains("WarcasketAll"))
                {
                    return true;
                }
            }
            return false;
        }

        public static void GenerateApparelListing(RoleApparelSettings settings)
        {
            RoleApparelRequirementsFull = new List<PreceptApparelRequirement>();
            IEnumerable<ThingDef> enumerable = from def in DefDatabase<ThingDef>.AllDefs
                                               where (bool)(def?.IsApparel) && IsViableApparel(def) && settings.apparelDictionary.ContainsKey(def.defName)
                                               select def;

            List<ThingDef> allApparel = enumerable.ToList();

            for (int i = 0; i < allApparel.Count(); i++)
            {
                if (allApparel[i]?.apparel != null && !allApparel[i].apparel.bodyPartGroups.NullOrEmpty())
                {
                    ApparelRequirement ar = new ApparelRequirement();
                    ar.bodyPartGroupsMatchAny = new List<BodyPartGroupDef>();
                    ar.bodyPartGroupsMatchAny = allApparel[i].apparel.bodyPartGroups;
                    ar.requiredDefs = new List<ThingDef>();
                    ar.requiredDefs.Add(allApparel[i]);

                    PreceptApparelRequirement par = new PreceptApparelRequirement();
                    par.requirement = ar;
                    RoleApparelRequirementsFull.Add(par);
                }
            }
        }

        public static void AddAllApparelToRole()
        {
            IEnumerable<PreceptDef> enumerable = from def in DefDatabase<PreceptDef>.AllDefs
                                                 where def.preceptClass.BaseType == typeof(Precept_Role)
                                                 select def;

            List<PreceptDef> allRoles = enumerable.ToList();

            for (int i = 0; i < allRoles.Count(); i++)
            {
                ApplyToRole(allRoles[i]);
            }
        }

        public static void ApplyToRole(PreceptDef role)
        {
            role.roleApparelRequirements = RoleApparelRequirementsFull;
        }
    }
}
