using System.Collections.Generic;

namespace Framework
{
    /// <summary>
    /// View nameの定義
    /// </summary>
    public class ViewNameDefine
    {
        /// <summary>
        /// Viewの名前の定義
        /// </summary>
        public enum ViewName
        {
            None = 0,
            // quest select
            QuestSelectChapter,
            QuestSelectArea,
            QuestSelectMainDetail,
            QuestSelectSupport,
            QuestSelectEventTop,
            QuestSelectEventSub,
            QuestSelectEventDetail,

            // event quest.
            EventQuestList,
            EventQuestTop,
            EventQuestBoxSummon,
            EventQuestBoxSummonAnim,
            EventQuestReward,
            EventQuestRandomReward,
            EventQuestExchange,
            EventQuestRankingTop,
            EventQuestRankingList,
            EventQuestDetailListPage,

            // login bonus
            LoginBonusMain,
            LoginBonusEvent,
            LoginBonusCharacter,

            // FEH
            CLB01_QuestMap = 214010,
        }

        private static Dictionary<int, string> ViewNameDic = new Dictionary<int, string>()
        {
            // quest select
            { (int)ViewName.QuestSelectChapter,             "Prefabs/OutGame/QuestSelect/QuestSelectChapterPage" },
            { (int)ViewName.QuestSelectArea,                "Prefabs/OutGame/QuestSelect/QuestSelectAreaPage" },
            { (int)ViewName.QuestSelectMainDetail,          "Prefabs/OutGame/QuestSelect/QuestSelectMainDetailPage" },
            { (int)ViewName.QuestSelectSupport,             "Prefabs/OutGame/QuestSupportSelect/QuestSupportCanvas" },
            { (int)ViewName.QuestSelectEventTop,            "Prefabs/OutGame/QuestSelect/Event/QuestEventEventTop" },
            { (int)ViewName.QuestSelectEventSub,            "Prefabs/OutGame/QuestSelect/Event/QuestSelectEventSub" },
            { (int)ViewName.QuestSelectEventDetail,         "Prefabs/OutGame/QuestSelect/Event/QuestSelectEventDetail" },

            // event quest.
            { (int)ViewName.EventQuestList,                 "Prefabs/OutGame/EventQuest/EventQuestListPage" },
            { (int)ViewName.EventQuestTop,                  "Prefabs/OutGame/EventQuest/EventQuestTop" },
            { (int)ViewName.EventQuestBoxSummon,            "Prefabs/OutGame/EventQuest/BoxSummonPage" },
            { (int)ViewName.EventQuestBoxSummonAnim,        "Prefabs/OutGame/EventQuest/BoxSummonAnimPage" },
            { (int)ViewName.EventQuestReward,               "Prefabs/OutGame/EventQuest/EventReward" },
            { (int)ViewName.EventQuestRandomReward,         "Prefabs/OutGame/EventQuest/RandomEventReward" },
            { (int)ViewName.EventQuestExchange,             "Prefabs/OutGame/EventQuest/EventExchangePage" },
            { (int)ViewName.EventQuestRankingTop,           "Prefabs/OutGame/EventQuest/EventQuestRankingTop" },
            { (int)ViewName.EventQuestRankingList,          "Prefabs/OutGame/EventQuest/EventRankingListPage" },
            { (int)ViewName.EventQuestDetailListPage,       "Prefabs/OutGame/EventQuest/EventQuestDetailListPage" },

            // login bonus
            { (int)ViewName.LoginBonusMain,                 "Prefabs/OutGame/LoginBonus/LoginBonusPageMain" },
            { (int)ViewName.LoginBonusEvent,                "Prefabs/OutGame/LoginBonus/LoginBonusPageEvent" },
            { (int)ViewName.LoginBonusCharacter,            "Prefabs/OutGame/LoginBonus/LoginBonusPageCharacter" },

            // CLB01
            { (int)ViewName.CLB01_QuestMap,                 "Prefabs/OutGame/EventQuest/21401/FEHQuestMap" },
        };

        /// <summary>
        /// ViewNameからPage名前の取得
        /// </summary>
        public static string GetPagePath(ViewName ViewName)
        {
            string path = "";
            ViewNameDic.TryGetValue((int)ViewName, out path);
            return path;
        }
    }
}
